using Evolve.World;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class PropNamingTests
    {
        [Test]
        public void IsCollider()
        {
            Assert.IsTrue(PropNaming.IsCollider("Rock_0_COL"));
            Assert.IsFalse(PropNaming.IsCollider("Rock_0"));
        }

        [Test]
        public void IsLod()
        {
            Assert.IsTrue(PropNaming.IsLod("Rock_0_LOD1", out int l));
            Assert.AreEqual(1, l);
            Assert.IsTrue(PropNaming.IsLod("Rock_0_LOD2", out l));
            Assert.AreEqual(2, l);
            Assert.IsFalse(PropNaming.IsLod("Rock_0", out _));
            Assert.IsFalse(PropNaming.IsLod("Rock_0_LODx", out _));
        }

        [Test]
        public void BaseName()
        {
            Assert.AreEqual("Rock_0", PropNaming.BaseName("Rock_0_COL"));
            Assert.AreEqual("Rock_0", PropNaming.BaseName("Rock_0_LOD2"));
            Assert.AreEqual("Rock_0", PropNaming.BaseName("Rock_0"));
        }

        [Test]
        public void NullAndEmptyDoNotThrow()
        {
            Assert.IsFalse(PropNaming.IsCollider(null));
            Assert.IsFalse(PropNaming.IsLod("", out _));
            Assert.AreEqual("", PropNaming.BaseName(null));
        }
    }
}
