using UnityEngine;

namespace Evolve.Abilities
{
    /// <summary>
    /// Stun and knock-back on a creature, put there by a skill (Ợ Chấn, Châm Choáng, Lưỡi Dài). Prey and Predator skip their own
    /// movement while <see cref="Holds"/> is true (and a stunned predator does not bite); during a knock this component moves the body.
    /// Runs on the machine that simulates the creature. shortcut: a co-op client's skill does not reach the host's creatures yet.
    /// </summary>
    public class CreatureStatus : MonoBehaviour
    {
        float stunUntil, knockUntil, groundOffset;
        Vector3 knockVelocity;
        Rigidbody rb;

        public bool Stunned => Time.time < stunUntil;
        bool Knocked => Time.time < knockUntil;

        /// <summary>True while the creature must not move or bite on its own.</summary>
        public static bool Holds(GameObject creature) =>
            creature.TryGetComponent(out CreatureStatus s) && (s.Stunned || s.Knocked);

        static CreatureStatus On(Component creature) =>
            creature.TryGetComponent(out CreatureStatus s) ? s : creature.gameObject.AddComponent<CreatureStatus>();

        /// <summary>Hold the creature still. <paramref name="stars"/> shows stars over its head (not wanted for a pulled prey, which is only held to be swallowed).</summary>
        public static void Stun(Component creature, float seconds, bool stars = true)
        {
            var s = On(creature);
            s.stunUntil = Mathf.Max(s.stunUntil, Time.time + seconds);
            if (stars) SkillFx.Stars(creature, seconds);
        }

        /// <summary>Slide the creature at <paramref name="velocity"/> (flat) for <paramref name="seconds"/>, following the ground.</summary>
        public static void Knock(Component creature, Vector3 velocity, float seconds)
        {
            var s = On(creature);
            velocity.y = 0f;
            s.knockVelocity = velocity;
            s.knockUntil = Time.time + seconds;
            s.groundOffset = Evolve.World.GroundHeight.OffsetOf(creature.transform.position);
        }

        void FixedUpdate()
        {
            if (!Knocked) return;
            if (rb == null && !TryGetComponent(out rb)) return;
            rb.MovePosition(Evolve.World.GroundHeight.Follow(rb.position + knockVelocity * Time.fixedDeltaTime, groundOffset));
        }
    }
}
