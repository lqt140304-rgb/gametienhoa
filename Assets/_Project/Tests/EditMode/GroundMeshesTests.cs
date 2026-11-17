using Evolve.World;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

namespace Evolve.Tests.EditMode
{
    public class GroundMeshesTests
    {
        [Test] public void RockIsFlatShadedAndAboutAMetreWide()
        {
            var rock = GroundMeshes.Rock(1, out var moss);
            Assert.AreEqual(80 * 3, rock.vertexCount);          // every triangle has its own vertices
            Assert.That(rock.bounds.size.x, Is.InRange(0.6f, 1.3f));
            Assert.Greater(moss.vertexCount, 0);                // some faces look up
            Assert.Less(moss.vertexCount, rock.vertexCount);    // but not all of them
            Assert.AreEqual(0, moss.vertexCount % 3);
        }

        [Test] public void TuftsHaveFiveBladesEach()
        {
            var positions = new List<Vector3> { Vector3.zero, new Vector3(3f, 0f, 1f), new Vector3(-2f, 0f, 4f) };
            var mesh = GroundMeshes.Tufts(positions, 1, 0.45f);
            Assert.AreEqual(3 * 5 * 3, mesh.vertexCount);
            Assert.Greater(mesh.bounds.max.y, 0.2f);
        }

        [Test] public void WindingLineKeepsItsEndsAndWinds()
        {
            var a = new Vector2(0f, 0f); var b = new Vector2(40f, 0f);
            var line = GroundMeshes.WindingLine(a, b, 3, 3.5f, 1.5f);
            Assert.AreEqual(a, line[0]);
            Assert.AreEqual(b.x, line[line.Length - 1].x, 1e-3f);
            Assert.AreEqual(0f, line[line.Length - 1].y, 1e-3f);
            float maxSide = 0f;
            foreach (var p in line) maxSide = Mathf.Max(maxSide, Mathf.Abs(p.y));
            Assert.Greater(maxSide, 0.5f);                      // it really winds
            Assert.LessOrEqual(maxSide, 3.5f + 1e-3f);
        }

        [Test] public void RibbonFacesUpAndFollowsTheGround()
        {
            var line = GroundMeshes.WindingLine(new Vector2(0f, 0f), new Vector2(20f, 5f), 2, 2f, 1.5f);
            var mesh = GroundMeshes.Ribbon(line, 0.9f, 4, (x, z) => 0.5f, 0.015f);
            var v = mesh.vertices; var t = mesh.triangles;
            for (int i = 0; i < t.Length; i += 3)
                Assert.Greater(Vector3.Cross(v[t[i + 1]] - v[t[i]], v[t[i + 2]] - v[t[i]]).y, 0f);
            foreach (var p in v) Assert.AreEqual(0.515f, p.y, 1e-4f);
            foreach (var uv in mesh.uv) Assert.That(uv.y, Is.InRange(0f, 1f));
        }
    }
}
