using System.Collections.Generic;
using UnityEngine;

namespace Evolve.World
{
    /// <summary>Hollow shapes built in code for caves and tunnels: a dome shell and an arched passage along a path.</summary>
    public static class CaveMeshes
    {
        /// <summary>
        /// The shell of a dome: the upper half of an ellipsoid with radii <paramref name="rx"/>, <paramref name="rz"/> on the
        /// ground and <paramref name="height"/> up, its wall wandering in and out a little (<paramref name="wobble"/>, seeded).
        /// Base ring at y = 0, centred on the origin. One surface: draw it with a double-sided material.
        /// </summary>
        public static Mesh Shell(float rx, float rz, float height, int sides, int rings, int seed, float wobble)
        {
            var rnd = new System.Random(seed);
            float p1 = (float)rnd.NextDouble() * 10f, p2 = (float)rnd.NextDouble() * 10f;
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            for (int r = 0; r < rings; r++)
            {
                float phi = r / (float)rings * Mathf.PI * 0.5f;
                float ring = Mathf.Cos(phi), y = height * Mathf.Sin(phi);
                for (int s = 0; s < sides; s++)
                {
                    float a = s * Mathf.PI * 2f / sides;
                    float bump = 1f + wobble * (ZoneLayout.Noise(Mathf.Cos(a) * 2f + p1, Mathf.Sin(a) * 2f + y * 0.25f + p2) - 0.5f) * 2f;
                    var v = new Vector3(Mathf.Cos(a) * rx * ring * bump, y, Mathf.Sin(a) * rz * ring * bump);
                    verts.Add(v);
                    uvs.Add(new Vector2(a * (rx + rz) * 0.5f / 4f, y / 4f));
                }
            }
            int top = verts.Count;
            verts.Add(new Vector3(0f, height, 0f));
            uvs.Add(new Vector2(0f, height / 4f));
            var tris = new List<int>();
            for (int r = 0; r < rings - 1; r++)
                for (int s = 0; s < sides; s++)
                {
                    int a = r * sides + s, b = r * sides + (s + 1) % sides, c = a + sides, d = b + sides;
                    tris.Add(a); tris.Add(c); tris.Add(b);
                    tris.Add(b); tris.Add(c); tris.Add(d);
                }
            int last = (rings - 1) * sides;
            for (int s = 0; s < sides; s++) { tris.Add(last + s); tris.Add(top); tris.Add(last + (s + 1) % sides); }
            var mesh = new Mesh { name = "CaveShell" };
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// An arched passage along <paramref name="path"/> (points on the ground): a half-ellipse
        /// <paramref name="width"/> wide and <paramref name="height"/> high at every point, its feet sunk 0.5 m into the
        /// ground. World-space vertices. One surface: draw it double-sided.
        /// </summary>
        public static Mesh ArchTunnel(IList<Vector3> path, float width, float height, int sides)
        {
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            float along = 0f;
            for (int i = 0; i < path.Count; i++)
            {
                if (i > 0) along += Vector3.Distance(path[i], path[i - 1]);
                Vector3 dir = path[Mathf.Min(i + 1, path.Count - 1)] - path[Mathf.Max(i - 1, 0)];
                dir.y = 0f;
                dir = dir.sqrMagnitude > 1e-6f ? dir.normalized : Vector3.forward;
                Vector3 side = new Vector3(dir.z, 0f, -dir.x);
                for (int k = 0; k <= sides; k++)
                {
                    float a = k * Mathf.PI / sides;
                    float sink = k == 0 || k == sides ? -0.5f : 0f;
                    verts.Add(path[i] + side * Mathf.Cos(a) * width * 0.5f + Vector3.up * (Mathf.Sin(a) * height + sink));
                    uvs.Add(new Vector2(k / (float)sides * 3f, along / 4f));
                }
            }
            var tris = new List<int>();
            int row = sides + 1;
            for (int i = 0; i + 1 < path.Count; i++)
                for (int k = 0; k < sides; k++)
                {
                    int a = i * row + k, b = a + 1, c = a + row, d = c + 1;
                    tris.Add(a); tris.Add(b); tris.Add(c);
                    tris.Add(b); tris.Add(d); tris.Add(c);
                }
            var mesh = new Mesh { name = "ArchTunnel", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// A solid upright disc (a flat cylinder): radius <paramref name="radius"/>, from y = 0 up to <paramref name="height"/>,
        /// base on the origin. Closed (top, side), so it works with a MeshCollider and an ordinary single-sided material.
        /// </summary>
        public static Mesh Disc(float radius, float height, int sides)
        {
            var verts = new List<Vector3>();
            var tris = new List<int>();
            for (int i = 0; i < sides; i++)
            {
                float a = i * Mathf.PI * 2f / sides;
                var d = new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius);
                verts.Add(d);                                  // bottom ring
                verts.Add(d + Vector3.up * height);            // top ring
            }
            int top = verts.Count;
            verts.Add(Vector3.up * height);                    // top centre
            for (int i = 0; i < sides; i++)
            {
                int n = (i + 1) % sides;
                int b0 = i * 2, t0 = i * 2 + 1, b1 = n * 2, t1 = n * 2 + 1;
                tris.Add(b0); tris.Add(t0); tris.Add(b1);      // side
                tris.Add(b1); tris.Add(t0); tris.Add(t1);
                tris.Add(top); tris.Add(t1); tris.Add(t0);     // top
            }
            var mesh = new Mesh { name = "Disc" };
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
