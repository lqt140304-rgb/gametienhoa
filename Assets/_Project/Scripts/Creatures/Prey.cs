using Evolve.Abilities;
using Evolve.Survival;
using UnityEngine;

namespace Evolve.Creatures
{
    /// <summary>
    /// Mồi sống: wanders, flees from nearby slimes (faster than a crawling slime),
    /// and is absorbed while a slime touches it. Respawns at home after dying.
    /// Needs a non-trigger Collider + Rigidbody so Bám Dính / Phun Dịch Nhầy can hit it.
    /// </summary>
    [RequireComponent(typeof(CreatureMotor))]
    public class Prey : MonoBehaviour
    {
        [SerializeField] float nutrition = 40f;
        [Tooltip("Nutrition per second while touched (faster than plant food)")]
        [SerializeField] float absorbRate = 10f;
        [SerializeField] float fleeRadius = 6f;
        [SerializeField] float respawnSeconds = 30f;

        CreatureMotor motor;
        float remaining;
        float respawnAt = -1f;
        Vector3 baseScale;

        public bool Alive => respawnAt < 0f;

        void Awake()
        {
            motor = GetComponent<CreatureMotor>();
            remaining = nutrition;
            baseScale = transform.localScale;
        }

        void Update()
        {
            if (!Alive)
            {
                if (Time.time >= respawnAt) Respawn();
                return;
            }
            var threat = CreatureMotor.NearestSlime(transform.position, fleeRadius, skipHidden: true);
            if (threat != null) motor.Move(transform.position - threat.transform.position, run: true);
            else motor.Wander();
        }

        void OnCollisionStay(Collision c)
        {
            if (!Alive) return;
            var stats = c.collider.GetComponentInParent<SurvivalStats>();
            if (stats == null || stats.State.IsDead) return;

            float rate = absorbRate * StickyGrip.AbsorbMultiplierFor(stats.gameObject);
            float amount = Mathf.Min(rate * Time.fixedDeltaTime, remaining);
            remaining -= amount;
            stats.State.Feed(amount);
            transform.localScale = baseScale * Mathf.Lerp(0.4f, 1f, remaining / nutrition);
            if (remaining <= 0f) Die();
        }

        void Die()
        {
            respawnAt = Time.time + respawnSeconds;
            SetVisible(false);
        }

        void Respawn()
        {
            respawnAt = -1f;
            remaining = nutrition;
            transform.localScale = baseScale;
            motor.ResetTo(motor.Home);
            SetVisible(true);
        }

        void SetVisible(bool on)
        {
            foreach (var col in GetComponentsInChildren<Collider>()) col.enabled = on;
            foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = on;
            GetComponent<Rigidbody>().isKinematic = !on;
            if (!on) motor.Stop();
        }
    }
}
