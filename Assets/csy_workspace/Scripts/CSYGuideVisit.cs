using System;

namespace CsyMuseum
{
    /// <summary>
    /// Per-exhibit visit state. Kept independent of Unity so boundary and timing
    /// behaviour can be checked without a headset or an Editor licence.
    /// </summary>
    public sealed class CSYGuideVisit
    {
        public bool Inside { get; private set; }
        public bool Played { get; private set; }
        public double EnteredAt { get; private set; }

        /// <summary>
        /// Distances and radii use the same world-space units. The vertical
        /// distance is signed; the horizontal distance must be non-negative.
        /// Negative configuration values are clamped to zero. Non-finite data
        /// and unavailable exhibits end the visit rather than starting audio.
        /// </summary>
        public bool Update(bool available, float horizontalDistance, float verticalDistance,
            float enterRadius, float exitRadius, float maxVertical, double now)
        {
            if (!available || !Finite(horizontalDistance) || horizontalDistance < 0f ||
                !Finite(verticalDistance) || !Finite(enterRadius) || !Finite(exitRadius) ||
                !Finite(maxVertical) || !Finite(now))
            {
                Reset();
                return false;
            }

            enterRadius = Math.Max(0f, enterRadius);
            exitRadius = Math.Max(enterRadius, Math.Max(0f, exitRadius));
            maxVertical = Math.Max(0f, maxVertical);

            // A clock reset must not inherit an old dwell interval.
            if (Inside && now < EnteredAt) Reset();

            float radius = Inside ? exitRadius : enterRadius;
            if (horizontalDistance > radius || Math.Abs(verticalDistance) > maxVertical)
            {
                Reset();
                return false;
            }

            if (!Inside)
            {
                Inside = true;
                Played = false;
                EnteredAt = now;
            }
            return true;
        }

        public bool Ready(double now, float dwellSeconds)
        {
            return Inside && !Played && Finite(now) && Finite(dwellSeconds) &&
                now >= EnteredAt && now - EnteredAt >= Math.Max(0f, dwellSeconds);
        }

        public void MarkPlayed()
        {
            if (Inside) Played = true;
        }

        public void Reset()
        {
            Inside = false;
            Played = false;
            EnteredAt = 0d;
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
