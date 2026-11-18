using System.Collections.Generic;
using Evolve.World;
using NUnit.Framework;
using UnityEngine;

namespace Evolve.Tests.EditMode
{
    public class VaultLayoutTests
    {
        static float Ceiling(float x, float z)
        {
            float d = Mathf.Sqrt(x * x + z * z) / 375f;
            return 32f * Mathf.Sqrt(Mathf.Max(0f, 1f - d * d));   // a dome like the scene's
        }

        static List<Vector2> Flat(RibPath r)
        {
            var l = new List<Vector2>();
            foreach (var p in r.Points) l.Add(new Vector2(p.x, p.z));
            return l;
        }

        [Test]
        public void Ribs_StartAtTheGreatHall()
        {
            foreach (var r in VaultLayout.Ribs(5, Ceiling))
            {
                if (r.Level != 0) continue;
                var s = new Vector2(r.Points[0].x, r.Points[0].z);
                if (Vector2.Distance(s, ZoneLayout.LakeCenter) < 20f) continue;   // the lake's own ribs
                Assert.IsFalse(ZoneLayout.InsideCave(s.x, s.y, 1f), "starts outside the hall");
                Assert.IsTrue(ZoneLayout.InsideCave(s.x, s.y, 12f), "but right by it");
            }
        }

        [Test]
        public void Ribs_CountAndRadii()
        {
            var ribs = VaultLayout.Ribs(5, Ceiling);
            int mains = 0;
            foreach (var r in ribs)
            {
                if (r.Level == 0) { mains++; Assert.GreaterOrEqual(r.RadiusStart, 2.4f); Assert.LessOrEqual(r.RadiusStart, 3.5f); }
                Assert.Less(r.RadiusEnd, r.RadiusStart + 1e-3f, "ribs thin out");
                Assert.GreaterOrEqual(r.Points.Length, 2);
            }
            Assert.GreaterOrEqual(mains, 12, "9-11 great ribs from the hall plus three from the lake");
            Assert.LessOrEqual(mains, 14);
        }

        [Test]
        public void Ribs_NeverCrossTheHall()
        {
            foreach (var r in VaultLayout.Ribs(5, Ceiling))
                foreach (var p in r.Points)
                    Assert.IsFalse(ZoneLayout.InsideCave(p.x, p.z, 2f), $"rib point {p} is over the hall");
        }

        [Test]
        public void Ribs_DoNotIntersectBeyondForks_ProductionSeedToo()
        {
            foreach (int seed in new[] { 5, 11 })
            {
                var ribs = VaultLayout.Ribs(seed, Ceiling);
                for (int i = 0; i < ribs.Length; i++)
                    for (int j = i + 1; j < ribs.Length; j++)
                    {
                        if (ribs[i].Level != 0 || ribs[j].Level != 0) continue;
                        foreach (var a in Flat(ribs[i]))
                            foreach (var b in Flat(ribs[j]))
                            {
                                if (Vector2.Distance(a, ZoneLayout.RootFoot.Center) < 40f && Vector2.Distance(b, ZoneLayout.RootFoot.Center) < 40f) continue;
                                if (Vector2.Distance(a, ZoneLayout.LakeCenter) < 30f && Vector2.Distance(b, ZoneLayout.LakeCenter) < 30f) continue;
                                Assert.GreaterOrEqual(Vector2.Distance(a, b), 4f, $"seed {seed}: main ribs {i} and {j} touch");
                            }
                    }
                foreach (var r in ribs) foreach (var p in r.Points) Assert.IsFalse(ZoneLayout.InsideCave(p.x, p.z, 2f), $"seed {seed}: a rib enters the hall");
            }
        }

        [Test]
        public void Ribs_DoNotIntersectBeyondForks()
        {
            var ribs = VaultLayout.Ribs(5, Ceiling);
            for (int i = 0; i < ribs.Length; i++)
                for (int j = i + 1; j < ribs.Length; j++)
                {
                    if (ribs[i].Level != 0 || ribs[j].Level != 0) continue;
                    foreach (var a in Flat(ribs[i]))
                        foreach (var b in Flat(ribs[j]))
                        {
                            if (Vector2.Distance(a, ZoneLayout.RootFoot.Center) < 40f && Vector2.Distance(b, ZoneLayout.RootFoot.Center) < 40f) continue; // they start side by side at the hall
                            if (Vector2.Distance(a, ZoneLayout.LakeCenter) < 30f && Vector2.Distance(b, ZoneLayout.LakeCenter) < 30f) continue;       // and the lake's at the lake
                            Assert.GreaterOrEqual(Vector2.Distance(a, b), 4f, $"main ribs {i} and {j} touch");
                        }
                }
        }

        [Test]
        public void VaultHeight_IsNotFlat()
        {
            // not the corners (where the dome itself falls away): the bays must make the difference
            float lake = VaultLayout.VaultHeight(ZoneLayout.LakeCenter.x, ZoneLayout.LakeCenter.y, Ceiling(ZoneLayout.LakeCenter.x, ZoneLayout.LakeCenter.y));
            float mulch = VaultLayout.VaultHeight(ZoneLayout.Humus.Center.x, ZoneLayout.Humus.Center.y, Ceiling(ZoneLayout.Humus.Center.x, ZoneLayout.Humus.Center.y));
            Assert.GreaterOrEqual(lake - mulch, 6f, "high bell over the lake, low over the mulch bank");
            float min = float.MaxValue, max = float.MinValue;
            for (float x = -180f; x <= 180f; x += 20f)
                for (float z = -180f; z <= 180f; z += 20f)
                {
                    float h = VaultLayout.VaultHeight(x, z, Ceiling(x, z));
                    min = Mathf.Min(min, h);
                    max = Mathf.Max(max, h);
                }
            Assert.GreaterOrEqual(max - min, 6f, "bays of different height");
        }

        [Test]
        public void VaultHeight_HasNoVerticalSteps()
        {
            for (float x = -230f; x <= 230f; x += 7f)
                for (float z = -230f; z <= 230f; z += 7f)
                {
                    float a = VaultLayout.VaultHeight(x, z, Ceiling(x, z));
                    float b = VaultLayout.VaultHeight(x + 2f, z, Ceiling(x + 2f, z));
                    Assert.LessOrEqual(Mathf.Abs(a - b), 1.6f, $"a step at {x},{z}");
                }
        }

        [Test]
        public void VaultHeight_StaysAboveTheHallDome()
        {
            var c = ZoneLayout.RootFoot.Center;
            float need = ZoneLayout.ZoneLevels[1] + ZoneLayout.CaveHeight + 4f;
            foreach (var d in new[] { new Vector2(0, 0), new Vector2(10, 0), new Vector2(-12, 8), new Vector2(0, -14) })
            {
                float h = VaultLayout.VaultHeight(c.x + d.x, c.y + d.y, Ceiling(c.x + d.x, c.y + d.y));
                Assert.GreaterOrEqual(h, need - 0.01f, $"vault under the hall's dome at {d}");
            }
        }

        [Test]
        public void VaultHeight_RespectsBounds()
        {
            for (float x = -240f; x <= 240f; x += 15f)
                for (float z = -240f; z <= 240f; z += 15f)
                {
                    float c = Ceiling(x, z);
                    float h = VaultLayout.VaultHeight(x, z, c);
                    Assert.GreaterOrEqual(h, VaultLayout.MinHeight - 1e-3f);
                    Assert.LessOrEqual(h, Mathf.Max(VaultLayout.MinHeight, c - VaultLayout.BelowDome) + 1e-3f);
                }
        }

        [Test]
        public void Slots_AreAwayFromHallAndRibs()
        {
            var ribs = VaultLayout.Ribs(5, Ceiling);
            var slots = VaultLayout.Slots(5, ribs);
            Assert.GreaterOrEqual(slots.Length, 30);
            foreach (var s in slots)
            {
                Assert.IsFalse(ZoneLayout.InsideCave(s.x, s.y, 4f), "slit over the hall");
                foreach (var r in ribs) foreach (var p in r.Points) Assert.GreaterOrEqual(Vector2.Distance(s, new Vector2(p.x, p.z)), 5f - 1e-3f);
            }
            for (int i = 0; i < slots.Length; i++)
            {
                float w = VaultLayout.SlotHalfWidth(i);
                Assert.GreaterOrEqual(w, 0.5f - 1e-4f);
                Assert.LessOrEqual(w, 2f + 1e-4f);
            }
        }

        [Test]
        public void FootPoints_AreOnTheRim()
        {
            var feet = VaultLayout.FootPoints(VaultLayout.Ribs(5, Ceiling));
            Assert.Greater(feet.Length, 0);
            foreach (var f in feet) Assert.GreaterOrEqual(Mathf.Max(Mathf.Abs(f.x), Mathf.Abs(f.y)), VaultLayout.Rim - 8f - 1e-3f);
        }

        [Test]
        public void Ribs_SameSeedSameResult()
        {
            var a = VaultLayout.Ribs(9, Ceiling);
            var b = VaultLayout.Ribs(9, Ceiling);
            Assert.AreEqual(a.Length, b.Length);
            for (int i = 0; i < a.Length; i++) Assert.AreEqual(a[i].Points[0], b[i].Points[0]);
        }

        [Test]
        public void Ribs_ManySeedsNeverThrow()
        {
            for (int seed = 0; seed < 40; seed++)
            {
                var ribs = VaultLayout.Ribs(seed, Ceiling);
                Assert.Greater(ribs.Length, 0);
                foreach (var r in ribs) Assert.GreaterOrEqual(r.Points.Length, 2);
                VaultLayout.Slots(seed, ribs);
                VaultLayout.FootPoints(ribs);
            }
        }
    }
}
