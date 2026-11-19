using System.Linq;
using Evolve.World;
using NUnit.Framework;
using UnityEngine;

namespace Evolve.Tests.EditMode
{
    public class CavernLayoutTests
    {
        [Test]
        public void LakeIsDeepInTheMiddleAndDryOutside()
        {
            Assert.GreaterOrEqual(CavernLayout.DepthAt(0f, 0f), 7.9f);
            Assert.AreEqual(0f, CavernLayout.DepthAt(0f, CavernLayout.LakeMeanRadius * 1.5f), 1e-5f);
            Assert.AreEqual(0f, CavernLayout.DepthAt(60f, 60f), 1e-5f);
            for (int i = 0; i < 24; i++)
            {
                float a = i * Mathf.PI / 12f;
                float shore = CavernLayout.LakeRadius(a);
                Assert.Greater(shore, 10f); Assert.Less(shore, 22f);           // wandering, never tiny or huge
                Assert.Less(CavernLayout.DepthAt(Mathf.Cos(a) * (shore - 0.2f), Mathf.Sin(a) * (shore - 0.2f)), 0.3f, "the shore is shallow");
            }
        }

        [Test]
        public void ShoreIsGentleSoTheSlimeCanWade()
        {
            // from the shore inward the bed drops no faster than 0.8 m per metre while the water is under 0.6 m deep (wading depth)
            for (int i = 0; i < 24; i++)
            {
                float a = i * Mathf.PI / 12f, shore = CavernLayout.LakeRadius(a);
                float prev = CavernLayout.GroundHeight(Mathf.Cos(a) * shore, Mathf.Sin(a) * shore);
                for (float r = shore - 0.25f; r > shore - 8f; r -= 0.25f)
                {
                    float x = Mathf.Cos(a) * r, z = Mathf.Sin(a) * r;
                    float h = CavernLayout.GroundHeight(x, z);
                    if (CavernLayout.DepthAt(x, z) < 0.6f) Assert.LessOrEqual(Mathf.Abs(h - prev) / 0.25f, 0.8f, $"angle {i * 15}, r {r}");
                    prev = h;
                }
            }
        }

        [Test]
        public void GroundIsFlatNearTheLakeAndWallsAreTall()
        {
            Assert.AreEqual(-CavernLayout.MaxDepth, CavernLayout.GroundHeight(0f, 0f), 0.2f);
            for (int i = 0; i < 12; i++)
            {
                float a = i * Mathf.PI / 6f;
                float r = CavernLayout.LakeRadius(a) + 6f;
                Assert.Less(CavernLayout.GroundHeight(Mathf.Cos(a) * r, Mathf.Sin(a) * r), 3f, "dry shore, not a wall");
                Assert.GreaterOrEqual(CavernLayout.GroundHeight(Mathf.Cos(a) * 50f, Mathf.Sin(a) * 50f), CavernLayout.WallHeight - 0.5f, "the wall is full height by 50 m");
            }
        }

        [Test]
        public void TheFloorRisesInPatchesButStaysWalkable()
        {
            float min = float.MaxValue, max = float.MinValue;
            for (float x = -30f; x <= 30f; x += 1f)
                for (float z = -30f; z <= 30f; z += 1f)
                {
                    if (CavernLayout.ShoreOffset(x, z) < 6f) continue;      // the dry floor beyond the shore
                    float h = CavernLayout.GroundHeight(x, z);
                    min = Mathf.Min(min, h); max = Mathf.Max(max, h);
                    float slope = Mathf.Abs(CavernLayout.GroundHeight(x + 1f, z) - h);
                    Assert.LessOrEqual(slope, 1.1f, $"too steep to walk at {x},{z}");
                }
            Assert.GreaterOrEqual(max - min, 1.2f, "real mounds, not a plain");
            Assert.Greater(min, -0.01f, "no pits below the water level on the dry floor");
        }

        [Test]
        public void SpawnIsOnDryGroundBesideTheWater()
        {
            var p = CavernLayout.SpawnPoint();
            Assert.AreEqual(0f, CavernLayout.DepthAt(p.x, p.z), 1e-5f);
            float d = Vector2.Distance(new Vector2(p.x, p.z), CavernLayout.LakeCenter) - CavernLayout.LakeRadius(Mathf.Atan2(p.z, p.x));
            Assert.AreEqual(3f, d, 0.01f);
            Assert.Greater(p.y, 0f);
            Assert.Less(p.y, 3f);
        }

        [Test]
        public void SunBeamThroughTheHoleLandsOnTheLake()
        {
            var hole = CavernLayout.HoleCenter();
            var d = CavernLayout.SunDirection;
            Assert.Less(d.y, 0f);
            float y = CavernLayout.CeilingY(hole.x, hole.y);
            float t = y / -d.y;
            var landing = new Vector2(hole.x + d.x * t, hole.y + d.z * t);
            Assert.AreEqual(0f, Vector2.Distance(landing, CavernLayout.LakeCenter), 0.5f);
            Assert.Greater(CavernLayout.DepthAt(landing.x, landing.y), 5f);
            // the hole is inside the cavern, over the floor and not beyond the wall
            Assert.Less(hole.magnitude, CavernLayout.FloorRadius);
            Assert.Greater(hole.magnitude, CavernLayout.HoleRadius);
            Assert.IsTrue(CavernLayout.InHole(hole.x, hole.y));
            Assert.IsFalse(CavernLayout.InHole(hole.x + CavernLayout.HoleRadius + 2f, hole.y));
        }

        [Test]
        public void RoofSitsOnTheWallAtTheEdge()
        {
            Assert.AreEqual(CavernLayout.CeilingHeight, CavernLayout.CeilingY(0f, 0f), 1e-4f);
            Assert.AreEqual(CavernLayout.WallHeight, CavernLayout.CeilingY(60f, 0f), 1e-4f);
            Assert.AreEqual(CavernLayout.WallHeight, CavernLayout.CeilingY(0f, -64f), 1e-4f);
        }

        [Test]
        public void PlantsRingTheLakeAndNeverGrowInWaterOrOnTheWall()
        {
            var pts = CavernLayout.Scatter(7, 8000, CavernLayout.PlantDensity);
            Assert.Greater(pts.Count, 600, "a carpet, not a few tufts");
            foreach (var p in pts)
            {
                Assert.AreEqual(0f, CavernLayout.DepthAt(p.x, p.z), 1e-5f);
                Assert.Less(p.y, 3.01f);
            }
            // more of them near the lake than far from it
            int near = pts.Count(p => Vector2.Distance(new Vector2(p.x, p.z), CavernLayout.LakeCenter) < 30f);
            Assert.Greater(near, pts.Count / 2);
        }

        [Test]
        public void OreIsRareAndOnDryGround()
        {
            var ore = CavernLayout.ScatterSpaced(11, 20000, CavernLayout.OreDensity, 10f, 20);
            Assert.GreaterOrEqual(ore.Count, 8);
            Assert.LessOrEqual(ore.Count, 20);
            foreach (var p in ore) { Assert.AreEqual(0f, CavernLayout.DepthAt(p.x, p.z), 1e-5f); Assert.Less(p.y, 8.01f); }
            for (int i = 0; i < ore.Count; i++)
                for (int j = i + 1; j < ore.Count; j++)
                    Assert.GreaterOrEqual(Vector2.Distance(new Vector2(ore[i].x, ore[i].z), new Vector2(ore[j].x, ore[j].z)), 10f - 1e-3f);
        }

        [Test]
        public void ScatterIsDeterministicBySeed()
        {
            var a = CavernLayout.Scatter(3, 500, CavernLayout.RockDensity);
            var b = CavernLayout.Scatter(3, 500, CavernLayout.RockDensity);
            CollectionAssert.AreEqual(a, b);
            Assert.AreEqual(0, CavernLayout.Scatter(3, 0, CavernLayout.RockDensity).Count);
        }

        [Test]
        public void WaterFieldFollowsTheCavernOnlyWhileItIsInstalled()
        {
            WaterField.Reset();
            Assert.IsFalse(WaterField.Custom);
            WaterField.Use(CavernLayout.DepthAt, CavernLayout.DepthAt, CavernLayout.WaterLevel);
            try
            {
                Assert.IsTrue(WaterField.Custom);
                Assert.AreEqual(CavernLayout.DepthAt(0f, 0f), WaterField.DepthAt(0f, 0f), 1e-5f);
                Assert.AreEqual(CavernLayout.WaterLevel, WaterField.Level, 1e-6f);
            }
            finally { WaterField.Reset(); }
            Assert.IsFalse(WaterField.Custom);
            Assert.AreEqual(ZoneLayout.WaterLevel, WaterField.Level, 1e-6f);
        }
    }
}
