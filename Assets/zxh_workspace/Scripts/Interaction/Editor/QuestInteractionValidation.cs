using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Management;

// Batch entry: -executeMethod QuestInteractionValidation.Run (without -quit).
public static class QuestInteractionValidation
{
    private const string Key = "ZXH.Validation";
    private static int frame;
    private static float started;
    private static XRGrabInteractable helmet;
    private static Transform ship;
    private static XRRayInteractor ray;
    private static XRInteractionManager manager;
    private static Vector3 homePosition, homeScale;
    private static Quaternion homeRotation;
    private static Transform homeParent;
    private static bool shipTesting;
    private static Vector3 shipPosition, shipScale, expectedFocus, expectedDisplayCenter;
    private static Quaternion shipRotation;
    private static int grabRound;
    private static bool timeoutTesting;
    private static RaycastHit shipHit;

    [InitializeOnLoadMethod]
    private static void Resume()
    {
        if (SessionState.GetBool(Key, false)) EditorApplication.update += Tick;
    }

    public static void Run()
    {
        try
        {
            QuestExhibitProjectSetup.Configure();
            // The simulated test has no HMD. Do not initialize the PC XR runtime.
            var settings = UnityEditor.XR.Management.XRGeneralSettingsPerBuildTarget
                .XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);
            settings.InitManagerOnStart = false;
            EditorSceneManager.OpenScene("Assets/Scenes/Museum.unity");
            foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
                foreach (var child in root.GetComponentsInChildren<Transform>(true))
                    if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) > 0)
                        Debug.LogWarning("ZXH MISSING SCRIPT: " + child.name);
            SessionState.SetBool(Key, true);
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            EditorApplication.isPlaying = true;
        }
        catch (Exception e) { Fail(e); }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private static void Tick()
    {
        if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        try
        {
            frame++;
            if (frame < 30) return;
            if (helmet == null)
            {
                var setup = UnityEngine.Object.FindObjectOfType<QuestExhibitSetup>();
                Require(setup != null && setup.helmet != null && setup.ship != null, "Missing scene references");
                helmet = setup.helmet.GetComponent<XRGrabInteractable>();
                ship = setup.ship;
                Require(helmet.GetComponent<HelmetHandInspection>() != null, "Helmet behavior missing");
                Require(ship.GetComponent<ShipDetailInspection>() != null, "Ship behavior missing");
                Require(ship.GetComponent<XRSimpleInteractable>().colliders.Count > 0, "Ship colliders missing");
                Require(!helmet.throwOnDetach && helmet.GetComponent<Rigidbody>().isKinematic,
                    "Helmet not configured for safe inspection");
                var bounds = ExhibitInspectionHint.GetBounds(ship);
                Require(bounds.size.y < Mathf.Max(bounds.size.x, bounds.size.z), "Ship appears vertically oriented");
                ray = UnityEngine.Object.FindObjectsOfType<XRRayInteractor>().First();
                manager = helmet.interactionManager;
                foreach (var controller in UnityEngine.Object.FindObjectsOfType<ActionBasedController>())
                    controller.enabled = false;
                homeParent = helmet.transform.parent;
                homePosition = helmet.transform.localPosition;
                homeRotation = helmet.transform.localRotation;
                homeScale = helmet.transform.localScale;
                manager.SelectEnter((UnityEngine.XR.Interaction.Toolkit.IXRSelectInteractor)ray, helmet);
                Require(helmet.isSelected, "Synthetic ray grab did not select helmet");
                Require(homeParent.GetComponent<ExhibitAutoRotation>().Paused, "Rotation did not pause");
                helmet.transform.position += new Vector3(0.4f, 0.3f, 0.2f);
                helmet.transform.rotation = Quaternion.Euler(20, 30, 40);
                manager.SelectExit((UnityEngine.XR.Interaction.Toolkit.IXRSelectInteractor)ray, helmet);
                started = Time.realtimeSinceStartup;
                Debug.Log("ZXH TEST: scene wiring, upright ship, synthetic grab/release passed");
                return;
            }
            if (!shipTesting && grabRound == 0 && Time.realtimeSinceStartup - started > 0.15f)
            {
                manager.SelectEnter((UnityEngine.XR.Interaction.Toolkit.IXRSelectInteractor)ray, helmet);
                helmet.transform.position += Vector3.right * 0.3f;
                manager.SelectExit((UnityEngine.XR.Interaction.Toolkit.IXRSelectInteractor)ray, helmet);
                grabRound = 1;
                started = Time.realtimeSinceStartup;
            }
            if (Time.realtimeSinceStartup - started < (timeoutTesting ? 13f : 1.2f)) return;
            if (shipTesting)
            {
                var detail = ship.GetComponent<ShipDetailInspection>();
                if (timeoutTesting)
                {
                    Require(Vector3.Distance(ship.localPosition, shipPosition) < 0.0001f, "Ship timeout did not restore position");
                    Require(Vector3.Distance(ship.localScale, shipScale) < 0.0001f, "Ship timeout did not restore scale");
                    Require(!ship.parent.GetComponent<ExhibitAutoRotation>().Paused, "Ship timeout did not resume rotation");
                    Debug.Log("ZXH TEST PASS: repeated ship inspection and 12 second automatic reset");
                    Finish(0);
                    return;
                }
                Require(Vector3.Distance(ship.localScale, shipScale * 2) < 0.0001f, "Ship zoom scale incorrect");
                Require(Vector3.Distance(ship.TransformPoint(expectedFocus),
                    expectedDisplayCenter) < 0.0001f, "Clicked ship detail did not move to display center");
                detail.Restore();
                Require(Vector3.Distance(ship.localPosition, shipPosition) < 0.0001f, "Ship reset position drift");
                Require(Vector3.Distance(ship.localScale, shipScale) < 0.0001f, "Ship reset scale drift");
                Require(Quaternion.Angle(ship.localRotation, shipRotation) < 0.01f, "Ship reset rotation drift");
                Require(!ship.parent.GetComponent<ExhibitAutoRotation>().Paused, "Ship rotation did not resume");
                Debug.Log("ZXH TEST PASS: ship surface collider hit, 2x zoom, reset pose and resumed rotation");
                Require(detail.InspectSurface(shipHit), "Repeated ship inspection failed");
                timeoutTesting = true;
                started = Time.realtimeSinceStartup;
                return;
            }
            Require(helmet.transform.parent == homeParent, "Helmet parent not restored");
            Require(Vector3.Distance(helmet.transform.localPosition, homePosition) < 0.0001f, "Helmet position drift");
            Require(Quaternion.Angle(helmet.transform.localRotation, homeRotation) < 0.01f, "Helmet rotation drift");
            Require(Vector3.Distance(helmet.transform.localScale, homeScale) < 0.0001f, "Helmet scale drift");
            Require(!homeParent.GetComponent<ExhibitAutoRotation>().Paused, "Rotation did not resume");
            Debug.Log("ZXH TEST PASS: helmet return pose, parent, scale and resumed rotation");
            Physics.SyncTransforms();
            RaycastHit surface = default;
            bool found = false;
            foreach (var collider in ship.GetComponentsInChildren<MeshCollider>())
            {
                float radius = collider.bounds.size.magnitude + 1;
                foreach (Vector3 axis in new[] { Vector3.up, Vector3.down, Vector3.forward, Vector3.back, Vector3.left, Vector3.right })
                    if (collider.Raycast(new Ray(collider.bounds.center + axis * radius, -axis), out surface, radius * 2))
                    { found = true; break; }
                if (found) break;
            }
            Require(found, "Cannot raycast ship surface");
            shipHit = surface;
            shipPosition = ship.localPosition; shipRotation = ship.localRotation; shipScale = ship.localScale;
            expectedFocus = ship.InverseTransformPoint(surface.point);
            expectedDisplayCenter = ExhibitInspectionHint.GetBounds(ship).center;
            Require(ship.GetComponent<ShipDetailInspection>().InspectSurface(surface), "Ship surface inspection failed");
            Require(ship.parent.GetComponent<ExhibitAutoRotation>().Paused, "Ship rotation did not pause");
            shipTesting = true;
            started = Time.realtimeSinceStartup;
        }
        catch (Exception e) { Fail(e); }
    }

    private static void Fail(Exception exception)
    {
        Debug.LogException(exception);
        Finish(1);
    }

    private static void Finish(int code)
    {
        SessionState.SetBool(Key, false);
        EditorApplication.update -= Tick;
        var settings = UnityEditor.XR.Management.XRGeneralSettingsPerBuildTarget
            .XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);
        if (settings != null)
        {
            settings.InitManagerOnStart = true;
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }
        if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
        if (Application.isBatchMode) EditorApplication.Exit(code);
    }
}
