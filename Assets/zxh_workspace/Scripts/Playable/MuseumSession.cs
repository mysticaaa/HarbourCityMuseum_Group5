using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using Unity.XR.CoreUtils;
using UnityEngine.InputSystem;

namespace ZxhMuseum
{
    public sealed class MuseumSession : MonoBehaviour
    {
        public static MuseumSession Instance { get; private set; }
        public const string InteriorPath = "Assets/zxh_workspace/Scenes/ShipInterior.unity";
        public XROrigin Origin { get; private set; }
        public Transform ShipSource { get; private set; }
        public bool UsesTeammateMuseum => boatOnly;
        private bool boatOnly;
        private CabinInputScope inputScope;
        private BoatViewHeight viewHeight;
        public bool InCabin { get; private set; }
        public bool Busy { get; private set; }
        public bool Seated { get; private set; }
        public bool CabinVisited { get; private set; }
        public bool HelmetVisited { get; set; }
        public int StoriesRead { get; private set; }
        private QuestExhibitSetup exhibits;
        private readonly List<GameObject> hidden = new List<GameObject>();
        private Vector3 museumPosition;
        private Quaternion museumRotation;
        private Vector3 standingPosition;
        private Quaternion standingRotation;
        private readonly List<LocomotionProvider> disabledLocomotion = new List<LocomotionProvider>();
        private WorldPanel entry, welcome, story;
        private float visitTime;
        private int storyIndex;
        private readonly bool[] visitedStories = new bool[3];
        private UnityEngine.UI.Image curtain;
        private UnityEngine.InputSystem.InputAction backAction;
        private WorldPanel helmetInfo;
        private InspectionReplica replica;
        private WorldPanel seatedControls;
        public NarrationPlayer Narrator { get; private set; }
        private readonly string[] storyTexts = {
            "1909-1935: UNDERWATER PORT WORK\nLondon Museum records a helmet used by Port of London Authority divers. Their work included salvage and clearing propellers.\nInternational example; our model is not that accession. Source: London Museum DK86.21/2.",
            "1948: SHIPS AND MIGRATION\nEmpire Windrush arrived at Tilbury on 22 June 1948, carrying passengers from the Caribbean and elsewhere.\nInternational example, not this fishing boat's history. Source: Royal Museums Greenwich.",
            "1970s ONWARD: REMEMBERING LIFE AT SEA\nRoyal Museums Greenwich's oral-history recordings preserve maritime memories, including work and migration.\nOur cabin and fictional log are teaching schematics, not an oral-history quotation." };
        public void Initialize(QuestExhibitSetup setup)
        {
            exhibits = setup;
            InitializeBoat(setup.xrOrigin, setup.ship, false);
        }
        public void InitializeBoat(XROrigin origin, Transform source, bool isolated = true)
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this; Origin = origin; ShipSource = source; boatOnly = isolated;
            gameObject.AddComponent<BoatWalkController>().Initialize(this);
            Narrator = gameObject.AddComponent<NarrationPlayer>();
            if (!boatOnly) WorldPanel.EnsureEventSystem();
            var eventSystem = UnityEngine.EventSystems.EventSystem.current;
            if (!boatOnly && eventSystem != null) eventSystem.transform.SetParent(transform, true);
            backAction = new UnityEngine.InputSystem.InputAction("ZXH_Back", UnityEngine.InputSystem.InputActionType.Button);
            backAction.AddBinding("<XRController>{RightHand}/secondaryButton");
            backAction.AddBinding("<XRController>{LeftHand}/secondaryButton");
            backAction.Enable();
            var camera = Origin.Camera;
            var fadeObject = new GameObject("ZXH_ComfortFade", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.Image));
            fadeObject.transform.SetParent(camera.transform, false);
            fadeObject.transform.localPosition = Vector3.forward * 0.4f;
            fadeObject.GetComponent<RectTransform>().sizeDelta = Vector2.one * 2;
            fadeObject.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            fadeObject.GetComponent<Canvas>().sortingOrder = 1000;
            curtain = fadeObject.GetComponent<UnityEngine.UI.Image>();
            curtain.color = Color.clear; curtain.raycastTarget = false;
            if (boatOnly) return;
            Vector3 at = exhibits.ship.position + new Vector3(0.9f, 0.7f, 0.6f);
            entry = WorldPanel.Create(transform, "Explore the fishing boat", at, FacingVisitor(at));
            entry.Status.text = "Click the ship for details.\nBoard the original boat's deck, then walk through its wheelhouse door.";
            entry.Button("Board the fishing boat", RequestEnter);
            entry.Button("Tour summary / restart", ShowSummary);
            Vector3 front = camera.transform.position + Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up).normalized * 1.5f;
            welcome = WorldPanel.Create(transform, "Harbour City Museum", front, FacingVisitor(front));
            welcome.Status.text = "Left stick: move. Right stick: snap turn.\nUse the grip to select objects and hold the helmet; release to return.\nUse the index trigger for UI buttons. Core tour: about five minutes.";
            welcome.Button("Start / skip practice", () => welcome.gameObject.SetActive(false));
            welcome.Button("Comfort: seated or standing", () => welcome.Status.text = "Both modes use rays. Teleport and snap-turn are available.\nVirtual seats do not mean a real chair is behind you.");
            welcome.Button("Audio on / off", () => { AudioListener.volume = AudioListener.volume > 0 ? 0 : 1; });
            welcome.Button("Listen to introduction", () => Narrator.Play("Welcome", "Use the left stick to move and the right stick to turn. Point either ray and use the grip to select objects. Hold the helmet, then release. Use the index trigger for menu buttons."));
            Vector3 storiesAt = exhibits.ship.position + new Vector3(-1.2f, 0.6f, 0.5f);
            story = WorldPanel.Create(transform, "Port-city stories", storiesAt, FacingVisitor(storiesAt));
            story.Status.text = storyTexts[0];
            story.Button("Next story", NextStory);
            story.Button("Previous story", () => { storyIndex = Mathf.Max(0, storyIndex - 1); story.Status.text = storyTexts[storyIndex]; });
            story.Button("Sources / reconstruction", () => story.Status.text = "London Museum: DK86.21/2\nRoyal Museums Greenwich: Windrush history; Oral histories\nFull links in zxh_workspace/PlayableDelivery.md.\nNo museum images copied; this cabin is schematic.");
            story.Button("Finish / summary", ShowSummary);
            story.Button("Listen to this story", () => Narrator.Play(new[] { "Work", "Migration", "Memories" }[storyIndex], storyTexts[storyIndex]));
            Vector3 helmetAt = exhibits.helmet.position + new Vector3(0.75f, 0.25f, 0.4f);
            helmetAt.y = Mathf.Clamp(exhibits.helmet.position.y - 0.15f, 1f, 1.25f);
            helmetInfo = WorldPanel.Create(transform, "Diving helmet inspection", helmetAt, FacingVisitor(helmetAt), 650);
            helmetInfo.Status.text = "Hold grip to inspect in your hand, or use a magnified visual copy.\nModel details require historical source verification.";
            helmetInfo.Button("Magnify without holding", () =>
            {
                CloseInspection(); HelmetVisited = true;
                replica = InspectionReplica.Create(exhibits.helmet, transform, helmetInfo.transform.position + Vector3.up * 0.7f);
            });
            helmetInfo.Button("Turn visual copy left", () => replica?.Turn(-30));
            helmetInfo.Button("Turn visual copy right", () => replica?.Turn(30));
            helmetInfo.Button("Close visual copy", CloseInspection);
            helmetInfo.Button("Listen to helmet guide", () => Narrator.Play("Helmet", "Inspect the shell, viewing ports and neck interface. Fittings vary; unseen components are not claimed. Use the magnified copy if holding the grip is difficult."));
            helmetInfo.Button("Shell", () => helmetInfo.Status.text = "The shell encloses the diver's head.\nMaterial and construction details for this model await verified sources.");
            helmetInfo.Button("Viewing ports", () => helmetInfo.Status.text = "Viewing ports let the diver observe surroundings.\nExamine visible openings; do not infer hidden construction.");
            helmetInfo.Button("Air supply", () => helmetInfo.Status.text = "Surface-supplied diving uses an external air supply.\nExact fittings vary; this model's connections need source verification.");
            helmetInfo.Button("Communications", () => helmetInfo.Status.text = "Some diving systems support communications.\nNo unseen communication part is claimed for this model.");
            helmetInfo.Button("Neck interface", () => helmetInfo.Status.text = "The helmet connects to the rest of the diving equipment at the neck.\nTreat the current model as an observation aid, not a dismantling guide.");
        }
        private Quaternion FacingVisitor(Vector3 point)
        {
            Vector3 direction = point - Origin.Camera.transform.position; direction.y = 0;
            return Quaternion.LookRotation(direction.sqrMagnitude > 0.001f ? direction : Vector3.forward);
        }
        public void RepositionWelcome()
        {
            if (welcome == null) return;
            Vector3 front = Origin.Camera.transform.position + Vector3.ProjectOnPlane(Origin.Camera.transform.forward, Vector3.up).normalized * 1.5f;
            welcome.transform.position = front; welcome.transform.rotation = FacingVisitor(front);
        }
        private void NextStory()
        {
            if (!visitedStories[storyIndex]) { visitedStories[storyIndex] = true; StoriesRead++; }
            storyIndex = (storyIndex + 1) % storyTexts.Length; story.Status.text = storyTexts[storyIndex];
        }
        private void ShowSummary()
        {
            entry.Status.text = "Cabin: " + (CabinVisited ? "visited" : "optional") + " | Helmet: " + (HelmetVisited ? "inspected" : "optional") +
                "\nStories explored: " + StoriesRead + "/3\nVisit time: " + Mathf.FloorToInt(visitTime / 60) + " min. Continue freely or restart.";
            if (welcome != null)
            {
                welcome.gameObject.SetActive(true); welcome.Status.text = entry.Status.text;
                if (!restartAdded) { welcome.Button("Restart tour", RestartTour); restartAdded = true; }
            }
        }
        private bool restartAdded;
        public void RestartTour()
        {
            if (Busy || InCabin) return;
            ReleaseSelections(); exhibits.ship.GetComponent<ShipDetailInspection>()?.Restore();
            CloseInspection();
            CabinVisited = HelmetVisited = false; StoriesRead = 0; visitTime = 0; storyIndex = 0;
            System.Array.Clear(visitedStories, 0, visitedStories.Length);
            timeNotified = false;
            story.Status.text = storyTexts[0]; welcome.gameObject.SetActive(false);
            entry.Status.text = "New tour. Inspect the exhibits or enter the cabin.";
        }
        public void RequestEnter() { if (!Busy && !InCabin) StartCoroutine(Enter()); }
        public void RequestReturn() { if (!Busy && InCabin) StartCoroutine(Return()); }
        private IEnumerator Fade(float target)
        {
            float start = curtain.color.a;
            for (float t = 0; t < 1; t += Time.unscaledDeltaTime / 0.2f)
            { curtain.color = new Color(0, 0, 0, Mathf.Lerp(start, target, t)); yield return null; }
            curtain.color = new Color(0, 0, 0, target);
        }
        private IEnumerator Enter()
        {
            Busy = true;
            Narrator.Stop();
            CloseInspection();
            ReleaseSelections(); ShipSource.GetComponent<ShipDetailInspection>()?.Restore();
            yield return new WaitForSeconds(0.5f);
            yield return Fade(1);
            museumPosition = Origin.transform.position; museumRotation = Origin.transform.rotation;
            Scene interior = SceneManager.GetSceneByPath(InteriorPath);
            if (!interior.isLoaded)
            {
                if (!Application.CanStreamedLevelBeLoaded(InteriorPath))
                { Debug.LogError("Cabin not in Build Settings. Run ZXH > Build vensail cabin integration."); if (entry != null) entry.Status.text = "Cabin not in Build Settings."; yield return Fade(0); Busy = false; yield break; }
                if (boatOnly) { inputScope = gameObject.AddComponent<CabinInputScope>(); inputScope.Acquire(); }
                var loading = SceneManager.LoadSceneAsync(InteriorPath, LoadSceneMode.Additive);
                if (loading == null) { yield return Fade(0); Busy = false; yield break; }
                yield return loading;
            }
            var museum = gameObject.scene;
            hidden.Clear();
            foreach (GameObject root in museum.GetRootGameObjects())
            {
                if (root == gameObject || root == Origin.transform.root.gameObject ||
                    root.GetComponentInChildren<XRInteractionManager>(true) != null) continue;
                if (root.activeSelf) { hidden.Add(root); root.SetActive(false); }
            }
            foreach (Transform panel in transform)
                if (panel.GetComponent<UnityEngine.EventSystems.EventSystem>() == null) panel.gameObject.SetActive(false);
            InCabin = CabinVisited = true;
            var deck = FindObjectOfType<ShipDeckEnvironment>();
            if (deck == null) throw new System.InvalidOperationException("Fishing boat deck missing after scene load");
            MoveTo(deck.SpawnPoint, 0);
            if (viewHeight == null) viewHeight = gameObject.AddComponent<BoatViewHeight>();
            viewHeight.Apply(Origin);
            GetComponent<NoHeadsetPreview>()?.RecenterLook();
            yield return Fade(0); Busy = false;
        }
        private IEnumerator Return()
        {
            Busy = true; ReleaseSelections();
            Narrator.Stop();
            yield return Fade(1);
            if (Seated) RestoreSeatPose();
            var cabin = SceneManager.GetSceneByPath(InteriorPath);
            if (cabin.isLoaded) yield return SceneManager.UnloadSceneAsync(cabin);
            foreach (GameObject root in hidden) if (root != null) root.SetActive(true);
            hidden.Clear();
            foreach (Transform panel in transform) panel.gameObject.SetActive(true);
            if (welcome != null) welcome.gameObject.SetActive(false);
            if (viewHeight != null) viewHeight.Restore();
            SetOriginPose(museumPosition, museumRotation);
            InCabin = false; yield return Fade(0); Busy = false;
            if (inputScope != null) { inputScope.Release(); Destroy(inputScope); inputScope = null; }
        }
        public void MoveTo(Vector3 floorPoint, float yaw)
        {
            float delta = Mathf.DeltaAngle(Origin.Camera.transform.eulerAngles.y, yaw);
            var rotation = Quaternion.AngleAxis(delta, Vector3.up) * Origin.transform.rotation;
            SetOriginPose(Origin.transform.position, rotation);
            Vector3 offset = Vector3.ProjectOnPlane(Origin.Camera.transform.position - Origin.transform.position, Vector3.up);
            SetOriginPose(floorPoint - offset, rotation);
        }
        private void SetOriginPose(Vector3 position, Quaternion rotation)
        {
            var controller = Origin.GetComponent<CharacterController>();
            bool active = controller != null && controller.enabled;
            if (controller != null) controller.enabled = false;
            Origin.transform.SetPositionAndRotation(position, rotation);
            if (controller != null) controller.enabled = active;
        }
        public void Sit(Vector3 headPoint, float yaw)
        {
            if (!InCabin || Busy || Seated) return;
            StartCoroutine(SeatTransition(headPoint, yaw));
        }
        private IEnumerator SeatTransition(Vector3 headPoint, float yaw)
        {
            Busy = true; ReleaseSelections(); yield return Fade(1);
            standingPosition = Origin.transform.position; standingRotation = Origin.transform.rotation;
            disabledLocomotion.Clear();
            foreach (var provider in Origin.GetComponentsInChildren<LocomotionProvider>())
                if (provider.enabled && !(provider is SnapTurnProviderBase)) { provider.enabled = false; disabledLocomotion.Add(provider); }
            MoveTo(headPoint, yaw);
            Vector3 correction = Vector3.up * (headPoint.y - Origin.Camera.transform.position.y);
            SetOriginPose(Origin.transform.position + correction, Origin.transform.rotation);
            Vector3 seatControlPoint = Origin.Camera.transform.position + Origin.Camera.transform.forward * 0.8f + Vector3.down * 0.18f;
            seatedControls = WorldPanel.Create(transform, "Virtual seat controls", seatControlPoint, FacingVisitor(seatControlPoint), 550);
            seatedControls.Status.text = "You are virtually seated.\nB/Y or this button returns to the saved standing point.";
            seatedControls.Button("Leave virtual seat", LeaveSeat);
            Seated = true; yield return Fade(0); Busy = false;
        }
        public void LeaveSeat()
        {
            if (!Seated || Busy) return;
            StartCoroutine(LeaveSeatTransition());
        }
        private IEnumerator LeaveSeatTransition()
        {
            Busy = true; yield return Fade(1);
            RestoreSeatPose(); yield return Fade(0); Busy = false;
        }
        private void RestoreSeatPose()
        {
            if (seatedControls != null) Destroy(seatedControls.gameObject);
            foreach (var provider in disabledLocomotion) if (provider != null) provider.enabled = true;
            disabledLocomotion.Clear(); SetOriginPose(standingPosition, standingRotation); Seated = false;
        }
        private void ReleaseSelections()
        {
            foreach (var interactable in FindObjectsOfType<XRBaseInteractable>())
                foreach (var interactor in new List<IXRSelectInteractor>(interactable.interactorsSelecting))
                    interactable.interactionManager.SelectExit(interactor, interactable);
        }
        public void CancelCurrentInteraction() { ReleaseSelections(); CloseInspection(); }
        private void Update()
        {
            visitTime += Time.deltaTime;
            if (visitTime > 300 && !timeNotified && entry != null)
            { timeNotified = true; entry.Status.text = "Five-minute tour complete. Continue freely or open the summary."; }
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
                RequestReturn();
            if (backAction != null && backAction.WasPressedThisFrame())
            {
                if (Seated) LeaveSeat();
                else if (InCabin)
                {
                    var cabin = FindObjectOfType<CabinEnvironment>();
                    if (cabin != null && cabin.Observation.Current != null) cabin.Observation.Close();
                    else RequestReturn();
                }
                else if (!boatOnly) { exhibits.ship.GetComponent<ShipDetailInspection>()?.Restore(); welcome.gameObject.SetActive(false); }
            }
        }
        private bool timeNotified;
        public void CloseInspection() { if (replica != null) Destroy(replica.gameObject); replica = null; }
        private void OnDestroy() { backAction?.Dispose(); if (Instance == this) Instance = null; }
    }
}
