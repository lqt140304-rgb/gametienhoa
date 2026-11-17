using System.Collections.Generic;
using UnityEngine;

namespace Evolve.Visuals
{
    /// <summary>
    /// Builds the faceted gel dome of the primordial slime and small helper meshes, all in code.
    /// Size matches the old 1 m sphere: bottom at y = -0.45, top at about y = 0.28, radius about 0.5.
    /// </summary>
    public static class SlimeMeshBuilder
    {
        public const float Bottom = -0.45f;
        public const float Top = 0.28f;

        /// <summary>Flat-shaded, slightly irregular dome with a soft skirt (80 triangles).</summary>
        public static Mesh Build(int seed = 7)
        {
            var verts = new List<Vector3>();
            var tris = new List<int>();
            Icosphere(verts, tris);

            var random = new System.Random(seed);
            var bump = new float[verts.Count];
            for (int i = 0; i < bump.Length; i++) bump[i] = (float)random.NextDouble() * 2f - 1f;

            var shaped = new Vector3[verts.Count];
            for (int i = 0; i < verts.Count; i++)
            {
                Vector3 v = verts[i];
                float skirt = v.y < 0f ? 1.1f : 1f;
                float radial = 0.5f * skirt * (1f + 0.07f * bump[i]);
                float y = v.y >= 0f ? Bottom + v.y * (Top - Bottom) : Bottom;
                shaped[i] = new Vector3(v.x * radial, y, v.z * radial);
            }

            // Flat shading: every triangle gets its own three vertices.
            var meshVerts = new Vector3[tris.Count];
            var meshTris = new int[tris.Count];
            for (int i = 0; i < tris.Count; i++) { meshVerts[i] = shaped[tris[i]]; meshTris[i] = i; }

            var mesh = new Mesh { name = "SlimeBody" };
            mesh.vertices = meshVerts;
            mesh.triangles = meshTris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>A cone with its base on y = 0 and its tip at y = height (for gel spikes).</summary>
        public static Mesh Cone(int sides, float radius, float height)
        {
            sides = Mathf.Max(3, sides);
            var verts = new List<Vector3>();
            var tris = new List<int>();
            for (int i = 0; i < sides; i++)
            {
                float a0 = i / (float)sides * Mathf.PI * 2f, a1 = (i + 1) / (float)sides * Mathf.PI * 2f;
                var p0 = new Vector3(Mathf.Cos(a0) * radius, 0f, Mathf.Sin(a0) * radius);
                var p1 = new Vector3(Mathf.Cos(a1) * radius, 0f, Mathf.Sin(a1) * radius);
                int s = verts.Count;
                verts.Add(p0); verts.Add(new Vector3(0f, height, 0f)); verts.Add(p1); // side (faces outward)
                tris.Add(s); tris.Add(s + 1); tris.Add(s + 2);
            }
            var mesh = new Mesh { name = "GelCone" };
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        static void Icosphere(List<Vector3> verts, List<int> tris)
        {
            float t = (1f + Mathf.Sqrt(5f)) / 2f;
            var baseVerts = new[]
            {
                new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0),
                new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t),
                new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1)
            };
            foreach (var v in baseVerts) verts.Add(v.normalized);
            var faces = new[]
            {
                0,11,5, 0,5,1, 0,1,7, 0,7,10, 0,10,11, 1,5,9, 5,11,4, 11,10,2, 10,7,6, 7,1,8,
                3,9,4, 3,4,2, 3,2,6, 3,6,8, 3,8,9, 4,9,5, 2,4,11, 6,2,10, 8,6,7, 9,8,1
            };

            // One subdivision step: 20 -> 80 triangles. Winding is kept (outward facing).
            var cache = new Dictionary<long, int>();
            for (int f = 0; f < faces.Length; f += 3)
            {
                int a = faces[f], b = faces[f + 1], c = faces[f + 2];
                int ab = Mid(verts, cache, a, b), bc = Mid(verts, cache, b, c), ca = Mid(verts, cache, c, a);
                tris.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
            }

            // Make every triangle face outward (Unity front faces are clockwise: normal = cross(b - a, c - a)).
            for (int i = 0; i < tris.Count; i += 3)
            {
                Vector3 a = verts[tris[i]], b = verts[tris[i + 1]], c = verts[tris[i + 2]];
                if (Vector3.Dot(Vector3.Cross(b - a, c - a), a + b + c) < 0f)
                    (tris[i + 1], tris[i + 2]) = (tris[i + 2], tris[i + 1]);
            }
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
