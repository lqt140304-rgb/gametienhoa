using System;
using Evolve.Swallow;
using Evolve.World;
using UnityEngine;

namespace Evolve.Maze
{
    /// <summary>
    /// Điều khiển Chu Kỳ Lũ trong scene: cập nhật mực nước, phát cảnh báo (tiếng ầm, nước rỉ),
    /// kiểm tra slime ngoài chỗ cao để trừ khối và cuốn sang khoang khác.
    /// </summary>
    public class FloodCycleController : MonoBehaviour
    {
        [Header("Thời gian Chu Kỳ Lũ (giây)")]
        [SerializeField] float cycleDurationSeconds = 720f;
        [SerializeField] float warningLeadSeconds = 60f;
        [SerializeField] float riseDurationSeconds = 60f;
        [SerializeField] float peakDurationSeconds = 90f;
        [SerializeField] float recedeDurationSeconds = 60f;

        [Header("Mực nước (m)")]
        [SerializeField] float baseWaterLevel = CavernLayout.WaterLevel;
        [SerializeField] float peakWaterLevel = 4.5f;
        [SerializeField] float highLedgeSafeHeight = 5.2f;
        [SerializeField] float massLossRatio = 0.20f;
        [SerializeField] float minMassFloor = 5f;
        [SerializeField] float washAwayCooldownSeconds = 8f;
        [SerializeField] float currentMaxForce = 18f;
        [SerializeField] float sweepSecondsBeforeWash = 2.5f;

        [Header("Tham chiếu trong scene (tùy chọn)")]
        [SerializeField] Transform floodWaterSurface;
        [SerializeField] AudioSource rumbleAudio;
        [SerializeField] ParticleSystem wallSeepParticles;

        float elapsedSeconds;
        float washCooldown;
        float sweptTimer;
        FloodPhase lastPhase = FloodPhase.Calm;
        int lastPeakCycle = -1;

        public FloodSnapshot CurrentSnapshot { get; private set; }
        public float CurrentWaterLevel => CurrentSnapshot.WaterLevel;
        public FloodPhase CurrentPhase => CurrentSnapshot.Phase;

        public event Action<FloodPhase> PhaseChanged;
        public event Action<float> WarningUpdated;
        public event Action<int> FloodPeakReached;
        public event Action<int, int, float> PlayerWashedAway;

        public FloodConfig Config => new FloodConfig(
            cycleDurationSeconds,
            warningLeadSeconds,
            riseDurationSeconds,
            peakDurationSeconds,
            recedeDurationSeconds,
            baseWaterLevel,
            peakWaterLevel,
            highLedgeSafeHeight,
            massLossRatio,
            minMassFloor);

        void OnEnable()
        {
            ApplyWaterField(baseWaterLevel);
        }

        void OnDisable()
        {
            WaterField.Reset();
        }

        void Update()
        {
            elapsedSeconds += Time.deltaTime;
            if (washCooldown > 0f) washCooldown -= Time.deltaTime;

            var cfg = Config;
            var snap = FloodCycleRules.Evaluate(elapsedSeconds, cfg);
            CurrentSnapshot = snap;

            if (snap.Phase != lastPhase)
            {
                lastPhase = snap.Phase;
                PhaseChanged?.Invoke(snap.Phase);
            }

            if (snap.Phase == FloodPhase.Peak && snap.CycleIndex != lastPeakCycle)
            {
                lastPeakCycle = snap.CycleIndex;
                FloodPeakReached?.Invoke(snap.CycleIndex);
            }

            UpdateVisualAndAudio(snap);
            ApplyWaterField(snap.WaterLevel);
            CheckPlayerFloodCatch(snap, cfg);
        }

        /// <summary>Đặt nhanh thời gian chu kỳ (dùng khi test hoặc debug).</summary>
        public void SetElapsedSeconds(float seconds) => elapsedSeconds = Mathf.Max(0f, seconds);

        void UpdateVisualAndAudio(FloodSnapshot snap)
        {
            if (floodWaterSurface != null)
            {
                var p = floodWaterSurface.position;
                var manager = FindFirstObjectByType<MazeWorldManager>();
                if (manager != null && manager.CurrentChamber != null)
                {
                    var origin = manager.CurrentChamber.transform.position;
                    p.x = origin.x;
                    p.z = origin.z;
                }
                p.y = snap.WaterLevel;
                floodWaterSurface.position = p;
            }

            if (snap.Phase == FloodPhase.Warning || snap.Phase == FloodPhase.Rising)
            {
                WarningUpdated?.Invoke(snap.WarningIntensity);
                if (rumbleAudio != null)
                {
                    if (!rumbleAudio.isPlaying) rumbleAudio.Play();
                    rumbleAudio.volume = Mathf.Clamp01(0.15f + 0.65f * snap.WarningIntensity);
                }
                if (wallSeepParticles != null && !wallSeepParticles.isPlaying)
                    wallSeepParticles.Play();
            }
            else
            {
                if (rumbleAudio != null && rumbleAudio.isPlaying) rumbleAudio.Stop();
                if (wallSeepParticles != null && wallSeepParticles.isPlaying) wallSeepParticles.Stop();
            }
        }

        void ApplyWaterField(float waterLevel)
        {
            // Giữ nguyên hồ ở đáy Hang Khởi Sinh, đồng thời nâng mực nước mặt khi lũ dâng
            WaterField.Use(
                (x, z) =>
                {
                    float lakeDepth = CavernLayout.DepthAt(x, z);
                    float floodDepth = Mathf.Max(0f, waterLevel - CavernLayout.GroundHeight(x, z));
                    return Mathf.Max(lakeDepth, floodDepth);
                },
                (x, z) =>
                {
                    float lakeDepth = CavernLayout.DepthAt(x, z);
                    float floodDepth = Mathf.Max(0f, waterLevel - CavernLayout.GroundHeight(x, z));
                    return Mathf.Max(lakeDepth, floodDepth);
                },
                waterLevel);
        }

        void FixedUpdate()
        {
            var cfg = Config;
            var snap = CurrentSnapshot;
            if (washCooldown > 0f || !snap.IsFloodActive)
            {
                sweptTimer = 0f;
                return;
            }

            var sensor = FindFirstObjectByType<ShelterSensor>();
            if (sensor == null) return;

            if (!FloodCycleRules.IsCaughtByFlood(sensor.transform.position.y, sensor.InShelter, snap, cfg))
            {
                sweptTimer = 0f;
                return;
            }

            // Đẩy vật lý slime theo dòng nước về phía hầm thấp trước khi bị cuốn sang khoang khác
            Vector3 drainPos = FindLowestDrainPosition(sensor.transform.position);
            var (dx, dz, force) = FloodCycleRules.ComputeCurrentPush(
                sensor.transform.position.x,
                sensor.transform.position.z,
                drainPos.x,
                drainPos.z,
                snap.WaterLevel,
                cfg.BaseWaterLevel,
                cfg.PeakWaterLevel,
                currentMaxForce);

            var rb = sensor.GetComponent<Rigidbody>();
            if (rb != null && force > 0f)
                rb.AddForce(new Vector3(dx * force, 0f, dz * force), ForceMode.Acceleration);
        }

        void CheckPlayerFloodCatch(FloodSnapshot snap, FloodConfig cfg)
        {
            if (washCooldown > 0f || !snap.IsFloodActive) return;

            var sensor = FindFirstObjectByType<ShelterSensor>();
            if (sensor == null) return;

            float playerY = sensor.transform.position.y;
            if (!FloodCycleRules.IsCaughtByFlood(playerY, sensor.InShelter, snap, cfg))
            {
                sweptTimer = 0f;
                return;
            }

            sweptTimer += Time.deltaTime;
            Vector3 drainPos = FindLowestDrainPosition(sensor.transform.position);
            float distToDrain = Vector2.Distance(
                new Vector2(sensor.transform.position.x, sensor.transform.position.z),
                new Vector2(drainPos.x, drainPos.z));

            if (sweptTimer < sweepSecondsBeforeWash && distToDrain > 3.0f) return;

            sweptTimer = 0f;
            washCooldown = washAwayCooldownSeconds;

            float lost = 0f;
            var body = sensor.GetComponent<SlimeBody>();
            if (body != null)
            {
                lost = FloodCycleRules.CalculateMassLoss(body.Mass, cfg);
                if (lost > 0f) body.TrySpend(lost);
            }

            var manager = FindFirstObjectByType<MazeWorldManager>();
            if (manager != null && manager.Graph != null)
            {
                int fromId = manager.CurrentChamberId;
                int toId = FloodCycleRules.PickWashAwayChamber(manager.Graph, fromId, manager.Seed, snap.CycleIndex);
                manager.TeleportPlayerToChamber(sensor.transform, toId);
                PlayerWashedAway?.Invoke(fromId, toId, lost);
                Debug.Log($"Lũ cuốn! Mất {lost:0.#} khối và trôi từ khoang {fromId} sang khoang {toId}.");
            }
            else
            {
                // Nếu chỉ có Hang Khởi Sinh chưa bật MazeWorldManager: đẩy lên bờ cao gần đó
                sensor.transform.position = CavernLayout.SpawnPoint() + Vector3.up * cfg.HighLedgeSafeHeight;
                PlayerWashedAway?.Invoke(0, 0, lost);
            }
        }

        Vector3 FindLowestDrainPosition(Vector3 fallbackPos)
        {
            var manager = FindFirstObjectByType<MazeWorldManager>();
            var chamber = manager != null ? manager.CurrentChamber : null;
            if (chamber == null || chamber.Portals.Count == 0)
                return fallbackPos + Vector3.forward * 6f;

            Vector3 best = chamber.Portals[0].transform.position;
            float lowestY = best.y;
            for (int i = 1; i < chamber.Portals.Count; i++)
            {
                var p = chamber.Portals[i];
                if (p != null && p.transform.position.y < lowestY)
                {
                    lowestY = p.transform.position.y;
                    best = p.transform.position;
                }
            }
            return best;
        }
    }
}
