using System;
using System.Collections.Generic;
using Evolve.Traits;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class InstabilityTests
    {
        [Test] public void Base_StartsHigh() => Assert.GreaterOrEqual(Instability.BaseFor(BirthStepKind.Disconnect, 0, 0f), 0.85f);
        [Test] public void Base_EndsAtZero() => Assert.AreEqual(0f, Instability.BaseFor(BirthStepKind.Stabilize, 0, 1f), 1e-5f);

        [Test]
        public void Base_Stabilize_Decreases()
        {
            float prev = 1f;
            for (float t = 0f; t <= 1f; t += 0.1f)
            {
                float v = Instability.BaseFor(BirthStepKind.Stabilize, 0, t);
                Assert.LessOrEqual(v, prev + 1e-6f);
                prev = v;
            }
        }

        [Test] public void Base_Gifts_Decrease() =>
            Assert.Greater(Instability.BaseFor(BirthStepKind.Gift, 0, 0f), Instability.BaseFor(BirthStepKind.Gift, 5, 1f));

        [Test]
        public void Instability_AlwaysInRange()
        {
            var pulses = new List<(float at, float strength)> { (1f, 0.9f), (1.1f, 0.9f), (1.2f, 0.9f) };   // stacked pulses must still clamp
            foreach (BirthStepKind kind in Enum.GetValues(typeof(BirthStepKind)))
                for (int g = 0; g < 6; g++)
                    for (float t = 0f; t <= 1f; t += 0.25f)
                    {
                        float b = Instability.BaseFor(kind, g, t);
                        Assert.That(b, Is.InRange(0f, 1f), kind + " " + g + " " + t);
                        Assert.That(Instability.Value(b, pulses, 1.3f), Is.InRange(0f, 1f));
                    }
        }

        [Test]
        public void Pulse_DecaysAndIsZeroBefore()
        {
            Assert.AreEqual(0f, Instability.Pulse(-0.1f, 0.5f));
            Assert.AreEqual(0.5f, Instability.Pulse(0f, 0.5f), 1e-5f);
            Assert.Less(Instability.Pulse(0.5f, 0.5f), Instability.Pulse(0.1f, 0.5f));
            Assert.Less(Instability.Pulse(3f, 0.5f), 0.01f);
        }

        [Test]
        public void Value_AddsPulse()
        {
            var pulses = new List<(float at, float strength)> { (2f, 0.3f) };
            Assert.AreEqual(0.4f, Instability.Value(0.4f, pulses, 1f), 1e-5f);        // before the pulse
            Assert.AreEqual(0.7f, Instability.Value(0.4f, pulses, 2f), 1e-5f);        // at the pulse
            Assert.AreEqual(0.4f, Instability.Value(0.4f, null, 5f), 1e-5f);
        }
    }
}
