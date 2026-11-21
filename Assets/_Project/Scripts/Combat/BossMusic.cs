using UnityEngine;

namespace Evolve.Combat
{
    /// <summary>
    /// The boss-fight music (Resources/Music/hound_battle.mp3, a loop made with ElevenLabs): plays from the start when a fight begins, keeps going
    /// while blows are traded, fades out and stops after 10 quiet seconds, comes back on the next blow, and cuts out quickly when the boss dies.
    /// When it plays and when it fades is <see cref="BossMusicRules"/>. One music player for the whole game; the bosses call <see cref="Ping"/>.
    /// </summary>
    public class BossMusic : MonoBehaviour
    {
        /// <summary>Loudness of the loop at full (the sound effects are louder, so the fight stays readable).</summary>
        public const float MaxVolume = 0.5f;

        static BossMusic instance;
        static BossMusicRules rules = new BossMusicRules();
        AudioSource source;
        AudioClip clip;
        bool triedLoad;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { instance = null; rules = new BossMusicRules(); }

        static void Ensure()
        {
            if (instance != null) return;
            var go = new GameObject("BossMusic");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<BossMusic>();
        }

        /// <summary>A blow in the fight (a boss attacking or being hurt).</summary>
        public static void Ping() { Ensure(); rules.Ping(); }

        /// <summary>The boss died: the music fades out within <paramref name="seconds"/>.</summary>
        public static void Silence(float seconds) { if (instance != null) rules.Silence(seconds); }

        void Awake()
        {
            source = gameObject.AddComponent<AudioSource>();
            source.loop = true; source.playOnAwake = false; source.spatialBlend = 0f; source.volume = 0f;
        }

        void Update()
        {
            rules.Tick(Time.unscaledDeltaTime);
            if (!triedLoad) { triedLoad = true; clip = Resources.Load<AudioClip>("Music/hound_battle"); source.clip = clip; if (clip == null) Debug.LogWarning("[Nhạc trùm] Chưa có Resources/Music/hound_battle.mp3"); }
            if (clip == null) return;
            source.volume = rules.Volume * MaxVolume;
            if (rules.Playing && !source.isPlaying) { source.time = 0f; source.Play(); }   // a new fight starts the track from the top
            else if (!rules.Playing && source.isPlaying) source.Stop();
        }
    }
}
