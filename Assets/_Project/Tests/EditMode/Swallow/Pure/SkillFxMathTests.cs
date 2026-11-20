using System.Collections.Generic;
using Evolve.Swallow;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class SkillFxMathTests
    {
        [Test] public void FamilyColors_AllDistinct()
        {
            var seen = new HashSet<string>();
            foreach (SkillFamily f in System.Enum.GetValues(typeof(SkillFamily)))
            {
                var c = SkillFxMath.FamilyColor(f);
                Assert.IsTrue(seen.Add($"{c.r:0.00}{c.g:0.00}{c.b:0.00}"), f.ToString());
            }
        }

        [Test] public void Envelope_ZeroAtEnds_OneInMiddle()
        {
            Assert.AreEqual(0f, SkillFxMath.Envelope(0f, 2f, 0.2f, 0.4f));
            Assert.AreEqual(0f, SkillFxMath.Envelope(2f, 2f, 0.2f, 0.4f));
            Assert.AreEqual(1f, SkillFxMath.Envelope(1f, 2f, 0.2f, 0.4f), 1e-4f);
            Assert.AreEqual(0.5f, SkillFxMath.Envelope(0.1f, 2f, 0.2f, 0.4f), 1e-4f);
            Assert.AreEqual(0.5f, SkillFxMath.Envelope(1.8f, 2f, 0.2f, 0.4f), 1e-4f);
        }

        [Test] public void Envelope_ShortEffect_NeverAboveOne()
        {
            for (float t = 0.01f; t < 0.3f; t += 0.01f) Assert.LessOrEqual(SkillFxMath.Envelope(t, 0.3f, 0.2f, 0.4f), 1f);
        }

        [Test] public void EaseOutBack_Endpoints_AndOvershoot()
        {
            Assert.AreEqual(0f, SkillFxMath.EaseOutBack(0f), 1e-4f);
            Assert.AreEqual(1f, SkillFxMath.EaseOutBack(1f), 1e-4f);
            Assert.Greater(SkillFxMath.EaseOutBack(0.7f), 1f);
        }

        [Test] public void EaseOutCubic_Endpoints_AndFastStart()
        {
            Assert.AreEqual(0f, SkillFxMath.EaseOutCubic(0f), 1e-4f);
            Assert.AreEqual(1f, SkillFxMath.EaseOutCubic(1f), 1e-4f);
            Assert.Greater(SkillFxMath.EaseOutCubic(0.3f), 0.3f);
        }

        [Test] public void PseudopodRadius_FlaresAtRoot_ClosesAtTip()
        {
            Assert.Greater(SkillFxMath.PseudopodRadius(0f, 0.3f, 0.1f, 0.6f, 0f, 0.1f), 0.3f);
            Assert.AreEqual(0f, SkillFxMath.PseudopodRadius(1f, 0.3f, 0.1f, 0.6f, 0f, 0.1f), 1e-4f);
            float mid = SkillFxMath.PseudopodRadius(0.5f, 0.3f, 0.1f, 0.6f, 0f, 0.1f);
            Assert.Less(mid, 0.3f); Assert.Greater(mid, 0.1f);
        }

        [Test] public void PseudopodRadius_BulbSwellsNearTip()
        {
            Assert.Greater(SkillFxMath.PseudopodRadius(0.86f, 0.3f, 0.1f, 0f, 0.8f, 0.1f), SkillFxMath.PseudopodRadius(0.86f, 0.3f, 0.1f, 0f, 0f, 0.1f));
        }

        [Test] public void Reach_OutHoldBack()
        {
            Assert.AreEqual(0f, SkillFxMath.Reach(0f, 1f, 0.1f, 0.2f));
            Assert.AreEqual(1f, SkillFxMath.Reach(0.5f, 1f, 0.1f, 0.2f), 1e-4f);
            Assert.AreEqual(0f, SkillFxMath.Reach(1f, 1f, 0.1f, 0.2f));
            Assert.Greater(SkillFxMath.Reach(0.05f, 1f, 0.1f, 0.2f), 0.5f);   // fast start
        }
    }
}
