using System.Linq;
using CsyMuseum;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public static class CSYExhibitGuideSetup
{
    private const string ClipPath = "Assets/csy_workspace/Audio/Phase3Helmet.wav";

    [MenuItem("CSY Tools/Phase 3/Add guide to selected helmet")]
    private static void InstallSelected()
    {
        if (Selection.activeGameObject == null || EditorUtility.IsPersistent(Selection.activeGameObject))
            throw new System.InvalidOperationException("Select the helmet in the scene Hierarchy.");
        var grab = Selection.activeGameObject.GetComponentInChildren<XRGrabInteractable>();
        if (!grab) grab = Selection.activeGameObject.GetComponentInParent<XRGrabInteractable>();
        if (!grab || !grab.GetComponent<HelmetHandInspection>())
            throw new System.InvalidOperationException("Select the helmet object with HelmetHandInspection and XRGrabInteractable.");
        Install(grab);
    }

    public static void BuildDemo()
    {
        var scene = EditorSceneManager.OpenScene("Assets/zxh_workspace/Scenes/Museum_Vensail_Cabin.unity");
        var helmets = Object.FindObjectsOfType<HelmetHandInspection>();
        if (helmets.Length != 1) throw new System.InvalidOperationException("Expected exactly one active helmet inspection object.");
        var guide = Install(helmets[0].GetComponent<XRGrabInteractable>());
        if (!guide.narration || !guide.inspectionObject || !guide.rangeAnchor)
            throw new System.InvalidOperationException("Phase 3 guide is missing a required reference.");
        if (!EditorSceneManager.SaveScene(scene, "Assets/csy_workspace/Scenes/Museum_CSY_Phase3_E_Demo.unity", true))
            throw new System.InvalidOperationException("Unable to save the Phase 3 demo copy.");
        Debug.Log("[CSY E] Demo saved: helmet, audio and fixed range anchor wired.");
    }

    private static CSYExhibitGuide Install(XRGrabInteractable grab)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(ClipPath);
        if (!clip) throw new System.InvalidOperationException("Missing narration clip at " + ClipPath);
        var existing = Object.FindObjectsOfType<CSYExhibitGuide>().FirstOrDefault(x => x.inspectionObject == grab);
        if (existing) return existing;
        var bounds = ExhibitInspectionHint.GetBounds(grab.transform);
        var eye = Camera.main;
        Vector3 towardsVisitor = eye ? eye.transform.position - bounds.center : -grab.transform.forward;
        towardsVisitor.y = 0f;
        if (towardsVisitor.sqrMagnitude < .01f) towardsVisitor = Vector3.back;
        towardsVisitor.Normalize();

        var marker = new GameObject("CSY Helmet Guide");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(marker, grab.gameObject.scene);
        Undo.RegisterCreatedObjectUndo(marker, "Add exhibit guide");
        var anchor = new GameObject("Fixed helmet range").transform;
        anchor.SetParent(marker.transform, false);
        marker.transform.SetPositionAndRotation(bounds.center + towardsVisitor * (bounds.extents.magnitude + .16f)
            + Vector3.up * Mathf.Clamp(bounds.extents.y, .19f, .35f), Quaternion.LookRotation(-towardsVisitor));
        anchor.position = bounds.center;
        var guide = Undo.AddComponent<CSYExhibitGuide>(marker);
        guide.narration = clip;
        guide.inspectionObject = grab;
        guide.rangeAnchor = anchor;
        EditorSceneManager.MarkSceneDirty(marker.scene);
        Selection.activeGameObject = marker;
        return guide;
    }
}
