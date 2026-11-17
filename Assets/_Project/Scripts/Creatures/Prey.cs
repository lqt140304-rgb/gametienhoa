using System;
using System.Collections.Generic;
using Evolve.Organs;
using Evolve.Survival;
using UnityEngine;

namespace Evolve.Creatures
{
    /// <summary>
    /// Small creature that wanders, flees from nearby players and tires after a
    /// sprint. Touch it to catch it: gives food and drops an organ the catcher lacks.
    /// Needs a kinematic Rigidbody and a non-trigger collider.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Prey : MonoBehaviour
    {
        static readonly List<Prey> active = new List<Prey>();
        public static IReadOnlyList<Prey> Active => active;
        /// <summary>Debug: prey stop moving.</summary>
        public static bool Frozen;

        [SerializeField] float wanderSpeed = 0.5f;
        [SerializeField] float fleeSpeed = 1.8f;
        [SerializeField] float fleeRadius = 6f;
        [Tooltip("Seconds it can flee before getting tired")]
        [SerializeField] float stamina = 5f;
        [Tooltip("Seconds it must rest when tired")]
        [SerializeField] float restTime = 1.5f;
        [SerializeField] float nutrition = 15f;
        [Tooltip("Spikes and other attacks can kill it: dying this way gives the same reward as catching it")]
        [SerializeField] float maxHealth = 12f;
        [Tooltip("What this creature drops when caught")]
        [SerializeField] DropTableSO drops;
        [Tooltip("Legacy list (used only if Drops is empty): uniform random, always drops")]
        [SerializeField] OrganSO[] dropTable;

        /// <summary>Area it stays inside (set by the spawner).</summary>
        public Vector3 HomeCenter { get; set; }
        public float HomeRadius { get; set; } = 25f;
        public bool IsTired => tiredUntil > Time.time;
        public float Health { get; private set; }
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

        /// <summary>Client: the host's health for this prey, as 0..1.</summary>
        public void SetNetHealth(float fraction) => netFraction = Mathf.Clamp01(fraction);

        /// <summary>Id shared with other machines (the host hands it out).</summary>
        public int NetId { get; set; }
        /// <summary>Client side: a copy of a creature that really lives on the host. It only follows what the host reports.</summary>
        public bool IsReplica { get; set; }

        const float CatchRadius = 0.9f;

        public event Action<Prey> Caught;

        Rigidbody rb;
        Vector3 wanderDir;
        float nextTurn, fleeTime, tiredUntil;
        bool caught;
        Evolve.World.VibrationEmitter emitter;
        float nextFleePulse;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            NetId = NetWorld.NextId();
            TryGetComponent(out emitter);
            Health = maxHealth;
        }

        void OnEnable() => active.Add(this);
        void OnDisable() => active.Remove(this);

        void FixedUpdate()
        {
            if (Frozen || IsReplica) return;
            Vector3 pos = rb.position;
            Vector3 dir;
            float speed;

            Transform threat = NearestPlayer(pos, out float dist);
            if (IsTired)
            {
                dir = Vector3.zero; speed = 0f;
            }
            else if (threat != null && dist < fleeRadius)
            {
                dir = pos - threat.position; dir.y = 0f; dir.Normalize();
                speed = fleeSpeed;
                fleeTime += Time.fixedDeltaTime;
                if (emitter != null && Time.time >= nextFleePulse) { emitter.Pulse(0.4f); nextFleePulse = Time.time + 0.5f; }
                if (fleeTime >= stamina) { tiredUntil = Time.time + restTime; fleeTime = 0f; }
            }
            else
            {
                fleeTime = Mathf.Max(0f, fleeTime - Time.fixedDeltaTime);
                if (Time.time >= nextTurn)
                {
                    float a = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                    wanderDir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                    nextTurn = Time.time + UnityEngine.Random.Range(2f, 4f);
                }
                dir = wanderDir; speed = wanderSpeed;
            }

            // Stay near home: steer back toward the centre when near the edge.
            Vector3 fromHome = pos - HomeCenter; fromHome.y = 0f;
            if (fromHome.magnitude > HomeRadius * 0.8f)
                dir = (dir - fromHome.normalized * 1.5f).normalized;

            if (dir.sqrMagnitude > 0.001f)
            {
                rb.MovePosition(pos + dir * speed * Time.fixedDeltaTime);
                rb.MoveRotation(Quaternion.Slerp(rb.rotation, Quaternion.LookRotation(dir), 0.2f));
            }

            CheckRemoteCatch();
        }

        // Remote players have no collider here (they are ghosts), so they catch by getting close.
        void CheckRemoteCatch()
        {
            if (caught) return;
            Vector3 pos = rb.position;
            foreach (var p in SurvivalStats.All)
            {
                if (!p.IsRemote || p.State.IsDead) continue;
                Vector3 d = p.transform.position - pos; d.y = 0f;
                if (d.sqrMagnitude <= CatchRadius * CatchRadius) { Catch(p); return; }
            }
        }

        static Transform NearestPlayer(Vector3 pos, out float dist)
        {
            Transform best = null; dist = float.MaxValue;
            foreach (var p in SurvivalStats.All)
            {
                if (p.State.IsDead) continue;
                float d = Vector3.Distance(pos, p.transform.position);
                if (d < dist) { dist = d; best = p.transform; }
            }
            return best;
        }

        void OnCollisionEnter(Collision c)
        {
            if (caught || IsReplica) return;
            var stats = c.collider.GetComponentInParent<SurvivalStats>();
            if (stats == null || stats.State.IsDead || stats.IsRemote) return;
            Catch(stats);
        }

        /// <summary>Hurt the prey (spikes and so on). At 0 health it dies and rewards the attacker like a catch.</summary>
        public void TakeDamage(float amount, SurvivalStats by)
        {
            if (IsReplica)
            {
                // The host applies the damage; the player who hit it sees the result at once.
                if (amount > 0f)
                {
                    predictedFraction = Mathf.Max(0f, HealthFraction - amount / Mathf.Max(1f, maxHealth));
                    predictedUntil = Time.time + 1f;
                    ShowHit(amount);
                    NetWorld.ReportCreatureHit?.Invoke(NetId, amount);
                }
                return;
            }
            if (caught || amount <= 0f) return;

            Health -= amount;
            ShowHit(amount);
            if (Health > 0f) return;

            if (by != null) Catch(by);
            else
            {
                caught = true;
                Caught?.Invoke(this);
                if (TryGetComponent(out Evolve.Ecology.EmbodiedCreature ec)) ec.NotifyKilled();
                Destroy(gameObject);
            }
        }

        void ShowHit(float amount)
        {
            HitFlash.Play(gameObject);
            CombatEvents.Raise(transform, amount);
        }

        void Catch(SurvivalStats stats)
        {
            caught = true;

            NetWorld.Feed(stats, nutrition);
            var host = stats.GetComponent<OrganHost>();
            var drop = PickDrop(host);
            if (drop != null) OrganPickup.Spawn(drop, new Vector3(transform.position.x, 0.25f, transform.position.z));

            Caught?.Invoke(this);
            if (TryGetComponent(out Evolve.Ecology.EmbodiedCreature ec)) ec.NotifyKilled();
            Destroy(gameObject);
        }

        /// <summary>Roll the drop table, skipping organs the catcher already owns.</summary>
        OrganSO PickDrop(OrganHost host)
        {
            if (drops != null) return drops.Roll(o => host != null && host.Has(o));
            if (dropTable == null) return null;
            var options = new List<OrganSO>();
            foreach (var o in dropTable)
                if (o != null && (host == null || !host.Has(o))) options.Add(o);
            return options.Count == 0 ? null : options[UnityEngine.Random.Range(0, options.Count)];
        }
    }
}
