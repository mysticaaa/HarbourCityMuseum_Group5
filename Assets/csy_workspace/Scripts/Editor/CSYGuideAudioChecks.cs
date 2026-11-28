using System;

namespace CsyMuseum
{
    /// <summary>Pure software checks; these do not claim audible or headset validation.</summary>
    public static class CSYGuideAudioChecks
    {
        public static int Run()
        {
            int checks = 0;
            // Bits: same scene, playing, recognised narration, registered, own.
            // Only masks 7, 11 and 15 represent another permitted narration
            // source playing in this gallery. Enumerate even irrelevant flags
            // so unrelated/own sources cannot accidentally acquire priority.
            for (int mask = 0; mask < 32; mask++)
            {
                bool actual = CSYGuideAudioRules.BlocksSpeech(Bit(mask, 0), Bit(mask, 1),
                    Bit(mask, 2), Bit(mask, 3), Bit(mask, 4));
                Check(actual == (mask == 7 || mask == 11 || mask == 15),
                    "Speech permission combination " + mask, ref checks);
            }

            // Bits: same scene, playing, looping, allowed, own. The single
            // accepted combination is explicitly authorised background music.
            for (int mask = 0; mask < 32; mask++)
            {
                bool actual = CSYGuideAudioRules.MayDuck(Bit(mask, 0), Bit(mask, 1),
                    Bit(mask, 2), Bit(mask, 3), Bit(mask, 4));
                Check(actual == (mask == 15), "Duck permission combination " + mask, ref checks);
            }

            foreach (float original in new[] { 0f, .1f, .7f, 1f, 16f, float.MaxValue })
                Check(CSYGuideAudioRules.CanRestore(original * .25f, original),
                    "Unchanged guide volume for " + original, ref checks);

            Check(CSYGuideAudioRules.CanRestore(.175004f, .7f),
                "Harmless floating-point drift may be restored", ref checks);
            Check(!CSYGuideAudioRules.CanRestore(.17502f, .7f),
                "Changed owner volume above tolerance is preserved", ref checks);
            Check(!CSYGuideAudioRules.CanRestore(.17498f, .7f),
                "Changed owner volume below tolerance is preserved", ref checks);
            Check(!CSYGuideAudioRules.CanRestore(.7f, .7f),
                "Another module already restored the volume", ref checks);
            Check(!CSYGuideAudioRules.CanRestore(0f, .7f),
                "Owner mute is preserved", ref checks);
            Check(CSYGuideAudioRules.CanRestore(.00001f, 0f),
                "Absolute tolerance includes its upper boundary", ref checks);
            Check(CSYGuideAudioRules.CanRestore(-.00001f, 0f),
                "Absolute tolerance includes its lower boundary", ref checks);
            Check(!CSYGuideAudioRules.CanRestore(.00001001f, 0f),
                "Absolute tolerance does not include larger changes", ref checks);
            Check(CSYGuideAudioRules.CanRestore(1000.005f, 4000f),
                "Relative tolerance applies above the absolute floor", ref checks);
            Check(!CSYGuideAudioRules.CanRestore(1000.02f, 4000f),
                "Relative tolerance rejects a substantial owner change", ref checks);
            Check(!CSYGuideAudioRules.CanRestore(0f, -1f),
                "Negative original volume cannot be restored", ref checks);
            Check(!CSYGuideAudioRules.CanRestore(-float.MaxValue, float.MaxValue),
                "Overflowing difference cannot match the guide value", ref checks);

            foreach (float invalid in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
            {
                Check(!CSYGuideAudioRules.CanRestore(.25f, invalid),
                    "Non-finite original volume is rejected", ref checks);
                Check(!CSYGuideAudioRules.CanRestore(invalid, 1f),
                    "Non-finite current volume is rejected", ref checks);
                Check(!CSYGuideAudioRules.CanRestore(invalid, invalid),
                    "Two non-finite values do not grant restoration", ref checks);
            }
            return checks;
        }

        private static bool Bit(int mask, int bit) => (mask & (1 << bit)) != 0;

        private static void Check(bool condition, string message, ref int count)
        {
            if (!condition) throw new InvalidOperationException("[CSY E audio] " + message);
            count++;
        }
    }
}
