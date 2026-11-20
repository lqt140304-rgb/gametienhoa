using System.Collections.Generic;
using Evolve.Creatures;
using Evolve.Survival;
using UnityEngine;

namespace Evolve.World
{
    /// <summary>
    /// Substitute sense for the eyeless stage. Press C to send out a vibration wave:
    /// it spreads outward, lifts the fog for a moment (MonochromeVision.Reveal) and makes
    /// every Nutrient (cyan), Prey (red) and Predator (purple) it touches flash once.
    /// Costs energy and has a cooldown. Assign a ParticleSystem prefab (short burst, not looping).
    /// </summary>
    public class VibrationSense : MonoBehaviour
    {
        [SerializeField] float radius = 25f;
        [Tooltip("Seconds for the wave to reach full radius")]
        [SerializeField] float expandSeconds = 1f;
        [Tooltip("Seconds the revealed space stays visible after the wave")]
        [SerializeField] float holdSeconds = 0.5f;
        [Tooltip("Seconds for the fog to close in again")]
        [SerializeField] float fadeSeconds = 1f;
        [SerializeField] float energyCost = 10f;
        [SerializeField] float cooldown = 5f;
        [SerializeField] ParticleSystem pulsePrefab;
        [SerializeField] Color foodColor = new Color(0.5f, 0.9f, 1f);
        [SerializeField] Color preyColor = new Color(1f, 0.4f, 0.35f);
        [SerializeField] Color predatorColor = new Color(0.75f, 0.35f, 1f);

        SurvivalStats stats;
        MonochromeVision vision;
        readonly HashSet<Transform> flashed = new HashSet<Transform>();
        /// <summary>The scan reaches this fraction of the serialized radius (half: the wave is a ring now, not a lit disc).</summary>
        public const float ScanRadiusFactor = 0.5f;
        /// <summary>The wave takes this many times the serialized expand time to cross its radius, and the echo on food lasts at least <see cref="MinEchoSeconds"/>.</summary>
        public const float ExpandFactor = 2.5f;
        public const float MinEchoSeconds = 2.2f;
        float scanExpand;
        float scanStart = -1f;
        float scanRadius;
        float readyAt;

        [Header("Passive")]
        [SerializeField] float passiveRadius = 30f;
        readonly Dictionary<VibrationEmitter, int> seenPulses = new Dictionary<VibrationEmitter, int>();

        public float EnergyCost => energyCost;
        public float CooldownRemaining => Mathf.Max(0f, readyAt - Time.time);
        public bool IsScanning => scanStart >= 0f;

        // Stress narrows the senses; applied at the point of use so the serialized radii stay untouched.
        // Low Tinh Chất Sống (vitamin) dulls the senses too.
        float SenseMul => stats != null && stats.Config != null
            ? StressEffects.SenseRadiusMultiplier(stats.State.Stress, stats.Config)
              * NutrientEffects.SenseMultiplier(stats.State.Nutrient(NutrientKind.Vitamin))
              * DiseaseRules.SenseMultiplier(stats.State.Has(DiseaseKind.SporeBlur)) : 1f;

        void Awake()
        {
            stats = GetComponent<SurvivalStats>();
        }

        void Update()
        {
            if (stats != null && stats.State.IsDead) { scanStart = -1f; return; }

            if (!SurvivalHUD.BlocksInput(gameObject) && KeyPressed.C() && Time.time >= readyAt && (stats == null || stats.State.TrySpendEnergy(energyCost)))
                StartScan();

            if (scanStart >= 0f) UpdateWave();
            UpdatePassive();
        }

        // Free listening: new pulses from emitters within passiveRadius show a faint ring, fainter with distance.
        void UpdatePassive()
        {
            if (pulsePrefab == null) return;
            var list = VibrationEmitter.Active;
            if (seenPulses.Count > 256) seenPulses.Clear();
            float pr = passiveRadius * SenseMul;
            float r2 = pr * pr;
            for (int i = 0; i < list.Count; i++)
            {
                var e = list[i];
                if (e == null || e.transform.IsChildOf(transform)) continue;
                seenPulses.TryGetValue(e, out int seen);
                if (e.PulseCount == seen) continue;
                seenPulses[e] = e.PulseCount;
                if (Time.time - e.LastPulseTime > 0.25f) continue; // stale (e.g. dictionary was reset)

                Vector3 at = e.transform.position + Vector3.up * 0.5f;
                float sq = (at - transform.position).sqrMagnitude;
                if (sq > r2) continue;
                float intensity = e.LastPulseStrength * (1f - Mathf.Sqrt(sq) / pr);
                if (intensity <= 0.02f) continue;

                Color c = e.IsPredator ? predatorColor : preyColor;
                c.a = Mathf.Clamp01(intensity);
                var p = Instantiate(pulsePrefab, at, Quaternion.identity);
                var main = p.main;
                main.startColor = c;
                Destroy(p.gameObject, main.duration + main.startLifetime.constantMax);
            }
        }

        void StartScan()
        {
            if (vision == null) vision = FindAnyObjectByType<MonochromeVision>();
            scanRadius = radius * ScanRadiusFactor * SenseMul;   // the sound wave reaches half as far as the old light scan
            scanStart = Time.time;
            readyAt = Time.time + cooldown;
            flashed.Clear();
            scanExpand = expandSeconds * ExpandFactor;
            if (vision != null) vision.Reveal(scanRadius, scanExpand, Mathf.Max(holdSeconds, MinEchoSeconds * 0.6f), Mathf.Max(fadeSeconds, MinEchoSeconds * 0.5f));
        }

        void UpdateWave()
        {
            float t = (Time.time - scanStart) / Mathf.Max(0.01f, scanExpand);
            float r = Mathf.Min(1f, t) * scanRadius;

            // no particles puffing out of the things the wave passes: the wave itself shows their shape (Shaders/SonarShell)

            if (t >= 1f) scanStart = -1f;
        }

        void TryFlash(Transform target, Vector3 offset, float waveRadius, Color c)
        {
            if (pulsePrefab == null || flashed.Contains(target)) return;
            Vector3 at = target.position + offset;
            if ((at - transform.position).sqrMagnitude > waveRadius * waveRadius) return;
            flashed.Add(target);

            var p = Instantiate(pulsePrefab, at, Quaternion.identity);
            var main = p.main;
            main.startColor = c;
            Destroy(p.gameObject, main.duration + main.startLifetime.constantMax);
        }
    }
}
