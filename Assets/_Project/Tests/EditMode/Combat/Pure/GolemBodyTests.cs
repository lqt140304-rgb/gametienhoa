using NUnit.Framework;
using Evolve.Combat;

namespace Evolve.Tests
{
    public class GolemBodyTests
    {
        static ArmPose P(string a, MovePhase ph, float t) => GolemRules.Evaluate(a, ph, t);

        [Test] public void Meteor_CrouchesDeepToSpring_ThenTucksTheLegsInTheAir_ThenAbsorbsTheLanding()
        {
            Assert.Greater(P("Meteor", MovePhase.Windup, 0.45f).Crouch, 0.8f);                 // the spring: knees bent just before it leaves the ground
            Assert.Less(P("Meteor", MovePhase.Windup, 1f).Crouch, 0.2f);                        // in the air: legs are no longer crouched
            Assert.Greater(P("Meteor", MovePhase.Windup, 0.8f).Tuck, 0.6f);                    // legs pulled up while it hangs in the sky
            Assert.Greater(P("Meteor", MovePhase.Active, 1f).Crouch, 0.6f);                    // lands in a deep crouch
            Assert.Less(P("Meteor", MovePhase.Recover, 1f).Crouch, 0.05f);                     // and stands up again
        }

        [Test] public void Punch_TwistsTheBodyBack_ThenThroughItWithAStepForward()
        {
            Assert.Less(P("Punch", MovePhase.Windup, 1f).Twist, -20f); Assert.Greater(P("Punch", MovePhase.Active, 1f).Twist, 20f);
            Assert.Greater(P("Punch", MovePhase.Active, 1f).Stance, 0.7f);
        }

        [Test] public void Throw_PlantsTheFrontFoot_AndTwistsLikeAThrower()
        {
            Assert.Greater(P("Throw", MovePhase.Windup, 1f).Stance, 0.4f);
            Assert.Less(P("Throw", MovePhase.Windup, 1f).Twist, -20f); Assert.Greater(P("Throw", MovePhase.Active, 1f).Twist, 15f);
        }

        [Test] public void Slam_SinksIntoTheBlow_AndHardenSquatsLow()
        {
            Assert.Greater(P("Slam", MovePhase.Active, 1f).Crouch, 0.4f); Assert.Greater(P("Harden", MovePhase.Active, 0.5f).Crouch, 0.4f);
        }

        [Test] public void EveryBodyValue_StaysInItsLimits_AndRestIsZero()
        {
            foreach (var a in new[] { "Slam", "Throw", "Harden", "CoreBlast", "Punch", "Grab", "Meteor", "Hurt", "Sweep", "Rush", "Dizzy" })
            {
                foreach (var ph in new[] { MovePhase.Windup, MovePhase.Active, MovePhase.Recover })
                    for (float t = 0f; t <= 1f; t += 0.1f)
                    {
                        var p = P(a, ph, t);
                        Assert.That(p.Crouch, Is.InRange(0f, 1f), a); Assert.That(p.Tuck, Is.InRange(0f, 1f), a);
                        Assert.That(p.Stance, Is.InRange(-1f, 1f), a); Assert.That(p.Twist, Is.InRange(-60f, 60f), a);
                    }
                Assert.AreEqual(0f, P(a, MovePhase.Idle, 0.5f).Crouch);
            }
        }

        [Test] public void Hurt_Flinches_AndSettles()
        {
            Assert.Less(P("Hurt", MovePhase.Active, 0.2f).Hunch, -10f);                       // thrown back by the blow
            Assert.AreEqual(0f, P("Hurt", MovePhase.Recover, 1f).Hunch, 1e-3f);
        }

        [Test] public void Gait_SwaysTheHipsFromSideToSide_OncePerStep_AndRunsLeanHarderThanWalks()
        {
            Assert.AreEqual(0f, GaitRules.Sway(0f, 4f), 1e-3f); Assert.AreEqual(4f, GaitRules.Sway(0.25f, 4f), 1e-3f); Assert.AreEqual(-4f, GaitRules.Sway(0.75f, 4f), 1e-3f);
            Assert.AreEqual(0f, GaitRules.RunBlend(1.5f)); Assert.AreEqual(1f, GaitRules.RunBlend(12f)); Assert.That(GaitRules.RunBlend(6f), Is.InRange(0.2f, 0.8f));
        }

        [Test] public void Meteor_PointsAtThePlayerFirst_ThenGathersAndCrouches_BeforeItJumps()
        {
            Assert.Less(P("Meteor", MovePhase.Windup, 0.2f).Back, -60f);                             // the right arm stretched out toward the player
            Assert.Greater(P("Meteor", MovePhase.Windup, 0.45f).Crouch, 0.8f);                       // then the deep crouch to spring
            Assert.AreEqual(0f, LeapRules.Height(MovePhase.Windup, 0.4f));                           // and it has not left the ground yet
            Assert.Greater(LeapRules.Height(MovePhase.Windup, 0.7f), 0f);
        }

        [Test] public void Rush_CoversTheHeadWithBothArms_AndStaysLow()
        {
            var r = P("Rush", MovePhase.Active, 0.5f);
            Assert.Greater(r.Raise, 130f); Assert.Greater(r.Hunch, 30f); Assert.Greater(r.Crouch, 0.2f); Assert.AreEqual(0f, r.Back);
        }

        [Test] public void Sweep_DrawsTheArmWide_ThenSweepsItAcrossWithATwist()
        {
            Assert.Greater(P("Sweep", MovePhase.Windup, 1f).Spread, 50f); Assert.Greater(P("Sweep", MovePhase.Windup, 1f).Twist, 20f);
            Assert.Less(P("Sweep", MovePhase.Active, 1f).Twist, -20f);
        }

        [Test] public void VolleyPoses_RiseFromACrouch_Roar_HugTheColumn_CarryItOverTheHead_AndFollowThrough()
        {
            Assert.Greater(GolemRules.Volley(VolleyStage.Rise, 0f).Crouch, 0.8f); Assert.AreEqual(0f, GolemRules.Volley(VolleyStage.Rise, VolleyFsm.RiseSeconds).Crouch, 1e-3f);
            var roar = GolemRules.Volley(VolleyStage.Roar, 1f);
            Assert.Less(roar.Hunch, -15f); Assert.Greater(roar.Spread, 40f); Assert.Greater(roar.Glow, 0.7f);                         // head thrown back, arms wide, the core blazing
            Assert.Greater(GolemRules.Volley(VolleyStage.Pull, 0.28f * VolleyFsm.PullSeconds).Hunch, 25f);                          // straining: bent over the column
            Assert.Less(GolemRules.Volley(VolleyStage.Pull, 0.2f * VolleyFsm.PullSeconds).Hunch, GolemRules.Volley(VolleyStage.Pull, 0.28f * VolleyFsm.PullSeconds).Hunch - 20f);   // heaving: leaning back
            Assert.Greater(GolemRules.Volley(VolleyStage.Pull, 1f).Raise, 40f);
            var carry = GolemRules.Volley(VolleyStage.Hold, 1f);
            Assert.Greater(carry.Raise - carry.Back, 130f); Assert.Greater(carry.Raise, 90f); Assert.Less(carry.Hunch, 0f);              // the right arm high over the head with the column, the left up beside it
            var after = GolemRules.Volley(VolleyStage.Release, 0.3f);
            Assert.Greater(after.Hunch, 25f); Assert.Greater(after.Twist, 20f); Assert.Greater(after.Stance, 0.6f);                      // leaning through the throw, the front foot out
            Assert.AreEqual(0f, GolemRules.Volley(VolleyStage.Release, VolleyFsm.ReleaseSeconds).Hunch, 1e-3f);                         // and back to rest before the next spike
            Assert.AreEqual(0f, GolemRules.Volley(VolleyStage.Walk, 1f).Crouch); Assert.AreEqual(0f, GolemRules.Volley(VolleyStage.Done, 1f).Raise);
        }
    }
}
