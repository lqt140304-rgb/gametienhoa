using UnityEngine;

namespace Evolve.Traits
{
    /// <summary>
    /// Plays Tiếng Vọng's voice lines. A line is an optional clip in Resources/Voice/Echo/&lt;id&gt; (see docs/art/echo-voice-lines.md):
    /// with no clip nothing plays and the scene is text only.
    /// </summary>
    public class BirthVoice : MonoBehaviour
    {
        AudioSource source;

        public void Play(string lineId)
        {
            var clip = Resources.Load<AudioClip>("Voice/Echo/" + lineId);
            if (clip == null) return;
            if (source == null)
            {
                source = gameObject.AddComponent<AudioSource>();
                source.spatialBlend = 0f;   // 2D: the voice is in the player's head
                source.playOnAwake = false;
            }
            source.Stop();
            source.PlayOneShot(clip);
        }
    }
}
