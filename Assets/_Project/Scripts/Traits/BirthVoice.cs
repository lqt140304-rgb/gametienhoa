using System.Collections.Generic;
using UnityEngine;

namespace Evolve.Traits
{
    /// <summary>
    /// Plays Tiếng Vọng's voice lines. A line is an optional clip in Resources/Voice/Echo/&lt;id&gt; (see docs/art/echo-voice-lines.md): with no clip
    /// nothing plays and the scene is text only. Lines can be queued so a card with two flaws reads both names. Busy tells the scene to wait.
    /// </summary>
    public class BirthVoice : MonoBehaviour
    {
        AudioSource source;
        readonly Queue<AudioClip> queue = new Queue<AudioClip>();

        /// <summary>True while a line is playing or waiting its turn.</summary>
        public bool Busy => queue.Count > 0 || (source != null && source.isPlaying);

        /// <summary>Stop whatever is playing and say this line now.</summary>
        public void Play(string lineId)
        {
            Stop();
            Enqueue(lineId);
        }

        /// <summary>Say this line after the ones already queued.</summary>
        public void Enqueue(string lineId)
        {
            if (string.IsNullOrEmpty(lineId)) return;
            var clip = Resources.Load<AudioClip>("Voice/Echo/" + lineId);
            if (clip != null) queue.Enqueue(clip);
        }

        public void Stop()
        {
            queue.Clear();
            if (source != null) source.Stop();
        }

        void Update()
        {
            if (queue.Count == 0 || (source != null && source.isPlaying)) return;
            if (source == null)
            {
                source = gameObject.AddComponent<AudioSource>();
                source.spatialBlend = 0f;   // 2D: the voice is in the player's head
                source.playOnAwake = false;
            }
            source.clip = queue.Dequeue();
            source.Play();
        }
    }
}
