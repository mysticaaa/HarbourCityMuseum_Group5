using System;

namespace CsyMuseum
{
    /// <summary>
    /// Deterministic software checks of guide copy and available actions.
    /// These do not claim a device test, user observation or usability outcome.
    /// </summary>
    public static class CSYGuidePresentationChecks
    {
        public static int Run()
        {
            int checks = 0;
            VerifyStateMessages(ref checks);
            VerifyPriority(ref checks);
            VerifyAllControlCombinations(ref checks);
            return checks;
        }

        private static void Require(bool condition, string message, ref int checks)
        {
            checks++;
            if (!condition) throw new InvalidOperationException("CSY guide presentation: " + message);
        }

        private static CSYGuideControls Normal(CSYGuideAudioState state, bool automatic = true)
        {
            return CSYGuidePresentation.Build(true, true, true, false, automatic, false, state);
        }

        private static void VerifyStateMessages(ref int checks)
        {
            var initial = Normal(CSYGuideAudioState.None);
            Require(initial.Message == "Pause and face the exhibit to hear its introduction.",
                "initial automatic introduction must describe how to start it", ref checks);
            Require(initial.ListenLabel == "Listen" && initial.AutoLabel == "Auto audio: ON",
                "initial labels must identify listening and automatic mode", ref checks);
            Require(initial.CanListen && initial.CanStop,
                "a pending automatic introduction supports manual listening and skipping", ref checks);

            var manual = Normal(CSYGuideAudioState.None, false);
            Require(manual.Message == "Auto audio is off. Select Listen or read here.",
                "manual mode must identify listening and reading alternatives", ref checks);
            Require(manual.AutoLabel == "Auto audio: OFF" && manual.CanListen && !manual.CanStop,
                "manual idle controls must not offer a meaningless stop action", ref checks);

            var playing = Normal(CSYGuideAudioState.Playing);
            Require(playing.Message == "Playing introduction. Select Stop to skip.",
                "playing status must explain the stop control", ref checks);
            Require(!playing.CanListen && playing.CanStop,
                "playing narration can stop but must not restart itself", ref checks);

            var skipped = Normal(CSYGuideAudioState.Skipped);
            Require(skipped.Message == "Audio stopped. Select Replay to listen again.",
                "skipped status must explain recovery", ref checks);
            Require(skipped.ListenLabel == "Replay" && skipped.CanListen && !skipped.CanStop,
                "skipped narration must offer replay without an idle stop control", ref checks);

            var interrupted = Normal(CSYGuideAudioState.Interrupted);
            Require(interrupted.Message == "Introduction interrupted. Select Replay when ready.",
                "interruption must be distinguishable from deliberate stop or normal completion", ref checks);
            Require(interrupted.CanListen && !interrupted.CanStop,
                "unblocked interrupted narration must be manually recoverable", ref checks);

            var finished = Normal(CSYGuideAudioState.Finished);
            Require(finished.Message == "Introduction finished. Select Replay to listen again.",
                "finished status must describe repeat listening", ref checks);
            Require(finished.ListenLabel == "Replay" && finished.CanListen && !finished.CanStop,
                "finished narration must offer replay without restarting automatically", ref checks);
        }

        private static void VerifyPriority(ref int checks)
        {
            foreach (CSYGuideAudioState state in Enum.GetValues(typeof(CSYGuideAudioState)))
            {
                foreach (bool automatic in new[] { false, true })
                {
                    var paused = CSYGuidePresentation.Build(false, false, false, true, automatic, true, state);
                    Require(paused.Message == "Guide paused while a menu or tutorial is open.",
                        "unavailable gallery must outrank every other message", ref checks);
                    Require(!paused.CanListen && !paused.CanStop,
                        "unavailable gallery must disable both audio controls", ref checks);

                    var distant = CSYGuidePresentation.Build(true, false, false, true, automatic, true, state);
                    Require(distant.Message == "Move closer to this exhibit to use its controls.",
                        "distance must outrank clip, mute, media and playback state", ref checks);

                    var missing = CSYGuidePresentation.Build(true, true, false, true, automatic, true, state);
                    Require(missing.Message == "Written guide available. Audio is not installed.",
                        "missing audio must retain the written alternative before other audio status", ref checks);
                    Require(!missing.CanListen && !missing.CanStop,
                        "missing clip must disable playback and stop", ref checks);

                    var muted = CSYGuidePresentation.Build(true, true, true, true, automatic, true, state);
                    Require(muted.Message == "Museum sound is muted. Read here or use the museum volume menu.",
                        "mute must outrank other media and playback state", ref checks);
                    Require(!muted.CanListen, "muted museum must not accept listening", ref checks);

                    var busy = CSYGuidePresentation.Build(true, true, true, true, automatic, false, state);
                    Require(busy.Message == "Another narration or video is playing. Read here and replay later.",
                        "other media must outrank this guide's stored playback state", ref checks);
                    Require(!busy.CanListen, "busy media must block competing playback", ref checks);
                }
            }
        }

        private static void VerifyAllControlCombinations(ref int checks)
        {
            // Six independent boolean inputs and five audio states = 320 cases.
            for (int bits = 0; bits < 64; bits++)
            {
                bool available = (bits & 1) != 0;
                bool inside = (bits & 2) != 0;
                bool hasClip = (bits & 4) != 0;
                bool busy = (bits & 8) != 0;
                bool automatic = (bits & 16) != 0;
                bool muted = (bits & 32) != 0;
                foreach (CSYGuideAudioState state in Enum.GetValues(typeof(CSYGuideAudioState)))
                {
                    var controls = CSYGuidePresentation.Build(available, inside, hasClip,
                        busy, automatic, muted, state);
                    Require(!string.IsNullOrEmpty(controls.Message),
                        "every input combination needs a visible explanation", ref checks);
                    Require(controls.AutoLabel == (automatic ? "Auto audio: ON" : "Auto audio: OFF"),
                        "auto label must always represent the actual setting", ref checks);
                    Require(controls.ListenLabel == (state == CSYGuideAudioState.None ? "Listen" : "Replay"),
                        "listen label must distinguish unstarted and previous playback", ref checks);

                    if (!available || !inside || !hasClip)
                    {
                        Require(!controls.CanListen && !controls.CanStop,
                            "unavailable, distant or audio-less guides must not expose audio actions", ref checks);
                    }
                    else
                    {
                        if (busy || muted || state == CSYGuideAudioState.Playing)
                            Require(!controls.CanListen, "blocked or playing audio must reject listening", ref checks);
                        else
                            Require(controls.CanListen, "a local, ready, unblocked guide must support listening", ref checks);

                        if (state == CSYGuideAudioState.Playing)
                            Require(controls.CanStop, "playing audio can be stopped even if media or mute state changes", ref checks);
                        else if (automatic && state == CSYGuideAudioState.None)
                            Require(controls.CanStop, "pending automatic audio can be skipped before it starts", ref checks);
                        else
                            Require(!controls.CanStop, "other idle states must not offer an ineffective stop action", ref checks);
                    }
                }
            }
        }
    }
}
