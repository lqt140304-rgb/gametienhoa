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
        float scanStart = -1f;
        float readyAt;

        public float EnergyCost => energyCost;
        public float CooldownRemaining => Mathf.Max(0f, readyAt - Time.time);
        public bool IsScanning => scanStart >= 0f;

        void Awake()
        {
            stats = GetComponent<SurvivalStats>();
        }

        void Update()
        {
            if (stats != null && stats.State.IsDead) { scanStart = -1f; return; }

            if (KeyPressed.C() && Time.time >= readyAt && (stats == null || stats.State.TrySpendEnergy(energyCost)))
                StartScan();

            if (scanStart >= 0f) UpdateWave();
        }

        void StartScan()
        {
            if (vision == null) vision = FindAnyObjectByType<MonochromeVision>();
            scanStart = Time.time;
            readyAt = Time.time + cooldown;
            flashed.Clear();
            if (vision != null) vision.Reveal(radius, expandSeconds, holdSeconds, fadeSeconds);
        }

        void UpdateWave()
        {
            float t = (Time.time - scanStart) / Mathf.Max(0.01f, expandSeconds);
            float r = Mathf.Min(1f, t) * radius;

            foreach (var n in Nutrient.Active) TryFlash(n.transform, Vector3.zero, r, foodColor);
            foreach (var p in Prey.Active) TryFlash(p.transform, Vector3.zero, r, preyColor);
            foreach (var p in Predator.Active) TryFlash(p.transform, Vector3.up * 0.5f, r, predatorColor);

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
