using System.Linq;
using Evolve.Creatures;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class ColonyRulesTests
    {
        static int Count(int n, ColonyRole r) => Enumerable.Range(0, n).Count(i => ColonyRules.RoleOf(i, n) == r);

        [Test] public void SmallGroup_AllForage() => Assert.AreEqual(4, Count(4, ColonyRole.Forager));

        [Test] public void Nest_SplitsIntoPairsScoutsKeepers()
        {
            Assert.AreEqual(4, Count(6, ColonyRole.Forager)); Assert.AreEqual(1, Count(6, ColonyRole.Scout)); Assert.AreEqual(1, Count(6, ColonyRole.Keeper));
            Assert.AreEqual(4, Count(8, ColonyRole.Forager)); Assert.AreEqual(2, Count(8, ColonyRole.Scout)); Assert.AreEqual(2, Count(8, ColonyRole.Keeper));
            Assert.AreEqual(6, Count(14, ColonyRole.Forager)); Assert.AreEqual(2, Count(14, ColonyRole.Scout)); Assert.AreEqual(6, Count(14, ColonyRole.Keeper));
        }

        [Test] public void Partners_ShareAHeading_ThatChangesOverTime()
        {
            Assert.AreEqual(ColonyRules.PairOf(0), ColonyRules.PairOf(1));
            Assert.AreNotEqual(ColonyRules.PairOf(1), ColonyRules.PairOf(2));
            Assert.AreEqual(ColonyRules.ForageAngle(1, 3f), ColonyRules.ForageAngle(1, 17f));
            Assert.AreNotEqual(ColonyRules.ForageAngle(0, 5f), ColonyRules.ForageAngle(1, 5f));
            Assert.AreNotEqual(ColonyRules.ForageAngle(0, 5f), ColonyRules.ForageAngle(0, 25f));
            float a = ColonyRules.ForageAngle(2, 99f);
            Assert.GreaterOrEqual(a, 0f); Assert.Less(a, 6.2832f);
        }

        [Test] public void Scouts_SpreadRoundTheNest()
        {
            Assert.AreEqual(System.Math.PI, ColonyRules.ScoutAngle(1, 2, 0f) - ColonyRules.ScoutAngle(0, 2, 0f), 1e-4);
            Assert.AreEqual(ColonyRules.ScoutAngle(0, 1, 0f), ColonyRules.ScoutAngle(0, 1, 60f), 1e-3);
        }
    }
}
