using Evolve.World;
using NUnit.Framework;
using UnityEngine;

namespace Evolve.Tests.EditMode
{
    public class WaterMeshesTests
    {
        [Test] public void RectVertexAndTriangleCounts()
        {
            var mesh = WaterMeshes.Rect(10f, 4f, 1f); // 10 x 4 cells
            Assert.AreEqual(11 * 5, mesh.vertexCount);
            Assert.AreEqual(10 * 4 * 2, mesh.triangles.Length / 3);
        }

        [Test] public void RectHasRequestedSizeAndIsFlat()
        {
            var b = WaterMeshes.Rect(24f, 8f, 1.25f).bounds;
            Assert.AreEqual(24f, b.size.x, 0.01f);
            Assert.AreEqual(8f, b.size.z, 0.01f);
            Assert.AreEqual(0f, b.size.y, 0.001f);
        }

        [Test] public void DiscStaysInsideItsRadius()
        {
            var mesh = WaterMeshes.Disc(15f, 1.25f);
            foreach (var v in mesh.vertices) Assert.LessOrEqual(new Vector2(v.x, v.z).magnitude, 15.001f);
            Assert.AreEqual(15f, mesh.bounds.extents.x, 0.1f);
        }

        [Test] public void AllTrianglesFaceUp()
        {
            foreach (var mesh in new[] { WaterMeshes.Rect(6f, 6f, 1f), WaterMeshes.Disc(5f, 1f) })
            {
                var v = mesh.vertices; var t = mesh.triangles;
                for (int i = 0; i < t.Length; i += 3)
                    Assert.Greater(Vector3.Cross(v[t[i + 1]] - v[t[i]], v[t[i + 2]] - v[t[i]]).y, 0f);
            }
        }

        [Test] public void RingsFaceUpAndSpanTheEdge()
        {
            var disc = WaterMeshes.DiscRing(10f, 1f, 0.5f, 32);
            Assert.AreEqual(11f, disc.bounds.extents.x, 0.05f);
            var rect = WaterMeshes.RectRing(20f, 8f, 1f, 0.5f, 1.25f);
            Assert.AreEqual(11f, rect.bounds.extents.x, 0.05f);
            Assert.AreEqual(5f, rect.bounds.extents.z, 0.05f);
            foreach (var mesh in new[] { disc, rect })
            {
                var v = mesh.vertices; var t = mesh.triangles;
                for (int i = 0; i < t.Length; i += 3)
                    Assert.Greater(Vector3.Cross(v[t[i + 1]] - v[t[i]], v[t[i + 2]] - v[t[i]]).y, 0f);
                foreach (var uv in mesh.uv) Assert.That(uv.y, Is.InRange(0f, 1f));
            }
        }

        [Test] public void UvsStayInZeroToOne()
        {
            foreach (var uv in WaterMeshes.Disc(8f, 1f).uv) { Assert.That(uv.x, Is.InRange(0f, 1f)); Assert.That(uv.y, Is.InRange(0f, 1f)); }
            foreach (var uv in WaterMeshes.Rect(8f, 3f, 1f).uv) { Assert.That(uv.x, Is.InRange(0f, 1f)); Assert.That(uv.y, Is.InRange(0f, 1f)); }
        }
    }
}
