using System.Collections.Generic;
using Evolve.Organs;
using Evolve.Traits;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class FlawCompensationTests
    {
        static readonly OrganSlot[] Legs = { OrganSlot.Legs };

        static List<(OrganSlot, float)> Worn(params (OrganSlot, float)[] items) => new List<(OrganSlot, float)>(items);

        [Test]
        public void NoOrgansGivesZero() =>
            Assert.AreEqual(0f, FlawCompensation.Fraction(Worn(), Legs), 1e-6f);

        [Test]
        public void OneMatchingOrganFullIntegrity() =>
            Assert.AreEqual(0.35f, FlawCompensation.Fraction(Worn((OrganSlot.Legs, 1f)), Legs), 1e-6f);

        [Test]
        public void IntegrityScalesContinuously() =>
            Assert.AreEqual(0.175f, FlawCompensation.Fraction(Worn((OrganSlot.Legs, 0.5f)), Legs), 1e-6f);

        [Test]
        public void TwoMatchingCapsAtSeventy()
        {
            Assert.AreEqual(0.7f, FlawCompensation.Fraction(Worn((OrganSlot.Legs, 1f), (OrganSlot.Legs, 1f)), Legs), 1e-6f);
            Assert.AreEqual(0.7f, FlawCompensation.Fraction(Worn((OrganSlot.Legs, 1f), (OrganSlot.Legs, 1f), (OrganSlot.Legs, 1f)), Legs), 1e-6f);
        }

        [Test]
        public void BadIntegrityIsClamped()
        {
            Assert.AreEqual(0f, FlawCompensation.Fraction(Worn((OrganSlot.Legs, -3f)), Legs), 1e-6f);
            Assert.AreEqual(0.35f, FlawCompensation.Fraction(Worn((OrganSlot.Legs, 5f)), Legs), 1e-6f);
            Assert.AreEqual(0f, FlawCompensation.Fraction(Worn((OrganSlot.Legs, float.NaN)), Legs), 1e-6f);
        }

        [Test]
        public void NonMatchingSlotIgnored() =>
            Assert.AreEqual(0f, FlawCompensation.Fraction(Worn((OrganSlot.Eyes, 1f)), Legs), 1e-6f);

        [Test]
        public void EmptyOrNullCompensatedByGivesZero()
        {
            Assert.AreEqual(0f, FlawCompensation.Fraction(Worn((OrganSlot.Legs, 1f)), new OrganSlot[0]), 1e-6f);
            Assert.AreEqual(0f, FlawCompensation.Fraction(Worn((OrganSlot.Legs, 1f)), null), 1e-6f);
            Assert.AreEqual(0f, FlawCompensation.Fraction(null, Legs), 1e-6f);
        }

        [Test]
        public void StrengthIsOneMinusFraction()
        {
            Assert.AreEqual(1f, FlawCompensation.Strength(0f), 1e-6f);
            Assert.AreEqual(0.65f, FlawCompensation.Strength(0.35f), 1e-6f);
            Assert.AreEqual(0.3f, FlawCompensation.Strength(0.7f), 1e-6f);
            Assert.AreEqual(0.3f, FlawCompensation.Strength(5f), 1e-6f);   // never below 30%
            Assert.AreEqual(1f, FlawCompensation.Strength(-1f), 1e-6f);
        }
    }
}
