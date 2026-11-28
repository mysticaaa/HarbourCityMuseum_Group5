using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace CsyMuseum
{
    [DisallowMultipleComponent]
    public sealed class CSYExhibitGuide : MonoBehaviour
    {
        public Transform rangeAnchor;
        public Transform displayModel;
        public AudioClip narration;
        public XRGrabInteractable inspectionObject;
        public string exhibitName = "Diving helmet";
        [TextArea] public string instruction = "Look for the viewing window. Hold the helmet to inspect; release to return.";
        [TextArea] public string transcript = "The viewing window lets a diver see while keeping water out of the helmet.";
        [TextArea] public string question = "What is the viewing window for?";
        public string correctAnswer = "Seeing underwater";
        public string otherAnswer = "Storing breathing air";
        public bool inspectionTask = true;
        [Min(.1f)] public float enterRadius = 1.6f;
        [Min(.1f)] public float exitRadius = 1.9f;
        [Min(.1f)] public float maxVertical = 2.2f;
        [Min(0f)] public float dwellSeconds = 1.2f;
        public CSYGuideVisit Visit { get; } = new CSYGuideVisit();
        private readonly CSYHelmetObservation observation = new CSYHelmetObservation();
        public bool TaskComplete => observation.Complete;
        public int Attempts => observation.Attempts;
        public CSYGuideAudioState AudioState { get; private set; }
        public bool Holding => inspectionObject && inspectionObject.isSelected;
        public Vector3 RangePosition => rangeAnchor ? rangeAnchor.position : transform.position;
        public CSYGuideCoordinator Coordinator { get; private set; }
        private CSYGuideCard card;
        private string feedback;

        public void Initialize(CSYGuideCoordinator coordinator, XRInteractionManager manager)
        {
            Coordinator = coordinator;
            card = gameObject.AddComponent<CSYGuideCard>();
            card.Build(this, manager);
            if (inspectionObject)
            {
                inspectionObject.selectEntered.AddListener(OnGrab);
                inspectionObject.selectExited.AddListener(OnRelease);
            }
            Refresh();
        }
        public void Answer(bool correct)
        {
            if (!inspectionTask || TaskComplete || !Coordinator || !Coordinator.CanInteract(this)) return;
            observation.Answer(correct);
            feedback = correct ? "Observation complete. The window provides a view while keeping water out."
                : "Try again: look through the clear opening. Which function does it support?";
            Refresh();
        }
        public void ResetTask()
        {
            if (!Coordinator || !Coordinator.CanInteract(this)) return;
            observation.Reset(); feedback = null; Refresh();
        }
        public void SetAudioState(CSYGuideAudioState state) { AudioState = state; }
        public void Refresh()
        {
            if (!card) return;
            card.SetText(Holding ? "Turn the helmet to inspect its window. Release to return it." : instruction,
                transcript, feedback ?? (inspectionTask ? question : "This is an illustrative museum model."));
        }
        public void ShowState(string state) { if (card) card.SetState(state); }
        public void ShowControls(CSYGuideControls controls, bool canInteract)
        { if (card) card.SetControls(controls, canInteract); }
        public void SetPresentation(bool visible, Camera camera) { if (card) card.SetPresentation(visible, camera); }
        private void OnGrab(SelectEnterEventArgs _) => Refresh();
        private void OnRelease(SelectExitEventArgs _) => Refresh();
        private void OnDisable()
        {
            if (Coordinator) Coordinator.GuideDisabled(this);
            Visit.Reset(); AudioState = CSYGuideAudioState.None;
            SetPresentation(false, null);
        }
        private void OnDestroy()
        {
            if (!inspectionObject) return;
            inspectionObject.selectEntered.RemoveListener(OnGrab);
            inspectionObject.selectExited.RemoveListener(OnRelease);
        }
    }
}
