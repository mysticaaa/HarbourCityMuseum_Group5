using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace ZxhMuseum
{
    public sealed class CabinEnvironment : MonoBehaviour
    {
        public VoyageState Navigation { get; private set; }
        public Transform Needle { get; private set; }
        public WheelRayControl Wheel { get; private set; }
        public int InteractableCount { get; private set; }
        private readonly List<Material> materials = new List<Material>();
        private WorldPanel instruments, compassView, cabinet;
        public CabinSeaWindow SeaWindow { get; private set; }
        public CabinSideWindow SideWindow { get; private set; }
        public GameObject Sofa { get; private set; }
        public PhysicalPushButton LeaveSeatButton { get; private set; }
        private Transform largeNeedle, drawer;
        private bool drawerOpen;
        private int logPage;
        private AudioSource bell;
        private AudioClip bellClip;
        private float lastBell = -10;
        private AudioSource radio;
        private AudioClip radioClip;
        private WorldPanel radioPanel;
        private WorldPanel logPanel;
        public ObservationService Observation { get; private set; }
        private readonly List<ReturnableTool> tools = new List<ReturnableTool>();
        public EngineCutaway Engine { get; private set; }
        public ShipDeckEnvironment Deck { get; private set; }
        private readonly Color wood = new Color(0.29f, 0.17f, 0.09f);
        private readonly Color brass = new Color(0.7f, 0.48f, 0.14f);
        private void Awake() => Build();
        private GameObject Shape(string name, PrimitiveType primitive, Vector3 position, Vector3 scale, Color color, Transform parent = null)
        {
            var go = GameObject.CreatePrimitive(primitive);
            go.name = name; go.transform.SetParent(parent != null ? parent : transform, false);
            go.transform.localPosition = position; go.transform.localScale = scale;
            var material = new Material(Shader.Find("Standard")); material.color = color;
            material.enableInstancing = true; materials.Add(material);
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }
        private GameObject Box(string name, Vector3 position, Vector3 scale, Color color, Transform parent = null)
            => Shape(name, PrimitiveType.Cube, position, scale, color, parent);
        private void Click(GameObject go, Action selected)
        {
            BoatTriggerAction.Bind(go, selected);
            go.AddComponent<DesktopObjectClick>().Selected = selected;
            InteractableCount++;
        }
        private WorldPanel Panel(string name, Vector3 position, float yaw = 180, float width = 800)
            => WorldPanel.Create(transform, name, position, Quaternion.Euler(0, yaw, 0), width);
        private void Build()
        {
            WorldPanel.EnsureEventSystem();
            Observation = gameObject.AddComponent<ObservationService>();
            Navigation = gameObject.AddComponent<VoyageState>();
            var floor = Box("Cabin floor - teleport surface", new Vector3(0, -0.1f, 0), new Vector3(4, 0.2f, 7), wood);
            floor.AddComponent<TeleportationArea>();
            // Right when facing the bow (local -Z). Leave a real opening through the wall.
            Box("Right wall forward", new Vector3(-2,1.05f,-1.95f), new Vector3(.15f,2.1f,3.1f),wood);
            Box("Right wall aft", new Vector3(-2,1.05f,2.15f), new Vector3(.15f,2.1f,2.7f),wood);
            Box("Right window sill", new Vector3(-2,.55f,.2f), new Vector3(.15f,1.1f,1.2f),wood);
            Box("Right window lintel", new Vector3(-2,2.04f,.2f), new Vector3(.15f,.12f,1.2f),wood);
            Box("Starboard bulkhead", new Vector3(2, 1.05f, 0), new Vector3(0.15f, 2.1f, 7), wood);
            Box("Forward window port frame", new Vector3(-1.6f, 1.05f, -3.5f), new Vector3(.8f, 2.1f, .15f), wood);
            Box("Forward window starboard frame", new Vector3(1.6f, 1.05f, -3.5f), new Vector3(.8f, 2.1f, .15f), wood);
            Box("Forward window sill", new Vector3(0, .55f, -3.5f), new Vector3(2.4f, 1.1f, .15f), wood);
            Box("Forward window lintel", new Vector3(0, 2.05f, -3.5f), new Vector3(2.4f, .1f, .15f), wood);
            Box("Entrance bulkhead port", new Vector3(-1.9f, 1.05f, 3.5f), new Vector3(.2f, 2.1f, 0.15f), new Color(.35f,.36f,.33f));
            Box("Entrance bulkhead starboard", new Vector3(.7f, 1.05f, 3.5f), new Vector3(2.6f, 2.1f, 0.15f), new Color(.35f,.36f,.33f));
            Box("Entrance lintel", new Vector3(-1.2f, 2.07f, 3.5f), new Vector3(1.2f, 0.16f, 0.15f), wood);
            Box("Ceiling", new Vector3(0, 2.15f, 0), new Vector3(4, 0.1f, 7), new Color(0.75f, 0.72f, 0.63f));
            // Keep the original model's transparent glazing and actual sea view; no opaque blue placeholder window.
            var light = new GameObject("Cabin ambient light").AddComponent<Light>();
            light.transform.SetParent(transform); light.type = LightType.Point;
            light.transform.localPosition = new Vector3(0, 2.1f, 0); light.range = 9; light.intensity = 0.9f;
            light.shadows = LightShadows.None;
            for (int i = 0; i < 13; i++)
            {
                var seam = Box("Floor plank seam", new Vector3(-1.8f + i * 0.3f, 0.004f, 0),
                    new Vector3(0.009f, 0.005f, 6.9f), new Color(0.09f, 0.06f, 0.04f));
                seam.GetComponent<Collider>().enabled = false;
            }
            for (int i = 0; i < 5; i++)
            {
                var beam = Box("Overhead frame", new Vector3(0, 2.04f, -2.8f + i * 1.4f), new Vector3(3.9f, 0.12f, 0.09f), wood);
                beam.GetComponent<Collider>().enabled = false;
            }
            Box("Helm console", new Vector3(0, 0.7f, -2.65f), new Vector3(2.4f, 1.2f, 0.7f), wood);
            BuildWheel(); BuildCompass(); BuildSeating(); BuildCabinet(); BuildStories(); BuildBell(); BuildRadio();
            var windowMaker=gameObject.AddComponent<PhysicalBoatControls>();
            SeaWindow=gameObject.AddComponent<CabinSeaWindow>();SeaWindow.Initialize(windowMaker);
            SideWindow=gameObject.AddComponent<CabinSideWindow>();SideWindow.Initialize(windowMaker);
            var engineRoot = new GameObject("ZXH_EngineTeachingDiagram");
            engineRoot.transform.SetParent(transform, false); engineRoot.transform.localPosition = new Vector3(1.4f, 0.8f, -0.65f);
            Engine = engineRoot.AddComponent<EngineCutaway>();
            instruments = Panel("Helm", new Vector3(.9f, 1.6f, -2.2f), 180, 600);
            instruments.Button("Left", () => Navigation.SetWheel(Navigation.WheelAngle - 10));
            instruments.Button("Centre", () => Navigation.SetWheel(0));
            instruments.Button("Right", () => Navigation.SetWheel(Navigation.WheelAngle + 10));
            var engineControls = Panel("Engine", new Vector3(-.9f, 1.6f, -2.2f), 180, 600);
            engineControls.Status.text = "Trigger: operate. Stop before changing direction.";
            engineControls.Button("Ahead", () => { if (Navigation.SetGear(1)) Navigation.SetPower(.6f); });
            engineControls.Button("Stop", () => { Navigation.SetPower(0); Navigation.SetGear(0); });
            engineControls.Button("Astern", () => { if (Navigation.SetGear(-1)) Navigation.SetPower(.4f); });
            var chart = Panel("Routes", new Vector3(-1.7f, 1.5f, -1f), 270, 600);
            chart.Status.text = "Schematic routes, not a real navigation chart.\nReach target within 10 degrees for 3 seconds.";
            chart.Button("East harbour: 090 degrees", () => Navigation.BeginRoute(90));
            chart.Button("South quay: 180 degrees", () => Navigation.BeginRoute(180));
            chart.Button("West harbour: 270 degrees", () => Navigation.BeginRoute(270));
            chart.Button("Skip / cancel task", Navigation.CancelTask);
            chart.Button("Reset navigation", Navigation.ResetState);
            var exit = Panel("Cabin entrance / tour controls", new Vector3(.9f, 1.35f, 3.3f), 0, 600);
            exit.Status.text = "Point + trigger. Grip to hold tools.";
            exit.Button("Return to Museum", () => MuseumSession.Instance?.RequestReturn());
            var deckRoot = new GameObject("ZXH_WalkableFishingBoatDeck");
            deckRoot.transform.SetParent(transform, false);
            transform.position = new Vector3(0, 2.1f, 5.2f);
            transform.rotation = Quaternion.Euler(0, 180, 0);
            deckRoot.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            Deck = deckRoot.AddComponent<ShipDeckEnvironment>();
            Deck.Initialize(this);
            exit.Button("Go outside to deck", () => Deck.Travel(true));
            exit.Button("Reset cabin", ResetCabin);
            exit.Button("Audio on / off", () => AudioListener.volume = AudioListener.volume > 0 ? 0 : 1);
            AddTeleportAnchor("Helm approach", new Vector3(0, 0.01f, -1.2f));
            AddTeleportAnchor("Living area approach", new Vector3(0, 0.01f, 0.6f));
            // Fixed walls, hull and mast are scenery with collision, not selectable exhibits.
            foreach (var interactable in GetComponentsInChildren<XRBaseInteractable>())
            {
                var record = interactable.gameObject.AddComponent<InteractionRecord>();
                record.assetId = "zxh.cabin." + interactable.name.Replace(" ", "-").ToLowerInvariant();
                record.category = interactable is ReturnableTool ? "Grabbable tool" : interactable is WheelRayControl ? "Single-axis control" : interactable is BaseTeleportationInteractable ? "Travel" : "Observation";
                record.description = interactable.name;
                var origin = MuseumSession.Instance != null ? MuseumSession.Instance.Origin : null;
                var ray = origin != null ? Array.Find(origin.GetComponentsInChildren<XRBaseInteractor>(),
                    candidate => candidate.isActiveAndEnabled && candidate.interactionManager != null) : null;
                var manager = ray != null ? ray.interactionManager :
                    Array.Find(FindObjectsOfType<XRInteractionManager>(), candidate => candidate.isActiveAndEnabled);
                if (manager != null) interactable.interactionManager = manager;
                if (!(interactable is BaseTeleportationInteractable) &&
                    (interactable is WheelRayControl || ExhibitInspectionHint.GetBounds(interactable.transform).size.magnitude < 1))
                    interactable.gameObject.AddComponent<ExhibitHighlight>();
            }
        }
        private void AddTeleportAnchor(string title, Vector3 position)
        {
            var marker = Shape(title, PrimitiveType.Cylinder, position, new Vector3(0.38f, 0.005f, 0.38f), new Color(0.1f, 0.45f, 0.45f));
            var anchor = marker.AddComponent<TeleportationAnchor>(); anchor.teleportAnchorTransform = marker.transform;
        }
        private void BuildWheel()
        {
            var root = new GameObject("ZXH_WheelAxis"); root.transform.SetParent(transform, false);
            root.transform.localPosition = new Vector3(0, 1.25f, -2.2f);
            var visual = new GameObject("WheelVisual").transform; visual.SetParent(root.transform, false);
            var hub = Shape("Wheel hub", PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.12f, brass, visual);
            hub.GetComponent<Collider>().enabled = false;
            for (int i = 0; i < 12; i++)
            {
                float angle = i * 30 * Mathf.Deg2Rad;
                var rim = Box("Wheel rim segment", new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * 0.3f,
                    new Vector3(0.17f, 0.045f, 0.06f), brass, visual);
                rim.transform.localRotation = Quaternion.Euler(0, 0, i * 30 + 90);
                if (i % 3 == 0)
                {
                    var spoke = Box("Wheel spoke", new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * 0.15f,
                        new Vector3(0.3f, 0.035f, 0.045f), brass, visual);
                    spoke.transform.localRotation = Quaternion.Euler(0, 0, i * 30);
                }
            }
            // Select the actual rim/spoke geometry, not an invisible sphere covering its holes.
            Wheel = root.AddComponent<WheelRayControl>(); Wheel.navigation = Navigation; Wheel.wheelVisual = visual;
            InteractableCount++;
        }
        private Transform CompassNeedle(Transform parent, Vector3 position, float scale)
        {
            var pivot = new GameObject("North needle axis").transform; pivot.SetParent(parent, false);
            pivot.localPosition = position; pivot.localScale = Vector3.one * scale;
            var visual = new GameObject("Compass card visual").transform; visual.SetParent(pivot, false);
            // The helm looks along cabin -Z. Keep the data axis at zero and align its visual north with the bow.
            visual.localRotation = Quaternion.Euler(0, 180, 0);
            var north = Box("North needle", new Vector3(0, 0, 0.075f), new Vector3(0.018f, 0.012f, 0.15f), Color.red, visual);
            var south = Box("South needle", new Vector3(0, 0, -0.075f), new Vector3(0.018f, 0.012f, 0.15f), Color.white, visual);
            north.GetComponent<Collider>().enabled = south.GetComponent<Collider>().enabled = false;
            var bowMark = Box("Fixed bow reference", position + new Vector3(0, .005f, -.17f * scale),
                new Vector3(.016f, .005f, .035f) * scale, Color.black, parent);
            bowMark.GetComponent<Collider>().enabled = false;
            string[] labels = { "N", "E", "S", "W" };
            for (int i = 0; i < 4; i++)
            {
                float angle = i * 90 * Mathf.Deg2Rad;
                var label = new GameObject(labels[i], typeof(TextMesh)); label.transform.SetParent(visual, false);
                label.transform.localPosition = new Vector3(Mathf.Sin(angle), 0.01f, Mathf.Cos(angle)) * 0.14f;
                label.transform.localRotation = Quaternion.Euler(90, 0, 0);
                var text = label.GetComponent<TextMesh>(); text.text = labels[i]; text.anchor = TextAnchor.MiddleCenter;
                text.fontSize = 36; text.characterSize = 0.012f; text.color = Color.black;
            }
            return pivot;
        }
        private void BuildCompass()
        {
            var compass = Shape("Compass - select to enlarge", PrimitiveType.Cylinder, new Vector3(-0.55f, 1.34f, -2.5f),
                new Vector3(0.35f, 0.04f, 0.35f), brass);
            Needle = CompassNeedle(transform, new Vector3(-0.55f, 1.4f, -2.5f), 1);
            compassView = Panel("Compass inspection", new Vector3(-1.45f, 1.6f, -0.4f), 270);
            compassView.Status.text = "Red N marks north relative to simulated ship heading.\nThe dial turns with heading; the cabin stays still.";
            compassView.Button("Close magnified compass", () => compassView.gameObject.SetActive(false));
            compassView.Button("Show North / 000", () => Navigation.SetHeadingForDemonstration(0));
            compassView.Button("Show East / 090", () => Navigation.SetHeadingForDemonstration(90));
            var enlarged = new GameObject("Magnified compass visual").transform; enlarged.SetParent(compassView.transform, false);
            // Canvas is millimetre-scaled; compensate so this is a 0.45 metre display.
            enlarged.localPosition = new Vector3(0, -450, -80); enlarged.localScale = Vector3.one * 1000;
            Shape("Magnified compass dial", PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.5f, 0.02f, 0.5f), brass, enlarged);
            largeNeedle = CompassNeedle(enlarged, Vector3.up * 0.035f, 1.5f);
            Click(compass, () => compassView.gameObject.SetActive(true));
            compassView.gameObject.SetActive(false);
        }
        private void BuildSeating()
        {
            Sofa = Box("Sofa - virtual seating", new Vector3(1.3f, 0.45f, 0.7f), new Vector3(0.9f, 0.35f, 1.8f), new Color(0.16f, 0.28f, 0.32f));
            Box("Sofa back", new Vector3(1.75f, 0.8f, 0.7f), new Vector3(0.18f, 0.8f, 1.8f), new Color(0.16f, 0.28f, 0.32f));
            Click(Sofa, () => MuseumSession.Instance?.Sit(transform.TransformPoint(new Vector3(1.1f, 1.2f, 0.7f)), transform.eulerAngles.y + 270));
            var maker=gameObject.AddComponent<PhysicalBoatControls>();
            maker.Part("Sofa leave button support",PrimitiveType.Cube,new Vector3(.81f,.55f,.5f),new Vector3(.15f,.38f,.32f),new Color(.18f,.22f,.24f));
            LeaveSeatButton=maker.Button("Sofa leave button",new Vector3(.75f,.9f,.5f),Quaternion.Euler(0,270,0),"LEAVE SEAT",()=>MuseumSession.Instance?.LeaveSeat());
            maker.Part("Sofa instruction backing",PrimitiveType.Cube,new Vector3(.8f,.68f,1.15f),new Vector3(.1f,.17f,.46f),new Color(.18f,.22f,.24f));
            var sign=new GameObject("Sofa instruction plate").transform;sign.SetParent(transform,false);
            sign.localPosition=new Vector3(.74f,.68f,1.15f);sign.localRotation=Quaternion.Euler(0,90,0);
            maker.Label(sign,"VIRTUAL SEAT\nPoint + trigger to sit",Vector3.zero,.02f,.4f,.11f);
        }
        private void BuildCabinet()
        {
            Box("Cabinet back", new Vector3(-1.82f, 0.55f, 1.7f), new Vector3(0.07f, 1.1f, 0.7f), wood);
            Box("Cabinet top", new Vector3(-1.45f, 1.08f, 1.7f), new Vector3(0.75f, 0.06f, 0.7f), wood);
            Box("Cabinet base", new Vector3(-1.45f, 0.1f, 1.7f), new Vector3(0.75f, 0.1f, 0.7f), wood);
            Box("Cabinet side A", new Vector3(-1.45f, 0.55f, 1.38f), new Vector3(0.75f, 1.1f, 0.06f), wood);
            Box("Cabinet side B", new Vector3(-1.45f, 0.55f, 2.02f), new Vector3(0.75f, 1.1f, 0.06f), wood);
            drawer = Box("Drawer handle", new Vector3(-1, 0.8f, 1.7f), new Vector3(0.08f, 0.1f, 0.4f), brass).transform;
            // Geometry is in world metres, independent of the handle's small primitive scale.
            var tray = Box("Sliding drawer tray", new Vector3(-1.35f, 0.75f, 1.7f), new Vector3(0.6f, 0.08f, 0.5f), wood);
            tray.transform.SetParent(drawer, true);
            var drawerFront = Box("Drawer front", new Vector3(-1.03f, 0.8f, 1.7f), new Vector3(0.06f, 0.25f, 0.58f), wood);
            drawerFront.transform.SetParent(drawer, true);
            Click(drawer.gameObject, ToggleDrawer);
            for (int i = 0; i < 2; i++)
            {
                var item = new GameObject(i == 0 ? "Wrench schematic" : "Gauge schematic");
                item.transform.SetParent(transform, false);
                item.transform.localPosition = new Vector3(-1.2f, 0.86f, 1.6f + i * 0.2f);
                if (i == 0)
                {
                    Box("Wrench handle", Vector3.zero, new Vector3(0.022f, 0.016f, 0.13f), brass, item.transform);
                    Box("Open jaw bridge", new Vector3(0, 0, 0.06f), new Vector3(0.08f, 0.016f, 0.02f), brass, item.transform);
                    Box("Open jaw left", new Vector3(-0.03f, 0, 0.085f), new Vector3(0.02f, 0.016f, 0.06f), brass, item.transform);
                    Box("Open jaw right", new Vector3(0.03f, 0, 0.085f), new Vector3(0.02f, 0.016f, 0.06f), brass, item.transform);
                }
                else
                {
                    Shape("Gauge dial", PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.1f, 0.014f, 0.1f), brass, item.transform);
                    Box("Gauge indicator", new Vector3(0, 0.018f, 0.016f), new Vector3(0.006f, 0.005f, 0.035f), Color.red, item.transform);
                }
                item.transform.SetParent(drawer, true);
                item.AddComponent<Rigidbody>();
                var toolGrab = item.AddComponent<ReturnableTool>();
                toolGrab.CanTake = () => drawerOpen && Mathf.Abs(drawer.localPosition.x + 0.8f) < 0.01f;
                tools.Add(toolGrab); InteractableCount++;
            }
            cabinet = Panel("Equipment / sources", new Vector3(-1.1f, 1.45f, 1.7f), 270, 650);
            cabinet.Status.text = "Schematic equipment.\nA valve regulates flow; a gauge reports a measurement.\nNot a real maintenance procedure.";
            cabinet.Button("Open / close drawer", ToggleDrawer);
            cabinet.Button("Inspect valve / gauge", () => cabinet.Status.text = "Teaching detail: compare an operating control with a measuring instrument.\nThe placeholder shapes are not a historical reconstruction.");
            cabinet.Button("Reset drawer", () =>
            {
                MuseumSession.Instance?.CancelCurrentInteraction();
                foreach (var item in tools) item.ResetPose();
                drawerOpen = false; drawer.localPosition = new Vector3(-1, 0.8f, 1.7f);
            });
            cabinet.Button("Reset tools", () => { MuseumSession.Instance?.CancelCurrentInteraction(); foreach (var item in tools) item.ResetPose(); });
            var tool = Shape("Valve teaching model", PrimitiveType.Cylinder, new Vector3(-1.45f, 1.15f, 1.7f), new Vector3(0.2f, 0.06f, 0.2f), brass);
            Click(tool, () => cabinet.Status.text = "Valve: a control for opening, closing or regulating a flow.\nTeaching model, not a documented ship component.");
        }
        private void ToggleDrawer()
        {
            foreach (var item in tools)
                if (item.isSelected || item.Returning) { cabinet.Status.text = "Release the tool and let it return before moving the drawer."; return; }
            drawerOpen = !drawerOpen;
            cabinet.Status.text = drawerOpen ? "Drawer open. Click again to close. Travel is limited to 0.2 m." : "Drawer closed.";
        }
        private void BuildStories()
        {
            var log = Panel("Voyage log", new Vector3(0.1f, 1.35f, 1.65f), 0, 650);
            logPanel = log;
            string[] pages = { "1 / 3 - Arrival\nA teaching crew prepares for a harbour journey. This is fictional demonstration content.",
                "2 / 3 - Instruments\nSelect a route, read the compass and steer. The vessel is fixed for comfort.",
                "3 / 3 - Reflection\nHow did workers share space and equipment? Compare the cabin with today's workplaces." };
            Action refresh = () => log.Status.text = pages[logPage]; refresh();
            log.Button("Previous page", () => { logPage = Mathf.Max(0, logPage - 1); refresh(); });
            log.Button("Next page", () => { logPage = Mathf.Min(2, logPage + 1); refresh(); });
        }
        private void BuildRadio()
        {
            var objectRadio = Box("Teaching radio", new Vector3(1.65f, 1.1f, 1.8f), new Vector3(0.25f, 0.18f, 0.2f), new Color(0.1f, 0.12f, 0.13f));
            radio = objectRadio.AddComponent<AudioSource>(); radio.loop = true; radio.spatialBlend = 1; radio.volume = 0.08f;
            const int rate = 22050; float[] samples = new float[rate * 3];
            var random = new System.Random(5424);
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)rate;
                samples[i] = (float)(random.NextDouble() - 0.5) * (0.08f + 0.04f * Mathf.Sin(t * 2 * Mathf.PI / 3));
            }
            radioClip = AudioClip.Create("Synthetic harbour ambience", samples.Length, 1, rate, false);
            radioClip.SetData(samples, 0); radio.clip = radioClip;
            radioPanel = Panel("Radio / sound", new Vector3(1.25f, 1.4f, 1.8f), 90, 550);
            radioPanel.Status.text = "Synthetic ambience, not a historical recording.\nAll information is available as text.";
            radioPanel.Button("Environment channel", () => { radio.Play(); radioPanel.Status.text = "Synthetic harbour ambience playing."; });
            radioPanel.Button("Interpretation channel", () => { radio.Stop(); radioPanel.Status.text = "Crews use instruments, logs and shared equipment.\nThis textual channel remains available with audio muted."; });
            radioPanel.Button("Off / close", () => { radio.Stop(); radioPanel.gameObject.SetActive(false); });
            Click(objectRadio, () => radioPanel.gameObject.SetActive(true)); radioPanel.gameObject.SetActive(false);
        }
        private void BuildBell()
        {
            var bellObject = Shape("Ship bell", PrimitiveType.Cylinder, new Vector3(1.55f, 1.55f, 2.2f), new Vector3(0.18f, 0.15f, 0.18f), brass);
            bell = bellObject.AddComponent<AudioSource>(); bell.spatialBlend = 1; bell.volume = 0.25f;
            const int rate = 22050; var samples = new float[rate];
            for (int i = 0; i < samples.Length; i++)
            { float t = i / (float)rate; samples[i] = (Mathf.Sin(t * 660 * Mathf.PI * 2) + Mathf.Sin(t * 990 * Mathf.PI * 2) * 0.3f) * Mathf.Exp(-t * 5) * 0.4f; }
            bellClip = AudioClip.Create("Teaching bell tone", samples.Length, 1, rate, false); bellClip.SetData(samples, 0);
            Click(bellObject, () =>
            {
                if (Time.time - lastBell < 1) return;
                lastBell = Time.time; bell.PlayOneShot(bellClip);
                instruments.Status.text = Navigation.Completed ? "Watch complete. Your teaching voyage card is earned." : "Ship bell rung. Sound is a teaching cue, not a historical signal.";
            });
        }
        public void ResetCabin()
        {
            MuseumSession.Instance?.CancelCurrentInteraction();
            Observation.Close();
            Engine.ResetDiagram();
            Deck.ResetDeck();
            MuseumSession.Instance?.Narrator.Stop();
            foreach (var item in tools) item.ResetPose();
            Navigation.ResetState(); MuseumSession.Instance?.LeaveSeat();
            drawerOpen = false; drawer.localPosition = new Vector3(-1, 0.8f, 1.7f);
            compassView.gameObject.SetActive(false); SeaWindow.Close(); SideWindow.Close(); logPage = 0;
            logPanel.Status.text = "1 / 3 - Arrival\nA teaching crew prepares for a harbour journey. This is fictional demonstration content.";
            radio.Stop(); radioPanel.gameObject.SetActive(false);
        }
        private void Update()
        {
            if (Navigation == null) return;
            if(LeaveSeatButton!=null)LeaveSeatButton.SetIndicator(MuseumSession.Instance!=null && MuseumSession.Instance.Seated);
            drawer.localPosition = Vector3.MoveTowards(drawer.localPosition,
                new Vector3(drawerOpen ? -0.8f : -1, 0.8f, 1.7f), Time.deltaTime * 0.4f);
            Needle.localRotation = Quaternion.AngleAxis(-Navigation.Heading, Vector3.up);
            largeNeedle.localRotation = Needle.localRotation;
            if (compassView.gameObject.activeSelf)
                compassView.Status.text = "Virtual heading: " + Navigation.Heading.ToString("000") + " degrees\nRed N points north relative to the simulated ship.\nThe room does not move.";
            instruments.Status.text = "Heading " + Navigation.Heading.ToString("000") + " | Wheel " + Navigation.WheelAngle.ToString("F0") +
                "\nPower " + (Navigation.Power * 100).ToString("F0") + "% | Speed " + Navigation.Speed.ToString("F1") +
                "\nTarget " + Navigation.Target.ToString("000") + " | Hold " + Navigation.HoldTime.ToString("F1") + "/3s\n" + Navigation.Notice;
        }
        private void OnDestroy()
        {
            foreach (var material in materials) if (material != null) Destroy(material);
            if (bellClip != null) Destroy(bellClip);
            if (radioClip != null) Destroy(radioClip);
        }
    }
}
