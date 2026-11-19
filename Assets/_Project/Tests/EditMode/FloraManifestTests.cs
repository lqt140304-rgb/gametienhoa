using System.Linq;
using Evolve.World;
using NUnit.Framework;
using UnityEngine;

namespace Evolve.Tests.EditMode
{
    public class FloraManifestTests
    {
        const string Two = "{\"assets\":[{\"id\":\"fern_02\",\"source\":\"polyhaven\",\"kind\":\"model\",\"resolution\":\"2k\",\"role\":\"foliage\",\"tier\":\"shrub\",\"species\":\"Fern\"}," +
                           "{\"id\":\"rock_07\",\"source\":\"polyhaven\",\"kind\":\"model\",\"resolution\":\"1k\",\"role\":\"solid\",\"tier\":\"rock\",\"species\":\"Rock\"}]}";

        [Test]
        public void Parse_ReadsAllFields()
        {
            var m = FloraManifest.Parse(Two);
            Assert.AreEqual(2, m.assets.Length);
            var a = m.assets[0];
            Assert.AreEqual("fern_02", a.id); Assert.AreEqual("polyhaven", a.source); Assert.AreEqual("model", a.kind);
            Assert.AreEqual("2k", a.resolution); Assert.AreEqual("foliage", a.role); Assert.AreEqual("shrub", a.tier); Assert.AreEqual("Fern", a.species);
            Assert.IsTrue(a.IsFoliage);
        }

        [Test]
        public void Parse_EmptyOrBroken_ReturnsEmpty()
        {
            Assert.AreEqual(0, FloraManifest.Parse("").assets.Length);
            Assert.AreEqual(0, FloraManifest.Parse(null).assets.Length);
            Assert.AreEqual(0, FloraManifest.Parse("{not json").assets.Length);
        }

        [Test]
        public void Tier_FiltersByTier()
        {
            var m = FloraManifest.Parse(Two);
            Assert.AreEqual(new[] { "rock_07" }, m.Tier("rock").Select(a => a.id).ToArray());
            Assert.AreEqual(0, m.Tier("tall").Count());
        }

        [Test]
        public void TheRepoManifestParsesAndStaysUnder3GbOfAssets()
        {
            var path = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Application.dataPath), "tools/assets/flora_manifest.json");
            var m = FloraManifest.Parse(System.IO.File.ReadAllText(path));
            Assert.Greater(m.assets.Length, 10);
            foreach (var a in m.assets) Assert.IsTrue(a.resolution == "1k" || a.resolution == "2k", a.id);
        }
    }
}
