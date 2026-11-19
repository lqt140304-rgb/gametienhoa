using System.Collections.Generic;
using UnityEngine;

namespace Evolve.Audio
{
    /// <summary>
    /// The slime's own sounds (crawl, hop, landing, swallow, dissolve, spit), played flat (2D) for the local player. The files are in
    /// Resources/Sfx/Slime/&lt;id&gt;.mp3, made by tools/voice/generate_slime_sfx.sh; a missing file is silent. One hidden object holds a few
    /// audio sources, so a sound never cuts another one off.
    /// </summary>
    public static class SlimeSfx
    {
        const int Voices = 6;
        public const float Master = 0.7f;

        static GameObject host;
        static AudioSource[] voices;
        static AudioSource loop;
        static string loopId;
        static int next;
        static readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();

        static bool Ready()
        {
            if (host != null) return true;
            host = new GameObject("Slime Sfx");
            host.hideFlags = HideFlags.HideAndDontSave;
            Object.DontDestroyOnLoad(host);
            voices = new AudioSource[Voices];
            for (int i = 0; i < Voices; i++) { voices[i] = host.AddComponent<AudioSource>(); voices[i].spatialBlend = 0f; voices[i].playOnAwake = false; }
            loop = host.AddComponent<AudioSource>();
            loop.spatialBlend = 0f; loop.playOnAwake = false; loop.loop = true;
            loopId = null;
            return true;
        }

        static AudioClip Get(string id)
        {
            if (cache.TryGetValue(id, out var c) && c != null) return c;
            c = Resources.Load<AudioClip>("Sfx/Slime/" + id);
            cache[id] = c;   // null is remembered too: a missing file is looked up once
            return c;
        }

        /// <summary>Play a sound once. <paramref name="pitchJitter"/> makes repeats (crawling) less mechanical.</summary>
        public static void Play(string id, float volume = 1f, float pitchJitter = 0.08f)
        {
            if (!Application.isPlaying || !Ready()) return;
            var clip = Get(id);
            if (clip == null) return;
            var src = voices[next]; next = (next + 1) % Voices;
            src.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
            src.PlayOneShot(clip, Mathf.Clamp01(volume) * Master);
        }

        /// <summary>One of the crawl sounds, chosen at random.</summary>
        public static void Step(float volume = 0.45f) => Play("slime_move_" + Random.Range(1, 4), volume, 0.12f);

        /// <summary>Start or stop a looping sound (the dissolving while F is held on a big carcass).</summary>
        public static void SetLoop(string id, bool on)
        {
            if (!Application.isPlaying) return;
            if (!on) { if (loop != null && loop.isPlaying) loop.Stop(); loopId = null; return; }
            if (!Ready()) return;
            if (loopId == id && loop.isPlaying) return;
            var clip = Get(id);
            if (clip == null) return;
            loopId = id; loop.clip = clip; loop.volume = 0.6f * Master; loop.Play();
        }
    }
}
