using System.Collections.Generic;
using Evolve.Creatures;
using Evolve.Survival;
using Evolve.Swallow;
using Evolve.World;
using UnityEngine;

namespace Evolve.Maze
{
    /// <summary>
    /// Bộ điều phối Kỳ Ngộ Tình Huống Môi Trường Động của một khoang (Mưa Axit Bào Tử, Bão Tinh Thể,
    /// Triều Nhiệt & Mạch Phun Ngầm, Rễ Đại Thụ Thức Tỉnh, Hơi Thở Cổ Long).
    /// </summary>
    public class ChamberEncounterZone : MonoBehaviour
    {
        [SerializeField] ChamberEncounterKind kind = ChamberEncounterKind.AcidSporeRain;
        [SerializeField] float phaseOffsetSeconds;
        [SerializeField] float safeCoverRadius = 4.2f;
        [SerializeField] float geyserPadRadius = 3.0f;
        [SerializeField] List<Transform> safeCoverPoints = new List<Transform>();
        [SerializeField] List<Transform> geyserPoints = new List<Transform>();
        [SerializeField] List<Light> situationLights = new List<Light>();

        readonly List<Color> baseLightColors = new List<Color>();
        readonly List<float> baseLightIntensities = new List<float>();

        EncounterPhase lastPhase = EncounterPhase.Calm;
        int lastRegrowCycle = -1;
        float lastLaunchTime = -10f;
        bool appliedPredatorStun;
        EncounterSituationSnapshot currentSnapshot;

        public ChamberEncounterKind Kind => kind;
        public EncounterSituationSnapshot CurrentSnapshot => currentSnapshot;

        /// <summary>Nhảy ngay tới pha Active của tình huống (dùng trong Menu God F1 để test nhanh).</summary>
        public void DebugTriggerNow()
        {
            if (kind == ChamberEncounterKind.None) return;
            float targetPhaseTime = ChamberEncounterRules.CalmSeconds + ChamberEncounterRules.TelegraphSeconds + 0.2f;
            phaseOffsetSeconds = targetPhaseTime - Time.time;
            lastPhase = EncounterPhase.Telegraph;
        }

        public void Configure(
            ChamberEncounterKind encounterKind,
            float offsetSeconds,
            List<Transform> covers,
            List<Transform> geysers,
            List<Light> lights)
        {
            kind = encounterKind;
            phaseOffsetSeconds = offsetSeconds;
            safeCoverPoints = covers ?? new List<Transform>();
            geyserPoints = geysers ?? new List<Transform>();
            situationLights = lights ?? new List<Light>();
            CacheBaseLights();
        }

        void Awake() => CacheBaseLights();

        void OnDisable()
        {
            if (appliedPredatorStun)
            {
                Predator.Frozen = false;
                appliedPredatorStun = false;
            }
        }

        void CacheBaseLights()
        {
            baseLightColors.Clear();
            baseLightIntensities.Clear();
            for (int i = 0; i < situationLights.Count; i++)
            {
                var l = situationLights[i];
                baseLightColors.Add(l != null ? l.color : Color.white);
                baseLightIntensities.Add(l != null ? l.intensity : 1f);
            }
        }

        void Update()
        {
            if (kind == ChamberEncounterKind.None) return;

            var snap = ChamberEncounterRules.EvaluateSituation(kind, Time.time + phaseOffsetSeconds);
            currentSnapshot = snap;

            if (snap.Phase != lastPhase)
            {
                lastPhase = snap.Phase;
                if (!string.IsNullOrEmpty(snap.Announcement))
                    Debug.Log($"[Tình huống hang] {snap.Announcement}");
            }

            UpdateAtmosphereLights(snap);
            UpdatePredatorStun(snap);

            var playerBody = FindLocalPlayerBody();
            if (playerBody == null) return;
            var stats = playerBody.GetComponent<SurvivalStats>();
            if (stats == null || stats.State.IsDead) return;

            // Chỉ áp dụng nếu player thực sự đang ở trong khoang này
            Vector3 playerPos = playerBody.transform.position;
            Vector3 toOrigin = playerPos - transform.parent.position;
            toOrigin.y = 0f;
            if (toOrigin.sqrMagnitude > 55f * 55f) return;

            bool inCover = IsNearAny(playerPos, safeCoverPoints, safeCoverRadius);
            bool inLowBasin = (playerPos.y - transform.parent.position.y) < 0.85f;
            bool onGeyser = IsNearAny(playerPos, geyserPoints, geyserPadRadius);

            var effect = ChamberEncounterRules.EvaluatePlayerInSituation(kind, snap, inCover, inLowBasin, onGeyser);

            if (Mathf.Abs(effect.HealthRatePerSecond) > 0.001f)
            {
                stats.State.Health = Mathf.Clamp(
                    stats.State.Health + effect.HealthRatePerSecond * Time.deltaTime,
                    0.5f,
                    stats.State.MaxHealth);
            }

            if (effect.LaunchImpulseY > 0f && Time.time - lastLaunchTime > 1.8f)
            {
                var rb = playerBody.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    lastLaunchTime = Time.time;
                    Vector3 toCenter = (transform.parent.position - playerPos);
                    toCenter.y = 0f;
                    toCenter = toCenter.sqrMagnitude > 0.1f ? toCenter.normalized : Vector3.forward;
                    rb.AddForce(Vector3.up * effect.LaunchImpulseY + toCenter * 3.5f, ForceMode.VelocityChange);
                }
            }

            if (effect.RegrowFoodAndScatterPrey && lastRegrowCycle != snap.CycleIndex)
            {
                lastRegrowCycle = snap.CycleIndex;
                TriggerRegrowAndScatter();
            }
        }

        void FixedUpdate()
        {
            if (kind == ChamberEncounterKind.None || currentSnapshot.Phase != EncounterPhase.Active) return;

            var playerBody = FindLocalPlayerBody();
            if (playerBody == null) return;

            Vector3 playerPos = playerBody.transform.position;
            Vector3 fromCenter = playerPos - transform.parent.position;
            fromCenter.y = 0f;
            if (fromCenter.sqrMagnitude > 55f * 55f) return;

            bool inCover = IsNearAny(playerPos, safeCoverPoints, safeCoverRadius);
            bool inLowBasin = (playerPos.y - transform.parent.position.y) < 0.85f;
            bool onGeyser = IsNearAny(playerPos, geyserPoints, geyserPadRadius);

            var effect = ChamberEncounterRules.EvaluatePlayerInSituation(kind, currentSnapshot, inCover, inLowBasin, onGeyser);
            if (effect.RadialPushForce > 0f)
            {
                var rb = playerBody.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    Vector3 pushDir = fromCenter.sqrMagnitude > 0.1f ? fromCenter.normalized : Vector3.forward;
                    rb.AddForce(pushDir * effect.RadialPushForce, ForceMode.Acceleration);
                }
            }
        }

        void UpdatePredatorStun(EncounterSituationSnapshot snap)
        {
            bool shouldStun = kind == ChamberEncounterKind.CrystalPulseStorm && snap.Phase == EncounterPhase.Active;
            if (shouldStun && !appliedPredatorStun)
            {
                Predator.Frozen = true;
                appliedPredatorStun = true;
            }
            else if (!shouldStun && appliedPredatorStun)
            {
                Predator.Frozen = false;
                appliedPredatorStun = false;
            }
        }

        void TriggerRegrowAndScatter()
        {
            var nutrients = transform.parent.GetComponentsInChildren<Nutrient>(true);
            for (int i = 0; i < nutrients.Length; i++)
                nutrients[i].ForceRespawn();

            if (kind == ChamberEncounterKind.AwakeningRoots)
            {
                var preys = transform.parent.GetComponentsInChildren<Prey>(false);
                for (int i = 0; i < preys.Length; i++)
                {
                    var rb = preys[i].GetComponent<Rigidbody>();
                    if (rb == null) continue;
                    Vector3 away = preys[i].transform.position - transform.parent.position;
                    away.y = 0f;
                    away = away.sqrMagnitude > 0.1f ? away.normalized : Vector3.forward;
                    rb.MovePosition(GroundHeight.Follow(rb.position + away * 2.5f, 0.15f));
                }
            }
        }

        void UpdateAtmosphereLights(EncounterSituationSnapshot snap)
        {
            Color targetTint = kind switch
            {
                ChamberEncounterKind.AcidSporeRain => new Color(0.75f, 0.28f, 0.95f),
                ChamberEncounterKind.CrystalPulseStorm => new Color(0.28f, 0.85f, 1.0f),
                ChamberEncounterKind.ThermalGeyserSurge => new Color(1.0f, 0.36f, 0.14f),
                ChamberEncounterKind.AwakeningRoots => new Color(0.55f, 1.0f, 0.45f),
                _ => new Color(1.0f, 0.65f, 0.25f)
            };

            for (int i = 0; i < situationLights.Count; i++)
            {
                var l = situationLights[i];
                if (l == null) continue;
                Color baseCol = i < baseLightColors.Count ? baseLightColors[i] : l.color;
                float baseInt = i < baseLightIntensities.Count ? baseLightIntensities[i] : l.intensity;

                if (snap.Phase == EncounterPhase.Telegraph)
                {
                    float blink = 0.65f + 0.55f * Mathf.Sin(Time.time * 10f);
                    l.color = Color.Lerp(baseCol, targetTint, 0.65f);
                    l.intensity = baseInt * blink;
                }
                else if (snap.Phase == EncounterPhase.Active)
                {
                    float pulse = 1.2f + 0.25f * Mathf.Sin(Time.time * 6f);
                    l.color = targetTint;
                    l.intensity = baseInt * pulse;
                }
                else
                {
                    l.color = baseCol;
                    l.intensity = baseInt;
                }
            }
        }

        static SlimeBody FindLocalPlayerBody()
        {
            var bodies = FindObjectsByType<SlimeBody>();
            for (int i = 0; i < bodies.Length; i++)
            {
                var stats = bodies[i].GetComponent<SurvivalStats>();
                if (stats != null && !stats.IsRemote) return bodies[i];
            }
            return bodies.Length > 0 ? bodies[0] : null;
        }

        static bool IsNearAny(Vector3 pos, List<Transform> points, float radius)
        {
            float r2 = radius * radius;
            for (int i = 0; i < points.Count; i++)
            {
                if (points[i] == null) continue;
                Vector3 d = pos - points[i].position;
                d.y = 0f;
                if (d.sqrMagnitude <= r2) return true;
            }
            return false;
        }
    }
}
