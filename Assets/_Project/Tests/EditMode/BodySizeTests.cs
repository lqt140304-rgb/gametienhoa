using Evolve.Player;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class BodySizeTests
    {
        [Test] public void ZeroOrgansIsUnit() => Assert.AreEqual(1f, BodySize.ScaleFor(0), 1e-4f);
        [Test] public void FiveOrgans() => Assert.AreEqual(1.3f, BodySize.ScaleFor(5), 1e-4f);
        [Test] public void CappedAt1_5() => Assert.AreEqual(1.5f, BodySize.ScaleFor(100), 1e-4f);
    }
}
