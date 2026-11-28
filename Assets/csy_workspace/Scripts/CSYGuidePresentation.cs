namespace CsyMuseum
{
    public enum CSYGuideAudioState
    {
        None,
        Playing,
        Finished,
        Skipped,
        Interrupted
    }

    /// <summary>Text and interaction state for the currently displayed guide controls.</summary>
    public readonly struct CSYGuideControls
    {
        public string Message { get; }
        public string ListenLabel { get; }
        public string AutoLabel { get; }
        public bool CanListen { get; }
        public bool CanStop { get; }

        public CSYGuideControls(string message, string listenLabel, string autoLabel,
            bool canListen, bool canStop)
        {
            Message = message;
            ListenLabel = listenLabel;
            AutoLabel = autoLabel;
            CanListen = canListen;
            CanStop = canStop;
        }
    }

    /// <summary>
    /// Produces the same visible status and button availability for desktop and XR.
    /// This pure function is independent of playback, Unity and controller input.
    /// </summary>
    public static class CSYGuidePresentation
    {
        public static CSYGuideControls Build(bool available, bool inside, bool hasClip,
            bool mediaBusy, bool automatic, bool muted, CSYGuideAudioState audioState)
        {
            string message;
            if (!available)
                message = "Guide paused while a menu or tutorial is open.";
            else if (!inside)
                message = "Move closer to this exhibit to use its controls.";
            else if (!hasClip)
                message = "Written guide available. Audio is not installed.";
            else if (muted)
                message = "Museum sound is muted. Read here or use the museum volume menu.";
            else if (mediaBusy)
                message = "Another narration or video is playing. Read here and replay later.";
            else if (audioState == CSYGuideAudioState.Playing)
                message = "Playing introduction. Select Stop to skip.";
            else if (audioState == CSYGuideAudioState.Skipped)
                message = "Audio stopped. Select Replay to listen again.";
            else if (audioState == CSYGuideAudioState.Interrupted)
                message = "Introduction interrupted. Select Replay when ready.";
            else if (audioState == CSYGuideAudioState.Finished)
                message = "Introduction finished. Select Replay to listen again.";
            else if (automatic)
                message = "Pause and face the exhibit to hear its introduction.";
            else
                message = "Auto audio is off. Select Listen or read here.";

            bool canListen = available && inside && hasClip && !mediaBusy && !muted &&
                audioState != CSYGuideAudioState.Playing;
            bool canStop = available && inside && hasClip &&
                (audioState == CSYGuideAudioState.Playing ||
                 (automatic && audioState == CSYGuideAudioState.None));

            return new CSYGuideControls(message,
                audioState == CSYGuideAudioState.None ? "Listen" : "Replay",
                automatic ? "Auto audio: ON" : "Auto audio: OFF",
                canListen, canStop);
        }
    }
}
