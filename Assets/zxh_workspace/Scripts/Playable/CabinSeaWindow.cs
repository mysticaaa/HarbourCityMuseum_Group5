using UnityEngine;

namespace ZxhMuseum
{
    public sealed class CabinSeaWindow : MonoBehaviour
    {
        public bool IsOpen { get; private set; }
        public Transform Hinge { get; private set; }
        private PhysicalPushButton latch;
        public void Initialize(PhysicalBoatControls maker)
        {
            Hinge=new GameObject("Sea window upper hinge").transform;Hinge.SetParent(transform,false);
            Hinge.localPosition=new Vector3(0,1.98f,-3.36f);
            var shutter=maker.Part("Opening sea window shutter",PrimitiveType.Cube,new Vector3(0,-.38f,0),
                new Vector3(1.05f,.74f,.045f),new Color(.21f,.29f,.30f),Hinge);
            BoatTriggerAction.Bind(shutter,Toggle);shutter.AddComponent<DesktopObjectClick>().Selected=Toggle;
            foreach(float side in new[]{-1f,1f})
                maker.Part("Sea window side frame",PrimitiveType.Cube,new Vector3(side*.57f,1.6f,-3.4f),new Vector3(.06f,.9f,.07f),new Color(.48f,.36f,.17f));
            foreach(float height in new[]{1.18f,2.02f})
                maker.Part("Sea window horizontal frame",PrimitiveType.Cube,new Vector3(0,height,-3.4f),new Vector3(1.2f,.06f,.07f),new Color(.48f,.36f,.17f));
            maker.Part("Sea window latch mounting rail",PrimitiveType.Cube,new Vector3(-.85f,1.55f,-3.4f),new Vector3(.6f,.07f,.07f),new Color(.48f,.36f,.17f));
            latch=maker.Button("Sea window latch",new Vector3(-.78f,1.55f,-3.28f),Quaternion.Euler(0,180,0),"WINDOW\nOPEN / CLOSE",Toggle);
        }
        public void Toggle() { IsOpen=!IsOpen; }
        public void Close() { IsOpen=false; }
        public void Advance(float seconds)
        {
            Hinge.localRotation=Quaternion.RotateTowards(Hinge.localRotation,Quaternion.Euler(IsOpen?90:0,0,0),seconds*75);
            latch.SetIndicator(IsOpen);
        }
        private void Update() { if(Hinge!=null)Advance(Time.deltaTime); }
    }
}
