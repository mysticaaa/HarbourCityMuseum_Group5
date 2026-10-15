using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ZxhMuseum;

public static class PlayableCabinBuilder
{
    [MenuItem("ZXH/Build playable cabin")]
    public static void Build()
    {
        const string folder = "Assets/zxh_workspace/Scenes";
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/zxh_workspace", "Scenes");
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MuseumSession.InteriorPath) == null)
        {
            var previous = EditorSceneManager.GetSceneManagerSetup();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("ZXH_ShipInterior").AddComponent<CabinEnvironment>();
            EditorSceneManager.SaveScene(scene, MuseumSession.InteriorPath);
            if (previous.Length > 0 && previous.All(item => !string.IsNullOrEmpty(item.path)))
                EditorSceneManager.RestoreSceneManagerSetup(previous);
            else EditorSceneManager.OpenScene("Assets/Scenes/Museum.unity");
        }
        var scenes = EditorBuildSettings.scenes.ToList();
        if (!scenes.Any(scene => scene.path == MuseumSession.InteriorPath))
            scenes.Add(new EditorBuildSettingsScene(MuseumSession.InteriorPath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        AssetDatabase.SaveAssets();
        Debug.Log("[ZXH] Playable cabin registered. Museum runtime creates entry and tour panels.");
    }
}
