using Evolve.Visuals;
using NUnit.Framework;
using UnityEngine;

namespace Evolve.Tests.EditMode
{
    public class SlimeMeshBuilderTests
    {
        [Test] public void BodyHas80Triangles()
        {
            var mesh = SlimeMeshBuilder.Build();
            Assert.AreEqual(80, mesh.triangles.Length / 3);
        }

        [Test] public void BodyIsFlatShaded()
        {
            var mesh = SlimeMeshBuilder.Build();
            Assert.AreEqual(mesh.triangles.Length, mesh.vertexCount); // no shared vertices
        }

        [Test] public void BodySitsOnTheOldSphereFootprint()
        {
            var b = SlimeMeshBuilder.Build().bounds;
            Assert.AreEqual(SlimeMeshBuilder.Bottom, b.min.y, 0.001f);
            Assert.That(b.max.y, Is.InRange(0.2f, 0.35f));
            Assert.That(b.extents.x, Is.InRange(0.4f, 0.65f));
        }

        [Test] public void SameSeedSameMesh()
        {
            Assert.AreEqual(SlimeMeshBuilder.Build(3).vertices, SlimeMeshBuilder.Build(3).vertices);
        }

        [Test] public void ConeTipIsAtHeight()
        {
            var b = SlimeMeshBuilder.Cone(6, 0.1f, 0.5f).bounds;
            Assert.AreEqual(0.5f, b.max.y, 0.001f);
            Assert.AreEqual(0f, b.min.y, 0.001f);
        }
    }
}
