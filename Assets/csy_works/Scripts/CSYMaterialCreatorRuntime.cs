using UnityEngine;

/// <summary>
/// 运行时/编辑器均可用的材质生成器。
/// 挂载到任意空 GameObject 上，Inspector 面板点击齿轮 → Create Room Materials。
/// 不需要顶部菜单。
///
/// COMP5424 Phase 2 — Harbour City Museum (Group 5) · Module 2 · csy
/// </summary>
public class CSYMaterialCreatorRuntime : MonoBehaviour
{
    private const string OUTPUT_FOLDER = "Assets/csy_works/Materials";

    [ContextMenu("Create Room Materials")]
    public void CreateAll()
    {
#if UNITY_EDITOR
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

        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.AssetDatabase.Refresh();
        Debug.Log("[CSYMaterialCreatorRuntime] All materials created in " + OUTPUT_FOLDER);
#else
        Debug.LogWarning("[CSYMaterialCreatorRuntime] Material creation is editor-only.");
#endif
    }

#if UNITY_EDITOR
    private void CreateMaterial(string name, Color albedo, float roughness, float metallic)
    {
        string path = OUTPUT_FOLDER + "/" + name + ".mat";
        UnityEngine.Material existing = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(path);
        if (existing != null)
        {
            existing.color = albedo;
            existing.SetFloat("_Glossiness", 1f - roughness);
            existing.SetFloat("_Metallic", metallic);
            UnityEditor.EditorUtility.SetDirty(existing);
            return;
        }

        UnityEngine.Material mat = new UnityEngine.Material(Shader.Find("Standard"));
        mat.color = albedo;
        mat.SetFloat("_Glossiness", 1f - roughness);
        mat.SetFloat("_Metallic", metallic);
        UnityEditor.AssetDatabase.CreateAsset(mat, path);
        Debug.Log("[CSYMaterialCreatorRuntime] Created: " + name);
    }

    private void EnsureFolder(string path)
    {
        string[] parts = path.Split('/');
        string current = "";
        foreach (string part in parts)
        {
            current = string.IsNullOrEmpty(current) ? part : current + "/" + part;
            if (!UnityEditor.AssetDatabase.IsValidFolder(current))
            {
                string parent = System.IO.Path.GetDirectoryName(current).Replace('\\', '/');
                string folderName = System.IO.Path.GetFileName(current);
                UnityEditor.AssetDatabase.CreateFolder(parent, folderName);
            }
        }
    }
#endif
}
