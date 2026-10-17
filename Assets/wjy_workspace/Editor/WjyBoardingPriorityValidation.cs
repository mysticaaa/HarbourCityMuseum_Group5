using System;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Unity.XR.CoreUtils;
using Wjy.Museum;
using ZxhMuseum;

public static class WjyBoardingPriorityValidation
{
    static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Call(object obj, string method) => obj.GetType().GetMethod(method, Private).Invoke(obj, null);
    static void Check(bool value, string message, StringBuilder report)
    { if (!value) throw new Exception(message); report.AppendLine("PASS: " + message); }

    // Never starts the user's scenes, shared startup scripts or connected XR hardware.
    public static void Run()
    {
        if (!Application.isBatchMode || !Application.dataPath.Replace('\\','/').Contains("/.work/BoardingPriorityValidation/"))
            throw new InvalidOperationException("Use an isolated BoardingPriorityValidation project only.");
        var report = new StringBuilder();
        int exit = 0;
        try
        {
            var boat = new GameObject("Test boat");
            var entry = boat.AddComponent<VensailCabinEntry>();
            var adapter = boat.AddComponent<WjyBoardingPanelPriority>(); Call(adapter, "Awake");
            var session = new GameObject("Test session").AddComponent<MuseumSession>();
            typeof(MuseumSession).GetProperty("Instance").SetValue(null, session);
            var rig = new GameObject("Existing rig").AddComponent<XROrigin>();
            typeof(MuseumSession).GetProperty("Origin").SetValue(session, rig);
            var camera = new GameObject("Existing camera").AddComponent<Camera>(); camera.tag = "MainCamera";
            camera.transform.SetParent(rig.transform); rig.Camera = camera;
            var left = new GameObject("Left ray").AddComponent<XRRayInteractor>(); left.transform.SetParent(rig.transform); left.raycastMask = ~0;
            var right = new GameObject("Right ray").AddComponent<XRRayInteractor>(); right.transform.SetParent(rig.transform); right.raycastMask = ~(1 << 8);
            int leftMask=left.raycastMask, rightMask=right.raycastMask;
            var panel = WorldPanel.Create(session.transform, "Board the ship?", new Vector3(0, 1.5f, 3), Quaternion.identity, 650);
            int presses = 0;
            var button = panel.Button("Enter ship", () => presses++);
            panel.Button("Cancel", () => {});
            typeof(VensailCabinEntry).GetField("confirmation", Private).SetValue(entry, panel);
            var buttonCollider = button.GetComponent<BoxCollider>();
            var start = buttonCollider.transform.TransformPoint(buttonCollider.center) - Vector3.forward * 2;
            var obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube); obstacle.name="Boat occlusion";
            obstacle.transform.position = start + Vector3.forward * .5f; obstacle.transform.localScale = Vector3.one * .2f;
            Physics.SyncTransforms();
            Check(Physics.Raycast(start, Vector3.forward, out var before, 5, left.raycastMask) && before.collider.gameObject==obstacle, "Boat blocks button before menu priority", report);
            Call(adapter, "LateUpdate"); Physics.SyncTransforms();
            Check(Physics.Raycast(start, Vector3.forward, out var after, 5, left.raycastMask) && after.collider==buttonCollider, "Menu ray hits button through boat geometry", report);
            Check((int)right.raycastMask == 1 << 5, "Both shared rays prioritize UI while menu is open", report);
            Check(obstacle.GetComponent<Collider>().enabled, "Boat physical collider stays enabled", report);
            Check(!panel.GetComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>().enabled, "Existing physical-button input path unchanged", report);
            button.GetComponent<BoatTriggerAction>().Press(); Check(presses==1, "Original button callback still executes once", report);
            typeof(VensailCabinEntry).GetField("confirmation", Private).SetValue(entry, null);
            Call(adapter, "LateUpdate");
            Check((int)left.raycastMask==leftMask && (int)right.raycastMask==rightMask, "Closing menu restores each ray's exact original mask", report);
            Check(button.gameObject.layer==0, "Closing menu restores original button layer", report);
            typeof(VensailCabinEntry).GetField("confirmation", Private).SetValue(entry, panel); Call(adapter, "LateUpdate");
            Call(adapter, "OnDisable");
            Check((int)left.raycastMask==leftMask && (int)right.raycastMask==rightMask, "Disabling adapter also restores masks", report);
            report.AppendLine("ALL CHECKS PASSED. Physics and callbacks checked in isolated editor; physical Quest test still required.");
        }
        catch(Exception error) { exit=1; report.AppendLine("FAIL: "+error); Debug.LogException(error); }
        File.WriteAllText("../BoardingPanelPriorityValidation.txt", report.ToString());
        Debug.Log(report.ToString()); EditorApplication.Exit(exit);
    }
}
