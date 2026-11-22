using System.Collections.Generic;
using UnityEngine;

namespace Evolve.Combat
{
    /// <summary>Sounds: the whoosh of Cáo Gió's wind spears (<see cref="WhooshSynth"/>) and the voice and body of Chó Xương. Chó Xương's sounds are real recordings made with ElevenLabs (Resources/Sfx/Hound/*.mp3, tools/voice/generate_hound_sfx.sh); a file that is missing falls back to a sound synthesised from code (<see cref="HoundSynth"/>). Each clip is loaded or built once on first use.</summary>
    public static class ProceduralSfx
    {
        public enum Kind { SpearLaunch, SpearOrbit, HoundGrowlShort, HoundRoar, HoundHowl, HoundThud, HoundSnap, HoundLunge, HoundSwipe, HoundSpit, HoundShatter, HoundHurt }

        const int Rate = 22050;
        static readonly Dictionary<Kind, AudioClip> clips = new Dictionary<Kind, AudioClip>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { clips.Clear(); files.Clear(); }

        static AudioClip Build(Kind kind, float[] samples)
        {
            var clip = AudioClip.Create(kind.ToString(), samples.Length, 1, Rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        static float[] Make(Kind kind)
        {
            switch (kind)
            {
                case Kind.SpearLaunch: return WhooshSynth.Make(11, Rate, 0.55f, 3600f, 600f);      // high to low: it flies past
                case Kind.SpearOrbit: return WhooshSynth.Make(23, Rate, 0.4f, 900f, 1500f);        // low to high: a hum winding up
                case Kind.HoundGrowlShort: return HoundSynth.Growl(3, Rate, 0.9f, 85f, 70f);
                case Kind.HoundRoar: return HoundSynth.Growl(7, Rate, 1.6f, 62f, 48f);
                case Kind.HoundHowl: return HoundSynth.Growl(9, Rate, 2.0f, 70f, 150f);            // rising: a howl that climbs
                case Kind.HoundThud: return HoundSynth.Thud(5, Rate, 0.7f);
                case Kind.HoundSnap: return HoundSynth.Snap(4, Rate, 0.3f);
                case Kind.HoundLunge: return WhooshSynth.Make(31, Rate, 0.6f, 500f, 2200f);        // a heavy body launching
                case Kind.HoundSpit: return WhooshSynth.Make(51, Rate, 0.4f, 3200f, 700f);
                case Kind.HoundShatter: return HoundSynth.Snap(8, Rate, 0.3f);
                case Kind.HoundHurt: return HoundSynth.Growl(13, Rate, 0.5f, 120f, 90f);
                default: return WhooshSynth.Make(41, Rate, 0.3f, 2600f, 900f);                       // a swipe
            }
        }

        static string FileName(Kind kind)
        {
            switch (kind)
            {
                case Kind.HoundGrowlShort: return "hound_growl_short";
                case Kind.HoundRoar: return "hound_roar";
                case Kind.HoundHowl: return "hound_howl";
                case Kind.HoundThud: return "hound_thud";
                case Kind.HoundSnap: return "hound_snap";
                case Kind.HoundLunge: return "hound_lunge";
                case Kind.HoundSwipe: return "hound_swipe";
                case Kind.HoundSpit: return "hound_spit";
                case Kind.HoundShatter: return "hound_shatter";
                case Kind.HoundHurt: return "hound_hurt";
                default: return null;
            }
        }

        static AudioClip Clip(Kind kind)
        {
            if (clips.TryGetValue(kind, out var c) && c != null) return c;
            string file = FileName(kind);
            if (file != null) c = Resources.Load<AudioClip>("Sfx/Hound/" + file);   // the real recording, if it is there
            if (c == null) c = Build(kind, Make(kind));
            return clips[kind] = c;
        }

        static readonly Dictionary<string, AudioClip> files = new Dictionary<string, AudioClip>();

        /// <summary>Play Resources/Sfx/<paramref name="path"/> (for example "Golem/golem_slam"); if the file is missing the <paramref name="fallback"/> sound is used instead.</summary>
        public static void PlayFile(string path, Vector3 position, float volume, Kind fallback)
        {
            if (!files.TryGetValue(path, out var c) || c == null) files[path] = c = Resources.Load<AudioClip>("Sfx/" + path);
            if (c == null) { Play(fallback, position, volume); return; }
            AudioSource.PlayClipAtPoint(c, position, volume);
        }

        public static void Play(Kind kind, Vector3 position, float volume = 1f) => AudioSource.PlayClipAtPoint(Clip(kind), position, volume);
    }
}
