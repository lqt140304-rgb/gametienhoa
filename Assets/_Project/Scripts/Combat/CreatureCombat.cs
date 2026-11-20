using System.Collections.Generic;
using Evolve.Abilities;
using Evolve.Creatures;
using Evolve.Survival;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Evolve.Combat
{
    /// <summary>
    /// A monster's attacks (spec 2026-11-20): runs the species' move set (<see cref="MovesetSeed"/>) on a Predator or a Prey. It picks a move
    /// with <see cref="MoveScheduler"/>, shows the tell as a body pose (and the species' animation trigger when its animator has one),
    /// strikes, then is open to a counter. While it is winding up, striking or recovering the host's own behaviour is paused (<see cref="Busy"/>).
    /// The mosquito (a drain move) is run by <see cref="LeechRules"/> instead: it hunts in a swarm, drains blood and is shaken off.
    /// </summary>
    [DefaultExecutionOrder(60)]
    public class CreatureCombat : MonoBehaviour
    {
        const float HitReach = 0.9f, SwarmRadius = 15f, FlySpeed = 2.2f, ScaredSpeed = 3f, DrainTick = 0.5f;
        static readonly Color TellColor = new Color(1f, 0.35f, 0.3f);
        static readonly List<CreatureCombat> all = new List<CreatureCombat>();
        public static IReadOnlyList<CreatureCombat> All => all;

        public float DamageScale { get; set; } = 1f;
        float tellScale = 1f;
        public float TellScale { get => tellScale; set { tellScale = value; if (scheduler != null) scheduler.TellScale = value; } }
        public bool Busy => scheduler != null && (scheduler.Busy || (leech != null && leech.State != LeechState.Idle));
        /// <summary>For the god menu: the leech state, or the move phase and name.</summary>
        public string Status => scheduler == null ? "-" : leech != null ? leech.State.ToString() : scheduler.Phase + (scheduler.Current != null ? " " + scheduler.Current.Name : "");
        public bool IsArmored => scheduler != null && scheduler.IsArmored;
        public float DamageTakenFactor => scheduler != null ? scheduler.DamageTakenFactor : 1f;

        IReadOnlyList<MoveDef> moves;
        MoveScheduler scheduler;
        LeechRules leech;
        Predator pred;
        Prey prey;
        CreatureMotor motor;
        CreatureLocomotion loco;
        Rigidbody rb;
        Collider col;
        Transform model;
        Vector3 baseScale;
        Quaternion lastRot = Quaternion.identity; Vector3 lastPos;
        bool poseApplied;
        SurvivalStats target;
        MovePhase prevPhase;
        Vector3 strikeDir;
        bool hitDone, hopFlag, skillFlag, hitFlag;
        float drainAccum;
        LeechState prevLeech;

        void Awake()
        {
            moves = MovesetSeed.For(name);
            if (moves.Count == 0) { enabled = false; return; }
            scheduler = new MoveScheduler(moves, Random.Range(int.MinValue, int.MaxValue)) { TellScale = tellScale };
            if (moves[0].Shape == MoveShape.Drain) leech = new LeechRules();
            pred = GetComponent<Predator>(); prey = GetComponent<Prey>();
            motor = GetComponent<CreatureMotor>(); loco = GetComponent<CreatureLocomotion>(); rb = GetComponent<Rigidbody>();
            col = GetComponentInChildren<Collider>();
            model = transform.Find("Model");
            if (model == null) foreach (var r in GetComponentsInChildren<Renderer>()) { model = r.transform; break; }
            if (model != null) baseScale = model.localScale;
        }

        void OnEnable()
        {
            all.Add(this);
            SkillBar.SkillUsed += OnSkillUsed;
            if (prey != null) prey.Damaged += OnDamaged;
        }

        void OnDisable()
        {
            all.Remove(this);
            SkillBar.SkillUsed -= OnSkillUsed;
            if (prey != null) prey.Damaged -= OnDamaged;
            scheduler?.Reset();
            RestoreModel();
        }

        void OnSkillUsed(SurvivalStats user) { if (user == target) skillFlag = true; }
        void OnDamaged(float amount, SurvivalStats by) { if (by != null && amount > 0f) hitFlag = true; }

        /// <summary>One mosquito noticed a player: every idle mosquito of the species within the swarm radius joins the hunt.</summary>
        public static void Alert(Vector3 center, float radius, string speciesKey)
        {
            foreach (var c in all)
                if (c != null && c.leech != null && SpeciesNames.Key(c.name) == speciesKey && (c.transform.position - center).sqrMagnitude <= radius * radius) c.leech.Alert();
        }

        float Radius => col != null ? Mathf.Max(col.bounds.extents.x, col.bounds.extents.z) : 0.4f;
        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

        void Update()
        {
            if (leech != null && Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) hopFlag = true;
        }

        SurvivalStats FindTarget()
        {
            SurvivalStats best = null; float bestD = float.MaxValue;
            foreach (var p in SurvivalStats.All)
            {
                if (p == null || p.IsRemote || p.State.IsDead) continue;
                if (pred != null && !pred.WantsToAttack(p)) continue;
                float d = Flat(p.transform.position - transform.position).sqrMagnitude;
                if (d < bestD) { bestD = d; best = p; }
            }
            return best;
        }

        bool HostPaused() =>
            (pred != null && (Predator.Frozen || pred.IsReplica || pred.Health <= 0f)) || (prey != null && (Prey.Frozen || prey.IsReplica)) || CreatureStatus.Holds(gameObject);

        void FixedUpdate()
        {
            if (HostPaused()) { scheduler.Reset(); hopFlag = skillFlag = hitFlag = false; return; }
            target = FindTarget();
            if (leech != null) { FixedLeech(); return; }

            float health = pred != null ? pred.HealthFraction : prey != null ? prey.HealthFraction : 1f;
            float dist = target != null ? Flat(target.transform.position - transform.position).magnitude : float.MaxValue;
            var started = scheduler.Update(Time.fixedDeltaTime, target != null, dist, health);
            if (scheduler.Phase != prevPhase) OnPhase(scheduler.Phase, scheduler.Current);
            if (started != null) BeginStrike(started);
            prevPhase = scheduler.Phase;

            if (target != null && scheduler.Phase == MovePhase.Windup) FaceTarget();
            if (scheduler.Phase == MovePhase.Active) TickStrike(scheduler.Current);
        }

        void FaceTarget()
        {
            Vector3 d = Flat(target.transform.position - transform.position);
            if (d.sqrMagnitude > 1e-4f) rb.MoveRotation(Quaternion.Slerp(rb.rotation, Quaternion.LookRotation(d), 0.25f));
        }

        /// <summary>Animation triggers by phase, named after the move's Anim ("Tell_Attack", "Strike_Attack", "Recover_Attack"); a missing trigger is skipped.</summary>
        void OnPhase(MovePhase phase, MoveDef m)
        {
            if (m == null) return;
            if (phase == MovePhase.Windup) BeginWindup(m);
            else if (phase == MovePhase.Active) loco?.PlayAction("Strike_" + m.Anim);
            else if (phase == MovePhase.Recover) loco?.PlayAction("Recover_" + m.Anim);
        }

        void BeginWindup(MoveDef m)
        {
            loco?.PlayAction(m.Anim);
            loco?.PlayAction("Tell_" + m.Anim);
            if (m.Shape == MoveShape.Spin || m.Shape == MoveShape.Call)
                SkillFx.Ring(transform.position, m.Radius, TellColor, m.Windup * tellScale, 0.3f);   // a ring on the ground shows how far the blow reaches
        }

        void BeginStrike(MoveDef m)
        {
            hitDone = false;
            strikeDir = target != null ? Flat(target.transform.position - transform.position).normalized : transform.forward;
            if (strikeDir.sqrMagnitude < 1e-4f) strikeDir = transform.forward;
            if (m.Shape == MoveShape.Spin) Hurt(m, Radius + m.Radius);
            else if (m.Shape == MoveShape.Call) CallPack(m);
        }

        void TickStrike(MoveDef m)
        {
            if (m.Shape != MoveShape.Lunge && m.Shape != MoveShape.Charge) return;
            if (m.Active > 0f && motor != null && !motor.Move(strikeDir, m.Distance / m.Active))
            {
                if (m.Shape == MoveShape.Charge) { CreatureStatus.Stun(this, 2f); scheduler.Stun(2f); loco?.PlayAction("Stun"); return; }   // hit a wall: dazed
            }
            if (!hitDone && target != null && Flat(target.transform.position - transform.position).magnitude <= Radius + HitReach) { Hurt(m, Radius + HitReach); hitDone = true; }
        }

        void Hurt(MoveDef m, float reach)
        {
            foreach (var p in SurvivalStats.All)
            {
                if (p == null || p.IsRemote || p.State.IsDead) continue;
                Vector3 away = Flat(p.transform.position - transform.position);
                if (away.magnitude > reach) continue;
                p.State.Damage(m.Damage * DamageScale);
                var prb = p.GetComponent<Rigidbody>();
                if (prb != null) prb.AddForce(away.normalized * 5f + Vector3.up * 2f, ForceMode.VelocityChange);
                if (m.Shape != MoveShape.Spin) break;   // a lunge or a charge hits one player
            }
        }

        void CallPack(MoveDef m)
        {
            if (target == null) return;
            foreach (var h in Predator.Active)
                if (h != null && h != pred && SpeciesNames.Key(h.name) == m.TargetSpecies && (h.transform.position - transform.position).sqrMagnitude <= m.Radius * m.Radius) h.Provoke(target);
        }

        // ---------- the mosquito ----------

        void FixedLeech()
        {
            var m = moves[0];
            var input = new LeechInput
            {
                PlayerAlive = target != null,
                PlayerDistance = target != null ? Vector3.Distance(target.transform.position, transform.position) : float.MaxValue,
                PlayerSpeed = target != null && target.TryGetComponent(out Rigidbody prb) ? Flat(prb.linearVelocity).magnitude : 0f,
                Hopped = hopFlag, SkillUsed = skillFlag, Hit = hitFlag,
            };
            hopFlag = skillFlag = hitFlag = false;
            float drained = leech.Update(Time.fixedDeltaTime, input);
            if (leech.State == LeechState.Hunt && prevLeech == LeechState.Idle) Alert(transform.position, SwarmRadius, SpeciesNames.Key(name));

            switch (leech.State)
            {
                case LeechState.Hunt:
                    if (target != null) Fly(target.transform.position - transform.position, FlySpeed);
                    break;
                case LeechState.Drain:
                    if (target != null)
                    {
                        Vector3 around = new Vector3(Mathf.Cos(Time.time * 3f), 0f, Mathf.Sin(Time.time * 3f)) * 0.35f;
                        rb.MovePosition(target.transform.position + Vector3.up * 0.55f + around);   // hovering on the slime
                        drainAccum += drained;
                        if (drainAccum >= DrainTick * m.Damage)
                        {
                            target.State.Damage(drainAccum);
                            prey?.Heal(drainAccum);   // it feeds on what it takes
                            drainAccum = 0f;
                        }
                    }
                    break;
                case LeechState.Scared:
                    if (target != null) Fly(transform.position - target.transform.position, ScaredSpeed);
                    break;
            }
            if (leech.State != prevLeech)
            {
                if (leech.State == LeechState.Drain) loco?.PlayAction("Feed");
                else if (leech.State == LeechState.Scared) loco?.PlayAction("Flee");
            }
            if (prevLeech == LeechState.Drain && leech.State != LeechState.Drain) drainAccum = 0f;
            prevLeech = leech.State;
        }

        void Fly(Vector3 dir, float speed)
        {
            dir.y = 0f;
            if (dir.sqrMagnitude < 1e-4f || motor == null) return;
            motor.Move(dir, speed);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, Quaternion.LookRotation(dir.normalized), 0.2f));
        }

        // ---------- the tell: the body's shape ----------

        void LateUpdate()
        {
            if (model == null || scheduler == null) return;
            Pose pose = Pose.Identity;
            var m = scheduler.Current;
            if (leech != null)
            {
                if (leech.State != LeechState.Idle) pose = TellPose.Evaluate(PoseKind.Hover, MovePhase.Windup, 1f);   // a quivering mosquito while it hunts and feeds
            }
            else if (m != null) pose = TellPose.Evaluate(MovesetSeed.PoseOf(m), scheduler.Phase, scheduler.Progress01);
            if (MovesetSeed.AuthoredClips.Contains(SpeciesNames.Key(name))) pose = Pose.Identity;   // the real clips carry the body language
            Apply(pose);
        }

        void Apply(Pose p)
        {
            // CreatureLocomotion (order 50) writes the model's position and rotation each frame; only undo my last offset when it left them alone
            if (poseApplied)
            {
                if (Quaternion.Angle(model.localRotation, lastRot) < 0.01f) model.localRotation = lastRotBefore;
                if ((model.localPosition - lastPos).sqrMagnitude < 1e-8f) model.localPosition = lastPosBefore;
            }
            lastRotBefore = model.localRotation; lastPosBefore = model.localPosition;
            Vector3 shake = p.Shake > 0f ? Random.insideUnitSphere * 0.03f * p.Shake : Vector3.zero;
            model.localScale = new Vector3(baseScale.x * p.ScaleX, baseScale.y * p.ScaleY, baseScale.z * p.ScaleZ);
            model.localRotation = model.localRotation * Quaternion.Euler(p.PitchDeg, 0f, 0f);
            model.localPosition += new Vector3(0f, p.OffsetY, p.OffsetZ) + shake;
            lastRot = model.localRotation; lastPos = model.localPosition;
            poseApplied = true;
        }

        Quaternion lastRotBefore = Quaternion.identity; Vector3 lastPosBefore;

        void RestoreModel()
        {
            if (model == null) return;
            model.localScale = baseScale;
            if (poseApplied) { model.localRotation = lastRotBefore; model.localPosition = lastPosBefore; }
            poseApplied = false;
        }
    }
}
