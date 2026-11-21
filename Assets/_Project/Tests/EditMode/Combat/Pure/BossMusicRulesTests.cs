using Evolve.Combat;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class BossMusicRulesTests
    {
        static void Run(BossMusicRules r, float seconds, float step = 0.1f) { for (float t = 0f; t < seconds; t += step) r.Tick(step); }

        [Test] public void Silent_UntilTheFightStarts()
        {
            var r = new BossMusicRules(); Run(r, 5f);
            Assert.AreEqual(0f, r.Volume); Assert.IsFalse(r.Playing);
        }

        [Test] public void AFightEvent_FadesTheMusicInQuickly()
        {
            var r = new BossMusicRules(); r.Ping(); Run(r, 0.3f);
            Assert.Greater(r.Volume, 0.3f); Assert.IsTrue(r.Playing);
            Run(r, 1f); Assert.AreEqual(1f, r.Volume, 1e-3f);
        }

        [Test] public void KeepsPlayingWhileTheFightGoesOn_AndFadesAfterTenQuietSeconds()
        {
            var r = new BossMusicRules(); r.Ping(); Run(r, 1f);
            for (int i = 0; i < 8; i++) { Run(r, 5f); r.Ping(); }                       // a blow every five seconds for 40 s
            Assert.AreEqual(1f, r.Volume, 1e-3f);
            Run(r, BossMusicRules.QuietSeconds - 0.2f); Assert.AreEqual(1f, r.Volume, 1e-3f);   // ten seconds are not up yet
            Run(r, 0.4f + BossMusicRules.FadeSeconds * 0.5f);
            Assert.Greater(r.Volume, 0f); Assert.Less(r.Volume, 1f);                     // halfway down
            Run(r, BossMusicRules.FadeSeconds); Assert.AreEqual(0f, r.Volume); Assert.IsFalse(r.Playing);
        }

        [Test] public void AnotherBlow_DuringTheFade_BringsTheMusicBack()
        {
            var r = new BossMusicRules(); r.Ping(); Run(r, 1f);
            Run(r, BossMusicRules.QuietSeconds + BossMusicRules.FadeSeconds * 0.6f);
            float low = r.Volume; Assert.Less(low, 0.6f);
            r.Ping(); Run(r, 0.5f); Assert.Greater(r.Volume, low);
            Run(r, 2f); Assert.AreEqual(1f, r.Volume, 1e-3f);
            Run(r, BossMusicRules.QuietSeconds - 2.6f); Assert.AreEqual(1f, r.Volume, 1e-3f);   // the quiet clock started again (2.5 s have passed since the blow)
        }

        [Test] public void Silence_FadesOutAtOnce_AndStaysOffUntilTheNextFight()
        {
            var r = new BossMusicRules(); r.Ping(); Run(r, 1f);
            r.Silence(1f); Run(r, 1.2f);
            Assert.AreEqual(0f, r.Volume);
            r.Ping(); Run(r, 1.5f); Assert.AreEqual(1f, r.Volume, 1e-3f);
        }

        [Test] public void VolumeNeverLeavesZeroToOne()
        {
            var r = new BossMusicRules();
            for (int i = 0; i < 500; i++) { if (i % 37 == 0) r.Ping(); if (i == 300) r.Silence(0.5f); r.Tick(0.1f); Assert.That(r.Volume, Is.InRange(0f, 1f)); }
        }
    }
}
