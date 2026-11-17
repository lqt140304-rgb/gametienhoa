using System;
using System.Collections.Generic;
using Evolve.Organs;
using Evolve.Survival;
using UnityEngine;

namespace Evolve.Creatures
{
    /// <summary>
    /// Hunts players: patrols its territory, chases what it detects (still targets are
    /// harder to notice), bites on contact with a knockback, tires after a long chase.
    /// It can be hurt by spikes (OrganHost.ContactDamage) or by being rammed at speed.
    /// Killing it gives food and a guaranteed, rarer drop.
    /// Needs a kinematic Rigidbody and a non-trigger collider.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Predator : MonoBehaviour
    {
        static readonly List<Predator> active = new List<Predator>();
        public static IReadOnlyList<Predator> Active => active;

        [Header("Movement")]
        [SerializeField] float patrolSpeed = 0.8f;
        [SerializeField] float chaseSpeed = 1.4f;
        [Tooltip("Seconds of chasing before it must rest")]
        [SerializeField] float chaseStamina = 6f;
        [SerializeField] float restTime = 2.5f;

        [Header("Senses")]
        [SerializeField] float detectRadius = 7f;
        [Tooltip("Detection radius multiplier when the target stands still")]
        [Range(0, 1)] [SerializeField] float stillDetectFactor = 0.4f;
        [Tooltip("Gives up the chase beyond this distance")]
        [SerializeField] float loseRadius = 12f;

        [Header("Combat")]
        [SerializeField] float biteDamage = 15f;
        [SerializeField] float biteCooldown = 1.2f;
        [SerializeField] float knockback = 4f;
        [Tooltip("Seconds it backs off after biting")]
        [SerializeField] float retreatTime = 1f;
        [SerializeField] float maxHealth = 40f;
        [Tooltip("Damage taken when a player rams into it faster than RamSpeed")]
        [SerializeField] float ramDamage = 10f;
        [SerializeField] float ramSpeed = 4f;

        [Header("Reward")]
        [SerializeField] float nutrition = 35f;
        [SerializeField] DropTableSO drops;

        public Vector3 HomeCenter { get; set; }
        public float HomeRadius { get; set; } = 12f;
        public float Health { get; private set; }
        public bool IsChasing => target != null;
        public bool IsTired => Time.time < tiredUntil;

        public event Action<Predator> Killed;

        Rigidbody rb;
        SurvivalStats target;
        Vector3 wanderDir;
        float nextTurn, chaseTime, tiredUntil, retreatUntil, nextBite;
        bool dead;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            Health = maxHealth;
            HomeCenter = transform.position;
        }

        void OnEnable() => active.Add(this);
        void OnDisable() => active.Remove(this);

        void FixedUpdate()
        {
            if (dead) return;
            Vector3 pos = rb.position;
            UpdateTarget(pos);

            Vector3 dir = Vector3.zero;
            float speed = 0f;

            if (target != null && Time.time < retreatUntil)
            {
                dir = Flat(pos - target.transform.position); speed = patrolSpeed;
            }
            else if (target != null)
            {
                if (!IsTired)
                {
                    dir = Flat(target.transform.position - pos); speed = chaseSpeed;
                    chaseTime += Time.fixedDeltaTime;
                    if (chaseTime >= chaseStamina) { tiredUntil = Time.time + restTime; chaseTime = 0f; }
                }
            }
            else
            {
                chaseTime = Mathf.Max(0f, chaseTime - Time.fixedDeltaTime);
                if (Time.time >= nextTurn)
                {
                    float a = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                    wanderDir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                    nextTurn = Time.time + UnityEngine.Random.Range(3f, 6f);
                }
                dir = wanderDir; speed = patrolSpeed;

                Vector3 fromHome = Flat(pos - HomeCenter, false);
                if (fromHome.magnitude > HomeRadius * 0.8f)
                    dir = (dir - fromHome.normalized * 1.5f).normalized;
            }

            if (dir.sqrMagnitude > 0.001f && speed > 0f)
            {
                rb.MovePosition(pos + dir * speed * Time.fixedDeltaTime);
                rb.MoveRotation(Quaternion.Slerp(rb.rotation, Quaternion.LookRotation(dir), 0.15f));
            }
        }

        void UpdateTarget(Vector3 pos)
        {
            if (target != null)
            {
                if (target.State.IsDead || Vector3.Distance(pos, target.transform.position) > loseRadius
                    || Vector3.Distance(target.transform.position, HomeCenter) > HomeRadius + loseRadius)
                    target = null;
                return;
            }
            foreach (var p in SurvivalStats.All)
            {
                if (p.State.IsDead) continue;
                float radius = detectRadius * (p.IsMoving ? 1f : stillDetectFactor);
                if (Vector3.Distance(pos, p.transform.position) <= radius) { target = p; break; }
            }
        }

        static Vector3 Flat(Vector3 v, bool normalize = true)
        {
            v.y = 0f;
            return normalize ? v.normalized : v;
        }

        void OnCollisionEnter(Collision c) => Contact(c, true);
        void OnCollisionStay(Collision c) => Contact(c, false);

        void Contact(Collision c, bool first)
        {
            if (dead) return;
            var stats = c.collider.GetComponentInParent<SurvivalStats>();
            if (stats == null || stats.State.IsDead) return;
            var host = stats.GetComponent<OrganHost>();

            // Rammed at speed (e.g. by a dash ability).
            if (first && c.relativeVelocity.magnitude >= ramSpeed)
                TakeDamage(ramDamage, stats);
            if (dead) return;

            if (Time.time < nextBite) return;
            nextBite = Time.time + biteCooldown;
            target = stats;

            stats.State.Damage(biteDamage);
            var playerRb = stats.GetComponent<Rigidbody>();
            if (playerRb != null)
                playerRb.AddForce(Flat(stats.transform.position - transform.position) * knockback + Vector3.up * 2f, ForceMode.VelocityChange);
            retreatUntil = Time.time + retreatTime;

            if (host != null && host.ContactDamage > 0f) TakeDamage(host.ContactDamage, stats);
        }

        public void TakeDamage(float amount, SurvivalStats by)
        {
            if (dead || amount <= 0f) return;
            Health -= amount;
            retreatUntil = Time.time + retreatTime;
            if (Health > 0f) return;

            dead = true;
            if (by != null)
            {
                by.State.Feed(nutrition);
                var host = by.GetComponent<OrganHost>();
                var drop = drops != null ? drops.Roll(o => host != null && host.Has(o)) : null;
                if (drop != null) OrganPickup.Spawn(drop, new Vector3(transform.position.x, 0.25f, transform.position.z));
            }
            Debug.Log($"{name} bị hạ gục");
            Killed?.Invoke(this);
            Destroy(gameObject);
        }
    }
}
