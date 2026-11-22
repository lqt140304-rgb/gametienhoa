using Evolve.Combat;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class ShockwaveRulesTests
    {
        // a wave from the origin along +x, 14 m long, 2.4 m wide, its front 1 m deep
        static bool Hit(float front, float px, float pz) => ShockwaveRules.Hits(0f, 0f, 1f, 0f, 14f, 2.4f, front, 1f, px, pz);

        [Test] public void Front_RunsAtItsSpeed_AndStopsAtTheEndOfTheWave()
        {
            Assert.AreEqual(0f, ShockwaveRules.Front(14f, 0f, 14f)); Assert.AreEqual(7f, ShockwaveRules.Front(14f, 0.5f, 14f), 1e-4f); Assert.AreEqual(14f, ShockwaveRules.Front(14f, 5f, 14f));
        }

        [Test] public void Hits_OnlyInsideTheMovingBand()
        {
            Assert.IsTrue(Hit(6f, 6f, 0f));                 // right at the front
            Assert.IsFalse(Hit(3f, 6f, 0f));                // the wave has not got there yet
            Assert.IsFalse(Hit(9f, 6f, 0f));                // it has already passed
            Assert.IsTrue(Hit(6f, 6.4f, 1.1f));             // just inside the width
            Assert.IsFalse(Hit(6f, 6f, 1.3f));              // a step to the side: dodged
            Assert.IsFalse(Hit(6f, 6f, -2f));
        }

        [Test] public void Hits_NothingBehindTheOrigin_NorBeyondTheEnd()
        {
            Assert.IsFalse(Hit(0.2f, -0.6f, 0f));           // behind the golem
            Assert.IsFalse(ShockwaveRules.Hits(0f, 0f, 1f, 0f, 14f, 2.4f, 14.4f, 1f, 14.6f, 0f));   // past the end of the wave
        }

        [Test] public void Hits_WorksInAnyDirection_AndNormalisesIt()
        {
            Assert.IsTrue(ShockwaveRules.Hits(5f, 5f, 0f, 3f, 14f, 2.4f, 6f, 1f, 5f, 11f));   // the wave goes along +z from (5, 5)
            Assert.IsFalse(ShockwaveRules.Hits(5f, 5f, 0f, 3f, 14f, 2.4f, 6f, 1f, 8f, 11f));
        }
    }
}
