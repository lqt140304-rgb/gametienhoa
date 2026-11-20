using Evolve.Creatures;
using Evolve.Survival;
using UnityEngine;

namespace Evolve.Abilities
{
    /// <summary>
    /// Shared shape "around me" (Ợ Chấn, Châm Choáng; later Nổ Bào Tử, Đập Đất, Gầm…): pushes, hurts and/or stuns creatures
    /// within <see cref="Radius"/>; <see cref="NearestOnly"/> hits just the closest one (a sting). Power by stars scales push,
    /// damage and stun. Loose rigidbodies (props) are pushed too.
    /// </summary>
    [CreateAssetMenu(menuName = "Evolve/Abilities/Area", fileName = "AreaSO")]
    public class AreaAbility : AbilitySO
    {
        public float Radius = 4f;
        [Tooltip("Push speed at the centre, m/s (40% at the edge)")]
        public float Push;
        public float Damage;
        public float StunSeconds;
        public bool NearestOnly;

        const float KnockSeconds = 0.35f;

        public override bool Activate(GameObject user) => Activate(user, 2);

        public override bool Activate(GameObject user, int level)
        {
            Vector3 c = user.transform.position;
            user.TryGetComponent(out SurvivalStats by);
            float power = Power(level);

            Component nearest = null;
            float best = float.MaxValue;
            void Consider(Component creature)
            {
                if (creature == null) return;
                Vector3 d = creature.transform.position - c; d.y = 0f;
                float dist = d.magnitude;
                if (dist > Radius) return;
                if (NearestOnly) { if (dist < best) { best = dist; nearest = creature; } return; }
                Hit(creature, d, dist, power, by);
            }
            foreach (var p in Predator.Active) Consider(p);
            foreach (var p in Prey.Active) Consider(p);

            if (NearestOnly)
            {
                if (nearest == null) return false;   // nothing in reach: no cooldown spent
                Vector3 d = nearest.transform.position - c; d.y = 0f;
                Hit(nearest, d, d.magnitude, power, by);
                return true;
            }

            if (Push > 0f)
                foreach (var col in Physics.OverlapSphere(c, Radius, ~0, QueryTriggerInteraction.Ignore))
                {
                    var rb = col.attachedRigidbody;
                    if (rb == null || rb.isKinematic || rb.gameObject == user) continue;
                    Vector3 d = rb.position - c; d.y = 0f;
                    rb.AddForce(d.normalized * Push * power * Evolve.Swallow.CaveSkillMath.Falloff(d.magnitude, Radius) + Vector3.up * 2f, ForceMode.VelocityChange);
                }
            return true;
        }

        void Hit(Component creature, Vector3 flat, float dist, float power, SurvivalStats by)
        {
            if (Push > 0f && flat.sqrMagnitude > 1e-4f)
                CreatureStatus.Knock(creature, flat.normalized * Push * power * Evolve.Swallow.CaveSkillMath.Falloff(dist, Radius), KnockSeconds);
            if (StunSeconds > 0f) CreatureStatus.Stun(creature, StunSeconds * power);
            if (Damage > 0f && creature is IDamageable target) target.TakeDamage(Damage * power, by);
        }
    }
}
