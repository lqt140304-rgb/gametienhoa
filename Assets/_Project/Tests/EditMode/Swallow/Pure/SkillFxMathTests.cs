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

        [Test] public void LimbWeight_FullOnAxis_NoneOutside_Tapering()
        {
            Assert.AreEqual(1f, SkillFxMath.LimbWeight(1f, 0.85f, 4f), 1e-4f);
            Assert.AreEqual(0f, SkillFxMath.LimbWeight(0.8f, 0.85f, 4f));
            float near = SkillFxMath.LimbWeight(0.98f, 0.85f, 4f), far = SkillFxMath.LimbWeight(0.9f, 0.85f, 4f);
            Assert.Greater(near, far); Assert.Greater(far, 0f);
            Assert.Greater(SkillFxMath.LimbWeight(0.9f, 0.85f, 1.5f), far);   // blunter = wider
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
