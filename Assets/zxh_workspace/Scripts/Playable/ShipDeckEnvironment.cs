using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit;

namespace ZxhMuseum
{
    // Original exhibit geometry is baked into a full-size, upright world-space vessel.
    public sealed class ShipDeckEnvironment : MonoBehaviour
    {
        public Vector3 SpawnPoint => new Vector3(1.25f, .04f, -10);
        public bool UsesExhibitionGeometry { get; private set; }
        public float PortBoomAngle { get; private set; }
        public float StarboardBoomAngle { get; private set; }
        public bool CabinDoorOpen { get; private set; }
        public bool HasOriginalHatch { get; private set; }
        public bool HasOriginalWinch { get; private set; }
        public bool HatchOpen => hatchOpen;
        public float WinchAngle => winch.localEulerAngles.x;
        private readonly List<Mesh> meshes = new List<Mesh>();
        private readonly List<Material> materials = new List<Material>();
        private sealed class SupportRope { public Mesh mesh; public Vector3[] home; public MeshCollider collider; public bool port; }
        private readonly List<SupportRope> ropes = new List<SupportRope>();
        private float lastPort = float.NaN, lastStarboard = float.NaN;
        private const float OutriggerCut = 3.4f; // Outside the original wheelhouse, not through its walls.
        private Transform portBoom, starboardBoom, door, hatch, winch;
        private WorldPanel controls;
        private CabinEnvironment cabin;
        private bool hatchOpen;
        private float portTarget, starboardTarget;
        private Vector3 doorHome;
        private ReturnableTool binoculars;
        private Material Material(Color color)
        {
            var mat = new Material(Shader.Find("Standard")) { color=color };
            materials.Add(mat); return mat;
        }
        private GameObject Box(string name, Vector3 position, Vector3 size, Color color, bool visible=true)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;
            go.transform.SetParent(transform,false);go.transform.localPosition=position;go.transform.localScale=size;
            go.GetComponent<Renderer>().sharedMaterial=Material(color);go.GetComponent<Renderer>().enabled=visible;
            if(!visible)go.layer=Physics.IgnoreRaycastLayer;
            return go;
        }
        private void Select(GameObject go, Action action)
        {
            BoatTriggerAction.Bind(go, action);
            go.AddComponent<DesktopObjectClick>().Selected=action;
        }
        public void Initialize(CabinEnvironment environment)
        {
            cabin=environment;
            var source=MuseumSession.Instance != null ? MuseumSession.Instance.ShipSource : null;
            if(source==null)throw new InvalidOperationException("Exhibition fishing boat reference is required.");
            BuildOriginalVessel(source);
            // Walkable overlays follow the imported fishing boat, not a second rectangular ship.
            var floor=Box("Aft working deck walkable surface",new Vector3(0,-.06f,-7),new Vector3(4.6f,.12f,12),Color.gray,false);
            floor.layer=0; // The invisible walk surface remains a deliberate teleport target.
            floor.AddComponent<TeleportationArea>();
            BuildRamp();
            for(int side=-1;side<=1;side+=2)
                Box("Deck edge collision "+side,new Vector3(side*2.5f,.7f,-7),new Vector3(.12f,1.4f,12),Color.gray,false);
            Box("Stern edge collision",new Vector3(0,.7f,-13),new Vector3(5,1.4f,.12f),Color.gray,false);
            door=Box("Sliding wheelhouse door",new Vector3(1.2f,3.15f,1.65f),new Vector3(1.1f,2.1f,.08f),new Color(.28f,.29f,.25f)).transform;
            doorHome=door.localPosition;Select(door.gameObject,ToggleDoor);
            var doorway=WorldPanel.Create(transform,"Wheelhouse entrance",new Vector3(-.7f,3.1f,1.3f),Quaternion.identity,600);
            doorway.Status.text="Open the door and walk inside.\nMouse: WASD / right drag. Quest: left stick / teleport.\nDoor and ramp are access additions to the original model.";
            doorway.Button("Open / close door",ToggleDoor);
            doorway.Button("Enter cabin (accessible shortcut)",()=>Travel(false));
            controls=WorldPanel.Create(transform,"Rigging",new Vector3(-1.8f,1.5f,-8.6f),Quaternion.Euler(0,270,0),600);
            controls.Button("Raise",()=>MoveRigging(true));
            controls.Button("Stop",StopRigging);
            controls.Button("Lower",()=>MoveRigging(false));
            var hatchControls=WorldPanel.Create(transform,"Deck hatch",new Vector3(1.8f,1.5f,-6.8f),Quaternion.Euler(0,90,0),600);
            hatchControls.Status.text="Point the ray and press the trigger.";
            hatchControls.Button("Raise hatch",()=>SetHatch(true));
            hatchControls.Button("Lower hatch",()=>SetHatch(false));
            var deckMenu=WorldPanel.Create(transform,"Deck",new Vector3(1.8f,1.5f,-10.2f),Quaternion.Euler(0,90,0),600);
            deckMenu.Status.text="Grip to hold tools. Trigger to operate controls.";
            deckMenu.Button("Reset",ResetDeck);
            deckMenu.Button("Return to Museum",()=>MuseumSession.Instance?.RequestReturn());
            BuildBinoculars();
            var sea=Box("Ocean horizon",new Vector3(0,-3,0),new Vector3(800,.1f,800),new Color(.10f,.35f,.48f));sea.GetComponent<Collider>().enabled=false;
            for(int i=0;i<32;i++){
                var wave=Box("Distant sea crest "+i,new Vector3(-48+i*3,-2.92f,18+(i%5)*4),new Vector3(2,.025f,.12f),new Color(.39f,.65f,.7f));wave.GetComponent<Collider>().enabled=false;
            }
            var sun=new GameObject("Ship daylight").AddComponent<Light>();sun.transform.SetParent(transform,false);
            sun.type=LightType.Directional;sun.intensity=.9f;sun.transform.rotation=Quaternion.Euler(35,-35,0);sun.shadows=LightShadows.None;
            var fill=new GameObject("Deck fill light").AddComponent<Light>();fill.transform.SetParent(transform,false);
            fill.transform.localPosition=new Vector3(0,5,-6);fill.type=LightType.Point;fill.range=30;fill.intensity=1.5f;fill.shadows=LightShadows.None;
        }
        public void ToggleDoor()
        {
            // Do not shut the doorway around a visitor.
            var camera=Camera.main;
            if(CabinDoorOpen && camera!=null && Mathf.Abs(camera.transform.position.z-1.65f)<.8f && Mathf.Abs(camera.transform.position.x-1.2f)<1)return;
            CabinDoorOpen=!CabinDoorOpen;
        }
        public void SetBoom(bool port,float angle)
        {
            if(port)portTarget=Mathf.Clamp(angle,-10,15);else starboardTarget=Mathf.Clamp(angle,-10,15);
        }
        public void ToggleHatch() => hatchOpen = !hatchOpen;
        public void SetHatch(bool raised) => hatchOpen = raised;
        public void MoveRigging(bool raise) { SetBoom(true,raise?15:-10); SetBoom(false,raise?15:-10); }
        public void StopRigging() { portTarget=PortBoomAngle; starboardTarget=StarboardBoomAngle; }
        public void TurnWinch() => winch.Rotate(30,0,0);
        private void BuildRamp()
        {
            // 0.26m steps with 0.18m risers; original aft deck remains level.
            for(int i=0;i<12;i++)
                Box("Wheelhouse access step "+i,new Vector3(1.2f,(i+1)*.0875f,-1.4f+i*.26f),new Vector3(1.1f,(i+1)*.175f,.27f),new Color(.30f,.32f,.29f));
            for(int side=-1;side<=1;side+=2)
            {
                var rail = Box("Access stair handrail "+side,new Vector3(1.2f+side*.55f,2.05f,.03f),new Vector3(.045f,3.76f,.045f),Color.gray);
                rail.transform.localRotation = Quaternion.FromToRotation(Vector3.up,new Vector3(0,2.1f,3.12f).normalized);
            }
        }
        private void BuildBinoculars()
        {
            var root=new GameObject("Deck binoculars - grab and return");root.transform.SetParent(transform,false);root.transform.localPosition=new Vector3(1.9f,1.05f,-9);
            foreach(float side in new[]{-.07f,.07f}){
                var barrel=Box("Binocular barrel "+side,new Vector3(1.9f+side,1.05f,-9),new Vector3(.09f,.09f,.22f),Color.black);barrel.transform.SetParent(root.transform,true);
            }
            var bridge=Box("Binocular bridge",new Vector3(1.9f,1.05f,-9),new Vector3(.14f,.035f,.07f),Color.gray);bridge.transform.SetParent(root.transform,true);
            binoculars=root.AddComponent<ReturnableTool>();
            Box("Binocular rack",new Vector3(1.9f,.8f,-9),new Vector3(.4f,.4f,.3f),new Color(.28f,.29f,.25f));
        }
        private void BuildOriginalVessel(Transform source)
        {
            var frame = source.parent != null ? source.parent.worldToLocalMatrix : Matrix4x4.identity;
            // Vensail rotates and stretches the imported root directly. Remove that display
            // transform and retain our working vessel's 1.4731 longitudinal calibration
            // (the previous Museum GLB child scale), without editing the exhibit itself.
            if (MuseumSession.Instance != null && MuseumSession.Instance.UsesTeammateMuseum)
                frame = Matrix4x4.Rotate(Quaternion.Euler(-90, 0, 0)) *
                    Matrix4x4.Scale(new Vector3(1, 1.4731f, 1)) * source.worldToLocalMatrix;
            var filters = source.GetComponentsInChildren<MeshFilter>(true);
            var bounds = new Bounds(); bool first = true;
            foreach (var filter in filters)
                foreach (var vertex in filter.sharedMesh.vertices)
                {
                    var point = frame.MultiplyPoint3x4(filter.transform.TransformPoint(vertex));
                    if (first) { bounds = new Bounds(point, Vector3.zero); first = false; } else bounds.Encapsulate(point);
                }
            float factor=28/Mathf.Max(bounds.size.x,bounds.size.z);
            var yaw=bounds.size.x>bounds.size.z?Quaternion.Euler(0,90,0):Quaternion.identity;
            portBoom=new GameObject("Original port fishing outrigger pivot").transform;portBoom.SetParent(transform,false);portBoom.localPosition=new Vector3(-2.7f,3.1f,0);
            starboardBoom=new GameObject("Original starboard fishing outrigger pivot").transform;starboardBoom.SetParent(transform,false);starboardBoom.localPosition=new Vector3(2.7f,3.1f,0);
            hatch=new GameObject("Original working deck hatch hinge").transform;hatch.SetParent(transform,false);hatch.localPosition=new Vector3(0,.5f,-8.5f);
            winch=new GameObject("Original port winch axle").transform;winch.SetParent(transform,false);winch.localPosition=new Vector3(-1.35f,.7f,-12);
            foreach(var filter in filters)
            {
                var original=filter.sharedMesh;var renderer=filter.GetComponent<MeshRenderer>();if(original==null||renderer==null)continue;
                var sourceVertices=original.vertices;var vertices=new Vector3[sourceVertices.Length];
                for(int i=0;i<vertices.Length;i++)vertices[i]=yaw*((frame.MultiplyPoint3x4(filter.transform.TransformPoint(sourceVertices[i]))-bounds.center)*factor)+Vector3.up*5.2f;
                var normals=original.normals;var matrix=(frame*filter.transform.localToWorldMatrix).inverse.transpose;
                for(int i=0;i<normals.Length;i++)normals[i]=yaw*matrix.MultiplyVector(normals[i]).normalized;
                // Retain the original UVs/materials while separating the two movable outer booms.
                var groups=new[]{new List<int>(),new List<int>(),new List<int>(),new List<int>(),new List<int>(),new List<int>(),new List<int>()};
                var triangles=original.triangles;
                for(int i=0;i<triangles.Length;i+=3)
                {
                    var a=vertices[triangles[i]];var b=vertices[triangles[i+1]];var c=vertices[triangles[i+2]];var centre=(a+b+c)/3;
                    // Cut only the access door aperture; all original external silhouette is retained.
                    bool outriggerRegion=centre.z> -1 && centre.z<4.5f && centre.y>2.8f;
                    int group=outriggerRegion && Mathf.Max(a.x,Mathf.Max(b.x,c.x))<-OutriggerCut?1:outriggerRegion && Mathf.Min(a.x,Mathf.Min(b.x,c.x))>OutriggerCut?2:0;
                    float ab=(b-a).magnitude,bc=(c-b).magnitude,ca=(a-c).magnitude;
                    float longest=Mathf.Max(ab,Mathf.Max(bc,ca)),shortest=Mathf.Min(ab,Mathf.Min(bc,ca));
                    if(group==0 && outriggerRegion && longest>1 && longest>shortest*8 && Vector3.Cross(b-a,c-a).magnitude<1)
                    {
                        float min=Mathf.Min(a.x,Mathf.Min(b.x,c.x)),max=Mathf.Max(a.x,Mathf.Max(b.x,c.x));
                        if(min< -OutriggerCut && max> -OutriggerCut && max<1)group=5;
                        else if(max>OutriggerCut && min<OutriggerCut && min> -1)group=6;
                    }
                    if(group==0 && Mathf.Max(Mathf.Abs(a.x),Mathf.Max(Mathf.Abs(b.x),Mathf.Abs(c.x)))<1.15f && centre.z>-9 && centre.z<-4 && centre.y>.1f && centre.y<1.1f && Mathf.Abs(Vector3.Cross(b-a,c-a).normalized.y)>.95f)group=3;
                    if(group==0 && centre.x> -2.2f && centre.x<-.6f && centre.z> -12.6f && centre.z< -10.5f && centre.y>.25f && centre.y<1.7f)group=4;
                    groups[group].Add(triangles[i]);groups[group].Add(triangles[i+1]);groups[group].Add(triangles[i+2]);
                }
                for(int group=0;group<7;group++)
                {
                    if(groups[group].Count==0)continue;
                    var parent=group==0 || group>=5?transform:group==1?portBoom:group==2?starboardBoom:group==3?hatch:winch;
                    var baked=new Mesh{name="ZXH original fishing boat "+filter.name+" group "+group,indexFormat=IndexFormat.UInt32};
                    var points=new Vector3[vertices.Length];for(int i=0;i<points.Length;i++)points[i]=vertices[i]-(group==0 || group>=5?Vector3.zero:parent.localPosition);
                    baked.vertices=points;baked.normals=normals;baked.uv=original.uv;baked.triangles=groups[group].ToArray();baked.RecalculateBounds();meshes.Add(baked);
                    if(group==0)
                    {
                        MeshDoorwayCut.Apply(baked,new Bounds(new Vector3(1.2f,3.15f,1.9f),new Vector3(1.24f,2.2f,2.4f)));
                        MeshDoorwayCut.Apply(baked,new Bounds(new Vector3(1.2f,2.16f,-.05f),new Vector3(1.24f,4.08f,3.3f)));
                        MeshDoorwayCut.Apply(baked,new Bounds(new Vector3(0,3.15f,5.3f),new Vector3(3.96f,2.12f,6.6f)));
                    }
                    else MeshDoorwayCut.Compact(baked);
                    if(baked.vertexCount==0)continue;
                    var item=new GameObject("Exhibition geometry "+filter.name+"_"+group);item.transform.SetParent(parent,false);
                    item.AddComponent<MeshFilter>().sharedMesh=baked;item.AddComponent<MeshRenderer>().sharedMaterials=renderer.sharedMaterials;
                    var collider=item.AddComponent<MeshCollider>();collider.sharedMesh=baked;
                    if(group==1 || group==2){bool port=group==1;Select(item,()=>SetBoom(port,(port?portTarget:starboardTarget)+5));}
                    else if(group==3){HasOriginalHatch=true;Select(item,ToggleHatch);}
                    else if(group==4){HasOriginalWinch=true;Select(item,TurnWinch);}
                    else if(group>=5)
                    {
                        bool port=group==5;
                        ropes.Add(new SupportRope{mesh=baked,home=baked.vertices,collider=collider,port=port});
                        Select(item,()=>SetBoom(port,(port?portTarget:starboardTarget)+5));
                    }
                    // Fixed hull/mast meshes keep collision only; no inspection menu or replica.
                }
            }
            AlignBoomPivot(portBoom);AlignBoomPivot(starboardBoom);
            if(HasOriginalHatch)
            {
                var boundsAtHome=ExhibitInspectionHint.GetBounds(hatch);
                var pivot=new Vector3(boundsAtHome.center.x,boundsAtHome.min.y,boundsAtHome.min.z);
                var correction=hatch.localPosition-pivot;
                foreach(Transform child in hatch)child.localPosition+=correction;
                hatch.localPosition=pivot;
            }
            UsesExhibitionGeometry=true;
            Debug.Log("[ZXH] Boat bake: source="+source.name+" rotation="+source.eulerAngles+" scale="+source.lossyScale+" bounds="+bounds+" hatch="+HasOriginalHatch+" winch="+HasOriginalWinch);
        }
        private void AlignBoomPivot(Transform pivot)
        {
            var points=new List<Vector3>();float nearest=float.MaxValue;
            foreach(var filter in pivot.GetComponentsInChildren<MeshFilter>())
                foreach(var vertex in filter.sharedMesh.vertices)
                {
                    var point=filter.transform.TransformPoint(vertex);
                    points.Add(point);nearest=Mathf.Min(nearest,Mathf.Abs(point.x));
                }
            if(points.Count==0)return;
            Vector3 attachment=Vector3.zero;int count=0;
            foreach(var point in points)if(Mathf.Abs(point.x)<nearest+.12f){attachment+=point;count++;}
            attachment/=count;
            var correction=pivot.position-attachment;
            foreach(Transform child in pivot)child.localPosition+=correction;
            pivot.position=attachment;
        }
        public void Travel(bool toDeck)
        {
            var session=MuseumSession.Instance;if(session==null||session.Busy||session.Seated)return;
            session.CancelCurrentInteraction();cabin.Observation.Close();
            session.MoveTo(toDeck?SpawnPoint:cabin.transform.TransformPoint(new Vector3(0,0,2.5f)),toDeck?0:0);
        }
        public void ResetDeck()
        {
            portTarget=starboardTarget=0;hatchOpen=false;
            if(CabinDoorOpen)ToggleDoor();
            if(winch!=null)winch.localRotation=Quaternion.identity;
            if(binoculars!=null)binoculars.ResetPose();
        }
        private void Update() => AdvanceMotion(Time.deltaTime);
        public void AdvanceMotion(float seconds)
        {
            if(controls==null)return;
            PortBoomAngle=Mathf.MoveTowards(PortBoomAngle,portTarget,seconds*5);
            StarboardBoomAngle=Mathf.MoveTowards(StarboardBoomAngle,starboardTarget,seconds*5);
            portBoom.localRotation=Quaternion.Euler(0,0,-PortBoomAngle);starboardBoom.localRotation=Quaternion.Euler(0,0,StarboardBoomAngle);
            if(!Mathf.Approximately(lastPort,PortBoomAngle) || !Mathf.Approximately(lastStarboard,StarboardBoomAngle))
            {
                foreach(var rope in ropes)
                {
                    var pivot=rope.port?portBoom:starboardBoom;
                    var points=new Vector3[rope.home.Length];
                    for(int i=0;i<points.Length;i++)
                    {
                        var v=rope.home[i];
                        var moved=pivot.localPosition+pivot.localRotation*(v-pivot.localPosition);
                        float attachment=Mathf.Abs(pivot.localPosition.x);
                        points[i]=Vector3.Lerp(v,moved,Mathf.InverseLerp(attachment-.05f,attachment+.35f,Mathf.Abs(v.x)));
                    }
                    rope.mesh.vertices=points;rope.mesh.RecalculateNormals();rope.mesh.RecalculateBounds();
                    rope.collider.sharedMesh=null;rope.collider.sharedMesh=rope.mesh;
                }
                lastPort=PortBoomAngle;lastStarboard=StarboardBoomAngle;
            }
            door.localPosition=Vector3.MoveTowards(door.localPosition,doorHome+(CabinDoorOpen?Vector3.right*1.2f:Vector3.zero),seconds);
            hatch.localRotation=Quaternion.RotateTowards(hatch.localRotation,Quaternion.Euler(hatchOpen?-65:0,0,0),seconds*60);
            controls.Status.text="Point + trigger\nLeft "+PortBoomAngle.ToString("F0")+" / Right "+StarboardBoomAngle.ToString("F0")+" degrees";
        }
        private void OnDestroy(){foreach(var mesh in meshes)if(mesh!=null)Destroy(mesh);foreach(var mat in materials)if(mat!=null)Destroy(mat);}
    }
}
