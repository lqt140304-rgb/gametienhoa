using Evolve.Player;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class BodySizeTests
    {
        [Test] public void StartMassIsUnit() => Assert.AreEqual(1f, BodySize.ScaleForMass(10f), 1e-4f);
        [Test] public void FloorAt0_6() => Assert.AreEqual(0.6f, BodySize.ScaleForMass(0f), 1e-4f);
        [Test] public void CappedAt2() => Assert.AreEqual(2f, BodySize.ScaleForMass(1000f), 1e-4f);
    }
}
