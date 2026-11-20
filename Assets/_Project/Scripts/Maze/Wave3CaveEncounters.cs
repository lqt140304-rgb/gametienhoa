using Evolve.Traits;
using UnityEngine;

namespace Evolve.Maze
{
    /// <summary>
    /// Hang Nấm Huỳnh Quang — Kỳ ngộ "Vòng Nấm":
    /// Vòng nấm sáng theo nhịp, bào tử bay; đứng giữa vòng lúc bào tử sáng rực -> đổi một Khuyết Điểm.
    /// </summary>
    public sealed class SporeRingEncounter : MonoBehaviour
    {
        public float TriggerRadius = 3.2f;
        public int RerollSeed = 706;
        public Renderer[] RingGlowRenderers;

        SporeRingState _state;
        Transform _player;
        Color[] _baseColors;

        void Awake() => _state = new SporeRingState();

        void OnEnable()
        {
            LightShafts.Register(transform.position, TriggerRadius * 1.1f, transform.position.y + 8f);
        }

        void OnDisable()
        {
            Vector3 c = transform.position;
            LightShafts.Unregister(p => (p - c).sqrMagnitude < 0.1f);
        }

        void Start()
        {
            if (RingGlowRenderers != null && RingGlowRenderers.Length > 0)
            {
                _baseColors = new Color[RingGlowRenderers.Length];
                for (int i = 0; i < RingGlowRenderers.Length; i++)
                    if (RingGlowRenderers[i] != null && RingGlowRenderers[i].sharedMaterial != null)
                        _baseColors[i] = RingGlowRenderers[i].sharedMaterial.color;
            }
        }

        void Update()
        {
            if (_state == null) return;
            if (_player == null)
            {
                var p = GameObject.FindWithTag("Player");
                if (p != null) _player = p.transform;
            }

            float pulse = _state.IsPeakGlow ? 1.8f : 0.35f + 0.45f * Mathf.Sin(_state.Phase01 * Mathf.PI * 2f);
            if (RingGlowRenderers != null && _baseColors != null)
            {
                for (int i = 0; i < RingGlowRenderers.Length; i++)
                {
                    if (RingGlowRenderers[i] == null) continue;
                    RingGlowRenderers[i].material.color = _baseColors[i] * pulse;
                }
            }

            bool inside = _player != null && Vector3.Distance(_player.position, transform.position) <= TriggerRadius;
            if (_state.Tick(Time.deltaTime, inside) && _player != null)
            {
                EncounterRewardBridge.RerollFlaw(_player.gameObject, RerollSeed);
                EncounterRewardBridge.Say("Bào tử huỳnh quang thấm qua lớp màng... một Khuyết Điểm trong ngươi vừa biến chuyển.");
            }
        }
    }

    /// <summary>
    /// Suối Nước Nóng — Kỳ ngộ "Tôi Luyện":
    /// Hơi nóng bốc lên theo đợt; ngâm qua trọn một đợt nóng mà không chết -> tăng chịu nhiệt suốt vòng.
    /// </summary>
    public sealed class HotSpringTemperEncounter : MonoBehaviour
    {
        public float PoolRadius = 4.5f;
        public float HeatBonus = 8f;
        public float SurgeCycleSeconds = 10f;
        public float SurgeActiveSeconds = 4.2f;
        public Renderer SteamGlowRenderer;

        HotSpringTemperState _state;
        Transform _player;
        float _cycleTimer;

        void Awake() => _state = new HotSpringTemperState(SurgeActiveSeconds);

        void OnEnable()
        {
            LightShafts.Register(transform.position, PoolRadius * 1.1f, transform.position.y + 7f);
        }

        void OnDisable()
        {
            Vector3 c = transform.position;
            LightShafts.Unregister(p => (p - c).sqrMagnitude < 0.1f);
        }

        void Update()
        {
            if (_state == null) return;
            _cycleTimer = (_cycleTimer + Time.deltaTime) % SurgeCycleSeconds;
            bool isSurge = _cycleTimer >= (SurgeCycleSeconds - SurgeActiveSeconds);

            if (SteamGlowRenderer != null)
            {
                SteamGlowRenderer.material.color = isSurge
                    ? new Color(1.0f, 0.55f, 0.18f, 1f) * 1.5f
                    : new Color(0.55f, 0.45f, 0.35f, 1f);
            }

            if (_player == null)
            {
                var p = GameObject.FindWithTag("Player");
                if (p != null) _player = p.transform;
            }
            if (_player == null) return;

            bool inWater = Vector3.Distance(_player.position, transform.position) <= PoolRadius;
            if (_state.Tick(Time.deltaTime, inWater, isSurge, playerAlive: true))
            {
                EncounterRewardBridge.AddHeatResistance(_player.gameObject, HeatBonus);
                EncounterRewardBridge.Say("Làn nước sôi tôi luyện lớp da ngoài của ngươi — hơi nóng từ nay khó lòng thiêu đốt.");
            }
        }
    }

    /// <summary>
    /// Khe Gió Hú — Kỳ ngộ "Túi Của Kẻ Đi Trước":
    /// Chỉ slime nhỏ lọt khe hẹp vào vùng Túi của kẻ chuyển sinh trước -> nhận vật phẩm + lời kể Tiếng Vọng.
    /// </summary>
    public sealed class WindGorgeStashEncounter : MonoBehaviour
    {
        public float StashRadius = 2.2f;
        public float MaxSlimeRadius = NarrowCrackStashState.DefaultMaxBodyRadius;

        NarrowCrackStashState _state;
        Transform _player;

        void Awake() => _state = new NarrowCrackStashState(MaxSlimeRadius);

        void OnEnable()
        {
            LightShafts.Register(transform.position, StashRadius * 1.2f, transform.position.y + 6f);
        }

        void OnDisable()
        {
            Vector3 c = transform.position;
            LightShafts.Unregister(p => (p - c).sqrMagnitude < 0.1f);
        }

        void Update()
        {
            if (_state == null || _state.Completed) return;
            if (_player == null)
            {
                var p = GameObject.FindWithTag("Player");
                if (p != null) _player = p.transform;
            }
            if (_player == null) return;

            bool inside = Vector3.Distance(_player.position, transform.position) <= StashRadius;
            float bodyRadius = _player.localScale.x * 0.5f;
            if (_state.TryClaim(inside, bodyRadius))
            {
                EncounterRewardBridge.GiveStomachItems(_player.gameObject, new[] { "Forage_Quartz", "Forage_EmberBerry" });
                EncounterRewardBridge.GrantGene(_player.gameObject, 1);
                EncounterRewardBridge.Say("Trong khe gió rít có một chiếc Túi bỏ lại... Kẻ đi trước đã không kịp ra khỏi Tầng Giữa.");
            }
        }
    }
}
