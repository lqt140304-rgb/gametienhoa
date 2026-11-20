using System.Linq;
using Evolve.Creatures;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class PreyDefenseTests
    {
        static PreyDefenseDef Def(string id) => PreyDefenseSeed.Get(id);

        [Test] public void Table_Has21Species_UniqueIds_CoveringTheSpecPrey()
        {
            Assert.AreEqual(21, PreyDefenseSeed.All.Count);
            Assert.AreEqual(21, PreyDefenseSeed.All.Select(d => d.Id).Distinct().Count());
            var spec = new[] { "Bọ Cánh Cứng Nhỏ", "Ốc Hồ", "Đom Đóm Sắc Màu", "Thỏ Lá", "Sên Rêu", "Muỗi Rêu", "Chim Non", "Dơi Âm Thoa", "Bọ Gai Tím",
                "Nấm Đi Bộ", "Bướm Bào Tử", "Ốc Lưu Huỳnh", "Cua Đá Nóng", "Dế Gió", "Chuột Gió", "Giòi Trắng", "Kền Kền Hang", "Bọ Đá", "Tắc Kè Đá", "Bướm Đêm", "Rết Mù" };
            CollectionAssert.AreEquivalent(spec, PreyDefenseSeed.All.Select(d => d.Species).ToArray());
        }

        [Test] public void EveryPrey_HasAnEscape_AndANumbersMatchItsDifficulty()
        {
            foreach (var d in PreyDefenseSeed.All)
            {
                Assert.IsTrue(d.HasEscape, d.Id);
                Assert.That(d.Difficulty, Is.InRange(1, 3), d.Id);
                Assert.AreEqual(PreyDefenseSeed.EscapeChanceByDifficulty[d.Difficulty], d.EscapeChance, 1e-4f, d.Id);
                Assert.LessOrEqual(d.CounterDamage, PreyDefenseSeed.MaxCounterDamageByDifficulty[d.Difficulty], d.Id);
                Assert.GreaterOrEqual(d.EscapeCooldown, 5f, d.Id);   // no spamming
            }
        }

        [Test] public void OnlyTwoSpeciesAreDifficulty3()
        {
            CollectionAssert.AreEquivalent(new[] { "bo_gai_tim", "cua_da_nong" }, PreyDefenseSeed.All.Where(d => d.Difficulty == 3).Select(d => d.Id).ToArray());
        }

        [Test] public void ThreeSpecialMechanics_Present()
        {
            Assert.Greater(Def("de_gio").AlarmRadius, 0f);                                   // báo động lan truyền
            Assert.Greater(Def("chim_non").CallHelpRadius, 0f);                              // kêu gọi cứu viện
            Assert.AreEqual("Chim Hang Mẹ", Def("chim_non").HelperName);
            Assert.IsTrue(Def("tac_ke_da").Decoy); Assert.IsTrue(Def("tac_ke_da").EscapesGrab);   // mồi nhử
        }

        [Test] public void FindBySpecies_MatchesPrefabNames()
        {
            Assert.AreEqual("oc_ho", PreyDefenseSeed.FindBySpecies("Ốc Hồ (Clone)").Id);
            Assert.AreEqual("tho_la", PreyDefenseSeed.FindBySpecies(" thỏ lá ").Id);
            Assert.IsNull(PreyDefenseSeed.FindBySpecies("Cóc Hồ Già"));
            Assert.IsNull(PreyDefenseSeed.FindBySpecies(null));
        }

        [Test] public void NoCounter_ForTheSpeciesTheTableSaysSo()
        {
            foreach (var id in new[] { "dom_dom", "buom_bao_tu", "chim_non", "de_gio", "tac_ke_da", "buom_dem" }) Assert.IsFalse(Def(id).HasCounter, id);
        }

        [Test] public void FleeRange_GrowsWithThreatSize()
        {
            Assert.AreEqual(6f, PreyDefenseRules.FleeRange(6f, 1f), 1e-4f);
            Assert.AreEqual(9f, PreyDefenseRules.FleeRange(6f, 3f), 1e-4f);
            Assert.AreEqual(9f, PreyDefenseRules.FleeRange(6f, 10f), 1e-4f);
            Assert.AreEqual(6f, PreyDefenseRules.FleeRange(6f, 0.2f), 1e-4f);
        }

        [Test] public void CounterWillingness_FallsWithThreatSize()
        {
            Assert.AreEqual(1f, PreyDefenseRules.CounterWillingness(1f));
            Assert.AreEqual(1f, PreyDefenseRules.CounterWillingness(1.5f));
            Assert.AreEqual(0.5f, PreyDefenseRules.CounterWillingness(2.75f), 1e-4f);
            Assert.AreEqual(0f, PreyDefenseRules.CounterWillingness(4f));
        }

        [Test] public void EscapeChanceVs_BiggerThreatIsHarderToEscape()
        {
            Assert.AreEqual(0.4f, PreyDefenseRules.EscapeChanceVs(0.4f, 1f), 1e-4f);
            Assert.Less(PreyDefenseRules.EscapeChanceVs(0.4f, 3f), 0.4f);
            Assert.AreEqual(0.4f * 0.55f, PreyDefenseRules.EscapeChanceVs(0.4f, 20f), 1e-4f);
        }

        static DefenseInput In(float dist = 3f, float ratio = 1f) => new DefenseInput { Now = 100f, ThreatSeen = true, ThreatDistance = dist, ThreatRatio = ratio, Roll = 0f };

        [Test] public void Escape_OnlyWithinRange_AndWhenReady_AndNotWhileGuarding()
        {
            var d = Def("tho_la");   // alert 6
            Assert.IsTrue(PreyDefenseRules.Decide(d, In(5f)).HasFlag(DefenseAction.Escape));
            Assert.IsFalse(PreyDefenseRules.Decide(d, In(9f)).HasFlag(DefenseAction.Escape));
            var cooling = In(5f); cooling.EscapeReadyAt = 101f;
            Assert.IsFalse(PreyDefenseRules.Decide(d, cooling).HasFlag(DefenseAction.Escape));
            var guarding = In(5f); guarding.Guarding = true;
            Assert.IsFalse(PreyDefenseRules.Decide(d, guarding).HasFlag(DefenseAction.Escape));
            var noThreat = In(1f); noThreat.ThreatSeen = false;
            Assert.AreEqual(DefenseAction.None, PreyDefenseRules.Decide(d, noThreat));
        }

        [Test] public void Counter_WhenHitCorneredOrTired_NotOtherwise()
        {
            var d = Def("bo_gai_tim");
            Assert.IsFalse(PreyDefenseRules.Decide(d, In(5f)).HasFlag(DefenseAction.Counter));
            var hit = In(1f); hit.JustHit = true; Assert.IsTrue(PreyDefenseRules.Decide(d, hit).HasFlag(DefenseAction.Counter));
            var cornered = In(1f); cornered.Cornered = true; Assert.IsTrue(PreyDefenseRules.Decide(d, cornered).HasFlag(DefenseAction.Counter));
            var tired = In(1f); tired.Tired = true; Assert.IsTrue(PreyDefenseRules.Decide(d, tired).HasFlag(DefenseAction.Counter));   // no more helpless exhaustion
            var cooling = hit; cooling.CounterReadyAt = 101f; Assert.IsFalse(PreyDefenseRules.Decide(d, cooling).HasFlag(DefenseAction.Counter));
        }

        [Test] public void Counter_Refused_AgainstMuchBiggerThreat_AndWithoutACounter()
        {
            var thorny = Def("bo_gai_tim");
            var big = In(1f, 5f); big.JustHit = true; big.Roll = 0.0f;
            Assert.IsFalse(PreyDefenseRules.Decide(thorny, big).HasFlag(DefenseAction.Counter));
            var butterfly = Def("buom_bao_tu");
            var hit = In(1f); hit.JustHit = true;
            Assert.IsFalse(PreyDefenseRules.Decide(butterfly, hit).HasFlag(DefenseAction.Counter));
        }

        [Test] public void Alarm_Once_CallHelp_OnlyWhenHit()
        {
            var cricket = Def("de_gio");
            Assert.IsTrue(PreyDefenseRules.Decide(cricket, In(20f)).HasFlag(DefenseAction.Alarm));
            var sent = In(20f); sent.AlarmSent = true; Assert.IsFalse(PreyDefenseRules.Decide(cricket, sent).HasFlag(DefenseAction.Alarm));
            var chick = Def("chim_non");
            Assert.IsFalse(PreyDefenseRules.Decide(chick, In(2f)).HasFlag(DefenseAction.CallHelp));
            var hit = In(2f); hit.JustHit = true; Assert.IsTrue(PreyDefenseRules.Decide(chick, hit).HasFlag(DefenseAction.CallHelp));
            hit.CallSent = true; Assert.IsFalse(PreyDefenseRules.Decide(chick, hit).HasFlag(DefenseAction.CallHelp));
        }

        [Test] public void BiggerThreat_IsFledFromSooner()
        {
            var d = Def("tho_la");   // alert 6, 9 at ratio 3
            Assert.IsFalse(PreyDefenseRules.Decide(d, In(8f, 1f)).HasFlag(DefenseAction.Escape));
            Assert.IsTrue(PreyDefenseRules.Decide(d, In(8f, 3f)).HasFlag(DefenseAction.Escape));
        }
    }
}
