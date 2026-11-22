using System;
using System.Collections.Generic;
using Evolve.Organs;
using Evolve.Survival;
using UnityEngine;

namespace Evolve.Creatures
{
    /// <summary>
    /// Small creature that wanders, flees from nearby players and tires after a
    /// sprint. Touch it to catch it: a slime swallows it alive (Nuốt Chửng) to digest or analyse it.
    /// Needs a kinematic Rigidbody and a non-trigger collider.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Prey : MonoBehaviour, IDamageable
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
        [Tooltip("Its genome: the organs Giải Mã can learn from it (read from this drop table)")]
        [SerializeField] DropTableSO drops;
        [Tooltip("Body mass: a slime swallows it whole if this is at most 80% of its own mass")]
        [SerializeField] float bodyMass = 3f;

        /// <summary>Area it stays inside (set by the spawner).</summary>
        public Vector3 HomeCenter { get; set; }
        public float HomeRadius { get; set; } = 25f;
        public bool IsTired => tiredUntil > Time.time;
        public float Health { get; private set; }
        public float MaxHealth => maxHealth;
        public float BodyMass => bodyMass;
        /// <summary>Set mass and health from code (the god menu's test creatures have no prefab to tune).</summary>
        public void SetStats(float mass, float health) { bodyMass = mass; maxHealth = health; Health = health; }
        /// <summary>A scavenger walks to this point (a carcass) instead of wandering; null = wander (CreatureBrain).</summary>
        public Vector3? Goal { get; set; }

        // ---- defence hooks (PreyDefense sets them) ----
        /// <summary>Speed multiplier while it runs (a skill).</summary>
        public float Boost { get; set; } = 1f;
        /// <summary>Until this time it flees from a threat at any distance.</summary>
        public float ForceFleeUntil { get; set; }

        /// <summary>A slime is latched on (Combat/LatchSession): killing it leaves a carcass instead of an instant swallow, so it keeps its genes.</summary>
        public bool Latched { get; set; }
        Evolve.Combat.CreatureCombat combat;
        /// <summary>Until this time it has learned to fear that slime and flees from farther away.</summary>
        public float RememberSlimeUntil { get; set; }

        /// <summary>A pounce (Vồ) by a slime that can swallow it whole: the normal catch.</summary>
        public void PounceCatch(SurvivalStats by) => TryCatch(by);
        public bool Zigzag { get; set; }
        /// <summary>No vibration pulse while it flees (Bướm Đêm).</summary>
        public bool Silent { get; set; }
        /// <summary>Damage taken as a share (0.5 = half) while it guards.</summary>
        public float DamageTakenMultiplier { get; set; } = 1f;
        /// <summary>Return true to stop a slime from catching it this moment.</summary>
        public System.Func<SurvivalStats, bool> CatchVeto { get; set; }
        /// <summary>A slime touched it (before the catch).</summary>
        public event Action<SurvivalStats> Touched;
        /// <summary>It took damage (amount, attacker).</summary>
        public event Action<float, SurvivalStats> Damaged;
        /// <summary>It can run again: no tiredness.</summary>
        public void RefreshStamina() { fleeTime = 0f; tiredUntil = 0f; }
        public void Heal(float amount) { if (amount > 0f) Health = Mathf.Min(maxHealth, Health + amount); }

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
        CreatureMotor motor;
        bool caught;
        Evolve.World.VibrationEmitter emitter;
        float nextFleePulse;

        CreatureLocomotion locomotion;

        void Awake()
        {
            if (!TryGetComponent(out motor)) motor = gameObject.AddComponent<CreatureMotor>();
            if (Evolve.Combat.MovesetSeed.Has(name) && !TryGetComponent(out combat)) combat = gameObject.AddComponent<Evolve.Combat.CreatureCombat>();   // the mosquito
            rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            NetId = NetWorld.NextId();
            TryGetComponent(out emitter);
            if (!TryGetComponent(out locomotion)) locomotion = gameObject.AddComponent<CreatureLocomotion>();
            string species = SpeciesNames.Key(name);
            if ((species == "bocanhcungnho" || species == "ocho") && !TryGetComponent<PreyDefense>(out _))
                gameObject.AddComponent<PreyDefense>();
            Health = maxHealth;
        }

        void OnEnable() => active.Add(this);
        void OnDisable() => active.Remove(this);

        void FixedUpdate()
        {
            if (Frozen || IsReplica || Evolve.Abilities.CreatureStatus.Holds(gameObject)) return;   // stunned or knocked by a skill
            if (combat != null && combat.Busy) return;   // hunting, draining or scared off: the combat component drives
            Vector3 pos = rb.position;
            Vector3 dir;
            float speed;

            Transform threat = NearestPlayer(pos, out float dist);
            if (IsTired)
            {
                dir = Vector3.zero; speed = 0f;
            }
            else if (threat != null && (dist < fleeRadius * (Time.time < RememberSlimeUntil ? 1.5f : 1f) || Time.time < ForceFleeUntil))
            {
                dir = pos - threat.position; dir.y = 0f; dir.Normalize();
                speed = fleeSpeed;
                fleeTime += Time.fixedDeltaTime;
                if (emitter != null && !Silent && Time.time >= nextFleePulse) { emitter.Pulse(0.4f); nextFleePulse = Time.time + 0.5f; }
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
                if (Goal.HasValue)
                {
                    Vector3 g = Goal.Value - pos; g.y = 0f;
                    if (g.magnitude > 1.2f) { dir = g.normalized; speed = wanderSpeed * 1.5f; } else { dir = Vector3.zero; speed = 0f; }
                }
            }

            if (Zigzag && threat != null && Time.time < ForceFleeUntil && dir.sqrMagnitude > 0.001f)
                dir = Quaternion.Euler(0f, Mathf.Sin(Time.time * 9f) * 35f, 0f) * dir;   // weaves while it runs

            // Stay near home: steer back toward the centre when near the edge.
            Vector3 fromHome = pos - HomeCenter; fromHome.y = 0f;
            if (fromHome.magnitude > HomeRadius * 0.8f)
                dir = (dir - fromHome.normalized * 1.5f).normalized;

            if (motor != null && motor.Profile.AvoidsWater)
            {
                Vector3 probe = pos + dir * 1.2f;
                if (Evolve.World.WaterField.DepthAt(probe.x, probe.z) > 0.05f)
                {
                    Vector3 away = SteerAwayFromWater(pos);
                    if (away.sqrMagnitude > 0.01f) dir = away;
                }
            }

            if (dir.sqrMagnitude > 0.001f)
            {
                if (!motor.Move(dir, speed * Boost * Evolve.Abilities.CreatureStatus.SpeedFactor(gameObject)))
                {
                    float a = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                    wanderDir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                    nextTurn = Time.time + UnityEngine.Random.Range(1.2f, 2.5f);
                }
                rb.MoveRotation(Quaternion.Slerp(rb.rotation, Quaternion.LookRotation(dir), 0.2f));
            }

            CheckRemoteCatch();
        }

        Vector3 SteerAwayFromWater(Vector3 pos)
        {
            for (int i = 0; i < 8; i++)
            {
                float ang = i * (Mathf.PI * 2f / 8f);
                Vector3 d = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
                Vector3 p = pos + d * 2.0f;
                if (Evolve.World.WaterField.DepthAt(p.x, p.z) <= 0.02f) return d;
            }
            return pos.sqrMagnitude > 0.1f ? -new Vector3(pos.x, 0f, pos.z).normalized : Vector3.forward;
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
                if (d.sqrMagnitude <= CatchRadius * CatchRadius) { TryCatch(p); return; }
            }
        }

        Transform NearestPlayer(Vector3 pos, out float dist)
        {
            Transform best = null; dist = float.MaxValue;
            foreach (var p in SurvivalStats.All)
            {
                if (p.State.IsDead || (CreatureBrain.Enabled && !FearsSlime(p))) continue;
                float d = Vector3.Distance(pos, p.transform.position);
                if (d < dist) { dist = d; best = p.transform; }
            }
            // hunters are threats too (a prey flees from whatever hunts it)
            if (!Predator.Passive)
                foreach (var h in Predator.Active)
                {
                    if (h == null) continue;
                    float d = Vector3.Distance(pos, h.transform.position);
                    if (d < dist) { dist = d; best = h.transform; }
                }
            return best;
        }

        void OnCollisionEnter(Collision c)
        {
            if (caught || IsReplica) return;
            var stats = c.collider.GetComponentInParent<SurvivalStats>();
            if (stats == null || stats.State.IsDead || stats.IsRemote) return;
            TryCatch(stats);
        }

        /// <summary>Hurt the prey (spikes and so on). At 0 health it dies and rewards the attacker like a catch.</summary>
        public void TakeDamage(float amount, SurvivalStats by)
        {
            if (by != null && amount > 0f) Evolve.Visuals.SlimeGelAnimator.Play(by.gameObject, Evolve.Visuals.GelMotion.Attack);
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

            amount *= DamageTakenMultiplier * (combat != null ? combat.DamageTakenFactor : 1f);
            Health -= amount;
            ShowHit(amount);
            Damaged?.Invoke(amount, by);
            if (Health > 0f) return;

            if (by != null && !Latched) Catch(by);
            else
            {
                caught = true;
                Caught?.Invoke(this);
                bool hasEc = TryGetComponent(out Evolve.Ecology.EmbodiedCreature ec);
                if (hasEc) ec.NotifyKilled();
                LeaveCarcass(hasEc ? ec.Region : null);
                Destroy(gameObject);
            }
        }

        /// <summary>It runs from a slime that could swallow it whole; one too big to swallow does not scare it (a slime climbs the food chain as it grows).</summary>
        bool FearsSlime(SurvivalStats p)
        {
            float mass = p.TryGetComponent(out Evolve.Swallow.SlimeBody body) ? body.Mass : 10f;
            return CreatureBehaviorRules.PreyFearsSlime(mass, bodyMass);
        }

        /// <summary>Killed by something other than a player (a hunter, a rival): the body stays for the scavengers.</summary>
        void LeaveCarcass(Evolve.Ecology.Region region)
        {
            Vector3 p = transform.position;
            var carcass = Evolve.Ecology.Carcass.Spawn(new Vector3(p.x, Evolve.World.GroundHeight.At(p.x, p.z, p.y) + 0.1f, p.z), Mathf.Max(0.4f, bodyMass * 0.15f), region);
            if (carcass == null) return;
            carcass.ResetFood(Mathf.Max(6f, bodyMass * 4f));
            var sample = carcass.gameObject.AddComponent<Evolve.Swallow.CarcassSample>();
            sample.Genes = Evolve.Swallow.Genome.From(drops, false);
            sample.Mass = bodyMass;
            sample.Label = Label;
        }

        void ShowHit(float amount)
        {
            HitFlash.Play(gameObject);
            locomotion?.PlayHit();
            CombatEvents.Raise(transform, amount);
        }

        /// <summary>A slime reached it: its defence may stop the catch (guarding, slipping out of the grab).</summary>
        void TryCatch(SurvivalStats stats)
        {
            Touched?.Invoke(stats);
            if (caught || (CatchVeto != null && CatchVeto(stats))) return;
            Catch(stats);
        }

        void Catch(SurvivalStats stats)
        {
            caught = true;

            // Nuốt Chửng: a local slime swallows it alive into Túi Không Đáy (fed when it is digested);
            // otherwise (remote player, stomach full, too big) it is eaten on the spot as before.
            var swallower = stats.GetComponent<Evolve.Swallow.Swallower>();
            bool swallowed = false;
            if (Evolve.Swallow.SwallowRules.ShouldUseStomach(stats.IsRemote, swallower != null))
            {
                swallowed = swallower.TrySwallowAlive(Label, bodyMass, nutrition, FoodKind.PreyMeat, Evolve.Swallow.Genome.From(drops, false));
                if (!swallowed) Evolve.Swallow.Echo.Say(swallower.Stomach.IsFull ? "«Túi Không Đáy đã đầy.»" : "«Con mồi quá lớn để nuốt nguyên.»");
            }
            if (!swallowed)
            {
                NetWorld.Feed(stats, nutrition, FoodProfiles.For(FoodKind.PreyMeat));
                Evolve.Visuals.SlimeGelAnimator.Play(stats.gameObject, Evolve.Visuals.GelMotion.Gulp);
            }

            Caught?.Invoke(this);
            if (TryGetComponent(out Evolve.Ecology.EmbodiedCreature ec)) ec.NotifyKilled();
            Destroy(gameObject);
        }

        string Label => name.Replace("(Clone)", "").Trim();
    }
}
