using Evolve.Survival;
using UnityEngine;

namespace Evolve.Creatures
{
    /// <summary>
    /// Kẻ thù: wanders, chases the nearest slime inside detectRadius, bites on contact,
    /// and gives up beyond loseRadius or when the target plays dead (Giả Xác).
    /// </summary>
    [RequireComponent(typeof(CreatureMotor))]
    public class Predator : MonoBehaviour
    {
        [SerializeField] float detectRadius = 10f;
        [SerializeField] float loseRadius = 16f;
        [SerializeField] float biteDamage = 15f;
        [SerializeField] float biteCooldown = 1.5f;

        CreatureMotor motor;
        SurvivalStats target;
        float nextBite;

        void Awake() => motor = GetComponent<CreatureMotor>();

        void Update()
        {
            if (target != null && !StillChasing(target)) target = null;
            if (target == null) target = CreatureMotor.NearestSlime(transform.position, detectRadius, skipHidden: true);

            if (target != null) motor.Move(target.transform.position - transform.position, run: true);
            else motor.Wander();
        }

        bool StillChasing(SurvivalStats t)
        {
            if (t.State.IsDead || Evolve.Abilities.PlayingDead.IsHidden(t.gameObject)) return false;
            return (t.transform.position - transform.position).sqrMagnitude <= loseRadius * loseRadius;
        }

        void OnCollisionStay(Collision c)
        {
            if (Time.time < nextBite) return;
            var stats = c.collider.GetComponentInParent<SurvivalStats>();
            if (stats == null || stats.State.IsDead || Evolve.Abilities.PlayingDead.IsHidden(stats.gameObject)) return;
            stats.State.Damage(biteDamage);
            nextBite = Time.time + biteCooldown;
        }
    }
}
