using System.Collections.Generic;
using Evolve.World;
using NUnit.Framework;
using UnityEngine;

namespace Evolve.Tests.EditMode
{
    public class GrassMathTests
    {
        [Test]
        public void Density_IsZeroInWaterAndBeyondTheRim()
        {
            Assert.AreEqual(0f, GrassMath.Density(ZoneLayout.LakeCenter.x, ZoneLayout.LakeCenter.y), "in the middle of the lake");
            Assert.AreEqual(0f, GrassMath.Density(GrassMath.Rim + 5f, 0f), "up the rim");
        }

        [Test]
        public void Density_StaysOffTheTrailMiddle()
        {
            foreach (var path in ZoneLayout.Paths)
                foreach (var p in path)
                    Assert.AreEqual(0f, GrassMath.Density(p.x, p.y), 1e-4f, $"on the trail at {p}");
        }

        [Test]
        public void Density_IsThickNearTheWaterAndGoneFarFromIt()
        {
            float best = 0f;
            for (float x = -200f; x <= 200f; x += 5f)
                for (float z = -200f; z <= 200f; z += 5f)
                {
                    float d = GrassMath.Density(x, z);
                    Assert.GreaterOrEqual(d, 0f);
                    Assert.LessOrEqual(d, 1f);
                    if (ZoneLayout.DistanceToWater(x, z) > 85f) Assert.AreEqual(0f, d, 1e-4f, $"far from water at {x},{z}");
                    best = Mathf.Max(best, d);
                }
            Assert.GreaterOrEqual(best, 0.5f, "somewhere the carpet is really thick");
        }

        [Test]
        public void SelectPushers_TakesTheNearestEightWithinReach()
        {
            var candidates = new List<Vector4>();
            for (int i = 0; i < 12; i++) candidates.Add(new Vector4(2f + i * 3f, 0f, 0f, 1.5f));   // 2 m, 5 m, ... 35 m from the camera
            candidates.Add(new Vector4(GrassMath.PusherReach + 10f, 0f, 0f, 1.5f));                  // too far
            candidates.Add(new Vector4(1f, 0f, 0f, 0f));                                              // no radius: ignored
            var result = new List<Vector4>();
            GrassMath.SelectPushers(candidates, Vector3.zero, result);
            Assert.AreEqual(GrassMath.MaxPushers, result.Count);
            for (int i = 1; i < result.Count; i++) Assert.Less(result[i - 1].x, result[i].x, "nearest first");
            Assert.AreEqual(2f, result[0].x, 1e-4f);
        }

        [Test]
        public void SelectPushers_HandlesNoneAndFew()
        {
            var result = new List<Vector4> { Vector4.one };
            GrassMath.SelectPushers(new List<Vector4>(), Vector3.zero, result);
            Assert.AreEqual(0, result.Count);
            GrassMath.SelectPushers(new List<Vector4> { new Vector4(3f, 0f, 4f, 1f) }, Vector3.zero, result);
            Assert.AreEqual(1, result.Count);
        }
    }
}
