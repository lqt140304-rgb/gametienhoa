using System.Linq;
using Evolve.Traits;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class TraitCatalogTests
    {
        [Test]
        public void FateCards_AlwaysAvailable_FromAssetsOrTheSeed()
        {
            var cards = TraitCatalog.FateCards();
            Assert.GreaterOrEqual(cards.Count, 1);
            Assert.IsTrue(cards.All(c => c.Options != null && c.Options.Length == 3), "every card has its three flaw options");
            if (TraitCatalog.Cores.Count == 0) Assert.AreEqual(12, cards.Count);   // no assets made yet: the built-in table
        }

        [Test]
        public void FateCards_UseAssetRarity_WhenAssetsExist()
        {
            if (TraitCatalog.Cores.Count == 0) Assert.Ignore("Run Evolve → Create Traits first.");
            var byId = TraitCatalog.FateCards().ToDictionary(c => c.Id);
            foreach (var core in TraitCatalog.Cores) Assert.AreEqual(core.Rarity, byId[core.Id].Rarity, core.Id);
        }
    }
}
