using System;
using System.Linq;
using Evolve.Combat;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class RegaliaMeshesTests
    {
        static void AssertIndicesValid(MeshData m) { foreach (int i in m.Triangles) Assert.That(i >= 0 && i < m.VertexCount, "index " + i); }

        [Test] public void Torus_CountsAndRadius()
        {
            var m = RegaliaMeshes.Torus(0.5f, 0.1f, 16, 8);
            Assert.AreEqual(16 * 8, m.VertexCount); Assert.AreEqual(16 * 8 * 6, m.Triangles.Length); AssertIndicesValid(m);
            for (int i = 0; i < m.VertexCount; i++)
            {
                float r = (float)Math.Sqrt(m.Vertices[i * 3] * m.Vertices[i * 3] + m.Vertices[i * 3 + 2] * m.Vertices[i * 3 + 2]);
                Assert.That(r >= 0.4f - 1e-4f && r <= 0.6f + 1e-4f, "radius " + r);
            }
        }

        [Test] public void Crown_Spikes()
        {
            var m = RegaliaMeshes.Crown(0.3f, 0.2f, 5);
            Assert.AreEqual(5 * 5, m.VertexCount); Assert.AreEqual(5 * 6 * 3, m.Triangles.Length); AssertIndicesValid(m);
            float top = float.MinValue; for (int i = 0; i < m.VertexCount; i++) top = Math.Max(top, m.Vertices[i * 3 + 1]);
            Assert.AreEqual(0.2f, top, 1e-4f);
            bool onRim = false;
            for (int i = 0; i < m.VertexCount; i++) if (Math.Abs(m.Vertices[i * 3 + 1]) < 1e-4f && Math.Abs(Math.Sqrt(m.Vertices[i * 3] * m.Vertices[i * 3] + m.Vertices[i * 3 + 2] * m.Vertices[i * 3 + 2]) - 0.3) < 0.1) onRim = true;
            Assert.IsTrue(onRim);
        }

        [Test] public void CapeGrid_Layout()
        {
            var m = RegaliaMeshes.CapeGrid(0.8f, 1.2f, 6, 8);
            Assert.AreEqual(7 * 9, m.VertexCount); Assert.AreEqual(6 * 8 * 6, m.Triangles.Length); AssertIndicesValid(m);
            for (int i = 0; i < m.VertexCount; i++)
            {
                float x = m.Vertices[i * 3], y = m.Vertices[i * 3 + 1], z = m.Vertices[i * 3 + 2];
                Assert.That(Math.Abs(x) <= 0.4f + 1e-4f); Assert.That(z <= 1e-4f && z >= -1.2f - 1e-4f); Assert.AreEqual(0f, y, 1e-6f);
            }
        }

        [Test] public void CapeWave_AnchoredAndBounded()
        {
            for (float t = 0f; t < 20f; t += 0.37f)
                for (float u = 0f; u <= 1f; u += 0.1f)
                {
                    Assert.AreEqual(0f, RegaliaMeshes.CapeWave(u, 0f, t), 1e-6f);
                    for (float v = 0f; v <= 1f; v += 0.1f) Assert.LessOrEqual(Math.Abs(RegaliaMeshes.CapeWave(u, v, t)), 1f);
                }
        }

        [Test] public void CapeWave_MovesWithTime() => Assert.AreNotEqual(RegaliaMeshes.CapeWave(0.5f, 1f, 0.3f), RegaliaMeshes.CapeWave(0.5f, 1f, 1.1f));

        static float Amplitude(float v) { float max = 0f; for (float t = 0f; t < 10f; t += 0.05f) for (float u = 0f; u <= 1f; u += 0.25f) max = Math.Max(max, Math.Abs(RegaliaMeshes.CapeWave(u, v, t))); return max; }

        [Test] public void CapeWave_GrowsTowardsTheHem() => Assert.Greater(Amplitude(1f), Amplitude(0.3f));
    }
}
