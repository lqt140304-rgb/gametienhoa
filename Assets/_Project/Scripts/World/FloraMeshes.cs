using System.Collections.Generic;
using Evolve.Visuals;
using UnityEngine;

namespace Evolve.World
{
    /// <summary>Plants of the lake built in code: giant mushrooms (stalk and cap), leaves and hanging vines.</summary>
    public static class FloraMeshes
    {
        /// <summary>
        /// A surface of revolution around the y axis. `profile` lists (radius, height) points; `interiorY` is a point
        /// on the axis inside the shape, so every triangle can be turned to face away from it. The radius wobbles a
        /// little around the axis (seeded), so nothing is a perfect circle.
        /// </summary>
        public static Mesh Lathe(IList<Vector2> profile, int sides, float interiorY, int seed, float wobble)
        {
            sides = Mathf.Max(5, sides);
            int rings = profile.Count;
            var rnd = new System.Random(seed);
            float p1 = (float)rnd.NextDouble() * Mathf.PI * 2f, p2 = (float)rnd.NextDouble() * Mathf.PI * 2f;
            var verts = new Vector3[rings * sides];
            for (int i = 0; i < rings; i++)
                for (int j = 0; j < sides; j++)
                {
                    float a = j / (float)sides * Mathf.PI * 2f;
                    float r = profile[i].x * (1f + wobble * (Mathf.Sin(3f * a + p1) + 0.5f * Mathf.Sin(5f * a + p2)));
                    verts[i * sides + j] = new Vector3(Mathf.Cos(a) * r, profile[i].y, Mathf.Sin(a) * r);
                }
            var tris = new List<int>();
            var inside = new Vector3(0f, interiorY, 0f);
            for (int i = 0; i < rings - 1; i++)
                for (int j = 0; j < sides; j++)
                {
                    int j2 = (j + 1) % sides;
                    int a = i * sides + j, b = i * sides + j2, c = (i + 1) * sides + j, d = (i + 1) * sides + j2;
                    Outward(tris, verts, a, c, b, inside);
                    Outward(tris, verts, b, c, d, inside);
                }
            var mesh = new Mesh { name = "Lathe" };
            mesh.vertices = verts;
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        static void Outward(List<int> tris, Vector3[] v, int a, int b, int c, Vector3 inside)
        {
            Vector3 n = Vector3.Cross(v[b] - v[a], v[c] - v[a]);
            if (n.sqrMagnitude < 1e-10f) return; // a pinched point on the axis
            if (Vector3.Dot(n, (v[a] + v[b] + v[c]) / 3f - inside) < 0f) { int t = b; b = c; c = t; }
            tris.Add(a); tris.Add(b); tris.Add(c);
        }

        /// <summary>
        /// A giant mushroom `height` metres tall, standing on y = 0: a stalk that flares at the foot, and a domed cap
        /// with a thick rim. Both lean the same way a little.
        /// </summary>
        public static void Mushroom(int seed, float height, out Mesh stalk, out Mesh cap, out float lean)
        {
            var rnd = new System.Random(seed);
            float h = height;
            float s = h * 0.075f * (0.85f + (float)rnd.NextDouble() * 0.3f);      // stalk radius
            float capR = h * 0.5f * (0.8f + (float)rnd.NextDouble() * 0.45f);     // cap radius
            float capH = capR * (0.42f + (float)rnd.NextDouble() * 0.2f);          // cap thickness
            float baseY = h * 0.76f;

            var stalkProfile = new List<Vector2>
            {
                new Vector2(0f, 0f), new Vector2(1.7f * s, 0.01f * h), new Vector2(1.4f * s, 0.07f * h), new Vector2(1.1f * s, 0.25f * h),
                new Vector2(0.9f * s, 0.55f * h), new Vector2(0.85f * s, baseY + 0.02f * h), new Vector2(0f, baseY + 0.04f * h)
            };
            var capProfile = new List<Vector2>
            {
                new Vector2(0f, baseY + capH), new Vector2(0.45f * capR, baseY + 0.93f * capH), new Vector2(0.8f * capR, baseY + 0.66f * capH),
                new Vector2(1.0f * capR, baseY + 0.28f * capH), new Vector2(1.04f * capR, baseY + 0.08f * capH),
                new Vector2(0.9f * capR, baseY - 0.03f * capH), new Vector2(0.55f * capR, baseY + 0.04f * capH),
                new Vector2(0.2f * capR, baseY + 0.1f * capH), new Vector2(0f, baseY + 0.12f * capH)
            };
            stalk = Lathe(stalkProfile, 10, h * 0.42f, seed, 0.05f);
            cap = Lathe(capProfile, 16, baseY + 0.25f * capH, seed + 1, 0.07f);

            lean = h * 0.14f * ((float)rnd.NextDouble() - 0.5f) * 2f;
            Bend(stalk, lean, h);
            Bend(cap, lean, h);
        }

        /// <summary>Height of the top of a cap at distance r from its axis, for placing glowing spots (matches Mushroom).</summary>
        public static float CapTopHeight(float height, float capRadius, float capThickness, float r)
        {
            float baseY = height * 0.76f;
            // top surface points of the profile: (0,1.0) (0.45,0.93) (0.8,0.66) (1.0,0.28) in units of capRadius / capThickness
            float u = Mathf.Clamp01(r / capRadius);
            float[] xs = { 0f, 0.45f, 0.8f, 1.0f };
            float[] ys = { 1.0f, 0.93f, 0.66f, 0.28f };
            for (int i = 0; i < 3; i++)
                if (u <= xs[i + 1])
                    return baseY + capThickness * Mathf.Lerp(ys[i], ys[i + 1], Mathf.InverseLerp(xs[i], xs[i + 1], u));
            return baseY + capThickness * 0.28f;
        }

        /// <summary>Sideways shift of a point at height y on a mushroom that leans by `lean` (matches Mushroom).</summary>
        public static float LeanShift(float lean, float y, float height)
        {
            float t = Mathf.Clamp01(y / Mathf.Max(0.01f, height));
            return lean * t * t;
        }

        // Leans a mesh sideways, more the higher it is (x shifts by lean * (y / height)^2).
        static void Bend(Mesh mesh, float lean, float height)
        {
            var v = mesh.vertices;
            for (int i = 0; i < v.Length; i++)
            {
                float t = Mathf.Clamp01(v[i].y / Mathf.Max(0.01f, height));
                v[i].x += lean * t * t;
            }
            mesh.vertices = v;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }

        /// <summary>A small ball (a smooth icosphere of radius 0.5): spots on caps, glowing berries.</summary>
        public static Mesh Blob()
        {
            var verts = new List<Vector3>();
            var tris = new List<int>();
            SlimeMeshBuilder.Icosphere(verts, tris, 1);
            for (int i = 0; i < verts.Count; i++) verts[i] *= 0.5f;
            var mesh = new Mesh { name = "Blob" };
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>A leaf: a bent diamond standing from the origin up along y. Draw with a double-sided material.</summary>
        public static Mesh Leaf(float length, float width)
        {
            var mesh = new Mesh { name = "Leaf" };
            mesh.vertices = new[]
            {
                Vector3.zero,
                new Vector3(-width * 0.5f, length * 0.45f, 0.02f * length),
                new Vector3(width * 0.5f, length * 0.45f, 0.02f * length),
                new Vector3(0f, length, 0.12f * length)
            };
            mesh.uv = new[] { new Vector2(0.5f, 0f), new Vector2(0f, 0.45f), new Vector2(1f, 0.45f), new Vector2(0.5f, 1f) };
            mesh.triangles = new[] { 0, 2, 1, 1, 2, 3 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>The swaying path of a hanging vine from `top` straight down for `length` metres.</summary>
        public static Vector3[] VinePath(Vector3 top, float length, int seed, float sway, int points)
        {
            var rnd = new System.Random(seed);
            float p1 = (float)rnd.NextDouble() * Mathf.PI * 2f, p2 = (float)rnd.NextDouble() * Mathf.PI * 2f;
            var path = new Vector3[points];
            for (int i = 0; i < points; i++)
            {
                float t = i / (float)(points - 1);
                float amount = sway * t * t;      // fixed at the top, swinging more toward the end
                path[i] = top + new Vector3(Mathf.Sin(t * 5f + p1) * amount, -t * length, Mathf.Sin(t * 4f + p2) * amount * 0.8f);
            }
            return path;
        }
    }
}
