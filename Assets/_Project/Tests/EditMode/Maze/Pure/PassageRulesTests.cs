using Evolve.Maze;
using NUnit.Framework;

namespace Evolve.Tests.EditMode.Maze
{
    public class PassageRulesTests
    {
        [Test]
        public void SmallTunnel_OnlyAllowsSmallSlime()
        {
            Assert.IsTrue(PassageRules.CanFitTunnelSize(TunnelSize.Small, 0.85f));
            Assert.IsTrue(PassageRules.CanFitTunnelSize(TunnelSize.Small, PassageRules.SmallTunnelMaxScale));
            Assert.IsFalse(PassageRules.CanFitTunnelSize(TunnelSize.Small, 1.0f));
            Assert.IsTrue(PassageRules.CanFitTunnelSize(TunnelSize.Medium, 1.0f));
            Assert.IsFalse(PassageRules.CanFitTunnelSize(TunnelSize.Medium, 1.6f));
            Assert.IsTrue(PassageRules.CanFitTunnelSize(TunnelSize.Large, 2.0f));
        }

        [Test]
        public void Obstacles_RequireMatchingCapability()
        {
            var none = new SlimeCapabilities(0.8f);
            Assert.IsTrue(PassageRules.CanPassObstacle(ObstacleKind.None, none));
            Assert.IsFalse(PassageRules.CanPassObstacle(ObstacleKind.Flooded, none));
            Assert.IsFalse(PassageRules.CanPassObstacle(ObstacleKind.HighWall, none));
            Assert.IsFalse(PassageRules.CanPassObstacle(ObstacleKind.CrumblingRock, none));
            Assert.IsFalse(PassageRules.CanPassObstacle(ObstacleKind.HotDry, none));
            Assert.IsFalse(PassageRules.CanPassObstacle(ObstacleKind.PitchDark, none));

            var full = new SlimeCapabilities(0.8f, true, true, true, true, true);
            Assert.IsTrue(PassageRules.CanPassObstacle(ObstacleKind.Flooded, full));
            Assert.IsTrue(PassageRules.CanPassObstacle(ObstacleKind.HighWall, full));
            Assert.IsTrue(PassageRules.CanPassObstacle(ObstacleKind.CrumblingRock, full));
            Assert.IsTrue(PassageRules.CanPassObstacle(ObstacleKind.HotDry, full));
            Assert.IsTrue(PassageRules.CanPassObstacle(ObstacleKind.PitchDark, full));
        }

        [Test]
        public void EvaluatePassage_ReturnsVietnameseReasonWhenBlocked()
        {
            var caps = new SlimeCapabilities(1.2f);
            bool ok = PassageRules.EvaluatePassage(TunnelSize.Small, ObstacleKind.None, caps, out var reason);
            Assert.IsFalse(ok);
            Assert.IsNotEmpty(reason);

            var smallCaps = new SlimeCapabilities(0.8f);
            ok = PassageRules.EvaluatePassage(TunnelSize.Small, ObstacleKind.Flooded, smallCaps, out reason);
            Assert.IsFalse(ok);
            Assert.That(reason, Does.Contain("thở nước"));

            var diver = new SlimeCapabilities(0.8f, canBreatheWater: true);
            ok = PassageRules.EvaluatePassage(TunnelSize.Small, ObstacleKind.Flooded, diver, out reason);
            Assert.IsTrue(ok);
            Assert.IsNull(reason);
        }
    }
}
