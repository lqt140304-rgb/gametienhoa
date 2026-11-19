using System.Collections.Generic;
using System.Reflection;
using Evolve.Abilities;
using Evolve.Organs;
using Evolve.Survival;
using Evolve.Swallow;
using NUnit.Framework;
using UnityEngine;

namespace Evolve.Tests.EditMode
{
    public class SkillBarMenuTests
    {
        GameObject player;
        SkillBar bar;
        Swallower swallower;
        readonly List<Object> made = new List<Object>();

        [SetUp]
        public void Make()
        {
            player = new GameObject("HUD test");
            player.AddComponent<SurvivalStats>();
            var host = player.AddComponent<OrganHost>();
            swallower = player.AddComponent<Swallower>();
            bar = player.AddComponent<SkillBar>();
            // EditMode does not guarantee Awake; this component reference is all that Sync needs.
            typeof(SkillBar).GetField("host", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(bar, host);
        }

        [TearDown]
        public void Clean() { Object.DestroyImmediate(player); foreach (var o in made) Object.DestroyImmediate(o); made.Clear(); }

        OrganSO Organ(string n, OrganSlot slot, OrganTier tier)
        {
            var o = ScriptableObject.CreateInstance<OrganSO>();
            o.name = n; o.DisplayName = n; o.Slot = slot; o.Tier = tier;
            made.Add(o);
            return o;
        }

        void Learn(OrganSO o, int extra = 0)
        {
            var genes = new List<Gene> { new Gene(o, AnalysisRules.FromTier(o.Tier)) };
            for (int i = 0; i <= extra; i++) swallower.Book.Submit(genes, 1f, () => 0f);
        }

        void Sync() => typeof(SkillBar).GetMethod("Sync", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(bar, null);

        [Test]
        public void ClearedSkillStaysInLibraryUntilAssignedAgain()
        {
            var ability = ScriptableObject.CreateInstance<SpikeBurstAbility>(); made.Add(ability);
            var organ = Organ("Spikes", OrganSlot.Back, OrganTier.B); organ.GrantedAbility = ability;
            Learn(organ);                                   // learned, never worn
            Sync(); Assert.AreEqual(organ, bar.GetSlot(0));
            bar.Clear(0); Sync(); Assert.AreEqual(-1, bar.SlotOf(organ));
            bar.Assign(3, organ); Sync(); Assert.AreEqual(organ, bar.GetSlot(3));
        }

        [Test]
        public void LegGeneGivesSprint_WithStarsAsLevel()
        {
            var legs = Organ("RunnerLegs", OrganSlot.Legs, OrganTier.C); legs.SpeedMultiplier = 2f;
            Learn(legs);
            Sync();
            Assert.AreEqual(legs, bar.GetSlot(0));
            Assert.IsInstanceOf<SprintAbility>(bar.AbilityFor(legs));
            Assert.AreEqual(1, bar.LevelFor(legs));
            Assert.AreEqual(4f, SkillRules.SprintSeconds(bar.LevelFor(legs)));
            Learn(legs, 3);                                 // 3 more samples → ★2
            Assert.AreEqual(2, bar.LevelFor(legs));
        }

        [Test]
        public void PassiveGeneIsNotAMove()
        {
            var eyes = Organ("Eyes", OrganSlot.Eyes, OrganTier.C); eyes.GrantsSight = true;
            Learn(eyes);
            Sync();
            Assert.IsFalse(bar.IsUnlocked(eyes));
            Assert.AreEqual(-1, bar.SlotOf(eyes));
        }

        [Test]
        public void OwnMoveWinsOverSpeed()
        {
            var charge = ScriptableObject.CreateInstance<ChargeAbility>(); made.Add(charge);
            var legs = Organ("MuscularLegs", OrganSlot.Legs, OrganTier.B); legs.SpeedMultiplier = 1.7f; legs.GrantedAbility = charge;
            Learn(legs);
            Assert.AreSame(charge, bar.AbilityFor(legs));
        }
    }
}
