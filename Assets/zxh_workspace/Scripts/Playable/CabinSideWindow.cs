using UnityEngine;
using UnityEngine.Rendering;

namespace ZxhMuseum
{
    public sealed class CabinSideWindow : MonoBehaviour
    {
        public bool IsOpen { get; private set; }
        public Transform Hinge { get; private set; }
        public GameObject Glass { get; private set; }
        public PhysicalPushButton Button { get; private set; }
        public void Initialize(PhysicalBoatControls maker)
        {
            var frame=new GameObject("Right glass window frame").transform;
            frame.SetParent(transform,false);frame.localPosition=new Vector3(-1.94f,1.54f,.2f);
            frame.localRotation=Quaternion.Euler(0,90,0);
            var brass=new Color(.48f,.36f,.17f);
            foreach(float x in new[]{-.6f,.6f})
                maker.Part("Window jamb",PrimitiveType.Cube,new Vector3(x,0,0),new Vector3(.055f,.94f,.09f),brass,frame);
            foreach(float y in new[]{-.44f,.44f})
                maker.Part("Window rail",PrimitiveType.Cube,new Vector3(0,y,0),new Vector3(1.25f,.055f,.09f),brass,frame);
            Hinge=new GameObject("Right window opening hinge").transform;Hinge.SetParent(frame,false);
            Hinge.localPosition=new Vector3(-.57f,0,0);
            Glass=maker.Part("Interactive right window glass",PrimitiveType.Cube,new Vector3(.57f,0,0),new Vector3(1.1f,.82f,.025f),new Color(.65f,.85f,.92f,.16f),Hinge);
            var material=Glass.GetComponent<Renderer>().sharedMaterial;
            material.SetFloat("_Mode",3);material.SetInt("_SrcBlend",(int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);material.SetInt("_ZWrite",0);
            material.EnableKeyword("_ALPHABLEND_ON");material.renderQueue=3000;
            foreach(float x in new[]{.015f,1.125f})
                maker.Part("Moving glass side trim",PrimitiveType.Cube,new Vector3(x,0,0),new Vector3(.035f,.85f,.04f),brass,Hinge);
            foreach(float y in new[]{-.41f,.41f})
                maker.Part("Moving glass horizontal trim",PrimitiveType.Cube,new Vector3(.57f,y,0),new Vector3(1.14f,.035f,.04f),brass,Hinge);
            BoatTriggerAction.Bind(Glass,Toggle);Glass.AddComponent<DesktopObjectClick>().Selected=Toggle;
            Button=maker.Button("Right window switch",new Vector3(-1.83f,1.35f,1.06f),Quaternion.Euler(0,-90,0),"WINDOW\nOPEN / CLOSE",Toggle);
        }
        public void Toggle() => IsOpen=!IsOpen;
        public void Close() => IsOpen=false;
        public void Advance(float seconds)
        {
            Hinge.localRotation=Quaternion.RotateTowards(Hinge.localRotation,Quaternion.Euler(0,IsOpen?90:0,0),seconds*75);
            Button.SetIndicator(IsOpen);
        }
        private void Update() { if(Hinge!=null)Advance(Time.deltaTime); }
    }
}
