using System.Collections.Generic;
using UnityEngine;

namespace Evolve.EditorTools
{
    /// <summary>
    /// Where things are in the lake zone, for the Dress Lake tools. It reads the scene and, when the project has one, the
    /// zone layout (World/ZoneLayout, found BY NAME so this compiles with or without it): the flat core around the pool plus
    /// each biome's pad. So decorations follow the layout instead of fixed old coordinates.
    /// </summary>
    internal static class LakeLayout
    {
        internal struct Area
        {
            public string Name;
            public Vector2 Center;
            public float Radius;
            public float Size => Mathf.PI * Radius * Radius;
        }

        const float CoreRadius = 62f;

        /// <summary>The flat core, plus every biome pad of the layout (if any). Always at least the core.</summary>
        internal static List<Area> Areas()
        {
            var areas = new List<Area> { new Area { Name = "Core", Center = Vector2.zero, Radius = CoreRadius } };
            var type = System.Type.GetType("Evolve.World.ZoneLayout, Evolve");
            var field = type != null ? type.GetField("Biomes") : null;
            if (field == null || !(field.GetValue(null) is System.Array pads)) return areas;
            foreach (var pad in pads)
            {
                var t = pad.GetType();
                var centre = t.GetField("Center")?.GetValue(pad);
                var radius = t.GetField("Radius")?.GetValue(pad);
                var name = t.GetField("Name")?.GetValue(pad) as string;
                if (centre is Vector2 c && radius is float r)
                    areas.Add(new Area { Name = name ?? "Pad", Center = c, Radius = Mathf.Max(5f, r - 4f) }); // stay off the very edge
            }
            return areas;
        }

        /// <summary>
        /// Random points over all areas, `perCore` of them over a core-sized area; bigger or smaller areas get
        /// proportionally more or fewer (the same density everywhere).
        /// </summary>
        internal static List<Vector2> Scatter(int perCore, System.Random rnd)
        {
            var points = new List<Vector2>();
            var areas = Areas();
            float coreSize = areas[0].Size;
            foreach (var area in areas)
            {
                int count = Mathf.RoundToInt(perCore * area.Size / coreSize);
                for (int i = 0; i < count; i++)
                {
                    float angle = (float)rnd.NextDouble() * Mathf.PI * 2f;
                    float distance = area.Radius * Mathf.Sqrt((float)rnd.NextDouble()); // even over the disc
                    points.Add(area.Center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance);
                }
            }
            return points;
        }

        /// <summary>Height of the cave roof above (x, z): from the dome if the scene has one, else a low default roof.</summary>
        internal static float CeilingY(float x, float z, float fallback = 20f)
        {
            var dome = GameObject.Find("Dome");
            var r = dome != null ? dome.GetComponent<Renderer>() : null;
            if (r == null) return fallback;
            Bounds b = r.bounds;
            float radius = Mathf.Max(1f, b.extents.x);
            float d = Vector2.Distance(new Vector2(x, z), new Vector2(b.center.x, b.center.z)) / radius;
            return b.min.y + b.size.y * Mathf.Sqrt(Mathf.Max(0f, 1f - d * d));
        }

        /// <summary>
        /// Where each winding stream starts (its upstream end): the end farther from the middle of the zone, and the
        /// direction the water flows from there (toward the other end). Used by the water tool (spring pool) and the spring tool.
        /// </summary>
        internal static void StreamEnds(Vector2 a, Vector2 b, out Vector2 source, out Vector2 flow)
        {
            bool aIsSource = a.sqrMagnitude >= b.sqrMagnitude;
            source = aIsSource ? a : b;
            Vector2 other = aIsSource ? b : a;
            flow = (other - source).sqrMagnitude > 1e-4f ? (other - source).normalized : Vector2.up;
        }

        /// <summary>True if the scene has its own hanging roots (a big cave) so the tools should not add more.</summary>
        internal static bool HasHangingRoots() => GameObject.Find("Hanging Roots") != null;

        /// <summary>
        /// Points where things can hang from the roof: the lower end of each hanging root of the scene, or the old ceiling
        /// roots, or (if there are none) a few spots under a low default roof.
        /// </summary>
        internal static List<Vector3> HangingPoints(System.Random rnd)
        {
            var points = new List<Vector3>();
            var hanging = GameObject.Find("Hanging Roots");
            if (hanging != null)
            {
                foreach (Transform child in hanging.transform)
                {
                    var r = child.GetComponent<Renderer>();
                    if (r != null) points.Add(new Vector3(r.bounds.center.x, r.bounds.min.y, r.bounds.center.z));
                }
                if (points.Count > 0) return points;
            }
            var ceiling = GameObject.Find("Root Ceiling");
            if (ceiling != null) foreach (Transform t in ceiling.transform) points.Add(t.position);
            if (points.Count > 0) return points;
            for (int i = 0; i < 8; i++)
                points.Add(new Vector3(Mathf.Lerp(-45f, 45f, (float)rnd.NextDouble()), 20f, Mathf.Lerp(-45f, 45f, (float)rnd.NextDouble())));
            return points;
        }
    }
}
