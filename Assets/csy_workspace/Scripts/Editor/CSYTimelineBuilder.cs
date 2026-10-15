#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using CsyMuseum;

public static class CSYTimelineBuilder
{
    public const string Source = "Assets/zxh_workspace/Scenes/Museum_Vensail_Cabin.unity";
    public const string Integrated = "Assets/csy_workspace/Scenes/Museum_CSY_Timeline.unity";
    public const string Preview = "Assets/csy_workspace/Scenes/Timeline_CSY_Preview.unity";

    [MenuItem("CSY Tools/Timeline/Build scenes")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play first");
        if (string.IsNullOrEmpty(SceneManager.GetActiveScene().path)) throw new InvalidOperationException("Open a saved project scene before building timeline copies");
        if (SceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Save your open scene before building timeline copies");
        string source = File.ReadAllText(Source);
        string script = AssetDatabase.FindAssets("CSYTimelineStation t:MonoScript")
            .Select(AssetDatabase.GUIDToAssetPath).Single(path => path.EndsWith("/CSYTimelineStation.cs"));
        string guid = AssetDatabase.AssetPathToGUID(script);
        const string marker = "--- !u!1660057539 &9223372036854775807";
        if (!source.Contains(marker) || source.Contains("&2300000001")) throw new InvalidOperationException("Unexpected source scene IDs");
        string root = "--- !u!1 &2300000000\nGameObject:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  serializedVersion: 6\n  m_Component:\n  - component: {fileID: 2300000001}\n  - component: {fileID: 2300000002}\n  m_Layer: 0\n  m_Name: CSY_OptionalTimeline\n  m_TagString: Untagged\n  m_IsActive: 1\n" +
            "--- !u!4 &2300000001\nTransform:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: 2300000000}\n  serializedVersion: 2\n  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}\n  m_LocalPosition: {x: 0, y: 0, z: 0}\n  m_LocalScale: {x: 1, y: 1, z: 1}\n  m_Children: []\n  m_Father: {fileID: 0}\n" +
            "--- !u!114 &2300000002\nMonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: 2300000000}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n  m_Script: {fileID: 11500000, guid: " + guid + ", type: 3}\n  m_Name: \n  m_EditorClassIdentifier: \n  placeNearViewerAtStart: 1\n  stationOffset: {x: 1.1, y: 0, z: 2.5}\n";
        File.WriteAllText(Integrated, source.Replace(marker, root + marker) + "  - {fileID: 2300000001}\n");
        AssetDatabase.ImportAsset(Integrated);

        // Build a separate, hardware-free preview without changing the open scene.
        var preview = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        var cameraObject = new GameObject("CSY_PreviewCamera", typeof(Camera), typeof(AudioListener));
        SceneManager.MoveGameObjectToScene(cameraObject, preview);
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0, 1.6f, -3);
        cameraObject.GetComponent<Camera>().nearClipPlane = .03f;
        var light = new GameObject("CSY_PreviewLight", typeof(Light));
        SceneManager.MoveGameObjectToScene(light, preview);
        light.GetComponent<Light>().type = LightType.Directional;
        light.transform.rotation = Quaternion.Euler(50, -30, 0);
        var station = new GameObject("CSY_OptionalTimeline", typeof(CSYTimelineStation));
        SceneManager.MoveGameObjectToScene(station, preview);
        var data = new SerializedObject(station.GetComponent<CSYTimelineStation>());
        data.FindProperty("stationOffset").vector3Value = new Vector3(0, 0, 2.5f);
        data.FindProperty("viewer").objectReferenceValue = cameraObject.GetComponent<Camera>();
        data.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.SaveScene(preview, Preview);
        EditorSceneManager.CloseScene(preview, true);
        var scenes = EditorBuildSettings.scenes.ToList();
        if (!scenes.Any(scene => scene.path == Integrated)) scenes.Add(new EditorBuildSettingsScene(Integrated, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        AssetDatabase.SaveAssets();
        // Persist the appended entry in batch mode too, before entering Play.
        const string buildSettingsPath = "ProjectSettings/EditorBuildSettings.asset";
        string saved = File.ReadAllText(buildSettingsPath);
        if (!saved.Contains("path: " + Integrated))
        {
            const string configMarker = "  m_configObjects:";
            if (!saved.Contains(configMarker)) throw new InvalidOperationException("Unexpected Build Settings layout");
            string entry = "  - enabled: 1\n    path: " + Integrated + "\n    guid: " + AssetDatabase.AssetPathToGUID(Integrated) + "\n";
            File.WriteAllText(buildSettingsPath, saved.Replace(configMarker, entry + configMarker));
        }
        Debug.Log("[CSY] Timeline preview and integration built; teammate scenes preserved.");
    }
    [MenuItem("CSY Tools/Timeline/Open desktop preview")]
    public static void OpenPreview()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (!File.Exists(Preview)) Build();
        EditorSceneManager.OpenScene(Preview);
    }
}
#endif
