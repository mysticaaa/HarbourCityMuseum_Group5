using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ZxhMuseum;

public static class VensailCabinIntegration
{
    public const string Source = "Assets/vensail_workspace/Scenes/VensailScene.unity";
    public const string Destination = "Assets/zxh_workspace/Scenes/Museum_Vensail_Cabin.unity";
    [MenuItem("ZXH/Build vensail cabin integration")]
    public static void Build()
    {
        var script = AssetDatabase.FindAssets("VensailCabinEntry t:MonoScript")
            .Select(AssetDatabase.GUIDToAssetPath).Single(p => p.EndsWith("/VensailCabinEntry.cs"));
        string guid = AssetDatabase.AssetPathToGUID(script);
        string original = File.ReadAllText(Source);
        const string sourceGuid = "56be8132ef1e55d4181f3a64851d6a1a";
        if (original.Split(new[] { sourceGuid }, StringSplitOptions.None).Length != 2)
            throw new InvalidOperationException("Expected one vensail ship entry; review teammate scene before integrating.");
        string integrated = original.Replace(sourceGuid, guid);
        // These prototype routes both load Ship_vensail, including one on the boat itself.
        // Disable only in our copy; preserve teammate source scripts and original scene.
        integrated = Regex.Replace(integrated, @"--- !u!114 &[^\r\n]+[\s\S]*?(?=--- !u!|\z)", match =>
            match.Value.Contains("guid: c38cc198dd23d664e91f3a897c2a7527")
                ? match.Value.Replace("m_Enabled: 1", "m_Enabled: 0") : match.Value);
        File.WriteAllText(Destination, integrated);
        AssetDatabase.ImportAsset(Destination);
        var scenes = EditorBuildSettings.scenes.ToList();
        foreach (var path in new[] { Destination, MuseumSession.InteriorPath })
            if (!scenes.Any(s => s.path == path)) scenes.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        AssetDatabase.SaveAssets();
        Debug.Log("ZXH INTEGRATION CREATED: teammate scene unchanged; zxh copy uses confirmation entry and disables old Ship_vensail prototype routes.");
    }
    [MenuItem("ZXH/Open vensail museum with cabin")]
    public static void Open() { Build(); EditorSceneManager.OpenScene(Destination); }
}
