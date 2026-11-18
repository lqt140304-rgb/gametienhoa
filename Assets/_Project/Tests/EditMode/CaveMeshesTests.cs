using System.Collections.Generic;
using Evolve.World;
using NUnit.Framework;
using UnityEngine;

namespace Evolve.Tests.EditMode
{
    public class CaveMeshesTests
    {
        [Test] public void ShellIsADomeOfTheGivenSize()
        {
            var mesh = CaveMeshes.Shell(19f, 15f, 12f, 48, 12, 1, 0.08f);
            var b = mesh.bounds;
            Assert.AreEqual(0f, b.min.y, 1e-3f);
            Assert.AreEqual(12f, b.max.y, 1e-3f);
            Assert.AreEqual(19f, b.extents.x, 19f * 0.1f);
            Assert.AreEqual(15f, b.extents.z, 15f * 0.1f);
        }

        [Test] public void CuttingADoorRemovesOnlyThatPart()
        {
            var mesh = CaveMeshes.Shell(19f, 15f, 12f, 72, 18, 1, 0.08f);
            int before = mesh.triangles.Length;
            WaterMeshes.RemoveTriangles(mesh, c => c.x > 0f && Mathf.Abs(c.z) < 2.6f && c.y < 3.6f);
            int after = mesh.triangles.Length;
            Assert.Less(after, before);
            Assert.Greater(after, before * 0.9f);
        }

        [Test] public void ShinglesLieJustOutsideTheDomeAndCanSkipADoor()
        {
            var all = CaveMeshes.Shingles(2f, 1.85f, 3f, 0.34f, 0.3f, 0.08f, 1, null);
            Assert.Greater(all.vertexCount, 400);
            foreach (var v in all.vertices)
            {
                Assert.IsFalse(float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z));
                float q = (v.x / 2f) * (v.x / 2f) + (v.y / 3f) * (v.y / 3f) + (v.z / 1.85f) * (v.z / 1.85f);
                Assert.Greater(q, 0.97f, "never inside the dome");
                Assert.Less(q, 1.2f, "never far outside it");
            }
            var noDoor = CaveMeshes.Shingles(2f, 1.85f, 3f, 0.34f, 0.3f, 0.08f, 1, c => c.x > 0f && Mathf.Abs(c.z) < 0.85f && c.y < 1.9f);
            Assert.Less(noDoor.vertexCount, all.vertexCount);
            foreach (var v in noDoor.vertices) Assert.IsFalse(v.x > 1.2f && Mathf.Abs(v.z) < 0.4f && v.y < 1.2f, "nothing across the doorway");
        }

        [Test] public void DiscIsASolidCylinder()
        {
            var mesh = CaveMeshes.Disc(4f, 0.25f, 32);
            Assert.AreEqual(8f, mesh.bounds.size.x, 0.01f);
            Assert.AreEqual(0.25f, mesh.bounds.size.y, 1e-4f);
            Assert.AreEqual(0f, mesh.bounds.min.y, 1e-4f);
        }

        [Test] public void TunnelArchesOverItsPath()
        {
            var path = new List<Vector3> { new Vector3(0f, 0f, 0f), new Vector3(0f, -1f, 10f), new Vector3(3f, -3f, 20f) };
            var mesh = CaveMeshes.ArchTunnel(path, 4.5f, 3.2f, 12);
            Assert.AreEqual(3 * 13, mesh.vertexCount);
            foreach (var v in mesh.vertices) Assert.IsFalse(float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z));
            Assert.GreaterOrEqual(mesh.bounds.max.y, 3.2f - 1e-3f);
        }
    }
}
