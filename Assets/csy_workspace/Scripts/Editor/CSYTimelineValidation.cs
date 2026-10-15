#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using CsyMuseum;
using ZxhMuseum;

public static class CSYTimelineValidation
{
    private const string Key = "CSY.Timeline.Validation";
    private static int phase, loops;
    private static double deadline;
    private static readonly List<string> results = new List<string>();
    private static Vector3 museumPosition;
    private static Quaternion museumRotation;
    private static Dictionary<Behaviour, bool> rigStates;
    private const string Folder = "Assets/csy_workspace/Evidence";

    [InitializeOnLoadMethod]
    private static void Resume()
    {
        if (!SessionState.GetBool(Key, false)) return;
        deadline = EditorApplication.timeSinceStartup + 240;
        EditorApplication.update += Tick;
    }
    // Batch-only: exits the test editor. Never interrupts an interactive user session.
    public static void Run()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Use RunInteractive in the editor");
        EditorSceneManager.OpenScene(CSYTimelineBuilder.Source);
        CSYTimelineBuilder.Build();
        EditorSceneManager.OpenScene(CSYTimelineBuilder.Preview);
        SessionState.SetBool(Key, true);
        EditorApplication.isPlaying = true;
    }
    private static void Require(bool valid, string message)
    {
        if (!valid) throw new Exception(message);
        results.Add("PASS: " + message);
    }
    private static void Tick()
    {
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Timeline validation timeout");
            if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
            foreach (var legacy in UnityEngine.Object.FindObjectsOfType<Valve.VR.SteamVR_Render>()) legacy.enabled = false;
            Time.timeScale = 1;
            var timeline = UnityEngine.Object.FindObjectOfType<CSYTimelineStation>();
            if (phase == 0)
            {
                if (timeline == null || timeline.Progress == null) return;
                Require(timeline.Progress.Count == 3 && timeline.Progress.ReadCount == 0, "Three chapters; no automatic read credit");
                foreach (var chapter in timeline.Chapters)
                    Require(Uri.TryCreate(chapter.sourceUrl, UriKind.Absolute, out var url) && url.Scheme == "https"
                        && !string.IsNullOrWhiteSpace(chapter.disclosure), "Source and interpretation: " + chapter.id);
                var state = new StoryProgress(new[] { "a", "b", "c" });
                state.Select(-4); Require(state.Index == 0, "Previous boundary clamps");
                state.Select(40); Require(state.Index == 2, "Next boundary clamps");
                state.MarkRead(); state.MarkRead(); Require(state.ReadCount == 1, "Repeated read marking is idempotent");
                state.Reset(); Require(state.ReadCount == 0 && state.Index == 0, "Read-marker reset");
                bool rejected = false;
                try { new StoryProgress(new[] { "a", "a" }); } catch (ArgumentException) { rejected = true; }
                Require(rejected, "Duplicate chapter ids rejected");
                Press(timeline, "1948: Ships connect people and places");
                Require(timeline.Progress.Index == 1 && timeline.Progress.ReadCount == 0, "Direct chapter selection does not claim reading");
                Press(timeline, "Source and reconstruction");
                Require(timeline.CurrentPage == "source" && timeline.Progress.Index == 1, "Sources keep current chapter");
                Press(timeline, "Return to this chapter");
                Press(timeline, "Mark this chapter read");
                Require(timeline.Progress.ReadCount == 1 && timeline.Progress.CurrentRead, "Physical button callback marks current chapter");
                timeline.ShowRecap(); timeline.RequestReset();
                Press(timeline, "Cancel");
                Require(timeline.Progress.ReadCount == 1, "Reset cancellation retains markers");
                timeline.CloseReading(); Press(timeline, "Explore the timeline");
                Require(timeline.Progress.ReadCount == 1 && timeline.CurrentPage == "overview", "Close and reopen preserves progress");
                timeline.ShowRecap(); timeline.RequestReset(); Press(timeline, "Confirm reset markers");
                Require(timeline.Progress.ReadCount == 0, "Reset confirmation clears timeline only");
                timeline.OpenChapter(0);
                Require(!FindButton(timeline, "Previous chapter").interactable, "Previous disabled on first chapter");
                timeline.OpenChapter(2);
                Require(!FindButton(timeline, "Next chapter").interactable, "Next disabled on final chapter");
                timeline.ShowOverview(); phase = 1;
            }
            else if (phase >= 1 && phase <= 4)
            {
                if (timeline == null) return;
                CheckLayout(timeline);
                Capture(timeline, new[] { "overview", "chapter", "sources", "recap" }[phase - 1]);
                if (phase == 1) timeline.OpenChapter(0);
                if (phase == 2) timeline.ShowSource();
                if (phase == 3) timeline.ShowRecap();
                if (++phase == 5) SceneManager.LoadSceneAsync(CSYTimelineBuilder.Integrated);
            }
            else if (phase == 5)
            {
                var session = MuseumSession.Instance;
                if (timeline == null || timeline.Progress == null || session == null || session.Origin == null) return;
                Require(UnityEngine.Object.FindObjectsOfType<Unity.XR.CoreUtils.XROrigin>().Length == 1, "Integration keeps one active XR rig");
                Require(UnityEngine.Object.FindObjectsOfType<FollowViewUI>(true).Length == 0, "Integration adds no competing follow-view menu");
                rigStates = session.Origin.GetComponentsInChildren<Behaviour>(true).ToDictionary(item => item, item => item.enabled);
                museumPosition = session.Origin.transform.position; museumRotation = session.Origin.transform.rotation;
                timeline.OpenChapter(1); timeline.MarkCurrentRead();
                session.RequestEnter(); phase = 6;
            }
            else if (phase == 6)
            {
                var session = MuseumSession.Instance;
                if (session == null || session.Busy || !session.InCabin) return;
                Require(UnityEngine.Object.FindObjectsOfType<CSYTimelineStation>().Length == 0, "Timeline is hidden while aboard");
                session.RequestReturn(); phase = 7;
            }
            else if (phase == 7)
            {
                var session = MuseumSession.Instance;
                if (session == null || session.Busy || session.InCabin || timeline == null) return;
                Require(timeline.Progress.ReadCount == 1 && timeline.Progress.Index == 1, "Return preserves chapter and read marker");
                Require(Vector3.Distance(museumPosition, session.Origin.transform.position) < .01f &&
                    Quaternion.Angle(museumRotation, session.Origin.transform.rotation) < .1f, "Museum pose restored");
                Require(rigStates.All(pair => pair.Key != null && pair.Key.enabled == pair.Value), "Rig component enabled states preserved");
                if (++loops < 3) { session.RequestEnter(); phase = 6; }
                else Finish(0, "Three boat return cycles verified; no headset hardware input was tested.");
            }
        }
        catch (Exception error) { Debug.LogException(error); Finish(1, error.ToString()); }
    }
    private static Button FindButton(CSYTimelineStation timeline, string caption) =>
        timeline.Panel.GetComponentsInChildren<Button>().Single(button => button.name == "Button_" + caption);
    private static void Press(CSYTimelineStation timeline, string caption)
    {
        var button = FindButton(timeline, caption);
        Require(button.interactable && button.GetComponent<BoatTriggerAction>() != null && button.GetComponent<Collider>() != null,
            "Ray/trigger target configured: " + caption);
        button.GetComponent<BoatTriggerAction>().Press();
    }
    private static void CheckLayout(CSYTimelineStation timeline)
    {
        Canvas.ForceUpdateCanvases();
        foreach (var text in timeline.Panel.GetComponentsInChildren<Text>())
            Require(text.preferredHeight <= text.rectTransform.rect.height + 2, "Text fits: " + text.text.Substring(0, Math.Min(28, text.text.Length)));
    }
    private static void Capture(CSYTimelineStation timeline, string name)
    {
        if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return;
        Directory.CreateDirectory(Folder);
        var camera = Camera.main;
        var target = new RenderTexture(1440, 1440, 24);
        var prior = camera.targetTexture; var active = RenderTexture.active;
        try
        {
            camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
            var image = new Texture2D(1440, 1440, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1440, 1440), 0, 0); image.Apply();
            File.WriteAllBytes(Folder + "/Timeline_" + name + ".png", image.EncodeToPNG());
            UnityEngine.Object.Destroy(image);
        }
        finally { camera.targetTexture = prior; RenderTexture.active = active; target.Release(); UnityEngine.Object.Destroy(target); }
    }
    private static void Finish(int exitCode, string message)
    {
        results.Add((exitCode == 0 ? "PASS: " : "FAIL: ") + message);
        results.Add("NOT TESTED: physical headset/hand-controller input, comfort, target-device frame rate, participant learning.");
        Directory.CreateDirectory(Folder);
        File.WriteAllLines(Folder + "/Timeline_validation.txt", results);
        Debug.Log("[CSY TIMELINE] " + message);
        SessionState.SetBool(Key, false); EditorApplication.update -= Tick;
        EditorApplication.Exit(exitCode);
    }
}
#endif
