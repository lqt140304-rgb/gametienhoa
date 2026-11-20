using System;
using UnityEngine;

namespace Evolve.World
{
    /// <summary>
    /// Where the water is, for swimming, the under-water tint and the ripple: the scene says. By default it answers from the old
    /// <see cref="ZoneLayout"/> (the Underroot Lake scenes); a scene with its own lake (the Slime Cavern) installs its own functions.
    /// </summary>
    public static class WaterField
    {
        static Func<float, float, float> depth, cleanDepth;
        static float level = ZoneLayout.WaterLevel;

        /// <summary>True when a scene has installed its own water (not the old layout's).</summary>
        public static bool Custom => depth != null;

        /// <summary>Height of every water surface (m).</summary>
        public static float Level => level;

        /// <summary>Water depth at a point (0 = dry); puddles included.</summary>
        public static float DepthAt(float x, float z) => depth != null ? depth(x, z) : ZoneLayout.DepthAt(x, z);

        /// <summary>Real water only: dry sinkholes below the lake level count as 0.</summary>
        public static float CleanDepthAt(float x, float z) => cleanDepth != null ? cleanDepth(x, z) : ZoneLayout.CleanDepthAt(x, z);

        public static void Use(Func<float, float, float> depthAt, Func<float, float, float> cleanDepthAt, float waterLevel)
        {
            depth = depthAt; cleanDepth = cleanDepthAt ?? depthAt; level = waterLevel;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset()
        {
            depth = null; cleanDepth = null; level = ZoneLayout.WaterLevel;
        }
    }
}
