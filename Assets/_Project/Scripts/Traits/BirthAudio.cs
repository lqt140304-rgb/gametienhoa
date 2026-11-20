using System.Collections.Generic;
using UnityEngine;

namespace Evolve.Traits
{
    /// <summary>
    /// Plays the scene's sound effects from Resources/Sfx/Birth/&lt;id&gt; (made by tools/voice/generate_birth_sfx.sh from BirthSfx). A missing file is silent.
    /// Keeps a low looping hum under the scene, quieter whenever Tiếng Vọng speaks.
    /// </summary>
    internal sealed class BirthAudio : MonoBehaviour
    {
        const float Master = 0.8f, HumLoud = 0.5f, HumQuiet = 0.2f;

        AudioSource effects, hum;
        BirthVoice voice;
        readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();
        bool humOn;

        public void Init(BirthVoice voice)
        {
            this.voice = voice;
            effects = gameObject.AddComponent<AudioSource>();
            effects.spatialBlend = 0f; effects.playOnAwake = false;
            hum = gameObject.AddComponent<AudioSource>();
            hum.spatialBlend = 0f; hum.playOnAwake = false; hum.loop = true; hum.volume = 0f;
        }

        AudioClip Get(string id)
        {
            if (cache.TryGetValue(id, out var c)) return c;
            c = Resources.Load<AudioClip>("Sfx/Birth/" + id);   // null if the file is not there
            cache[id] = c;
            return c;
        }

        public void Play(string id, float volume = 1f)
        {
            var clip = Get(id);
            if (clip != null && effects != null) effects.PlayOneShot(clip, Mathf.Clamp01(volume) * Master);
        }

        public void SetHum(bool on)
        {
            humOn = on;
            if (on && hum != null && !hum.isPlaying)
            {
                var clip = Get("hum_loop");
                if (clip != null) { hum.clip = clip; hum.Play(); }
            }
        }

        public void StopAll()
        {
            humOn = false;
            if (hum != null) hum.Stop();
            if (effects != null) effects.Stop();
        }

        void Update()
        {
            if (hum == null) return;
            float target = !humOn ? 0f : (voice != null && voice.Busy ? HumQuiet : HumLoud);
            hum.volume = Mathf.MoveTowards(hum.volume, target * Master, Time.unscaledDeltaTime * 0.8f);
        }
    }
}
