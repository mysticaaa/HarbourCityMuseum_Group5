using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

namespace ZzxWorkspace.FirstDay
{
    // Builds only exhibit-local geometry; borrows the existing interactable's manager.
    public sealed partial class HarbourTabletopPrototype : MonoBehaviour
    {
        public const string TargetScene = "Assets/261122/Museum_261122_1.unity";
        public enum Stage { Idle, Explore, Choosing, Report, Cargo, Selected, Loading, Letter, Complete }
        public Stage CurrentStage { get; private set; }
        [Min(.1f)] public float movementSeconds = .8f;
        private Transform model, hook, crate, ferry;
        private GameObject target;
        private TMP_Text message;
        private XRInteractionManager manager;
        private readonly List<Material> materials = new List<Material>();
        private Vector3 crateHome, ferryHome;
        private LineRenderer cable;
        private Material wood, sea, white, dark, cyan;
        private int lastClick = -1;
        private Coroutine runningAnimation;
        private int activeArea=-1;
        private bool formalLoading;
        private readonly bool[] visited = new bool[3];
        private readonly HarbourPrototypeButton[] areaButtons = new HarbourPrototypeButton[3];
        private Transform repairBoat, cargoBoat, repairMarker;
        private Vector3 repairHome, cargoHome;

        private void Start()
        {
            var old = GetComponentInChildren<XRBaseInteractable>(true);
            manager = old ? old.interactionManager : null;
            if (!manager)
            {
                Debug.LogError("Harbour prototype needs the exhibit's existing XRInteractionManager.", this);
                enabled = false;
                return;
            }
            foreach (var b in GetComponentsInChildren<FirstDayMapButton>(true)) b.enabled = false;
            foreach (string name in new[] { "Harbour map", "Documents", "Journey ending", "Restart button",
                "Letter sign", "Pass sign", "Map sign",
                "Letter sign panel", "Pass sign panel", "Map sign panel",
                "Collection", "Introduction", "Instructions", "Fiction notice", "Title" })
            { var t = transform.Find(name); if (t) t.gameObject.SetActive(false); }
            SetText("Introduction", "A first day at the harbour. Help Liang report for work.");
            SetText("Instructions", "POINT + TRIGGER / Explore, check in, then load one crate.");
            SetText("Progress", ""); SetText("Task feedback", "");
            Build(); BuildPresentation(); BuildHarbourArt(); BuildLowTable(); ResetExperience();
        }
        private void SetText(string name, string text)
        { var t = transform.Find(name); if (t && t.TryGetComponent<TMP_Text>(out var label)) label.text = text; }
        private Material Mat(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var m = new Material(shader) { color = color }; materials.Add(m); return m;
        }
        private Transform Box(string name, Vector3 p, Vector3 size, Material mat, Transform parent = null)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube); g.name = name;
            g.transform.SetParent(parent ? parent : model, false); g.transform.localPosition = p;
            g.transform.localScale = size; g.GetComponent<Renderer>().sharedMaterial = mat;
            // Decorative geometry never occludes a ray aimed at a button or target.
            g.GetComponent<Collider>().enabled = false; return g.transform;
        }
        private TMP_Text Label(string name, string content, Vector3 p, Vector2 size, float font = .035f)
        {
            var g = new GameObject(name); g.transform.SetParent(model, false); g.transform.localPosition = p;
            var t = g.AddComponent<TextMeshPro>();
            t.text = content; t.fontSize = font * 10; t.color = Color.white;
            t.alignment = TextAlignmentOptions.Center; t.rectTransform.sizeDelta = size;
            t.enableAutoSizing = false; t.enableWordWrapping = true;
            t.lineSpacing = 12; t.margin = new Vector4(.012f,.008f,.012f,.008f);
            return t;
        }
        private GameObject Button(string name, string text, Vector3 p, Vector3 size, System.Action action)
        {
            var root = new GameObject(name); root.transform.SetParent(model, false); root.transform.localPosition = p;
            var c = root.AddComponent<BoxCollider>(); c.size = size;
            var face = Box("Button face", Vector3.zero, size, dark, root.transform);
            var label = Label(name + " label", text, p + new Vector3(0, 0, -size.z / 2 - .004f), new Vector2(size.x * .95f, size.y * .9f), .032f);
            label.transform.SetParent(root.transform, true);
            var xr = root.AddComponent<XRSimpleInteractable>(); xr.interactionManager = manager;
            var input = root.AddComponent<HarbourPrototypeButton>(); input.action = () =>
            { if (lastClick == Time.frameCount) return; lastClick = Time.frameCount; action(); };
            input.face = face.GetComponent<Renderer>(); input.normal = dark; input.highlight = cyan;
            return root;
        }
        private void Build()
        {
            model = new GameObject("Harbour tabletop prototype").transform; model.SetParent(transform, false);
            wood = Mat(new Color(.55f,.34f,.16f)); sea = Mat(new Color(.08f,.31f,.40f));
            white = Mat(new Color(.88f,.86f,.77f)); dark = Mat(new Color(.06f,.12f,.16f)); cyan = Mat(new Color(.87f,.45f,.08f));
            Box("Water tray", new Vector3(0,.915f,.12f), new Vector3(1.86f,.035f,.52f), sea);
            for (int i = 0; i < 3; i++)
            {
                float x = (i-1)*.60f;
                Box("Pier", new Vector3(x,.95f,.24f), new Vector3(.17f,.045f,.26f), wood);
                var boat = new GameObject(i == 0 ? "Ferry" : i == 1 ? "Cargo ship" : "Repair boat").transform;
                boat.SetParent(model,false); boat.localPosition = new Vector3(x,.965f,.03f);
                Box("Hull",Vector3.zero,new Vector3(.34f,.065f,.15f),dark,boat);
                Box("Cabin",new Vector3(-.09f,.06f,0),new Vector3(.11f,.07f,.12f),white,boat);
                if(i == 0) { ferry = boat; ferryHome = boat.localPosition; }
                if(i == 1) { cargoBoat=boat; cargoHome=boat.localPosition; }
                if(i == 2) { repairBoat=boat; repairHome=boat.localPosition; }
                int area = i;
                Button("Area " + i, i == 0 ? "01 FERRY" : i == 1 ? "02 CARGO" : "03 REPAIR",
                    new Vector3(x,.94f,-.20f),new Vector3(.49f,.10f,.045f),()=>Explore(area));
                areaButtons[i]=model.Find("Area " + i).GetComponent<HarbourPrototypeButton>();
            }
            Box("Crane left",new Vector3(-.22f,1.15f,.22f),new Vector3(.035f,.40f,.04f),wood);
            Box("Crane right",new Vector3(.22f,1.15f,.22f),new Vector3(.035f,.40f,.04f),wood);
            Box("Crane beam",new Vector3(0,1.36f,.22f),new Vector3(.49f,.035f,.045f),wood);
            hook = Box("Hook",new Vector3(.17f,1.28f,.20f),new Vector3(.025f,.035f,.025f),cyan);
            cable = new GameObject("Cable").AddComponent<LineRenderer>(); cable.transform.SetParent(model,false);
            cable.useWorldSpace=false; cable.positionCount=2; cable.startWidth=cable.endWidth=.005f;
            var cableMat = new Material(Shader.Find("Sprites/Default")); materials.Add(cableMat); cable.sharedMaterial=cableMat;
            cable.startColor=cable.endColor=Color.black;
            crateHome = new Vector3(-.035f,1.0115f,.21f);
            crate = Box("Cargo crate",crateHome,new Vector3(.060f,.060f,.060f),wood);
            var crateButton = Button("Select cargo", "CRATE", new Vector3(-.035f,1.064f,.21f),new Vector3(.16f,.12f,.12f),SelectCrate);
            crateInput=crateButton;
            crateButton.transform.Find("Button face").GetComponent<Renderer>().enabled = false;
            target = Button("Boat target", "LOAD HERE",new Vector3(.07f,1.04f,-.015f),new Vector3(.22f,.09f,.12f),Load);
            target.transform.Find("Button face").GetComponent<Renderer>().enabled=false;
            repairMarker = Box("Repair status marker",new Vector3(.60f,1.08f,.03f),new Vector3(.12f,.025f,.06f),cyan);
            repairMarker.gameObject.SetActive(false);
            message = Label("Narration subtitles","",new Vector3(0,1.58f,.32f),new Vector2(1.76f,.38f),.044f);
            Box("Subtitle backing",new Vector3(0,1.58f,.35f),new Vector3(1.90f,.43f,.018f),dark);
            mainAction = Button("Main action","START",new Vector3(-.48f,1.26f,.30f),new Vector3(.64f,.12f,.035f),Next);
            skipAction = Button("Skip","STOP DEMO",new Vector3(.48f,1.26f,.30f),new Vector3(.64f,.12f,.035f),Skip);


            Button("Reset","RESTART",new Vector3(.60f,.72f,-.32f),new Vector3(.50f,.12f,.04f),ResetExperience);
            Label("Story notice", "Fictional Reconstruction", new Vector3(0,.51f,-.35f),new Vector2(1.75f,.09f),.028f);
        }
        public void ResetExperience()
        {
            StopAnimation(); CurrentStage=Stage.Idle; activeArea=-1;
            passRead=letterRead=false;
            for(int i=0;i<3;i++) { visited[i]=false; SetAreaLabel(i); }
            ResetModels(); CloseReader(); choices.SetActive(false); stamp.SetActive(false);
            ShowTask("FIRST DAY AT THE HARBOUR", "Select START to help Liang find his workplace.");
        }
        private void Say(string text) { message.text=text; }
        private void ShowTask(string heading,string text) { Say("<b>"+heading+"</b>\n"+text); RefreshUI(); }
        private void RefreshUI()
        {
            mainAction.SetActive(CurrentStage==Stage.Idle || CurrentStage==Stage.Explore || CurrentStage==Stage.Choosing || CurrentStage==Stage.Report);
            string text=CurrentStage==Stage.Idle ? "START" : CurrentStage==Stage.Report ? "REPORT AT 02" : "FIND WORKPLACE";
            mainAction.GetComponentInChildren<TMP_Text>().text=text;
            skipAction.SetActive(runningAnimation!=null || formalLoading);
            skipAction.GetComponentInChildren<TMP_Text>().text=formalLoading ? "SKIP LIFT" : "STOP DEMO";
            crateInput.SetActive(runningAnimation==null && (CurrentStage==Stage.Cargo || CurrentStage==Stage.Selected));
            target.SetActive(runningAnimation==null && CurrentStage==Stage.Selected);
            for(int i=0;i<3;i++) areaButtons[i].suggested=activeArea==i || (CurrentStage==Stage.Report && i==1);
        }
        private void Next()
        {
            if(formalLoading) return;
            CloseReader(); StopAnimation();
            if(CurrentStage==Stage.Idle)
            {
                CurrentStage=Stage.Explore;
                ShowTask("HELP LIANG FIND HIS WORKPLACE", "Liang has just arrived in the harbour city. Help him find his assigned workplace. Read both documents.");
            }
            else if(CurrentStage==Stage.Report)
            {
                CurrentStage=Stage.Cargo; stamp.SetActive(true);
                ShowTask("NEXT MORNING / REPORT SUCCESSFUL", "Berth No. 02. Select the shore crate, then LOAD HERE.");
            }
            else if(CurrentStage==Stage.Explore || CurrentStage==Stage.Choosing)
            {
                if(!passRead || !letterRead) { ShowTask("LOOK FOR CLUES", "Read both WORK PASS and LETTER HOME. Explore any area you like."); return; }
                CurrentStage=Stage.Choosing; choices.SetActive(true);
                ShowTask("FIND LIANG'S WORKPLACE", "Where should Liang report for work?");
            }
        }
        private void ChooseWorkplace(int area)
        {
            if(CurrentStage!=Stage.Choosing) return;
            StopAnimation();
            if(area==0) ShowTask("FERRY / PASSENGER TRANSPORT", "Check Liang's work pass again. You can explore and choose again.");
            else if(area==2) ShowTask("REPAIR / BOAT MAINTENANCE", "What kind of work is Liang assigned to? You can choose again.");
            else
            {
                CurrentStage=Stage.Report; choices.SetActive(false); activeArea=1;
                ShowTask("CORRECT / CARGO", "Cargo workers handle goods for transport. Report to Berth No. 02.");
            }
        }
        private void SetAreaLabel(int area)
        {
            string[] names={"01 FERRY", "02 CARGO", "03 REPAIR"};
            areaButtons[area].GetComponentInChildren<TMP_Text>().text=names[area]+(visited[area] ? " [SEEN]" : "");
        }
        private void ResetModels()
        {
            ferry.localPosition=ferryHome; cargoBoat.localPosition=cargoHome; repairBoat.localPosition=repairHome;
            passengers.SetActive(false); passengers.transform.localPosition=passengerHome; repairMarker.gameObject.SetActive(false); repairTool.gameObject.SetActive(false); repairWorker.SetActive(false);
            repairMarker.GetComponent<Renderer>().sharedMaterial=cyan;
            repairTool.localRotation=Quaternion.identity;
            if(ferryDoor) { ferryDoor.localRotation=Quaternion.identity; ferryDoor.localPosition=doorClosedPosition; }
            if(ferryDoorFace) ferryDoorFace.sharedMaterial=white;
            if(ferryOpeningFace) ferryOpeningFace.sharedMaterial=white;
            crate.localPosition=CurrentStage==Stage.Letter || CurrentStage==Stage.Complete ? cargoLanding : crateHome;
            crate.GetComponent<Renderer>().sharedMaterial=CurrentStage==Stage.Selected ? cyan : wood;
            hook.localPosition=hookHome;
        }
        private void StopAnimation()
        {
            if(runningAnimation!=null) StopCoroutine(runningAnimation);
            runningAnimation=null; formalLoading=false; activeArea=-1;
            if(model && passengers) { ResetModels(); RefreshUI(); }
        }
        private void Explore(int area)
        {
            if(formalLoading) { ShowTask("LOADING IN PROGRESS", "Wait for the lift or select SKIP LIFT, then explore any area."); return; }
            CloseReader(); StopAnimation(); activeArea=area;
            runningAnimation=StartCoroutine(VisitArea(area)); RefreshUI();
        }
        private IEnumerator VisitArea(int area)
        {
            // Let the coroutine handle be assigned before any possible completion.
            yield return null;
            if(area==0)
            {
                ShowTask("FERRY / PASSENGER TRANSPORT", "Ferries carry passengers. Liang arrived in the city by ferry.");
                ferry.localPosition=ferryHome+Vector3.back*.10f;
                yield return Move(ferry,ferry.localPosition,ferryHome);
                yield return SlideDoor(true);
                passengers.SetActive(true);
                yield return Move(passengers.transform,passengerHome,passengerHome+new Vector3(.14f,0,0));
                // Passengers remain ashore while the door closes, rather than walking back aboard.
                yield return new WaitForSeconds(.35f);
                yield return SlideDoor(false);
                passengers.SetActive(false); passengers.transform.localPosition=passengerHome;
                yield return Move(ferry,ferryHome,ferryHome+Vector3.back*.10f);
            }
            else if(area==1)
            {
                ShowTask("CARGO / GOODS HANDLING", "Cargo terminals load goods for transport by ship.");
                crate.localPosition=crateHome; crate.GetComponent<Renderer>().sharedMaterial=wood;
                hook.localPosition=hookHome;
                cargoBoat.localPosition=cargoHome+Vector3.back*.10f;
                yield return Move(cargoBoat,cargoBoat.localPosition,cargoHome);
                crate.localPosition=crateHome; crate.GetComponent<Renderer>().sharedMaterial=cyan;
                yield return CargoMotion();
            }
            else
            {
                ShowTask("REPAIR / INSPECTION AND MAINTENANCE", "Repair berths keep boats in working condition.");
                yield return Move(repairBoat,repairHome,repairHome+Vector3.forward*.09f);
                repairMarker.gameObject.SetActive(true); repairTool.gameObject.SetActive(true); repairWorker.SetActive(true);
                for(float t=0;t<1.6f;t+=Time.deltaTime) { repairTool.localRotation=Quaternion.Euler(0,0,Mathf.Sin(t*12)*25); workerArm.localRotation=Quaternion.Euler(Mathf.Sin(t*12)*30,0,-35); yield return null; }
                repairMarker.GetComponent<Renderer>().sharedMaterial=repairGreen;
                yield return new WaitForSeconds(.4f);
                repairMarker.gameObject.SetActive(false); repairTool.gameObject.SetActive(false); repairWorker.SetActive(false);
                yield return Move(repairBoat,repairBoat.localPosition,repairHome);
            }
            visited[area]=true; SetAreaLabel(area);
            runningAnimation=null; ResetModels(); RefreshUI();
            if(CurrentStage==Stage.Explore) Say("<b>EXPLORE AT YOUR OWN PACE</b>\nRead both documents, then select FIND WORKPLACE.");
        }
        private void SelectCrate()
        {
            if(CurrentStage!=Stage.Cargo && CurrentStage!=Stage.Selected) return;
            CloseReader(); StopAnimation(); CurrentStage=Stage.Selected;
            crate.GetComponent<Renderer>().sharedMaterial=cyan;
            ShowTask("CRATE SELECTED", "Select LOAD HERE on the cargo ship.");
        }
        private void Load()
        {
            if(CurrentStage!=Stage.Selected) return;
            StopAnimation(); CurrentStage=Stage.Loading; formalLoading=true;
            runningAnimation=StartCoroutine(Loading()); RefreshUI();
        }
        private IEnumerator Loading()
        {
            yield return null;
            ShowTask("LOADING THE SHIP", "Shore → lift → ship. The crane carries the goods.");
            yield return CargoMotion();
            runningAnimation=null; formalLoading=false; CurrentStage=Stage.Letter; ResetModels();
            ShowTask("WELL DONE!", "Liang found his workplace and completed his first loading task. Open LETTER HOME to reflect.");
        }
        private IEnumerator CargoMotion()
        {
            yield return Move(hook,hook.localPosition,crateHome+Vector3.up*.065f);
            yield return Lift(new Vector3(crateHome.x,1.26f,crateHome.z));
            yield return Lift(new Vector3(.07f,1.26f,.015f));
            yield return Lift(cargoLanding+Vector3.up*.065f);
            yield return Move(hook,hook.localPosition,hookHome);
        }
        private void Skip()
        {
            bool finish=formalLoading; StopAnimation();
            if(finish) { CurrentStage=Stage.Letter; ResetModels(); ShowTask("LOADING COMPLETE", "Liang's first task is done. Open LETTER HOME to finish."); }
            else ShowTask("DEMO STOPPED", "Replay any area or continue your story.");
        }
        private IEnumerator SlideDoor(bool open)
        {
            ferryDoorFace.sharedMaterial=white;
            if(open) ferryOpeningFace.sharedMaterial=ferryDoorYellow;
            Vector3 start=ferryDoor.localPosition;
            Vector3 end=doorClosedPosition+(open ? Vector3.back*.055f : Vector3.zero);
            ferryDoor.localRotation=Quaternion.identity;
            for(float t=0;t<.75f;t+=Time.deltaTime)
            {
                ferryDoor.localPosition=Vector3.Lerp(start,end,Mathf.SmoothStep(0,1,t/.75f));
                yield return null;
            }
            ferryDoor.localPosition=end;
            if(!open) { ferryDoorFace.sharedMaterial=white; ferryOpeningFace.sharedMaterial=white; }
        }
        private IEnumerator Lift(Vector3 end)
        {
            Vector3 start=hook.localPosition;
            float duration=Mathf.Max(.1f,movementSeconds);
            for(float t=0;t<duration;t+=Time.deltaTime) { hook.localPosition=Vector3.Lerp(start,end,Mathf.SmoothStep(0,1,t/duration)); crate.localPosition=hook.localPosition-Vector3.up*.065f; yield return null; }
            hook.localPosition=end; crate.localPosition=end-Vector3.up*.065f;
        }
        private IEnumerator Move(Transform item,Vector3 start,Vector3 end)
        {
            float duration=Mathf.Max(.1f,movementSeconds);
            for(float t=0;t<duration;t+=Time.deltaTime) { item.localPosition=Vector3.Lerp(start,end,Mathf.SmoothStep(0,1,t/duration)); yield return null; }
            item.localPosition=end;
        }
        private void Update()
        {
            if(!model) return;
            cable.SetPosition(0,new Vector3(hook.localPosition.x,1.34f,hook.localPosition.z)); cable.SetPosition(1,hook.localPosition);
            if(Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && Camera.main)
            {
                var ray=Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
                if(Physics.Raycast(ray,out var hit,15) && hit.collider.TryGetComponent<HarbourPrototypeButton>(out var button)) button.Invoke();
            }
        }
        private void OnDisable()
        {
            if(!model || !passengers) return;
            // An interrupted formal lift must remain resumable after the exhibit is re-enabled.
            bool interrupted=formalLoading;
            StopAnimation();
            if(interrupted) { CurrentStage=Stage.Cargo; ResetModels(); ShowTask("READY TO LOAD", "Select the crate to begin the lift again."); }
        }
        private void OnDestroy()
        {
            foreach(var m in materials) if(m) Destroy(m);
            foreach(var mesh in artMeshes) if(mesh) Destroy(mesh);
            foreach(var texture in artTextures) if(texture) Destroy(texture);
        }
    }
}
