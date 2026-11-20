using System;
using System.Linq;
using Evolve.Maze;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class ChamberZonesTests
    {
        const float Radius = 75f, Usable = Radius * 0.74f;
        static float Ang((float X, float Z) p) => (float)((Math.Atan2(p.Z, p.X) * 180.0 / Math.PI + 360.0) % 360.0);
        static float Dist((float X, float Z) p) => (float)Math.Sqrt(p.X * p.X + p.Z * p.Z);
        static float Gap(float a, float b) => (float)Math.Abs(((a - b) % 360.0 + 540.0) % 360.0 - 180.0);
        static float Between((float X, float Z) a, (float X, float Z) b) => (float)Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Z - b.Z) * (a.Z - b.Z));

        [Test] public void Beacon_FacesTheMainEntrance()
        {
            var plan = ChamberZones.Plan(Radius, new[] { 0f, 120f }, 11, 6);
            Assert.GreaterOrEqual(Ang(plan.Beacon), 150f); Assert.LessOrEqual(Ang(plan.Beacon), 210f);
            Assert.GreaterOrEqual(Dist(plan.Beacon), Usable * 0.4f - 0.01f); Assert.LessOrEqual(Dist(plan.Beacon), Usable * 0.6f + 0.01f);
        }

        [Test] public void Nests_AvoidTunnelMouths_AndEachOther()
        {
            var tunnels = new[] { 0f, 120f };
            var plan = ChamberZones.Plan(Radius, tunnels, 11, 6);
            Assert.GreaterOrEqual(plan.Nests.Count, 4);
            foreach (var n in plan.Nests)
            {
                Assert.GreaterOrEqual(Dist(n), Usable * 0.4f - 0.01f); Assert.LessOrEqual(Dist(n), Usable * 0.85f + 0.01f);
                foreach (float t in tunnels) Assert.Greater(Gap(Ang(n), t), 21.9f, "in front of a tunnel mouth");
                Assert.GreaterOrEqual(Between(n, plan.Beacon), 19.9f);
            }
            for (int i = 0; i < plan.Nests.Count; i++) for (int j = i + 1; j < plan.Nests.Count; j++) Assert.GreaterOrEqual(Between(plan.Nests[i], plan.Nests[j]), 19.9f);
        }

        [Test] public void Nook_FarFromMouths_OpeningAwayFromCentre()
        {
            var tunnels = new[] { 0f, 120f };
            var plan = ChamberZones.Plan(Radius, tunnels, 11, 6);
            Assert.GreaterOrEqual(Dist(plan.Nook), Usable * 0.8f - 0.01f); Assert.LessOrEqual(Dist(plan.Nook), Usable * 0.95f + 0.01f);
            foreach (float t in tunnels) Assert.Greater(Gap(Ang(plan.Nook), t), 40f);
            Assert.GreaterOrEqual(Gap(plan.NookOpeningDeg, Ang(plan.Nook) + 180f), 90f);   // the opening does not face the centre
        }

        [Test] public void SameSeed_SamePlan_DifferentSeed_DifferentNests()
        {
            var tunnels = new[] { 0f, 120f };
            var a = ChamberZones.Plan(Radius, tunnels, 5, 4); var b = ChamberZones.Plan(Radius, tunnels, 5, 4); var c = ChamberZones.Plan(Radius, tunnels, 6, 4);
            Assert.AreEqual(a.Beacon, b.Beacon); CollectionAssert.AreEqual(a.Nests, b.Nests); Assert.AreEqual(a.Nook, b.Nook);
            CollectionAssert.AreNotEqual(a.Nests, c.Nests);
        }

        [Test] public void OneTunnelOrNone_StillReturnsBeaconAndANest()
        {
            foreach (var tunnels in new[] { new float[0], new[] { 0f }, null })
            {
                var plan = ChamberZones.Plan(Radius, tunnels, 3, 3);
                Assert.GreaterOrEqual(plan.Nests.Count, 1);
                Assert.Greater(Dist(plan.Beacon), 0f);
            }
        }
    }
}
