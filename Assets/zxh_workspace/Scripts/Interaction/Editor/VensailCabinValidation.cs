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
    private static Vector3 beforeSeat;
    private static Quaternion beforeSeatRotation;
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
                        try
                        {
                            VerifyButtons(session, deck);
                            beforeSeat=session.Origin.transform.position;beforeSeatRotation=session.Origin.transform.rotation;
                            interior.Sofa.GetComponent<BoatTriggerAction>().Press(); phase=5;
                        }
                        catch (Exception error) { Debug.LogException(error); Finish(1, "ZXH VENSAIL CABIN FAIL"); }
                    };
                    return;
                }
                session.RequestReturn(); phase = 2;
            }
            else if(phase==5 && session.Seated)
            {
                var cabin=UnityEngine.Object.FindObjectOfType<CabinEnvironment>();
                Require(Mathf.Abs(cabin.transform.InverseTransformPoint(session.Origin.Camera.transform.position).y-1.2f)<.02f,"Clicking sofa did not seat at correct height");
                cabin.LeaveSeatButton.Actuate();phase=6;
            }
            else if(phase==6 && !session.Seated)
            {
                Require(Vector3.Distance(beforeSeat,session.Origin.transform.position)<.01f && Quaternion.Angle(beforeSeatRotation,session.Origin.transform.rotation)<.1f,"Physical leave button did not restore standing pose");
                Debug.Log("ZXH PHYSICAL SEAT PASS: sofa trigger seats directly, physical leave button restores standing pose.");
                session.RequestReturn();phase=2;
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
        Require(!UnityEngine.Object.FindObjectsOfType<WorldPanel>().Any(p=>p.name=="ZXH_UI_Rigging" || p.name=="ZXH_UI_Deck hatch" || p.name=="ZXH_UI_Wheelhouse entrance"),"Old floating device menus remain");
        var testHand=new GameObject("ZXH temporary physical control test hand");
        var controller=testHand.AddComponent<ActionBasedController>();
        var ray=testHand.AddComponent<XRRayInteractor>();ray.xrController=controller;
        var handle=deck.Lever.Handle.GetComponent<XRSimpleInteractable>();ray.interactionManager=handle.interactionManager;
        var device=InputSystem.AddDevice<Keyboard>();
        var oldFocus=InputSystem.settings.backgroundBehavior;
        var oldEditor=InputSystem.settings.editorInputBehaviorInPlayMode;
        using(var action=new InputAction("Physical control test trigger",InputActionType.Button,"<Keyboard>/space"))
        {
            try
            {
                InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
                InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                InputSystem.EnableDevice(device);
                controller.activateAction=new InputActionProperty(action);action.Enable();
                testHand.transform.position=deck.Lever.transform.TransformPoint(new Vector3(0,.65f,-.45f));
                testHand.transform.LookAt(deck.Lever.Handle.position);
                Vector3 neutral=testHand.transform.position;
                Vector3 neutralHandle=deck.Lever.Handle.position;
                ray.interactionManager.HoverEnter(ray,handle);
                InputState.Change(device,new KeyboardState());
                deck.Lever.Handle.GetComponent<BoatTriggerAction>().ProcessTriggerInput();
                Require(deck.Lever.Direction==0,"Hover alone moved the lever");
                InputState.Change(device,new KeyboardState(UnityEngine.InputSystem.Key.Space));
                Require(action.WasPressedThisFrame(),"Synthetic Activate did not press");
                deck.Lever.Handle.GetComponent<BoatTriggerAction>().ProcessTriggerInput();
                testHand.transform.position=neutral+deck.Lever.transform.right*.2f;
                deck.Lever.ProcessHeldInput();Require(deck.Lever.Direction==0,"Lateral movement must not reverse the forward/back lever");
                testHand.transform.position=neutral+deck.Lever.transform.forward*.2f;
                deck.Lever.ProcessHeldInput();deck.AdvanceMotion(1);
                Require(deck.Lever.Direction==1 && deck.PortBoomAngle>4.9f,"Pushing held lever forward did not raise rigging");
                Require(Vector3.Dot(deck.Lever.Handle.position-neutralHandle,deck.Lever.transform.forward)>.1f,"Visible handle moved opposite forward input");
                testHand.transform.position=neutral;deck.Lever.ProcessHeldInput();float stopped=deck.PortBoomAngle;
                deck.AdvanceMotion(2);Require(deck.Lever.Direction==0 && Mathf.Abs(deck.PortBoomAngle-stopped)<.001f,"Lever centre did not stop");
                testHand.transform.position=neutral-deck.Lever.transform.forward*.2f;
                deck.Lever.ProcessHeldInput();deck.AdvanceMotion(10);
                Require(deck.Lever.Direction==-1 && Mathf.Abs(deck.PortBoomAngle+10)<.001f,"Lever back did not lower to limit");
                Require(Vector3.Dot(deck.Lever.Handle.position-neutralHandle,deck.Lever.transform.forward)<-.1f,"Visible handle moved opposite backward input");
                InputState.Change(device,new KeyboardState());deck.Lever.ProcessHeldInput();
                Require(deck.Lever.Direction==0,"Releasing trigger did not return lever to stop");
                ray.interactionManager.HoverExit(ray,handle);
            }
            finally
            {
                InputSystem.RemoveDevice(device);UnityEngine.Object.DestroyImmediate(testHand);deck.Lever.Stop();
                InputSystem.settings.backgroundBehavior=oldFocus;InputSystem.settings.editorInputBehaviorInPlayMode=oldEditor;
            }
        }
        deck.HatchButton.Actuate();deck.AdvanceMotion(2);
        Require(deck.HatchOpen,"Physical hatch button did not open hatch");
        deck.HatchButton.Actuate();deck.AdvanceMotion(2);
        Require(!deck.HatchOpen,"Second hatch press did not close hatch");
        deck.OuterDoorButton.Actuate();deck.AdvanceMotion(2);Require(deck.CabinDoorOpen,"Outside physical door button failed");
        deck.InnerDoorButton.Actuate();deck.AdvanceMotion(2);Require(!deck.CabinDoorOpen,"Inside physical door button failed");
        var cabin=UnityEngine.Object.FindObjectOfType<CabinEnvironment>();
        Require(GameObject.Find("Port story picture")==null,"Placeholder interpretation picture remains");
        cabin.SideWindow.Button.Actuate();cabin.SideWindow.Advance(2);
        Require(cabin.SideWindow.IsOpen && Quaternion.Angle(cabin.SideWindow.Hinge.localRotation,Quaternion.Euler(0,90,0))<.1f,"Right glass window did not open");
        cabin.SideWindow.Button.Actuate();cabin.SideWindow.Advance(2);
        Require(!cabin.SideWindow.IsOpen && Quaternion.Angle(cabin.SideWindow.Hinge.localRotation,Quaternion.identity)<.1f,"Right glass window did not close");
        Require(cabin.SideWindow.Glass.GetComponent<Renderer>().sharedMaterial.color.a<.3f,"Right window glass is opaque");
        cabin.SeaWindow.Toggle();cabin.SeaWindow.Advance(2);
        Require(cabin.SeaWindow.IsOpen && Quaternion.Angle(cabin.SeaWindow.Hinge.localRotation,Quaternion.Euler(90,0,0))<.1f,"Sea window did not open");
        cabin.SeaWindow.Toggle();cabin.SeaWindow.Advance(2);
        Require(!cabin.SeaWindow.IsOpen && Quaternion.Angle(cabin.SeaWindow.Hinge.localRotation,Quaternion.identity)<.1f,"Sea window did not close");
        if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-zxh-render-controls")>=0)PhysicalControlPreview.Capture(cabin,deck);
        Debug.Log("ZXH PHYSICAL CONTROLS PASS: real XRI trigger/lever movement, centre/release stop, lower limit, toggle hatch/door buttons and opening sea window.");
    }
}

public sealed class VensailButtonTestRunner : MonoBehaviour
{
    public Action Check;
    private void Update() { enabled = false; Check?.Invoke(); Destroy(this); }
}
