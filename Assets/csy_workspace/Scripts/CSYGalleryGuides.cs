using System.Collections.Generic;
using System.Linq;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using ZxhMuseum;
using ZzxWorkspace.FirstDay;

namespace CsyMuseum
{
    // A single additive scene adapter. Existing exhibits, bindings and placement remain owned by teammates.
    public sealed class CSYGalleryGuides : MonoBehaviour
    {
        public AudioClip helmetAudio, boatAudio, storyAudio;
        public Vector3 helmetCard = new Vector3(-4.65f, 1.75f, -.08f);
        public Vector3 boatCard = new Vector3(3.5f, 1.75f, -.6f);
        public Vector3 storyCard = new Vector3(-8.5f, 1.75f, -.25f);
        public CSYGuideCoordinator Coordinator { get; private set; }
        private readonly Dictionary<GameObject, bool> hiddenHints = new Dictionary<GameObject, bool>();
        private readonly List<Transform> guidedModels = new List<Transform>();

        private void Start()
        {
            var rig = FindObjectsOfType<XROrigin>().FirstOrDefault(x => x.gameObject.scene == gameObject.scene && x.Camera && x.Camera.isActiveAndEnabled);
            if (!rig) { Debug.LogError("[CSY E] No active gallery XR camera.", this); enabled = false; return; }
            var manager = rig.GetComponentsInChildren<XRBaseInteractor>(true).Select(x => x.interactionManager).FirstOrDefault(x => x && x.isActiveAndEnabled);
            if (!manager) manager = FindObjectsOfType<XRInteractionManager>().FirstOrDefault(x => x.gameObject.scene == gameObject.scene);
            if (!manager) { Debug.LogError("[CSY E] No gallery interaction manager.", this); enabled = false; return; }
            Coordinator = gameObject.AddComponent<CSYGuideCoordinator>(); Coordinator.visitorCamera = rig.Camera;
            var helmet = FindObjectsOfType<HelmetHandInspection>().FirstOrDefault(x => x.gameObject.scene == gameObject.scene);
            var boat = FindObjectsOfType<VensailCabinEntry>().FirstOrDefault(x => x.gameObject.scene == gameObject.scene);
            var story = FindObjectsOfType<HarbourTabletopPrototype>().FirstOrDefault(x => x.gameObject.scene == gameObject.scene);
            if (helmet)
            {
                var guide = Add("Diving helmet", helmet.transform, helmetCard, helmetAudio);
                guide.inspectionObject = helmet.GetComponent<XRGrabInteractable>();
                guide.transcript = "The viewing window lets a diver see while keeping water out. This model is an observation aid; its exact historical provenance is not established.";
                guide.Initialize(Coordinator, manager);
            }
            if (boat)
            {
                var guide = Add("Fishing boat", boat.transform, boatCard, boatAudio);
                guide.inspectionTask = false;
                guide.instruction = "Point at the boat and use Trigger to request boarding. Choose Enter ship or Cancel.";
                guide.transcript = "Compare the display model with its explorable deck and cabin. The interior is a teaching reconstruction, not a certified vessel simulation.";
                guide.Initialize(Coordinator, manager);
            }
            if (story)
            {
                var guide = Add("First Day in the Harbour", story.transform, storyCard, storyAudio);
                guide.inspectionTask = false;
                guide.instruction = "Select START. Read the pass and letter, then follow the board. You can explore or retry.";
                guide.transcript = "Find the workplace and complete a small loading task. You can retry or restart. This fictional port story is an interpretive reconstruction.";
                guide.Initialize(Coordinator, manager);
            }
            if (Coordinator.Guides.Count != 3) Debug.LogError("[CSY E] Expected helmet, active boat and First Day exhibits.", this);
            SuppressOldHints();
        }

        private CSYExhibitGuide Add(string title, Transform model, Vector3 position, AudioClip audio)
        {
            var go = new GameObject("CSY Guide - " + title); go.transform.SetParent(transform, false);
            go.transform.SetPositionAndRotation(position, Quaternion.identity);
            var guide = go.AddComponent<CSYExhibitGuide>(); guide.exhibitName = title; guide.narration = audio;
            guide.displayModel = model;
            var anchor = new GameObject("Fixed visit region").transform; anchor.SetParent(go.transform, false);
            anchor.position = ExhibitInspectionHint.GetBounds(model).center;
            guide.rangeAnchor = anchor; guidedModels.Add(model); Coordinator.Guides.Add(guide);
            if (!audio) Debug.LogError("[CSY E] Missing clip for " + title, this);
            return guide;
        }
        private void SuppressOldHints()
        {
            foreach (var hint in FindObjectsOfType<ExhibitInspectionHint>(true))
                if (hint.Source && guidedModels.Contains(hint.Source) && !hiddenHints.ContainsKey(hint.gameObject))
                { hiddenHints[hint.gameObject] = hint.gameObject.activeSelf; hint.gameObject.SetActive(false); }
        }
        private void OnEnable()
        {
            if (!Coordinator) return;
            Coordinator.enabled = true; SuppressOldHints();
        }
        private void OnDisable()
        {
            if (Coordinator) Coordinator.enabled = false;
            foreach (var entry in hiddenHints) if (entry.Key) entry.Key.SetActive(entry.Value);
            hiddenHints.Clear();
        }
    }
}
