using Evolve.World;
using NUnit.Framework;
using UnityEngine;

namespace Evolve.Tests.EditMode
{
    public class DomeMeshTests
    {
        const float Radius = 300f, Height = 85f;
        const int Segments = 24, Rings = 8;

        Mesh mesh;

        [SetUp] public void SetUp() => mesh = DomeMesh.Build(Radius, Height, Segments, Rings);
        [TearDown] public void TearDown() => Object.DestroyImmediate(mesh);

        [Test]
        public void VertexAndTriangleCounts()
        {
            // grid of (rings + 1) x (segments + 1) vertices; two triangles per quad = rings * segments * 2 triangles
            Assert.AreEqual((Rings + 1) * (Segments + 1), mesh.vertexCount);
            Assert.AreEqual(Rings * Segments * 2 * 3, mesh.triangles.Length);
        }

        [Test]
        public void NormalsPointInward()
        {
            var v = mesh.vertices;
            var n = mesh.normals;
            Assert.AreEqual(v.Length, n.Length);
            for (int i = 0; i < v.Length; i++)
                if (v[i].y > 0.1f)
                    Assert.Greater(Vector3.Dot(n[i], Vector3.zero - v[i]), 0f, $"vertex {i}");
        }

        [Test]
        public void ApexAndRimHeights()
        {
            float max = float.MinValue, min = float.MaxValue;
            foreach (var p in mesh.vertices) { max = Mathf.Max(max, p.y); min = Mathf.Min(min, p.y); }
            Assert.AreEqual(Height, max, 1e-3f);
            Assert.AreEqual(0f, min, 0.01f);
        }

        [Test]
        public void WindingMatchesNormals()
        {
            var v = mesh.vertices;
            var n = mesh.normals;
            var t = mesh.triangles;
            int tri = (Rings / 2) * Segments * 2 * 3 + 3 * 5; // a triangle in a middle ring, not at the apex
            Vector3 face = Vector3.Cross(v[t[tri + 1]] - v[t[tri]], v[t[tri + 2]] - v[t[tri]]);
            Assert.Greater(Vector3.Dot(face, n[t[tri]]), 0f);
        }
    }
}
