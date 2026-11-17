using System.Collections.Generic;
using UnityEngine;

namespace Evolve.Visuals
{
    /// <summary>
    /// Builds the smooth gel body of the slime and the soft organ shapes (tubes, flared bases), all in code.
    /// Body size matches the old 1 m sphere: bottom at y = -0.45, top at about y = 0.28, radius about 0.5-0.65.
    /// </summary>
    public static class SlimeMeshBuilder
    {
        public const float Bottom = -0.45f;
        public const float Top = 0.28f;

        /// <summary>
        /// Smooth gel dome: the hump is shifted to the back, the skirt spreads out in uneven lobes like a puddle.
        /// 1280 triangles, shared vertices (smooth shading), planar UVs for the vein texture.
        /// </summary>
        public static Mesh Build(int seed = 7)
        {
            var verts = new List<Vector3>();
            var tris = new List<int>();
            Icosphere(verts, tris, 3);

            var random = new System.Random(seed);
            var bump = new float[verts.Count];
            for (int i = 0; i < bump.Length; i++) bump[i] = (float)random.NextDouble() * 2f - 1f;

            var shaped = new Vector3[verts.Count];
            var uv = new Vector2[verts.Count];
            for (int i = 0; i < verts.Count; i++)
            {
                Vector3 v = verts[i];
                float h = Mathf.Max(0f, v.y);
                float a = Mathf.Atan2(v.z, v.x);

                // Lobed, spreading skirt near the ground.
                float low = Mathf.Clamp01(1f - h * 2.6f);
                float lobes = low * (0.12f * Mathf.Sin(3f * a + seed) + 0.07f * Mathf.Sin(5f * a + 1.7f) + 0.03f * Mathf.Sin(8f * a));
                float radial = 0.5f * (1f + 0.16f * low * low + lobes + 0.015f * bump[i]);

                // Thin spreading edge, then a rounded hump (power > 1 keeps the lower part flat).
                float y = v.y >= 0f ? Bottom + (Top - Bottom) * Mathf.Pow(h, 1.35f) : Bottom;
                // Hump leans to the back: higher parts shift back (-z).
                float x = v.x * radial;
                float z = v.z * radial - 0.09f * h * h;

                shaped[i] = new Vector3(x, y, z);
                uv[i] = new Vector2(x + 0.5f, z + 0.5f);
            }

            var mesh = new Mesh { name = "SlimeBody" };
            mesh.SetVertices(shaped);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// A smooth tube along a path with a radius at every point (parallel-transport frames). The first end is
        /// open (it is meant to sit inside the body), the last end closes when its radius is 0.
        /// Used for eye stalks, pseudopods, horns and, with a straight path, flared bases.
        /// </summary>
        public static Mesh Tube(Vector3[] path, float[] radii, int sides = 10, string name = "GelTube")
        {
            int rings = path.Length;
            sides = Mathf.Max(3, sides);
            var verts = new Vector3[rings * sides];
            var centers = new Vector3[rings];

            Vector3 normal = Vector3.zero;
            for (int i = 0; i < rings; i++)
            {
                Vector3 t = (path[Mathf.Min(i + 1, rings - 1)] - path[Mathf.Max(i - 1, 0)]).normalized;
                if (t.sqrMagnitude < 1e-6f) t = Vector3.up;
                if (i == 0)
                {
                    normal = Vector3.Cross(t, Mathf.Abs(t.y) < 0.9f ? Vector3.up : Vector3.right).normalized;
                }
                else
                {
                    normal = (normal - Vector3.Dot(normal, t) * t).normalized;
                    if (normal.sqrMagnitude < 1e-6f) normal = Vector3.Cross(t, Vector3.right).normalized;
                }
                Vector3 binormal = Vector3.Cross(t, normal);
                centers[i] = path[i];
                for (int j = 0; j < sides; j++)
                {
                    float a = j / (float)sides * Mathf.PI * 2f;
                    verts[i * sides + j] = path[i] + (normal * Mathf.Cos(a) + binormal * Mathf.Sin(a)) * radii[i];
                }
            }

            var tris = new List<int>();
            for (int i = 0; i < rings - 1; i++)
                for (int j = 0; j < sides; j++)
                {
                    int j2 = (j + 1) % sides;
                    int a = i * sides + j, b = i * sides + j2, c = (i + 1) * sides + j, d = (i + 1) * sides + j2;
                    AddOutward(tris, verts, a, c, b, centers[i], centers[i + 1]);
                    AddOutward(tris, verts, b, c, d, centers[i], centers[i + 1]);
                }

            var mesh = new Mesh { name = name };
            mesh.vertices = verts;
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// A soft flared base (like a trumpet bell) that spreads wide at the bottom and narrows to the organ:
        /// stands on y = 0 and ends at y = height. Placed where an organ meets the body so the joint melts in.
        /// </summary>
        public static Mesh Flare(float baseRadius, float topRadius, float height, int sides = 14, int rings = 7)
        {
            var path = new Vector3[rings];
            var radii = new float[rings];
            for (int i = 0; i < rings; i++)
            {
                float t = i / (float)(rings - 1);
                path[i] = new Vector3(0f, t * height, 0f);
                float k = Mathf.Pow(1f - t, 3f);            // concave: wide at the base, quickly narrow
                radii[i] = topRadius + (baseRadius - topRadius) * k;
            }
            return Tube(path, radii, sides, "GelFlare");
        }

        // Keeps the triangle facing away from the tube's axis (Unity front faces are clockwise).
        static void AddOutward(List<int> tris, Vector3[] verts, int a, int b, int c, Vector3 axisA, Vector3 axisB)
        {
            Vector3 pa = verts[a], pb = verts[b], pc = verts[c];
            Vector3 n = Vector3.Cross(pb - pa, pc - pa);
            if (n.sqrMagnitude < 1e-12f) return; // degenerate (a closed tip)
            Vector3 outward = (pa + pb + pc) / 3f - (axisA + axisB) * 0.5f;
            if (Vector3.Dot(n, outward) < 0f) { int s = b; b = c; c = s; }
            tris.Add(a); tris.Add(b); tris.Add(c);
        }

        static void Icosphere(List<Vector3> verts, List<int> tris, int subdivisions)
        {
            float t = (1f + Mathf.Sqrt(5f)) / 2f;
            var baseVerts = new[]
            {
                new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0),
                new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t),
                new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1)
            };
            foreach (var v in baseVerts) verts.Add(v.normalized);
            var current = new List<int>
            {
                0,11,5, 0,5,1, 0,1,7, 0,7,10, 0,10,11, 1,5,9, 5,11,4, 11,10,2, 10,7,6, 7,1,8,
                3,9,4, 3,4,2, 3,2,6, 3,6,8, 3,8,9, 4,9,5, 2,4,11, 6,2,10, 8,6,7, 9,8,1
            };

            for (int step = 0; step < subdivisions; step++)
            {
                var cache = new Dictionary<long, int>();
                var next = new List<int>(current.Count * 4);
                for (int f = 0; f < current.Count; f += 3)
                {
                    int a = current[f], b = current[f + 1], c = current[f + 2];
                    int ab = Mid(verts, cache, a, b), bc = Mid(verts, cache, b, c), ca = Mid(verts, cache, c, a);
                    next.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
                }
                current = next;
            }

            // Make every triangle face outward (Unity front faces are clockwise: normal = cross(b - a, c - a)).
            for (int i = 0; i < current.Count; i += 3)
            {
                Vector3 a = verts[current[i]], b = verts[current[i + 1]], c = verts[current[i + 2]];
                if (Vector3.Dot(Vector3.Cross(b - a, c - a), a + b + c) < 0f)
                    (current[i + 1], current[i + 2]) = (current[i + 2], current[i + 1]);
            }
            tris.AddRange(current);
        }

        static int Mid(List<Vector3> verts, Dictionary<long, int> cache, int a, int b)
        {
            long key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
            if (cache.TryGetValue(key, out int found)) return found;
            verts.Add(((verts[a] + verts[b]) * 0.5f).normalized);
            cache[key] = verts.Count - 1;
            return verts.Count - 1;
        }
    }
}
