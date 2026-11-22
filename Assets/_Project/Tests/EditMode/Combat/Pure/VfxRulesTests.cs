using System.Linq;
using Evolve.Combat;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class VfxRulesTests
    {
        static readonly string[] Bosses = { "Golem Thạch Anh", "Chim Hang Mẹ", "Chó Xương" };

        [Test] public void EveryMoveOfEveryKittedBoss_HasAChargeAndAnAftermath_NotJustAnImpact()
        {
            foreach (var boss in Bosses)
                foreach (var m in MovesetSeed.For(boss))
                {
                    var plan = VfxRules.PlanFor(boss, m);
                    Assert.Greater(plan.ChargePuffsPerSecond, 0f, boss + " / " + m.Name + ": the tell has something to see");
                    Assert.Greater(plan.AfterSeconds, 0f, boss + " / " + m.Name + ": something is left behind after the hit");
                    Assert.Greater(plan.AfterEmbers, 0, boss + " / " + m.Name);
                    Assert.GreaterOrEqual(plan.ChargeRings, 1, boss + " / " + m.Name);
                }
        }

        [Test] public void BiggerMoves_GetBiggerStages()
        {
            var moves = MovesetSeed.ForForm("Golem Thạch Anh", MoveForm.Fist);
            var normal = VfxRules.PlanFor("Golem Thạch Anh", moves.First(m => m.Role == MoveRole.Normal));
            var special = VfxRules.PlanFor("Golem Thạch Anh", moves.Single(m => m.Role == MoveRole.Special));
            var ult = VfxRules.PlanFor("Golem Thạch Anh", moves.Single(m => m.Role == MoveRole.Ultimate));
            Assert.Less(normal.ChargePuffsPerSecond, special.ChargePuffsPerSecond); Assert.LessOrEqual(special.ChargePuffsPerSecond, ult.ChargePuffsPerSecond);
            Assert.Less(normal.OrbitCount, special.OrbitCount); Assert.Less(special.OrbitCount, ult.OrbitCount + 1);
            Assert.Less(normal.AfterSeconds, special.AfterSeconds); Assert.GreaterOrEqual(ult.ChargeRings, special.ChargeRings);
        }

        [Test] public void EachSpeciesHasItsOwnColour()
        {
            var tints = Bosses.Select(b => VfxRules.PlanFor(b, MovesetSeed.For(b).First(m => m.Role == MoveRole.Normal)).Tint).Distinct().ToList();
            Assert.AreEqual(3, tints.Count);
        }

        [Test] public void TheChargeBuildsSlowlyThenFast_AndTheAftermathFadesAway()
        {
            Assert.AreEqual(0f, VfxRules.ChargeIntensity(0f), 1e-4f); Assert.AreEqual(1f, VfxRules.ChargeIntensity(1f), 1e-4f);
            Assert.Less(VfxRules.ChargeIntensity(0.5f), 0.4f);                               // still gathering at half way
            float last = 0f; for (float p = 0f; p <= 1.001f; p += 0.05f) { float v = VfxRules.ChargeIntensity(p); Assert.GreaterOrEqual(v, last - 1e-5f); last = v; }
            Assert.AreEqual(1f, VfxRules.AfterIntensity(0f, 2f), 1e-4f); Assert.AreEqual(0f, VfxRules.AfterIntensity(2f, 2f), 1e-4f); Assert.AreEqual(0f, VfxRules.AfterIntensity(5f, 2f));
            Assert.Less(VfxRules.AfterIntensity(1f, 2f), VfxRules.AfterIntensity(0.5f, 2f));
        }
    }
}
