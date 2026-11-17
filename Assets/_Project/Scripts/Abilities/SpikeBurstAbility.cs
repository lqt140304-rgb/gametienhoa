using Evolve.Player;
using Evolve.Survival;
using UnityEngine;

namespace Evolve.Abilities
{
    /// <summary>
    /// Skill of the Toxic Spikes (tier B): fires a fan of spikes toward the crosshair (aim mode, Alt) or the way
    /// the camera faces (mouse mode). Lv3 adds 2 spikes and 50% damage. Proposed numbers, tune in the asset.
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

        public override bool Activate(GameObject user) => Activate(user, 2);

        public override bool Activate(GameObject user, int level)
        {
            user.TryGetComponent(out SurvivalStats stats);
            Vector3 origin = user.transform.position + Vector3.up * 0.3f;
            Vector3 aim = AimController.AimDirection(user, origin, out _);

            int count = Mathf.Max(1, SpikeCount) + (level >= 3 ? 2 : 0);
            float damage = SpikeDamage * Power(level);
            for (int i = 0; i < count; i++)
            {
                float t = count == 1 ? 0.5f : i / (float)(count - 1);
                float angle = Mathf.Lerp(-SpreadAngle * 0.5f, SpreadAngle * 0.5f, t);
                Vector3 d = Quaternion.AngleAxis(angle, Vector3.up) * aim;
                SpikeProjectile.Spawn(origin + d * 0.6f, d, Speed, Range, damage, stats);
            }
            return true;
        }
    }
}
