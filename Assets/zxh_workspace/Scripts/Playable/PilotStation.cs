using UnityEngine;

namespace ZxhMuseum
{
    // Built around the existing helm. Physical controls use the museum trigger bridge.
    public sealed class PilotStation : MonoBehaviour
    {
        public PhysicalPushButton Ahead,Neutral,Astern,MorePower,LessPower;
        public GameObject Chair { get; private set; }
        public PhysicalPushButton Stand { get; private set; }
        private CabinEnvironment cabin;
        private TextMesh readout;
        private Transform throttle;
        public void Initialize(CabinEnvironment owner)
        {
            cabin=owner;var maker=gameObject.AddComponent<PhysicalBoatControls>();
            var metal=new Color(.16f,.2f,.22f);var upholstery=new Color(.12f,.2f,.21f);
            maker.Part("Pilot chair pedestal",PrimitiveType.Cube,new Vector3(0,.25f,-1.35f),new Vector3(.16f,.5f,.16f),metal);
            Chair=maker.Part("Pilot chair seat",PrimitiveType.Cube,new Vector3(0,.53f,-1.35f),new Vector3(.55f,.14f,.55f),upholstery);
            maker.Part("Pilot chair back",PrimitiveType.Cube,new Vector3(0,.85f,-1.08f),new Vector3(.55f,.62f,.09f),upholstery);
            foreach(float x in new[]{-.29f,.29f})maker.Part("Pilot armrest",PrimitiveType.Cube,new Vector3(x,.72f,-1.35f),new Vector3(.06f,.08f,.46f),metal);
            System.Action sit=()=>MuseumSession.Instance?.Sit(transform.TransformPoint(new Vector3(0,1.5f,-1.35f)),transform.eulerAngles.y+180);
            BoatTriggerAction.Bind(Chair,sit);Chair.AddComponent<DesktopObjectClick>().Selected=sit;
            Stand=maker.Button("Pilot stand button",new Vector3(.55f,1.05f,-1.65f),Quaternion.Euler(0,180,0),"STAND / LEAVE",()=>MuseumSession.Instance?.LeaveSeat());
            var facing=Quaternion.Euler(0,180,0);
            Ahead=maker.Button("Engine ahead",new Vector3(-.95f,.83f,-2.25f),facing,"AHEAD",()=>SelectGear(1));
            Neutral=maker.Button("Engine neutral",new Vector3(-.6f,.83f,-2.25f),facing,"STOP / N",()=>SelectGear(0));
            Astern=maker.Button("Engine astern",new Vector3(-.25f,.83f,-2.25f),facing,"ASTERN",()=>SelectGear(-1));
            MorePower=maker.Button("Throttle increase",new Vector3(-.95f,1.18f,-2.25f),facing,"POWER +10%",()=>cabin.Navigation.SetPower(cabin.Navigation.Power+.1f));
            LessPower=maker.Button("Throttle decrease",new Vector3(-.6f,1.18f,-2.25f),facing,"POWER -10%",()=>cabin.Navigation.SetPower(cabin.Navigation.Power-.1f));
            var screen=maker.Part("Helm instrument panel",PrimitiveType.Cube,new Vector3(.75f,1.48f,-2.4f),new Vector3(.72f,.36f,.04f),Color.black);
            screen.GetComponent<Collider>().enabled=false;
            var face=new GameObject("Live instrument face").transform;face.SetParent(transform,false);
            face.localPosition=new Vector3(.75f,1.48f,-2.37f);face.localRotation=facing;
            maker.Label(face,"HDG 000  SPEED -9.9 kn\nASTERN  POWER 100%\nRUDDER -135\nSTOP / N, wait to reverse",Vector3.zero,.027f,.66f,.3f);
            readout=face.GetComponentInChildren<TextMesh>();
            throttle=new GameObject("Throttle indicator pivot").transform;throttle.SetParent(transform,false);throttle.localPosition=new Vector3(-1.12f,1.38f,-2.6f);
            var knob=maker.Part("Throttle indicator",PrimitiveType.Cube,new Vector3(0,.06f,0),new Vector3(.04f,.12f,.04f),new Color(.85f,.5f,.1f),throttle);
            knob.GetComponent<Collider>().enabled=false;
            maker.InstructionSign("Pilot operation guide",new Vector3(.75f,1.86f,-2.62f),facing,"PILOT STATION","Seat: TRIGGER / Wheel: hold GRIP\nAHEAD or ASTERN, then POWER +\nSTOP / N before reversing",.72f,.25f);
        }
        public void SelectGear(int gear)
        {
            if(cabin.Navigation.Gear==gear)return;
            if(cabin.Navigation.SetGear(gear) && gear!=0)
            {
                cabin.Navigation.SetPower(.3f);
                if(!cabin.SeaWindow.IsOpen)cabin.SeaWindow.Toggle();
            }
        }
        private void LateUpdate()
        {
            if(cabin==null)return;var n=cabin.Navigation;
            readout.text="HDG "+n.Heading.ToString("000")+"  SPEED "+(n.Speed*1.94384f).ToString("F1")+" kn\n"+
                (n.Gear==0?"NEUTRAL":n.Gear>0?"AHEAD":"ASTERN")+"  POWER "+(n.Power*100).ToString("F0")+"%\nRUDDER "+n.WheelAngle.ToString("F0")+
                "\n"+(n.Notice.StartsWith("Reduce")?"STOP / N, wait to reverse":"Grip wheel; + / - power");
            throttle.localRotation=Quaternion.Euler(n.Power*50-25,0,0);
            Ahead.SetIndicator(n.Gear==1);Neutral.SetIndicator(n.Gear==0);Astern.SetIndicator(n.Gear==-1);
        }
    }
}
