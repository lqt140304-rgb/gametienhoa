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

        [Test] public void OutlineIsClosedAndWithinReach()
        {
            var outline = WaterOutline.Make(24f, 8f, false, 3, 0.8f, 80);
            Assert.AreEqual(80, outline.Length);
            foreach (var p in outline)
            {
                Assert.LessOrEqual(Mathf.Abs(p.x), 12f + 0.9f);
                Assert.LessOrEqual(Mathf.Abs(p.y), 4f + 0.9f);
            }
        }

        [Test] public void OutlineIsNotAPerfectShapeAndDependsOnSeed()
        {
            var a = WaterOutline.Make(30f, 30f, true, 1, 0.8f, 90);
            var b = WaterOutline.Make(30f, 30f, true, 2, 0.8f, 90);
            float minR = float.MaxValue, maxR = 0f;
            foreach (var p in a) { minR = Mathf.Min(minR, p.magnitude); maxR = Mathf.Max(maxR, p.magnitude); }
            Assert.Greater(maxR - minR, 0.3f);   // the shore wanders (a perfect circle would be 0)
            Assert.AreNotEqual(a[10], b[10]);
        }

        [Test] public void OutlineWithoutWobbleIsTheBaseShape()
        {
            foreach (var p in WaterOutline.Make(20f, 20f, true, 5, 0f, 60)) Assert.AreEqual(10f, p.magnitude, 0.05f);
        }

        [Test] public void OrganicSurfaceFacesUpAndStaysInsideTheOutline()
        {
            var outline = WaterOutline.Make(20f, 6f, false, 4, 0.8f, 60);
            var mesh = WaterMeshes.Organic(outline, 1.25f);
            var v = mesh.vertices; var t = mesh.triangles;
            for (int i = 0; i < t.Length; i += 3)
                Assert.Greater(Vector3.Cross(v[t[i + 1]] - v[t[i]], v[t[i + 2]] - v[t[i]]).y, 0f);
            foreach (var uv in mesh.uv) { Assert.That(uv.x, Is.InRange(-0.001f, 1.001f)); Assert.That(uv.y, Is.InRange(-0.001f, 1.001f)); }
        }

        [Test] public void BandFacesUpWithUvAcrossZeroToOne()
        {
            var outline = WaterOutline.Make(12f, 12f, true, 6, 0.8f, 48);
            var mesh = WaterMeshes.Band(outline, 1f, 0.4f, 7);
            var v = mesh.vertices; var t = mesh.triangles;
            for (int i = 0; i < t.Length; i += 3)
                Assert.Greater(Vector3.Cross(v[t[i + 1]] - v[t[i]], v[t[i + 2]] - v[t[i]]).y, 0f);
            foreach (var uv in mesh.uv) Assert.That(uv.y, Is.InRange(0f, 1f));
        }

        [Test] public void StreamEdgesAreAWidthApart()
        {
            var path = GroundMeshes.WindingLine(new Vector2(0f, 0f), new Vector2(60f, 0f), 3, 4f, 2f, 50f);
            WaterStream.Edges(path, 3f, 1, 0.25f, out var left, out var right);
            Assert.AreEqual(path.Length, left.Length);
            for (int i = 0; i < path.Length; i++)
            {
                float width = Vector2.Distance(left[i], right[i]);
                Assert.That(width, Is.InRange(3f * 2f * 0.7f, 3f * 2f * 1.3f));
            }
            Assert.AreEqual(path.Length * 2, WaterStream.Polygon(left, right).Length);
        }

        [Test] public void WindingStreamActuallyWindsAndStaysOneRiver()
        {
            var path = GroundMeshes.WindingLine(new Vector2(0f, 0f), new Vector2(60f, 0f), 3, 4f, 2f, 50f);
            float maxSide = 0f;
            foreach (var p in path) maxSide = Mathf.Max(maxSide, Mathf.Abs(p.y));
            Assert.Greater(maxSide, 1f);                       // it bends
            // The bends are gentle enough that the two banks never cross: no flipped strip triangle.
            WaterStream.Edges(path, 3f, 1, 0.25f, out var left, out var right);
            var mesh = WaterMeshes.Strip(left, right, 1.25f);
            var v = mesh.vertices; var t = mesh.triangles;
            for (int i = 0; i < t.Length; i += 3)
                Assert.Greater(Vector3.Cross(v[t[i + 1]] - v[t[i]], v[t[i + 2]] - v[t[i]]).y, 0f);
        }

        [Test] public void StreamBandsFaceUpAndKeepUvInRange()
        {
            var path = GroundMeshes.WindingLine(new Vector2(0f, 0f), new Vector2(40f, 5f), 4, 3f, 2f, 50f);
            WaterStream.Edges(path, 3f, 2, 0.2f, out var left, out var right);
            var band = WaterMeshes.Combine(WaterMeshes.EdgeBand(left, 1f, 0.4f, 1f, 1), WaterMeshes.EdgeBand(right, 1f, 0.4f, -1f, 2));
            var v = band.vertices; var t = band.triangles;
            Assert.Greater(t.Length, 0);
            for (int i = 0; i < t.Length; i += 3)
                Assert.Greater(Vector3.Cross(v[t[i + 1]] - v[t[i]], v[t[i + 2]] - v[t[i]]).y, 0f);
            foreach (var uv in band.uv) Assert.That(uv.y, Is.InRange(0f, 1f));
        }

        [Test] public void UvsStayInZeroToOne()
        {
            foreach (var uv in WaterMeshes.Disc(8f, 1f).uv) { Assert.That(uv.x, Is.InRange(0f, 1f)); Assert.That(uv.y, Is.InRange(0f, 1f)); }
            foreach (var uv in WaterMeshes.Rect(8f, 3f, 1f).uv) { Assert.That(uv.x, Is.InRange(0f, 1f)); Assert.That(uv.y, Is.InRange(0f, 1f)); }
        }
    }
}
