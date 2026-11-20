using System;
using Evolve.Combat;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class SpearRulesTests
    {
        static float Angle((float x, float y, float z) p) => (float)(Math.Atan2(p.z, p.x) * 180.0 / Math.PI);
        static float Gap(float a, float b) => (float)Math.Abs(((a - b) % 360.0 + 540.0) % 360.0 - 180.0);

        [Test] public void Orbit_ThreeSpears_Are120DegreesApart_OnOneCircle()
        {
            var p = new[] { SpearRules.OrbitOffset(0, 3, 1.3f, 0.9f, 540f, 0f), SpearRules.OrbitOffset(1, 3, 1.3f, 0.9f, 540f, 0f), SpearRules.OrbitOffset(2, 3, 1.3f, 0.9f, 540f, 0f) };
            foreach (var q in p) Assert.AreEqual(0.9f, (float)Math.Sqrt(q.x * q.x + q.z * q.z), 1e-3f);
            Assert.AreEqual(120f, Gap(Angle(p[0]), Angle(p[1])), 0.1f);
            Assert.AreEqual(120f, Gap(Angle(p[1]), Angle(p[2])), 0.1f);
            Assert.AreEqual(120f, Gap(Angle(p[2]), Angle(p[0])), 0.1f);
        }

        [Test] public void Orbit_Turns_AtTheGivenSpeed()
        {
            var a = SpearRules.OrbitOffset(0, 3, 0f, 0.9f, 360f, 0f); var b = SpearRules.OrbitOffset(0, 3, 0.5f, 0.9f, 360f, 0f);
            Assert.AreEqual(180f, Gap(Angle(a), Angle(b)), 0.1f);
        }

        [Test] public void Orbit_Height_StaysNearTheGivenHeight()
        {
            for (float t = 0f; t < 5f; t += 0.07f) Assert.LessOrEqual(Math.Abs(SpearRules.OrbitOffset(1, 3, t, 0.9f, 540f, 1.5f).y - 1.5f), 0.15f);
        }

        [Test] public void Heading_IsTangent()
        {
            var p = SpearRules.OrbitOffset(1, 3, 0.8f, 0.9f, 540f, 0f);
            Assert.AreEqual(0f, Gap(SpearRules.OrbitHeadingDeg(1, 3, 0.8f, 540f), Angle(p) + 90f), 0.1f);
        }

        [Test] public void LaunchTime_ByIndex()
        {
            Assert.AreEqual(0f, SpearRules.LaunchTime(0, 0.3f)); Assert.AreEqual(0.3f, SpearRules.LaunchTime(1, 0.3f), 1e-5f); Assert.AreEqual(0.6f, SpearRules.LaunchTime(2, 0.3f), 1e-5f);
        }

        static float Rms(float[] s, int from, int to) { double sum = 0; for (int i = from; i < to; i++) sum += s[i] * s[i]; return (float)Math.Sqrt(sum / Math.Max(1, to - from)); }

        [Test] public void Whoosh_Length_Peak_NoNaN()
        {
            var s = WhooshSynth.Make(1, 22050, 0.5f, 3600f, 600f);
            Assert.AreEqual(11025, s.Length);
            float peak = 0f; foreach (float v in s) { Assert.IsFalse(float.IsNaN(v) || float.IsInfinity(v)); peak = Math.Max(peak, Math.Abs(v)); }
            Assert.GreaterOrEqual(peak, 0.8f); Assert.LessOrEqual(peak, 0.86f);
        }

        [Test] public void Whoosh_Deterministic_AndSeedChangesIt()
        {
            CollectionAssert.AreEqual(WhooshSynth.Make(2, 22050, 0.2f, 1000f, 2000f), WhooshSynth.Make(2, 22050, 0.2f, 1000f, 2000f));
            CollectionAssert.AreNotEqual(WhooshSynth.Make(2, 22050, 0.2f, 1000f, 2000f), WhooshSynth.Make(3, 22050, 0.2f, 1000f, 2000f));
        }

        [Test] public void Whoosh_FadesInAndOut()
        {
            var s = WhooshSynth.Make(5, 22050, 0.6f, 3000f, 700f); int n = s.Length;
            float mid = Rms(s, (int)(n * 0.4f), (int)(n * 0.6f));
            Assert.Less(Rms(s, 0, (int)(n * 0.05f)), mid / 3f);
            Assert.Less(Rms(s, (int)(n * 0.95f), n), mid / 3f);
        }
    }
}
