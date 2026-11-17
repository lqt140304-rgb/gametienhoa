using Evolve.Organs;
using NUnit.Framework;
using UnityEngine;

namespace Evolve.Tests.EditMode
{
    public class OrganTierTests
    {
        [Test]
        public void ExistingTierValuesUnchanged()
        {
            Assert.AreEqual(0, (int)OrganTier.C);
            Assert.AreEqual(1, (int)OrganTier.B);
            Assert.AreEqual(2, (int)OrganTier.A);
            Assert.AreEqual(3, (int)OrganTier.S);
        }

        [Test]
        public void TierDIsBelowC() => Assert.Less((int)OrganTier.D, (int)OrganTier.C);

        [Test]
        public void AbsorbExpD() => Assert.AreEqual(8f, OrganTiers.AbsorbExp(OrganTier.D));

        [Test]
        public void DropTableHasZeroDWeightByDefault()
        {
            var t = ScriptableObject.CreateInstance<DropTableSO>();
            Assert.AreEqual(0f, t.TierWeight(OrganTier.D));
            Object.DestroyImmediate(t);
        }
    }
}
