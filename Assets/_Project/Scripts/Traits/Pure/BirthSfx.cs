using System.Collections.Generic;

namespace Evolve.Traits
{
    /// <summary>
    /// Every sound effect of the birth scene (spec 5.7): its name, a text prompt for ElevenLabs Sound Effects, its length and whether it loops.
    /// The files are made from this table by tools/voice/generate_birth_sfx.sh into Resources/Sfx/Birth/&lt;id&gt;.mp3; a missing file is silent.
    /// </summary>
    public static class BirthSfx
    {
        public readonly struct Sfx
        {
            public readonly string Id, Prompt;
            public readonly float Seconds;
            public readonly bool Loop;
            public Sfx(string id, string prompt, float seconds, bool loop = false) { Id = id; Prompt = prompt; Seconds = seconds; Loop = loop; }
        }

        public static IReadOnlyList<Sfx> All { get; } = new List<Sfx>
        {
            new Sfx("type_tick", "tiny mechanical keyboard tick, soft digital click, single short blip", 0.5f),
            new Sfx("glitch_burst", "digital glitch burst, static crackle, signal tearing apart, short", 1.5f),
            new Sfx("alarm", "urgent sci-fi warning alarm, pulsing red alert beeps, tense", 3f),
            new Sfx("hum_loop", "low unstable electrical hum, ominous sci-fi drone, flickering power, seamless loop", 8f, true),
            new Sfx("whoosh", "fast whoosh swoosh, data stream flying past, short", 1f),
            new Sfx("card_deal", "card flicked and sliding across glass, quick light whoosh", 0.8f),
            new Sfx("card_flip", "card flip snap, sharp magical click with tiny sparkle", 0.6f),
            new Sfx("core_common", "soft crystalline chime, subtle sparkle reveal, gentle", 2f),
            new Sfx("core_rare", "bright magical reveal chime with rising shimmer, uplifting", 2.5f),
            new Sfx("core_mythic", "epic golden reveal, powerful choir hit, rising shimmer and a booming impact", 4f),
            new Sfx("flaw_reveal", "ominous low thud with a dissonant warning tone, dark", 2.5f),
            new Sfx("slam_big", "massive impact slam, heavy boom with metallic crash and deep rumble", 2f),
            new Sfx("slam_small", "short heavy impact hit, punchy thud with a crack", 1f),
            new Sfx("stable_chime", "calm soft system chime, gentle resolve, peaceful sci-fi confirmation tone", 3f),
            new Sfx("collapse", "a digital world shattering into pieces, glass breaking with electronic distortion and static", 3f),
            new Sfx("pull", "rising sucking whoosh, being dragged fast through a data tunnel, accelerating", 3f),
            new Sfx("gift_tui", "a dark hollow pouch opening, soft whoosh then a deep gulp", 2.5f),
            new Sfx("gift_nuot", "wet squishy gulp swallow, a blob engulfing something", 2f),
            new Sfx("gift_giaima", "rapid data decoding blips, rising computer chatter resolving into a clean tone", 3f),
            new Sfx("gift_bienhinh", "squishy morphing transformation, elastic stretch and snap back", 2.5f),
            new Sfx("gift_songdo", "a sonar ping with an echoing ripple fading away", 3f),
            new Sfx("gift_phanthan", "a small blob splitting off with a wet pop, then merging back with a soft squelch", 3f),
        };

        public static Sfx? Find(string id)
        {
            foreach (var s in All) if (s.Id == id) return s;
            return null;
        }
    }
}
