using Evolve.World;
using NUnit.Framework;
using UnityEngine;

namespace Evolve.Tests.EditMode
{
    public class FloraMeshesTests
    {
        [Test] public void MushroomIsAboutTheRequestedHeight()
        {
            FloraMeshes.Mushroom(1, 4f, out var stalk, out var cap, out _);
            Assert.AreEqual(0f, stalk.bounds.min.y, 0.01f);
            Assert.That(cap.bounds.max.y, Is.InRange(4f * 0.9f, 4f * 1.4f));
            Assert.Greater(cap.bounds.extents.x, stalk.bounds.extents.x * 2f);   // the cap is much wider than the stalk
        }

        [Test] public void MushroomNormalsPointAwayFromTheAxis()
        {
            FloraMeshes.Mushroom(2, 3f, out _, out var cap, out _);
            var v = cap.vertices; var n = cap.normals;
            int outward = 0, total = 0;
            for (int i = 0; i < v.Length; i++)
            {
                if (n[i].sqrMagnitude < 0.5f) continue;   // pinched points on the axis
                total++;
                if (Vector3.Dot(n[i], v[i] - new Vector3(0f, 3f * 0.76f + 0.1f, 0f)) > 0f) outward++;
            }
            Assert.Greater(outward, total * 0.75f);
        }

        [Test] public void SameSeedSameMushroom()
        {
            FloraMeshes.Mushroom(5, 2f, out _, out var a, out _);
            FloraMeshes.Mushroom(5, 2f, out _, out var b, out _);
            Assert.AreEqual(a.vertices, b.vertices);
        }

        [Test] public void CapTopHeightFallsFromCentreToRim()
        {
            float centre = FloraMeshes.CapTopHeight(4f, 2f, 0.9f, 0f), rim = FloraMeshes.CapTopHeight(4f, 2f, 0.9f, 2f);
            Assert.Greater(centre, rim);
            Assert.AreEqual(4f * 0.76f + 0.9f, centre, 1e-4f);
        }

        [Test] public void VineHangsDownFromItsTop()
        {
            var top = new Vector3(3f, 20f, -4f);
            var path = FloraMeshes.VinePath(top, 12f, 7, 1.1f, 14);
            Assert.AreEqual(14, path.Length);
            Assert.AreEqual(top, path[0]);
            Assert.AreEqual(8f, path[path.Length - 1].y, 1e-3f);
            for (int i = 1; i < path.Length; i++) Assert.Less(path[i].y, path[i - 1].y);
        }

        [Test] public void LeafStandsUpFromTheOrigin()
        {
            var b = FloraMeshes.Leaf(0.45f, 0.22f).bounds;
            Assert.AreEqual(0f, b.min.y, 1e-4f);
            Assert.AreEqual(0.45f, b.max.y, 1e-4f);
        }
    }
}
