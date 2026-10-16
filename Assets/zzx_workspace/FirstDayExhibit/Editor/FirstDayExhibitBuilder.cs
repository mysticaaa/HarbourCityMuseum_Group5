using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using ZzxWorkspace.FirstDay;
using Object = UnityEngine.Object;

namespace ZzxWorkspace.FirstDayEditor
{
    public static class FirstDayExhibitBuilder
    {
        public const string Folder = "Assets/zzx_workspace/FirstDayExhibit";
        public const string Source = "Assets/261016/Museum_261016.unity";
        public const string Preview = "Assets/zzx_workspace/Scenes/Museum_FirstDay_ZZX.unity";
        public const string Prefab = Folder + "/Prefabs/FirstDayExhibit.prefab";
        private static Material navy, paper, copper, sea, ink, white;
        private static TMP_FontAsset font;

        [MenuItem("Harbour City Museum/ZZX/First Day/1 - Build exhibit and copied museum %#F11")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode first.");
            if (File.Exists(Preview) && File.Exists(Prefab)) { Validate(); return; }
            if (File.Exists(Preview) || File.Exists(Prefab)) throw new InvalidOperationException("Partial output exists; inspect it before regeneration.");
            Directory.CreateDirectory(Folder + "/Prefabs");
            Directory.CreateDirectory(Folder + "/Materials");
            Directory.CreateDirectory("Assets/zzx_workspace/Scenes");
            Directory.CreateDirectory(Folder + "/Validation");
            AssetDatabase.Refresh();
            if (!AssetDatabase.CopyAsset(Source, Preview)) throw new IOException("Could not copy the museum scene.");
            var originalActive = SceneManager.GetActiveScene();
            Scene scene = default;
            GameObject root = null;
            try
            {
                scene = EditorSceneManager.OpenScene(Preview, OpenSceneMode.Additive);
                SceneManager.SetActiveScene(scene);
                var all = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Transform>(true)).ToArray();
                var grab = all.First(x => x.name == "TestCube4_Grabing").GetComponent<XRGrabInteractable>();
                var button = all.First(x => x.name == "Btn_Play" && x.GetComponent<XRSimpleInteractable>() != null).GetComponent<XRSimpleInteractable>();
                navy = Mat("HarbourNavy", new Color(.055f,.12f,.17f));
                paper = Mat("ArchivePaper", new Color(.87f,.81f,.65f));
                copper = Mat("Brass", new Color(.72f,.47f,.22f));
                sea = Mat("MapWater", new Color(.12f,.29f,.34f));
                ink = Mat("Ink", new Color(.07f,.12f,.14f));
                white = Mat("WarmWhite", new Color(.95f,.93f,.84f));
                font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
                if (font == null) throw new InvalidOperationException("Existing LiberationSans SDF font not found.");
                root = new GameObject("ZZX_FirstDayExhibit");
                var task = root.AddComponent<FirstDayJourney>();
                // Standalone display faces local -Z. Root scale stays at one.
                Box(root.transform,"Archive plinth", new Vector3(0,.48f,.15f),new Vector3(3.8f,.96f,.9f),navy);
                Box(root.transform,"Brass rim",new Vector3(0,.975f,.15f),new Vector3(3.84f,.035f,.94f),copper);
                Box(root.transform,"Interpretation wall",new Vector3(0,1.85f,.48f),new Vector3(3.8f,1.7f,.08f),navy);
                Text(root.transform,"Collection", "HARBOUR CITY MUSEUM   /   WORK & MIGRATION",new Vector3(-1.75f,2.62f,.42f),new Vector2(3.5f,.12f),.055f,ColorOf(copper));
                Text(root.transform,"Title","FIRST DAY IN THE HARBOUR",new Vector3(-1.75f,2.40f,.42f),new Vector2(3.5f,.25f),.115f,ColorOf(white));
                Text(root.transform,"Introduction","One arrival. Three clues. Find Liang's first workplace.\nA fictional port-city story, set in 1958.",new Vector3(-1.75f,2.17f,.42f),new Vector2(3.5f,.22f),.065f,ColorOf(white));
                Text(root.transform,"Instructions","GRAB to inspect the letter and pass; release to return.\nPoint at a map marker and press TRIGGER to choose.",new Vector3(-1.75f,1.98f,.42f),new Vector2(3.5f,.25f),.066f,ColorOf(paper));
                task.progress = Text(root.transform,"Progress","",new Vector3(-1.75f,1.74f,.42f),new Vector2(3.5f,.18f),.05f,ColorOf(copper));
                task.feedback = Text(root.transform,"Task feedback","",new Vector3(-1.75f,1.57f,.40f),new Vector2(3.0f,.29f),.055f,ColorOf(white));
                // A low inclined lectern holds the map; letter and pass sit to its left.
                var documents = new GameObject("Documents"); documents.transform.SetParent(root.transform,false);
                Document(documents.transform, grab, "Letter", new Vector3(-1.36f,1.21f,-.12f),new Vector2(.63f,.72f),
                    "LETTER HOME / 1958", "Dear Mei,\nI arrived by ferry this morning.\nTomorrow I will carry cargo at\nthe berth written on my pass.\nI found a bed on Dock Street,\na short walk from the harbour.\nWhen wages come, I will send\nmoney home.\nLiang", "The ferry marks his arrival, not his workplace.");
                Document(documents.transform, grab, "Work pass",new Vector3(-.58f,1.15f,-.12f),new Vector2(.60f,.47f),
                    "HARBOUR WORK PASS", "LIANG CHEN\nCargo handling / Day shift\nREPORT TO BERTH 02\nIssued 14 May 1958", "Cargo handling. Berth 02. Compare this with the map.");
                var map = new GameObject("Harbour map"); map.transform.SetParent(root.transform,false);
                map.transform.localPosition = new Vector3(.88f,1.23f,.1f);
                map.transform.localRotation = Quaternion.Euler(62,0,0);
                Box(map.transform,"Map board",Vector3.zero,new Vector3(1.45f,.88f,.035f),paper);
                Box(map.transform,"Harbour water",new Vector3(0,.02f,-.025f),new Vector3(1.35f,.48f,.012f),sea,false);
                Text(map.transform,"Map title","HARBOUR DISTRICT / 1958",new Vector3(-.64f,.35f,-.04f),new Vector2(1.3f,.12f),.052f,ColorOf(ink));
                Box(map.transform,"Dock Street",new Vector3(0,-.29f,-.025f),new Vector3(1.30f,.065f,.012f),copper,false);
                Text(map.transform,"Street label","DOCK STREET / WORKERS' LODGING",new Vector3(-.63f,-.37f,-.04f),new Vector2(1.3f,.10f),.033f,ColorOf(ink));
                for (int i=1;i<=3;i++)
                {
                    float x = -.47f + (i-1)*.47f;
                    Box(map.transform,"Pier "+i,new Vector3(x,-.01f,-.045f),new Vector3(.19f,.38f,.025f),copper,false);
                    MakeButton(map.transform,button,task,i,new Vector3(x,.12f,-.08f),new Vector3(.37f,.23f,.04f),"0"+i+"\n"+(i==1?"FERRY":i==2?"CARGO":"REPAIR"));
                }
                MakeButton(root.transform,button,task,0,new Vector3(1.52f,1.57f,.37f),new Vector3(.43f,.22f,.055f),"RESTART");
                var ending = new GameObject("Journey ending"); ending.transform.SetParent(root.transform,false);
                task.ending = ending;
                Box(ending.transform,"Ending panel",new Vector3(0,1.72f,.28f),new Vector3(3.55f,.65f,.035f),navy,false);
                Text(ending.transform,"Story ending","A JOB BECOMES A PLACE IN THE CITY\nLiang's first wages paid for a bed near Dock Street. Cargo work tied\nhis daily walk, shared lodging and letters home to the harbour.\nMigration changed the city through everyday lives like his.",new Vector3(-1.65f,1.99f,.245f),new Vector2(3.3f,.56f),.069f,ColorOf(white));
                Text(root.transform,"Fiction notice","INTERPRETIVE RECONSTRUCTION / All people, documents and places are fictional.",new Vector3(-1.75f,.70f,-.31f),new Vector2(3.5f,.18f),.044f,ColorOf(paper));
                task.ResetJourney();
                PrefabUtility.SaveAsPrefabAsset(root, Prefab);
                Object.DestroyImmediate(root); root = null;
                FirstDayPlacement.CompactPrefab();
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab),scene);
                PositionAtArrival(instance,scene);
                EditorSceneManager.SaveScene(scene,Preview);
                AssetDatabase.SaveAssets();
                ValidateScene(scene);
                File.WriteAllText(Folder+"/Validation/BUILD_COMPLETE.txt",DateTime.UtcNow.ToString("O")+"\n"+Prefab+"\n"+Preview);
                Debug.Log("ZZX First Day: prefab and copied museum generated. Original museum untouched.");
            }
            finally
            {
                if (root != null) Object.DestroyImmediate(root);
                if (originalActive.IsValid()) SceneManager.SetActiveScene(originalActive);
                if (scene.IsValid()) EditorSceneManager.CloseScene(scene,true);
            }
        }

        private static Material Mat(string name,Color colour)
        {
            var mat = new Material(Shader.Find("Standard")) { name=name, color=colour };
            AssetDatabase.CreateAsset(mat,Folder+"/Materials/"+name+".mat"); return mat;
        }
        private static Color ColorOf(Material m) { return m.color; }
        private static GameObject Box(Transform parent,string name,Vector3 position,Vector3 scale,Material material,bool collider=true)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube); go.name=name;go.transform.SetParent(parent,false);
            go.transform.localPosition=position;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=material;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>()); return go;
        }
        private static TextMeshPro Text(Transform parent,string name,string text,Vector3 position,Vector2 size,float metres,Color colour)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=position;
            var tmp=go.AddComponent<TextMeshPro>();tmp.font=font;tmp.text=text;tmp.fontSize=metres*10;
            tmp.color=colour;tmp.alignment=TextAlignmentOptions.TopLeft;tmp.enableWordWrapping=true;
            tmp.rectTransform.sizeDelta=size;tmp.rectTransform.pivot=new Vector2(0,1);
            tmp.overflowMode=TextOverflowModes.Overflow;tmp.richText=false;return tmp;
        }
        private static T Copy<T>(T source,GameObject target) where T:Component
        {
            ComponentUtility.CopyComponent(source);
            if (!ComponentUtility.PasteComponentAsNew(target)) throw new InvalidOperationException("Cannot copy "+typeof(T).Name);
            var component=target.GetComponent<T>();
            if (component is XRBaseInteractable item)
            {
                // Preserve interaction tuning; references must resolve within this prefab / destination scene.
                item.interactionManager=null;
                item.colliders.Clear(); item.colliders.Add(target.GetComponent<Collider>());
            }
            return component;
        }
        private static void Document(Transform parent,XRGrabInteractable source,string name,Vector3 position,Vector2 size,string title,string body,string hint)
        {
            var pivot=new GameObject(name);pivot.transform.SetParent(parent,false);pivot.transform.localPosition=position;
            pivot.transform.localRotation=Quaternion.Euler(62,0,0);
            Box(pivot.transform,"Archive card",Vector3.zero,new Vector3(size.x,size.y,.018f),paper,false);
            var collider=pivot.AddComponent<BoxCollider>();collider.size=new Vector3(size.x,size.y,.025f);
            Copy(source.GetComponent<Rigidbody>(),pivot).isKinematic=true;
            // These documents return to the display instead of being thrown.
            Copy(source,pivot).throwOnDetach=false;
            Text(pivot.transform,"Heading",title,new Vector3(-size.x/2+.025f,size.y/2-.03f,-.015f),new Vector2(size.x-.05f,.08f),.036f,ColorOf(ink));
            Text(pivot.transform,"Document text",body,new Vector3(-size.x/2+.025f,size.y/2-.13f,-.015f),new Vector2(size.x-.05f,size.y-.15f),.036f,ColorOf(ink));
            var hintObject=new GameObject("Inspection hint");hintObject.transform.SetParent(parent,false);
            Text(hintObject.transform,"Hint",hint,new Vector3(position.x-.3f,1.35f,.22f),new Vector2(.66f,.22f),.046f,ColorOf(white));
            // Another sample assembly declares the same class name; select the project's assembly explicitly.
            var showType=typeof(FirstDayJourney).Assembly.GetType("ShowObjectNameOnGrab",true);
            var show=pivot.AddComponent(showType);
            var serialized=new SerializedObject(show);serialized.FindProperty("targetObject").objectReferenceValue=hintObject;serialized.ApplyModifiedPropertiesWithoutUndo();
            hintObject.SetActive(false);
            pivot.AddComponent<ZzxWorkspace.ReturnToOriginalWhenNotGrabbed>();
        }
        private static void MakeButton(Transform parent,XRSimpleInteractable source,FirstDayJourney task,int berth,Vector3 position,Vector3 size,string label)
        {
            var go=new GameObject(berth==0?"Restart button":"Berth "+berth);go.transform.SetParent(parent,false);go.transform.localPosition=position;
            var collider=go.AddComponent<BoxCollider>();collider.size=size;
            Box(go.transform,"Button face",Vector3.zero,size,navy,false);
            Copy(source,go);
            var action=go.AddComponent<FirstDayMapButton>();action.journey=task;action.berth=berth;
            Text(go.transform,"Label",label,new Vector3(-size.x/2+.02f,size.y/2-.02f,-size.z/2-.008f),new Vector2(size.x-.04f,size.y-.04f),berth==0?.036f:.044f,ColorOf(white));
        }
        private static void PositionAtArrival(GameObject exhibit,Scene scene)
        {
            FirstDayPlacement.SidePosition(exhibit);
        }

        [MenuItem("Harbour City Museum/ZZX/First Day/2 - Validate saved exhibit %#F10")]
        public static void Validate()
        {
            var scene=EditorSceneManager.OpenScene(Preview,OpenSceneMode.Additive);
            try { ValidateScene(scene); } finally { EditorSceneManager.CloseScene(scene,true); }
        }
        private static void ValidateScene(Scene scene)
        {
            var all=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<Transform>(true)).ToArray();
            var instance=all.Single(x=>x.GetComponent<FirstDayJourney>()!=null).gameObject;
            var issues=new List<string>();
            var inheritedNotes=new List<string>();
            int cameras=all.Count(x=>x.gameObject.activeInHierarchy && x.GetComponent<Camera>()!=null && x.GetComponent<Camera>().enabled);
            int managers=all.Count(x=>x.gameObject.activeInHierarchy && x.GetComponent<XRInteractionManager>()!=null && x.GetComponent<XRInteractionManager>().enabled);
            if(cameras!=1) issues.Add("Active camera count = "+cameras);
            if(managers!=1) issues.Add("Active XR interaction manager count = "+managers);
            if(instance.GetComponentsInChildren<Camera>(true).Length!=0) issues.Add("Exhibit contains a camera.");
            if(instance.GetComponentsInChildren<FirstDayMapButton>(true).Length!=4) issues.Add("Expected three choices and restart.");
            if(instance.GetComponentsInChildren<XRGrabInteractable>(true).Length!=2) issues.Add("Expected two grab documents.");
            foreach(var t in all) if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0)
            {
                string path=t.name;for(var p=t.parent;p!=null;p=p.parent)path=p.name+"/"+path;
                if(t.gameObject.activeInHierarchy)issues.Add("Active missing script: "+path);
                else inheritedNotes.Add("Inherited inactive missing script, unchanged: "+path);
            }
            var task=instance.GetComponent<FirstDayJourney>();
            if(task.feedback==null || task.progress==null || task.ending==null) issues.Add("Unbound task references.");
            foreach(var action in instance.GetComponentsInChildren<FirstDayMapButton>(true)) if(action.journey!=task) issues.Add("Unbound button: "+action.name);
            string report="UTC: "+DateTime.UtcNow.ToString("O")+"\nActive cameras: "+cameras+"\nActive XR managers: "+managers+"\n"+
                "Position: "+instance.transform.position+"\n"+(issues.Count==0?"PASS: serialized integration checks\n":string.Join("\n",issues))+"\n"+string.Join("\n",inheritedNotes)+"\nHeadset and executable testing: not performed by this check.\n";
            File.WriteAllText(Folder+"/Validation/IntegrationReport.txt",report);
            if(issues.Count>0) throw new InvalidOperationException(report);
            Debug.Log(report);
        }
    }
}
