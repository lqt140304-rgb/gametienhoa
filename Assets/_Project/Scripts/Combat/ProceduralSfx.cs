using UnityEngine;

namespace Evolve.Combat
{
    /// <summary>Sounds made from code (no files): the whoosh of Cáo Gió's wind spears, built once from <see cref="WhooshSynth"/>.</summary>
    public static class ProceduralSfx
    {
        public enum Kind { SpearLaunch, SpearOrbit }

        const int Rate = 22050;
        static AudioClip launch, orbit;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { launch = orbit = null; }

        static AudioClip Build(string name, float[] samples)
        {
            var clip = AudioClip.Create(name, samples.Length, 1, Rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        static AudioClip Clip(Kind kind)
        {
            if (kind == Kind.SpearLaunch) return launch != null ? launch : launch = Build("SpearLaunch", WhooshSynth.Make(11, Rate, 0.55f, 3600f, 600f));   // high to low: it flies past
            return orbit != null ? orbit : orbit = Build("SpearOrbit", WhooshSynth.Make(23, Rate, 0.4f, 900f, 1500f));                                    // low to high: a hum winding up
        }

        public static void Play(Kind kind, Vector3 position, float volume = 1f) => AudioSource.PlayClipAtPoint(Clip(kind), position, volume);
    }
}
