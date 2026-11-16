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
        }

        void OnEnable() { if (remaining > 0f) active.Add(this); }
        void OnDisable() => active.Remove(this);

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
            if (remaining <= 0f) return;
            var stats = other.GetComponentInParent<SurvivalStats>();
            if (stats == null || stats.State.IsDead) return;

            float amount = Mathf.Min(absorbRate * Time.fixedDeltaTime, remaining);
            remaining -= amount;
            stats.State.Feed(amount);
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
