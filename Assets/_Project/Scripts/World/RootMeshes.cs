using UnityEngine;
using Evolve.Visuals;

namespace Evolve.World
{
    /// <summary>Old tree roots and rays of light, built in code: knobby tapering roots, arches, branches, light shafts.</summary>
    public static class RootMeshes
    {
        /// <summary>
        /// A root along a path: thick at the start, tapering to the tip, with lumps (the radius swells and shrinks
        /// along its length, seeded). `knobbiness` 0 = smooth, 0.3 = very lumpy.
        /// </summary>
        public static Mesh RootTube(Vector3[] path, float baseRadius, float tipRadius, int seed, float knobbiness, int sides = 8)
        {
            var rnd = new System.Random(seed);
            float p1 = (float)rnd.NextDouble() * Mathf.PI * 2f, p2 = (float)rnd.NextDouble() * Mathf.PI * 2f;
            var radii = new float[path.Length];
            for (int i = 0; i < radii.Length; i++)
            {
                float t = i / (float)(radii.Length - 1);
                float taper = Mathf.Lerp(baseRadius, tipRadius, Mathf.SmoothStep(0f, 1f, t));
                float lump = 1f + knobbiness * (Mathf.Sin(t * 17f + p1) + 0.6f * Mathf.Sin(t * 29f + p2)) / 1.6f;
                radii[i] = Mathf.Max(0.01f, taper * lump);
            }
            radii[radii.Length - 1] = Mathf.Min(radii[radii.Length - 1], tipRadius); // a blunt, not bulging, end
            return SlimeMeshBuilder.Tube(path, radii, sides, "Root");
        }

        /// <summary>
        /// A ceiling of interwoven roots over <paramref name="area"/> (x, z = world x, z), hanging about
        /// <paramref name="height"/> m up: thick roots on a ~16 m grid, sagging between their knots, and (when
        /// <paramref name="thin"/>) a mat of thin roots on a ~6 m grid filling the gaps. Knots come from the world grid, so
        /// neighbouring areas join without seams. No root crosses within <paramref name="holeRadius"/> of a hole (where light
        /// comes through). World-space vertices.
        /// </summary>
        public static Mesh Lattice(Rect area, float height, int seed, System.Collections.Generic.IList<Vector2> holes = null, float holeRadius = 7f, bool thin = true)
        {
            var parts = new System.Collections.Generic.List<CombineInstance>();
            AddLattice(parts, area, height, seed, 16f, 5f, 0.6f, 1.4f, 1f, 3f, 0.15f, 5, 8, holes, holeRadius);
            if (thin) AddLattice(parts, area, height - 0.8f, seed + 7, 6f, 2f, 0.15f, 0.35f, 0.5f, 1.5f, 0.35f, 4, 5, holes, holeRadius);
            var mesh = new Mesh { name = "RootLattice", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            if (parts.Count > 0) mesh.CombineMeshes(parts.ToArray(), true, false);
            mesh.RecalculateBounds();
            return mesh;
        }

        static void AddLattice(System.Collections.Generic.List<CombineInstance> parts, Rect area, float height, int seed, float spacing,
            float jitter, float minR, float maxR, float minSag, float maxSag, float skip, int points, int sides,
            System.Collections.Generic.IList<Vector2> holes, float holeRadius)
        {
            int i0 = Mathf.FloorToInt(area.xMin / spacing), i1 = Mathf.CeilToInt(area.xMax / spacing);
            int j0 = Mathf.FloorToInt(area.yMin / spacing), j1 = Mathf.CeilToInt(area.yMax / spacing);
            for (int i = i0; i < i1; i++)
                for (int j = j0; j < j1; j++)
                {
                    Vector3 a = Knot(i, j, seed, spacing, jitter, height);
                    if (!area.Contains(new Vector2(a.x, a.z))) continue;           // every knot belongs to exactly one area
                    for (int e = 0; e < 2; e++)
                    {
                        if (Hash01(i, j, seed * 3 + e) < skip) continue;            // gaps
                        Vector3 b = e == 0 ? Knot(i + 1, j, seed, spacing, jitter, height) : Knot(i, j + 1, seed, spacing, jitter, height);
                        Vector2 mid = new Vector2(a.x + b.x, a.z + b.z) * 0.5f;
                        bool blocked = false;
                        if (holes != null)
                            foreach (var h in holes) if (Vector2.Distance(mid, h) < holeRadius) { blocked = true; break; }
                        if (blocked) continue;
                        float sag = Mathf.Lerp(minSag, maxSag, Hash01(i, j, seed * 5 + e));
                        Vector3 side = Vector3.Cross(b - a, Vector3.up).normalized;
                        float sway = (Hash01(i, j, seed * 7 + e) - 0.5f) * spacing * 0.25f;
                        var path = new Vector3[points];
                        for (int k = 0; k < points; k++)
                        {
                            float t = k / (float)(points - 1);
                            path[k] = Vector3.Lerp(a, b, t) + Vector3.down * sag * Mathf.Sin(t * Mathf.PI) + side * sway * Mathf.Sin(t * Mathf.PI);
                        }
                        float r = Mathf.Lerp(minR, maxR, Hash01(i, j, seed * 11 + e));
                        parts.Add(new CombineInstance { mesh = RootTube(path, r, r * 0.8f, seed * 13 + i * 31 + j, 0.15f, sides), transform = Matrix4x4.identity });
                    }
                }
        }

        static Vector3 Knot(int i, int j, int seed, float spacing, float jitter, float height)
        {
            float x = (i + 0.5f) * spacing + (Hash01(i, j, seed) - 0.5f) * 2f * jitter;
            float z = (j + 0.5f) * spacing + (Hash01(i, j, seed + 1) - 0.5f) * 2f * jitter;
            float y = height + (Hash01(i, j, seed + 2) - 0.5f) * 3f;
            return new Vector3(x, y, z);
        }

        static float Hash01(int x, int y, int s)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + s * 1274126177;
                h = (h ^ (h >> 13)) * 1274126177;
                h ^= h >> 16;
                return (h & 0xffff) / 65535f;
            }
        }

        /// <summary>
        /// An arch from a to b (two points on the ground) rising `height` metres in between, wandering sideways a little.
        /// Both ends are sunk `sink` metres below the given points so they grow out of the ground.
        /// </summary>
        public static Vector3[] ArchPath(Vector3 a, Vector3 b, float height, int seed, float wobble, int points, float sink = 0.35f)
        {
            var rnd = new System.Random(seed);
            float p1 = (float)rnd.NextDouble() * Mathf.PI * 2f;
            Vector3 along = b - a;
            Vector3 side = Vector3.Cross(Vector3.up, along).normalized;
            var path = new Vector3[points];
            for (int i = 0; i < points; i++)
            {
                float t = i / (float)(points - 1);
                float rise = Mathf.Sin(t * Mathf.PI);
                // a rounded arch whose shoulders are fuller than a plain sine
                float arch = Mathf.Pow(Mathf.Max(0f, rise), 0.75f) * height; // sin(pi) is a tiny NEGATIVE number in float; a power of it is NaN
                Vector3 p = a + along * t + Vector3.up * arch + side * (wobble * rise * Mathf.Sin(t * Mathf.PI * 3f + p1));
                p.y -= sink * (1f - rise); // buried at both ends
                path[i] = p;
            }
            return path;
        }

        /// <summary>A branch leaving `from` in direction `dir`, drooping toward the ground over `length` metres.</summary>
        public static Vector3[] BranchPath(Vector3 from, Vector3 dir, float length, float droop, int seed, int points)
        {
            var rnd = new System.Random(seed);
            float p1 = (float)rnd.NextDouble() * Mathf.PI * 2f;
            dir = dir.sqrMagnitude < 1e-6f ? Vector3.forward : dir.normalized;
            Vector3 side = Vector3.Cross(Vector3.up, dir).normalized;
            var path = new Vector3[points];
            for (int i = 0; i < points; i++)
            {
                float t = i / (float)(points - 1);
                path[i] = from + dir * (length * t) + Vector3.down * (droop * t * t) + side * (0.25f * length * Mathf.Sin(t * 4f + p1) * t);
            }
            return path;
        }

        /// <summary>
        /// A cone of light from a small circle at the top (y = 0) widening down to y = -height. u runs around, v from
        /// the top (0) to the bottom (1). Meant for a double-sided, fading translucent material.
        /// </summary>
        public static Mesh LightShaft(float topRadius, float bottomRadius, float height, int sides, int rings, int seed)
        {
            sides = Mathf.Max(6, sides); rings = Mathf.Max(2, rings);
            var rnd = new System.Random(seed);
            float p1 = (float)rnd.NextDouble() * Mathf.PI * 2f, p2 = (float)rnd.NextDouble() * Mathf.PI * 2f;
            var verts = new Vector3[(rings + 1) * (sides + 1)];
            var uvs = new Vector2[verts.Length];
            for (int i = 0; i <= rings; i++)
            {
                float t = i / (float)rings;
                float radius = Mathf.Lerp(topRadius, bottomRadius, t);
                for (int j = 0; j <= sides; j++)
                {
                    float a = j / (float)sides * Mathf.PI * 2f;
                    float r = radius * (1f + 0.08f * Mathf.Sin(3f * a + p1) + 0.05f * Mathf.Sin(5f * a + p2)); // not a perfect cone
                    verts[i * (sides + 1) + j] = new Vector3(Mathf.Cos(a) * r, -t * height, Mathf.Sin(a) * r);
                    uvs[i * (sides + 1) + j] = new Vector2(j / (float)sides, t);
                }
            }
            var tris = new System.Collections.Generic.List<int>();
            for (int i = 0; i < rings; i++)
                for (int j = 0; j < sides; j++)
                {
                    int a = i * (sides + 1) + j, b = a + 1, c = a + sides + 1, d = c + 1;
                    tris.Add(a); tris.Add(c); tris.Add(b);
                    tris.Add(b); tris.Add(c); tris.Add(d);
                }
            var mesh = new Mesh { name = "LightShaft" };
            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
