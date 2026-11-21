using UnityEngine;
using System.Collections.Generic;

namespace ZxhMuseum
{
    // Move external visual references, never the tracked rig or walkable collision geometry.
    public sealed class BoatDrivingFeedback : MonoBehaviour
    {
        public bool MotionEnabled=true, SoundEnabled=true;
        public Vector3 VirtualPosition { get; private set; }
        public Transform Reference { get; private set; }
        private VoyageState navigation;
        private Transform scenery;
        private AudioSource engine;
        private AudioClip clip;
        private readonly List<Vector3> referencePositions=new List<Vector3>();
        public void Initialize(VoyageState state)
        {
            navigation=state;
            var maker=gameObject.AddComponent<PhysicalBoatControls>();
            scenery=new GameObject("External sailing references").transform;scenery.SetParent(transform,false);
            for(int i=0;i<12;i++)
            {
                float angle=i*30*Mathf.Deg2Rad;
                var buoy=maker.Part("Distant navigation buoy",PrimitiveType.Cylinder,
                    new Vector3(Mathf.Sin(angle)*55,-1,Mathf.Cos(angle)*55),new Vector3(1,3,1),
                    i%2==0?new Color(.8f,.15f,.08f):new Color(.1f,.65f,.3f),scenery);
                buoy.GetComponent<Collider>().enabled=false;
                if(i==0)Reference=buoy.transform;
                var crest=maker.Part("Moving sea reference",PrimitiveType.Cube,
                    new Vector3((i%2==0?-1:1)*(5+i),-2.9f,-35+i*7),new Vector3(2,.03f,1),new Color(.6f,.8f,.85f),scenery);
                crest.GetComponent<Collider>().enabled=false;
            }
            foreach(Transform child in scenery)referencePositions.Add(child.localPosition);
            engine=gameObject.AddComponent<AudioSource>();engine.loop=true;engine.spatialBlend=0;engine.playOnAwake=false;
            const int rate=22050;var samples=new float[rate];
            for(int i=0;i<rate;i++){float t=i/(float)rate;samples[i]=Mathf.Sin(2*Mathf.PI*55*t)*.6f+Mathf.Sin(2*Mathf.PI*110*t)*.2f;}
            clip=AudioClip.Create("Synthetic marine engine",rate,1,rate,false);clip.SetData(samples,0);engine.clip=clip;
            engine.volume=0;engine.Play();
        }
        private void Update()=>Advance(Time.deltaTime);
        public void Advance(float seconds)
        {
            if(navigation==null || scenery==null)return;
            float dt=Mathf.Clamp(seconds,0,1);
            VirtualPosition+=Quaternion.Euler(0,navigation.Heading,0)*Vector3.forward*navigation.Speed*dt;
            if(MotionEnabled)
            {
                scenery.localRotation=Quaternion.Euler(0,-navigation.Heading,0);
                for(int i=0;i<scenery.childCount;i++)
                {
                    var relative=referencePositions[i]-VirtualPosition;
                    relative.x=Mathf.Repeat(relative.x+80,160)-80;
                    relative.z=Mathf.Repeat(relative.z+80,160)-80;
                    var item=scenery.GetChild(i);item.localPosition=relative;
                    // Recycle distant references out of sight, keeping motion continuous nearby.
                    item.GetComponent<Renderer>().enabled=new Vector2(relative.x,relative.z).magnitude<70;
                }
            }
            engine.volume=SoundEnabled?Mathf.Lerp(engine.volume,navigation.Power*.12f,Mathf.Clamp01(dt*3)):0;
            engine.pitch=.65f+navigation.Power*.65f+Mathf.Abs(navigation.Speed)*.03f;
        }
        public void ResetFeedback()
        {
            VirtualPosition=Vector3.zero;
            if(scenery!=null){scenery.localPosition=Vector3.zero;scenery.localRotation=Quaternion.identity;}
            if(scenery!=null)for(int i=0;i<scenery.childCount;i++)
            {scenery.GetChild(i).localPosition=referencePositions[i];scenery.GetChild(i).GetComponent<Renderer>().enabled=true;}
            if(engine!=null)engine.volume=0;
        }
        private void OnDestroy(){if(clip!=null)Destroy(clip);}
    }
}
