using System;
using System.IO;
using System.Linq;
using Evolve.Traits;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class BirthTests
    {
        static BirthFlow Flow() => new BirthFlow(BirthScript.BuildSteps());

        [Test]
        public void Steps_AreInTheDesignedOrder()
        {
            var kinds = BirthScript.BuildSteps().Select(s => s.Kind).ToArray();
            var expect = new[]
            {
                BirthStepKind.Boot, BirthStepKind.Line, BirthStepKind.Line, BirthStepKind.Line,
                BirthStepKind.Gift, BirthStepKind.Gift, BirthStepKind.Gift, BirthStepKind.Gift, BirthStepKind.Gift, BirthStepKind.Gift,
                BirthStepKind.Line, BirthStepKind.DealCore, BirthStepKind.PickCore, BirthStepKind.RevealCore,
                BirthStepKind.DealFlaw, BirthStepKind.PickFlaw, BirthStepKind.RevealFlaw, BirthStepKind.Summary
            };
            CollectionAssert.AreEqual(expect, kinds);
        }

        [Test]
        public void GiftSteps_FollowTheSixGifts()
        {
            var gifts = BirthScript.BuildSteps().Where(s => s.Kind == BirthStepKind.Gift).ToList();
            Assert.AreEqual(6, gifts.Count);
            for (int i = 0; i < 6; i++)
            {
                Assert.AreEqual(i, gifts[i].GiftIndex);
                Assert.AreEqual(BirthScript.Gifts[i].Id, gifts[i].VoiceId);
                StringAssert.Contains(BirthScript.Gifts[i].Name, gifts[i].Text);
            }
        }

        [Test]
        public void Gifts_SixUniqueWithText()
        {
            Assert.AreEqual(6, BirthScript.Gifts.Count);
            Assert.AreEqual(6, BirthScript.Gifts.Select(g => g.Id).Distinct().Count());
            CollectionAssert.AreEqual(new[] { "Túi Không Đáy", "Nuốt Chửng", "Giải Mã", "Biến Hình", "Sóng Dò", "Phân Thân" }, BirthScript.Gifts.Select(g => g.Name));
            Assert.IsFalse(BirthScript.Gifts.First(g => g.Name == "Phân Thân").Implemented);
        }

        [Test]
        public void EveryTimedStepHasTimeToRead()
        {
            foreach (var s in BirthScript.BuildSteps())
                if (!s.Interactive) Assert.GreaterOrEqual(s.MinSeconds, 3f, s.Kind + " " + s.VoiceId);
        }

        // ---------------------------------------------------------------- the flow

        [Test]
        public void Timed_Step_Advances_AfterMinSecondsAndSilence()
        {
            var f = Flow();
            f.Tick(1f, false);
            Assert.AreEqual(0, f.Index);
            f.Tick(f.Current.MinSeconds, false);
            Assert.AreEqual(1, f.Index, "after its minimum time and a silent gap the next step starts");
            Assert.AreEqual(0f, f.StepTime, 1e-5f);
        }

        [Test]
        public void VoiceBusy_HoldsTheStep()
        {
            var f = Flow();
            f.Tick(f.Current.MinSeconds + 5f, true);   // the voice is still talking
            Assert.AreEqual(0, f.Index);
            f.Tick(BirthFlow.GapAfterVoice + 0.01f, false);
            Assert.AreEqual(1, f.Index);
        }

        [Test]
        public void SilenceGap_RestartsWhenTheVoiceTalksAgain()
        {
            var f = Flow();
            f.Tick(f.Current.MinSeconds, true);
            f.Tick(BirthFlow.GapAfterVoice * 0.6f, false);
            f.Tick(0.05f, true);                        // the voice came back
            f.Tick(BirthFlow.GapAfterVoice * 0.6f, false);
            Assert.AreEqual(0, f.Index, "the gap must be silent for its full length");
        }

        static BirthFlow FlowAt(BirthStepKind kind)
        {
            var f = Flow();
            while (!f.Finished && f.Current.Kind != kind) f.Tick(100f, false);
            return f;
        }

        [Test]
        public void PickSteps_WaitForever_AndAdvanceOnPick()
        {
            var f = FlowAt(BirthStepKind.PickCore);
            Assert.IsTrue(f.Current.Interactive);
            int at = f.Index;
            f.Tick(10000f, false);
            Assert.AreEqual(at, f.Index);
            Assert.IsFalse(f.CanSkip);
            Assert.IsTrue(f.Pick());
            Assert.AreEqual(BirthStepKind.RevealCore, f.Current.Kind);
        }

        [Test]
        public void Pick_OutsideAPickStep_DoesNothing()
        {
            var f = Flow();
            Assert.IsFalse(f.Pick());
            Assert.AreEqual(0, f.Index);
        }

        [Test]
        public void Skip_Rules()
        {
            var f = Flow();
            f.Tick(1f, true);
            f.Skip();
            Assert.AreEqual(0, f.Index, "too early to skip");
            f.Tick(BirthFlow.SkipAfter, true);
            Assert.IsTrue(f.CanSkip);
            f.Skip();
            Assert.AreEqual(1, f.Index, "skipping cuts the line and its voice");
        }

        [Test]
        public void SkipIntro_JumpsToTheFirstDeal_OnlyBeforeIt()
        {
            var f = Flow();
            f.Tick(BirthFlow.SkipAfter, true);
            f.SkipIntro();
            Assert.AreEqual(BirthStepKind.DealCore, f.Current.Kind);
            int at = f.Index;
            f.SkipIntro();
            Assert.AreEqual(at, f.Index, "after the intro it does nothing");
        }

        [Test]
        public void Flow_FinishesAfterTheSummary()
        {
            var f = Flow();
            int guard = 0;
            while (!f.Finished && guard++ < 100) { if (f.Current.Interactive) f.Pick(); else f.Tick(100f, false); }
            Assert.IsTrue(f.Finished);
            Assert.IsNull(f.Current);
            f.Tick(1f, false);   // harmless after the end
            Assert.IsFalse(f.Pick());
        }

        // ---------------------------------------------------------------- the words

        [Test]
        public void AllVoiceLines_UniqueIds_CoverSteps_CoresAndFlaws()
        {
            var lines = BirthScript.AllVoiceLines(TraitSeed.Cores, TraitSeed.Flaws);
            var ids = lines.Select(l => l.id).ToList();
            Assert.AreEqual(ids.Count, ids.Distinct().Count());
            Assert.AreEqual(13 + 12 + 12, lines.Count);
            foreach (var step in BirthScript.BuildSteps().Where(s => !string.IsNullOrEmpty(s.VoiceId))) CollectionAssert.Contains(ids, step.VoiceId);
            Assert.AreEqual("Phát hiện Lõi độc nhất: Nhân Quen Đất.", lines.First(l => l.id == "core.quen_dat").text);
            Assert.AreEqual("Phát hiện khiếm khuyết: Đói Sớm.", lines.First(l => l.id == "flaw.doi_som").text);
            Assert.IsTrue(lines.All(l => !string.IsNullOrWhiteSpace(l.text)));
        }

        [Test]
        public void SummaryLine_ContainsCoreAndFlaws()
        {
            string line = BirthScript.SummaryLine("Nhân Quen Đất", new[] { "Đói Sớm", "Lạ Vùng" });
            StringAssert.Contains("Nhân Quen Đất", line);
            StringAssert.Contains("Đói Sớm + Lạ Vùng", line);
            StringAssert.Contains("Nhân Quen Đất", BirthScript.SummaryLine("Nhân Quen Đất", null));
        }

        /// <summary>
        /// Not a check: with EXPORT_VOICE_LINES=&lt;file&gt; it writes the voice line list (id, tab, text) that tools/voice/generate_echo_voice.sh reads,
        /// so the words live in one place (BirthScript). Without the variable it does nothing.
        /// </summary>
        [Test]
        public void ExportVoiceLines()
        {
            var path = Environment.GetEnvironmentVariable("EXPORT_VOICE_LINES");
            if (string.IsNullOrEmpty(path)) Assert.Pass("EXPORT_VOICE_LINES not set");
            File.WriteAllLines(path, BirthScript.AllVoiceLines(TraitSeed.Cores, TraitSeed.Flaws).Select(l => l.id + "\t" + l.text));
        }
    }
}
