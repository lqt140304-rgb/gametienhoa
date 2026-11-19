using System.Linq;
using Evolve.Traits;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class BirthTests
    {
        [Test]
        public void PhaseAt_Boundaries()
        {
            Assert.AreEqual(BirthPhase.Drop, BirthTimeline.PhaseAt(0f));
            Assert.AreEqual(BirthPhase.Drop, BirthTimeline.PhaseAt(1.99f));
            Assert.AreEqual(BirthPhase.Echo, BirthTimeline.PhaseAt(2f));
            Assert.AreEqual(BirthPhase.DealCore, BirthTimeline.PhaseAt(7f));
            Assert.AreEqual(BirthPhase.DealFlaw, BirthTimeline.PhaseAt(14f));
            Assert.AreEqual(BirthPhase.Summary, BirthTimeline.PhaseAt(19f));
            Assert.AreEqual(BirthPhase.Done, BirthTimeline.PhaseAt(22f));
            Assert.AreEqual(BirthPhase.Drop, BirthTimeline.PhaseAt(-1f));
        }

        [Test]
        public void Total_IsSumOfPhases() =>
            Assert.AreEqual(BirthTimeline.DropSeconds + BirthTimeline.EchoSeconds + BirthTimeline.DealCoreSeconds + BirthTimeline.DealFlawSeconds + BirthTimeline.SummarySeconds, BirthTimeline.Total, 1e-5f);

        [Test] public void Total_Is22() => Assert.AreEqual(22f, BirthTimeline.Total, 1e-5f);

        [Test]
        public void PhaseFraction_RunsZeroToOneInsideAPhase()
        {
            Assert.AreEqual(0f, BirthTimeline.PhaseFraction(7f), 1e-5f);
            Assert.AreEqual(0.5f, BirthTimeline.PhaseFraction(10.5f), 1e-5f);
            Assert.AreEqual(1f, BirthTimeline.PhaseFraction(30f), 1e-5f);   // Done
        }

        [Test] public void Skip_Before3_DoesNothing() => Assert.AreEqual(1.5f, BirthTimeline.Skip(1.5f));
        [Test] public void Skip_DuringDeal_JumpsToSummary() => Assert.AreEqual(19f, BirthTimeline.Skip(9f), 1e-5f);
        [Test] public void Skip_InSummary_DoesNothing() => Assert.AreEqual(20f, BirthTimeline.Skip(20f));
        [Test] public void Skip_At3_Works() => Assert.AreEqual(19f, BirthTimeline.Skip(3f), 1e-5f);

        [Test]
        public void DealProgress_StaggeredAndClamped()
        {
            Assert.AreEqual(0f, BirthTimeline.DealProgress(0f, 0, 5), 1e-5f);
            Assert.AreEqual(1f, BirthTimeline.DealProgress(0.4f, 4, 5), 1e-5f);     // the last card is dealt by 40% of the phase
            Assert.AreEqual(1f, BirthTimeline.DealProgress(1f, 0, 5), 1e-5f);
            Assert.Less(BirthTimeline.DealProgress(0.1f, 4, 5), BirthTimeline.DealProgress(0.1f, 0, 5));   // later cards start later
            Assert.AreEqual(0f, BirthTimeline.DealProgress(0.5f, 0, 0), 1e-5f);
        }

        [Test]
        public void FlipProgress_ZeroThenOne()
        {
            Assert.AreEqual(0f, BirthTimeline.FlipProgress(0.6f), 1e-5f);
            Assert.AreEqual(1f, BirthTimeline.FlipProgress(0.9f), 1e-5f);
            Assert.AreEqual(0.5f, BirthTimeline.FlipProgress(0.775f), 1e-4f);
            Assert.AreEqual(1f, BirthTimeline.FlipProgress(1f), 1e-5f);
        }

        [Test]
        public void ShouldRun_OnlyWithoutCoreAndNotYetRun()
        {
            Assert.IsTrue(BirthTimeline.ShouldRun(false, false));
            Assert.IsFalse(BirthTimeline.ShouldRun(true, false));
            Assert.IsFalse(BirthTimeline.ShouldRun(false, true));   // ran this run already (skipped or finished): never again on respawn
            Assert.IsFalse(BirthTimeline.ShouldRun(true, true));
        }

        [Test]
        public void Gifts_SixUniqueWithText()
        {
            Assert.AreEqual(6, BirthScript.Gifts.Count);
            Assert.AreEqual(6, BirthScript.Gifts.Select(g => g.Id).Distinct().Count());
            Assert.IsTrue(BirthScript.Gifts.All(g => !string.IsNullOrEmpty(g.Name) && !string.IsNullOrEmpty(g.Line)));
            CollectionAssert.AreEqual(new[] { "Túi Không Đáy", "Nuốt Chửng", "Giải Mã", "Biến Hình", "Sóng Dò", "Phân Thân" }, BirthScript.Gifts.Select(g => g.Name));
            Assert.IsFalse(BirthScript.Gifts.First(g => g.Name == "Phân Thân").Implemented, "Phân Thân is not built yet");
        }

        [Test]
        public void EchoLines_IdsUniqueAndKnown()
        {
            var ids = BirthScript.EchoLines.Select(l => l.id).ToList();
            Assert.AreEqual(ids.Count, ids.Distinct().Count());
            foreach (var need in new[] { "echo.born", "echo.gift", "echo.core", "echo.flaw", "echo.done" }) CollectionAssert.Contains(ids, need);
            Assert.IsTrue(BirthScript.EchoLines.All(l => !string.IsNullOrEmpty(l.text)));
        }

        [Test]
        public void SummaryLine_ContainsCoreAndFlaws()
        {
            string line = BirthScript.SummaryLine("Nhân Quen Đất", new[] { "Đói Sớm", "Lạ Vùng" });
            StringAssert.Contains("Nhân Quen Đất", line);
            StringAssert.Contains("Đói Sớm + Lạ Vùng", line);
            StringAssert.Contains("Nhân Quen Đất", BirthScript.SummaryLine("Nhân Quen Đất", null));
        }
    }
}
