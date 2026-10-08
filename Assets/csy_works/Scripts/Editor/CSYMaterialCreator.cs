#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// 一键创建 Module 2 所需的基础房间材质。
/// 菜单: CSY Tools > Create Room Materials
/// 所有材质输出到 Assets/csy_works/Materials/
///
/// COMP5424 Phase 2 — Harbour City Museum (Group 5) · Module 2 · csy
/// </summary>
public static class CSYMaterialCreator
{
    private const string OUTPUT_FOLDER = "Assets/csy_works/Materials";

    [MenuItem("CSY Tools/Create Room Materials")]
    public static void CreateAll()
    {
        EnsureFolder(OUTPUT_FOLDER);

        CreateMaterial("Mat_Floor_DarkWood",   new Color(0.235f, 0.165f, 0.125f), 0.8f, 0f);
        CreateMaterial("Mat_Wall_Cream",       new Color(0.96f,  0.94f,  0.91f),  0.9f, 0f);
        CreateMaterial("Mat_Wainscoting_Teak",  new Color(0.545f, 0.435f, 0.278f), 0.6f, 0f);
        CreateMaterial("Mat_Ceiling_OffWhite",  new Color(0.973f, 0.965f, 0.949f), 0.9f, 0f);
        CreateMaterial("Mat_DisplayPlatform",   new Color(0.173f, 0.173f, 0.165f), 0.4f, 0f);
        CreateMaterial("Mat_Pedestal_Teak",     new Color(0.545f, 0.435f, 0.278f), 0.5f, 0f);
        CreateMaterial("Mat_TimelinePanel_Navy", new Color(0.102f, 0.227f, 0.290f), 0.7f, 0f);
        CreateMaterial("Mat_BrassTrim",        new Color(0.722f, 0.525f, 0.043f), 0.3f, 0.8f);
        CreateMaterial("Mat_Parchment",         new Color(0.910f, 0.835f, 0.690f), 0.9f, 0f);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[CSYMaterialCreator] All materials created in " + OUTPUT_FOLDER);
    }

    private static void CreateMaterial(string name, Color albedo, float roughness, float metallic)
    {
        string path = OUTPUT_FOLDER + "/" + name + ".mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
        {
            existing.color = albedo;
            existing.SetFloat("_Glossiness", 1f - roughness);
            existing.SetFloat("_Metallic", metallic);
            EditorUtility.SetDirty(existing);
            return;
        }

        Material mat = new Material(Shader.Find("Standard"));
        mat.color = albedo;
        mat.SetFloat("_Glossiness", 1f - roughness);
        mat.SetFloat("_Metallic", metallic);
        AssetDatabase.CreateAsset(mat, path);
        Debug.Log("[CSYMaterialCreator] Created: " + name);
    }

    private static void EnsureFolder(string path)
    {
        string[] parts = path.Split('/');
        string current = "";
        foreach (string part in parts)
        {
            current = string.IsNullOrEmpty(current) ? part : current + "/" + part;
            if (!AssetDatabase.IsValidFolder(current))
            {
                string parent = System.IO.Path.GetDirectoryName(current).Replace('\\', '/');
                string folderName = System.IO.Path.GetFileName(current);
                AssetDatabase.CreateFolder(parent, folderName);
            }
        }
    }
}
#endif
