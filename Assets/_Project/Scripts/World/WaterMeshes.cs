using System.Collections.Generic;
using UnityEngine;

namespace Evolve.World
{
    /// <summary>Flat water surface meshes (all facing up, UV 0..1 across the surface), built in code.</summary>
    public static class WaterMeshes
    {
        /// <summary>A rectangle centred on the origin in the xz plane, split into cells of about `cell` metres.</summary>
        public static Mesh Rect(float width, float depth, float cell)
        {
            int nx = Mathf.Max(1, Mathf.RoundToInt(width / Mathf.Max(0.1f, cell)));
            int nz = Mathf.Max(1, Mathf.RoundToInt(depth / Mathf.Max(0.1f, cell)));
            var verts = new List<Vector3>((nx + 1) * (nz + 1));
            var uvs = new List<Vector2>(verts.Capacity);
            for (int z = 0; z <= nz; z++)
                for (int x = 0; x <= nx; x++)
                {
                    float u = x / (float)nx, v = z / (float)nz;
                    verts.Add(new Vector3((u - 0.5f) * width, 0f, (v - 0.5f) * depth));
                    uvs.Add(new Vector2(u, v));
                }
            var tris = new List<int>(nx * nz * 6);
            for (int z = 0; z < nz; z++)
                for (int x = 0; x < nx; x++)
                {
                    int a = z * (nx + 1) + x, b = (z + 1) * (nx + 1) + x, c = a + 1, d = b + 1;
                    AddUp(tris, verts, a, b, c);
                    AddUp(tris, verts, b, d, c);
                }
            return Make("WaterRect", verts, uvs, tris);
        }

        /// <summary>A disc centred on the origin: one centre vertex and rings of vertices spaced about `cell` metres.</summary>
        public static Mesh Disc(float radius, float cell)
        {
            int rings = Mathf.Max(2, Mathf.RoundToInt(radius / Mathf.Max(0.1f, cell)));
            int sectors = Mathf.Max(12, Mathf.RoundToInt(2f * Mathf.PI * radius / Mathf.Max(0.1f, cell)));
            var verts = new List<Vector3> { Vector3.zero };
            var uvs = new List<Vector2> { new Vector2(0.5f, 0.5f) };
            for (int r = 1; r <= rings; r++)
            {
                float rr = radius * r / rings;
                for (int s = 0; s < sectors; s++)
                {
                    float a = s / (float)sectors * Mathf.PI * 2f;
                    var p = new Vector3(Mathf.Cos(a) * rr, 0f, Mathf.Sin(a) * rr);
                    verts.Add(p);
                    uvs.Add(new Vector2(p.x / (2f * radius) + 0.5f, p.z / (2f * radius) + 0.5f));
                }
            }
            var tris = new List<int>();
            for (int s = 0; s < sectors; s++) AddUp(tris, verts, 0, 1 + s, 1 + (s + 1) % sectors);
            for (int r = 1; r < rings; r++)
                for (int s = 0; s < sectors; s++)
                {
                    int s2 = (s + 1) % sectors;
                    int a = 1 + (r - 1) * sectors + s, b = 1 + (r - 1) * sectors + s2;
                    int c = 1 + r * sectors + s, d = 1 + r * sectors + s2;
                    AddUp(tris, verts, a, c, b);
                    AddUp(tris, verts, b, c, d);
                }
            return Make("WaterDisc", verts, uvs, tris);
        }

        /// <summary>
        /// A ring around a disc of `radius`: from `outer` metres outside the edge (v = 0) to `inner` metres inside it
        /// (v = 1). u runs once around, scaled so the pattern repeats about every 2 m.
        /// </summary>
        public static Mesh DiscRing(float radius, float outer, float inner, int sectors)
        {
            sectors = Mathf.Max(12, sectors);
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            float circumference = 2f * Mathf.PI * radius;
            for (int s = 0; s <= sectors; s++)
            {
                float a = s / (float)sectors * Mathf.PI * 2f;
                var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                float u = s / (float)sectors * circumference / 2f;
                verts.Add(dir * (radius + outer)); uvs.Add(new Vector2(u, 0f));
                verts.Add(dir * Mathf.Max(0.05f, radius - inner)); uvs.Add(new Vector2(u, 1f));
            }
            var tris = new List<int>();
            for (int s = 0; s < sectors; s++)
            {
                int o0 = s * 2, i0 = o0 + 1, o1 = o0 + 2, i1 = o0 + 3;
                AddUp(tris, verts, o0, i0, o1);
                AddUp(tris, verts, o1, i0, i1);
            }
            return Make("WaterDiscRing", verts, uvs, tris);
        }

        /// <summary>The same kind of ring around a rectangle (outer rectangle v = 0, inner rectangle v = 1).</summary>
        public static Mesh RectRing(float width, float depth, float outer, float inner, float cell)
        {
            float hw = width * 0.5f, hd = depth * 0.5f;
            var corners = new[]
            {
                new Vector2(-1f, -1f), new Vector2(1f, -1f), new Vector2(1f, 1f), new Vector2(-1f, 1f)
            };
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            float travelled = 0f;
            Vector3 lastOuter = Vector3.zero;
            bool first = true;
            for (int e = 0; e < 4; e++)
            {
                Vector2 c0 = corners[e], c1 = corners[(e + 1) % 4];
                float edgeLength = e % 2 == 0 ? width : depth;
                int n = Mathf.Max(1, Mathf.RoundToInt(edgeLength / Mathf.Max(0.1f, cell)));
                for (int k = 0; k < n; k++)
                {
                    Vector2 c = Vector2.Lerp(c0, c1, k / (float)n);
                    var o = new Vector3(c.x * (hw + outer), 0f, c.y * (hd + outer));
                    var i = new Vector3(c.x * Mathf.Max(0.05f, hw - inner), 0f, c.y * Mathf.Max(0.05f, hd - inner));
                    // The ring's points on the straight parts: move along the edge, not along the diagonal.
                    float t = k / (float)n;
                    var along = new Vector2(Mathf.Lerp(c0.x, c1.x, t), Mathf.Lerp(c0.y, c1.y, t));
                    if (e % 2 == 0) { o.x = along.x * (hw + outer); i.x = along.x * Mathf.Max(0.05f, hw - inner); }
                    else { o.z = along.y * (hd + outer); i.z = along.y * Mathf.Max(0.05f, hd - inner); }
                    if (!first) travelled += Vector3.Distance(o, lastOuter);
                    lastOuter = o; first = false;
                    verts.Add(o); uvs.Add(new Vector2(travelled / 2f, 0f));
                    verts.Add(i); uvs.Add(new Vector2(travelled / 2f, 1f));
                }
            }
            // close the loop
            verts.Add(verts[0]); uvs.Add(new Vector2((travelled + Vector3.Distance(lastOuter, verts[0])) / 2f, 0f));
            verts.Add(verts[1]); uvs.Add(new Vector2((travelled + Vector3.Distance(lastOuter, verts[0])) / 2f, 1f));
            var tris = new List<int>();
            int pairs = verts.Count / 2;
            for (int s = 0; s < pairs - 1; s++)
            {
                int o0 = s * 2, i0 = o0 + 1, o1 = o0 + 2, i1 = o0 + 3;
                AddUp(tris, verts, o0, i0, o1);
                AddUp(tris, verts, o1, i0, i1);
            }
            return Make("WaterRectRing", verts, uvs, tris);
        }

        // Adds a triangle facing up (Unity front faces are clockwise: normal = cross(b - a, c - a)).
        static void AddUp(List<int> tris, List<Vector3> verts, int a, int b, int c)
        {
            if (Vector3.Cross(verts[b] - verts[a], verts[c] - verts[a]).y < 0f) { int t = b; b = c; c = t; }
            tris.Add(a); tris.Add(b); tris.Add(c);
        }

        static Mesh Make(string name, List<Vector3> verts, List<Vector2> uvs, List<int> tris)
        {
            var mesh = new Mesh { name = name };
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            var normals = new Vector3[verts.Count];
            for (int i = 0; i < normals.Length; i++) normals[i] = Vector3.up;
            mesh.normals = normals;
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
