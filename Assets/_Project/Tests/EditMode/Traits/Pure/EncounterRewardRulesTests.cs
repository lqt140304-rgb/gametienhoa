using System.Collections.Generic;
using System.Linq;
using Evolve.Swallow;
using Evolve.Traits;
using Evolve.Visuals;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class EncounterRewardRulesTests
    {
        static List<GeneCandidate> Pool() => new List<GeneCandidate>
        {
            new GeneCandidate("c1", GeneRarity.Common), new GeneCandidate("c2", GeneRarity.Common), new GeneCandidate("r1", GeneRarity.Rare),
            new GeneCandidate("e1", GeneRarity.Epic), new GeneCandidate("l1", GeneRarity.Legendary),
        };

        [Test] public void PickGene_RespectsMinimumRarity_AndKnownGenes()
        {
            var known = new HashSet<string> { "r1" };
            for (int seed = 0; seed < 200; seed++)
            {
                string id = EncounterRewardRules.PickGene(Pool(), known, GeneRarity.Rare, seed);
                Assert.That(id, Is.AnyOf("e1", "l1"), "seed " + seed);   // r1 is known, commons are too weak
            }
        }

        [Test] public void PickGene_SameSeedSameGene_AndNullWhenNothingLeft()
        {
            Assert.AreEqual(EncounterRewardRules.PickGene(Pool(), null, GeneRarity.Common, 7), EncounterRewardRules.PickGene(Pool(), null, GeneRarity.Common, 7));
            var all = new HashSet<string>(Pool().Select(c => c.Id));
            Assert.IsNull(EncounterRewardRules.PickGene(Pool(), all, GeneRarity.Common, 1));
            Assert.AreEqual("l1", EncounterRewardRules.PickGene(Pool(), null, GeneRarity.Legendary, 1));
            Assert.IsNull(EncounterRewardRules.PickGene(null, null, GeneRarity.Common, 1));
        }

        [Test] public void PickGene_CommonerGenesComeMoreOften_LegendaryRarely()
        {
            int common = 0, legendary = 0;
            for (int seed = 0; seed < 4000; seed++)
            {
                string id = EncounterRewardRules.PickGene(Pool(), null, GeneRarity.Common, seed);
                if (id.StartsWith("c")) common++; else if (id == "l1") legendary++;
            }
            Assert.Greater(common, legendary * 5);
            Assert.Greater(legendary, 0);
        }

        [Test] public void RerollFlaw_NeverSameNeverDuplicate_ChangesExactlyOne()
        {
            var current = new[] { "a", "b" };
            var pool = new[] { "a", "b", "c", "d", "e" };
            for (int seed = 0; seed < 300; seed++)
            {
                var r = EncounterRewardRules.RerollFlaw(current, pool, seed);
                Assert.AreEqual(2, r.Length);
                Assert.AreEqual(2, r.Distinct().Count(), "seed " + seed);
                Assert.AreEqual(1, r.Count(x => current.Contains(x)), "exactly one flaw kept, seed " + seed);
            }
        }

        [Test] public void RerollFlaw_Deterministic_AndUnchangedWhenNothingToDraw()
        {
            CollectionAssert.AreEqual(EncounterRewardRules.RerollFlaw(new[] { "a" }, new[] { "a", "b", "c" }, 5), EncounterRewardRules.RerollFlaw(new[] { "a" }, new[] { "a", "b", "c" }, 5));
            CollectionAssert.AreEqual(new[] { "a" }, EncounterRewardRules.RerollFlaw(new[] { "a" }, new[] { "a" }, 1));
            CollectionAssert.AreEqual(new string[0], EncounterRewardRules.RerollFlaw(new string[0], new[] { "a" }, 1));
            CollectionAssert.AreEqual(new[] { "a" }, EncounterRewardRules.RerollFlaw(new[] { "a" }, null, 1));
        }

        [Test] public void CoreBoost_StepsAndCaps()
        {
            Assert.AreEqual(1f, EncounterRewardRules.CoreBoost(0), 1e-4f);
            Assert.AreEqual(1.15f, EncounterRewardRules.CoreBoost(1), 1e-4f);
            Assert.AreEqual(1.45f, EncounterRewardRules.CoreBoost(3), 1e-4f);
            Assert.AreEqual(1.45f, EncounterRewardRules.CoreBoost(9), 1e-4f);
            Assert.AreEqual(1f, EncounterRewardRules.CoreBoost(-2), 1e-4f);
        }

        [Test] public void BoostMultiplier_AndReduction_StrengthenTheEffect_NotTheBase()
        {
            Assert.AreEqual(1f, EncounterRewardRules.BoostMultiplier(1f, 1.45f), 1e-4f);
            Assert.AreEqual(1.5f, EncounterRewardRules.BoostMultiplier(1.5f, 1f), 1e-4f);
            Assert.AreEqual(1.575f, EncounterRewardRules.BoostMultiplier(1.5f, 1.15f), 1e-3f);
            Assert.AreEqual(0.4f, EncounterRewardRules.BoostReduction(0.4f, 1f), 1e-4f);
            Assert.Less(EncounterRewardRules.BoostReduction(0.4f, 1.45f), 0.4f);
            Assert.AreEqual(0f, EncounterRewardRules.BoostReduction(0.4f, 5f), 1e-4f);
        }

        [Test] public void AddHeat_CapsAtTwenty_NeverNegative()
        {
            Assert.AreEqual(8f, EncounterRewardRules.AddHeat(0f, 8f), 1e-4f);
            Assert.AreEqual(20f, EncounterRewardRules.AddHeat(15f, 8f), 1e-4f);
            Assert.AreEqual(0f, EncounterRewardRules.AddHeat(2f, -9f), 1e-4f);
        }

        [Test] public void ColorUnlocks_AddEncodeDecode_Tolerant()
        {
            var u = new ColorUnlocks();
            Assert.IsTrue(u.Add(ColorUnlocks.Rainbow)); Assert.IsFalse(u.Add(ColorUnlocks.Rainbow));
            Assert.IsFalse(u.Add(null)); Assert.IsFalse(u.Add("  ")); Assert.IsFalse(u.Add("a,b"));
            Assert.IsTrue(u.Has("rainbow")); Assert.IsFalse(u.Has("gold")); Assert.IsFalse(u.Has(null));
            u.Add("aurora");
            var back = ColorUnlocks.Decode(u.Encode());
            Assert.IsTrue(back.Has("rainbow")); Assert.IsTrue(back.Has("aurora")); Assert.AreEqual(2, back.Count);
            Assert.AreEqual(0, ColorUnlocks.Decode(null).Count); Assert.AreEqual(0, ColorUnlocks.Decode(",,  ,").Count);
        }
    }
}
