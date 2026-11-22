using UnityEngine;

namespace Evolve.Combat
{
    /// <summary>
    /// The boss-fight music (Resources/Music/<boss>_battle.mp3, a loop made with ElevenLabs, one per boss: hound_battle, golem_battle...): plays from the start when a fight begins, keeps going
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
        string wanted = "hound_battle", loaded;

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
        public static void Ping(string track = "hound_battle") { Ensure(); instance.wanted = track; rules.Ping(); }

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
            if (loaded != wanted && !rules.Playing)   // another boss's fight starts: load its track (never swap one that is playing)
            {
                loaded = wanted; clip = Resources.Load<AudioClip>("Music/" + wanted); source.clip = clip;
                if (clip == null) Debug.LogWarning("[Nhạc trùm] Chưa có Resources/Music/" + wanted + ".mp3 (hoặc file là con trỏ Git LFS chưa tải: chạy git lfs pull)");
                else if (clip.length < 1f) Debug.LogWarning("[Nhạc trùm] " + wanted + " nạp được nhưng dài " + clip.length + " s: file hỏng?");
            }
            if (clip == null) return;
            source.volume = rules.Volume * MaxVolume;
            if (rules.Playing && !source.isPlaying) { source.time = 0f; source.Play(); Debug.Log("[Nhạc trùm] phát " + wanted + " (âm lượng " + source.volume.ToString("0.00") + ")"); }   // a new fight starts the track from the top
            else if (!rules.Playing && source.isPlaying) source.Stop();
        }
    }
}
