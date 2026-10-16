using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ZzxWorkspace.FirstDay;
using TMPro;
using Object=UnityEngine.Object;

namespace ZzxWorkspace.FirstDayEditor
{
    public static class FirstDayPlacement
    {
        public static void SidePosition(GameObject root)
        {
            root.transform.SetPositionAndRotation(new Vector3(3.5f,.145f,-.75f),Quaternion.Euler(0,90,0));
            root.transform.localScale=Vector3.one;
        }
        public static void CompactPrefab()
        {
            var root=PrefabUtility.LoadPrefabContents(FirstDayExhibitBuilder.Prefab);
            try
            {
                var t=root.transform;
                Set(t,"Archive plinth",new Vector3(0,.43f,.1f),new Vector3(2,.86f,.70f));
                Set(t,"Brass rim",new Vector3(0,.88f,.1f),new Vector3(2.04f,.025f,.72f));
                Set(t,"Interpretation wall",new Vector3(0,1.51f,.43f),new Vector3(2,1.14f,.045f));
                Label(t,"Collection",new Vector3(-.92f,2.01f,.40f),new Vector2(1.84f,.08f),.033f,"HARBOUR CITY MUSEUM / WORK & MIGRATION");
                Label(t,"Title",new Vector3(-.92f,1.89f,.40f),new Vector2(1.84f,.13f),.072f,"FIRST DAY IN THE HARBOUR");
                Label(t,"Introduction",new Vector3(-.92f,1.73f,.40f),new Vector2(1.84f,.14f),.041f,"Find Liang's first workplace using the letter, pass and map.\nA fictional port-city story, set in 1958.");
                Label(t,"Instructions",new Vector3(-.92f,1.55f,.40f),new Vector2(1.84f,.14f),.041f,"GRAB the letter and pass; release to return.\nPoint at a berth and press TRIGGER to choose.");
                Label(t,"Progress",new Vector3(-.92f,1.40f,.40f),new Vector2(1.5f,.09f),.028f,null);
                Label(t,"Task feedback",new Vector3(-.92f,1.28f,.39f),new Vector2(1.48f,.2f),.04f,null);
                Set(t,"Documents/Letter",new Vector3(-.69f,1.03f,-.08f),Vector3.one*.68f);
                Set(t,"Documents/Work pass",new Vector3(-.21f,1.00f,-.08f),Vector3.one*.68f);
                Set(t,"Harbour map",new Vector3(.5f,1.03f,.02f),Vector3.one*.62f);
                Set(t,"Restart button",new Vector3(.82f,1.05f,.36f),Vector3.one*.65f);
                Set(t,"Journey ending/Ending panel",new Vector3(0,1.51f,.28f),new Vector3(1.88f,.64f,.025f));
                Label(t,"Journey ending/Story ending",new Vector3(-.87f,1.79f,.26f),new Vector2(1.74f,.56f),.043f,null);
                Label(t,"Fiction notice",new Vector3(-.92f,.62f,-.258f),new Vector2(1.84f,.16f),.03f,"INTERPRETIVE RECONSTRUCTION\nAll people, documents and places are fictional.");
                int index=0;
                foreach(Transform child in t.Find("Documents"))
                {
                    if(child.name!="Inspection hint")continue;
                    var hint=child.GetComponentInChildren<TMP_Text>(true);
                    hint.transform.localPosition=new Vector3(index++==0?-.92f:.04f,1.66f,.21f);
                    hint.rectTransform.sizeDelta=new Vector2(.88f,.15f);hint.fontSize=.32f;
                }
                PrefabUtility.SaveAsPrefabAsset(root,FirstDayExhibitBuilder.Prefab);
            }
            finally {PrefabUtility.UnloadPrefabContents(root);}
        }
        private static void Set(Transform root,string path,Vector3 position,Vector3 scale)
        {
            var item=root.Find(path);item.localPosition=position;item.localScale=scale;
        }
        private static void Label(Transform root,string path,Vector3 position,Vector2 area,float size,string content)
        {
            var item=root.Find(path);item.localPosition=position;
            var text=item.GetComponent<TMP_Text>();text.rectTransform.sizeDelta=area;text.fontSize=size*10;
            if(content!=null)text.text=content;
        }
        [MenuItem("Harbour City Museum/ZZX/First Day/9 - Compact exhibit at empty side wall %#F3")]
        public static void CompactAndPlace()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play mode first.");
            var existing=SceneManager.GetSceneByPath(FirstDayExhibitBuilder.Preview);
            bool opened=!existing.IsValid();
            var scene=opened?EditorSceneManager.OpenScene(FirstDayExhibitBuilder.Preview,OpenSceneMode.Additive):existing;
            if(scene.isDirty)throw new InvalidOperationException("Save your ZZX scene changes before adjusting the exhibit.");
            try
            {
                CompactPrefab();
                var exhibit=scene.GetRootGameObjects().Single(x=>x.GetComponent<FirstDayJourney>()!=null);
                SidePosition(exhibit);
                EditorSceneManager.SaveScene(scene,FirstDayExhibitBuilder.Preview);
                // Cameras exist only during inspection and are never saved in either scene.
                Render(scene,new Vector3(-2,2.2f,1),new Vector3(3.4f,1.05f,-.75f),"SideWallIntegration.png");
                Render(scene,new Vector3(.5f,1.85f,-.75f),new Vector3(3.5f,1.2f,-.75f),"CompactExhibit.png");
                var camera=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<Camera>(true)).First(x=>x.enabled&&x.gameObject.activeInHierarchy);
                Render(scene,camera.transform.position,camera.transform.position+camera.transform.forward*4,"UnchangedArrivalView.png");
                File.WriteAllText(FirstDayExhibitBuilder.Folder+"/Validation/PlacementReport.txt","Only the ZZX prefab and its instance were changed.\nRoot: (3.5, 0.145, -0.75), Y rotation: 90 degrees.\nPlinth: 2.04m wide including rim, 0.72m deep, 2.08m overall height.\nExisting exhibits, cameras and input settings untouched.\n");
            }
            finally {if(opened)EditorSceneManager.CloseScene(scene,true);}
            AssetDatabase.SaveAssets();
        }
        [MenuItem("Harbour City Museum/ZZX/First Day/8 - Survey existing room %#F4")]
        public static void Survey()
        {
            var existing=SceneManager.GetSceneByPath(FirstDayExhibitBuilder.Preview);
            bool opened=!existing.IsValid();
            var scene=opened?EditorSceneManager.OpenScene(FirstDayExhibitBuilder.Preview,OpenSceneMode.Additive):existing;
            try
            {
                var text=new StringBuilder();
                var renderers=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Renderer>(true));
                foreach(var r in renderers.Where(r=>r.gameObject.activeInHierarchy))
                {
                    string path=r.name;for(var p=r.transform.parent;p!=null;p=p.parent)path=p.name+"/"+path;
                    text.AppendLine(path+" | center="+r.bounds.center.ToString("F3")+" | size="+r.bounds.size.ToString("F3"));
                }
                File.WriteAllText(FirstDayExhibitBuilder.Folder+"/Validation/RoomSurvey.txt",text.ToString());
                Render(scene,new Vector3(0,17,0),new Vector3(0,0,0),"RoomTopDown.png",true);
            }
            finally {if(opened)EditorSceneManager.CloseScene(scene,true);}
        }
        public static void Render(Scene scene,Vector3 position,Vector3 target,string filename,bool top=false)
        {
            var cameraObject=new GameObject("ZZX temporary placement inspection");
            SceneManager.MoveGameObjectToScene(cameraObject,scene);
            var camera=cameraObject.AddComponent<Camera>();
            RenderTexture texture=null;Texture2D output=null;
            try
            {
                camera.scene=scene;camera.transform.position=position;camera.transform.LookAt(target);
                camera.fieldOfView=65;camera.nearClipPlane=.05f;
                if(top){camera.orthographic=true;camera.orthographicSize=10;camera.farClipPlane=100;}
                texture=new RenderTexture(1600,1200,24);camera.targetTexture=texture;camera.Render();
                var previous=RenderTexture.active;
                try{RenderTexture.active=texture;output=new Texture2D(1600,1200,TextureFormat.RGB24,false);output.ReadPixels(new Rect(0,0,1600,1200),0,0);output.Apply();}
                finally{RenderTexture.active=previous;}
                File.WriteAllBytes(FirstDayExhibitBuilder.Folder+"/Validation/"+filename,output.EncodeToPNG());
                camera.targetTexture=null;
            }
            finally{Object.DestroyImmediate(cameraObject);if(texture!=null)Object.DestroyImmediate(texture);if(output!=null)Object.DestroyImmediate(output);}
        }
    }
}
