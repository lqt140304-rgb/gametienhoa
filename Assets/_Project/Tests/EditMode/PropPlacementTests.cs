using System.Collections.Generic;
using Evolve.World;
using NUnit.Framework;
using UnityEngine;

namespace Evolve.Tests.EditMode
{
    public class PropPlacementTests
    {
        static readonly Rect Area = new Rect(-50f, -50f, 100f, 100f);
        static readonly Vector2 Sizes = new Vector2(0.5f, 2f);

        [Test]
        public void Rocks_SameSeedSameResult()
        {
            var a = PropPlacement.Rocks(7, Area, 30, null, 6, Sizes);
            var b = PropPlacement.Rocks(7, Area, 30, null, 6, Sizes);
            Assert.AreEqual(a.Count, b.Count);
            for (int i = 0; i < a.Count; i++) Assert.AreEqual(a[i].Position, b[i].Position);
        }

        [Test]
        public void Rocks_DifferentSeedDiffers()
        {
            var a = PropPlacement.Rocks(7, Area, 30, null, 6, Sizes);
            var b = PropPlacement.Rocks(8, Area, 30, null, 6, Sizes);
            Assert.AreNotEqual(a[0].Position, b[0].Position);
        }

        [Test]
        public void Rocks_NeverBlocked()
        {
            bool Blocked(float x, float z) => x * x + z * z < 20f * 20f;
            foreach (var s in PropPlacement.Rocks(3, Area, 60, Blocked, 6, Sizes))
                Assert.IsFalse(Blocked(s.Position.x, s.Position.y));
        }

        [Test]
        public void Rocks_SpacedApart()
        {
            var list = PropPlacement.Rocks(5, Area, 80, null, 6, Sizes);
            for (int i = 0; i < list.Count; i++)
                for (int j = i + 1; j < list.Count; j++)
                    Assert.GreaterOrEqual(Vector2.Distance(list[i].Position, list[j].Position), 0.5f * (list[i].Size + list[j].Size) + PropPlacement.Gap - 1e-3f);
        }

        [Test]
        public void Rocks_CountNotExceededAndSizeWithinRange()
        {
            var list = PropPlacement.Rocks(9, Area, 25, null, 6, Sizes);
            Assert.LessOrEqual(list.Count, 25);
            foreach (var s in list)
            {
                Assert.GreaterOrEqual(s.Size, Sizes.x - 1e-4f);
                Assert.LessOrEqual(s.Size, Sizes.y + 1e-4f);
                Assert.Less(s.Variant, 6);
            }
        }

        [Test]
        public void Rocks_ZeroVariantsOrCountGivesEmpty()
        {
            Assert.AreEqual(0, PropPlacement.Rocks(1, Area, 10, null, 0, Sizes).Count);
            Assert.AreEqual(0, PropPlacement.Rocks(1, Area, 0, null, 6, Sizes).Count);
        }

        [Test]
        public void Allowed_RejectsDeepWater()
        {
            Assert.IsFalse(PropPlacement.Allowed(0, 0, (x, z) => 0.6f, null, null, 0f));
            Assert.IsTrue(PropPlacement.Allowed(0, 0, (x, z) => 0.4f, null, null, 0f));
        }

        [Test]
        public void Allowed_RejectsNearPath()
        {
            Assert.IsFalse(PropPlacement.Allowed(0, 0, null, (x, z) => 2.9f, null, 0f));
            Assert.IsTrue(PropPlacement.Allowed(0, 0, null, (x, z) => 3.1f, null, 0f));
        }

        [Test]
        public void Allowed_RejectsKeepClear()
        {
            var keep = new List<Vector2> { new Vector2(0f, 0f) };
            Assert.IsFalse(PropPlacement.Allowed(5.9f, 0f, null, null, keep, 6f));
            Assert.IsTrue(PropPlacement.Allowed(6.1f, 0f, null, null, keep, 6f));
        }
    }
}
