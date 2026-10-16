using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit;
using Unity.XR.CoreUtils;
using Wjy.Museum;

public static class WjyMuseumExhibitBuilder
{
    const string Root="Assets/wjy_workspace/";
    const string Models=Root+"Models/TraditionalFishingRod/";
    static Material[] materials;

    [MenuItem("WJY/Museum/Rebuild traditional fishing rod")]
    public static void Build()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play mode first.");
        Directory.CreateDirectory(Root+".work");Directory.CreateDirectory(Models+"Materials");AssetDatabase.Refresh();
        var scene=EditorSceneManager.OpenScene(Root+"Museum.unity");
        var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
        var template=all.Single(t=>t.name=="socle.005_socle_0");
        var pedestalMesh=template.GetComponent<MeshFilter>().sharedMesh;
        var pedestalRotation=template.rotation;var pedestalScale=template.lossyScale;
        var rig=all.Single(t=>t.name=="XR Origin (XR Rig)" && t.parent==null).GetComponent<XROrigin>();
        var manager=all.Select(t=>t.GetComponent<XRInteractionManager>()).First(x=>x && x.gameObject.activeInHierarchy);
        var old=GameObject.Find("WJY_FishingRodStation");if(old)UnityEngine.Object.DestroyImmediate(old);
        materials=new[]{Mat("AgedBamboo",new Color(.55f,.32f,.105f),0,.25f),Mat("DarkBindings",new Color(.12f,.065f,.025f),0,.15f),Mat("OldBrass",new Color(.49f,.34f,.14f),.65f,.5f),Mat("CorkGrip",new Color(.34f,.20f,.08f),0,.12f),Mat("WoundLine",new Color(.86f,.79f,.57f),0,.25f),Mat("HookSteel",new Color(.45f,.48f,.49f),.85f,.7f)};
        var rodMesh=SaveMesh(MakeRod(),"TraditionalBambooRod.asset");var hookMesh=SaveMesh(MakeHook(),"FishingHook.asset");
        var station=new GameObject("WJY_FishingRodStation");station.transform.position=new Vector3(3.2f,.1427f,-.75f);
        var pedestal=MeshObject("WhitePedestal",station.transform,pedestalMesh,new[]{Mat("PedestalWhite",new Color(.94f,.94f,.92f),0,.18f)});
        pedestal.transform.rotation=pedestalRotation;pedestal.transform.localScale=pedestalScale;
        var rod=new GameObject("TraditionalBambooFishingRod");rod.transform.SetParent(station.transform,false);rod.transform.localPosition=new Vector3(0,.8573f,0);
        MeshObject("BambooRod_Reel_Guides",rod.transform,rodMesh,materials);
        var anchor=Child("GripAnchor",rod.transform,new Vector3(0,.16f,-.035f));anchor.localRotation=Quaternion.Euler(-65,0,0);
        var tip=Child("RodTip",rod.transform,new Vector3(0,2.1f,-.028f));
        var guides=new List<Transform>();
        foreach(var p in new[]{new Vector3(0,.37f,-.107f),new Vector3(0,.5f,-.036f),new Vector3(0,.85f,-.034f),new Vector3(0,1.2f,-.032f),new Vector3(0,1.55f,-.03f),new Vector3(0,1.85f,-.028f)})guides.Add(Child("LineGuide_"+guides.Count,rod.transform,p));
        var hook=MeshObject("FishingHook",rod.transform,hookMesh,materials).transform;hook.position=tip.position+Vector3.down*1.05f+Vector3.right*.12f;
        var line=Child("FishingLine",rod.transform,Vector3.zero).gameObject.AddComponent<LineRenderer>();
        line.useWorldSpace=true;line.widthMultiplier=.0035f;line.numCapVertices=3;line.numCornerVertices=3;
        line.sharedMaterial=Unlit("FishingLine",new Color(.87f,.85f,.69f));line.shadowCastingMode=ShadowCastingMode.Off;
        var positions=guides.Select(t=>t.position).Concat(new[]{tip.position,hook.position}).ToArray();line.positionCount=positions.Length;line.SetPositions(positions);
        var body=rod.AddComponent<Rigidbody>();body.isKinematic=true;body.useGravity=false;
        var collider=rod.AddComponent<BoxCollider>();collider.center=new Vector3(0,1.03f,-.035f);collider.size=new Vector3(.12f,2.15f,.19f);
        var grab=rod.AddComponent<WjyFishingRodGrab>();grab.interactionManager=manager;grab.attachTransform=anchor;grab.useDynamicAttach=false;grab.throwOnDetach=false;
        grab.movementType=XRBaseInteractable.MovementType.Kinematic;grab.attachEaseInTime=.18f;grab.selectMode=InteractableSelectMode.Single;grab.colliders.Clear();grab.colliders.Add(collider);
        var water=MakeWater(station.transform);
        var label=Child("FishingInstructions",station.transform,new Vector3(0,.33f,.365f));label.localRotation=Quaternion.Euler(0,180,0);
        var text=label.gameObject.AddComponent<TextMesh>();text.text="GRIP: pick up\nTRIGGER: hold, swing, release";text.characterSize=.009f;text.fontSize=60;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.color=new Color(.14f,.12f,.08f);
        var interaction=rod.AddComponent<WjyFishingRodInteraction>();var serialized=new SerializedObject(interaction);
        Set(serialized,"sharedRig",rig);Set(serialized,"rodTip",tip);Set(serialized,"hook",hook);Set(serialized,"fishingLine",line);Set(serialized,"waterSurface",water);Set(serialized,"instructions",text);
        var array=serialized.FindProperty("lineGuides");array.arraySize=guides.Count;for(int i=0;i<guides.Count;i++)array.GetArrayElementAtIndex(i).objectReferenceValue=guides[i];serialized.ApplyModifiedPropertiesWithoutUndo();
        var lamp=Child("FishingRodDisplayLight",station.transform,new Vector3(0,3.5f,2.25f));lamp.LookAt(tip.position-Vector3.up);
        var light=lamp.gameObject.AddComponent<Light>();light.type=LightType.Spot;light.spotAngle=85;light.range=8;light.intensity=3;light.color=new Color(1,.96f,.87f);light.shadows=LightShadows.Soft;
        PrefabUtility.SaveAsPrefabAsset(station,Root+"Prefabs/TraditionalFishingRodExhibit.prefab");
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Directory.CreateDirectory(Root+"Documentation");
        Render(Root+"Documentation/FishingRodPreview.png",new Vector3(4.15f,2.2f,2.35f),new Vector3(3.2f,1.7f,-.9f));
        Render(Root+"Documentation/FishingRodWaterPreview.png",new Vector3(5.7f,3.4f,1.6f),new Vector3(3.2f,1.1f,-2.1f));
        Debug.Log("[WJY] Traditional bamboo rod, hook, line and Grip/Trigger interaction saved.");
    }
    static void Set(SerializedObject s,string name,UnityEngine.Object value)=>s.FindProperty(name).objectReferenceValue=value;
    static Transform Child(string name,Transform parent,Vector3 position){var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=position;return t;}
    static GameObject MeshObject(string name,Transform parent,Mesh mesh,Material[] mats){var go=Child(name,parent,Vector3.zero).gameObject;go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterials=mats;return go;}
    static GameObject Cube(string name,Transform parent,Vector3 position,Vector3 scale,Material mat){var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=mat;return go;}
    static BoxCollider MakeWater(Transform parent)
    {
        var wood=Mat("BasinWood",new Color(.19f,.12f,.065f),0,.2f);var c=new Vector3(0,.13f,-2.6f);
        Cube("CastingBasin_Base",parent,c,new Vector3(2.4f,.14f,1.6f),wood);
        Cube("CastingBasin_Back",parent,c+new Vector3(0,.15f,-.8f),new Vector3(2.5f,.3f,.09f),wood);
        Cube("CastingBasin_Front",parent,c+new Vector3(0,.15f,.8f),new Vector3(2.5f,.3f,.09f),wood);
        Cube("CastingBasin_Left",parent,c+new Vector3(-1.2f,.15f,0),new Vector3(.09f,.3f,1.6f),wood);
        Cube("CastingBasin_Right",parent,c+new Vector3(1.2f,.15f,0),new Vector3(.09f,.3f,1.6f),wood);
        var water=Cube("CastingWater",parent,c+new Vector3(0,.23f,0),new Vector3(2.3f,.02f,1.5f),Mat("Water",new Color(.035f,.32f,.43f),.15f,.9f));var col=water.GetComponent<BoxCollider>();col.isTrigger=true;return col;
    }
    static Mesh MakeRod()
    {
        var b=new Geometry();b.Tube(new[]{Vector3.zero,new Vector3(0,.28f,0)},.025f,.022f,3);
        b.Tube(new[]{new Vector3(0,.25f,0),new Vector3(.002f,1.2f,0),new Vector3(0,2.1f,0)},.015f,.003f,0);
        foreach(float y in new[]{.28f,.57f,.88f,1.2f,1.53f,1.83f}){float r=Mathf.Lerp(.015f,.003f,y/2.1f);b.Tube(new[]{new Vector3(0,y-.012f,0),new Vector3(0,y+.012f,0)},r+.003f,r+.003f,0);b.Tube(new[]{new Vector3(0,y+.014f,0),new Vector3(0,y+.032f,0)},r+.0015f,r+.0015f,1);}
        foreach(float y in new[]{.5f,.85f,1.2f,1.55f,1.85f,2.1f}){float z=-Mathf.Lerp(.036f,.028f,y/2.1f);b.Tube(new[]{new Vector3(0,y,0),new Vector3(0,y,z+.008f)},.0025f,.0025f,2);b.Tube(Circle(new Vector3(0,y,z),Vector3.up,.011f),.0022f,.0022f,2);}
        Vector3 reel=new Vector3(0,.33f,-.075f);b.Tube(new[]{new Vector3(0,.31f,0),reel},.008f,.008f,2);b.Tube(new[]{reel+Vector3.left*.04f,reel+Vector3.right*.04f},.044f,.044f,1);
        foreach(float x in new[]{-.047f,.047f}){b.Tube(Circle(reel+Vector3.right*x,Vector3.right,.065f),.006f,.006f,2);for(int i=0;i<6;i++){float a=i*Mathf.PI/3;b.Tube(new[]{reel+Vector3.right*x,reel+new Vector3(x,Mathf.Cos(a)*.061f,Mathf.Sin(a)*.061f)},.003f,.003f,2);}}
        for(int i=0;i<12;i++)b.Tube(Circle(reel+Vector3.right*(-.032f+i*.0057f),Vector3.right,.047f),.0027f,.0027f,4);
        b.Tube(new[]{reel+Vector3.right*.05f,reel+new Vector3(.078f,.045f,0),reel+new Vector3(.105f,.045f,0)},.005f,.005f,2);b.Tube(new[]{reel+new Vector3(.10f,.045f,0),reel+new Vector3(.13f,.045f,0)},.011f,.011f,3);b.Tube(new[]{new Vector3(0,-.006f,0),new Vector3(0,.015f,0)},.026f,.026f,2);
        return b.Mesh("Traditional bamboo rod with brass reel");
    }
    static Mesh MakeHook()
    {
        var b=new Geometry();b.Tube(Circle(Vector3.zero,Vector3.forward,.009f),.0025f,.0025f,5);var points=new List<Vector3>{new Vector3(0,-.009f,0),new Vector3(0,-.085f,0)};
        for(int i=0;i<=16;i++){float a=Mathf.PI+i*Mathf.PI/16;points.Add(new Vector3(.027f+Mathf.Cos(a)*.027f,-.085f+Mathf.Sin(a)*.035f,0));}points.Add(new Vector3(.05f,-.055f,0));b.Tube(points,.0035f,.001f,5);b.Tube(new[]{new Vector3(.05f,-.055f,0),new Vector3(.043f,-.07f,0)},.0008f,.002f,5);return b.Mesh("Forged J hook and eye");
    }
    static List<Vector3> Circle(Vector3 center,Vector3 axis,float radius)
    {
        var u=Vector3.Cross(axis,Mathf.Abs(Vector3.Dot(axis,Vector3.up))>.9f?Vector3.forward:Vector3.up).normalized;var v=Vector3.Cross(axis,u).normalized;var points=new List<Vector3>();for(int i=0;i<=32;i++){float a=i*Mathf.PI*2/32;points.Add(center+radius*(Mathf.Cos(a)*u+Mathf.Sin(a)*v));}return points;
    }
    sealed class Geometry
    {
        readonly List<Vector3> vertices=new List<Vector3>();readonly List<Vector2> uv=new List<Vector2>();readonly List<int>[] triangles=Enumerable.Range(0,6).Select(_=>new List<int>()).ToArray();
        public void Tube(IList<Vector3> path,float startRadius,float endRadius,int mat)
        {
            const int sides=10;int offset=vertices.Count;
            for(int j=0;j<path.Count;j++)
            {
                Vector3 axis=(path[Mathf.Min(j+1,path.Count-1)]-path[Mathf.Max(j-1,0)]).normalized;var u=Vector3.Cross(axis,Mathf.Abs(Vector3.Dot(axis,Vector3.up))>.9f?Vector3.forward:Vector3.up).normalized;var v=Vector3.Cross(axis,u).normalized;
                for(int i=0;i<sides;i++){float a=i*Mathf.PI*2/sides;float radius=Mathf.Lerp(startRadius,endRadius,j/(float)(path.Count-1));vertices.Add(path[j]+radius*(u*Mathf.Cos(a)+v*Mathf.Sin(a)));uv.Add(new Vector2(i/(float)sides,j/(float)(path.Count-1)));if(j==0)continue;int k=offset+j*sides+i;int next=offset+j*sides+(i+1)%sides;triangles[mat].AddRange(new[]{k,k-sides,next,next,k-sides,next-sides});}
            }
            for(int i=1;i<sides-1;i++){triangles[mat].AddRange(new[]{offset,offset+i+1,offset+i});int last=offset+(path.Count-1)*sides;triangles[mat].AddRange(new[]{last,last+i,last+i+1});}
        }
        public Mesh Mesh(string name){var mesh=new Mesh{name=name,indexFormat=IndexFormat.UInt32,subMeshCount=6};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);for(int i=0;i<6;i++)mesh.SetTriangles(triangles[i],i);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;}
    }
    static Mesh SaveMesh(Mesh mesh,string filename){var existing=AssetDatabase.LoadAssetAtPath<Mesh>(Models+filename);if(existing){EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);return existing;}AssetDatabase.CreateAsset(mesh,Models+filename);return mesh;}
    static Material Mat(string name,Color color,float metallic,float gloss){string path=Models+"Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Standard")){name=name};AssetDatabase.CreateAsset(m,path);}m.color=color;m.SetFloat("_Metallic",metallic);m.SetFloat("_Glossiness",gloss);EditorUtility.SetDirty(m);return m;}
    static Material Unlit(string name,Color color){string path=Models+"Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Unlit/Color")){name=name};AssetDatabase.CreateAsset(m,path);}m.color=color;EditorUtility.SetDirty(m);return m;}
    public static void Render(string path,Vector3 position,Vector3 target)
    {
        var go=new GameObject("Temporary WJY preview");var camera=go.AddComponent<Camera>();camera.transform.position=position;camera.transform.LookAt(target);camera.fieldOfView=65;camera.nearClipPlane=.03f;var rt=new RenderTexture(1600,1100,24);camera.targetTexture=rt;camera.Render();var previous=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());RenderTexture.active=previous;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);
    }
}
