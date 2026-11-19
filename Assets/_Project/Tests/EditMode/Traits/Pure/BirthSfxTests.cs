using System;
using System.IO;
using System.Linq;
using Evolve.Traits;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class BirthSfxTests
    {
        [Test]
        public void Ids_UniqueWithPromptAndSensibleLength()
        {
            Assert.AreEqual(BirthSfx.All.Count, BirthSfx.All.Select(s => s.Id).Distinct().Count());
            foreach (var s in BirthSfx.All)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(s.Prompt), s.Id);
                Assert.That(s.Seconds, Is.InRange(0.5f, 22f), s.Id);
            }
        }

        [Test]
        public void HasEverySoundTheSceneUses()
        {
            string[] need = { "type_tick", "glitch_burst", "alarm", "hum_loop", "whoosh", "card_deal", "card_flip", "core_common", "core_rare", "core_mythic",
                              "flaw_reveal", "slam_big", "slam_small", "stable_chime", "collapse", "pull",
                              "gift_tui", "gift_nuot", "gift_giaima", "gift_bienhinh", "gift_songdo", "gift_phanthan" };
            foreach (var id in need) Assert.IsTrue(BirthSfx.Find(id).HasValue, id);
            Assert.IsTrue(BirthSfx.Find("hum_loop").Value.Loop);
            Assert.IsFalse(BirthSfx.Find("nope").HasValue);
        }

        /// <summary>Not a check: with EXPORT_BIRTH_SFX=&lt;file&gt; it writes id, seconds and prompt (tab separated) for tools/voice/generate_birth_sfx.sh.</summary>
        [Test]
        public void ExportBirthSfx()
        {
            var path = Environment.GetEnvironmentVariable("EXPORT_BIRTH_SFX");
            if (string.IsNullOrEmpty(path)) Assert.Pass("EXPORT_BIRTH_SFX not set");
            File.WriteAllLines(path, BirthSfx.All.Select(s => s.Id + "\t" + s.Seconds.ToString(System.Globalization.CultureInfo.InvariantCulture) + "\t" + (s.Loop ? "loop" : "once") + "\t" + s.Prompt));
        }
    }
}
