using System;

namespace CsyMuseum
{
    /// <summary>
    /// Audio ownership rules independent of Unity objects. A sound effect is not
    /// narration, and a looping source is not permission to change its volume.
    /// </summary>
    public static class CSYGuideAudioRules
    {
        public static bool BlocksSpeech(bool sameScene, bool playing, bool knownNarration,
            bool explicitlyRegistered, bool ownVoice)
        {
            return sameScene && playing && !ownVoice && (knownNarration || explicitlyRegistered);
        }

        public static bool MayDuck(bool sameScene, bool playing, bool looping,
            bool explicitlyAllowed, bool ownVoice)
        {
            return sameScene && playing && looping && explicitlyAllowed && !ownVoice;
        }

        public static bool CanRestore(float current, float original)
        {
            if (!Finite(current) || !Finite(original) || original < 0f) return false;
            float expected = original * .25f;
            float tolerance = Math.Max(.00001f, Math.Abs(expected) * .00001f);
            // Restore only the value this guide wrote. Respect later changes by
            // the source owner, such as a new volume setting or a fade.
            return Math.Abs(current - expected) <= tolerance;
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
