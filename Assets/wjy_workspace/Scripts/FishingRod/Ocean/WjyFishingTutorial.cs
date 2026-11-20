using UnityEngine;
using RodState = Wjy.Museum.WjyFishingRodCatchInteraction.RodState;
using Step = Wjy.Museum.WjyFishingTutorialUI.Step;

namespace Wjy.Museum
{
    [DisallowMultipleComponent]
    public sealed class WjyFishingTutorial : MonoBehaviour
    {
        private enum Lesson { FirstVisit, Practising, Completed, Dismissed }

        private WjyOceanFishingSession session;
        private WjyFishingRodCatchInteraction rod;
        private WjyMuseumPauseMenu menu;
        private WjyFishingTutorialUI panel;
        private Lesson lesson;
        private bool leftHand, displayedLeft;
        private string correction, displayedCorrection;
        internal Step DisplayedStep { get; private set; }

        internal void Initialize(WjyOceanFishingSession oceanSession, WjyFishingRodCatchInteraction fishingRod)
        {
            session = oceanSession;
            rod = fishingRod;
            menu = GetComponent<WjyMuseumPauseMenu>();
            panel = gameObject.AddComponent<WjyFishingTutorialUI>();
            panel.Build(rod.PedestalInstructions.canvas.transform, rod.SharedRig.Camera, Skip, Replay, session.EnterOcean);
            rod.StateChanged += OnRodStateChanged;
            rod.PracticeIssueDetected += OnPracticeIssue;
            Refresh();
        }

        private void LateUpdate()
        {
            if (!panel) return;
            if ((lesson == Lesson.FirstVisit || lesson == Lesson.Practising) && !session.AtSea &&
                (!menu || !menu.IsOpen) && rod.State == RodState.Display && rod.TryGetPointingHand(out var pointingLeft))
            {
                leftHand = pointingLeft;
                lesson = Lesson.Practising;
            }
            Refresh();
        }

        private void OnRodStateChanged(RodState previous, RodState current)
        {
            if (!isActiveAndEnabled || session.AtSea) return;
            if (current == RodState.Held && rod.HoldingRay)
            {
                leftHand = rod.HeldInLeftHand;
                if (lesson == Lesson.FirstVisit) lesson = Lesson.Practising;
            }
            // Casting again without using a tutorial button continues as free practice.
            if (lesson == Lesson.Completed && current == RodState.Charging) lesson = Lesson.Dismissed;
            if (lesson != Lesson.Practising) return;
            if (previous == RodState.Reeling && current == RodState.Held)
                lesson = Lesson.Completed;
            if (current == RodState.Held || current == RodState.Charging || current == RodState.Flying)
                correction = null;
        }

        private void OnPracticeIssue(WjyFishingRodCatchInteraction.PracticeIssue issue)
        {
            if (lesson != Lesson.Practising) return;
            correction = issue == WjyFishingRodCatchInteraction.PracticeIssue.GripReleased
                ? "Keep Grip held. Release only Trigger to cast."
                : "Swing forward before releasing Trigger.";
        }

        private void Skip()
        {
            lesson = Lesson.Dismissed;
            correction = null;
            Refresh();
        }

        private void Replay()
        {
            lesson = Lesson.Practising;
            correction = null;
            Refresh();
        }

        internal void LeaveGallery()
        {
            lesson = Lesson.Dismissed;
            correction = null;
            Present(Step.Hidden);
        }

        private void Refresh()
        {
            if (session.AtSea || !rod.isActiveAndEnabled || (menu && menu.IsOpen))
                Present(Step.Hidden);
            else if (lesson == Lesson.Completed)
                Present(Step.Complete);
            else if (lesson == Lesson.Practising)
                Present(StepForAction(rod.State));
            else
                Present(Step.Available);
        }

        private static Step StepForAction(RodState state)
        {
            switch (state)
            {
                case RodState.Held: return Step.HoldTrigger;
                case RodState.Charging: return Step.Swing;
                case RodState.Flying: return Step.Flying;
                case RodState.PracticeLanded: return Step.Reel;
                case RodState.Reeling: return Step.Reeling;
                default: return Step.Grip;
            }
        }

        private void Present(Step step)
        {
            if (step == DisplayedStep && displayedCorrection == correction && displayedLeft == leftHand) return;
            DisplayedStep = step;
            displayedLeft = leftHand;
            displayedCorrection = correction;
            panel.Show(step, correction, leftHand);
        }

        private void OnDisable()
        {
            if (panel) Present(Step.Hidden);
        }

        private void OnDestroy()
        {
            if (!rod) return;
            rod.StateChanged -= OnRodStateChanged;
            rod.PracticeIssueDetected -= OnPracticeIssue;
        }
    }
}
