using System.Linq;
using Evolve.Traits;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class SlamPlanTests
    {
        static readonly string[] Gifts = { "Túi Không Đáy", "Nuốt Chửng", "Giải Mã", "Biến Hình", "Sóng Dò", "Phân Thân" };

        static System.Collections.Generic.List<SlamPlan.Slam> Plan(TraitRarity r = TraitRarity.Common, params string[] flaws) =>
            SlamPlan.Build("Nhân Quen Đất", r, flaws.Length > 0 ? flaws : new[] { "Đói Sớm" }, Gifts);

        [Test]
        public void Order_CoreFlawGifts()
        {
            var kinds = Plan().Select(s => s.Kind).ToArray();
            CollectionAssert.AreEqual(new[] { SlamKind.Core, SlamKind.Flaw, SlamKind.Gift, SlamKind.Gift, SlamKind.Gift, SlamKind.Gift, SlamKind.Gift, SlamKind.Gift }, kinds);
            Assert.AreEqual("Nhân Quen Đất", Plan()[0].Text);
            Assert.AreEqual("Phân Thân", Plan().Last().Text);
        }

        [Test]
        public void Angles_AsDesigned()
        {
            var plan = Plan(TraitRarity.Common, "Đói Sớm", "Lạ Vùng");
            Assert.AreEqual(-7f, plan[0].Angle);
            Assert.AreEqual(6f, plan[1].Angle);
            Assert.AreEqual(-5f, plan[2].Angle, "a second flaw tilts the other way");
            var gifts = plan.Where(s => s.Kind == SlamKind.Gift).ToList();
            for (int i = 0; i < gifts.Count; i++)
            {
                Assert.That(System.Math.Abs(gifts[i].Angle), Is.InRange(4f, 9f));
                if (i > 0) Assert.AreNotEqual(System.Math.Sign(gifts[i].Angle), System.Math.Sign(gifts[i - 1].Angle), "gifts alternate left and right");
            }
        }

        [Test]
        public void Mythic_ShakesDouble() =>
            Assert.AreEqual(2f * Plan(TraitRarity.Common)[0].Shake, Plan(TraitRarity.Mythic)[0].Shake, 1e-5f);

        [Test]
        public void Shake_GiftsSmallerThanCore()
        {
            var plan = Plan();
            foreach (var g in plan.Where(s => s.Kind == SlamKind.Gift)) Assert.Less(g.Shake, plan[0].Shake);
        }

        [Test]
        public void Plan_TwoFlaws_BothSlam() =>
            Assert.AreEqual(2, Plan(TraitRarity.Common, "Đói Sớm", "Lạ Vùng").Count(s => s.Kind == SlamKind.Flaw));

        [Test]
        public void Times_Increase_AndTotalCoversTheLastSlam()
        {
            var plan = Plan(TraitRarity.Rare, "A", "B");
            for (int i = 1; i < plan.Count; i++) Assert.Greater(plan[i].At, plan[i - 1].At);
            Assert.Greater(SlamPlan.TotalSeconds(plan), plan.Last().At);
            Assert.AreEqual(0f, SlamPlan.TotalSeconds(new System.Collections.Generic.List<SlamPlan.Slam>()));
        }

        [Test]
        public void NullFlawsOrGifts_DoNotThrow()
        {
            var plan = SlamPlan.Build("X", TraitRarity.Common, null, null);
            Assert.AreEqual(1, plan.Count);
        }
    }
}
