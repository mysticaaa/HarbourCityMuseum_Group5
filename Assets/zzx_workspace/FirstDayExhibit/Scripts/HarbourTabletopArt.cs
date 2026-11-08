using System.Collections.Generic;
using UnityEngine;

namespace ZzxWorkspace.FirstDay
{
    // Decorative geometry only. No inputs, colliders, animation paths or sign transforms.
    public sealed partial class HarbourTabletopPrototype
    {
        private readonly List<Mesh> artMeshes=new List<Mesh>();
        private readonly List<Texture2D> artTextures=new List<Texture2D>();

        private Material ArtMaterial(Color color,float smoothness=.1f)
        {
            var mat=Mat(color);
            if(mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness",smoothness);
            if(mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness",smoothness);
            return mat;
        }
        private void BuildHarbourArt()
        {
            var timber=ArtMaterial(new Color(.55f,.39f,.24f));
            var paleWood=ArtMaterial(new Color(.68f,.51f,.33f));
            var steel=ArtMaterial(new Color(.27f,.29f,.30f));
            var slate=ArtMaterial(new Color(.26f,.31f,.35f));
            var masonry=ArtMaterial(new Color(.66f,.63f,.55f));
            var glass=ArtMaterial(new Color(.12f,.22f,.26f),.35f);
            var red=ArtMaterial(new Color(.38f,.15f,.12f));
            var green=ArtMaterial(new Color(.18f,.34f,.28f));
            var sand=ArtMaterial(new Color(.72f,.66f,.55f));
            AddWoodGrain(timber); AddWoodGrain(paleWood);
            // Shared prototype materials remain in place so selected-state feedback still works.
            AddWoodGrain(wood);
            sea.color=new Color(.18f,.36f,.45f);
            if(sea.HasProperty("_Smoothness")) sea.SetFloat("_Smoothness",.32f);

            Box("Diorama rear quay",new Vector3(0,.95f,.325f),new Vector3(1.84f,.045f,.13f),sand);
            for(int side=-1;side<=1;side+=2)
                Box("Tray side rim",new Vector3(side*.935f,.945f,.12f),new Vector3(.016f,.055f,.53f),steel);
            Box("Tray front rim",new Vector3(0,.93f,-.146f),new Vector3(1.88f,.025f,.015f),steel);
            for(int i=0;i<3;i++)
            {
                float x=(i-1)*.60f;
                for(int n=0;n<9;n++)
                    Box("Pier decking plank",new Vector3(x,.976f,.12f+n*.026f),new Vector3(.18f,.009f,.023f),n%2==0?timber:paleWood);
                for(int n=0;n<3;n++)
                    for(int side=-1;side<=1;side+=2)
                    {
                        Box("Pier support",new Vector3(x+side*.075f,.925f,.14f+n*.085f),new Vector3(.023f,.105f,.023f),timber);
                        Cylinder("Mooring bollard",new Vector3(x+side*.075f,.998f,.14f+n*.085f),new Vector3(.022f,.021f,.022f),steel);
                    }
            }
            // Reference shoreline: a lengthwise passenger pier, a cargo basin,
            // and a raised slipway. These are decorative overlays only.
            Box("Ferry cross pier",new Vector3(-.60f,.965f,.15f),new Vector3(.16f,.025f,.34f),timber);
            for(int i=0;i<12;i++)
                Box("Ferry cross pier board",new Vector3(-.60f,.981f,-.005f+i*.028f),new Vector3(.16f,.008f,.025f),paleWood);
            for(int side=-1;side<=1;side+=2)
                Box("Cargo basin quay",new Vector3(side*.28f,.958f,.13f),new Vector3(side==1?.15f:.09f,.055f,.38f),paleWood);
            Box("Repair slipway base",new Vector3(.60f,.945f,.105f),new Vector3(.45f,.026f,.38f),timber);
            for(int i=0;i<14;i++)
                Box("Slipway boards",new Vector3(.60f,.963f,-.07f+i*.027f),new Vector3(.43f,.008f,.024f),i%2==0?timber:paleWood);
            for(int side=-1;side<=1;side+=2)
                for(int n=0;n<14;n++)
                    Box("Cargo quay boards",new Vector3(side*.28f,.989f,-.045f+n*.027f),new Vector3(side==1?.15f:.09f,.009f,.024f),n%2==0?paleWood:timber);
            DressBoat(ferry,green,glass,slate,timber);
            DressBoat(cargoBoat,red,glass,slate,timber);
            DressBoat(repairBoat,red,glass,slate,timber);
            RotateBoatArt(ferry,90);
            RotateBoatArt(repairBoat,90);

            // Replace just the existing crane's surface material, preserving its transform.
            foreach(string name in new[]{"Crane left","Crane right","Crane beam"})
                model.Find(name).GetComponent<Renderer>().sharedMaterial=steel;
            for(int side=-1;side<=1;side+=2)
            {
                Box("Crane foundation",new Vector3(side*.22f,.99f,.22f),new Vector3(.075f,.035f,.08f),masonry);
                var brace=Box("Crane diagonal",new Vector3(side*.19f,1.07f,.22f),new Vector3(.018f,.17f,.025f),steel);
                brace.localRotation=Quaternion.Euler(0,0,side*23);
            }
            Box("Hook metal housing",Vector3.zero,new Vector3(1.15f,.9f,1.15f),steel,hook);
            // Crate is animated and scaled: decorations use normalized local dimensions.
            DressCrate(crate,paleWood);
            var stored=new List<Transform>();
            foreach(Transform child in model) if(child.name=="Stored cargo") stored.Add(child);
            foreach(var box in stored) { box.GetComponent<Renderer>().sharedMaterial=wood; DressCrate(box,paleWood); }

            // Compact sheds sit behind the boats, below the task UI.
            Building("Ferry ticket office",new Vector3(-.49f,.98f,.315f),new Vector3(.24f,.16f,.14f),masonry,slate,glass);
            Building("Cargo warehouse",new Vector3(.30f,.98f,.32f),new Vector3(.20f,.16f,.12f),masonry,slate,glass);
            Building("Harbour signal tower",new Vector3(-.83f,.98f,.32f),new Vector3(.09f,.24f,.09f),masonry,red,glass);
            Box("Warehouse chimney",new Vector3(.35f,1.18f,.35f),new Vector3(.022f,.14f,.025f),masonry);
            // Open-front repair shelter: boat and maintenance animation remain visible.
            for(int side=-1;side<=1;side+=2)
            {
                Box("Repair shed post",new Vector3(.60f+side*.22f,1.065f,.25f),new Vector3(.018f,.19f,.018f),timber);
                Box("Slipway rail",new Vector3(.60f+side*.08f,.966f,.10f),new Vector3(.012f,.012f,.30f),steel);
            }
            Roof("Repair shed roof",new Vector3(.60f,1.15f,.15f),new Vector3(.48f,.04f,.36f),slate);
            for(int side=-1;side<=1;side+=2)
            {
                Box("Repair front post",new Vector3(.60f+side*.22f,1.065f,-.02f),new Vector3(.018f,.19f,.018f),timber);
                Box("Repair side beam",new Vector3(.60f+side*.22f,1.14f,.15f),new Vector3(.022f,.025f,.36f),timber);
            }
            Box("Repair shed back beam",new Vector3(.60f,1.14f,.35f),new Vector3(.46f,.025f,.025f),timber);
            for(int i=0;i<2;i++) Cylinder("Harbour barrel",new Vector3(.87f,1.005f,.27f+i*.06f),new Vector3(.035f,.04f,.035f),timber);
            // Fine, fixed water marks: no additional simulation or animation workload.
            for(int i=0;i<12;i++)
                Box("Water ripple",new Vector3(-.84f+i*.15f,.934f,-.08f+(i%3)*.045f),new Vector3(.045f,.001f,.002f),slate);
        }
        private void RotateBoatArt(Transform anchor,float degrees)
        {
            // Rotate the visual children, preserving the animated anchor and saved home position.
            var children=new List<Transform>(); foreach(Transform child in anchor) children.Add(child);
            var visual=new GameObject("Reference boat orientation").transform; visual.SetParent(anchor,false);
            foreach(var child in children) child.SetParent(visual,false);
            visual.localRotation=Quaternion.Euler(0,degrees,0);
        }
        private void DressBoat(Transform boat,Material bottom,Material glass,Material roof,Material deck)
        {
            boat.Find("Hull").GetComponent<Renderer>().enabled=false;
            Hull("Shaped hull",boat,new Vector3(0,-.005f,0),new Vector3(.36f,.075f,.16f),bottom);
            Hull("Hull upper band",boat,new Vector3(0,.025f,0),new Vector3(.36f,.024f,.16f),white);
            Hull("Deck",boat,new Vector3(0,.040f,0),new Vector3(.32f,.009f,.135f),deck);
            Box("Cabin roof",new Vector3(-.09f,.103f,0),new Vector3(.125f,.012f,.135f),roof,boat);
            for(int i=0;i<3;i++)
                Box("Wheelhouse window",new Vector3(-.127f+i*.035f,.066f,-.062f),new Vector3(.023f,.029f,.006f),glass,boat);
            Box("Front window",new Vector3(-.032f,.066f,0),new Vector3(.006f,.028f,.075f),glass,boat);
            Box("Funnel",new Vector3(-.12f,.126f,.035f),new Vector3(.022f,.05f,.025f),roof,boat);
            Box("Funnel cap",new Vector3(-.12f,.151f,.035f),new Vector3(.028f,.01f,.030f),bottom,boat);
        }
        private void DressCrate(Transform box,Material trim)
        {
            for(int side=-1;side<=1;side+=2)
            {
                Box("Crate upright",new Vector3(side*.43f,0,-.515f),new Vector3(.09f,1.03f,.035f),trim,box);
                Box("Crate crossbar",new Vector3(0,side*.43f,-.515f),new Vector3(.94f,.09f,.035f),trim,box);
                var diagonal=Box("Crate diagonal",new Vector3(0,0,-.54f),new Vector3(.08f,1.12f,.035f),trim,box);
                diagonal.localRotation=Quaternion.Euler(0,0,side*42);
            }
        }
        private Transform Cylinder(string name,Vector3 p,Vector3 size,Material mat)
        {
            var obj=GameObject.CreatePrimitive(PrimitiveType.Cylinder); obj.name=name;
            obj.transform.SetParent(model,false); obj.transform.localPosition=p; obj.transform.localScale=size;
            obj.GetComponent<Collider>().enabled=false; obj.GetComponent<Renderer>().sharedMaterial=mat; return obj.transform;
        }
        private void Hull(string name,Transform parent,Vector3 p,Vector3 size,Material mat)
        {
            // Six-sided footprint with a pointed bow and tapered lower hull.
            Vector2[] ring={new Vector2(-.5f,-.38f),new Vector2(.27f,-.5f),new Vector2(.5f,0),new Vector2(.27f,.5f),new Vector2(-.5f,.38f),new Vector2(-.54f,0)};
            var vertices=new List<Vector3>(); var triangles=new List<int>();
            for(int level=0;level<2;level++) foreach(var v in ring)
                vertices.Add(new Vector3(v.x*size.x*(level==0?.83f:1), (level-.5f)*size.y,v.y*size.z*(level==0?.78f:1)));
            for(int i=0;i<6;i++)
            { int next=(i+1)%6; triangles.AddRange(new[]{i,next,i+6,next,next+6,i+6}); }
            for(int i=1;i<5;i++) { triangles.AddRange(new[]{6,6+i,7+i}); triangles.AddRange(new[]{0,i+1,i}); }
            ArtMesh(name,parent,p,vertices.ToArray(),triangles.ToArray(),mat);
        }
        private void Roof(string name,Vector3 p,Vector3 size,Material mat)
        {
            float x=size.x/2,z=size.z/2,h=size.y;
            var vertices=new[]{new Vector3(-x,0,-z),new Vector3(0,h,-z),new Vector3(x,0,-z),new Vector3(-x,0,z),new Vector3(0,h,z),new Vector3(x,0,z)};
            ArtMesh(name,model,p,vertices,new[]{0,1,3,1,4,3,1,2,4,2,5,4,0,2,1,3,4,5,0,3,2,2,3,5},mat);
        }
        private void Building(string name,Vector3 basePoint,Vector3 size,Material wall,Material roof,Material window)
        {
            Box(name,basePoint+Vector3.up*size.y/2,size,wall);
            Roof(name+" roof",basePoint+Vector3.up*size.y,new Vector3(size.x*1.1f,.045f,size.z*1.15f),roof);
            Box(name+" doorway",basePoint+new Vector3(0,.045f,-size.z/2-.002f),new Vector3(.045f,.09f,.004f),window);
        }
        private void ArtMesh(string name,Transform parent,Vector3 p,Vector3[] vertices,int[] triangles,Material mat)
        {
            for(int i=0;i<triangles.Length;i+=3) { int a=triangles[i]; triangles[i]=triangles[i+1]; triangles[i+1]=a; }
            var mesh=new Mesh {name=name,vertices=vertices,triangles=triangles}; mesh.RecalculateNormals(); mesh.RecalculateBounds(); artMeshes.Add(mesh);
            var obj=new GameObject(name); obj.transform.SetParent(parent,false); obj.transform.localPosition=p;
            obj.AddComponent<MeshFilter>().sharedMesh=mesh; obj.AddComponent<MeshRenderer>().sharedMaterial=mat;
        }
        private void AddWoodGrain(Material material)
        {
            var texture=new Texture2D(64,64,TextureFormat.RGB24,false) {name="Subtle wood grain",wrapMode=TextureWrapMode.Repeat};
            var pixels=new Color[4096];
            for(int y=0;y<64;y++) for(int x=0;x<64;x++)
            {
                float grain=.90f+.07f*Mathf.Sin(y*1.8f+Mathf.Sin(x*.15f))+.025f*Mathf.Sin(x*12.3f+y*7.7f);
                pixels[y*64+x]=new Color(grain,grain,grain);
            }
            texture.SetPixels(pixels); texture.Apply(); artTextures.Add(texture); material.mainTexture=texture;
        }
    }
}
