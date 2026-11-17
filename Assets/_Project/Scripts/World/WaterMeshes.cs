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
