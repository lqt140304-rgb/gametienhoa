using Evolve.Combat;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class RoomRulesTests
    {
        [Test] public void Activates_OnlyWhenEveryLivingPlayerIsInside()
        {
            Assert.IsTrue(RoomRules.ShouldActivate(2, 2)); Assert.IsFalse(RoomRules.ShouldActivate(1, 2)); Assert.IsTrue(RoomRules.ShouldActivate(1, 1));
        }

        [Test] public void NeverActivates_WithNoOneAlive()
        {
            Assert.IsFalse(RoomRules.ShouldActivate(0, 0));   // a room with nobody in the session does not wake the boss
        }

        [Test] public void Door_StartsOpen_ClosesInOneAndAHalfSeconds()
        {
            var d = new DoorFsm();
            Assert.AreEqual(DoorState.Open, d.State); Assert.AreEqual(1f, d.Openness, 1e-4f);
            d.Close(); Assert.AreEqual(DoorState.Closing, d.State);
            d.Tick(0.75f); Assert.AreEqual(DoorState.Closing, d.State); Assert.AreEqual(0.5f, d.Openness, 0.02f);
            d.Tick(0.8f); Assert.AreEqual(DoorState.Closed, d.State); Assert.AreEqual(0f, d.Openness, 1e-4f);
            Assert.AreEqual(1.5f, DoorFsm.CloseSeconds);
        }

        [Test] public void Door_OpensBackAfterBeingClosed_AndAnUnneededCallDoesNothing()
        {
            var d = new DoorFsm();
            d.Open(); Assert.AreEqual(DoorState.Open, d.State);       // already open
            d.Close(); d.Tick(2f); d.Close(); Assert.AreEqual(DoorState.Closed, d.State);   // already closed
            d.Open(); Assert.AreEqual(DoorState.Opening, d.State);
            d.Tick(0.75f); Assert.AreEqual(0.5f, d.Openness, 0.02f);
            d.Tick(1f); Assert.AreEqual(DoorState.Open, d.State); Assert.AreEqual(1f, d.Openness, 1e-4f);
        }

        [Test] public void Door_ReversesMidWay_FromWhereItIs()
        {
            var d = new DoorFsm();
            d.Close(); d.Tick(0.75f); d.Open();                        // the boss died while the door was half shut
            Assert.AreEqual(DoorState.Opening, d.State); Assert.AreEqual(0.5f, d.Openness, 0.02f);
            d.Tick(0.4f); Assert.Greater(d.Openness, 0.5f);
        }
    }
}
