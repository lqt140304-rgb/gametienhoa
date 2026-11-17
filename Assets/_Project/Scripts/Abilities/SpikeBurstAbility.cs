using Evolve.Player;
using Evolve.Survival;
using UnityEngine;

namespace Evolve.Abilities
{
    /// <summary>
    /// Skill of the Toxic Spikes (tier B): fires a fan of spikes toward the crosshair (aim mode, Alt) or the mouse
    /// cursor (mouse mode). Lv3 adds 2 spikes and 50% damage. Proposed numbers, tune in the asset.
    /// </summary>
    [CreateAssetMenu(menuName = "Evolve/Abilities/Spike Burst", fileName = "SpikeBurstSO")]
    public class SpikeBurstAbility : AbilitySO
    {
        [Tooltip("Spikes in one volley at Lv2 (Lv3 adds 2)")]
        public int SpikeCount = 5;
        [Tooltip("Damage of each spike at Lv2 (Lv3 x1.5)")]
        public float SpikeDamage = 6f;
        [Tooltip("Total spread of the fan, in degrees")]
        public float SpreadAngle = 14f;
        [Tooltip("How far a spike flies, in metres")]
        public float Range = 14f;
        [Tooltip("Spike speed, metres per second")]
        public float Speed = 24f;

        [Tooltip("How far below the body centre the spikes start, so they reach small creatures on the ground")]
        public float SpawnDrop = 0.2f;
        [Tooltip("Aim assist: a creature within this angle (degrees) of where you aim is targeted, including its height")]
        public float AssistAngle = 12f;

        public override bool Activate(GameObject user) => Activate(user, 2);

        /// <summary>
        /// Level shots pass over small creatures. If a creature is close to the aim line, bend the aim toward its
        /// centre (height included), so a shot aimed near it hits it instead of flying over.
        /// </summary>
        Vector3 AssistAim(Vector3 origin, Vector3 aim)
        {
            Vector3 flatAim = new Vector3(aim.x, 0f, aim.z);
            if (flatAim.sqrMagnitude < 1e-4f) return aim;
            flatAim.Normalize();

            Vector3 best = aim;
            float bestAngle = AssistAngle;
            void Consider(Vector3 centre)
            {
                Vector3 to = centre - origin;
                Vector3 flat = new Vector3(to.x, 0f, to.z);
                if (flat.magnitude > Range || flat.sqrMagnitude < 0.01f) return;
                float angle = Vector3.Angle(flatAim, flat);
                if (angle < bestAngle) { bestAngle = angle; best = to.normalized; }
            }
            foreach (var predator in Evolve.Creatures.Predator.Active)
                if (predator != null) Consider(CentreOf(predator.gameObject));
            foreach (var prey in Evolve.Creatures.Prey.Active)
                if (prey != null) Consider(CentreOf(prey.gameObject));
            return best;
        }

        static Vector3 CentreOf(GameObject creature)
        {
            var col = creature.GetComponentInChildren<Collider>();
            return col != null ? col.bounds.center : creature.transform.position;
        }

        public override bool Activate(GameObject user, int level)
        {
            user.TryGetComponent(out SurvivalStats stats);
            // From the centre of the body, so a level shot hits creatures standing on the ground.
            Vector3 origin = user.transform.position;
            Vector3 aim = AimController.AimDirection(user, origin, out _);
            aim = AssistAim(origin, aim);

            int count = Mathf.Max(1, SpikeCount) + (level >= 3 ? 2 : 0);
            float damage = SpikeDamage * Power(level);
            for (int i = 0; i < count; i++)
            {
                float t = count == 1 ? 0.5f : i / (float)(count - 1);
                float angle = Mathf.Lerp(-SpreadAngle * 0.5f, SpreadAngle * 0.5f, t);
                Vector3 d = Quaternion.AngleAxis(angle, Vector3.up) * aim;
                // Spawn low, near the ground, where small creatures are.
                SpikeProjectile.Spawn(origin + d * 0.6f + Vector3.down * SpawnDrop, d, Speed, Range, damage, stats);
            }
            return true;
        }
    }
}
