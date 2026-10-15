using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEditor.XR.Management;
using ZxhMuseum;

public static class PlayableFlowValidation
{
    private const string Key = "ZXH.PlayableFlowValidation";
    private static int frame, phase, loop;
    private static bool boatLayoutVerified;
    private static UnityEngine.UI.Button occlusionButton;
    private static float timeout;
    private static MuseumSession session;
    private static CabinEnvironment cabin;
    private static Vector3 originalPosition;
    private static Quaternion originalRotation;
    private static ReturnableTool tool;
    private static Vector3 toolPosition, toolScale;
    private static Quaternion toolRotation;
    private static Transform toolHome;
    private static float waitUntil;
    private static XRRayInteractor toolRay;
    [InitializeOnLoadMethod]
    private static void Resume() { if (SessionState.GetBool(Key, false)) EditorApplication.update += Tick; }
    public static void Run()
    {
        try
        {
            PlayableCabinBuilder.Build();
            QuestExhibitProjectSetup.Configure();
            XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone).InitManagerOnStart = false;
            EditorSceneManager.OpenScene("Assets/Scenes/Museum.unity");
            SessionState.SetBool(Key, true);
            EditorApplication.update -= Tick; EditorApplication.update += Tick;
            EditorApplication.isPlaying = true;
        }
        catch (Exception e) { Debug.LogException(e); Finish(1); }
    }
    private static void Assert(bool value, string message) { if (!value) throw new Exception(message); }
    private static void Tick()
    {
        if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        try
        {
            if (++frame < 40) return;
            if (timeout == 0) timeout = Time.realtimeSinceStartup + 90;
            Assert(Time.realtimeSinceStartup < timeout, "Playable flow timeout");
            if (phase == 0)
            {
                session = MuseumSession.Instance;
                Assert(session != null, "Museum session missing");
                Assert(UnityEngine.Object.FindObjectsOfType<WorldPanel>().Length >= 4, "Museum exhibit panels missing");
                originalPosition = session.Origin.transform.position; originalRotation = session.Origin.transform.rotation;
                foreach (var controller in UnityEngine.Object.FindObjectsOfType<ActionBasedController>()) controller.enabled = false;
                session.RequestEnter(); phase = 1;
            }
            else if (phase == 1 && !session.Busy && session.InCabin)
            {
                var deckCheck = UnityEngine.Object.FindObjectOfType<ShipDeckEnvironment>();
                if (deckCheck == null) throw new Exception("Walkable deck was not generated");
                if (!boatLayoutVerified)
                {
                    Assert(deckCheck.UsesExhibitionGeometry, "Full-size boat does not use the exhibit geometry");
                    Assert(deckCheck.HasOriginalHatch && deckCheck.HasOriginalWinch, "Original hatch or winch geometry not found");
                    deckCheck.SetBoom(true,100); deckCheck.SetBoom(false,-100); deckCheck.ToggleHatch(); deckCheck.TurnWinch();
                    Assert(Vector3.Distance(Vector3.ProjectOnPlane(session.Origin.Camera.transform.position - deckCheck.SpawnPoint, Vector3.up), Vector3.zero) < .1f,
                        "Entry did not land on the original aft deck");
                    deckCheck.ToggleDoor(); waitUntil = Time.realtimeSinceStartup + 3.5f; phase = 11; return;
                }
                deckCheck.Travel(true);
                if (Vector3.ProjectOnPlane(session.Origin.Camera.transform.position - deckCheck.SpawnPoint, Vector3.up).magnitude > .1f)
                    throw new Exception("Deck transfer missed its destination");
                deckCheck.Travel(false);
                cabin = UnityEngine.Object.FindObjectOfType<CabinEnvironment>();
                Assert(cabin != null && cabin.Wheel != null && cabin.Needle != null, "Cabin devices missing");
                Assert(UnityEngine.Object.FindObjectsOfType<Unity.XR.CoreUtils.XROrigin>().Length == 1, "Multiple active XR origins");
                Assert(UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.isActiveAndEnabled,
                    "Cabin XR UI EventSystem disabled");
                Assert(cabin.InteractableCount >= 5, "Cabin interaction targets missing");
                var records = cabin.GetComponentsInChildren<InteractionRecord>(true);
                Assert(records.Length >= 10 && records.Select(record => record.assetId).Distinct().Count() == records.Length,
                    "Interaction asset IDs missing or duplicated");
                foreach (string key in new[] { "Welcome", "Cabin", "Wheel", "Helmet", "Work", "Migration", "Memories" })
                {
                    var clip = Resources.Load<AudioClip>("ZxhNarration/" + key);
                    Assert(clip != null && clip.length > 1, "Narration audio missing: " + key);
                }
                session.Narrator.Play("Welcome", "incorrect fallback");
                Assert(session.Narrator.GetComponentsInChildren<WorldPanel>().Last().Status.text.StartsWith("Welcome to Harbour City Museum"),
                    "Narration did not load matching transcript");
                session.Narrator.Stop();
                cabin.Engine.ToggleCover(); Assert(cabin.Engine.IsOpen, "Engine cover did not open");
                cabin.Engine.GetComponentsInChildren<UnityEngine.UI.Button>().First(button => button.name == "Button_Play / pause mechanism").onClick.Invoke();
                Assert(cabin.Engine.Running, "Engine teaching animation did not start");
                cabin.Engine.ResetDiagram(); Assert(!cabin.Engine.IsOpen && !cabin.Engine.Running, "Engine diagram reset failed");
                if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
                    CaptureCabin();
                var nav = cabin.Navigation;
                nav.ResetState(); nav.SetWheel(1000); Assert(nav.WheelAngle == 135, "Wheel upper limit incorrect");
                nav.SetWheel(-1000); Assert(nav.WheelAngle == -135, "Wheel lower limit incorrect");
                nav.SetHeadingForDemonstration(359); nav.BeginRoute(0);
                nav.Step(3.1f); Assert(nav.Completed, "Route wrap-around hold completion failed");
                nav.ResetState(); nav.SetWheel(90); nav.Step(1);
                Assert(nav.Heading == 0, "Stopped ship must not turn");
                Assert(nav.SetGear(1), "Ahead failed at rest"); nav.SetPower(1); nav.Step(1);
                Assert(nav.Speed > 0 && nav.Heading > 0, "Engine/wheel/heading linkage failed");
                Assert(!nav.SetGear(-1), "Unsafe direction switch accepted");
                nav.SetGear(0); nav.Step(10); Assert(Mathf.Abs(nav.Speed) < 0.01f, "Neutral did not slow to rest");
                nav.ResetState(); nav.SetHeadingForDemonstration(90);
                phase = 2;
            }
            else if (phase == 11 && Time.realtimeSinceStartup >= waitUntil)
            {
                var deckCheck = UnityEngine.Object.FindObjectOfType<ShipDeckEnvironment>();
                Assert(Mathf.Abs(deckCheck.PortBoomAngle-15)<.1f && Mathf.Abs(deckCheck.StarboardBoomAngle+10)<.1f,
                    "Original fishing outrigger motion/limits incorrect");
                Assert(deckCheck.HatchOpen && Mathf.Abs(deckCheck.WinchAngle-30)<.1f,"Original hatch/winch action failed");
                session.MoveTo(new Vector3(1.2f, .04f, -2), 0);
                Physics.SyncTransforms();
                var walk = session.GetComponent<BoatWalkController>();
                for (int i = 0; i < 360; i++) walk.Advance(Vector2.up, 1f / 60);
                Assert(session.Origin.transform.position.z > 1.9f && session.Origin.transform.position.y > 1.8f,
                    "Cannot physically walk from deck into wheelhouse: " + session.Origin.transform.position);
                Debug.Log("ZXH BOAT LAYOUT PASS: original vessel geometry, deck-first entry, open door and collision-aware walk up to cabin");
                cabin=UnityEngine.Object.FindObjectOfType<CabinEnvironment>();
                cabin.Navigation.SetWheel(90);waitUntil=Time.realtimeSinceStartup+.1f;phase=12;
            }
            else if (phase == 12 && Time.realtimeSinceStartup >= waitUntil)
            {
                Assert(Vector3.Dot(cabin.Wheel.wheelVisual.up,session.Origin.Camera.transform.right)>.99f,
                    "Right helm must move the top of the wheel toward the visitor's right");
                var north=cabin.Needle.Find("Compass card visual/North needle");
                Assert(Vector3.Dot((north.position-cabin.Needle.position).normalized,-cabin.transform.forward)>.99f,
                    "Compass north at heading 000 does not point toward the boat bow");
                var movable=UnityEngine.Object.FindObjectOfType<ShipDeckEnvironment>().GetComponentsInChildren<MeshFilter>()
                    .Where(filter=>filter.transform.parent.name.Contains("fishing outrigger pivot")).ToArray();
                Assert(movable.Length>=2 && movable.All(filter=>filter.sharedMesh.bounds.min.z>-4),"Fishing rods were included in movable outriggers");
                cabin.Navigation.SetWheel(0);
                session.MoveTo(new Vector3(1.2f,2.1f,.8f),0);
                occlusionButton=cabin.GetComponentsInChildren<UnityEngine.UI.Button>().First(button=>button.name=="Button_Ahead (when stopped)");
                UnityEngine.Object.FindObjectOfType<ShipDeckEnvironment>().ToggleDoor();
                waitUntil=Time.realtimeSinceStartup+1.6f;phase=13;
                Debug.Log("ZXH HELM DIRECTION PASS: right turn is visually clockwise; fishing rods stay outside boom meshes");
            }
            else if (phase == 13 && Time.realtimeSinceStartup >= waitUntil)
            {
                Physics.SyncTransforms();
                Assert(!CanClickButton(occlusionButton),"Mouse can select cabin control through closed door");
                UnityEngine.Object.FindObjectOfType<ShipDeckEnvironment>().ToggleDoor();
                waitUntil=Time.realtimeSinceStartup+1.6f;phase=14;
            }
            else if (phase == 14 && Time.realtimeSinceStartup >= waitUntil)
            {
                Physics.SyncTransforms();
                Assert(CanClickButton(occlusionButton),"Cabin control remains blocked after opening door");
                boatLayoutVerified=true;phase=1;
                Debug.Log("ZXH UI OCCLUSION PASS: physical closed door blocks mouse ray; opening it restores the same control hit");
            }
            else if (phase == 2)
            {
                Assert(Quaternion.Angle(cabin.Needle.localRotation, Quaternion.Euler(0, -90, 0)) < 0.1f, "Compass east reading incorrect");
                cabin.Navigation.SetHeadingForDemonstration(270); phase = 3;
            }
            else if (phase == 3)
            {
                Assert(Quaternion.Angle(cabin.Needle.localRotation, Quaternion.Euler(0, -270, 0)) < 0.1f, "Compass west reading incorrect");
                tool = cabin.GetComponentInChildren<ReturnableTool>();
                toolRay = UnityEngine.Object.FindObjectsOfType<XRRayInteractor>().First();
                Assert(tool != null && !tool.IsSelectableBy(toolRay), "Closed drawer must block tool selection");
                cabin.GetComponentsInChildren<UnityEngine.UI.Button>().First(button => button.name == "Button_Open / close drawer").onClick.Invoke();
                waitUntil = Time.realtimeSinceStartup + 0.7f; phase = 8;
            }
            else if (phase == 8 && Time.realtimeSinceStartup >= waitUntil)
            {
                Assert(tool.IsSelectableBy(toolRay), "Open drawer tool cannot be grabbed");
                toolHome = tool.transform.parent; toolPosition = tool.transform.localPosition;
                toolScale = tool.transform.localScale; toolRotation = tool.transform.localRotation;
                tool.interactionManager.SelectEnter((IXRSelectInteractor)toolRay, tool);
                Assert(tool.isSelected, "Tool selection failed");
                tool.transform.position += Vector3.up * 0.4f;
                tool.transform.rotation = Quaternion.Euler(25, 35, 45);
                tool.interactionManager.SelectExit((IXRSelectInteractor)toolRay, tool);
                Assert(tool.Returning, "Tool did not start returning");
                cabin.GetComponentsInChildren<UnityEngine.UI.Button>().First(button => button.name == "Button_Open / close drawer").onClick.Invoke();
                Assert(tool.CanTake(), "Drawer moved while tool was returning");
                waitUntil = Time.realtimeSinceStartup + 0.7f; phase = 9;
            }
            else if (phase == 9 && Time.realtimeSinceStartup >= waitUntil)
            {
                Assert(!tool.Returning && tool.transform.parent == toolHome, "Tool parent or return state incorrect");
                Assert(Vector3.Distance(tool.transform.localPosition, toolPosition) < 0.0001f, "Tool return position drift");
                Assert(Vector3.Distance(tool.transform.localScale, toolScale) < 0.0001f, "Tool return scale drift");
                Assert(Quaternion.Angle(tool.transform.localRotation, toolRotation) < 0.01f, "Tool return rotation drift");
                var second = cabin.GetComponentsInChildren<ReturnableTool>().Last();
                cabin.Observation.Open(tool.transform, "First", "First tool");
                cabin.Observation.Open(second.transform, "Second", "Second tool");
                Assert(cabin.Observation.Current == second.transform, "Observation did not switch objects");
                phase = 10;
            }
            else if (phase == 10)
            {
                Assert(UnityEngine.Object.FindObjectsOfType<InspectionReplica>().Length == 1, "Observation copy leaked");
                cabin.Observation.Close();
                cabin.ResetCabin();
                Assert(!tool.IsSelectableBy(toolRay), "Reset did not close the drawer");
                Debug.Log("ZXH TOOLS PASS: closed-drawer gate, grab, movement, safe return, pose reset and exclusive observation");
                session.Sit(cabin.transform.TransformPoint(new Vector3(1.1f, 1.2f, 0.7f)), cabin.transform.eulerAngles.y + 270); phase = 4;
            }
            else if (phase == 4 && !session.Busy)
            {
                Assert(session.Seated, "Virtual seating failed");
                Assert(Mathf.Abs(session.Origin.Camera.transform.position.y - cabin.transform.TransformPoint(new Vector3(1.1f, 1.2f, .7f)).y) < 0.01f, "Seated head height incorrect");
                session.LeaveSeat(); phase = 7;
            }
            else if (phase == 7 && !session.Busy)
            {
                Assert(!session.Seated, "Leaving seat failed");
                cabin.ResetCabin(); Assert(cabin.Navigation.WheelAngle == 0 && cabin.Navigation.Heading == 0, "Cabin reset failed");
                session.RequestReturn(); phase = 5;
            }
            else if (phase == 5 && !session.Busy && !session.InCabin)
            {
                Assert(!SceneManager.GetSceneByPath(MuseumSession.InteriorPath).isLoaded, "Interior scene leaked");
                Assert(Vector3.Distance(session.Origin.transform.position, originalPosition) < 0.01f, "Museum return position drift");
                Assert(Quaternion.Angle(session.Origin.transform.rotation, originalRotation) < 0.1f, "Museum return rotation drift");
                if (++loop < 10) { session.RequestEnter(); phase = 6; }
                else
                {
                    session.RestartTour(); Assert(!session.CabinVisited && !session.HelmetVisited, "Tour restart progress not cleared");
                    Debug.Log("ZXH PLAYABLE PASS: ten cabin round trips, active XR rig/UI, wheel limits, compass, navigation, task, seat and restart");
                    Debug.Log("ZXH CONTENT PASS: unique interaction IDs, seven narration clips/transcripts, engine cover/play/reset");
                    Finish(0);
                }
            }
            else if (phase == 6 && !session.Busy && session.InCabin) { session.RequestReturn(); phase = 5; }
        }
        catch (Exception e) { Debug.LogException(e); Finish(1); }
    }
    private static void Finish(int code)
    {
        SessionState.SetBool(Key, false); EditorApplication.update -= Tick;
        var settings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);
        if (settings != null) { settings.InitManagerOnStart = true; EditorUtility.SetDirty(settings); AssetDatabase.SaveAssets(); }
        if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
        if (Application.isBatchMode) EditorApplication.Exit(code);
    }
    private static bool CanClickButton(UnityEngine.UI.Button button)
    {
        Canvas.ForceUpdateCanvases();
        var rect=button.GetComponent<RectTransform>();
        var position=session.Origin.Camera.WorldToScreenPoint(rect.TransformPoint(rect.rect.center));
        var pointer=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){position=position};
        var hits=new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
        button.GetComponentInParent<UnityEngine.UI.GraphicRaycaster>().Raycast(pointer,hits);
        return hits.Any(hit=>hit.gameObject==button.gameObject);
    }
    private static void CaptureCabin()
    {
        var cameraObject = new GameObject("Validation camera");
        var camera = cameraObject.AddComponent<Camera>();
        camera.transform.position = cabin.transform.TransformPoint(new Vector3(0, 1.65f, 2.4f));
        camera.transform.rotation = cabin.transform.rotation * Quaternion.Euler(0, 180, 0);
        camera.fieldOfView = 75; camera.nearClipPlane = 0.05f;
        var texture = new RenderTexture(1400, 900, 24);
        camera.targetTexture = texture; camera.Render();
        var previous = RenderTexture.active; RenderTexture.active = texture;
        var image = new Texture2D(1400, 900, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, 1400, 900), 0, 0); image.Apply();
        System.IO.File.WriteAllBytes("Logs/ZXH-Cabin-Preview.png", image.EncodeToPNG());
        camera.transform.position = new Vector3(1.25f, 1.7f, -10);
        camera.transform.LookAt(new Vector3(0, 4, 0));
        camera.Render();
        image.ReadPixels(new Rect(0, 0, 1400, 900), 0, 0); image.Apply();
        System.IO.File.WriteAllBytes("Logs/ZXH-Deck-Preview.png", image.EncodeToPNG());
        camera.transform.position = new Vector3(22, 12, -22);
        camera.transform.LookAt(new Vector3(0, 4, 0));
        camera.Render(); image.ReadPixels(new Rect(0, 0, 1400, 900), 0, 0); image.Apply();
        System.IO.File.WriteAllBytes("Logs/ZXH-WalkableBoat-Preview.png", image.EncodeToPNG());
        RenderTexture.active = previous; camera.targetTexture = null;
        UnityEngine.Object.Destroy(image); UnityEngine.Object.Destroy(texture); UnityEngine.Object.Destroy(cameraObject);
    }
}
