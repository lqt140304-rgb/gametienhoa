using System;
using Evolve.Survival;
using UnityEngine;

namespace Evolve.Swallow
{
    /// <summary>
    /// The slime's body mass. Mass sets max health (mass × 10) and body size (SlimeController reads it).
    /// Digesting adds mass; sculpting organs spends it, never below MassRules.MinMass.
    /// </summary>
    public class SlimeBody : MonoBehaviour
    {
        [SerializeField] float mass = MassRules.StartMass;

        SurvivalStats stats;

        public float Mass => mass;
        public event Action Changed;

        void Awake()
        {
            stats = GetComponent<SurvivalStats>();
            Apply();
        }

        public void AddMass(float m)
        {
            if (m <= 0f) return;
            mass += m;
            Apply();
        }

        /// <summary>Spend mass; false (and nothing spent) when it would go below MinMass.</summary>
        public bool TrySpend(float m)
        {
            float left = MassRules.AfterSpend(mass, m);
            if (left < 0f) return false;
            mass = left;
            Apply();
            return true;
        }

        /// <summary>Debug / tests: set mass directly.</summary>
        public void SetMass(float m)
        {
            mass = Mathf.Max(MassRules.MinMass, m);
            Apply();
        }

        /// <summary>Re-apply max health (a passive such as Da Dày changed).</summary>
        public void Refresh() => Apply();

        void Apply()
        {
            if (stats == null) stats = GetComponent<SurvivalStats>();
            if (stats != null && !stats.IsRemote)
            {
                var s = stats.State;
                var host = GetComponent<Evolve.Organs.OrganHost>();
                s.MaxHealth = MassRules.MaxHealth(mass) + (host != null ? host.MaxHealthBonus : 0f);
                if (s.Health > s.MaxHealth) s.Health = s.MaxHealth;
            }
            Changed?.Invoke();
        }
    }
}
