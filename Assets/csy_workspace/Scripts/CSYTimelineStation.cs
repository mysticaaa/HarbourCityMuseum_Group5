using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ZxhMuseum;

namespace CsyMuseum
{
    /// <summary>Optional, stationary story station. Reuses team ray/trigger buttons.</summary>
    public sealed class CSYTimelineStation : MonoBehaviour
    {
        [SerializeField] private Camera viewer;
        [SerializeField] private bool placeNearViewerAtStart = true;
        [SerializeField] private Vector3 stationOffset = new Vector3(1.1f, 0, 2.5f);
        private StoryCatalogue catalogue;
        private WorldPanel panel;
        private bool initialized;
        public StoryProgress Progress { get; private set; }
        public string CurrentPage { get; private set; } = "overview";
        public string VisibleText { get; private set; }
        public StoryChapter[] Chapters => catalogue.chapters;
        public WorldPanel Panel => panel;

        private void Start() { Initialize(); }
        public void Initialize()
        {
            if (initialized) return;
            var asset = Resources.Load<TextAsset>("CsyTour/Stories");
            if (asset == null) { Debug.LogError("[CSY] Story catalogue missing", this); enabled = false; return; }
            catalogue = JsonUtility.FromJson<StoryCatalogue>(asset.text);
            if (catalogue == null || catalogue.chapters == null || catalogue.chapters.Length == 0)
                throw new InvalidOperationException("Empty CSY timeline");
            var ids = Array.ConvertAll(catalogue.chapters, chapter => chapter.id);
            Progress = new StoryProgress(ids);
            if (viewer == null) viewer = Camera.main;
            if (viewer == null) { Debug.LogError("[CSY] Timeline needs a viewer camera", this); enabled = false; return; }
            if (placeNearViewerAtStart)
            {
                Vector3 forward = Vector3.ProjectOnPlane(viewer.transform.forward, Vector3.up);
                if (forward.sqrMagnitude < .001f) forward = Vector3.forward;
                Quaternion facing = Quaternion.LookRotation(forward.normalized);
                transform.SetPositionAndRotation(viewer.transform.position + facing * stationOffset, facing);
            }
            // Position is fixed after placement: this is a gallery station, not the pending follow-view menu.
            if (EventSystem.current == null) WorldPanel.EnsureEventSystem();
            initialized = true;
            ShowOverview();
        }

        private WorldPanel NewPage(string title)
        {
            if (panel != null) { panel.gameObject.SetActive(false); Destroy(panel.gameObject); }
            panel = WorldPanel.Create(transform, title, Vector3.zero, Quaternion.identity, 1000);
            panel.GetComponent<Canvas>().worldCamera = viewer;
            panel.GetComponentsInChildren<Text>()[0].fontSize = 34;
            return panel;
        }
        public void ShowOverview()
        {
            if (!initialized) return;
            CurrentPage = "overview";
            NewPage("Port-city timeline | CSY");
            VisibleText = "Ships, work and everyday life\nChoose any chapter, or continue exploring the exhibits.\n" +
                "Read markers: " + Progress.ReadCount + "/" + Progress.Count + ". No chapter is compulsory.";
            panel.Status.text = VisibleText;
            for (int i = 0; i < catalogue.chapters.Length; i++)
            {
                int chapter = i;
                panel.Button(catalogue.chapters[i].period + ": " + catalogue.chapters[i].title,
                    () => OpenChapter(chapter));
            }
            panel.Button("Visit recap", ShowRecap);
            panel.Button("Continue exploring", CloseReading);
        }
        public void OpenChapter(int index)
        {
            if (!initialized) return;
            Progress.Select(index);
            CurrentPage = "chapter";
            StoryChapter chapter = catalogue.chapters[Progress.Index];
            NewPage(chapter.period + " | " + chapter.title);
            panel.Status.text = "Chapter " + (Progress.Index + 1) + "/" + Progress.Count +
                " | " + (Progress.CurrentRead ? "Marked read" : "Not marked read") +
                "\nInternational museum example; Harbour City Museum is fictional.";
            VisibleText = chapter.body + "\n\nLook / think: " + chapter.prompt;
            panel.Label(VisibleText, 30, 330);
            Button previous = panel.Button("Previous chapter", () => OpenChapter(Progress.Index - 1));
            Button next = panel.Button("Next chapter", () => OpenChapter(Progress.Index + 1));
            previous.interactable = Progress.Index > 0;
            next.interactable = Progress.Index < Progress.Count - 1;
            panel.Button(Progress.CurrentRead ? "Read marker saved" : "Mark this chapter read", MarkCurrentRead)
                .interactable = !Progress.CurrentRead;
            panel.Button("Source and reconstruction", ShowSource);
            panel.Button("Choose another chapter", ShowOverview);
            panel.Button("Continue exploring", CloseReading);
        }
        public void MarkCurrentRead()
        {
            if (CurrentPage != "chapter") return;
            Progress.MarkRead();
            OpenChapter(Progress.Index);
        }
        public void ShowSource()
        {
            if (!initialized) return;
            CurrentPage = "source";
            StoryChapter chapter = catalogue.chapters[Progress.Index];
            NewPage("Source and interpretation");
            panel.Status.text = chapter.sourceLabel;
            VisibleText = chapter.sourceUrl + "\n\n" + chapter.disclosure +
                "\n\nText is paraphrased. No museum image, historical voice or quotation is reproduced.";
            panel.Label(VisibleText, 28, 400);
            panel.Button("Return to this chapter", () => OpenChapter(Progress.Index));
            panel.Button("Timeline overview", ShowOverview);
        }
        public void ShowRecap()
        {
            if (!initialized) return;
            CurrentPage = "recap";
            NewPage("Connect objects and people");
            panel.Status.text = "Chapters marked read: " + Progress.ReadCount + "/" + Progress.Count;
            VisibleText = "Ship: explore the spaces and work aboard.\nHelmet: consider work below water and communication.\n" +
                "Stories: connect objects to migration and everyday experience.\n\n" +
                "Which object now raises a new question for you? A read marker records your choice, not a learning score.";
            panel.Label(VisibleText, 30, 330);
            panel.Button("Choose another chapter", ShowOverview);
            panel.Button("Continue exploring", CloseReading);
            panel.Button("Reset read markers...", RequestReset);
        }
        public void RequestReset()
        {
            CurrentPage = "reset";
            NewPage("Reset timeline markers?");
            panel.Status.text = "This clears only the timeline's read markers. Ship, helmet and visit state are unchanged.";
            panel.Button("Confirm reset markers", ConfirmReset);
            panel.Button("Cancel", ShowRecap);
        }
        public void ConfirmReset() { if (CurrentPage == "reset") { Progress.Reset(); ShowOverview(); } }
        public void CloseReading()
        {
            if (!initialized) return;
            CurrentPage = "closed";
            NewPage("Port-city stories (optional)");
            VisibleText = "Continue with the ship and helmet.\nYour timeline markers are kept for this visit.";
            panel.Status.text = VisibleText;
            panel.Button("Explore the timeline", ShowOverview);
        }
    }
}
