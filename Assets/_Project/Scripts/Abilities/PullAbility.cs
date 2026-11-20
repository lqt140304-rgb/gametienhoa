using System.Collections.Generic;
using Evolve.Creatures;
using Evolve.Player;
using UnityEngine;

namespace Evolve.Abilities
{
    /// <summary>
    /// Shared shape "shoot out" for a tongue (Lưỡi Dài): grabs the nearest prey within <see cref="Distance"/> metres and
    /// <see cref="ConeAngle"/> of the aim and drags it to the slime's mouth, ready to be rammed and swallowed alive.
    /// Only prey (Prey) is pulled; hunters and cave lords are too heavy.
    /// </summary>
    [CreateAssetMenu(menuName = "Evolve/Abilities/Pull", fileName = "PullSO")]
    public class PullAbility : CaveAbility
    {
        public float Distance = 6f;
        public float ConeAngle = 30f;
        public int MaxTargets = 1;
        [Tooltip("MaxTargets at ★3 (0 = unchanged)")]
        public int Star3Targets;

        const float PullSeconds = 0.3f, StopShort = 1f;

        public override bool Activate(GameObject user, int stars)
        {
            Vector3 c = user.transform.position;
            Vector3 aim = AimController.AimDirection(user, c, out _);
            aim.y = 0f;
            if (aim.sqrMagnitude < 0.01f) aim = user.transform.forward;

            float range = Distance * StarPower(stars);
            var hits = new List<(Prey p, float d)>();
            foreach (var p in Prey.Active)
            {
                if (p == null) continue;
                Vector3 d = p.transform.position - c; d.y = 0f;
                if (d.magnitude <= range && d.magnitude > 0.01f && Vector3.Angle(aim, d) <= ConeAngle) hits.Add((p, d.magnitude));
            }
            if (hits.Count == 0) return false;   // missed: no cooldown spent
            hits.Sort((a, b) => a.d.CompareTo(b.d));
            int max = Mathf.Max(1, stars >= 3 && Star3Targets > 0 ? Star3Targets : MaxTargets);

            for (int i = 0; i < hits.Count && i < max; i++)
            {
                var prey = hits[i].p;
                Vector3 to = c - prey.transform.position; to.y = 0f;
                float travel = Mathf.Max(0f, to.magnitude - StopShort - i * 0.6f);   // a second one stops a little further out
                CreatureStatus.Knock(prey, to.normalized * travel / PullSeconds, PullSeconds);
                CreatureStatus.Stun(prey, PullSeconds + 0.5f, false);   // a moment to swallow it
                SkillFx.Tongue(user.transform, prey.transform, Evolve.Swallow.SkillFxMath.TongueColor);
            }
            return true;
        }
    }
}
