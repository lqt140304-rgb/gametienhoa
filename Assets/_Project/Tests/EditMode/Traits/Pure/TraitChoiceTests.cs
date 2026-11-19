using System.Collections.Generic;
using Evolve.Traits;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class TraitChoiceTests
    {
        [Test]
        public void ChoiceMustBeExactlyOneOfTheOptions()
        {
            var options = new List<string[]> { new[] { "a" }, new[] { "b" }, new[] { "c" } };
            Assert.IsTrue(TraitChoice.IsValid(options, new[] { "b" }));
            Assert.IsFalse(TraitChoice.IsValid(options, new[] { "x" }));
            Assert.IsFalse(TraitChoice.IsValid(options, new[] { "a", "b" }));
            Assert.IsFalse(TraitChoice.IsValid(options, new string[0]));
            Assert.IsFalse(TraitChoice.IsValid(null, new[] { "a" }));
            var heavy = new List<string[]> { new[] { "a", "b" }, new[] { "c", "d" } };
            Assert.IsTrue(TraitChoice.IsValid(heavy, new[] { "b", "a" }));    // order does not matter
            Assert.IsFalse(TraitChoice.IsValid(heavy, new[] { "a", "c" }));   // half of one option, half of another
            Assert.IsFalse(TraitChoice.IsValid(heavy, new[] { "a", "a" }));
        }

        static List<FateCard> Pool() => new List<FateCard>
        {
            new FateCard("core.x", TraitRarity.Common, new[] { new[] { "f.a" }, new[] { "f.b" }, new[] { "f.c" } }),
            new FateCard("core.heavy", TraitRarity.Mythic, new[] { new[] { "f.a", "f.b" }, new[] { "f.c", "f.d" }, new[] { "f.e", "f.f" } }),
        };

        [Test]
        public void Pick_KnownCoreAndItsOption_Accepted()
        {
            Assert.IsTrue(TraitChoice.IsKnownPick(Pool(), "core.x", new[] { "f.b" }));
            Assert.IsTrue(TraitChoice.IsKnownPick(Pool(), "core.heavy", new[] { "f.d", "f.c" }));
        }

        [Test]
        public void Pick_UnknownCore_Rejected() => Assert.IsFalse(TraitChoice.IsKnownPick(Pool(), "core.nope", new[] { "f.a" }));

        [Test]
        public void Pick_FlawOfAnotherCore_Rejected() => Assert.IsFalse(TraitChoice.IsKnownPick(Pool(), "core.x", new[] { "f.e" }));

        [Test]
        public void Pick_EmptyOrNull_Rejected()
        {
            Assert.IsFalse(TraitChoice.IsKnownPick(Pool(), "core.x", new string[0]));
            Assert.IsFalse(TraitChoice.IsKnownPick(Pool(), null, new[] { "f.a" }));
            Assert.IsFalse(TraitChoice.IsKnownPick(null, "core.x", new[] { "f.a" }));
            Assert.IsFalse(TraitChoice.IsKnownPick(new List<FateCard>(), "core.x", new[] { "f.a" }));
        }
    }
}
