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
        /// <summary>Debug: predators ignore players and never bite.</summary>
        public static bool Passive;
        /// <summary>Debug: predators stop moving.</summary>
        public static bool Frozen;

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
        public float MaxHealth => maxHealth;

        float netFraction = 1f, predictedFraction = 1f, predictedUntil;

        /// <summary>
        /// Health as 0..1 for bars. A replica shows the host's value, but drops at once when the local player hits it
        /// (for a second, until the host's own number arrives).
        /// </summary>
        public float HealthFraction
        {
            get
            {
                if (!IsReplica) return Mathf.Clamp01(Health / Mathf.Max(1f, maxHealth));
                return Time.time < predictedUntil ? Mathf.Min(netFraction, predictedFraction) : netFraction;
            }
        }

        /// <summary>Client: the host's health for this predator, as 0..1.</summary>
        public void SetNetHealth(float fraction) => netFraction = Mathf.Clamp01(fraction);

        void ShowHit(float amount)
        {
            HitFlash.Play(gameObject);
            CombatEvents.Raise(transform, amount);
        }

        /// <summary>Id shared with other machines (the host hands it out).</summary>
        public int NetId { get; set; }
        /// <summary>Client side: a copy of a predator that really lives on the host. It reports hits to the host.</summary>
        public bool IsReplica { get; set; }

        const float BiteReach = 1.1f;

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
            NetId = NetWorld.NextId();
        }

        void OnEnable() => active.Add(this);
        void OnDisable() => active.Remove(this);

        void FixedUpdate()
        {
            if (dead || Frozen || IsReplica) return;
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

            BiteRemotePlayers();
        }

        // Remote players have no collider here (they are ghosts), so they are bitten by being close enough.
        // The damage is sent to the player's own machine.
        void BiteRemotePlayers()
        {
            if (Passive || Time.time < nextBite) return;
            Vector3 pos = rb.position;
            foreach (var p in SurvivalStats.All)
            {
                if (!p.IsRemote || p.State.IsDead) continue;
                Vector3 d = p.transform.position - pos; d.y = 0f;
                if (d.sqrMagnitude > BiteReach * BiteReach) continue;

                nextBite = Time.time + biteCooldown;
                target = p;
                retreatUntil = Time.time + retreatTime;
                NetWorld.BiteRemote?.Invoke(p, biteDamage, Flat(p.transform.position - transform.position) * knockback);

                var host = p.GetComponent<OrganHost>();
                if (host != null && host.ContactDamage > 0f) TakeDamage(host.ContactDamage, p);
                return;
            }
        }

        void UpdateTarget(Vector3 pos)
        {
            if (Passive) { target = null; return; }
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
            if (IsReplica)
            {
                // The host decides about bites. A replica only reports a ram by the local player.
                if (stats != null && !stats.IsRemote && !stats.State.IsDead && first && c.relativeVelocity.magnitude >= ramSpeed)
                    TakeDamage(ramDamage, stats);
                return;
            }
            if (stats != null && stats.IsRemote) return; // remote players are handled by distance
            if (Passive && stats != null)
            {
                // Still takes ram damage in peaceful mode, but never bites back.
                if (first && c.relativeVelocity.magnitude >= ramSpeed) TakeDamage(ramDamage, stats);
                return;
            }
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
            if (by != null && amount > 0f) Evolve.Visuals.SlimeGelAnimator.Play(by.gameObject, Evolve.Visuals.GelMotion.Attack);
            if (IsReplica)
            {
                // A replica takes no damage itself: the host applies it and the replica follows.
                // But the player who hit it sees the result at once (flash, number, bar) without waiting.
                if (amount > 0f)
                {
                    predictedFraction = Mathf.Max(0f, HealthFraction - amount / Mathf.Max(1f, maxHealth));
                    predictedUntil = Time.time + 1f;
                    ShowHit(amount);
                    NetWorld.ReportCreatureHit?.Invoke(NetId, amount);
                }
                return;
            }
            if (dead || amount <= 0f) return;
            Health -= amount;
            retreatUntil = Time.time + retreatTime;
            ShowHit(amount);
            if (Health > 0f) return;

            dead = true;
            if (by != null)
            {
                NetWorld.Feed(by, nutrition);
                var host = by.GetComponent<OrganHost>();
                var drop = drops != null ? drops.Roll(o => host != null && host.Has(o)) : null;
                if (drop != null) OrganPickup.Spawn(drop, new Vector3(transform.position.x, 0.25f, transform.position.z));
            }
            Debug.Log($"{name} bị hạ gục");
            Killed?.Invoke(this);
            bool hasEc = TryGetComponent(out Evolve.Ecology.EmbodiedCreature ec);
            if (hasEc) ec.NotifyKilled();
            Evolve.Ecology.Carcass.Spawn(new Vector3(transform.position.x, 0.2f, transform.position.z), 1.5f, hasEc ? ec.Region : null);
            Destroy(gameObject);
        }
    }
}
