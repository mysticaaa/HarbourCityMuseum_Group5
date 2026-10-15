using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using ZxhMuseum;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class VensailCabinValidation
{
    private const string Key = "ZXH.VensailValidation";
    private static double deadline;
    private static int phase, loops;
    private static Dictionary<Behaviour, bool> rigStates;
    private static Vector3 position;
    private static Quaternion rotation;
    private static Vector3 originalFloorOffset;
    private static float originalCameraHeight, confirmationTime;
    [InitializeOnLoadMethod]
    private static void Resume()
    {
        if (!SessionState.GetBool(Key, false)) return;
        deadline = EditorApplication.timeSinceStartup + 180;
        EditorApplication.update += Tick;
    }
    public static void Run()
    {
        VensailCabinIntegration.Build();
        EditorSceneManager.OpenScene(VensailCabinIntegration.Destination);
        SessionState.SetBool(Key, true);
        EditorApplication.isPlaying = true;
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new Exception(message); }
    private static void Tick()
    {
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Vensail cabin test timeout");
            if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
            // Hardware-free test only; retain the actual project and scene input settings.
            foreach (var legacy in UnityEngine.Object.FindObjectsOfType<Valve.VR.SteamVR_Render>()) legacy.enabled = false;
            Time.timeScale = 1;
            var session = MuseumSession.Instance;
            if (session == null || session.Busy) return;
            if (phase == 0)
            {
                Require(UnityEngine.Object.FindObjectsOfType<VensailCabinEntry>().Length == 1, "Expected single ship entry");
                Require(UnityEngine.Object.FindObjectsOfType<QuestExhibitSetup>().Length == 0, "Legacy museum setup unexpectedly active");
                rigStates = session.Origin.GetComponentsInChildren<Behaviour>(true).ToDictionary(b => b, b => b.enabled);
                position = session.Origin.transform.position; rotation = session.Origin.transform.rotation;
                originalFloorOffset = session.Origin.CameraFloorOffsetObject.transform.localPosition;
                originalCameraHeight = session.Origin.CameraYOffset;
                Require(!UnityEngine.Object.FindObjectsOfType<WorldPanel>().Any(), "Extra museum panels unexpectedly created");
                Require(UnityEngine.Object.FindObjectsOfType<Triggering>(true).All(t => !t.enabled), "Prototype empty-scene route is still enabled");
                var entry = UnityEngine.Object.FindObjectOfType<VensailCabinEntry>();
                entry.RequestConfirmation();
                Require(entry.ConfirmationOpen && !session.InCabin && !session.Busy, "Click must show confirmation without loading");
                entry.ConfirmEntry();
                Require(!session.Busy, "Opening trigger also confirmed entry");
                entry.CancelEntry();
                Require(!entry.ConfirmationOpen && !session.Busy, "Cancel unexpectedly entered ship");
                entry.RequestConfirmation(); confirmationTime = Time.unscaledTime; phase = 4;
            }
            else if (phase == 4 && Time.unscaledTime - confirmationTime > .3f)
            {
                var entry = UnityEngine.Object.FindObjectOfType<VensailCabinEntry>();
                entry.ConfirmEntry();
                Require(!entry.ConfirmationOpen && session.Busy, "Confirm did not start ship entry");
                phase = 1;
            }
            else if (phase == 1 && session.InCabin)
            {
                var deck = UnityEngine.Object.FindObjectOfType<ShipDeckEnvironment>();
                Require(deck != null && deck.UsesExhibitionGeometry && deck.HasOriginalHatch && deck.HasOriginalWinch, "Original ship geometry/gear missing");
                foreach (var mesh in deck.GetComponentsInChildren<MeshFilter>())
                    if (mesh.name.StartsWith("Exhibition geometry ") && mesh.name.EndsWith("_0"))
                        Require(mesh.GetComponent<XRBaseInteractable>() == null, "Fixed ship structure still opens an inspection replica");
                var interior = UnityEngine.Object.FindObjectOfType<CabinEnvironment>();
                Require(interior != null && interior.Wheel != null && interior.Needle != null && interior.Navigation != null &&
                    interior.GetComponentsInChildren<ReturnableTool>(true).Length >= 2, "Dedicated cabin controls or grabbable tools missing");
                Require(session.Origin.CameraFloorOffsetObject.transform.localPosition.y > originalFloorOffset.y + .14f, "Boat view was not raised");
                var activeRay = session.Origin.GetComponentsInChildren<XRBaseInteractor>()
                    .FirstOrDefault(r => r.isActiveAndEnabled && r.interactionManager != null);
                Require(activeRay != null, "Teammate rig has no active interactor manager");
                foreach (var item in UnityEngine.Object.FindObjectOfType<CabinEnvironment>().GetComponentsInChildren<XRBaseInteractable>())
                    Require(item.interactionManager == activeRay.interactionManager, "Cabin interaction uses a different manager: " + item.name);
                foreach (var item in rigStates) Require(item.Key.enabled == item.Value, "Rig binding/provider state changed aboard: " + item.Key);
                if (loops == 0)
                {
                    phase = 3;
                    session.gameObject.AddComponent<VensailButtonTestRunner>().Check = () =>
                    {
                        try { VerifyButtons(session, deck); session.RequestReturn(); phase = 2; }
                        catch (Exception error) { Debug.LogException(error); Finish(1, "ZXH VENSAIL CABIN FAIL"); }
                    };
                    return;
                }
                session.RequestReturn(); phase = 2;
            }
            else if (phase == 2 && !session.InCabin)
            {
                Require(!SceneManager.GetSceneByPath(MuseumSession.InteriorPath).isLoaded, "Interior still loaded");
                Require(Vector3.Distance(position, session.Origin.transform.position) < .01f && Quaternion.Angle(rotation, session.Origin.transform.rotation) < .1f, "Return pose changed");
                foreach (var item in rigStates) Require(item.Key.enabled == item.Value, "Rig state changed after return");
                Require(UnityEngine.Object.FindObjectsOfType<CabinInputScope>().Length == 0, "Cabin input scope leaked");
                Require(Mathf.Abs(session.Origin.CameraYOffset - originalCameraHeight) < .001f &&
                    Vector3.Distance(session.Origin.CameraFloorOffsetObject.transform.localPosition, originalFloorOffset) < .001f, "Museum camera height was not restored");
                if (++loops < 3) { session.RequestEnter(); phase = 1; }
                else Finish(0, "ZXH VENSAIL CABIN PASS: confirmation/cancel/debounce, prototype routes disabled, elevated boat view and restored museum height; original controls and three return trips verified.");
            }
        }
        catch (Exception error) { Debug.LogException(error); Finish(1, "ZXH VENSAIL CABIN FAIL"); }
    }
    private static void Finish(int code, string message)
    {
        Debug.Log(message); SessionState.SetBool(Key, false); EditorApplication.update -= Tick;
        EditorApplication.Exit(code);
    }
    private static BoatTriggerAction Button(string panel, string caption)
    {
        var world = UnityEngine.Object.FindObjectsOfType<WorldPanel>().Single(p => p.name == "ZXH_UI_" + panel);
        return world.GetComponentsInChildren<BoatTriggerAction>().Single(b => b.name == "Button_" + caption);
    }
    private static void VerifyButtons(MuseumSession session, ShipDeckEnvironment deck)
    {
        var button = Button("Rigging", "Raise");
        // Headless mode disables the headset's modality-controlled rays. Use a temporary
        // XRI controller to exercise the same hover + Activate path without changing the rig.
        var testHand = new GameObject("ZXH temporary input test hand");
        var controller = testHand.AddComponent<ActionBasedController>();
        var ray = testHand.AddComponent<XRRayInteractor>();
        ray.xrController = controller;
        ray.interactionManager = button.GetComponent<XRSimpleInteractable>().interactionManager;
        var saved = controller.activateAction;
        var oldMode = InputSystem.settings.updateMode;
        var oldFocus = InputSystem.settings.backgroundBehavior;
        var oldEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
        var device = InputSystem.AddDevice<Keyboard>();
        using (var action = new InputAction("Test trigger", InputActionType.Button, "<Keyboard>/space"))
        {
            try
            {
                InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                controller.activateAction = new InputActionProperty(action); action.Enable();
                var target = button.GetComponent<XRSimpleInteractable>();
                ray.interactionManager.HoverEnter(ray, target);
                InputState.Change(device, new KeyboardState());
                button.ProcessTriggerInput(); deck.AdvanceMotion(1);
                Require(Mathf.Abs(deck.PortBoomAngle) < .001f, "Hover without trigger changed rigging");
                InputState.Change(device, new KeyboardState(UnityEngine.InputSystem.Key.Space));
                Require(action.WasPressedThisFrame(), "Simulated Activate action was not pressed");
                Require(target.interactorsHovering.Contains(ray), "Temporary ray did not enter button hover");
                button.ProcessTriggerInput(); deck.AdvanceMotion(1);
                Require(deck.PortBoomAngle > 4.9f && deck.StarboardBoomAngle > 4.9f, "Original Activate binding did not raise rigging");
                ray.interactionManager.HoverExit(ray, target);
            }
            finally
            {
                controller.activateAction = saved; InputSystem.RemoveDevice(device); UnityEngine.Object.DestroyImmediate(testHand);
                InputSystem.settings.updateMode = oldMode;
                InputSystem.settings.backgroundBehavior = oldFocus;
                InputSystem.settings.editorInputBehaviorInPlayMode = oldEditorInput;
            }
        }
        Button("Rigging", "Stop").Press(); float stopped = deck.PortBoomAngle;
        deck.AdvanceMotion(10); Require(Mathf.Abs(deck.PortBoomAngle - stopped) < .001f, "Stop did not stop motion");
        Button("Rigging", "Lower").Press(); deck.AdvanceMotion(10);
        Require(Mathf.Abs(deck.PortBoomAngle + 10) < .001f && Mathf.Abs(deck.StarboardBoomAngle + 10) < .001f, "Lower exceeded/missed limit");
        Button("Deck hatch", "Raise hatch").Press(); deck.AdvanceMotion(2);
        var hatch = deck.transform.Find("Original working deck hatch hinge");
        Require(deck.HatchOpen && Quaternion.Angle(hatch.localRotation, Quaternion.Euler(-65, 0, 0)) < .1f, "Raise hatch button failed");
        Button("Deck hatch", "Lower hatch").Press(); deck.AdvanceMotion(2);
        Require(!deck.HatchOpen && Quaternion.Angle(hatch.localRotation, Quaternion.identity) < .1f, "Lower hatch button failed");
        var collider = button.GetComponent<BoxCollider>(); collider.enabled = true; Physics.SyncTransforms();
        Vector3 point = button.transform.TransformPoint(collider.center);
        Require(Physics.Raycast(point - button.transform.forward * .02f, button.transform.forward, out var hit, .04f) && hit.collider == collider, "Button collider cannot be hit");
        Debug.Log("ZXH BUTTON PASS: simulated XRI hover + Activate; hover alone inert; rigging raise/stop/lower; physical hatch raise/lower and collider hit.");
    }
}

public sealed class VensailButtonTestRunner : MonoBehaviour
{
    public Action Check;
    private void Update() { enabled = false; Check?.Invoke(); Destroy(this); }
}
