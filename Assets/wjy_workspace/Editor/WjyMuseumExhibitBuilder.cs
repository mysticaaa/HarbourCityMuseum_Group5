using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class WjyMuseumExhibitBuilder
{
    const string Root = "Assets/wjy_workspace/";
    [MenuItem("WJY/Museum/Rebuild fishing rod exhibit")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play mode first.");
        Directory.CreateDirectory(Root + ".work");
        var scene = EditorSceneManager.OpenScene(Root + "Museum.unity");
        var old = GameObject.Find("WJY_FishingRodStation");
        if (old) UnityEngine.Object.DestroyImmediate(old);
        var all = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
        var shell = all.Single(t => t.name == "PIECE");
        var room = all.Single(t => t.name == "wip_-_exhibition_room");
        var sizeControl = room.GetComponent<MuseumRoomLength>();
        if (!sizeControl)
        {
            sizeControl = room.gameObject.AddComponent<MuseumRoomLength>();
            var length = shell.GetComponentInChildren<Renderer>().bounds.size.x;
            sizeControl.Configure(shell, length, length * 1.25f);
        }
        var station = new GameObject("WJY_FishingRodStation");
        station.transform.position = new Vector3(3.2f, 0.1427f, -0.75f);
        var template = all.Single(t => t.name == "socle.005_socle_0");
        var pedestal = UnityEngine.Object.Instantiate(template.gameObject, station.transform);
        pedestal.name = "WhitePedestal";
        pedestal.transform.rotation = template.rotation;
        pedestal.transform.localScale = template.lossyScale;
        pedestal.transform.position = new Vector3(3.2f, template.position.y, -0.75f);
        var materialsPath = Root + "Models/FishingRod/Materials";
        Directory.CreateDirectory(materialsPath);
        pedestal.GetComponent<Renderer>().sharedMaterial = Material("PedestalWhite", new Color(.94f,.94f,.92f),0f,.18f);
        var pivot = new GameObject("WJY_FishingRodRotationPivot");
        pivot.transform.SetParent(station.transform, false);
        pivot.transform.position = new Vector3(3.2f,1f,-0.75f);
        var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Models/FishingRod/AntiqueFishingRod.obj"));
        if (!model) throw new InvalidOperationException("Fishing rod OBJ was not imported.");
        model.name = "AntiqueFishingRod";
        model.transform.SetParent(pivot.transform,false);
        var colours = new [] { new Color(.57f,.36f,.14f), new Color(.47f,.34f,.16f), new Color(.13f,.09f,.06f), new Color(.63f,.43f,.19f), new Color(.7f,.61f,.43f), new Color(.45f,.29f,.13f) };
        var names = new [] { "Bamboo", "Brass", "DarkWrap", "BambooDetail", "IvoryReel", "CorkGrip" };
        foreach (var renderer in model.GetComponentsInChildren<Renderer>())
        {
            renderer.sharedMaterials = renderer.sharedMaterials.Select(m =>
            {
                int i = Array.FindIndex(names,n => m && m.name.StartsWith(n,StringComparison.Ordinal));
                return i < 0 ? m : Material(names[i],colours[i],i==1?.55f:0f,i==1?.45f:.18f);
            }).ToArray();
        }
        // Exactly the same component and FixedUpdate logic as helmet and boat.
        var rotation = pivot.AddComponent<ExhibitAutoRotation>();
        var serialized = new SerializedObject(rotation);
        serialized.FindProperty("degreesPerSecond").floatValue = 15f;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        var lightObject = new GameObject("FishingRodDisplayLight");
        lightObject.transform.SetParent(station.transform,false);
        lightObject.transform.position = new Vector3(3.2f,3.8f,1.5f);
        lightObject.transform.LookAt(new Vector3(3.2f,1.6f,-.75f));
        var displayLight = lightObject.AddComponent<Light>();
        displayLight.type = LightType.Spot;
        displayLight.spotAngle = 70f;
        displayLight.range = 7f;
        displayLight.intensity = 2f;
        displayLight.color = new Color(1f,.94f,.84f);
        displayLight.shadows = LightShadows.Soft;
        Directory.CreateDirectory(Root + "Prefabs");
        PrefabUtility.SaveAsPrefabAsset(station,Root + "Prefabs/FishingRodExhibit.prefab");
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Directory.CreateDirectory(Root + "Documentation");
        Render(new Vector3(3.7f,2f,2.2f),new Vector3(3.2f,1.65f,-.75f),Root+"Documentation/FishingRodPreview.png");
        Render(new Vector3(-.7f,2.2f,2.3f),new Vector3(-.7f,1.5f,-1f),Root+"Documentation/GalleryPreview.png",100f);
        File.WriteAllText(Root+".work/build-report.txt", $"Room shell length={shell.GetComponentInChildren<Renderer>().bounds.size.x:F3}m\nRod bounds={BoundsOf(model).center:F3}/{BoundsOf(model).size:F3}\nPedestal bounds={pedestal.GetComponent<Renderer>().bounds.center:F3}/{pedestal.GetComponent<Renderer>().bounds.size:F3}\nRotators={UnityEngine.Object.FindObjectsOfType<ExhibitAutoRotation>().Length}\n");
        Debug.Log("[WJY] Fishing rod exhibit built and Museum scene saved.");
        VerifyFixedUpdates();
    }
    static void VerifyFixedUpdates()
    {
        var report = new StringBuilder();
        var rotators = UnityEngine.Object.FindObjectsOfType<ExhibitAutoRotation>();
        var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        var start = typeof(ExhibitAutoRotation).GetMethod("Start",flags);
        var step = typeof(ExhibitAutoRotation).GetMethod("FixedUpdate",flags);
        bool passed = rotators.Length==3;
        foreach(var r in rotators)
        {
            start.Invoke(r,null);
            var before = r.transform.rotation;
            var beforePosition = r.transform.position;
            for(int i=0;i<100;i++)step.Invoke(r,null);
            float angle=Quaternion.Angle(before,r.transform.rotation);
            passed &= angle>29f && angle<31f;
            var paused = r.transform.rotation;
            r.Paused=true;
            step.Invoke(r,null);
            passed &= Quaternion.Angle(paused,r.transform.rotation)<.001f;
            report.AppendLine($"{r.name}: 100 FixedUpdate calls rotated {angle:F3} degrees; pause works.");
            r.Paused=false;
            r.transform.SetPositionAndRotation(beforePosition,before);
        }
        report.AppendLine(passed?"PASS":"FAIL");
        File.WriteAllText(Root+".work/rotation-report.txt",report.ToString());
        if(!passed)throw new InvalidOperationException("Shared rotation function verification failed.");
    }
    static Bounds BoundsOf(GameObject model)
    {
        var renderers = model.GetComponentsInChildren<Renderer>();
        var bounds = renderers[0].bounds;
        foreach(var r in renderers.Skip(1)) bounds.Encapsulate(r.bounds);
        return bounds;
    }
    static Material Material(string name, Color colour, float metallic, float gloss)
    {
        string path = Root+"Models/FishingRod/Materials/"+name+".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material) return material;
        material = new Material(Shader.Find("Standard")) { name=name, color=colour };
        material.SetFloat("_Metallic",metallic);
        material.SetFloat("_Glossiness",gloss);
        AssetDatabase.CreateAsset(material,path);
        return material;
    }
    static void Render(Vector3 position, Vector3 target, string path, float fov=65f)
    {
        var go = new GameObject("WJY temporary preview");
        var camera = go.AddComponent<Camera>();
        camera.transform.position = position;
        camera.transform.LookAt(target);
        camera.fieldOfView = fov;
        camera.nearClipPlane = .05f;
        var rt = new RenderTexture(1600,1000,24);
        camera.targetTexture = rt;
        camera.Render();
        var previous = RenderTexture.active;
        RenderTexture.active = rt;
        var texture = new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);
        texture.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);
        texture.Apply();
        File.WriteAllBytes(path,texture.EncodeToPNG());
        RenderTexture.active = previous;
        camera.targetTexture = null;
        UnityEngine.Object.DestroyImmediate(texture);
        UnityEngine.Object.DestroyImmediate(rt);
        UnityEngine.Object.DestroyImmediate(go);
    }
}
