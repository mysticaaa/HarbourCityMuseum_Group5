using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using Unity.XR.CoreUtils;

namespace Wjy.Museum
{
    [DisallowMultipleComponent, RequireComponent(typeof(WjyFishingRodGrab), typeof(Rigidbody))]
    public sealed class WjyFishingRodCatchInteraction : MonoBehaviour
    {
        public enum RodState { Display, Held, Charging, Flying, InWater, Returning, FishHooked, NoBite, Reeling, Escaped, PracticeLanded }
        internal enum PracticeIssue { GripReleased, WeakSwing }
        internal event System.Action<RodState, RodState> StateChanged;
        internal event System.Action<PracticeIssue> PracticeIssueDetected;
        internal event System.Action<WjyCatchKind> CatchLanded;
        internal bool HeldInLeftHand => holdingHand?.node == XRNode.LeftHand;
        private WjyFishingCatchVisuals visuals;
        private Text pedestalInstructions;
        private WjyMuseumPauseMenu menu;
        private WjyOceanFishingSession oceanSession;
        private WjyOceanCatchReveal oceanReveal;
        private bool UsesViewResultPanel => oceanReveal && oceanSession && oceanSession.UseMuseumExhibitLayout;
        private bool IsGalleryPractice => oceanSession && oceanSession.UseMuseumExhibitLayout && !oceanSession.AtSea;
        private bool revealPlayed, returningCatch;
        private Vector3 returnHookStart;
        internal Text PedestalInstructions => pedestalInstructions;
        internal Transform DisplayModel => visuals?.RodMotion;
        private float waterElapsed, reelTime, nextHaptic;
        private Vector3 waterLanding, reelStart;
        private Vector3 driftPosition, driftTarget, driftAxis;
        private float biteDeadline, resultDelay;
        private static readonly WjyCatchKind[] CatchKinds = (WjyCatchKind[])System.Enum.GetValues(typeof(WjyCatchKind));
        private readonly List<WjyCatchKind> availableCatches = new List<WjyCatchKind>(CatchKinds);
        private bool catchCommitted;
        // A separate random stream keeps drifting independent of the bite roll.
        private readonly System.Random driftRandom = new System.Random(System.Guid.NewGuid().GetHashCode());
        public bool HasFish { get; private set; }
        public bool FloatVisible => visuals != null && visuals.Float.gameObject.activeSelf;
        public bool FishVisible => visuals != null && visuals.Fish.gameObject.activeSelf;
        public string CatchMessage => visuals?.Message;
        private bool IsInWater => State == RodState.InWater || State == RodState.FishHooked || State == RodState.NoBite || State == RodState.Escaped;
        [Header("Existing shared player")]
        [SerializeField] private XROrigin sharedRig;
        [Header("Rod and line")]
        [SerializeField] private Transform rodTip;
        [SerializeField] private Transform[] lineGuides;
        [SerializeField] private Transform hook;
        [SerializeField] private LineRenderer fishingLine;
        [SerializeField] private BoxCollider waterSurface;
        [SerializeField] private TextMesh instructions;
        [Header("Display and casting")]
        [SerializeField] private float displayDegreesPerSecond = 15f;
        [SerializeField] private float hangingLineLength = 1.05f;
        [SerializeField] private float minimumSwingSpeed = .35f;
        [SerializeField] private float returnDuration = .4f;

        private sealed class Hand
        {
            public XRRayInteractor ray;
            public XRNode node;
            public bool gripWasDown, triggerWasDown;
        }
        private readonly List<Hand> hands = new List<Hand>();
        private const float SwingWindow = .22f;
        private struct Motion { public float time; public Vector3 velocity; }
        private readonly Queue<Motion> motions = new Queue<Motion>();
        private bool TapToCast => oceanSession && oceanSession.PressTriggerToCast;
        private Vector3 flightStart;
        private string DisplayHint => TapToCast ? "GRIP: pick up\nTRIGGER: tap to cast" : "GRIP: pick up\nTRIGGER: hold, swing, release";
        private string CastAgainHint => TapToCast ? "Tap TRIGGER to cast again" : "Hold TRIGGER and swing to cast again";
        private WjyFishingRodGrab grab;
        private Rigidbody body;
        private Hand holdingHand;
        private Transform homeParent;
        private Transform hookHomeParent;
        private Vector3 homePosition, homeScale, returnPosition, lastTipPosition, hookVelocity;
        private Quaternion homeRotation, returnRotation;
        private float returnTime, lastSampleTime, flightTime, castDuration;
        private readonly RaycastHit[] practiceHits = new RaycastHit[16];
        private bool triggerReady;
        private XRRayInteractor forceGrabRay;
        private bool previousForceGrab;
        public RodState State { get; private set; }
        public XRRayInteractor HoldingRay => holdingHand?.ray;
        public Vector3 HookPosition => hook.position;
        public XROrigin SharedRig => sharedRig;
#if UNITY_EDITOR
        // Automated checks use the same state machine without a physical headset.
        public bool EditorSkipDevicePolling { get; set; }
#endif

        private void Awake()
        {
            grab = GetComponent<WjyFishingRodGrab>();
            body = GetComponent<Rigidbody>();
            homeParent = transform.parent;
            oceanSession = homeParent.GetComponent<WjyOceanFishingSession>();
            homePosition = transform.localPosition;
            homeRotation = transform.localRotation;
            homeScale = transform.localScale;
            hookHomeParent = hook ? hook.parent : null;
            grab.selectEntered.AddListener(OnGrabbed);
            grab.selectExited.AddListener(OnReleased);
        }

        private void Start()
        {
            if (!sharedRig || !rodTip || !hook || !fishingLine || (!waterSurface && !oceanSession))
            {
                Debug.LogError("[WJY] Fishing rod is missing its rig, hook, line or water reference.", this);
                enabled = false;
                return;
            }
            foreach (var ray in sharedRig.GetComponentsInChildren<XRRayInteractor>(true))
            {
                // Sample rigs name the ray "Ray Interactor"; the handedness is on an ancestor.
                // Hand-tracking rays are intentionally excluded from controller Grip polling.
                for (var ancestor = ray.transform; ancestor && ancestor != sharedRig.transform; ancestor = ancestor.parent)
                {
                    if (ancestor.name.Contains("Left Controller"))
                    { hands.Add(new Hand { ray = ray, node = XRNode.LeftHand }); break; }
                    if (ancestor.name.Contains("Right Controller"))
                    { hands.Add(new Hand { ray = ray, node = XRNode.RightHand }); break; }
                }
            }
            visuals = new WjyFishingCatchVisuals();
            visuals.Create(transform, hook, homeParent, rodTip, lineGuides);
            menu = homeParent ? homeParent.GetComponent<WjyMuseumPauseMenu>() : null;
            CreatePedestalInstructions();
            SetState(RodState.Display, DisplayHint);
            RestHook();
            DrawLine();
        }

        private void Update()
        {
#if UNITY_EDITOR
            if(EditorSkipDevicePolling)return;
#endif
            bool menuOpen = menu && menu.IsOpen;
            foreach (var hand in hands)
            {
                var device = InputDevices.GetDeviceAtXRNode(hand.node);
                bool connected = device.isValid;
                bool gripDown = connected && Button(device, CommonUsages.gripButton, CommonUsages.grip);
                bool triggerDown = connected && Button(device, CommonUsages.triggerButton, CommonUsages.trigger);
                if (holdingHand == hand)
                {
                    if (!connected || !hand.ray.isActiveAndEnabled)
                        EndGrip();
                    else if (!gripDown)
                        ReleaseGrip();
                    else if (menuOpen)
                    {
                        triggerReady = false;
                        if (State == RodState.Charging)
                            SetState(RodState.Held, "Hold TRIGGER and swing to cast");
                    }
                    else
                    {
                        if (!triggerDown) triggerReady = true;
                        ProcessTrigger(triggerDown && !hand.triggerWasDown,
                            !triggerDown && hand.triggerWasDown);
                    }
                }
                else if (!menuOpen && holdingHand == null && State == RodState.Display && gripDown && !hand.gripWasDown)
                    TryGrip(hand.ray);
                hand.gripWasDown = gripDown;
                hand.triggerWasDown = triggerDown;
            }
        }

        private static bool Button(InputDevice device, InputFeatureUsage<bool> button, InputFeatureUsage<float> axis)
        {
            if (device.TryGetFeatureValue(button, out bool pressed)) return pressed;
            return device.TryGetFeatureValue(axis, out float value) && value >= .65f;
        }

        // Called only on the Grip rising edge. Shared input bindings remain unchanged.
        public bool TryGrip(XRRayInteractor ray)
        {
            if (State != RodState.Display || holdingHand != null || !sharedRig ||
                !PointsAtRod(ray) || !ray.transform.IsChildOf(sharedRig.transform)) return false;
            return BeginGripSelection(ray);
        }

        private bool PointsAtRod(XRRayInteractor ray)
        {
            if (!ray || !ray.isActiveAndEnabled || ray.hasSelection) return false;
            if (IsGalleryPractice && ray.TryGetCurrentRaycast(out _, out _, out _, out _, out bool uiClosest) && uiClosest) return false;
            return ray.TryGetCurrent3DRaycastHit(out var hit) && hit.collider && hit.collider.transform.IsChildOf(transform);
        }

        internal bool TryGetPointingHand(out bool leftHand)
        {
            foreach (var hand in hands)
            {
                if (!hand.ray || hand.ray.lineType != XRRayInteractor.LineType.StraightLine || !PointsAtRod(hand.ray)) continue;
                leftHand = hand.node == XRNode.LeftHand;
                return true;
            }
            leftHand = false;
            return false;
        }

        internal bool BeginGripSelection(XRRayInteractor ray)
        {
            if (oceanSession && !oceanSession.AtSea && !oceanSession.UseMuseumExhibitLayout) return false;
            var hand = hands.Find(h => h.ray == ray);
            var manager = ray.interactionManager;
            if (hand == null || !manager || !manager.isActiveAndEnabled || ray.hasSelection) return false;
            grab.interactionManager = manager;
            grab.AuthorizedRay = ray;
            if (!manager.IsSelectPossible(ray, grab)) { grab.AuthorizedRay = null; return false; }
            forceGrabRay = ray;
            previousForceGrab = ray.useForceGrab;
            ray.useForceGrab = true; // Grip brings only this exhibit to the hand.
            ray.StartManualInteraction((IXRSelectInteractable)grab);
            if (!grab.IsSelected(ray))
            {
                if (ray.isPerformingManualInteraction) ray.EndManualInteraction();
                grab.AuthorizedRay = null;
                RestoreForceGrab();
                return false;
            }
            return true;
        }

        private void OnGrabbed(SelectEnterEventArgs args)
        {
            holdingHand = hands.Find(h => ReferenceEquals(h.ray, args.interactorObject));
            triggerReady = false; // A Trigger already held while grabbing cannot cast accidentally.
            motions.Clear();
            lastTipPosition = rodTip.position;
            lastSampleTime = Time.time;
            SetState(RodState.Held, TapToCast ? "Keep GRIP held\nAim and tap TRIGGER to cast" :
                "Hold GRIP to inspect\nHold TRIGGER + swing\nRelease TRIGGER to cast");
            (args.interactorObject as XRBaseControllerInteractor)?.SendHapticImpulse(.2f,.07f);
        }

        public void ReleaseGrip()
        {
            if (holdingHand != null && IsGalleryPractice && (!menu || !menu.IsOpen))
                PracticeIssueDetected?.Invoke(PracticeIssue.GripReleased);
            EndGrip();
        }

        private void EndGrip()
        {
            var ray = holdingHand?.ray;
            if (ray && ray.isPerformingManualInteraction && grab.IsSelected(ray)) ray.EndManualInteraction();
            else if (holdingHand != null) BeginReturn();
        }

        private void OnReleased(SelectExitEventArgs args) => BeginReturn();
        private void BeginReturn()
        {
            if (TapToCast && State == RodState.FishHooked) HasFish = false;
            returningCatch = oceanReveal && HasFish && !catchCommitted;
            returnHookStart = hook.position;
            if (!UsesViewResultPanel) CommitCatch();
            if (visuals != null)
            {
                if (UsesViewResultPanel) visuals.ShowMessage(null);
                visuals.Float.gameObject.SetActive(false);
                visuals.Fish.gameObject.SetActive(HasFish);
            }
            RestoreForceGrab();
            holdingHand = null;
            grab.AuthorizedRay = null;
            triggerReady = false;
            hookVelocity = Vector3.zero;
            motions.Clear();
            transform.SetParent(homeParent,true);
            returnPosition = transform.localPosition;
            returnRotation = transform.localRotation;
            returnTime = 0;
            body.useGravity = false;
            body.isKinematic = true;
            SetState(RodState.Returning,"Returning to display");
        }

        private void RestoreForceGrab()
        {
            if (forceGrabRay) forceGrabRay.useForceGrab = previousForceGrab;
            forceGrabRay = null;
        }

        private void ProcessTrigger(bool pressed, bool released)
        {
            if (pressed && triggerReady && (IsInWater || State == RodState.PracticeLanded))
            {
                if (UsesViewResultPanel) visuals.ShowMessage(null);
                reelStart = hook.position;
                reelTime = 0;
                visuals.Float.gameObject.SetActive(false);
                visuals.Fish.gameObject.SetActive(HasFish);
                SetState(RodState.Reeling, "Reeling in...");
                return;
            }
            if (pressed && triggerReady && State == RodState.Held)
            {
                HasFish = false;
                catchCommitted = false;
                revealPlayed = false;
                visuals.Float.gameObject.SetActive(false);
                visuals.Fish.gameObject.SetActive(false);
                visuals.ShowMessage(null);
                motions.Clear();
                RestHook();
                if (TapToCast)
                {
                    LaunchTapCast();
                    return;
                }
                SetState(RodState.Charging,"Swing the rod, then\nrelease TRIGGER");
            }
            if (released && State == RodState.Charging)
            {
                Vector3 swing = RecentSwing();
                if (swing.magnitude < minimumSwingSpeed)
                {
                    SetState(RodState.Held,"Swing before releasing\nTRIGGER");
                    if (IsGalleryPractice) PracticeIssueDetected?.Invoke(PracticeIssue.WeakSwing);
                }
                else LaunchHook(swing);
            }
        }

        private void LaunchTapCast()
        {
            const float range = 6f;
            var ray = holdingHand.ray;
            var pointer = ray.rayOriginTransform ? ray.rayOriginTransform : ray.transform;
            var view = sharedRig.Camera.transform;
            var heading = Vector3.ProjectOnPlane(pointer.forward, Vector3.up);
            // A nearly vertical pointer has no reliable bearing on the water.
            if (heading.sqrMagnitude < .04f) heading = Vector3.ProjectOnPlane(view.forward, Vector3.up);
            if (heading.sqrMagnitude < .04f) heading = Vector3.Cross(view.right, Vector3.up);
            heading.Normalize();
            var start = rodTip.position;
            var target = start + heading * range;
            if (IsGalleryPractice)
            {
                target.y = sharedRig.transform.position.y;
            }
            else
            {
                var bounds = waterSurface.bounds;
                // Even a cast across the deck must reach open water beyond the far rail.
                float clearOfDeck = Vector3.Dot(bounds.center - start, heading) + 3f;
                target = start + heading * Mathf.Max(range, clearOfDeck);
                target.x = Mathf.Clamp(target.x, bounds.min.x + .35f, bounds.max.x - .35f);
                target.z = Mathf.Clamp(target.z, bounds.min.z + .35f, bounds.max.z - .35f);
                target.y = SurfaceHeight(target);
            }
            BeginFlight(target, .65f);
        }

        private Vector3 RecentSwing()
        {
            Vector3 best = Vector3.zero;
            foreach(var motion in motions)
                if (Time.time-motion.time <= SwingWindow && motion.velocity.sqrMagnitude>best.sqrMagnitude)
                    best = motion.velocity;
            return best;
        }

        public void LaunchHook(Vector3 swingVelocity)
        {
            if (holdingHand == null || State != RodState.Charging || swingVelocity.magnitude<minimumSwingSpeed) return;
            float speed = swingVelocity.magnitude;
            Vector3 direction = Vector3.ProjectOnPlane(swingVelocity, Vector3.up).normalized;
            Vector3 target;
            if (IsGalleryPractice)
            {
                var feet = sharedRig.Camera.transform.position;
                feet.y = sharedRig.transform.position.y;
                target = OpenCastTarget(direction, speed, feet);
                target.y = feet.y;
            }
            else
            {
                var bounds = waterSurface.bounds;
                target = oceanReveal ? OpenCastTarget(direction, speed, bounds.center)
                    : bounds.center + direction * Mathf.Clamp(speed * .12f, .1f, .6f);
                target.x = Mathf.Clamp(target.x, bounds.min.x + .12f, bounds.max.x - .12f);
                target.z = Mathf.Clamp(target.z, bounds.min.z + .12f, bounds.max.z - .12f);
                target.y = bounds.max.y;
            }
            float duration = Mathf.Clamp(Vector3.Distance(rodTip.position,target)/(3f+speed*.4f),.55f,1.3f);
            BeginFlight(target, duration);
        }

        private void BeginFlight(Vector3 target, float duration)
        {
            flightStart = rodTip.position;
            hook.position = flightStart;
            hook.SetParent(homeParent,true); // Flight stays in world space while the hand continues moving.
            castDuration = duration;
            hookVelocity = (target - flightStart) / duration;
            if (!TapToCast) hookVelocity -= Physics.gravity * (.5f * duration);
            flightTime = 0;
            SetState(RodState.Flying,"Casting...");
            holdingHand.ray.SendHapticImpulse(.25f,.07f);
        }

        private Vector3 OpenCastTarget(Vector3 direction, float speed, Vector3 center)
        {
            float range = Mathf.Clamp(3f + speed * 1.4f, 4f, 12f);
            if (direction.sqrMagnitude < .01f)
                direction = Vector3.ProjectOnPlane(sharedRig.Camera.transform.forward, Vector3.up).normalized;
            if (direction.sqrMagnitude < .01f) direction = Vector3.forward;
            var target = rodTip.position + direction * range;
            // Keep a short cast clear of the raft, or of the visitor during a gallery rehearsal.
            var offset = Vector3.ProjectOnPlane(target - center, Vector3.up);
            return offset.sqrMagnitude < 3.6f * 3.6f ? center + direction * 3.6f : target;
        }

        private void FixedUpdate()
        {
            if (menu && menu.IsOpen) return;
            if (State == RodState.Flying) AdvanceHook(Time.fixedDeltaTime);
            else if (IsInWater) AdvanceFishing(Time.fixedDeltaTime);
            else if (State == RodState.Reeling) AdvanceReel(Time.fixedDeltaTime);
        }

        internal void AdvanceFishing(float dt)
        {
            if (!IsInWater) return;
            waterElapsed += dt;
            if (State == RodState.InWater && waterElapsed >= resultDelay)
            {
                HasFish = Random.value < .8f;
                if (HasFish)
                {
                    if (availableCatches.Count == 0)
                        availableCatches.AddRange(CatchKinds);
                    visuals.SelectCatch(availableCatches[Random.Range(0, availableCatches.Count)]);
                    biteDeadline = waterElapsed + 3f;
                }
                SetState(HasFish ? RodState.FishHooked : RodState.NoBite,
                    HasFish ? "Fish on!\nTRIGGER: reel in" : "TRIGGER: reel in and try again");
                visuals.ShowMessage(HasFish ? (TapToCast ? "Fish on! Tap Trigger to reel in within 3 seconds!" : "Something is hooked! Reel in within 3 seconds!")
                    : "Fish don\u2019t seem to like you. Try again.");
            }
            if (State == RodState.FishHooked && waterElapsed >= biteDeadline)
            {
                HasFish = false;
                visuals.Fish.gameObject.SetActive(false);
                SetState(RodState.Escaped, "The fish is gone. Try again!");
                visuals.ShowMessage("The fish is gone. Try again!", true);
            }
            var bounds = waterSurface.bounds;
            if (TapToCast)
                driftPosition = waterLanding + driftAxis * (.24f * Mathf.Sin(waterElapsed * 1.2f));
            else
            {
                driftPosition = Vector3.MoveTowards(driftPosition, driftTarget, .14f * dt);
                if ((driftPosition - driftTarget).sqrMagnitude < .0025f) ChooseDriftTarget();
            }
            bool biting = State == RodState.FishHooked;
            Vector3 movement = Vector3.up * (Mathf.Sin(waterElapsed * 2.6f) * .008f);
            if (biting)
            {
                movement += new Vector3(Mathf.Sin(waterElapsed * 23f) * .025f,
                    Mathf.Sin(waterElapsed * 31f) * .025f, Mathf.Cos(waterElapsed * 19f) * .015f);
                if (waterElapsed >= nextHaptic)
                {
                    holdingHand?.ray.SendHapticImpulse(.28f, .09f);
                    nextHaptic = waterElapsed + .35f;
                }
            }
            Vector3 floatPosition = driftPosition + movement;
            floatPosition.x = Mathf.Clamp(floatPosition.x, bounds.min.x + .04f, bounds.max.x - .04f);
            floatPosition.z = Mathf.Clamp(floatPosition.z, bounds.min.z + .04f, bounds.max.z - .04f);
            if (oceanReveal) floatPosition.y = SurfaceHeight(floatPosition) + movement.y;
            visuals.Float.position = floatPosition;
            visuals.Float.rotation = Quaternion.identity;
            hook.position = visuals.Float.position + Vector3.down * .22f;
            hook.rotation = Quaternion.identity;
        }

        private void ChooseDriftTarget()
        {
            var bounds = waterSurface.bounds;
            if (oceanReveal)
            {
                // Each float wanders near its actual splash rather than crossing the raft.
                bounds = new Bounds(waterLanding - Vector3.up * .05f, new Vector3(1.25f,.1f,1.25f));
            }
            driftTarget = new Vector3(
                Mathf.Lerp(bounds.min.x + .09f, bounds.max.x - .09f, (float)driftRandom.NextDouble()),
                bounds.max.y,
                Mathf.Lerp(bounds.min.z + .09f, bounds.max.z - .09f, (float)driftRandom.NextDouble()));
        }

        internal void AdvanceReel(float dt)
        {
            if (State != RodState.Reeling) return;
            reelTime += dt;
            Vector3 target = rodTip.position + Vector3.down * hangingLineLength + transform.right * .12f;
            float t = Mathf.SmoothStep(0, 1, Mathf.Clamp01(reelTime / .8f));
            MoveRetrievedHook(reelStart, target, t);
            if (t < 1) return;
            RestHook();
            CommitCatch();
            SetState(RodState.Held, CastAgainHint);
            if (HasFish) visuals.ShowMessage("You caught " + visuals.CatchLabel + "!\n" + CastAgainHint + ".");
        }

        private void CommitCatch()
        {
            // Reserve on bite; consume only when the catch is actually brought back.
            // A later Grip release must not consume the same reward twice.
            if (!HasFish || catchCommitted || visuals == null) return;
            availableCatches.Remove(visuals.CatchKind);
            catchCommitted = true;
            CatchLanded?.Invoke(visuals.CatchKind);
        }

        internal void QueueCollectionMilestone(int discoveredCount)
        {
            visuals.QueueCollectionMilestone(discoveredCount, CatchKinds.Length, oceanReveal);
        }

        private void MoveRetrievedHook(Vector3 start, Vector3 end, float t)
        {
            Vector3 previous = hook.position;
            hook.position = Vector3.Lerp(start, end, t);
            if (!oceanReveal || !HasFish) return;
            hook.position += Vector3.up * (2.4f * t * (1f - t));
            float aboveWater = hook.position.y - SurfaceHeight(hook.position);
            if (!revealPlayed && aboveWater >= 0f)
            {
                float before = previous.y - SurfaceHeight(previous);
                var atSurface = Vector3.Lerp(previous, hook.position, Mathf.InverseLerp(before, aboveWater, 0f));
                atSurface.y = SurfaceHeight(atSurface);
                revealPlayed = true;
                oceanReveal.Play((int)visuals.CatchKind, atSurface, sharedRig.Camera.transform);
            }
        }

        private float SurfaceHeight(Vector3 point)
        {
            if (!oceanReveal) return waterSurface.bounds.max.y;
            return homeParent.position.y + WjyOceanEnvironment.WaveHeight(homeParent.InverseTransformPoint(point), Time.time);
        }

        // Move the same rod between its two holders so its collection round survives a visit.
        internal void PlaceAt(Transform holder, Vector3 position, Quaternion rotation, BoxCollider water, WjyOceanCatchReveal reveal)
        {
            HasFish = false;
            EndGrip();
            returningCatch = revealPlayed = catchCommitted = false;
            homeParent = holder;
            homePosition = position;
            homeRotation = rotation;
            transform.SetParent(holder, false);
            transform.SetLocalPositionAndRotation(position, rotation);
            transform.localScale = homeScale;
            body.position = transform.position;
            body.rotation = transform.rotation;
            waterSurface = water;
            oceanReveal = reveal;
            visuals.Float.SetParent(holder, false);
            visuals.PlacePanel(holder, UsesViewResultPanel ? sharedRig.Camera : null);
            visuals.Float.gameObject.SetActive(false);
            visuals.Fish.gameObject.SetActive(false);
            visuals.ShowMessage(null);
            RestHook();
            DrawLine();
            SetState(RodState.Display, DisplayHint);
        }

        internal void AdvanceHook(float dt)
        {
            bool practice = IsGalleryPractice;
            bool straight = TapToCast;
            if (practice || straight) dt = Mathf.Min(dt, castDuration - flightTime);
            Vector3 previous = hook.position;
            flightTime += dt;
            Vector3 next;
            if (straight)
                next = flightStart + hookVelocity * flightTime;
            else
            {
                next = previous + hookVelocity*dt + Physics.gravity*(.5f*dt*dt);
                hookVelocity += Physics.gravity*dt;
            }
            if (practice)
            {
                bool hitSurface = TryPracticeContact(previous, next, out var contact);
                hook.position = hitSurface ? contact.point + contact.normal * .012f : next;
                if (hitSurface || flightTime >= castDuration)
                {
                    hookVelocity = Vector3.zero;
                    SetState(RodState.PracticeLanded, "Practice cast landed\nPress TRIGGER to reel in");
                    holdingHand.ray.SendHapticImpulse(.12f, .06f);
                }
                return;
            }
            if (straight)
            {
                hook.position = next;
                if (flightTime >= castDuration) LandInWater(next);
                return;
            }
            var bounds = waterSurface.bounds;
            float startHeight = previous.y - SurfaceHeight(previous);
            float endHeight = next.y - SurfaceHeight(next);
            if (startHeight >= 0f && endHeight <= 0f)
            {
                Vector3 crossing = Vector3.Lerp(previous,next,Mathf.InverseLerp(startHeight,endHeight,0f));
                if (crossing.x>=bounds.min.x && crossing.x<=bounds.max.x && crossing.z>=bounds.min.z && crossing.z<=bounds.max.z)
                {
                    LandInWater(crossing);
                    return;
                }
            }
            hook.position = next;
            if (flightTime>4f || next.y < (oceanReveal ? bounds.max.y - 1f : -1f))
            {
                RestHook();
                SetState(RodState.Held, CastAgainHint);
            }
        }

        private void LandInWater(Vector3 landing)
        {
            waterLanding = new Vector3(landing.x, SurfaceHeight(landing), landing.z);
            driftPosition = waterLanding;
            if (TapToCast)
            {
                float angle = (float)driftRandom.NextDouble() * Mathf.PI * 2f;
                driftAxis = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            }
            else ChooseDriftTarget();
            waterElapsed = nextHaptic = 0;
            resultDelay = Random.Range(1f, 3f);
            HasFish = false;
            visuals.Float.position = waterLanding;
            visuals.Float.rotation = Quaternion.identity;
            visuals.Float.gameObject.SetActive(true);
            hook.position = waterLanding + Vector3.down * .22f;
            hookVelocity = Vector3.zero;
            SetState(RodState.InWater,"Wait for a bite...\nTRIGGER: reel in\nRelease GRIP: return");
            holdingHand?.ray.SendHapticImpulse(.12f,.06f);
        }

        private bool TryPracticeContact(Vector3 from, Vector3 to, out RaycastHit closest)
        {
            closest = default;
            var segment = new Ray(from, to - from);
            float distance = Vector3.Distance(from, to);
            var hits = practiceHits;
            int count = Physics.RaycastNonAlloc(segment, hits, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            if (count == hits.Length)
            {
                hits = Physics.RaycastAll(segment, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                count = hits.Length;
            }
            for (int i = 0; i < count; i++)
            {
                var hit = hits[i];
                var obstacle = hit.collider.transform;
                if (obstacle.IsChildOf(transform) || obstacle.IsChildOf(sharedRig.transform)) continue;
                if (!closest.collider || hit.distance < closest.distance) closest = hit;
            }
            return closest.collider != null;
        }

        private void LateUpdate()
        {
            if (!rodTip || !hook || !fishingLine) return;
            if (State == RodState.Display) transform.Rotate(Vector3.up,displayDegreesPerSecond*Time.deltaTime,Space.World);
            if (State == RodState.Returning)
            {
                returnTime += Time.deltaTime;
                float t = Mathf.SmoothStep(0,1,Mathf.Clamp01(returnTime/returnDuration));
                transform.localPosition = Vector3.Lerp(returnPosition,homePosition,t);
                transform.localRotation = Quaternion.Slerp(returnRotation,homeRotation,t);
                transform.localScale = homeScale;
                if (returningCatch)
                {
                    MoveRetrievedHook(returnHookStart, rodTip.position + Vector3.down * hangingLineLength + transform.right * .12f, t);
                    if (t >= 1f && UsesViewResultPanel) visuals.ShowMessage("You caught " + visuals.CatchLabel + "!");
                }
                if (t >= 1f)
                {
                    CommitCatch();
                    returningCatch = false;
                    SetState(RodState.Display, DisplayHint);
                }
            }
            Vector3 panelOrigin = oceanReveal ? homeParent.position + new Vector3(0,.35f,2.4f)
                : waterSurface ? waterSurface.transform.TransformPoint(waterSurface.center) : transform.position;
            visuals?.Pose(Time.time, State == RodState.FishHooked, panelOrigin, sharedRig.Camera,
                State == RodState.Held || State == RodState.Display);
            if (State != RodState.Flying && State != RodState.PracticeLanded && !IsInWater && State != RodState.Reeling && !returningCatch) RestHook();
            if (holdingHand != null && !TapToCast) SampleTipMotion();
            DrawLine();
        }

        private void SampleTipMotion()
        {
            float now = Time.time;
            float dt = now - lastSampleTime;
            if (dt <= .0001f) return;
            motions.Enqueue(new Motion { time = now, velocity = (rodTip.position - lastTipPosition) / dt });
            while (motions.Count > 0 && now - motions.Peek().time > SwingWindow) motions.Dequeue();
            lastSampleTime = now;
            lastTipPosition = rodTip.position;
        }

        internal void DismissBasinMessage() => visuals?.ShowMessage(null);

        private void RestHook()
        {
            if(hook.parent!=hookHomeParent)hook.SetParent(hookHomeParent,true);
            hook.position = rodTip.position+Vector3.down*hangingLineLength+transform.right*.12f;
            hook.rotation = Quaternion.identity;
        }

        private void DrawLine()
        {
            const int hangingSegments = 16;
            int guides = lineGuides.Length;
            bool throughFloat = IsInWater && FloatVisible;
            fishingLine.positionCount = guides+hangingSegments+1+(throughFloat?1:0);
            for(int i=0;i<guides;i++) fishingLine.SetPosition(i,lineGuides[i].position);
            for(int i=0;i<=hangingSegments;i++)
            {
                float t=i/(float)hangingSegments;
                float sag=State==RodState.Flying?.08f:IsInWater?.16f:.025f;
                Vector3 p=Vector3.Lerp(rodTip.position,throughFloat?visuals.Float.position:hook.position,t)+Vector3.down*(4f*t*(1f-t)*sag);
                fishingLine.SetPosition(guides+i,p);
            }
            if (throughFloat) fishingLine.SetPosition(fishingLine.positionCount-1, hook.position);
        }

        private void CreatePedestalInstructions()
        {
            if (!instructions) return;
            var canvasObject = new GameObject("Fitted Fishing Instructions", typeof(RectTransform), typeof(Canvas));
            var face = (RectTransform)canvasObject.transform;
            face.SetParent(instructions.transform.parent, false);
            face.localPosition = instructions.transform.localPosition;
            face.localRotation = instructions.transform.localRotation;
            face.localScale = Vector3.one * .001f;
            face.sizeDelta = new Vector2(830, 500);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = sharedRig.Camera;

            pedestalInstructions = new GameObject("Instructions", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            var label = pedestalInstructions.rectTransform;
            label.SetParent(face, false);
            label.anchorMin = Vector2.zero;
            label.anchorMax = Vector2.one;
            label.offsetMin = new Vector2(15, 15);
            label.offsetMax = new Vector2(-15, -15);
            pedestalInstructions.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            pedestalInstructions.fontSize = 64;
            pedestalInstructions.resizeTextForBestFit = true;
            pedestalInstructions.resizeTextMinSize = 30;
            pedestalInstructions.resizeTextMaxSize = 64;
            pedestalInstructions.alignment = TextAnchor.MiddleCenter;
            pedestalInstructions.color = new Color(.14f, .12f, .08f);
            pedestalInstructions.raycastTarget = false;
            instructions.GetComponent<MeshRenderer>().enabled = false;
        }

        private void SetState(RodState state,string message)
        {
            var previous = State;
            State=state;
            if (previous != state) StateChanged?.Invoke(previous, state);
            // The gallery pedestal is the Go Fishing button's permanent caption.
            if (oceanSession && oceanSession.UseMuseumExhibitLayout) return;
            string caption = message + "\n<b>Start the Fishing Experience</b>\n(7 Surprises!)";
            if (pedestalInstructions) pedestalInstructions.text = caption;
            else if (instructions) instructions.text = caption;
        }
        private void OnApplicationPause(bool paused) { if(paused)EndGrip(); }
        private void OnApplicationFocus(bool focused) { if(!focused)EndGrip(); }
        private void OnDisable()
        {
            if(!grab)return;
            HasFish = false; // Disabling an unfinished attempt does not collect its reward.
            EndGrip();
            if(homeParent)transform.SetParent(homeParent,false);
            transform.localPosition=homePosition;
            transform.localRotation=homeRotation;
            transform.localScale=homeScale;
            if(hook && rodTip && hookHomeParent)RestHook();
            HasFish = false;
            if (visuals != null)
            {
                visuals.Float.gameObject.SetActive(false);
                visuals.Fish.gameObject.SetActive(false);
                visuals.ShowMessage(null);
                visuals.Pose(0, false, Vector3.zero, null);
            }
            State=RodState.Display;
        }
        private void OnDestroy()
        {
            if (pedestalInstructions) Destroy(pedestalInstructions.transform.parent.gameObject);
            visuals?.Dispose();
            if(!grab)return;
            grab.selectEntered.RemoveListener(OnGrabbed);
            grab.selectExited.RemoveListener(OnReleased);
        }
    }
}

