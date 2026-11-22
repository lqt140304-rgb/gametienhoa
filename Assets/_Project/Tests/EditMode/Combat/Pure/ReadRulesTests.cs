using Evolve.Combat;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class ReadRulesTests
    {
        static PlayerReader Watch(float dist, float speed, float seconds)
        {
            var r = new PlayerReader();
            for (float t = 0f; t < seconds; t += 0.1f) r.Observe(0.1f, dist, speed);
            return r;
        }
        static MoveDef M(MoveRole role) => new MoveDef { Name = role.ToString(), Role = role };

        [Test] public void NothingIsAssumed_UntilItHasWatchedForAFewSeconds()
        {
            var r = Watch(3f, 1f, 1.5f);
            Assert.AreEqual(PlayerStyle.Unknown, r.Style);
            foreach (MoveRole role in System.Enum.GetValues(typeof(MoveRole))) Assert.AreEqual(1f, r.Weight(M(role)), 1e-4f);
        }

        [Test] public void AGluedPlayer_IsAHugger_AndTheBossLeansOnCloseAttacks()
        {
            var r = Watch(3f, 1f, 8f);
            Assert.AreEqual(PlayerStyle.Hugger, r.Style);
            Assert.Greater(r.Weight(M(MoveRole.Normal)), 1.4f); Assert.Less(r.Weight(M(MoveRole.Ranged)), 0.7f); Assert.Less(r.Weight(M(MoveRole.Special)), 1f);
        }

        [Test] public void AFarPlayer_IsACamper_AndTheBossLeansOnRangedAndSpecial()
        {
            var r = Watch(15f, 0.5f, 10f);
            Assert.AreEqual(PlayerStyle.Camper, r.Style);
            Assert.Greater(r.Weight(M(MoveRole.Ranged)), 1.4f); Assert.Greater(r.Weight(M(MoveRole.Special)), 1.3f); Assert.Less(r.Weight(M(MoveRole.Normal)), 0.5f);
        }

        [Test] public void AFastMovingPlayerAtMidRange_IsARunner_AndTheBossAnswersWithTheCharge()
        {
            var r = Watch(8f, 6f, 8f);
            Assert.AreEqual(PlayerStyle.Runner, r.Style);
            Assert.Greater(r.Weight(M(MoveRole.Combo)), 1.5f); Assert.Less(r.Weight(M(MoveRole.Ranged)), 1f);
        }

        [Test] public void TheReadingFades_WhenThePlayerChangesHabit()
        {
            var r = Watch(3f, 1f, 8f); Assert.AreEqual(PlayerStyle.Hugger, r.Style);
            for (float t = 0f; t < 15f; t += 0.1f) r.Observe(0.1f, 15f, 0.5f);
            Assert.AreEqual(PlayerStyle.Camper, r.Style);
        }

        [Test] public void ThreeDodgesToTheSameSide_ArePredictable_MixedOnesAreNot()
        {
            var r = new PlayerReader();
            r.NoteDodge(+3f); r.NoteDodge(+2f); Assert.AreEqual(0f, r.PredictedLateral());   // two is a coincidence
            r.NoteDodge(+4f); Assert.Greater(r.PredictedLateral(), 0.7f);
            var m = new PlayerReader(); m.NoteDodge(+3f); m.NoteDodge(-3f); m.NoteDodge(+3f); m.NoteDodge(-3f);
            Assert.AreEqual(0f, m.PredictedLateral());
            var left = new PlayerReader(); left.NoteDodge(-3f); left.NoteDodge(-3f); left.NoteDodge(-3f); Assert.Less(left.PredictedLateral(), -0.7f);
        }

        [Test] public void OnlyRealSidestepsCount_AndOnlyTheLastFewAreRemembered()
        {
            var r = new PlayerReader();
            r.NoteDodge(0.2f); r.NoteDodge(0.3f); r.NoteDodge(-0.1f); r.NoteDodge(0.4f);   // standing still is not a dodge
            Assert.AreEqual(0f, r.PredictedLateral());
            for (int i = 0; i < 4; i++) r.NoteDodge(+3f);
            r.NoteDodge(-3f); r.NoteDodge(-3f); r.NoteDodge(-3f); r.NoteDodge(-3f);          // the habit changed
            Assert.Less(r.PredictedLateral(), -0.7f);
        }

        [Test] public void LeadAimsAheadOfAMovingPlayer_MoreForARunner()
        {
            var still = Watch(8f, 0f, 8f); var run = Watch(8f, 6f, 8f);
            var a = still.Lead(5f, 0f, 1f); var b = run.Lead(5f, 0f, 1f);
            Assert.Greater(a.x, 0f); Assert.Greater(b.x, a.x); Assert.LessOrEqual(b.x, 5f);   // never more than the full distance the player will cover
            Assert.AreEqual(0f, run.Lead(0f, 0f, 1f).x, 1e-4f);
        }
    }
}
