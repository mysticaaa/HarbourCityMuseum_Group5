using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Unity.XR.CoreUtils;
using Wjy.Museum;

// Integration tools run in an isolated project copy; only the saved target scene is copied back.
public static class WjyMuseum261016Integration
{
    const string ScenePath="Assets/261016/Museum_261016_2.unity";
    const string Output="Assets/wjy_workspace/Documentation/";
    static string PathOf(Transform t)=>t.parent?PathOf(t.parent)+"/"+t.name:t.name;
    public static void Audit()
    {
        var scene=EditorSceneManager.OpenScene(ScenePath);
        var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
        var s=new StringBuilder();
        foreach(var t in all)
        {
            var rig=t.GetComponent<XROrigin>();var ray=t.GetComponent<XRRayInteractor>();var manager=t.GetComponent<XRInteractionManager>();var camera=t.GetComponent<Camera>();
            if(rig||ray||manager||camera||t.name=="PIECE"||t.name.Contains("FishingBoat")||t.name.Contains("DivingHelmet"))
                s.AppendLine($"{PathOf(t)} active={t.gameObject.activeInHierarchy} pos={t.position} scale={t.localScale} rig={!!rig} camera={!!camera} manager={!!manager} ray={!!ray} rayManager={ray?.interactionManager?.name} forceGrab={ray?.useForceGrab}");
            var renderer=t.GetComponent<Renderer>();
            if(renderer&&(PathOf(t).Contains("PIECE")||t.name.Contains("FishingBoat")||t.name.Contains("socle")))s.AppendLine(PathOf(t)+" bounds="+renderer.bounds);
            foreach(var component in t.GetComponents<MonoBehaviour>())
                if(component&&(component.GetType().Name.Contains("Inspection")||component.GetType().Name.Contains("Exhibit")||component is XRBaseInteractable))
                    s.AppendLine($"  component={component.GetType().Name} active={component.isActiveAndEnabled} manager={(component as XRBaseInteractable)?.interactionManager?.name}");
        }
        Directory.CreateDirectory(Output);File.WriteAllText(Output+"Museum261016Audit.txt",s.ToString());
        Debug.Log(s.ToString());
    }
    public static void Integrate()
    {
        var scene=EditorSceneManager.OpenScene(ScenePath);
        var existing=scene.GetRootGameObjects().FirstOrDefault(g=>g.GetComponentInChildren<WjyFishingRodInteraction>(true));
        if(existing)UnityEngine.Object.DestroyImmediate(existing);
        var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
        var rig=all.Select(t=>t.GetComponent<XROrigin>()).Single(r=>r&&r.gameObject.activeInHierarchy);
        var managers=all.Select(t=>t.GetComponent<XRInteractionManager>()).Where(m=>m&&m.isActiveAndEnabled).ToArray();
        // Keep the scene's established manager; disable only the redundant prefab-local manager.
        var manager=managers.FirstOrDefault(m=>m.transform.parent==null)??managers.First();
        foreach(var interactable in all.Select(t=>t.GetComponent<XRBaseInteractable>()).Where(i=>i))
            if(interactable.interactionManager && interactable.interactionManager!=manager)
            {interactable.interactionManager=manager;PrefabUtility.RecordPrefabInstancePropertyModifications(interactable);}
        foreach(var other in managers.Where(m=>m!=manager))
        {
            other.enabled=false;PrefabUtility.RecordPrefabInstancePropertyModifications(other);
            other.gameObject.SetActive(false);PrefabUtility.RecordPrefabInstancePropertyModifications(other.gameObject);
        }
        foreach(var group in rig.GetComponentsInChildren<XRInteractionGroup>(true)) {group.interactionManager=manager;PrefabUtility.RecordPrefabInstancePropertyModifications(group);}
        foreach(var ray in rig.GetComponentsInChildren<XRBaseInteractor>(true)) {ray.interactionManager=manager;PrefabUtility.RecordPrefabInstancePropertyModifications(ray);}
        var station=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/wjy_workspace/Prefabs/TraditionalFishingRodExhibit.prefab"),scene);
        station.transform.position=new Vector3(3.2f,.1427f,-.75f);
        var rod=station.GetComponentInChildren<WjyFishingRodInteraction>();
        var serialized=new SerializedObject(rod);serialized.FindProperty("sharedRig").objectReferenceValue=rig;serialized.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.RecordPrefabInstancePropertyModifications(rod);
        var grab=rod.GetComponent<WjyFishingRodGrab>();grab.interactionManager=manager;PrefabUtility.RecordPrefabInstancePropertyModifications(grab);
        var shell=all.Single(t=>t.name=="PIECE");var shellRenderers=shell.GetComponentsInChildren<Renderer>();
        var bounds=shellRenderers[0].bounds;foreach(var renderer in shellRenderers.Skip(1))bounds.Encapsulate(renderer.bounds);
        var room=shell;while(room.parent)room=room.parent;
        var length=room.GetComponent<MuseumRoomLength>();
        if(!length){length=room.gameObject.AddComponent<MuseumRoomLength>();length.Configure(shell,bounds.size.x,bounds.size.x*1.25f);}
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        Audit();Render();
    }
    public static void Render()
    {
        var go=new GameObject("Temporary integration preview");var c=go.AddComponent<Camera>();c.transform.position=new Vector3(5.6f,3.3f,2.5f);c.transform.LookAt(new Vector3(2.5f,1.3f,-2));c.fieldOfView=65;c.nearClipPlane=.05f;
        var rt=new RenderTexture(1400,1000,24);c.targetTexture=rt;c.Render();RenderTexture.active=rt;var image=new Texture2D(1400,1000,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1400,1000),0,0);image.Apply();File.WriteAllBytes(Output+"Museum261016FishingPreview.png",image.EncodeToPNG());RenderTexture.active=null;c.targetTexture=null;UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(image);
    }
}
