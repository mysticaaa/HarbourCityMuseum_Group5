using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using ZzxWorkspace.FirstDay;

// Run in a disposable project copy. The source prefab and scene are not saved here.
public static class FirstDayReadableDisplay
{
    const string Source="Assets/zzx_workspace/FirstDayExhibit/Prefabs/FirstDayExhibit_261016.prefab";
    const string Output="Assets/zzx_workspace/FirstDayExhibit/Prefabs/FirstDayExhibit_261016_4.prefab";
    const string Report="Assets/zzx_workspace/Integration~/ReadableDisplay/";
    public static void UpdateFeedbackAndReset()
    {
        if(!Application.isBatchMode)throw new InvalidOperationException("Use an isolated batch-mode project copy.");
        var root=PrefabUtility.LoadPrefabContents(Output);
        try
        {
            var t=root.transform;
            Label(t.Find("Task feedback"),"",new Vector3(.05f,1.64f,.25f),new Vector2(.90f,.21f),.34f);
            t.Find("Task feedback").GetComponent<TMP_Text>().alignment=TextAlignmentOptions.Center;
            var reset=t.Find("Restart button") ?? t.Find("Journey ending/Restart button");
            reset.SetParent(t.Find("Journey ending"),false);
            reset.localPosition=new Vector3(.69f,1.28f,.10f);
            reset.Find("Button face").GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/zzx_workspace/FirstDayExhibit/Materials/WarmWhite.mat");
            var resetLabel=reset.Find("Label").GetComponent<TMP_Text>();
            resetLabel.text="RESET";resetLabel.alignment=TextAlignmentOptions.Center;
            resetLabel.rectTransform.pivot=new Vector2(.5f,.5f);resetLabel.rectTransform.sizeDelta=new Vector2(.39f,.18f);
            resetLabel.transform.localPosition=new Vector3(0,0,-.04f);resetLabel.fontSize=.60f;
            resetLabel.color=new Color(.055f,.12f,.17f);resetLabel.fontStyle=FontStyles.Bold;
            var endingPanel=t.Find("Journey ending/Ending panel");
            endingPanel.localPosition=new Vector3(0,1.51f,.15f);endingPanel.localScale=new Vector3(1.96f,.70f,.025f);
            var story=t.Find("Journey ending/Story ending").GetComponent<TMP_Text>();
            story.transform.localPosition=new Vector3(-.87f,1.79f,.12f);
            story.rectTransform.sizeDelta=new Vector2(1.74f,.40f);
            story.fontSize=.39f;
            root.GetComponent<FirstDayJourney>().ResetJourney();
            PrefabUtility.SaveAsPrefabAsset(root,Output);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene);
        var display=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Output));
        var task=display.GetComponent<FirstDayJourney>();
        task.Choose(1);Render(display);File.Copy(Report+"preview.png",Report+"map-feedback.png",true);
        task.ResetJourney();task.Choose(2);Render(display);File.Copy(Report+"preview.png",Report+"ending-reset.png",true);
        var button=display.transform.Find("Journey ending/Restart button");
        var action=button.GetComponent<FirstDayMapButton>();
        if(action.berth!=0 || action.journey!=task || !button.gameObject.activeInHierarchy)throw new InvalidOperationException("RESET not wired or visible");
        var face=button.GetComponent<BoxCollider>().bounds;
        var panel=display.transform.Find("Journey ending/Ending panel").GetComponent<Renderer>().bounds;
        var storyText=display.transform.Find("Journey ending/Story ending").GetComponent<TMP_Text>();storyText.ForceMeshUpdate();
        if(face.max.z>=panel.min.z || storyText.isTextOverflowing)throw new InvalidOperationException("RESET obscured or ending text overflow");
        task.ResetJourney();
        if(task.ending.activeSelf || task.IsComplete || task.Attempts!=0 || task.feedback.text!="")throw new InvalidOperationException("Reset failed");
        File.WriteAllText(Report+"reset-check.txt","PASS: feedback centred over map; RESET visible on completion, berth=0 wired to existing journey; collider in front of panel; ending text fits; reset closes ending, clears answer and feedback.\n");
        UnityEngine.Object.DestroyImmediate(display);
        FirstDayMuseumIntegrationChecks.Smoke();
    }
    public static void PreviewAndCheck()
    {
        if(!Application.isBatchMode)throw new InvalidOperationException("Use an isolated batch-mode project copy.");
        UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene);
        var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Output));
        Render(root);
        UnityEngine.Object.DestroyImmediate(root);
        FirstDayMuseumIntegrationChecks.Smoke();
    }
    public static void Build()
    {
        if(!Application.isBatchMode)throw new InvalidOperationException("Use an isolated batch-mode project copy.");
        Directory.CreateDirectory(Report);
        if(!AssetDatabase.CopyAsset(Source,Output))throw new InvalidOperationException("Output already exists or copy failed.");
        var root=PrefabUtility.LoadPrefabContents(Output);
        try
        {
            var t=root.transform;
            foreach(var path in new[]{"Collection","Instructions","Progress","Fiction notice","Documents/Letter/Heading","Documents/Work pass/Heading","Harbour map/Map title","Harbour map/Dock Street","Harbour map/Street label","Harbour map/Pier 1","Harbour map/Pier 2","Harbour map/Pier 3"})t.Find(path).gameObject.SetActive(false);
            foreach(var hint in root.GetComponentsInChildren<FirstDayInspectionHint>(true))hint.enabled=false;
            Label(t.Find("Title"),"FIRST DAY IN THE HARBOUR",new Vector3(-.92f,2.02f,.39f),new Vector2(1.84f,.14f),.78f);
            Label(t.Find("Introduction"),"Liang is a migrant dockworker arriving in 1958.\nWhere is his first workplace? Read his letter home\nand work pass, then choose a berth on the map.",new Vector3(-.92f,1.83f,.39f),new Vector2(1.84f,.31f),.48f);
            Label(t.Find("Task feedback"),"",new Vector3(-.92f,1.51f,.38f),new Vector2(1.84f,.10f),.34f);
            Sign(t,"Letter sign","LETTER HOME",-.69f,.43f);
            Sign(t,"Pass sign","WORK PASS",-.21f,.43f);
            Sign(t,"Map sign","HARBOUR MAP",.50f,.90f);
            foreach(var name in new[]{"Letter","Work pass"})
            {
                var body=t.Find("Documents/"+name+"/Document text").GetComponent<TMP_Text>();
                var position=body.transform.localPosition;position.y+=.09f;body.transform.localPosition=position;
                var area=body.rectTransform.sizeDelta;area.y+=.09f;body.rectTransform.sizeDelta=area;
                body.fontSize=name=="Letter"?.39f:.42f;
            }
            for(int i=1;i<=3;i++)
            {
                var button=t.Find("Harbour map/Berth "+i);
                button.localPosition=new Vector3((i-2)*.45f,0,-.08f);
                var label=button.Find("Label").GetComponent<TMP_Text>();
                label.alignment=TextAlignmentOptions.Center;
                label.fontSize=.48f;
            }
            var journey=root.GetComponent<FirstDayJourney>();journey.concisePresentation=true;journey.ResetJourney();
            PrefabUtility.SaveAsPrefabAsset(root,Output);
            Render(root);
            var texts=root.GetComponentsInChildren<TMP_Text>().Where(x=>x.gameObject.activeInHierarchy).ToArray();
            File.WriteAllText(Report+"layout.txt",string.Join("\n",texts.Select(x=>x.name+" font="+x.fontSize+" overflow="+x.isTextOverflowing+" text="+x.text)));
            if(texts.Any(x=>x.isTextOverflowing && (x.name=="Introduction" || x.name=="Title" || x.name.EndsWith(" sign"))))throw new InvalidOperationException("Main text overflow");
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();
    }
    static void Label(Transform t,string content,Vector3 position,Vector2 size,float font)
    {
        t.localPosition=position;
        var text=t.GetComponent<TMP_Text>();text.text=content;text.rectTransform.sizeDelta=size;text.fontSize=font;
        text.ForceMeshUpdate(true);
    }
    static void Sign(Transform root,string name,string content,float x,float width)
    {
        var panel=GameObject.CreatePrimitive(PrimitiveType.Cube);panel.name=name+" panel";panel.transform.SetParent(root,false);
        panel.transform.localPosition=new Vector3(x,1.345f,.28f);panel.transform.localScale=new Vector3(width,.13f,.018f);
        UnityEngine.Object.DestroyImmediate(panel.GetComponent<Collider>());
        panel.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/zzx_workspace/FirstDayExhibit/Materials/WarmWhite.mat");
        var go=new GameObject(name);go.transform.SetParent(root,false);
        var label=go.AddComponent<TextMeshPro>();label.font=root.Find("Title").GetComponent<TMP_Text>().font;
        label.color=new Color(.055f,.12f,.17f);label.fontStyle=FontStyles.Bold;label.alignment=TextAlignmentOptions.Center;
        label.rectTransform.pivot=new Vector2(.5f,.5f);label.enableWordWrapping=false;
        Label(go.transform,content,new Vector3(x,1.345f,.265f),new Vector2(width-.025f,.105f),width<.5f?.40f:.53f);
    }
    static void Render(GameObject root)
    {
        foreach(var text in root.GetComponentsInChildren<TMP_Text>())text.ForceMeshUpdate(true);
        var go=new GameObject("Temporary display camera");var camera=go.AddComponent<Camera>();
        var lightGo=new GameObject("Temporary display light");var light=lightGo.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.2f;lightGo.transform.rotation=Quaternion.Euler(35,-20,0);
        var scene=root.scene;UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,scene);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(lightGo,scene);
        camera.scene=scene;camera.transform.position=new Vector3(0,1.95f,-3.2f);camera.transform.LookAt(new Vector3(0,1.12f,0));camera.fieldOfView=41;camera.nearClipPlane=.05f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.13f,.16f,.19f);
        var rt=new RenderTexture(1600,1600,24);camera.targetTexture=rt;camera.Render();var previous=RenderTexture.active;RenderTexture.active=rt;
        var image=new Texture2D(1600,1600,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,1600),0,0);image.Apply();File.WriteAllBytes(Report+"preview.png",image.EncodeToPNG());
        RenderTexture.active=previous;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(lightGo);
    }
}
