using Evolve.Creatures;
using Evolve.Ecology;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class CreatureBehaviorTests
    {
        static HunterInput Base() => new HunterInput { Hunger = 0f, PlayerDist = 99f, PreyDist = 99f, CarcassDist = 99f, GuardedCarcassDist = 99f, RivalDist = 99f, RivalRatio = 1f };

        [Test] public void HunterToSlime_BySize()
        {
            Assert.AreEqual(SlimeAttitude.Hunt, CreatureBehaviorRules.HunterToSlime(5f, 15f));      // a small slime is prey
            Assert.AreEqual(SlimeAttitude.Hunt, CreatureBehaviorRules.HunterToSlime(18f, 15f));     // 1.2x still prey
            Assert.AreEqual(SlimeAttitude.Ignore, CreatureBehaviorRules.HunterToSlime(22f, 15f));
            Assert.AreEqual(SlimeAttitude.Avoid, CreatureBehaviorRules.HunterToSlime(27f, 15f));    // 1.8x: avoided
            Assert.AreEqual(SlimeAttitude.Hunt, CreatureBehaviorRules.HunterToSlime(30f, 0f));
        }

        [Test] public void PreyFearsSlime_OnlyIfItCouldBeSwallowed()
        {
            Assert.IsTrue(CreatureBehaviorRules.PreyFearsSlime(10f, 8f));    // exactly 80%
            Assert.IsFalse(CreatureBehaviorRules.PreyFearsSlime(10f, 8.5f)); // too big to swallow: it ignores the slime
            Assert.IsTrue(CreatureBehaviorRules.PreyFearsSlime(30f, 3f));
        }

        [Test] public void Wanders_WhenNothingToDo()
        {
            Assert.AreEqual(HunterMode.Wander, CreatureBehaviorRules.DecideHunter(Base()));
        }

        [Test] public void ChasesPrey_OnlyWhenHungryAndInSense()
        {
            var i = Base(); i.HasPrey = true; i.PreyDist = 8f;
            Assert.AreEqual(HunterMode.Wander, CreatureBehaviorRules.DecideHunter(i));   // fed
            i.Hunger = 0.6f; Assert.AreEqual(HunterMode.ChasePrey, CreatureBehaviorRules.DecideHunter(i));
            i.PreyDist = 20f; Assert.AreEqual(HunterMode.Wander, CreatureBehaviorRules.DecideHunter(i));   // too far
        }

        [Test] public void ChasesTheSlime_WhenItIsPrey_EvenIfFed_AndPicksTheNearerOfPlayerAndPrey()
        {
            var i = Base(); i.PlayerSeen = true; i.PlayerDist = 5f; i.Attitude = SlimeAttitude.Hunt;
            Assert.AreEqual(HunterMode.ChasePlayer, CreatureBehaviorRules.DecideHunter(i));
            i.Hunger = 0.9f; i.HasPrey = true; i.PreyDist = 3f;
            Assert.AreEqual(HunterMode.ChasePrey, CreatureBehaviorRules.DecideHunter(i));   // the prey is nearer
            i.PreyDist = 7f;
            Assert.AreEqual(HunterMode.ChasePlayer, CreatureBehaviorRules.DecideHunter(i));
        }

        [Test] public void IgnoresASlimeThatIsNotPrey_AvoidsABigOne()
        {
            var i = Base(); i.PlayerSeen = true; i.PlayerDist = 5f; i.Attitude = SlimeAttitude.Ignore;
            Assert.AreEqual(HunterMode.Wander, CreatureBehaviorRules.DecideHunter(i));
            i.Attitude = SlimeAttitude.Avoid;
            Assert.AreEqual(HunterMode.AvoidPlayer, CreatureBehaviorRules.DecideHunter(i));
            i.PlayerDist = 15f;   // far enough: no need to run
            Assert.AreEqual(HunterMode.Wander, CreatureBehaviorRules.DecideHunter(i));
        }

        [Test] public void Scavenger_GoesToACarcass_WhenHungryEnough()
        {
            var i = Base(); i.Scavenger = true; i.HasCarcass = true; i.CarcassDist = 10f;
            Assert.AreEqual(HunterMode.Wander, CreatureBehaviorRules.DecideHunter(i));   // fed
            i.Hunger = 0.3f; Assert.AreEqual(HunterMode.ToCarcass, CreatureBehaviorRules.DecideHunter(i));
            i.CarcassDist = 30f; Assert.AreEqual(HunterMode.Wander, CreatureBehaviorRules.DecideHunter(i));
            i.CarcassDist = 10f; i.Scavenger = false; Assert.AreEqual(HunterMode.Wander, CreatureBehaviorRules.DecideHunter(i));
        }

        [Test] public void Guardian_StaysByTheCarcass_DrivesIntrudersOff_EvenWhenFed()
        {
            var i = Base(); i.Guardian = true; i.HasGuardedCarcass = true; i.GuardedCarcassDist = 10f;
            Assert.AreEqual(HunterMode.ToCarcass, CreatureBehaviorRules.DecideHunter(i));
            i.GuardedCarcassDist = 1f; Assert.AreEqual(HunterMode.Guard, CreatureBehaviorRules.DecideHunter(i));
            i.IntruderNear = true; Assert.AreEqual(HunterMode.ChaseIntruder, CreatureBehaviorRules.DecideHunter(i));
            var free = Base(); free.Guardian = true;   // no big carcass: an ordinary hunter
            Assert.AreEqual(HunterMode.Wander, CreatureBehaviorRules.DecideHunter(free));
        }

        [Test] public void Guardian_StillRunsFromAHugeSlime()
        {
            var i = Base(); i.Guardian = true; i.HasGuardedCarcass = true; i.GuardedCarcassDist = 1f; i.IntruderNear = true;
            i.PlayerSeen = true; i.PlayerDist = 4f; i.Attitude = SlimeAttitude.Avoid;
            Assert.AreEqual(HunterMode.AvoidPlayer, CreatureBehaviorRules.DecideHunter(i));
        }

        [Test] public void Rivals_BiggerOneIsAvoided_HungryEqualOneIsFought_OtherwiseIgnored()
        {
            var i = Base(); i.HasRival = true; i.RivalDist = 4f;
            i.RivalRatio = 1.5f; Assert.AreEqual(HunterMode.AvoidRival, CreatureBehaviorRules.DecideHunter(i));
            i.RivalRatio = 0.9f; Assert.AreEqual(HunterMode.Wander, CreatureBehaviorRules.DecideHunter(i));   // fed: leaves it alone
            i.Hunger = 0.7f; Assert.AreEqual(HunterMode.FightRival, CreatureBehaviorRules.DecideHunter(i));
            i.RivalRatio = 1.15f; Assert.AreEqual(HunterMode.Wander, CreatureBehaviorRules.DecideHunter(i));  // slightly bigger: not worth it
            i.RivalRatio = 0.9f; i.RivalDist = 12f; Assert.AreEqual(HunterMode.Wander, CreatureBehaviorRules.DecideHunter(i));
        }

        [Test] public void PreyScavenges_OnlyWithoutThreat()
        {
            Assert.IsTrue(CreatureBehaviorRules.PreyScavenges(true, 10f, false));
            Assert.IsFalse(CreatureBehaviorRules.PreyScavenges(true, 10f, true));
            Assert.IsFalse(CreatureBehaviorRules.PreyScavenges(true, 40f, false));
            Assert.IsFalse(CreatureBehaviorRules.PreyScavenges(false, 1f, false));
        }

        [Test] public void Seed_NamesMapBothWays_AndDietsMatchTheSpec()
        {
            Assert.AreEqual("coc_ho_gia", CaveEcologySeed.IdByName("Cóc Hồ Già (Clone)"));
            Assert.AreEqual("bo_canh_cung", CaveEcologySeed.IdByName("bọ cánh cứng nhỏ"));
            Assert.AreEqual("Chó Xương", CaveEcologySeed.NameOfId("cho_xuong"));
            Assert.IsNull(CaveEcologySeed.IdByName("Con Gì Đó"));
            CollectionAssert.AreEquivalent(new[] { "Thằn Lằn Bờ Hồ", "Bọ Cánh Cứng Nhỏ" }, CaveEcologySeed.DietSpeciesNames("coc_ho_gia"));   // Cóc eats both
            CollectionAssert.AreEquivalent(new[] { "Tắc Kè Đá" }, CaveEcologySeed.DietSpeciesNames("slime_da"));
            Assert.AreEqual(0, CaveEcologySeed.DietSpeciesNames("golem_thach_anh").Count);   // Golem does not eat
        }

        [Test] public void Seed_CarcassEaters()
        {
            foreach (var id in new[] { "gioi_trang", "ken_ken_hang", "bo_an_xac", "cho_xuong" }) Assert.IsTrue(CaveEcologySeed.EatsCarcass(id), id);
            foreach (var id in new[] { "tho_la", "coc_ho_gia", "slime_da" }) Assert.IsFalse(CaveEcologySeed.EatsCarcass(id), id);
            Assert.AreEqual("cho_xuong", CaveEcologySeed.GuardianId);
        }
    }
}
