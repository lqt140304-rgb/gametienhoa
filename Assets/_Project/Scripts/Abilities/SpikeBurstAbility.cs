using System.Collections.Generic;
using Evolve.Creatures;
using Evolve.Survival;
using UnityEngine;

namespace Evolve.Abilities
{
    /// <summary>
    /// Skill of the Toxic Spikes (tier B): the spikes burst outward and hurt every predator close by.
    /// Proposed numbers, tune in the asset.
    /// </summary>
    [CreateAssetMenu(menuName = "Evolve/Abilities/Spike Burst", fileName = "SpikeBurstSO")]
    public class SpikeBurstAbility : AbilitySO
    {
        public float Radius = 3f;
        public float Damage = 12f;

        public override bool Activate(GameObject user) => Activate(user, 2);

        public override bool Activate(GameObject user, int level)
        {
            user.TryGetComponent(out SurvivalStats stats);
            float radius = Radius * (level >= 3 ? 1.25f : 1f);
            float damage = Damage * Power(level);

            // Copy first: a kill removes the predator from the active list.
            var targets = new List<Predator>(Predator.Active);
            foreach (var p in targets)
            {
                if (p == null) continue;
                if ((p.transform.position - user.transform.position).sqrMagnitude <= radius * radius)
                    p.TakeDamage(damage, stats);
            }
            return true;
        }
    }
}
