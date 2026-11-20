using System;
using System.Collections.Generic;
using Evolve.Swallow;
using Evolve.Traits;
using Evolve.Visuals;
using Evolve.World;
using UnityEngine;

namespace Evolve.Maze
{
    /// <summary>Đăng ký các cột tia sáng trong hang với Evolve.Traits.LightShafts.</summary>
    public class ChamberLightShaftRegistrar : MonoBehaviour
    {
        [Serializable]
        public struct ShaftSpec
        {
            public Vector3 localCenter;
            public float radius;
            public float topLocalY;
        }

        [SerializeField] List<ShaftSpec> shafts = new List<ShaftSpec>();
        readonly List<Func<Vector3, bool>> handles = new List<Func<Vector3, bool>>();

        public void Configure(List<ShaftSpec> specs)
        {
            shafts = specs ?? new List<ShaftSpec>();
            if (isActiveAndEnabled)
            {
                UnregisterAll();
                RegisterAll();
            }
        }

        void OnEnable() => RegisterAll();

        void OnDisable() => UnregisterAll();

        void RegisterAll()
        {
            for (int i = 0; i < shafts.Count; i++)
            {
                var s = shafts[i];
                Vector3 worldCenter = transform.TransformPoint(s.localCenter);
                float worldTopY = transform.position.y + s.topLocalY;
                handles.Add(LightShafts.Register(worldCenter, Mathf.Max(0.5f, s.radius), worldTopY));
            }
        }

        void UnregisterAll()
        {
            for (int i = 0; i < handles.Count; i++)
                LightShafts.Unregister(handles[i]);
            handles.Clear();
        }
    }

    /// <summary>Kỳ ngộ Hang Khởi Sinh: Tia Nắng Đầu Tiên.</summary>
    public class FirstSunbeamEncounter : MonoBehaviour
    {
        [SerializeField] float radius = 5.5f;
        [SerializeField] float topLocalY = 26f;

        readonly CaveEncounterWave1Rules.FirstSunbeamState state = new CaveEncounterWave1Rules.FirstSunbeamState();
        Func<Vector3, bool> shaftHandle;
        AudioSource chimeSource;

        public void Configure(float beamRadius, float topY)
        {
            radius = beamRadius;
            topLocalY = topY;
        }

        void OnEnable()
        {
            shaftHandle = LightShafts.Register(transform.position, radius, transform.position.y + topLocalY);
        }

        void OnDisable()
        {
            if (shaftHandle != null)
            {
                LightShafts.Unregister(shaftHandle);
                shaftHandle = null;
            }
        }

        void Update()
        {
            if (state.Triggered) return;
            var player = FindLocalPlayer();
            if (player == null) return;

            Vector3 delta = player.transform.position - transform.position;
            float dist2 = delta.x * delta.x + delta.z * delta.z;
            bool inBeam = dist2 <= radius * radius && player.transform.position.y <= transform.position.y + topLocalY;

            if (state.TryTrigger(inBeam))
            {
                PlayChime(523.25f, 0.45f);
                EncounterRewardBridge.Say("Ánh nắng đầu tiên chạm vào cơ thể mềm mại của ngươi. Hãy nuốt rêu và sinh vật nhỏ để lớn lên.");
                EncounterRewardBridge.RefillStats(player.gameObject, 22f);
            }
        }

        static SlimeBody FindLocalPlayer()
        {
            var bodies = FindObjectsByType<SlimeBody>();
            for (int i = 0; i < bodies.Length; i++)
                if (bodies[i] != null && bodies[i].gameObject.activeInHierarchy) return bodies[i];
            return null;
        }

        void PlayChime(float freq, float duration)
        {
            if (chimeSource == null) chimeSource = gameObject.AddComponent<AudioSource>();
            chimeSource.spatialBlend = 0.25f;
            chimeSource.volume = 0.35f;
            chimeSource.PlayOneShot(ProceduralToneClip.CreateSine(freq, duration));
        }
    }

    /// <summary>Kỳ ngộ Vườn Quặng Cầu Vồng: Cầu Vồng Trong Thân.</summary>
    public class RainbowInBodyEncounter : MonoBehaviour
    {
        readonly CaveEncounterWave1Rules.RainbowBodyState state = new CaveEncounterWave1Rules.RainbowBodyState();
        float rainbowShiftTimer;
        Color finalUnlockedColor = new Color(0.32f, 0.92f, 0.88f, 1f);

        public bool NotifyOreSwallowed(int colorIndex, GameObject player)
        {
            bool added = state.RecordOreSwallowed(colorIndex);
            if (added)
            {
                Debug.Log($"[Kỳ Ngộ - Vườn Quặng] Đã nuốt quặng màu #{colorIndex + 1} ({state.AbsorbedCount}/{CaveEncounterWave1Rules.RequiredRainbowColors}).");
                if (state.HasAllColors)
                    EncounterRewardBridge.Say("Đủ năm sắc quặng đang chảy trong thân... hãy ngâm mình xuống dòng suối.");
            }
            return added;
        }

        public void NotifyPlayerInStream(GameObject player)
        {
            if (!state.TryCompleteInStream(true)) return;

            rainbowShiftTimer = 4.5f;

            EncounterRewardBridge.UnlockColor(player, finalUnlockedColor);
            EncounterRewardBridge.GrantGene(player, 1);
            EncounterRewardBridge.Say("Dòng suối gột rửa lớp vỏ, ánh cầu vồng bừng sáng từ bên trong thân ngươi.");
        }

        void Update()
        {
            if (rainbowShiftTimer <= 0f) return;
            rainbowShiftTimer -= Time.deltaTime;
            if (rainbowShiftTimer <= 0f)
            {
                SlimeColor.Set(SlimePart.Body, finalUnlockedColor);
                return;
            }
            float h = Mathf.Repeat(Time.time * 0.65f, 1f);
            SlimeColor.Set(SlimePart.Body, Color.HSVToRGB(h, 0.72f, 0.95f));
        }
    }

    /// <summary>Mảnh quặng màu phát sáng trong Vườn Quặng Cầu Vồng.</summary>
    [RequireComponent(typeof(Collider))]
    public class RainbowOrePiece : MonoBehaviour
    {
        [SerializeField] int colorIndex;
        [SerializeField] RainbowInBodyEncounter owner;
        bool swallowed;

        public void Configure(int index, RainbowInBodyEncounter encounter)
        {
            colorIndex = index;
            owner = encounter;
        }

        void Awake() => GetComponent<Collider>().isTrigger = true;

        void OnTriggerEnter(Collider other)
        {
            if (swallowed) return;
            var body = other.GetComponentInParent<SlimeBody>();
            if (body == null) return;

            swallowed = true;
            if (owner != null) owner.NotifyOreSwallowed(colorIndex, body.gameObject);
            gameObject.SetActive(false);
        }
    }

    /// <summary>Vùng suối trong Vườn Quặng Cầu Vồng để hoàn tất kỳ ngộ Cầu Vồng Trong Thân.</summary>
    [RequireComponent(typeof(Collider))]
    public class RainbowStreamZone : MonoBehaviour
    {
        [SerializeField] RainbowInBodyEncounter owner;

        public void Configure(RainbowInBodyEncounter encounter) => owner = encounter;

        void Awake() => GetComponent<Collider>().isTrigger = true;

        void OnTriggerStay(Collider other)
        {
            if (owner == null) return;
            var body = other.GetComponentInParent<SlimeBody>();
            if (body != null) owner.NotifyPlayerInStream(body.gameObject);
        }
    }

    /// <summary>Kỳ ngộ Vực Nứt Tím: Hợp Âm Tinh Thể (Sóng Dò vào 3 tinh thể từ Trầm lên Cao).</summary>
    public class CrystalChordEncounter : MonoBehaviour
    {
        [SerializeField] GameObject coreCrystalReward;
        [SerializeField] Light fissureCoreLight;

        readonly CaveEncounterWave1Rules.CrystalChordState state = new CaveEncounterWave1Rules.CrystalChordState();
        AudioSource sfx;

        public void Configure(GameObject coreCrystal, Light coreLight)
        {
            coreCrystalReward = coreCrystal;
            fissureCoreLight = coreLight;
            if (coreCrystalReward != null) coreCrystalReward.SetActive(false);
            if (fissureCoreLight != null) fissureCoreLight.enabled = false;
        }

        public void OnCrystalHitBySonar(int pitchIndex)
        {
            var result = state.HitBySonar(pitchIndex);
            float[] freqs = { 196.00f, 293.66f, 440.00f }; // G3 (Trầm), D4 (Trung), A4 (Cao)
            float pitchFreq = freqs[Mathf.Clamp(pitchIndex, 0, freqs.Length - 1)];

            if (result == ChordStepResult.ResetWrongOrder)
            {
                PlayTone(110f, 0.30f);
                Debug.Log("[Kỳ Ngộ - Vực Nứt Tím] Sai cao độ hợp âm tinh thể, thứ tự đã đặt lại.");
                return;
            }

            PlayTone(pitchFreq, 0.55f);
            if (result == ChordStepResult.Completed)
            {
                if (coreCrystalReward != null) coreCrystalReward.SetActive(true);
                if (fissureCoreLight != null) fissureCoreLight.enabled = true;
                EncounterRewardBridge.Say("Ba tinh thể ngân vang hòa nhịp — vết nứt lớn toác ra, để lộ tinh thể lõi sáng rực.");
            }
        }

        public void NotifyCoreSwallowed(GameObject player)
        {
            if (!state.TrySwallowCore()) return;
            if (coreCrystalReward != null) coreCrystalReward.SetActive(false);
            EncounterRewardBridge.GrantGene(player, 2); // Rare+
            EncounterRewardBridge.Say("Tinh thể lõi tan vào thân ngươi với luồng gene tinh khiết.");
        }

        void PlayTone(float freq, float duration)
        {
            if (sfx == null) sfx = gameObject.AddComponent<AudioSource>();
            sfx.spatialBlend = 0.35f;
            sfx.volume = 0.40f;
            sfx.PlayOneShot(ProceduralToneClip.CreateSine(freq, duration));
        }
    }

    /// <summary>Một trong 3 trụ tinh thể cộng hưởng (Trầm=0, Trung=1, Cao=2) phản hồi khi trúng Sóng Dò.</summary>
    public class CrystalChordResonator : MonoBehaviour
    {
        [SerializeField] int pitchIndex;
        [SerializeField] CrystalChordEncounter encounter;
        [SerializeField] float cooldownSeconds = 0.45f;
        float lastHitTime = -10f;

        public void Configure(int pitch, CrystalChordEncounter owner)
        {
            pitchIndex = pitch;
            encounter = owner;
        }

        void OnEnable() => MonochromeVision.SonarPulsed += HandleSonarPulsed;

        void OnDisable() => MonochromeVision.SonarPulsed -= HandleSonarPulsed;

        void HandleSonarPulsed(Vector3 origin, float maxRadius)
        {
            if (encounter == null || Time.time - lastHitTime < cooldownSeconds) return;
            float dist = Vector3.Distance(origin, transform.position);
            if (dist <= Mathf.Max(14f, maxRadius))
            {
                lastHitTime = Time.time;
                encounter.OnCrystalHitBySonar(pitchIndex);
            }
        }
    }

    /// <summary>Tinh thể lõi lộ ra sau khi hoàn tất Hợp Âm Tinh Thể.</summary>
    [RequireComponent(typeof(Collider))]
    public class CrystalCorePickup : MonoBehaviour
    {
        [SerializeField] CrystalChordEncounter encounter;

        public void Configure(CrystalChordEncounter owner) => encounter = owner;

        void Awake() => GetComponent<Collider>().isTrigger = true;

        void OnTriggerEnter(Collider other)
        {
            if (encounter == null) return;
            var body = other.GetComponentInParent<SlimeBody>();
            if (body != null) encounter.NotifyCoreSwallowed(body.gameObject);
        }
    }

    static class ProceduralToneClip
    {
        public static AudioClip CreateSine(float frequency, float duration)
        {
            int sampleRate = 22050;
            int samples = Mathf.Max(1, Mathf.RoundToInt(sampleRate * duration));
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)sampleRate;
                float env = Mathf.Clamp01(1f - i / (float)samples);
                data[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * env * 0.6f;
            }
            var clip = AudioClip.Create($"Chime_{frequency:0}", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
