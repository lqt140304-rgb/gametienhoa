using Evolve.World;
using NUnit.Framework;
using UnityEngine;

namespace Evolve.Tests.EditMode
{
    public class CityLayoutTests
    {
        [Test] public void TheHallIsGrand()
        {
            Assert.AreEqual(29f, ZoneLayout.CaveRadii.x); Assert.AreEqual(22f, ZoneLayout.CaveRadii.y);   // 58 x 44 m
            Assert.AreEqual(18f, ZoneLayout.CaveHeight);
        }

        [Test] public void EightPillarsStandWellApartInsideTheHall()
        {
            var p = CityLayout.Pillars;
            Assert.AreEqual(8, p.Length);
            for (int i = 0; i < p.Length; i++)
            {
                Assert.IsTrue(CityLayout.InsideHall(p[i], 4f), "pillar " + i);
                Assert.GreaterOrEqual(Vector2.Distance(p[i], CityLayout.DaisCenter), CityLayout.DaisRadius + 3f + CityLayout.PillarRadius, "clear of the dais");
                for (int j = i + 1; j < p.Length; j++)
                    Assert.GreaterOrEqual(Vector2.Distance(p[i], p[j]) - 2f * CityLayout.PillarRadius, 3f, $"gap between pillars {i} and {j}");   // a slime and a treefolk pass
                Assert.GreaterOrEqual(Vector2.Distance(p[i], CityLayout.DoorAt), 10f, "the door stays clear");
                Assert.GreaterOrEqual(Vector2.Distance(p[i], CityLayout.SlitAt), 10f, "the slit stays clear");
            }
        }

        [Test] public void TheDaisThroneAndFurnaceLieInsideAndTheRoofIsHighEnough()
        {
            Assert.IsTrue(CityLayout.InsideHall(CityLayout.DaisCenter, 10f));
            Assert.IsTrue(CityLayout.InsideHall(CityLayout.ThroneAt, 10f));
            Assert.IsTrue(CityLayout.InsideHall(CityLayout.FurnaceAt, 2f));
            Assert.GreaterOrEqual(CityLayout.RoofAbove(CityLayout.DaisCenter, 1.2f), 17f);
            foreach (var p in CityLayout.Pillars) Assert.Greater(CityLayout.RoofAbove(p, 1.2f), 8f, "a pillar reaches a real roof");
            Assert.AreEqual(0.75f, CityLayout.DaisTop, 1e-4f);
        }

        [Test] public void TheHallStandsOnDryLandWithDoorAndSlitOnItsWall()
        {
            for (int a = 0; a < 48; a++)
            {
                float ang = a * Mathf.PI / 24f;
                var c = CityLayout.HallCenter;
                for (float s = 0.3f; s <= 1.1f; s += 0.2f)
                    Assert.AreEqual(0f, ZoneLayout.DepthAt(c.x + Mathf.Cos(ang) * ZoneLayout.CaveRadii.x * s, c.y + Mathf.Sin(ang) * ZoneLayout.CaveRadii.y * s), $"hall floor at {a},{s}");
            }
            Assert.IsFalse(CityLayout.InsideHall(CityLayout.DoorAt, 1f));
            Assert.IsTrue(ZoneLayout.InsideCave(CityLayout.DoorAt.x - 0.5f, CityLayout.DoorAt.y));
            Assert.IsTrue(ZoneLayout.InsideCave(CityLayout.SlitAt.x, CityLayout.SlitAt.y - 0.5f));
        }

        [Test] public void HousesStandRoundTheHall()
        {
            var houses = CityLayout.Houses();
            Assert.GreaterOrEqual(houses.Length, 10);
            Assert.LessOrEqual(houses.Length, CityLayout.MaxHouses);
            int shelters = 0;
            for (int i = 0; i < houses.Length; i++)
            {
                var p = houses[i].Position;
                Assert.IsFalse(ZoneLayout.InsideCave(p.x, p.y, 3f), "outside the hall");
                Assert.AreEqual(0f, ZoneLayout.DepthAt(p.x, p.y), "dry");
                Assert.GreaterOrEqual(ZoneLayout.DistanceToPath(p.x, p.y), 5f, "off the trails");
                Assert.GreaterOrEqual(ZoneLayout.DistanceToLine(p, ZoneLayout.TunnelLine), 6f, "off the tunnel");
                var step = CityLayout.DoorStep(houses[i]);
                Assert.Less(Vector2.Distance(step, CityLayout.HallCenter), Vector2.Distance(p, CityLayout.HallCenter), "the door faces the hall");
                if (houses[i].Shelter) shelters++;
                for (int j = i + 1; j < houses.Length; j++) Assert.GreaterOrEqual(Vector2.Distance(p, houses[j].Position), 7.5f);
            }
            Assert.AreEqual(3, shelters);
        }

        [Test] public void TheTunnelStartsOutsideTheEnlargedHall()
        {
            foreach (var p in ZoneLayout.TunnelLine) Assert.IsFalse(ZoneLayout.InsideCave(p.x, p.y, 3f));
        }
    }
}
