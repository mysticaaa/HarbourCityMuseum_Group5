using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using Unity.XR.CoreUtils;

namespace Wjy.Museum
{
    [DisallowMultipleComponent, RequireComponent(typeof(WjyFishingRodGrab), typeof(Rigidbody))]
    public sealed class WjyFishingRodCatchInteraction : MonoBehaviour
    {
        public enum RodState { Display, Held, Charging, Flying, InWater, Returning, FishHooked, NoBite, Reeling, Escaped }
        private WjyFishingCatchVisuals visuals;
        private float waterElapsed, reelTime, nextHaptic;
        private Vector3 waterLanding, reelStart;
        private Vector3 driftPosition, driftTarget;
        private float biteDeadline, resultDelay;
        private readonly List<WjyCatchKind> availableCatches = new List<WjyCatchKind>(
            (WjyCatchKind[])System.Enum.GetValues(typeof(WjyCatchKind)));
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
        private struct Motion { public float time; public Vector3 velocity; }
        private readonly Queue<Motion> motions = new Queue<Motion>();
        private WjyFishingRodGrab grab;
        private Rigidbody body;
        private Hand holdingHand;
        private Transform homeParent;
        private Transform hookHomeParent;
        private Vector3 homePosition, homeScale, returnPosition, lastTipPosition, hookVelocity;
        private Quaternion homeRotation, returnRotation;
        private float returnTime, lastSampleTime, flightTime;
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
            homePosition = transform.localPosition;
            homeRotation = transform.localRotation;
            homeScale = transform.localScale;
            hookHomeParent = hook ? hook.parent : null;
            grab.selectEntered.AddListener(OnGrabbed);
            grab.selectExited.AddListener(OnReleased);
        }

        private void Start()
        {
            if (!sharedRig || !rodTip || !hook || !fishingLine || !waterSurface)
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
            SetState(RodState.Display, "GRIP: pick up\nTRIGGER: hold, swing, release");
            RestHook();
            DrawLine();
        }

        private void Update()
        {
#if UNITY_EDITOR
            if(EditorSkipDevicePolling)return;
#endif
            foreach (var hand in hands)
            {
                var device = InputDevices.GetDeviceAtXRNode(hand.node);
                bool connected = device.isValid;
                bool gripDown = connected && Button(device, CommonUsages.gripButton, CommonUsages.grip);
                bool triggerDown = connected && Button(device, CommonUsages.triggerButton, CommonUsages.trigger);
                if (holdingHand == hand)
                {
                    if (!connected || !gripDown || !hand.ray.isActiveAndEnabled)
                        ReleaseGrip();
                    else
                    {
                        if (!triggerDown) triggerReady = true;
                        ProcessTrigger(triggerDown && !hand.triggerWasDown,
                            !triggerDown && hand.triggerWasDown);
                    }
                }
                else if (holdingHand == null && State == RodState.Display && gripDown && !hand.gripWasDown)
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
            if (!ray || State != RodState.Display || holdingHand != null || ray.hasSelection
                || !ray.isActiveAndEnabled || !sharedRig || !ray.transform.IsChildOf(sharedRig.transform)
                || !ray.TryGetCurrent3DRaycastHit(out var hit)
                || !hit.collider || !hit.collider.transform.IsChildOf(transform)) return false;
            return BeginGripSelection(ray);
        }

        internal bool BeginGripSelection(XRRayInteractor ray)
        {
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
            SetState(RodState.Held, "Hold GRIP to inspect\nHold TRIGGER + swing\nRelease TRIGGER to cast");
            (args.interactorObject as XRBaseControllerInteractor)?.SendHapticImpulse(.2f,.07f);
        }

        public void ReleaseGrip()
        {
            var ray = holdingHand?.ray;
            if (ray && ray.isPerformingManualInteraction && grab.IsSelected(ray)) ray.EndManualInteraction();
            else if (holdingHand != null) BeginReturn();
        }

        private void OnReleased(SelectExitEventArgs args) => BeginReturn();
        private void BeginReturn()
        {
            CommitCatch();
            if (visuals != null)
            {
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
            if (pressed && triggerReady && IsInWater)
            {
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
                visuals.Float.gameObject.SetActive(false);
                visuals.Fish.gameObject.SetActive(false);
                visuals.ShowMessage(null);
                motions.Clear();
                RestHook();
                SetState(RodState.Charging,"Swing the rod, then\nrelease TRIGGER");
            }
            if (released && State == RodState.Charging)
            {
                Vector3 swing = RecentSwing();
                if (swing.magnitude < minimumSwingSpeed)
                    SetState(RodState.Held,"Swing before releasing\nTRIGGER");
                else LaunchHook(swing);
            }
        }

        private Vector3 RecentSwing()
        {
            Vector3 best = Vector3.zero;
            foreach(var motion in motions)
                if (Time.time-motion.time <= .22f && motion.velocity.sqrMagnitude>best.sqrMagnitude)
                    best = motion.velocity;
            return best;
        }

        public void LaunchHook(Vector3 swingVelocity)
        {
            if (holdingHand == null || State != RodState.Charging || swingVelocity.magnitude<minimumSwingSpeed) return;
            // Keep the practice cast inside the small water exhibit. Swing direction moves the landing point.
            var bounds = waterSurface.bounds;
            Vector3 direction = Vector3.ProjectOnPlane(swingVelocity,Vector3.up).normalized;
            Vector3 target = bounds.center + direction * Mathf.Clamp(swingVelocity.magnitude*.12f,.1f,.6f);
            target.x = Mathf.Clamp(target.x,bounds.min.x+.12f,bounds.max.x-.12f);
            target.z = Mathf.Clamp(target.z,bounds.min.z+.12f,bounds.max.z-.12f);
            target.y = bounds.max.y;
            hook.position = rodTip.position;
            hook.SetParent(homeParent,true); // Flight stays in world space while the hand continues moving.
            float duration = Mathf.Clamp(Vector3.Distance(hook.position,target)/(3f+swingVelocity.magnitude*.4f),.55f,1.3f);
            hookVelocity = (target-hook.position-Physics.gravity*(.5f*duration*duration))/duration;
            flightTime = 0;
            SetState(RodState.Flying,"Casting...");
            holdingHand.ray.SendHapticImpulse(.25f,.07f);
        }

        private void FixedUpdate()
        {
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
                        availableCatches.AddRange((WjyCatchKind[])System.Enum.GetValues(typeof(WjyCatchKind)));
                    visuals.SelectCatch(availableCatches[Random.Range(0, availableCatches.Count)]);
                    biteDeadline = waterElapsed + 3f;
                }
                SetState(HasFish ? RodState.FishHooked : RodState.NoBite,
                    HasFish ? "Fish on!\nTRIGGER: reel in" : "TRIGGER: reel in and try again");
                visuals.ShowMessage(HasFish ? "Something is hooked! Reel in within 3 seconds!"
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
            driftPosition = Vector3.MoveTowards(driftPosition, driftTarget, .14f * dt);
            if ((driftPosition - driftTarget).sqrMagnitude < .0025f) ChooseDriftTarget();
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
            visuals.Float.position = floatPosition;
            visuals.Float.rotation = Quaternion.identity;
            hook.position = visuals.Float.position + Vector3.down * .22f;
            hook.rotation = Quaternion.identity;
        }

        private void ChooseDriftTarget()
        {
            var bounds = waterSurface.bounds;
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
            hook.position = Vector3.Lerp(reelStart, target, t);
            if (t < 1) return;
            RestHook();
            CommitCatch();
            SetState(RodState.Held, "Hold TRIGGER and swing to cast again");
            if (HasFish) visuals.ShowMessage("You caught " + visuals.CatchLabel + "!\nHold TRIGGER and swing to cast again.");
        }

        private void CommitCatch()
        {
            // Reserve on bite; consume only when the catch is actually brought back.
            // A later Grip release must not consume the same reward twice.
            if (!HasFish || catchCommitted || visuals == null) return;
            availableCatches.Remove(visuals.CatchKind);
            catchCommitted = true;
        }

        internal void AdvanceHook(float dt)
        {
            Vector3 previous = hook.position;
            Vector3 next = previous + hookVelocity*dt + Physics.gravity*(.5f*dt*dt);
            hookVelocity += Physics.gravity*dt;
            flightTime += dt;
            var bounds = waterSurface.bounds;
            float surface = bounds.max.y;
            if (previous.y>=surface && next.y<=surface)
            {
                Vector3 crossing = Vector3.Lerp(previous,next,Mathf.InverseLerp(previous.y,next.y,surface));
                if (crossing.x>=bounds.min.x && crossing.x<=bounds.max.x && crossing.z>=bounds.min.z && crossing.z<=bounds.max.z)
                {
                    waterLanding = new Vector3(crossing.x,surface,crossing.z);
                    driftPosition = waterLanding;
                    ChooseDriftTarget();
                    waterElapsed = nextHaptic = 0;
                    resultDelay = Random.Range(1f, 3f); // One fresh wait time per water entry.
                    HasFish = false;
                    visuals.Float.position = waterLanding;
                    visuals.Float.rotation = Quaternion.identity;
                    visuals.Float.gameObject.SetActive(true);
                    hook.position = waterLanding + Vector3.down * .22f;
                    hookVelocity = Vector3.zero;
                    SetState(RodState.InWater,"Wait for a bite...\nTRIGGER: reel in\nRelease GRIP: return");
                    holdingHand?.ray.SendHapticImpulse(.12f,.06f);
                    return;
                }
            }
            hook.position = next;
            if (flightTime>4f || next.y<-1f)
            {
                RestHook();
                SetState(RodState.Held,"Hold TRIGGER and swing to cast again");
            }
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
                if (t>=1) SetState(RodState.Display,"GRIP: pick up\nTRIGGER: hold, swing, release");
            }
            visuals?.Pose(Time.time, State == RodState.FishHooked, waterSurface.bounds.center, sharedRig.Camera);
            if (State != RodState.Flying && !IsInWater && State != RodState.Reeling) RestHook();
            if (holdingHand != null)
            {
                float dt = Time.time-lastSampleTime;
                if (dt>.0001f)
                {
                    motions.Enqueue(new Motion { time=Time.time, velocity=(rodTip.position-lastTipPosition)/dt });
                    while(motions.Count>0 && Time.time-motions.Peek().time>.22f) motions.Dequeue();
                }
                lastSampleTime = Time.time;
                lastTipPosition = rodTip.position;
            }
            DrawLine();
        }

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

        private void SetState(RodState state,string message)
        {
            State=state;
            if(instructions)instructions.text=message;
        }
        private void OnApplicationPause(bool paused) { if(paused)ReleaseGrip(); }
        private void OnApplicationFocus(bool focused) { if(!focused)ReleaseGrip(); }
        private void OnDisable()
        {
            if(!grab)return;
            HasFish = false; // Disabling an unfinished attempt does not collect its reward.
            ReleaseGrip();
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
            visuals?.Dispose();
            if(!grab)return;
            grab.selectEntered.RemoveListener(OnGrabbed);
            grab.selectExited.RemoveListener(OnReleased);
        }
    }
}

