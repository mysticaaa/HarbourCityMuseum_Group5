using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

namespace ZzxWorkspace.FirstDay
{
    // Builds only exhibit-local geometry; borrows the existing interactable's manager.
    public sealed class HarbourTabletopPrototype : MonoBehaviour
    {
        public const string TargetScene = "Assets/261122/Museum_261122_1.unity";
        public enum Stage { Idle, Pass, Explore, Cargo, Selected, Loading, Letter, Complete }
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
        private Coroutine ferryMotion;
        private bool tourBusy;
        private readonly bool[] visited = new bool[3];
        private readonly HarbourPrototypeButton[] areaButtons = new HarbourPrototypeButton[3];
        private Transform repairBoat, cargoBoat;
        private Vector3 repairHome, cargoHome;
        private const string LetterText = "LETTER HOME\n\"I arrived by ferry. Tomorrow I start work at Berth 02.\"";
        private Transform repairMarker;

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
                "Collection", "Introduction", "Instructions", "Fiction notice" })
            { var t = transform.Find(name); if (t) t.gameObject.SetActive(false); }
            SetText("Introduction", "A first day at the harbour. Help Liang report for work.");
            SetText("Instructions", "POINT + TRIGGER / Explore, check in, then load one crate.");
            SetText("Progress", ""); SetText("Task feedback", "");
            Build(); ResetExperience();
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
            white = Mat(new Color(.88f,.86f,.77f)); dark = Mat(new Color(.06f,.12f,.16f)); cyan = Mat(new Color(.1f,.75f,.8f));
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
            crateHome = new Vector3(.17f,1.02f,.22f);
            crate = Box("Cargo crate",crateHome,new Vector3(.075f,.075f,.075f),wood);
            var crateButton = Button("Select cargo", "CRATE", new Vector3(.17f,1.08f,.20f),new Vector3(.16f,.12f,.12f),SelectCrate);
            crateButton.transform.Find("Button face").GetComponent<Renderer>().enabled = false;
            target = Button("Boat target", "LOAD HERE",new Vector3(.07f,1.04f,-.015f),new Vector3(.22f,.09f,.12f),Load);
            repairMarker = Box("Repair status marker",new Vector3(.60f,1.08f,.03f),new Vector3(.12f,.025f,.06f),cyan);
            repairMarker.gameObject.SetActive(false);
            message = Label("Narration subtitles","",new Vector3(0,1.58f,.32f),new Vector2(1.76f,.38f),.044f);
            Box("Subtitle backing",new Vector3(0,1.58f,.35f),new Vector3(1.90f,.43f,.018f),dark);
            Button("Main action","START / REPORT",new Vector3(-.48f,1.26f,.30f),new Vector3(.64f,.12f,.035f),Next);
            Button("Skip","SKIP LIFT",new Vector3(.48f,1.26f,.30f),new Vector3(.64f,.12f,.035f),Skip);
            Button("Work pass","WORK PASS",new Vector3(-.60f,.72f,-.32f),new Vector3(.50f,.12f,.04f),ReadPass);
            Button("Letter","LETTER HOME",new Vector3(0,.72f,-.32f),new Vector3(.50f,.12f,.04f),ReadLetter);
            Button("Reset","RESTART",new Vector3(.60f,.72f,-.32f),new Vector3(.50f,.12f,.04f),ResetExperience);
            Label("Story notice", "A fictional harbour story", new Vector3(0,.51f,-.35f),new Vector2(1.75f,.09f),.028f);
        }
        public void ResetExperience()
        {
            StopAllCoroutines(); ferryMotion=null; tourBusy=false; CurrentStage = Stage.Idle;
            for(int i=0;i<3;i++) { visited[i]=false; areaButtons[i].suggested=false; }
            repairBoat.localPosition=repairHome; cargoBoat.localPosition=cargoHome;
            repairMarker.gameObject.SetActive(false);
            crate.GetComponent<Renderer>().sharedMaterial=wood;
            crate.localPosition = crateHome; hook.localPosition = new Vector3(.17f,1.28f,.20f);
            ferry.localPosition=ferryHome; target.SetActive(false);
            Say("YOUR FIRST DAY\nSelect START, then open WORK PASS.");
        }
        private void Say(string text) { message.text = text; }
        private void Next()
        {
            if(CurrentStage == Stage.Idle) { CurrentStage=Stage.Pass; Say("WHERE DO I GO?\nOpen WORK PASS to find your workplace."); }
            else if(CurrentStage == Stage.Explore && !tourBusy && AllVisited()) { ClearSuggestions(); CurrentStage=Stage.Cargo; Say("READY FOR WORK\nSelect the crate, then LOAD HERE."); }
            else if(CurrentStage == Stage.Explore) { if(!tourBusy) GuideTour(); }
            else if(CurrentStage == Stage.Letter) ReadLetter();
            else Say(CurrentStage == Stage.Pass ? "Select WORK PASS to inspect your destination." : "Explore the model or follow the highlighted loading target.");
        }
        private void ReadPass()
        {
            if(CurrentStage == Stage.Loading || tourBusy) return;
            if(CurrentStage == Stage.Idle || CurrentStage == Stage.Pass) CurrentStage=Stage.Explore;
            if(CurrentStage==Stage.Explore) GuideTour();
            else Say("WORK PASS: BERTH 02\nCargo handling / Liang");
        }
        private bool AllVisited() => visited[0] && visited[1] && visited[2];
        private void ClearSuggestions() { foreach(var button in areaButtons) button.suggested=false; }
        private void GuideTour()
        {
            ClearSuggestions();
            for(int i=0;i<3;i++) if(!visited[i])
            {
                areaButtons[i].suggested=true;
                string[] names={"01 FERRY", "02 CARGO", "03 REPAIR"};
                Say("DISCOVER THE HARBOUR " + (i+1) + "/3\nSelect " + names[i] + " to see what happens here.");
                return;
            }
            Say("ALL THREE AREAS EXPLORED\nYour workplace is CARGO. Select REPORT.");
        }
        private void Explore(int area)
        {
            if(CurrentStage == Stage.Loading || tourBusy) return;
            if(CurrentStage==Stage.Idle || CurrentStage==Stage.Pass)
            { Say("FIRST, OPEN WORK PASS\nThen explore all three harbour areas."); return; }
            tourBusy=true; ClearSuggestions();
            ferryMotion=StartCoroutine(VisitArea(area));
        }
        private IEnumerator VisitArea(int area)
        {
            if(area==0)
            {
                Say("FERRY / ARRIVING IN THE CITY\nWatch the passenger boat approach the pier.");
                yield return Move(ferry,ferry.localPosition,ferryHome+Vector3.back*.09f);
                yield return Move(ferry,ferry.localPosition,ferryHome);
            }
            else if(area==1)
            {
                Say("CARGO / GOODS MOVE THROUGH THE PORT\nWatch the cargo ship arrive for loading.");
                yield return Move(cargoBoat,cargoBoat.localPosition,cargoHome+Vector3.right*.09f);
                yield return Move(cargoBoat,cargoBoat.localPosition,cargoHome);
            }
            else
            {
                Say("REPAIR / KEEPING BOATS WORKING\nWatch the boat move into its repair berth.");
                repairMarker.gameObject.SetActive(true);
                yield return Move(repairBoat,repairBoat.localPosition,repairHome+Vector3.forward*.10f);
                yield return Move(repairBoat,repairBoat.localPosition,repairHome);
            }
            visited[area]=true; tourBusy=false; ferryMotion=null;
            if(CurrentStage==Stage.Explore) GuideTour();
        }
        private void SelectCrate()
        {
            if(tourBusy) return;
            if(CurrentStage!=Stage.Cargo && CurrentStage!=Stage.Selected) { if(CurrentStage!=Stage.Loading) Say("BEFORE YOU LOAD\nOpen WORK PASS, then select REPORT."); return; }
            CurrentStage=Stage.Selected; crate.GetComponent<Renderer>().sharedMaterial=cyan;
            target.SetActive(true); Say("CRATE SELECTED\nSelect LOAD HERE on the ship.");
        }
        private void Load() { if(CurrentStage==Stage.Selected) { CurrentStage=Stage.Loading; target.SetActive(false); StartCoroutine(Loading()); } }
        private IEnumerator Loading()
        {
            Say("LOADING THE SHIP\nThe crane carries the crate from shore to ship.");
            yield return Move(hook,hook.localPosition,crateHome+Vector3.up*.065f);
            yield return Lift(new Vector3(.17f,1.26f,.22f));
            yield return Lift(new Vector3(.07f,1.26f,.015f));
            yield return Lift(new Vector3(.07f,1.085f,.015f));
            yield return Move(hook,hook.localPosition,new Vector3(.07f,1.28f,.015f));
            FinishCargo();
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
        private void FinishCargo()
        {
            crate.localPosition=new Vector3(.07f,1.02f,.015f); hook.localPosition=new Vector3(.07f,1.28f,.015f);
            crate.GetComponent<Renderer>().sharedMaterial=wood;
            CurrentStage=Stage.Letter; Say("WELL DONE! ONE CRATE LOADED\nOpen LETTER HOME to finish your day.");
        }
        private void Skip() { if(CurrentStage==Stage.Loading) { StopAllCoroutines(); ferry.localPosition=ferryHome; FinishCargo(); } }
        private void ReadLetter()
        {
            if(CurrentStage==Stage.Loading || tourBusy) return;
            if(CurrentStage==Stage.Letter || CurrentStage==Stage.Complete)
            {
                CurrentStage=Stage.Complete;
                Say(LetterText + "\nDAY COMPLETE / Explore again or RESTART.");
            }
            else Say(LetterText);
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
        private void OnDestroy() { foreach(var m in materials) if(m) Destroy(m); }
    }
}
