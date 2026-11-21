using System.Collections.Generic;
using UnityEngine;

namespace Evolve.Combat
{
    /// <summary>Sounds made from code (no files): the whoosh of Cáo Gió's wind spears (<see cref="WhooshSynth"/>) and the voice and body of Chó Xương (<see cref="HoundSynth"/>), each built once on first use.</summary>
    public static class ProceduralSfx
    {
        public enum Kind { SpearLaunch, SpearOrbit, HoundGrowlShort, HoundRoar, HoundHowl, HoundThud, HoundSnap, HoundLunge, HoundSwipe }

        const int Rate = 22050;
        static readonly Dictionary<Kind, AudioClip> clips = new Dictionary<Kind, AudioClip>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { clips.Clear(); }

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
                default: return WhooshSynth.Make(41, Rate, 0.3f, 2600f, 900f);                       // a swipe
            }
        }

        static AudioClip Clip(Kind kind)
        {
            if (!clips.TryGetValue(kind, out var c) || c == null) clips[kind] = c = Build(kind, Make(kind));
            return c;
        }

        public static void Play(Kind kind, Vector3 position, float volume = 1f) => AudioSource.PlayClipAtPoint(Clip(kind), position, volume);
    }
}
