using System.Collections.Generic;
using UnityEngine;

namespace ZxhMuseum
{
    // Opt-in on scene 6's XR rig only. Shared scene 5 and ShipInterior assets stay unchanged.
    public sealed class SceneSixDoorClearance : MonoBehaviour
    {
        private CabinEnvironment applied;
        private readonly List<Mesh> copies=new List<Mesh>();
        public int CorrectedColliders { get; private set; }
        public static readonly Bounds Headroom=new Bounds(new Vector3(1.2f,4.75f,1.6f),new Vector3(1.5f,1.5f,2.8f));
        private void Update()
        {
            var session=MuseumSession.Instance;
            if(session==null || session.Origin==null || !transform.IsChildOf(session.Origin.transform))return;
            var cabin=FindObjectOfType<CabinEnvironment>();
            if(cabin==null || cabin==applied)return;
            Apply(cabin);
        }
        public void Apply(CabinEnvironment cabin)
        {
            foreach(var mesh in copies)if(mesh!=null)Destroy(mesh);
            copies.Clear();
            applied=cabin;CorrectedColliders=0;
            // Cabin Awake builds at the origin, then relocates the complete assembly.
            // Read bounds only after physics has received those final transforms.
            Physics.SyncTransforms();
            foreach(var collider in cabin.GetComponentsInChildren<Collider>())
            {
                if(!collider.enabled || collider.isTrigger || !collider.bounds.Intersects(Headroom))continue;
                // Keep the working door and all interactable fittings intact.
                if(collider.name=="Sliding wheelhouse door" || collider.GetComponentInParent<UnityEngine.XR.Interaction.Toolkit.XRBaseInteractable>()!=null)continue;
                var cut=LocalBounds(collider.transform,Headroom);
                if(collider is MeshCollider mesh && mesh.sharedMesh!=null && !mesh.convex)
                {
                    var copy=Instantiate(mesh.sharedMesh);copy.name="Scene 6 door head clearance";
                    MeshDoorwayCut.Apply(copy,cut);copies.Add(copy);mesh.sharedMesh=copy;CorrectedColliders++;
                }
                else if(collider is BoxCollider box)
                {
                    Split(box,cut);CorrectedColliders++;
                }
            }
            Physics.SyncTransforms();
            Debug.Log("[ZXH scene 6] Doorway head clearance corrected: "+CorrectedColliders);
        }
        private static Bounds LocalBounds(Transform target,Bounds world)
        {
            var bounds=new Bounds(target.InverseTransformPoint(world.min),Vector3.zero);
            for(int mask=0;mask<8;mask++)bounds.Encapsulate(target.InverseTransformPoint(new Vector3(
                (mask&1)==0?world.min.x:world.max.x,(mask&2)==0?world.min.y:world.max.y,(mask&4)==0?world.min.z:world.max.z)));
            return bounds;
        }
        private static void Split(BoxCollider original,Bounds cut)
        {
            var bounds=new Bounds(original.center,original.size);var lo=bounds.min;var hi=bounds.max;
            var a=Vector3.Max(lo,cut.min);var b=Vector3.Min(hi,cut.max);
            if(a.x>=b.x || a.y>=b.y || a.z>=b.z)return;
            Add(original,lo,new Vector3(a.x,hi.y,hi.z));Add(original,new Vector3(b.x,lo.y,lo.z),hi);
            Add(original,new Vector3(a.x,lo.y,lo.z),new Vector3(b.x,a.y,hi.z));
            Add(original,new Vector3(a.x,b.y,lo.z),new Vector3(b.x,hi.y,hi.z));
            Add(original,new Vector3(a.x,a.y,lo.z),new Vector3(b.x,b.y,a.z));
            Add(original,new Vector3(a.x,a.y,b.z),new Vector3(b.x,b.y,hi.z));
            original.enabled=false;
        }
        private static void Add(BoxCollider original,Vector3 lo,Vector3 hi)
        {
            if(hi.x-lo.x<.0001f || hi.y-lo.y<.0001f || hi.z-lo.z<.0001f)return;
            var part=original.gameObject.AddComponent<BoxCollider>();part.center=(lo+hi)*.5f;part.size=hi-lo;
            part.sharedMaterial=original.sharedMaterial;part.isTrigger=original.isTrigger;
        }
        private void OnDestroy() { foreach(var mesh in copies)if(mesh!=null)Destroy(mesh); }
    }
}
