using System.Collections.Generic;
using UnityEngine;

namespace Evolve.Abilities
{
    /// <summary>Meshes the skill effects need, built once from code: a flat ring, a faceted crystal and a sphere.</summary>
    public static class SkillFxMeshes
    {
        static Mesh ring, crystal, sphere;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { ring = crystal = sphere = null; }

        public static Mesh Sphere()
        {
            if (sphere != null) return sphere;
            var g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere = g.GetComponent<MeshFilter>().sharedMesh;
            Object.Destroy(g);
            return sphere;
        }

        /// <summary>A flat ring of radius 1 on the ground plane; the inner edge is transparent so it reads as a soft wave, not a hoop.</summary>
        public static Mesh Ring()
        {
            if (ring != null) return ring;
            const int n = 48;
            const float inner = 0.74f;
            var v = new Vector3[n * 2];
            var col = new Color[n * 2];
            var nor = new Vector3[n * 2];
            var tri = new int[n * 6];
            for (int i = 0; i < n; i++)
            {
                float a = i / (float)n * Mathf.PI * 2f;
                var d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                v[i * 2] = d * inner; col[i * 2] = new Color(1f, 1f, 1f, 0f);
                v[i * 2 + 1] = d; col[i * 2 + 1] = Color.white;
                nor[i * 2] = nor[i * 2 + 1] = Vector3.up;
                int p = i * 2, q = ((i + 1) % n) * 2;
                int k = i * 6;
                tri[k] = p; tri[k + 1] = p + 1; tri[k + 2] = q;
                tri[k + 3] = q; tri[k + 4] = p + 1; tri[k + 5] = q + 1;
            }
            ring = new Mesh { name = "SkillRing", vertices = v, colors = col, normals = nor, triangles = tri, hideFlags = HideFlags.HideAndDontSave };
            return ring;
        }

        /// <summary>A six-sided crystal, base at y = 0, tip at y = 1 (so scaling y by a length reaches that far). Flat facets.</summary>
        public static Mesh Crystal()
        {
            if (crystal != null) return crystal;
            const int sides = 6;
            const float baseR = 0.20f, shoulderR = 0.30f, shoulderY = 0.42f;
            var verts = new List<Vector3>();
            var tris = new List<int>();
            Vector3 Ring(int i, float r, float y)
            {
                float a = i / (float)sides * Mathf.PI * 2f;
                return new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r);
            }
            void Tri(Vector3 a, Vector3 b, Vector3 c)
            {
                int s = verts.Count;
                verts.Add(a); verts.Add(b); verts.Add(c);
                tris.Add(s); tris.Add(s + 1); tris.Add(s + 2);
            }
            var tip = new Vector3(0f, 1f, 0f);
            for (int i = 0; i < sides; i++)
            {
                int j = (i + 1) % sides;
                Vector3 b0 = Ring(i, baseR, 0f), b1 = Ring(j, baseR, 0f), s0 = Ring(i, shoulderR, shoulderY), s1 = Ring(j, shoulderR, shoulderY);
                Tri(b0, s0, s1); Tri(b0, s1, b1);   // lower band
                Tri(s0, tip, s1);                   // upper point
            }
            crystal = new Mesh { name = "SkillCrystal", hideFlags = HideFlags.HideAndDontSave };
            crystal.SetVertices(verts);
            crystal.SetTriangles(tris, 0);
            crystal.RecalculateNormals();
            crystal.RecalculateBounds();
            return crystal;
        }
    }
}
