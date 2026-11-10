using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using ZzxWorkspace.FirstDay;

public static class HarbourPrefabMigration
{
    private const string Root="Assets/zzx_workspace/FirstDayExhibit/";
    private const string Prefab=Root+"Prefabs/FirstDayExhibit_261016_4.prefab";
    private const string AssetsFolder=Root+"SavedHarbourAssets";
    public static void Batch()
    {
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene(HarbourTabletopPrototype.TargetScene);
        Migrate();
    }
    public static void Compare()
    {
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene(HarbourTabletopPrototype.TargetScene);
        var manager=UnityEngine.Object.FindObjectsOfType<XRInteractionManager>().First(x=>x.gameObject.scene.path==HarbourTabletopPrototype.TargetScene);
        var saved=PrefabUtility.LoadPrefabContents(Prefab);
        var generated=new GameObject("Comparison only");
        try
        {
            generated.AddComponent<HarbourTabletopPrototype>().BuildSavedExhibit(manager);
            var a=saved.GetComponentsInChildren<Transform>(true).Skip(1).ToArray();
            var b=generated.GetComponentsInChildren<Transform>(true).Skip(1).ToArray();
            if(a.Length!=b.Length) throw new Exception("Geometry object count differs: "+a.Length+" / "+b.Length);
            for(int i=0;i<a.Length;i++)
            {
                if(a[i].name!=b[i].name || Vector3.Distance(a[i].localPosition,b[i].localPosition)>.00001f || Vector3.Distance(a[i].localScale,b[i].localScale)>.00001f || Quaternion.Angle(a[i].localRotation,b[i].localRotation)>.001f || a[i].gameObject.activeSelf!=b[i].gameObject.activeSelf)
                    throw new Exception("Saved geometry differs at "+i+": "+a[i].name+" / "+b[i].name);
                var ta=a[i].GetComponent<TMPro.TMP_Text>(); var tb=b[i].GetComponent<TMPro.TMP_Text>();
                if(ta && (ta.text!=tb.text || ta.fontSize!=tb.fontSize)) throw new Exception("Text differs at "+a[i].name);
                var ra=a[i].GetComponent<Renderer>(); var rb=b[i].GetComponent<Renderer>();
                if(ra && rb && ra.sharedMaterial && rb.sharedMaterial && ra.sharedMaterial.HasProperty("_Color") && rb.sharedMaterial.HasProperty("_Color") && ra.sharedMaterial.color!=rb.sharedMaterial.color)
                    throw new Exception("Color differs at "+a[i].name);
            }
            File.WriteAllText(Root+"Validation~/GeometryComparison.txt","PASS: "+a.Length+" object transforms, active states, text and material colors match the original generator. No scene saved.");
        }
        finally { PrefabUtility.UnloadPrefabContents(saved); UnityEngine.Object.DestroyImmediate(generated); }
    }
    [MenuItem("Harbour City Museum/ZZX/Save editable harbour prefab %&b")]
    public static void Migrate()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play first.");
        var existing=AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);
        if(existing.GetComponent<HarbourTabletopPrototype>()?.IsSavedExhibit==true)
        {
            if(new SerializedObject(existing.GetComponent<HarbourTabletopPrototype>()).FindProperty("wood").objectReferenceValue)
            { Debug.Log("Harbour prefab already saved."); return; }
            File.Copy(Root+"IntegrationBackup~/FirstDayExhibit.before-saved.prefab",Prefab,true);
            AssetDatabase.ImportAsset(Prefab,ImportAssetOptions.ForceUpdate);
        }
        Directory.CreateDirectory(Root+"IntegrationBackup~");
        if(!File.Exists(Root+"IntegrationBackup~/FirstDayExhibit.before-saved.prefab")) File.Copy(Prefab,Root+"IntegrationBackup~/FirstDayExhibit.before-saved.prefab");
        if(!AssetDatabase.IsValidFolder(AssetsFolder)) AssetDatabase.CreateFolder(Root.TrimEnd('/'),"SavedHarbourAssets");
        GameObject root=PrefabUtility.LoadPrefabContents(Prefab);
        try
        {
            var shared=UnityEngine.Object.FindObjectsOfType<XRInteractionManager>().FirstOrDefault(x=>x.gameObject.scene.path==HarbourTabletopPrototype.TargetScene);
            if(!shared) throw new InvalidOperationException("Open the target museum with its existing XRInteractionManager first.");
            var controller=root.AddComponent<HarbourTabletopPrototype>();
            controller.BuildSavedExhibit(shared);
            // The generated geometry replaces only this exhibit's old children.
            foreach(Transform child in root.transform.Cast<Transform>().ToArray())
                if(child.name!="Harbour tabletop prototype" && child.name!="Wood tabletop" && child.name!="Table apron" && child.name!="Wood table leg")
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
            foreach(var legacy in root.GetComponents<FirstDayJourney>()) UnityEngine.Object.DestroyImmediate(legacy);
            controller.BindSavedManager(null); // Scene objects cannot be stored in a prefab asset.
            foreach(var text in root.GetComponentsInChildren<TMPro.TMP_Text>(true)) text.ForceMeshUpdate(true);
            int index=0;
            // Persist generated meshes/textures/materials before saving their references.
            var dependencies=controller.GeneratedAssets().ToArray();
            foreach(var item in dependencies)
            {
                if(!item || AssetDatabase.Contains(item) || !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(item))) continue;
                if(!(item is Mesh) && !(item is Material) && !(item is Texture2D)) continue;
                // TMP-generated text meshes are regenerated from their serialized text.
                if(item is Mesh mesh && mesh.name.StartsWith("TextMeshPro")) continue;
                string ext=item is Material ? ".mat" : ".asset";
                string path=AssetDatabase.GenerateUniqueAssetPath(AssetsFolder+"/Generated_"+(index++)+ext);
                AssetDatabase.CreateAsset(item,path);
            }
            PrefabUtility.SaveAsPrefabAsset(root,Prefab);
            AssetDatabase.SaveAssets();
            File.WriteAllText(Root+"IntegrationBackup~/MigrationReport.txt","Saved editable prefab: "+Prefab+"\nGenerated assets: "+index+"\nTarget scene and public XR assets unchanged.\n");
            Debug.Log("Saved editable harbour prefab; target scene was not modified.");
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
}
