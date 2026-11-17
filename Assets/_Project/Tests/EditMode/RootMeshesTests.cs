using Evolve.World;
using NUnit.Framework;
using UnityEngine;

namespace Evolve.Tests.EditMode
{
    public class RootMeshesTests
    {
        [Test] public void ArchRisesInTheMiddleAndIsBuriedAtTheEnds()
        {
            var a = new Vector3(0f, 0f, 0f); var b = new Vector3(8f, 0f, 0f);
            var path = RootMeshes.ArchPath(a, b, 3f, 1, 0.9f, 16, 0.35f);
            Assert.AreEqual(16, path.Length);
            Assert.AreEqual(-0.35f, path[0].y, 1e-3f);
            Assert.AreEqual(-0.35f, path[path.Length - 1].y, 1e-3f);
            float top = float.MinValue;
            foreach (var p in path) top = Mathf.Max(top, p.y);
            Assert.That(top, Is.InRange(2.5f, 3.2f));
        }

        [Test] public void LatticeCoversItsAreaAtItsHeight()
        {
            var area = new Rect(0f, 0f, 64f, 64f);
            var mesh = RootMeshes.Lattice(area, 25f, 3, new[] { new Vector2(32f, 32f) });
            Assert.Greater(mesh.vertexCount, 100);
            var b = mesh.bounds;
            Assert.LessOrEqual(b.min.x, 16f); Assert.GreaterOrEqual(b.max.x, 48f);
            Assert.LessOrEqual(b.min.z, 16f); Assert.GreaterOrEqual(b.max.z, 48f);
            Assert.GreaterOrEqual(b.min.y, 25f - 7f); Assert.LessOrEqual(b.max.y, 25f + 3.5f);
            foreach (var v in mesh.vertices) Assert.IsFalse(float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z));
            Assert.Less(RootMeshes.Lattice(area, 25f, 3, null, 7f, false).vertexCount, mesh.vertexCount, "the far version is lighter");
        }

        [Test] public void ArchAndTubeNeverContainNaN()
        {
            for (int seed = 1; seed <= 30; seed++)
            {
                var path = RootMeshes.ArchPath(new Vector3(0f, 0f, 0f), new Vector3(7f, 0f, 3f), 2.5f, seed, 0.9f, 16);
                foreach (var p in path) Assert.IsFalse(float.IsNaN(p.x) || float.IsNaN(p.y) || float.IsNaN(p.z), "path point is NaN");
                var mesh = RootMeshes.RootTube(path, 0.5f, 0.4f, seed, 0.12f);
                foreach (var v in mesh.vertices) Assert.IsFalse(float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z), "mesh vertex is NaN");
            }
        }

        [Test] public void RootTaperKeepsTheTipThin()
        {
            var path = new[] { Vector3.zero, Vector3.up, Vector3.up * 2f, Vector3.up * 3f, Vector3.up * 4f };
            var mesh = RootMeshes.RootTube(path, 0.8f, 0.05f, 3, 0.15f, 8);
            Assert.AreEqual(5 * 8, mesh.vertexCount);
            // the first ring is far wider than the last
            var v = mesh.vertices;
            float first = 0f, last = 0f;
            for (int j = 0; j < 8; j++) { first = Mathf.Max(first, new Vector2(v[j].x, v[j].z).magnitude); last = Mathf.Max(last, new Vector2(v[32 + j].x, v[32 + j].z).magnitude); }
            Assert.Greater(first, last * 5f);
        }

        [Test] public void BranchStartsAtTheGivenPointAndDroops()
        {
            var from = new Vector3(2f, 3f, 1f);
            var path = RootMeshes.BranchPath(from, Vector3.right, 4f, 2f, 5, 9);
            Assert.AreEqual(from, path[0]);
            Assert.AreEqual(1f, path[path.Length - 1].y, 1e-3f);          // 3 - 2 droop
            Assert.AreEqual(6f, path[path.Length - 1].x, 1e-3f);          // 2 + 4 length
        }

        [Test] public void LightShaftWidensDownwardFromTheOrigin()
        {
            var mesh = RootMeshes.LightShaft(1f, 4f, 20f, 18, 8, 2);
            Assert.AreEqual(0f, mesh.bounds.max.y, 1e-3f);
            Assert.AreEqual(-20f, mesh.bounds.min.y, 1e-3f);
            Assert.Greater(mesh.bounds.extents.x, 3.4f);                  // about the bottom radius
            foreach (var uv in mesh.uv) { Assert.That(uv.x, Is.InRange(0f, 1f)); Assert.That(uv.y, Is.InRange(0f, 1f)); }
        }
    }
}
