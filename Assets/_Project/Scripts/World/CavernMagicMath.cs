using UnityEngine;

namespace Evolve.World
{
    /// <summary>
    /// The numbers of the cavern's magic light (pure, shared by the Editor tool and the tests): where the beam comes in and lands, how big the
    /// pool of light on the water is, and the ring above the floor where the fireflies and spores drift.
    /// </summary>
    public static class CavernMagicMath
    {
        public const float BeamBottomExtra = 1.2f;   // the beam widens by this many metres of radius on its way down

        public static Vector3 BeamTop
        {
            get { var h = CavernLayout.HoleCenter(); return new Vector3(h.x, CavernLayout.CeilingY(h.x, h.y), h.y); }
        }

        public static float BeamHeight => BeamTop.y / -CavernLayout.SunDirection.y;

        /// <summary>Where the beam's axis meets the water surface.</summary>
        public static Vector3 BeamHit => BeamTop + CavernLayout.SunDirection * BeamHeight;

        public static float BeamTopRadius => CavernLayout.HoleRadius;
        public static float BeamBottomRadius => CavernLayout.HoleRadius + BeamBottomExtra;

        /// <summary>Radius of the pool of light on the water.</summary>
        public static float PoolRadius => BeamBottomRadius;

        // The drifting lights live in a flattened ring round the lake: from just beyond the shore out to a little before the wall.
        public static float RingInner => CavernLayout.LakeMeanRadius * 0.9f;
        public static float RingOuter => CavernLayout.FloorRadius + 6f;
        public static float RingCentreRadius => (RingInner + RingOuter) * 0.5f;
        public static float RingTubeRadius => (RingOuter - RingInner) * 0.5f;
        public const float RingHeightLow = 0.5f, RingHeightHigh = 4.5f;

        /// <summary>The sparkles on the water spread over this radius (inside the shore on every side).</summary>
        public static float GlintRadius => CavernLayout.LakeMeanRadius * 0.82f;
    }
}
