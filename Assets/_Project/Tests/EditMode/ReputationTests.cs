using System.Collections.Generic;
using Evolve.Civilization;
using NUnit.Framework;
using UnityEngine;

namespace Evolve.Tests.EditMode
{
    public class ReputationTests
    {
        [Test] public void StandingsFollowTheThresholds()
        {
            Assert.AreEqual(Standing.Hostile, ReputationRules.StandingOf(-50f));
            Assert.AreEqual(Standing.Cold, ReputationRules.StandingOf(-49.9f));
            Assert.AreEqual(Standing.Cold, ReputationRules.StandingOf(-10.1f));
            Assert.AreEqual(Standing.Neutral, ReputationRules.StandingOf(-10f));
            Assert.AreEqual(Standing.Neutral, ReputationRules.StandingOf(0f));
            Assert.AreEqual(Standing.Friendly, ReputationRules.StandingOf(30f));
            Assert.AreEqual(Standing.Member, ReputationRules.StandingOf(70f));
        }

        [Test] public void HittingCostsTheAttackerAndAQuarterForCompanions()
        {
            var ledger = new TribeLedger();
            object me = "me", friend = "friend";
            ledger.Offend(me, ReputationRules.HitPenalty, new List<object> { friend, me });
            Assert.AreEqual(-15f, ledger.Get(me), 1e-4f);
            Assert.AreEqual(-3.75f, ledger.Get(friend), 1e-4f);
            for (int i = 0; i < 5; i++) ledger.Offend(me, ReputationRules.KillPenalty, null);
            Assert.AreEqual(-100f, ledger.Get(me), 1e-4f);
            Assert.AreEqual(Standing.Hostile, ledger.StandingOf(me));
        }

        [Test] public void OfferingsAtoneOnlyUpToCold()
        {
            var ledger = new TribeLedger();
            object me = "me";
            ledger.Offend(me, -80f, null);
            Assert.IsTrue(ledger.Offer(me));
            Assert.AreEqual(-68f, ledger.Get(me), 1e-4f);
            for (int i = 0; i < 20; i++) ledger.Offer(me);
            Assert.AreEqual(ReputationRules.AtonementCap, ledger.Get(me), 1e-4f);
            Assert.IsFalse(ledger.Offer(me), "no farming reputation upward");
            Assert.IsFalse(ledger.Offer("stranger"), "a neutral player gains nothing");
            Assert.AreEqual(0f, ledger.Get("stranger"));
        }
    }
}
