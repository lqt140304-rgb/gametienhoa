using Evolve.Creatures;
using Evolve.Player;
using UnityEngine;

namespace Evolve.Abilities
{
    /// <summary>
    /// Shared shape "shoot out" for a tongue (Lưỡi Dài): grabs the nearest prey within <see cref="Distance"/> metres and
    /// <see cref="ConeAngle"/> of the aim and drags it to the slime's mouth, ready to be rammed and swallowed alive.
    /// Only prey (small, Prey) is pulled; hunters and cave lords are too heavy.
    /// </summary>
    [CreateAssetMenu(menuName = "Evolve/Abilities/Pull", fileName = "PullSO")]
    public class PullAbility : AbilitySO
    {
        public float Distance = 6f;
        public float ConeAngle = 30f;

        const float PullSeconds = 0.3f, StopShort = 1f;

        public override bool Activate(GameObject user) => Activate(user, 2);

        public override bool Activate(GameObject user, int level)
        {
            Vector3 c = user.transform.position;
            Vector3 aim = AimController.AimDirection(user, c, out _);
            aim.y = 0f;
            if (aim.sqrMagnitude < 0.01f) aim = user.transform.forward;

            float range = Distance * Power(level);
            Prey best = null;
            float bestDist = float.MaxValue;
            foreach (var p in Prey.Active)
            {
                if (p == null) continue;
                Vector3 d = p.transform.position - c; d.y = 0f;
                float dist = d.magnitude;
                if (dist > range || dist < 0.01f || Vector3.Angle(aim, d) > ConeAngle || dist >= bestDist) continue;
                best = p; bestDist = dist;
            }
            if (best == null) return false;   // missed: no cooldown spent

            Vector3 to = c - best.transform.position; to.y = 0f;
            float travel = Mathf.Max(0f, to.magnitude - StopShort);
            CreatureStatus.Knock(best, to.normalized * travel / PullSeconds, PullSeconds);
            CreatureStatus.Stun(best, PullSeconds + 0.5f);   // a moment to swallow it
            return true;
        }
    }
}
