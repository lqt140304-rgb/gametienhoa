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
                float rise = Mathf.Max(0f, Mathf.Sin(t * Mathf.PI)); // sin(pi) is slightly negative in float; Pow of a negative is NaN
                // a rounded arch whose shoulders are fuller than a plain sine
                float arch = Mathf.Pow(rise, 0.75f) * height;
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
