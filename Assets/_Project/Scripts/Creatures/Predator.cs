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
    public class Predator : MonoBehaviour, IDamageable
    {
        static readonly List<Predator> active = new List<Predator>();
        public static IReadOnlyList<Predator> Active => active;
        /// <summary>Debug: predators ignore players and never bite.</summary>
        public static bool Passive;

        /// <summary>A gentle cave lord (start cavern) ignores players until one hurts it (<see cref="CaveTier.IsCalm"/>).</summary>
        public bool Gentle { get; set; }
        Evolve.Combat.CreatureCombat combat;
        public bool IsCalm => Calm;
        public float ChaseSpeed => chaseSpeed;

        /// <summary>May this player be attacked right now: not in peaceful mode, not while a gentle lord is calm, not inside a safe zone, and the brain agrees.</summary>
        public bool WantsToAttack(SurvivalStats p) =>
            !Passive && !Calm && !SafeZone.Contains(p.transform.position) && (TargetFilter == null || TargetFilter(p));

        /// <summary>A slime is latched on (Combat/LatchSession).</summary>
        public bool Latched { get; set; }
        /// <summary>Until this time it has learned to watch for that slime from farther away.</summary>
        public float RememberSlimeUntil { get; set; }
        float provokedUntil;
        bool Calm => CaveTier.IsCalm(Gentle, provokedUntil, Time.time);

        /// <summary>Cave tier tuning: multiplies bite damage, detection range, and speeds (see CreatureTuning).</summary>
        public void ScaleDanger(float damage, float detect, float speed, float size = 1f)
        {
            reachScale *= size;
            biteDamage *= damage; detectRadius *= detect; chaseSpeed *= speed; patrolSpeed *= speed;
        }
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
        [Tooltip("Its genome: the organs Giải Mã can learn from its carcass (read from this drop table)")]
        [SerializeField] DropTableSO drops;
        [Tooltip("Body mass: a slime swallows the carcass whole if this is at most 80% of its own mass, else dissolves it")]
        [SerializeField] float bodyMass = 15f;
        [Tooltip("Pack leader (con đầu đàn): only leaders carry legendary genes")]
        [SerializeField] bool isLeader;

        public Vector3 HomeCenter { get; set; }
        public float HomeRadius { get; set; } = 12f;
        public float Health { get; private set; }
        public bool IsChasing => target != null;
        public bool IsTired => Time.time < tiredUntil;
        public float MaxHealth => maxHealth;
        public float BodyMass => bodyMass;
        /// <summary>Set mass and health from code (the god menu's test creatures have no prefab to tune).</summary>
        public void Heal(float amount) { if (!dead && amount > 0f) Health = Mathf.Min(maxHealth, Health + amount); }
        public void SetStats(float mass, float health) { bodyMass = mass; maxHealth = health; Health = health; }

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
            locomotion?.PlayHit();
            CombatEvents.Raise(transform, amount);
        }

        /// <summary>Id shared with other machines (the host hands it out).</summary>
        public int NetId { get; set; }
        /// <summary>Client side: a copy of a predator that really lives on the host. It reports hits to the host.</summary>
        public bool IsReplica { get; set; }

        const float BaseBiteReach = 1.1f;
        float reachScale = 1f;
        /// <summary>How near (centre to centre) it must be to bite: grows with the creature, or a big lord could never reach prey pushed out of its body.</summary>
        float BiteReach => BaseBiteReach * reachScale;

        public event Action<Predator> Killed;

        Rigidbody rb;
        SurvivalStats target;
        Vector3 wanderDir;
        CreatureMotor motor;
        float moveSpeed;
        float nextTurn, chaseTime, tiredUntil, retreatUntil, nextBite, blindUntil, lastAteAt;

        // ---- scene behaviour (CreatureBrain sets these; with no brain the old behaviour runs) ----
        /// <summary>What the brain wants now; None = patrol and hunt the players as before.</summary>
        [NonSerialized] public PredatorIntent Intent;
        /// <summary>Which players it may hunt (a slime much bigger than it is not hunted); null = all.</summary>
        public Func<SurvivalStats, bool> TargetFilter;
        const float FullSeconds = 60f, HungerRiseSeconds = 120f;
        /// <summary>0 = just ate, 1 = starving: after a minute fed it gets hungry over two more.</summary>
        public float Hunger01 => Mathf.Clamp01((Time.time - lastAteAt - FullSeconds) / HungerRiseSeconds);
        public void NoteAte() => lastAteAt = Time.time;
        /// <summary>Sets how hungry it is right now (0 fed, 1 starving); the god menu spawns test hunters hungry so they go hunting at once.</summary>
        public void SetHunger01(float h) => lastAteAt = Time.time - FullSeconds - Mathf.Clamp01(h) * HungerRiseSeconds;
        bool dead;

        CreatureLocomotion locomotion;

        void Awake()
        {
            if (!TryGetComponent(out motor)) motor = gameObject.AddComponent<CreatureMotor>();
            rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            if (!TryGetComponent(out locomotion)) locomotion = gameObject.AddComponent<CreatureLocomotion>();
            if (Evolve.Combat.MovesetSeed.Has(name) && !TryGetComponent(out combat)) combat = gameObject.AddComponent<Evolve.Combat.CreatureCombat>();   // telegraphed attacks
            Health = maxHealth;
            lastAteAt = Time.time - UnityEngine.Random.Range(FullSeconds + HungerRiseSeconds * 0.1f, FullSeconds + HungerRiseSeconds * 0.7f);   // they start at different, already noticeable hunger, so they hunt soon
            HomeCenter = transform.position;
            NetId = NetWorld.NextId();
        }

        void OnEnable() => active.Add(this);
        void OnDisable() => active.Remove(this);

        void FixedUpdate()
        {
            if (dead || Frozen || IsReplica || Evolve.Abilities.CreatureStatus.Holds(gameObject)) return;   // stunned or knocked by a skill
            if (combat != null && combat.Busy) return;   // winding up, striking or recovering: the move set drives
            Vector3 pos = rb.position;
            UpdateTarget(pos);
            if (target == null && combat != null && !Passive && !Calm) target = combat.EngagedTarget(24f);   // its moves reach 18 m: if they can fire, it must walk toward the player, not wander off
            if (target == null && Intent.Mode != IntentMode.None) { MoveByIntent(pos); BiteRemotePlayers(); return; }   // the brain is in charge

            Vector3 dir = Vector3.zero, look = Vector3.zero;
            float speed = 0f;

            if (target != null && combat == null && Time.time < retreatUntil)
            {
                dir = Flat(pos - target.transform.position); speed = patrolSpeed;
            }
            else if (target != null)
            {
                if (combat != null)   // a moveset creature: run in when far, stand and face the player once in striking distance, never tire or back off
                {
                    Vector3 to = Flat(target.transform.position - pos, false);
                    speed = combat.Steer(chaseSpeed, to, out dir, out look);
                    if (speed <= 0f && to.sqrMagnitude > 1e-4f) rb.MoveRotation(Quaternion.Slerp(rb.rotation, Quaternion.LookRotation(look), 0.15f));
                }
                else if (!IsTired)
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
                var lure = Evolve.Traits.TraitSense.Lure(pos);   // Dễ Bị Chú Ý: drift toward the player instead of wandering
                if (lure != null) dir = (Flat(lure.transform.position - pos) * 0.8f + wanderDir * 0.2f).normalized;

                Vector3 fromHome = Flat(pos - HomeCenter, false);
                if (fromHome.magnitude > HomeRadius * 0.8f)
                    dir = (dir - fromHome.normalized * 1.5f).normalized;

                if (motor != null && motor.Profile.AvoidsWater)
                {
                    Vector3 probe = pos + dir * 1.5f;
                    if (Evolve.World.WaterField.DepthAt(probe.x, probe.z) > 0.05f)
                    {
                        Vector3 away = SteerAwayFromWater(pos);
                        if (away.sqrMagnitude > 0.01f) dir = away;
                    }
                }

                // Cóc Hồ Già: ở lòng hồ và bờ thoai thoải, không trèo lên vách hang dốc đứng
                if (motor != null && motor.Profile.CanSwim && (name.IndexOf("CocHoGia", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("coc_ho_gia", StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    float distFromCenter = Evolve.World.CavernLayout.RadiusFromCenter(pos.x, pos.z);
                    if (distFromCenter > Evolve.World.CavernLayout.FloorRadius - 3.5f)
                    {
                        Vector3 toCenter = new Vector3(-pos.x, 0f, -pos.z).normalized;
                        dir = (dir + toCenter * 2.2f).normalized;
                    }
                }
            }

            if (speed <= 0f) moveSpeed = 0f;
            if (dir.sqrMagnitude > 0.001f && speed > 0f)
            {
                if (combat != null) moveSpeed = Mathf.MoveTowards(moveSpeed, speed, 5f * Time.fixedDeltaTime); else moveSpeed = speed;   // a moveset creature builds up speed instead of starting at full speed at once
                if (!motor.Move(dir, moveSpeed * Evolve.Abilities.CreatureStatus.SpeedFactor(gameObject)))
                {
                    float a = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                    wanderDir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                    nextTurn = Time.time + UnityEngine.Random.Range(1.8f, 3.5f);
                }
                rb.MoveRotation(Quaternion.Slerp(rb.rotation, Quaternion.LookRotation(look.sqrMagnitude > 1e-6f ? look : dir), 0.15f));
            }

            BiteRemotePlayers();
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

        /// <summary>Carries out the brain's intent: chase and bite a creature, walk to a point, hold, or run.</summary>
        void MoveByIntent(Vector3 pos)
        {
            Vector3 dir = Vector3.zero;
            float speed = 0f;
            var it = Intent;
            switch (it.Mode)
            {
                case IntentMode.Chase:
                    if (it.Target == null) break;
                    Vector3 to = it.Target.position - pos; to.y = 0f;
                    float d = to.magnitude;
                    if (d > BiteReach) { dir = to.normalized; speed = chaseSpeed; }
                    if (d <= BiteReach * 1.3f && Time.time >= nextBite) { nextBite = Time.time + biteCooldown; Strike(it.Target, it.Meal); }
                    if (dir.sqrMagnitude < 0.001f && to.sqrMagnitude > 1e-4f) dir = to.normalized * 0.001f;   // keep facing it
                    break;
                case IntentMode.GoTo:
                    Vector3 g = it.Point - pos; g.y = 0f;
                    if (g.magnitude > it.Radius) { dir = g.normalized; speed = patrolSpeed; }
                    break;
                case IntentMode.Avoid:
                    if (it.Target == null) break;
                    dir = Flat(pos - it.Target.position); speed = chaseSpeed;
                    break;
            }
            if (dir.sqrMagnitude > 1e-7f)
            {
                if (speed > 0f) motor.Move(dir, speed * Evolve.Abilities.CreatureStatus.SpeedFactor(gameObject));
                rb.MoveRotation(Quaternion.Slerp(rb.rotation, Quaternion.LookRotation(dir.normalized), 0.15f));
            }
        }

        /// <summary>A bite at another creature (prey, or a rival hunter). Eating a kill makes it full.</summary>
        void Strike(Transform victim, bool meal)
        {
            if (victim == null || !victim.TryGetComponent(out IDamageable damageable)) return;
            locomotion?.PlayAction("Attack");
            damageable.TakeDamage(biteDamage, null);
            bool dead = damageable is Prey p ? p.Health <= 0f : damageable is Predator h && h.Health <= 0f;
            if (dead && meal) NoteAte();   // a rival's body is no meal: the scavengers get it
        }

        // Remote players have no collider here (they are ghosts), so they are bitten by being close enough.
        // The damage is sent to the player's own machine.
        void BiteRemotePlayers()
        {
            if (Passive || Calm || Time.time < nextBite) return;   // remote (co-op) players are still bitten here: moves only target the local slime
            Vector3 pos = rb.position;
            foreach (var p in SurvivalStats.All)
            {
                if (!p.IsRemote || p.State.IsDead || SafeZone.Contains(p.transform.position)) continue;
                Vector3 d = p.transform.position - pos; d.y = 0f;
                if (d.sqrMagnitude > BiteReach * BiteReach) continue;

                nextBite = Time.time + biteCooldown;
                target = p;
                if (combat == null) retreatUntil = Time.time + retreatTime;
                NetWorld.BiteRemote?.Invoke(p, biteDamage, Flat(p.transform.position - transform.position) * knockback);

                var host = p.GetComponent<OrganHost>();
                if (host != null && host.ContactDamage > 0f) TakeDamage(host.ContactDamage, p);
                return;
            }
        }

        /// <summary>Prey defence (flash, smoke, hiding): drop the target and ignore everyone for a while.</summary>
        public void LoseTarget(float seconds) { target = null; blindUntil = Mathf.Max(blindUntil, Time.time + seconds); }

        /// <summary>A prey called for help (Chim Non): go for this player.</summary>
        public void Provoke(SurvivalStats player)
        {
            if (dead || IsReplica || Passive || Calm || player == null || player.State.IsDead || Time.time < blindUntil) return;
            target = player;
        }

        void UpdateTarget(Vector3 pos)
        {
            if (Passive || Calm || Time.time < blindUntil) { target = null; return; }
            if (target != null && (SafeZone.Contains(target.transform.position) || (TargetFilter != null && !TargetFilter(target)))) target = null;
            if (target != null)
            {
                if (target.State.IsDead || Vector3.Distance(pos, target.transform.position) > loseRadius
                    || Vector3.Distance(target.transform.position, HomeCenter) > HomeRadius + loseRadius)
                    target = null;
                return;
            }
            foreach (var p in SurvivalStats.All)
            {
                if (p.State.IsDead || SafeZone.Contains(p.transform.position) || (TargetFilter != null && !TargetFilter(p))) continue;
                float radius = detectRadius * (Time.time < RememberSlimeUntil ? 1.3f : 1f) * (p.IsMoving ? 1f : stillDetectFactor) * Evolve.Traits.TraitSense.DetectMultiplier(p) * Evolve.Abilities.BodyState.DetectMultiplier(p);   // Nhân Ngụy Sắc, Phát Sáng
                if (Vector3.Distance(pos, p.transform.position) <= radius || Evolve.Traits.TraitSense.SmellsTrail(p, pos)) { target = p; break; }   // Vệt Mùi
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
            if ((Passive || Calm) && stats != null)
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

            if (combat != null) return;   // a monster with a move set strikes with its moves, not by touching
            if (Time.time < nextBite || Evolve.Abilities.CreatureStatus.Holds(gameObject)) return;
            nextBite = Time.time + biteCooldown;
            target = stats;

            stats.State.Damage(biteDamage);
            var playerRb = stats.GetComponent<Rigidbody>();
            if (playerRb != null)
                if (!playerRb.isKinematic) playerRb.AddForce(Flat(stats.transform.position - transform.position) * knockback + Vector3.up * 2f, ForceMode.VelocityChange);
            if (combat == null) retreatUntil = Time.time + retreatTime;

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
            if (by != null && Gentle) { provokedUntil = Time.time + CaveTier.ProvokeSeconds; target = by; }   // a gentle lord fights back at whoever hurt it
            if (combat != null) amount *= combat.DamageTakenFactor * combat.ShellFactor(by);   // open to a counter while recovering; the scorpion's shell turns a blow from the front
            Health -= amount;
            if (combat == null) retreatUntil = Time.time + retreatTime;
            ShowHit(amount);
            if (Health > 0f) return;

            dead = true;
            // A slime with Nuốt Chửng swallows the carcass (F) to learn from it; anyone else is fed on the spot as before.
            if (by != null && !Evolve.Swallow.SwallowRules.ShouldUseStomach(by.IsRemote, by.GetComponent<Evolve.Swallow.Swallower>() != null))
                NetWorld.Feed(by, nutrition, FoodProfiles.For(FoodKind.PredatorMeat));
            Debug.Log($"{name} bị hạ gục");
            Killed?.Invoke(this);
            bool hasEc = TryGetComponent(out Evolve.Ecology.EmbodiedCreature ec);
            if (hasEc) ec.NotifyKilled();
            var carcass = Evolve.Ecology.Carcass.Spawn(new Vector3(transform.position.x, Evolve.World.GroundHeight.At(transform.position.x, transform.position.z, 0f) + 0.2f, transform.position.z), 1.5f, hasEc ? ec.Region : null);
            if (carcass != null)
            {
                var sample = carcass.gameObject.AddComponent<Evolve.Swallow.CarcassSample>();
                sample.Genes = Evolve.Swallow.Genome.From(drops, isLeader);
                sample.Mass = bodyMass;
                sample.Label = name.Replace("(Clone)", "").Trim();
            }
            Destroy(gameObject);
        }
    }
}
