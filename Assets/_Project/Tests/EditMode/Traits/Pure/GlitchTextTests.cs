using System.Linq;
using Evolve.Traits;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class GlitchTextTests
    {
        const string Sample = "Xác nhận. Một linh hồn vừa chuyển sinh vào thế giới này.";

        [Test]
        public void Typed_CountsCharactersAndClamps()
        {
            Assert.AreEqual(0, GlitchText.TypedCount(Sample, 0f, 30f));
            Assert.AreEqual(15, GlitchText.TypedCount(Sample, 0.5f, 30f));
            Assert.AreEqual(Sample.Length, GlitchText.TypedCount(Sample, 99f, 30f));
            Assert.AreEqual(0, GlitchText.TypedCount(Sample, -1f, 30f));
            Assert.AreEqual(0, GlitchText.TypedCount(null, 1f, 30f));
        }

        [Test]
        public void Corrupt_ZeroInstability_ChangesNothing() => Assert.AreEqual(Sample, GlitchText.Corrupt(Sample, 0f, 7));

        [Test]
        public void Corrupt_KeepsLengthAndSpaces()
        {
            for (int seed = 0; seed < 50; seed++)
            {
                var c = GlitchText.Corrupt(Sample, 1f, seed);
                Assert.AreEqual(Sample.Length, c.Length);
                for (int i = 0; i < Sample.Length; i++) if (char.IsWhiteSpace(Sample[i]) || Sample[i] == '.') Assert.AreEqual(Sample[i], c[i]);
            }
        }

        [Test]
        public void Corrupt_ChangesSomeButNotMost()
        {
            int changed = 0, total = 0;
            for (int seed = 0; seed < 200; seed++)
            {
                var c = GlitchText.Corrupt(Sample, 1f, seed);
                for (int i = 0; i < Sample.Length; i++) { total++; if (c[i] != Sample[i]) changed++; }
            }
            float share = changed / (float)total;
            Assert.Greater(share, 0.02f, "a broken connection garbles some letters");
            Assert.Less(share, 0.35f, "but the text stays readable");
        }

        [Test]
        public void Corrupt_IsDeterministicPerSeed()
        {
            Assert.AreEqual(GlitchText.Corrupt(Sample, 0.8f, 11), GlitchText.Corrupt(Sample, 0.8f, 11));
            Assert.IsTrue(Enumerable.Range(0, 20).Select(s => GlitchText.Corrupt(Sample, 0.8f, s)).Distinct().Count() > 5);
        }

        [Test]
        public void Corrupt_NullOrEmpty_ReturnsEmpty()
        {
            Assert.AreEqual("", GlitchText.Corrupt(null, 1f, 1));
            Assert.AreEqual("", GlitchText.Corrupt("", 1f, 1));
        }
    }
}
