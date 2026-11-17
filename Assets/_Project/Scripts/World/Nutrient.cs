using System.Collections.Generic;
using Evolve.Survival;
using UnityEngine;

namespace Evolve.World
{
    /// <summary>
    /// Food blob absorbed through the membrane while the slime touches it.
    /// Needs a trigger collider. Shrinks as it is absorbed, then respawns.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Nutrient : MonoBehaviour
    {
        static readonly List<Nutrient> active = new List<Nutrient>();
        /// <summary>All nutrients currently available to eat.</summary>
        public static IReadOnlyList<Nutrient> Active => active;

        static readonly List<Nutrient> all = new List<Nutrient>();
        /// <summary>Every nutrient in the scene, eaten or not (same scene on every machine, so the same food).</summary>
        public static IReadOnlyList<Nutrient> All => all;

        /// <summary>
        /// True on a client: the host owns the food. Nothing is eaten locally; the host feeds this player
        /// (it sees them as a ghost) and reports how much of each blob is left (NetApply).
        /// </summary>
        public static bool ClientMode;

        const float AbsorbReach = 0.9f;

        [SerializeField] float totalNutrition = 25f;
        [Tooltip("Nutrition absorbed per second (primitive membrane is slow)")]
        [SerializeField] float absorbRate = 5f;
        [SerializeField] float respawnSeconds = 20f;

        float remaining;
        Vector3 baseScale;
        float respawnAt = -1f;
        Collider col;
        Renderer rend;

        void Awake()
        {
            col = GetComponent<Collider>();
            col.isTrigger = true;
            rend = GetComponentInChildren<Renderer>();
            baseScale = transform.localScale;
            remaining = totalNutrition;
            all.Add(this);
        }

        void OnEnable() { if (remaining > 0f) active.Add(this); }
        void OnDisable() => active.Remove(this);
        void OnDestroy() => all.Remove(this);

        /// <summary>Fraction left (1 = fresh, 0 = eaten).</summary>
        public float Fraction => totalNutrition > 0f ? Mathf.Clamp01(remaining / totalNutrition) : 0f;

        /// <summary>Client: show the host's state of this blob (0 = eaten).</summary>
        public void NetApply(float fraction)
        {
            bool on = fraction > 0.001f;
            SetActive(on);
            if (on) transform.localScale = baseScale * Mathf.Lerp(0.3f, 1f, Mathf.Clamp01(fraction));
        }

        /// <summary>Back to single player: show the local state again.</summary>
        public void RefreshLocal()
        {
            bool on = remaining > 0f;
            SetActive(on);
            transform.localScale = baseScale * (on ? Mathf.Lerp(0.3f, 1f, remaining / totalNutrition) : 1f);
        }

        // Remote players have no collider here (they are ghosts), so they eat by standing close.
        void FixedUpdate()
        {
            if (ClientMode || remaining <= 0f) return;
            foreach (var p in SurvivalStats.All)
            {
                if (!p.IsRemote || p.State.IsDead) continue;
                Vector3 d = p.transform.position - transform.position; d.y = 0f;
                if (d.sqrMagnitude <= AbsorbReach * AbsorbReach) Absorb(p);
                if (remaining <= 0f) return;
            }
        }

        void Update()
        {
            if (respawnAt > 0f && Time.time >= respawnAt)
            {
                respawnAt = -1f;
                remaining = totalNutrition;
                transform.localScale = baseScale;
                SetActive(true);
            }
        }

        void OnTriggerStay(Collider other)
        {
            if (ClientMode || remaining <= 0f) return;
            var stats = other.GetComponentInParent<SurvivalStats>();
            if (stats == null || stats.State.IsDead || stats.IsRemote) return;
            Absorb(stats);
        }

        void Absorb(SurvivalStats stats)
        {
            float amount = Mathf.Min(absorbRate * Time.fixedDeltaTime, remaining);
            remaining -= amount;
            NetWorld.Feed(stats, amount);
            Evolve.Visuals.SlimeGelAnimator.Play(stats.gameObject, Evolve.Visuals.GelMotion.Absorb);
            transform.localScale = baseScale * Mathf.Lerp(0.3f, 1f, remaining / totalNutrition);

            if (remaining <= 0f)
            {
                SetActive(false);
                respawnAt = Time.time + respawnSeconds;
            }
        }

        void SetActive(bool on)
        {
            if (on) { if (!active.Contains(this)) active.Add(this); }
            else active.Remove(this);
            col.enabled = on;
            if (rend) rend.enabled = on;
        }
    }
}
