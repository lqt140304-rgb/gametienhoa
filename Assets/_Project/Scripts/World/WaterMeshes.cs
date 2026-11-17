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
        /// A surface filling an outline (star-shaped around the origin): rings of vertices shrink from the shore to the
        /// centre, spaced about `cell` metres apart. UVs follow the outline's bounding box.
        /// </summary>
        public static Mesh Organic(Vector2[] outline, float cell)
        {
            int n = outline.Length;
            float extent = 0f;
            Vector2 min = outline[0], max = outline[0];
            foreach (var p in outline)
            {
                extent = Mathf.Max(extent, p.magnitude);
                min = Vector2.Min(min, p); max = Vector2.Max(max, p);
            }
            int rings = Mathf.Max(2, Mathf.RoundToInt(extent / Mathf.Max(0.1f, cell)));
            Vector2 size = new Vector2(Mathf.Max(1e-3f, max.x - min.x), Mathf.Max(1e-3f, max.y - min.y));

            var verts = new List<Vector3> { Vector3.zero };
            var uvs = new List<Vector2> { new Vector2((0f - min.x) / size.x, (0f - min.y) / size.y) };
            for (int r = 1; r <= rings; r++)
            {
                float t = r / (float)rings;
                for (int k = 0; k < n; k++)
                {
                    Vector2 p = outline[k] * t;
                    verts.Add(new Vector3(p.x, 0f, p.y));
                    uvs.Add(new Vector2((p.x - min.x) / size.x, (p.y - min.y) / size.y));
                }
            }
            var tris = new List<int>();
            for (int k = 0; k < n; k++) AddUp(tris, verts, 0, 1 + k, 1 + (k + 1) % n);
            for (int r = 1; r < rings; r++)
                for (int k = 0; k < n; k++)
                {
                    int k2 = (k + 1) % n;
                    int a = 1 + (r - 1) * n + k, b = 1 + (r - 1) * n + k2;
                    int c = 1 + r * n + k, d = 1 + r * n + k2;
                    AddUp(tris, verts, a, c, b);
                    AddUp(tris, verts, b, c, d);
                }
            return Make("WaterOrganic", verts, uvs, tris);
        }

        /// <summary>
        /// A band along an outline: `outer` metres outside the shore (v = 0) to `inner` metres inside it (v = 1).
        /// The band's width wanders a little (seeded), so it never looks like a ruled line. u runs along the shore,
        /// repeating about every 2 m and closing exactly.
        /// </summary>
        public static Mesh Band(Vector2[] outline, float outer, float inner, int seed)
        {
            int n = outline.Length;
            var rnd = new System.Random(seed);
            float p1 = (float)rnd.NextDouble() * Mathf.PI * 2f, p2 = (float)rnd.NextDouble() * Mathf.PI * 2f;

            float perimeter = 0f;
            for (int k = 0; k < n; k++) perimeter += Vector2.Distance(outline[k], outline[(k + 1) % n]);
            float repeats = Mathf.Max(1f, Mathf.Round(perimeter / 2f));

            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            for (int k = 0; k <= n; k++)
            {
                int i = k % n;
                Vector2 tangent = (outline[(i + 1) % n] - outline[(i - 1 + n) % n]).normalized;
                Vector2 normal = new Vector2(tangent.y, -tangent.x);
                if (Vector2.Dot(normal, outline[i]) < 0f) normal = -normal; // always pointing away from the pond
                float u = k / (float)n;
                float wobble = 0.75f + 0.25f * Mathf.Sin(u * Mathf.PI * 2f * 7f + p1) + 0.15f * Mathf.Sin(u * Mathf.PI * 2f * 13f + p2);
                Vector2 o = outline[i] + normal * (outer * wobble);
                Vector2 inn = outline[i] - normal * inner;
                verts.Add(new Vector3(o.x, 0f, o.y)); uvs.Add(new Vector2(u * repeats, 0f));
                verts.Add(new Vector3(inn.x, 0f, inn.y)); uvs.Add(new Vector2(u * repeats, 1f));
            }
            var tris = new List<int>();
            for (int k = 0; k < n; k++)
            {
                int o0 = k * 2, i0 = o0 + 1, o1 = o0 + 2, i1 = o0 + 3;
                AddUp(tris, verts, o0, i0, o1);
                AddUp(tris, verts, o1, i0, i1);
            }
            return Make("WaterBand", verts, uvs, tris);
        }

        /// <summary>Removes the triangles whose centre (in the mesh's own space) is chosen by `drop`.</summary>
        public static void RemoveTriangles(Mesh mesh, System.Func<Vector3, bool> drop)
        {
            var v = mesh.vertices;
            var t = mesh.triangles;
            var keep = new List<int>(t.Length);
            for (int i = 0; i < t.Length; i += 3)
            {
                Vector3 c = (v[t[i]] + v[t[i + 1]] + v[t[i + 2]]) / 3f;
                if (drop(c)) continue;
                keep.Add(t[i]); keep.Add(t[i + 1]); keep.Add(t[i + 2]);
            }
            mesh.SetTriangles(keep, 0);
            mesh.RecalculateBounds();
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
