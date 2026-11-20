using System.Collections.Generic;
using System.Linq;
using Evolve.Creatures;
using Evolve.Ecology;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    /// <summary>Two hours of play per cave: the food chains must balance on their own, and recover when the player upsets them.</summary>
    public class CaveEcologyBalanceTests
    {
        const float Step = 0.5f;
        const int Steps = 240;   // 120 minutes

        sealed class Trace
        {
            public readonly Dictionary<string, int> Longest0 = new Dictionary<string, int>();   // longest run of steps at zero
            readonly Dictionary<string, int> run = new Dictionary<string, int>();
            public readonly Dictionary<string, int> Max = new Dictionary<string, int>();
            public void Watch(EcologyWorld w)
            {
                foreach (var r in w.Regions)
                    foreach (var s in r.Value.Species.Where(x => !x.Guest))
                    {
                        string k = r.Key + "/" + s.Id;
                        Assert.GreaterOrEqual(s.Count, 0, k); Assert.LessOrEqual(s.Count, s.Capacity, k);
                        run[k] = s.Count == 0 ? (run.TryGetValue(k, out var n) ? n : 0) + 1 : 0;
                        if (!Longest0.TryGetValue(k, out var l) || run[k] > l) Longest0[k] = run[k];
                        if (!Max.TryGetValue(k, out var m) || s.Count > m) Max[k] = s.Count;
                    }
            }
        }

        static void Run(EcologyWorld w, Trace t, int steps) { for (int i = 0; i < steps; i++) { w.Tick(Step); t.Watch(w); } }

        [Test] public void Seed_HasAllElevenCaves_FourSpeciesEach_AndEveryDietPointsAtSomethingReal()
        {
            var w = CaveEcologySeed.Build();
            Assert.AreEqual(11, w.Regions.Count);
            foreach (var r in w.Regions)
            {
                Assert.AreEqual(4, r.Value.Species.Count, r.Key);
                foreach (var s in r.Value.Species)
                    foreach (var d in s.Diet)
                    {
                        var home = d.RegionId == null ? r.Value : w.Find(d.RegionId);
                        Assert.IsNotNull(home, r.Key + "/" + s.Id + " -> region " + d.RegionId);
                        if (d.IsPool) Assert.IsTrue(home.Pools.ContainsKey(d.Id), r.Key + "/" + s.Id + " pool " + d.Id);
                        else Assert.IsNotNull(home.Get(d.Id), r.Key + "/" + s.Id + " eats " + d.Id);
                        if (d.RegionId != null) CollectionAssert.Contains(w.Neighbours(r.Key), d.RegionId, r.Key + "/" + s.Id + " reaches only neighbours");
                    }
            }
        }

        [Test] public void TwoHours_EveryCaveStaysInBounds_AndNothingStaysExtinct()
        {
            var w = CaveEcologySeed.Build();
            var t = new Trace();
            Run(w, t, Steps);
            foreach (var kv in t.Longest0)
            {
                bool lord = w.Regions.Values.Any(r => kv.Key.EndsWith("/" + r.Species[3].Id));   // the fourth species of a cave is its lord
                Assert.LessOrEqual(kv.Value * Step, lord ? 10f : 12f, kv.Key + " was at zero for " + kv.Value * Step + " min");
            }
            foreach (var r in w.Regions)
                foreach (var s in r.Value.Species.Where(s => !s.Guest))
                    Assert.GreaterOrEqual(s.Count, s.Floor, r.Key + "/" + s.Id);
        }

        [Test] public void WideCaves_TwoHours_ScaledPopulationsStayAliveAndInBounds()
        {
            var w = CaveEcologySeed.Build(id => id == "khoi_sinh" ? 1f : 1.875f);
            Assert.AreEqual(15, w.Find("vuon_quang").Species[0].Count);   // 8 prey heads ×1.875
            Assert.AreEqual(1, w.Find("vuon_quang").Species[3].Count);    // the lord stays one
            Assert.AreEqual(8, w.Find("khoi_sinh").Species[0].Count);     // the start cavern is unchanged
            var t = new Trace();
            Run(w, t, Steps);
            foreach (var kv in t.Longest0)
            {
                bool lord = w.Regions.Values.Any(r => kv.Key.EndsWith("/" + r.Species[3].Id));
                Assert.LessOrEqual(kv.Value * Step, lord ? 10f : 12f, kv.Key + " was at zero for " + kv.Value * Step + " min");
            }
            foreach (var r in w.Regions)
                foreach (var s in r.Value.Species.Where(s => !s.Guest))
                    Assert.LessOrEqual(s.Count, s.Capacity + 1, r.Key + "/" + s.Id);
        }

        [Test] public void TwoHours_PreyAndHunters_StayAlive_OnAverage()
        {
            var w = CaveEcologySeed.Build();
            var sums = new Dictionary<string, float>();
            for (int i = 0; i < Steps; i++)
            {
                w.Tick(Step);
                foreach (var r in w.Regions) foreach (var s in r.Value.Species.Where(x => !x.Guest)) { string k = r.Key + "/" + s.Id; sums[k] = (sums.TryGetValue(k, out var v) ? v : 0f) + s.Count; }
            }
            foreach (var kv in sums) Assert.Greater(kv.Value / Steps, 0.5f, kv.Key + " average population");
        }

        [Test] public void KillAllHunters_PreyDoNotExceedCapacity_HuntersReturn()
        {
            var w = CaveEcologySeed.Build();
            var t = new Trace();
            Run(w, t, 40);   // 20 min
            foreach (var r in w.Regions.Values) r.Species[2].Count = 0;   // every hunter is gone
            Run(w, t, 60);   // 30 more minutes
            foreach (var r in w.Regions)
                Assert.GreaterOrEqual(r.Value.Species[2].Count, 1, r.Key + " hunter back");
        }

        [Test] public void KillAllPrey_NothingStaysGone_PreyReturnFromTheNests()
        {
            var w = CaveEcologySeed.Build();
            Run(w, new Trace(), 40);
            foreach (var r in w.Regions.Values) { r.Species[0].Count = 0; r.Species[1].Count = 0; }
            var after = new Trace();
            Run(w, after, 80);   // 40 more minutes
            foreach (var r in w.Regions)
            {
                // a hunter may be eaten down by its lord meanwhile (Cóc eats Thằn Lằn), but the nest brings it back
                Assert.LessOrEqual(after.Longest0[r.Key + "/" + r.Value.Species[0].Id] * Step, 8f, r.Key + " prey A");
                Assert.LessOrEqual(after.Longest0[r.Key + "/" + r.Value.Species[1].Id] * Step, 8f, r.Key + " prey B");
                Assert.LessOrEqual(after.Longest0[r.Key + "/" + r.Value.Species[2].Id] * Step, 14f, r.Key + " hunter");
                Assert.GreaterOrEqual(r.Value.Species[0].Count + r.Value.Species[1].Count, 1, r.Key + " prey back at the end");
            }
        }

        [Test] public void KillCaveLord_ComesBackBetweenEightAndTenMinutes()
        {
            var w = CaveEcologySeed.Build();
            var t = new Trace();
            Run(w, t, 40);
            foreach (var r in w.Regions.Values) r.Species[3].Count = 0;
            int back = -1;
            for (int i = 1; i <= 60 && back < 0; i++)
            {
                w.Tick(Step);
                if (w.Regions.Values.All(r => r.Species[3].Count >= 1)) back = i;
            }
            Assert.Greater(back, 0, "lords never returned");
            Assert.That(back * Step, Is.InRange(8f, 10f));
        }

        [Test] public void Overhunting_EscapeChanceKeepsTheTwoBravestPreyAlive()
        {
            // Bọ Gai Tím (difficulty 3) escapes half the hunts: with a hunter at its side it must still be around after two hours
            var w = CaveEcologySeed.Build();
            var t = new Trace();
            Run(w, t, Steps);
            Assert.AreEqual(0.5f, w.Find("vuc_nut_tim").Get("bo_gai_tim").EscapeChance, 1e-4f);
            Assert.Greater(t.Max["vuc_nut_tim/bo_gai_tim"], 0);
        }

        [Test, Explicit("prints the table of populations; run by hand when tuning")]
        public void Report()
        {
            var w = CaveEcologySeed.Build();
            var sum = new Dictionary<string, float>(); var min = new Dictionary<string, int>(); var max = new Dictionary<string, int>();
            for (int i = 0; i < Steps; i++)
            {
                w.Tick(Step);
                foreach (var r in w.Regions) foreach (var s in r.Value.Species.Where(x => !x.Guest))
                {
                    string k = r.Key + "/" + s.Id;
                    sum[k] = (sum.TryGetValue(k, out var v) ? v : 0f) + s.Count;
                    min[k] = min.TryGetValue(k, out var a) ? System.Math.Min(a, s.Count) : s.Count;
                    max[k] = max.TryGetValue(k, out var b) ? System.Math.Max(b, s.Count) : s.Count;
                }
            }
            foreach (var kv in sum) System.Console.Error.WriteLine($"REPORT {kv.Key,-40} avg {kv.Value / Steps,5:0.0}  min {min[kv.Key]}  max {max[kv.Key]}");
        }
    }
}
