using System.Collections.Generic;
using Evolve.Visuals;
using UnityEngine;

namespace Evolve.World
{
    /// <summary>Small ground decorations built in code: low-poly rocks with a moss cap, grass tufts, winding mud paths.</summary>
    public static class GroundMeshes
    {
        /// <summary>
        /// A rock about 1 m wide: 80 flat-shaded triangles, lumpy, flattened below. `moss` gets a slightly larger copy of
        /// the upward-facing triangles (a cap of moss on top).
        /// </summary>
        public static Mesh Rock(int seed, out Mesh moss, float mossMinUp = 0.55f)
        {
            var verts = new List<Vector3>();
            var tris = new List<int>();
            SlimeMeshBuilder.Icosphere(verts, tris, 1);
            var rnd = new System.Random(seed);
            var shaped = new Vector3[verts.Count];
            for (int i = 0; i < verts.Count; i++)
            {
                Vector3 v = verts[i] * (0.8f + (float)rnd.NextDouble() * 0.4f) * 0.5f;
                v.y *= 0.65f;
                if (v.y < -0.12f) v.y = -0.12f; // flat bottom, sunk into the ground a little
                shaped[i] = v;
            }
            var rockVerts = new List<Vector3>();
            var mossVerts = new List<Vector3>();
            for (int i = 0; i < tris.Count; i += 3)
            {
                Vector3 a = shaped[tris[i]], b = shaped[tris[i + 1]], c = shaped[tris[i + 2]];
                rockVerts.Add(a); rockVerts.Add(b); rockVerts.Add(c);
                Vector3 n = Vector3.Cross(b - a, c - a).normalized;
                if (n.y > mossMinUp)
                {
                    var lift = Vector3.up * 0.012f;
                    mossVerts.Add(a * 1.04f + lift); mossVerts.Add(b * 1.04f + lift); mossVerts.Add(c * 1.04f + lift);
                }
            }
            var rock = FromTriangles("Rock", rockVerts);
            moss = FromTriangles("RockMoss", mossVerts);
            return rock;
        }

        static Mesh FromTriangles(string name, List<Vector3> verts)
        {
            var mesh = new Mesh { name = name };
            var idx = new int[verts.Count];
            for (int i = 0; i < idx.Length; i++) idx[i] = i;
            mesh.SetVertices(verts);
            mesh.SetTriangles(idx, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Grass tufts: five blades each (a thin triangle leaning outward). Draw with a double-sided material.</summary>
        public static Mesh Tufts(IList<Vector3> positions, int seed, float height)
        {
            var rnd = new System.Random(seed);
            var verts = new List<Vector3>(positions.Count * 15);
            var normals = new List<Vector3>(verts.Capacity);
            var uvs = new List<Vector2>(verts.Capacity);
            var idx = new List<int>(verts.Capacity);
            foreach (var p in positions)
            {
                for (int b = 0; b < 5; b++)
                {
                    float a = (float)rnd.NextDouble() * Mathf.PI * 2f;
                    var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                    var side = new Vector3(-dir.z, 0f, dir.x);
                    float h = height * (0.7f + (float)rnd.NextDouble() * 0.6f);
                    float w = height * 0.06f;
                    var root = p + dir * (0.05f + (float)rnd.NextDouble() * 0.12f);
                    int s = verts.Count;
                    verts.Add(root + side * w); verts.Add(root - side * w); verts.Add(root + Vector3.up * h + dir * (0.18f * h));
                    uvs.Add(new Vector2(0f, 0f)); uvs.Add(new Vector2(1f, 0f)); uvs.Add(new Vector2(0.5f, 1f));
                    for (int k = 0; k < 3; k++) normals.Add(Vector3.up); // lit like the ground, soft
                    idx.Add(s); idx.Add(s + 2); idx.Add(s + 1);
                }
            }
            var mesh = new Mesh { name = "Tufts" };
            if (verts.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(idx, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Fallen leaves lying flat: a pointed oval each, random direction and size. Use a double-sided material.</summary>
        public static Mesh FallenLeaves(IList<Vector3> positions, int seed, float size)
        {
            var rnd = new System.Random(seed);
            var verts = new List<Vector3>(positions.Count * 4);
            var normals = new List<Vector3>(positions.Count * 4);
            var idx = new List<int>(positions.Count * 6);
            foreach (var p in positions)
            {
                float a = (float)rnd.NextDouble() * Mathf.PI * 2f;
                var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                var side = new Vector3(-dir.z, 0f, dir.x);
                float len = size * (0.7f + (float)rnd.NextDouble() * 0.7f), width = len * (0.35f + (float)rnd.NextDouble() * 0.15f);
                int s = verts.Count;
                verts.Add(p - dir * len * 0.5f);
                verts.Add(p - side * width * 0.5f + dir * len * 0.1f);
                verts.Add(p + side * width * 0.5f + dir * len * 0.1f);
                verts.Add(p + dir * len * 0.5f);
                for (int k = 0; k < 4; k++) normals.Add(Vector3.up);
                idx.Add(s); idx.Add(s + 1); idx.Add(s + 2);
                idx.Add(s + 1); idx.Add(s + 3); idx.Add(s + 2);
            }
            var mesh = new Mesh { name = "FallenLeaves" };
            if (verts.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetTriangles(idx, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>A line from a to b that winds sideways in between (zero sideways offset at both ends).</summary>
        public static Vector2[] WindingLine(Vector2 a, Vector2 b, int seed, float amplitude, float step, float waveLength = 14f)
        {
            float length = Vector2.Distance(a, b);
            int n = Mathf.Max(2, Mathf.RoundToInt(length / Mathf.Max(0.5f, step)));
            var rnd = new System.Random(seed);
            float p1 = (float)rnd.NextDouble() * Mathf.PI * 2f, p2 = (float)rnd.NextDouble() * Mathf.PI * 2f;
            int waves = Mathf.Max(1, Mathf.RoundToInt(length / Mathf.Max(4f, waveLength)));
            Vector2 dir = (b - a).normalized, side = new Vector2(-dir.y, dir.x);
            var line = new Vector2[n + 1];
            for (int i = 0; i <= n; i++)
            {
                float t = i / (float)n;
                float ends = Mathf.Sin(t * Mathf.PI);
                float offset = amplitude * ends * (Mathf.Sin(t * Mathf.PI * 2f * waves + p1) + 0.4f * Mathf.Sin(t * Mathf.PI * 2f * waves * 2.3f + p2)) / 1.4f;
                line[i] = Vector2.Lerp(a, b, t) + side * offset;
            }
            return line;
        }

        /// <summary>
        /// A flat ribbon following a line (x, z), `halfWidth` to each side (width wanders a little), lying `lift` above
        /// the ground whose height is given by `yAt`. v = 0 / 1 on the two edges, u runs along (a repeat every 3 m).
        /// </summary>
        public static Mesh Ribbon(IList<Vector2> line, float halfWidth, int seed, System.Func<float, float, float> yAt, float lift)
        {
            var rnd = new System.Random(seed);
            float ph = (float)rnd.NextDouble() * Mathf.PI * 2f;
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            float travelled = 0f;
            for (int i = 0; i < line.Count; i++)
            {
                Vector2 prev = line[Mathf.Max(0, i - 1)], next = line[Mathf.Min(line.Count - 1, i + 1)];
                Vector2 t = (next - prev).normalized;
                Vector2 n = new Vector2(-t.y, t.x);
                if (i > 0) travelled += Vector2.Distance(line[i], line[i - 1]);
                float w = halfWidth * (0.8f + 0.25f * Mathf.Sin(travelled * 0.9f + ph));
                Vector2 l = line[i] + n * w, r = line[i] - n * w;
                verts.Add(new Vector3(l.x, yAt(l.x, l.y) + lift, l.y)); uvs.Add(new Vector2(travelled / 3f, 0f));
                verts.Add(new Vector3(r.x, yAt(r.x, r.y) + lift, r.y)); uvs.Add(new Vector2(travelled / 3f, 1f));
            }
            var tris = new List<int>();
            for (int i = 0; i < line.Count - 1; i++)
            {
                int a = i * 2, b = a + 1, c = a + 2, d = a + 3;
                Up(tris, verts, a, b, c);
                Up(tris, verts, c, b, d);
            }
            var mesh = new Mesh { name = "Ribbon" };
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            var normals = new Vector3[verts.Count];
            for (int i = 0; i < normals.Length; i++) normals[i] = Vector3.up;
            mesh.normals = normals;
            mesh.RecalculateBounds();
            return mesh;
        }

        static void Up(List<int> tris, List<Vector3> v, int a, int b, int c)
        {
            if (Vector3.Cross(v[b] - v[a], v[c] - v[a]).y < 0f) { int t = b; b = c; c = t; }
            tris.Add(a); tris.Add(b); tris.Add(c);
        }
    }
}
