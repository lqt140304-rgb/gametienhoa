using System.Collections.Generic;
using Evolve.Creatures;
using Evolve.Survival;
using UnityEngine;

namespace Evolve.Abilities
{
    /// <summary>
    /// Shared shape "around me" (Ợ Chấn, Châm Choáng; later Nổ Bào Tử, Đập Đất, Gầm…): pushes, hurts and/or stuns creatures
    /// within <see cref="Radius"/>, all of them or only the nearest <see cref="MaxTargets"/>. Loose rigidbodies (props) are pushed too.
    /// </summary>
    [CreateAssetMenu(menuName = "Evolve/Abilities/Area", fileName = "AreaSO")]
    public class AreaAbility : CaveAbility
    {
        public float Radius = 4f;
        [Tooltip("Push speed at the centre, m/s (40% at the edge)")]
        public float Push;
        public float Damage;
        public float StunSeconds;
        [Tooltip("0 = every creature in reach, else only the nearest few")]
        public int MaxTargets;
        [Header("★3")]
        public float Star3Stun;
        [Tooltip("MaxTargets at ★3 (0 = unchanged)")]
        public int Star3Targets;

        const float KnockSeconds = 0.35f;

        public override bool Activate(GameObject user, int stars)
        {
            Vector3 c = user.transform.position;
            user.TryGetComponent(out SurvivalStats by);
            float power = StarPower(stars);
            int max = stars >= 3 && Star3Targets > 0 ? Star3Targets : MaxTargets;
            float stun = StunSeconds * power + (stars >= 3 ? Star3Stun : 0f);

            var inReach = new List<(Component c, float d)>();
            void Consider(Component creature)
            {
                if (creature == null) return;
                Vector3 d = creature.transform.position - c; d.y = 0f;
                if (d.magnitude <= Radius) inReach.Add((creature, d.magnitude));
            }
            foreach (var p in Predator.Active) Consider(p);
            foreach (var p in Prey.Active) Consider(p);
            if (max > 0)
            {
                if (inReach.Count == 0) return false;   // a sting with nothing in reach: no cooldown spent
                inReach.Sort((a, b) => a.d.CompareTo(b.d));
                if (inReach.Count > max) inReach.RemoveRange(max, inReach.Count - max);
            }

            foreach (var (creature, dist) in inReach)
            {
                Vector3 flat = creature.transform.position - c; flat.y = 0f;
                if (Push > 0f && flat.sqrMagnitude > 1e-4f)
                    CreatureStatus.Knock(creature, flat.normalized * Push * power * Evolve.Swallow.CaveSkillMath.Falloff(dist, Radius), KnockSeconds);
                if (stun > 0f) CreatureStatus.Stun(creature, stun);
                if (Damage > 0f && creature is IDamageable target) target.TakeDamage(Damage * power, by);
            }

            var tint = Evolve.Swallow.SkillFxMath.FamilyColor(Family);
            if (max > 0)
                foreach (var (creature, _) in inReach) SkillFx.Sting(user, SkillFx.Center(creature.transform), tint);   // a jab of the slime's own gel
            else
            {
                SkillFx.Ring(c, Radius, tint);
                SkillFx.Ring(c, Radius * 0.65f, tint, 0.4f, 0.1f);
                SkillFx.Dust(c, Radius, tint);
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
    }
}
