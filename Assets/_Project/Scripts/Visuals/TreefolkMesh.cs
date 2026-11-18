using System.Collections.Generic;
using Evolve.World;
using UnityEngine;

namespace Evolve.Visuals
{
    /// <summary>
    /// A placeholder treefolk built in code: a short gnarled trunk on three root feet, two branch arms and a crown of moss.
    /// Origin at its feet, facing +z. About <c>height</c> tall.
    /// </summary>
    public static class TreefolkMesh
    {
        public static Mesh Build(int seed, float height)
        {
            var rnd = new System.Random(seed);
            float h = height, body = h * 0.82f;
            var parts = new List<CombineInstance>();
            void Add(Mesh m) => parts.Add(new CombineInstance { mesh = m, transform = Matrix4x4.identity });

            // trunk, leaning a little
            float lean = ((float)rnd.NextDouble() - 0.5f) * 0.08f;
            var trunk = new Vector3[6];
            for (int i = 0; i < trunk.Length; i++)
            {
                float t = i / (float)(trunk.Length - 1);
                trunk[i] = new Vector3(Mathf.Sin(t * 2.2f) * lean * h, t * body, 0f);
            }
            Add(RootMeshes.RootTube(trunk, h * 0.17f, h * 0.12f, seed, 0.25f, 9));

            // three root feet, splayed and dug a little into the ground
            for (int f = 0; f < 3; f++)
            {
                float a = f * Mathf.PI * 2f / 3f + (float)rnd.NextDouble() * 0.4f;
                var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                Add(RootMeshes.RootTube(new[] { Vector3.up * h * 0.18f, dir * h * 0.16f + Vector3.up * h * 0.05f, dir * h * 0.3f - Vector3.up * 0.03f },
                    h * 0.07f, h * 0.035f, seed + 10 + f, 0.2f, 6));
            }

            // two arms reaching out and a little down
            foreach (float side in new[] { -1f, 1f })
            {
                var start = new Vector3(side * h * 0.1f, body * 0.68f, 0.02f);
                Add(RootMeshes.RootTube(new[] { start, start + new Vector3(side * h * 0.2f, h * 0.02f, h * 0.06f), start + new Vector3(side * h * 0.32f, -h * 0.14f, h * 0.1f) },
                    h * 0.055f, h * 0.025f, seed + 20 + (side > 0 ? 1 : 0), 0.2f, 6));
            }

            // a crown of moss (a squashed ball) on top
            var crown = FloraMeshes.Blob();
            parts.Add(new CombineInstance { mesh = crown, transform = Matrix4x4.TRS(new Vector3(lean * h, body, 0f), Quaternion.identity, new Vector3(h * 0.5f, h * 0.34f, h * 0.48f)) });

            var mesh = new Mesh { name = "Treefolk" };
            mesh.CombineMeshes(parts.ToArray(), true, true);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
