using Evolve.Player;
using NUnit.Framework;
using UnityEngine;

namespace Evolve.Tests.EditMode
{
    public class SwimLogicTests
    {
        [Test] public void ShallowWaterIsWadedDeepWaterIsSwum()
        {
            Assert.IsFalse(SwimLogic.IsSwimming(0.4f));   // the Life Shallows
            Assert.IsFalse(SwimLogic.IsSwimming(0.15f));  // the stream
            Assert.IsTrue(SwimLogic.IsSwimming(0.6f));
            Assert.IsTrue(SwimLogic.IsSwimming(2f));      // a deep pool
        }

        [Test] public void SpaceRisesCtrlDives()
        {
            Assert.AreEqual(SwimLogic.VerticalSpeed, SwimLogic.Velocity(Vector3.zero, 1f, 1f).y, 1e-5f);
            Assert.AreEqual(-SwimLogic.VerticalSpeed, SwimLogic.Velocity(Vector3.zero, -1f, 1f).y, 1e-5f);
        }

        [Test] public void NoInputDriftsGentlyUp()
        {
            float y = SwimLogic.Velocity(Vector3.forward, 0f, 1f).y;
            Assert.Greater(y, 0f);
            Assert.LessOrEqual(y, 0.3f);
        }

        [Test] public void FloatsWithOnlyTheBackAboveWater()
        {
            Assert.AreEqual(-0.14f, SwimLogic.TargetY(0.06f), 1e-5f);
        }

        [Test] public void UnderwaterCameraDoesNotFlickerAtTheSurface()
        {
            Assert.IsFalse(SwimLogic.IsUnderwater(0.03f, 0.06f, false));   // just below, not yet
            Assert.IsTrue(SwimLogic.IsUnderwater(0.0f, 0.06f, false));
            Assert.IsTrue(SwimLogic.IsUnderwater(0.09f, 0.06f, true));     // just above, still under
            Assert.IsFalse(SwimLogic.IsUnderwater(0.12f, 0.06f, true));
        }

        [Test] public void HorizontalFollowsMoveDirection()
        {
            var v = SwimLogic.Velocity(new Vector3(1f, 0f, 0f), 0f, 0.65f);
            Assert.AreEqual(0.65f, v.x, 1e-5f);
            Assert.AreEqual(0f, v.z, 1e-5f);
        }
    }
}
