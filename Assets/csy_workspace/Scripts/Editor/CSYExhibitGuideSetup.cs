using CsyMuseum;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CSYExhibitGuideSetup
{
    public const string SourceScene = "Assets/261122/Museum_261122_1.unity";
    public const string DemoScene = "Assets/csy_workspace/Scenes/Phase3E_XR/Museum_261122_1.unity";

    [MenuItem("CSY Tools/Phase 3/Open integrated E scene")]
    public static void OpenDemo()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(DemoScene);
    }
    [MenuItem("CSY Tools/Phase 3/Install E guides in current scene")]
    public static void InstallCurrent()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new System.InvalidOperationException("Install guides before entering Play mode.");
        var scene = EditorSceneManager.GetActiveScene();
        foreach (var root in scene.GetRootGameObjects())
            if (root.GetComponent<CSYGalleryGuides>()) { Selection.activeGameObject = root; return; }
        var go = new GameObject("CSY Phase 3 Exhibit Guidance");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
        Undo.RegisterCreatedObjectUndo(go, "Install E guides");
        var adapter = Undo.AddComponent<CSYGalleryGuides>(go);
        adapter.helmetAudio = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/csy_workspace/Audio/Phase3Helmet.wav");
        adapter.boatAudio = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/csy_workspace/Audio/Phase3Boat.wav");
        adapter.storyAudio = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/csy_workspace/Audio/Phase3FirstDay.wav");
        EditorSceneManager.MarkSceneDirty(scene); Selection.activeGameObject = go;
    }
    public static void BuildDemo()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.OpenScene(SourceScene); InstallCurrent();
        if (!EditorSceneManager.SaveScene(scene, DemoScene, true)) throw new System.InvalidOperationException("Failed to save E scene.");
    }
}
