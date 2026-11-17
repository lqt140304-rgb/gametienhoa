using Evolve.Visuals;
using NUnit.Framework;
using UnityEngine;

namespace Evolve.Tests.EditMode
{
    public class SlimeMeshBuilderTests
    {
        [Test] public void BodyHas1280Triangles()
        {
            Assert.AreEqual(1280, SlimeMeshBuilder.Build().triangles.Length / 3);
        }

        [Test] public void BodyIsSmoothAndHasUvs()
        {
            var mesh = SlimeMeshBuilder.Build();
            Assert.AreEqual(642, mesh.vertexCount); // shared vertices
            Assert.AreEqual(mesh.vertexCount, mesh.uv.Length);
        }

        [Test] public void BodySitsOnTheOldSphereFootprint()
        {
            var b = SlimeMeshBuilder.Build().bounds;
            Assert.AreEqual(SlimeMeshBuilder.Bottom, b.min.y, 0.001f);
            Assert.That(b.max.y, Is.InRange(0.2f, 0.35f));
            Assert.That(b.extents.x, Is.InRange(0.4f, 0.8f));
        }

        [Test] public void SameSeedSameMesh()
        {
            Assert.AreEqual(SlimeMeshBuilder.Build(3).vertices, SlimeMeshBuilder.Build(3).vertices);
        }

        [Test] public void TubeHasOneRingPerPathPoint()
        {
            var path = new[] { Vector3.zero, Vector3.up * 0.2f, Vector3.up * 0.4f };
            var mesh = SlimeMeshBuilder.Tube(path, new[] { 0.1f, 0.08f, 0f }, 8);
            Assert.AreEqual(3 * 8, mesh.vertexCount);
            Assert.AreEqual(0.4f, mesh.bounds.max.y, 0.001f);
        }

        [Test] public void FlareIsWideAtTheBase()
        {
            var mesh = SlimeMeshBuilder.Flare(0.2f, 0.05f, 0.1f);
            Assert.AreEqual(0.1f, mesh.bounds.max.y, 0.001f);
            Assert.Greater(mesh.bounds.extents.x, 0.15f);
        }
    }
}
