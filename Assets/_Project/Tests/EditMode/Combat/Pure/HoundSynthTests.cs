using System;
using System.Linq;
using Evolve.Combat;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class HoundSynthTests
    {
        const int Rate = 22050;
        static void Sane(float[] s, float seconds)
        {
            Assert.AreEqual((int)(Rate * seconds), s.Length);
            Assert.IsTrue(s.All(x => !float.IsNaN(x) && !float.IsInfinity(x)));
            float peak = s.Max(Math.Abs);
            Assert.That(peak, Is.InRange(0.5f, 0.86f));
        }

        [Test] public void Growl_IsSane_AndDeterministicBySeed()
        {
            var a = HoundSynth.Growl(5, Rate, 1.2f, 70f, 55f); var b = HoundSynth.Growl(5, Rate, 1.2f, 70f, 55f); var c = HoundSynth.Growl(6, Rate, 1.2f, 70f, 55f);
            Sane(a, 1.2f); CollectionAssert.AreEqual(a, b); CollectionAssert.AreNotEqual(a, c);
        }

        [Test] public void Growl_FadesInAndOut()
        {
            var s = HoundSynth.Growl(1, Rate, 1f, 80f, 80f);
            Assert.Less(Math.Abs(s[0]), 0.05f); Assert.Less(Math.Abs(s[s.Length - 1]), 0.05f);
        }

        [Test] public void Thud_IsSane_AndMostOfItsEnergyComesFirst()
        {
            var s = HoundSynth.Thud(3, Rate, 0.6f);
            Sane(s, 0.6f);
            double first = 0, second = 0;
            for (int i = 0; i < s.Length; i++) { if (i < s.Length / 3) first += s[i] * s[i]; else second += s[i] * s[i]; }
            Assert.Greater(first, second);
        }

        [Test] public void Snap_IsShort_AndLoudAtTheStart()
        {
            var s = HoundSynth.Snap(2, Rate, 0.25f);
            Sane(s, 0.25f);
            Assert.Greater(s.Take(Rate / 20).Max(Math.Abs), s.Skip(s.Length * 3 / 4).Max(Math.Abs));
        }
    }
}
