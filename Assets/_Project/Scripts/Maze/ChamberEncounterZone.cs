using Evolve.Survival;
using Evolve.Swallow;
using UnityEngine;

namespace Evolve.Maze
{
    /// <summary>
    /// Thực thể tương tác cho Kỳ Ngộ hoặc Bẫy môi trường riêng của từng khoang (lấy cảm hứng từ kỳ ngộ TFT).
    /// Tự nạp lại sau mỗi đỉnh Chu Kỳ Lũ.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ChamberEncounterZone : MonoBehaviour
    {
        [SerializeField] ChamberEncounterKind kind = ChamberEncounterKind.AncientFont;
        [SerializeField] Light auraLight;
        [SerializeField] Transform visualRoot;

        bool consumed;
        float lastTriggerTime = -10f;
        float baseLightIntensity = 2f;
        Vector3 baseVisualScale = Vector3.one;
        FloodCycleController floodController;

        public ChamberEncounterKind Kind => kind;
        public bool IsConsumed => consumed;

        public void Configure(ChamberEncounterKind encounterKind, Light lightComp, Transform visual)
        {
            kind = encounterKind;
            auraLight = lightComp;
            visualRoot = visual;
            if (auraLight != null) baseLightIntensity = auraLight.intensity;
            if (visualRoot != null) baseVisualScale = visualRoot.localScale;
        }

        void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
            if (auraLight != null) baseLightIntensity = auraLight.intensity;
            if (visualRoot != null) baseVisualScale = visualRoot.localScale;
        }

        void OnEnable()
        {
            floodController = FindAnyObjectByType<FloodCycleController>();
            if (floodController != null) floodController.FloodPeakReached += OnFloodPeak;
        }

        void OnDisable()
        {
            if (floodController != null) floodController.FloodPeakReached -= OnFloodPeak;
        }

        void OnFloodPeak(int cycleIndex)
        {
            consumed = false;
            if (visualRoot != null) visualRoot.localScale = baseVisualScale;
        }

        void Update()
        {
            if (auraLight == null) return;
            if (consumed)
            {
                auraLight.intensity = baseLightIntensity * 0.15f;
                return;
            }

            if (kind == ChamberEncounterKind.UndergroundGeyser || kind == ChamberEncounterKind.ThermalVentTrap)
            {
                bool active = ChamberEncounterRules.IsCycleActive(kind, Time.time);
                float pulse = active ? 2.1f + 0.6f * Mathf.Sin(Time.time * 18f) : 0.45f + 0.25f * Mathf.Sin(Time.time * 4f);
                auraLight.intensity = baseLightIntensity * pulse;
            }
            else
            {
                auraLight.intensity = baseLightIntensity * (0.85f + 0.25f * Mathf.Sin(Time.time * 2.4f));
            }
        }

        void OnTriggerStay(Collider other)
        {
            if (Time.time - lastTriggerTime < 1.2f) return;

            var body = other.GetComponentInParent<SlimeBody>();
            var stats = other.GetComponentInParent<SurvivalStats>();
            if (body == null || stats == null || stats.IsRemote || stats.State.IsDead) return;

            var outcome = ChamberEncounterRules.Evaluate(
                kind,
                stats.State.Health,
                stats.State.MaxHealth,
                body.Mass,
                consumed,
                Time.time);

            if (!outcome.Triggered)
            {
                if (!string.IsNullOrEmpty(outcome.Message) && Time.time - lastTriggerTime > 3f)
                {
                    lastTriggerTime = Time.time;
                    Debug.Log($"[Kỳ Ngộ] {outcome.Message}");
                }
                return;
            }

            lastTriggerTime = Time.time;
            if (outcome.Consumed)
            {
                consumed = true;
                if (visualRoot != null) visualRoot.localScale = baseVisualScale * 0.45f;
            }

            if (Mathf.Abs(outcome.HealthDelta) > 0.001f)
            {
                stats.State.Health = Mathf.Clamp(stats.State.Health + outcome.HealthDelta, 1f, stats.State.MaxHealth);
            }

            if (outcome.MassGain > 0f)
            {
                body.AddMass(outcome.MassGain);
            }

            if (outcome.ImpulseY > 0f || outcome.ImpulseRadial > 0f)
            {
                var rb = body.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    Vector3 radial = body.transform.position - transform.position;
                    radial.y = 0f;
                    radial = radial.sqrMagnitude > 0.01f ? radial.normalized : transform.forward;
                    rb.AddForce(radial * outcome.ImpulseRadial + Vector3.up * outcome.ImpulseY, ForceMode.VelocityChange);
                }
            }

            if (!string.IsNullOrEmpty(outcome.Message))
                Debug.Log($"[Mê cung] {outcome.Message}");
        }
    }
}
