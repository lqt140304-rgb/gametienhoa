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

        [Test] public void PathsLeadFromEveryDoorstepRoundTheRingToTheHallDoor()
        {
            var houses = CityLayout.Houses();
            var paths = CityLayout.HousePaths();
            Assert.AreEqual(houses.Length, paths.Count);
            for (int i = 0; i < paths.Count; i++)
            {
                var path = paths[i];
                Assert.AreEqual(CityLayout.DoorStep(houses[i]), path[0]);
                Assert.Less(Vector2.Distance(path[path.Length - 1], CityLayout.DoorAt), 5f, "ends at the hall door");
                for (int k = 0; k < path.Length; k++)
                {
                    Assert.AreEqual(0f, ZoneLayout.DepthAt(path[k].x, path[k].y), "path on dry land");
                    if (k == 0) continue;
                    foreach (var h in houses) Assert.GreaterOrEqual(Vector2.Distance(path[k], h.Position), h.Radius + 0.4f, "the path does not run through a hut");
                }
            }
        }

        [Test] public void StatuesStandBetweenTheHutsAndOutOfTheWay()
        {
            var statues = CityLayout.Statues();
            Assert.GreaterOrEqual(statues.Count, 5);
            foreach (var (p, _) in statues)
            {
                Assert.AreEqual(0f, ZoneLayout.DepthAt(p.x, p.y));
                Assert.GreaterOrEqual(ZoneLayout.DistanceToPath(p.x, p.y), 3f);
                foreach (var h in CityLayout.Houses()) Assert.GreaterOrEqual(Vector2.Distance(p, h.Position), h.Radius + 2.5f);
            }
        }

        [Test] public void EveryClassHasItsOwnHouseOnAGoodSite()
        {
            var cls = CityLayout.ClassHouses();
            Assert.AreEqual(8, cls.Length, "one house per class");
            var seen = new System.Collections.Generic.HashSet<Evolve.Civilization.TreefolkClass>();
            var houses = CityLayout.Houses();
            for (int i = 0; i < cls.Length; i++)
            {
                var p = cls[i].Position;
                Assert.IsTrue(seen.Add(cls[i].Class), "no class twice");
                Assert.AreEqual(0f, ZoneLayout.DepthAt(p.x, p.y), "dry");
                Assert.GreaterOrEqual(ZoneLayout.DistanceToPath(p.x, p.y), 5f);
                Assert.IsFalse(ZoneLayout.InsideCave(p.x, p.y, 9f), "outside the village round the hall");
                foreach (var h in houses) Assert.GreaterOrEqual(Vector2.Distance(p, h.Position), h.Radius + 7f, "clear of the huts");
                for (int j = i + 1; j < cls.Length; j++) Assert.GreaterOrEqual(Vector2.Distance(p, cls[j].Position), CityLayout.ClassHouseSpacing);
            }
        }

        [Test] public void ClassHousesAreNotARingOrEvenlySpaced()
        {
            var cls = CityLayout.ClassHouses();
            var angles = new System.Collections.Generic.List<float>();
            float rMin = float.MaxValue, rMax = 0f;
            foreach (var h in cls)
            {
                Vector2 d = h.Position - CityLayout.HallCenter;
                angles.Add(Mathf.Repeat(Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg, 360f));
                rMin = Mathf.Min(rMin, d.magnitude); rMax = Mathf.Max(rMax, d.magnitude);
            }
            Assert.GreaterOrEqual(rMax - rMin, 12f, "near and far, not all at one distance");
            angles.Sort();
            float sum = 0f, sum2 = 0f;
            for (int i = 0; i < angles.Count; i++)
            {
                float gap = (angles[(i + 1) % angles.Count] - angles[i] + 360f) % 360f;
                sum += gap; sum2 += gap * gap;
            }
            float mean = sum / angles.Count, sd = Mathf.Sqrt(Mathf.Max(0f, sum2 / angles.Count - mean * mean));
            Assert.GreaterOrEqual(sd, 8f, "angular gaps differ");
        }

        [Test] public void ClassHousesFaceTheHallAndSuitTheirClass()
        {
            var cls = CityLayout.ClassHouses();
            float waterDuong = 0f, waterOthers = 0f; int n = 0; float hDuong = 0f, hXa = 0f, hAvg = 0f;
            foreach (var h in cls)
            {
                float w = ZoneLayout.DistanceToWater(h.Position.x, h.Position.y);
                float ht = TerrainShape.HeightAt(h.Position.x, h.Position.y, CityLayout.TerrainSeed);
                if (h.Class == Evolve.Civilization.TreefolkClass.DuongThe) { waterDuong = w; hDuong = ht; }
                else { waterOthers += w; n++; }
                if (h.Class == Evolve.Civilization.TreefolkClass.XaThe) hXa = ht;
                hAvg += ht;
            }
            Assert.LessOrEqual(waterDuong, waterOthers / n, "Dưỡng Thể lives nearest the water");
            Assert.GreaterOrEqual(hXa, hAvg / cls.Length, "Xạ Thể lives on high ground");
        }

        [Test] public void TheTunnelStartsOutsideTheEnlargedHall()
        {
            foreach (var p in ZoneLayout.TunnelLine) Assert.IsFalse(ZoneLayout.InsideCave(p.x, p.y, 3f));
        }
    }
}
