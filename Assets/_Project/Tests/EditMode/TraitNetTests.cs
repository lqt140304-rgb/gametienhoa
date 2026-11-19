using Evolve.Traits;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class TraitNetTests
    {
        [Test]
        public void HandEncodingRoundTrip()
        {
            foreach (var hand in new[] { new string[0], new[] { "core.a" }, new[] { "core.a", "core.b", "core.c" }, new[] { "core.màng_thấm", "core.đất" } })
                CollectionAssert.AreEqual(hand, TraitNet.DecodeHand(TraitNet.EncodeHand(hand)));
        }

        [Test]
        public void DecodeGarbageReturnsEmpty()
        {
            Assert.AreEqual(0, TraitNet.DecodeHand(null).Length);
            Assert.AreEqual(0, TraitNet.DecodeHand("").Length);
            Assert.AreEqual(0, TraitNet.DecodeHand("   \n \n").Length);
            Assert.AreEqual(0, TraitNet.EncodeHand(null).Length);
        }

        [Test]
        public void PickRoundTripAndRejectsTooShort()
        {
            Assert.IsTrue(TraitNet.DecodePick(TraitNet.EncodePick("core.x", new[] { "flaw.a", "flaw.b" }), out var core, out var flaws));
            Assert.AreEqual("core.x", core);
            CollectionAssert.AreEqual(new[] { "flaw.a", "flaw.b" }, flaws);
            Assert.IsFalse(TraitNet.DecodePick("core.x", out _, out _));   // no flaw
            Assert.IsFalse(TraitNet.DecodePick(null, out _, out _));
        }
    }
}
