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
        public bool Busy => scheduler != null && (scheduler.Busy || (leech != null && leech.State != LeechState.Idle) || (ambush != null && ambush.State == AmbushState.Lurking) || perch != PerchState.None);
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
        AmbushRules ambush;
        Renderer[] renderers;
        GameObject eyes;
        static Material eyeMat;
        bool hidden, iceArmed;
        Vector3 iceSpot, retreatDir;
        float retreatLeft;
        static readonly Color IceColor = new Color(0.55f, 0.85f, 1f);

        enum PerchState { None, Jumping, Perched, Dropping }
        PerchState perch;
        Vector3 perchFrom, perchTo, perchOrigin, ledgePoint;
        (float vx, float vy, float vz) perchVel;
        float perchT, perchFlight, perchUntil, noTargetFor, nextLedgeCheck;
        bool ledgeFound;
        readonly List<Ledge> ledgeBuffer = new List<Ledge>();
        const float DropSeconds = 0.6f;

        /// <summary>A lurking creature: hidden, only a pair of glowing eyes shows.</summary>
        public bool IsHidden => hidden;

        void Awake()
        {
            moves = MovesetSeed.For(name);
            if (moves.Count == 0) { enabled = false; return; }
            scheduler = new MoveScheduler(moves, Random.Range(int.MinValue, int.MaxValue)) { TellScale = tellScale };
            if (moves[0].Shape == MoveShape.Drain) leech = new LeechRules();
            if (MovesetSeed.IsAmbusher(name)) ambush = new AmbushRules();
            foreach (var mv in moves) if (mv.Shape == MoveShape.Perch)
                scheduler.Allow = m => perch == PerchState.Perched ? (m.Shape == MoveShape.Throw || m.Shape == MoveShape.Call) : m.Shape != MoveShape.Perch || FindLedge(m);   // on a ledge it only throws and calls; a leap needs a ledge to leap to
            pred = GetComponent<Predator>(); prey = GetComponent<Prey>();
            motor = GetComponent<CreatureMotor>(); loco = GetComponent<CreatureLocomotion>(); rb = GetComponent<Rigidbody>();
            col = GetComponentInChildren<Collider>();
            model = transform.Find("Model");
            if (model == null) foreach (var r in GetComponentsInChildren<Renderer>()) { model = r.transform; break; }
            if (model != null) baseScale = model.localScale;
            if (ambush != null) { renderers = GetComponentsInChildren<Renderer>(); SetHidden(true); }   // starts lurking
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
            SetHidden(false);
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
            TickPerch();
            if (HostPaused())
            {
                scheduler.Reset(); hopFlag = skillFlag = hitFlag = false; iceArmed = false; retreatLeft = 0f;
                if (ambush != null && ambush.State == AmbushState.Lurking) { ambush.Update(0f, 0f); ambush.StrikeDone(); }   // stunned or killed while hiding: it shows itself
                SetHidden(false);
                return;
            }
            target = FindTarget();
            if (leech != null) { FixedLeech(); return; }

            float health = pred != null ? pred.HealthFraction : prey != null ? prey.HealthFraction : 1f;
            float dist = target != null ? Flat(target.transform.position - transform.position).magnitude : float.MaxValue;
            if (ambush != null)
            {
                if (ambush.Update(Time.fixedDeltaTime, dist)) { SetHidden(false); scheduler.Force(MovesetSeed.AmbushMove(name), 0.2f); }   // bursts out with a quick tell
                else if (ambush.State == AmbushState.Lurking) { if (!hidden) SetHidden(true); scheduler.Reset(); prevPhase = MovePhase.Idle; return; }
                else if (ambush.State == AmbushState.Revealed && !scheduler.Busy) ambush.StrikeDone();
            }
            noTargetFor = target != null ? 0f : noTargetFor + Time.fixedDeltaTime;
            var started = scheduler.Update(Time.fixedDeltaTime, target != null, perch == PerchState.Perched ? dist * 0.875f : dist, health);   // from a ledge it throws up to 16 m, not 14
            if (scheduler.Phase != prevPhase) OnPhase(scheduler.Phase, scheduler.Current);
            if (started != null) BeginStrike(started);
            prevPhase = scheduler.Phase;

            if (target != null && scheduler.Phase == MovePhase.Windup) FaceTarget();
            if (scheduler.Phase == MovePhase.Active) TickStrike(scheduler.Current);
            else if (scheduler.Phase == MovePhase.Recover && retreatLeft > 0f) TickRetreat(scheduler.Current);
        }

        void TickRetreat(MoveDef m)
        {
            float speed = m.RetreatDistance / Mathf.Max(0.1f, m.Recover);
            if (motor == null || !motor.Move(retreatDir, speed)) { retreatLeft = 0f; return; }   // a wall stops the run
            retreatLeft -= speed * Time.fixedDeltaTime;
        }

        // ---------- lurking ----------

        void SetHidden(bool h)
        {
            if (renderers == null || h == hidden) return;
            hidden = h;
            foreach (var r in renderers) if (r != null) r.enabled = !h;
            if (h) MakeEyes(); else if (eyes != null) Destroy(eyes);
        }

        void MakeEyes()
        {
            if (eyeMat == null) eyeMat = SkillFx.Gel(new Color(1f, 0.8f, 0.25f), 1f, 0f, 3f);
            eyes = new GameObject("Mắt Rình");
            eyes.transform.SetParent(transform, false);
            eyes.transform.localPosition = Vector3.up * Radius * 0.9f + Vector3.forward * Radius * 0.6f;
            for (int i = -1; i <= 1; i += 2)
            {
                var eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(eye.GetComponent<Collider>());
                eye.transform.SetParent(eyes.transform, false);
                eye.transform.localPosition = Vector3.right * 0.1f * i;
                eye.transform.localScale = Vector3.one * 0.08f;
                eye.GetComponent<MeshRenderer>().sharedMaterial = eyeMat;
            }
            var light = eyes.AddComponent<Light>();
            light.type = LightType.Point; light.range = 2.5f; light.intensity = 1.2f; light.color = new Color(1f, 0.8f, 0.3f); light.shadows = LightShadows.None;
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
            else if (phase == MovePhase.Recover)
            {
                loco?.PlayAction("Recover_" + m.Anim);
                if (m.RetreatDistance > 0f)   // hit and run: it bolts away from the player
                {
                    retreatLeft = m.RetreatDistance;
                    Vector3 away = target != null ? Flat(transform.position - target.transform.position) : -transform.forward;
                    retreatDir = away.sqrMagnitude > 1e-4f ? away.normalized : -transform.forward;
                }
                if (m.Shape == MoveShape.IceDrop) IceImpact(m);
            }
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
            if (m.Shape == MoveShape.IceDrop && target != null)
            {
                iceSpot = target.transform.position; iceArmed = true;
                SkillFx.Ring(iceSpot, m.Radius, IceColor, m.Active, 0.3f);   // the ring under the player's feet: step out before the block falls
            }
            if (m.Shape == MoveShape.Throw && target != null)
                MonsterProjectile.Launch(transform.position + Vector3.up * Radius * 1.5f, target.transform.position + Vector3.up * 0.2f, m.FlightSeconds, m.Damage * DamageScale);
            if (m.Shape == MoveShape.Perch) BeginPerch(m);
            if (m.Shape == MoveShape.Spin) Hurt(m, Radius + m.Radius);
            else if (m.Shape == MoveShape.Call) CallPack(m);
        }

        void TickStrike(MoveDef m)
        {
            if (m.Shape == MoveShape.Gust) { Gust(m); return; }
            if (m.Shape != MoveShape.Lunge && m.Shape != MoveShape.Charge) return;
            if (m.Active > 0f && motor != null && !motor.Move(strikeDir, m.Distance / m.Active))
            {
                if (m.Shape == MoveShape.Charge) { CreatureStatus.Stun(this, 2f); scheduler.Stun(2f); loco?.PlayAction("Stun"); return; }   // hit a wall: dazed
            }
            if (!hitDone && target != null && Flat(target.transform.position - transform.position).magnitude <= Radius + HitReach) { Hurt(m, Radius + HitReach); hitDone = true; }
        }

        // ---------- leaping up to a ledge ----------

        /// <summary>Is there a ledge worth leaping to? Looks at 16 directions and 3 distances around the creature, at most twice a second.</summary>
        bool FindLedge(MoveDef m)
        {
            if (Time.time < nextLedgeCheck) return ledgeFound;
            nextLedgeCheck = Time.time + 0.5f;
            ledgeFound = false;
            if (target == null) return false;
            Vector3 pos = rb.position;
            ledgeBuffer.Clear();
            for (int d = 0; d < 16; d++)
                foreach (float r in new[] { 3f, 5f, 7f })
                {
                    if (r > m.Distance) continue;
                    float a = d * Mathf.PI * 2f / 16f;
                    var at = new Vector3(pos.x + Mathf.Cos(a) * r, pos.y + m.MaxHeight + 1f, pos.z + Mathf.Sin(a) * r);
                    if (Physics.Raycast(at, Vector3.down, out var hit, m.MaxHeight + 3f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) && hit.normal.y > 0.7f && hit.collider.attachedRigidbody == null)
                        ledgeBuffer.Add(new Ledge(hit.point.x, hit.point.y, hit.point.z));
                }
            Vector3 tp = target.transform.position;
            float groundY = Evolve.World.GroundHeight.At(pos.x, pos.z, pos.y);
            int best = PerchRules.PickLedge(ledgeBuffer, pos.x, groundY, pos.z, tp.x, tp.z, m.MinHeight, m.MaxHeight, m.Distance);
            if (best < 0) return false;
            var l = ledgeBuffer[best];
            ledgePoint = new Vector3(l.X, l.Y, l.Z);
            return ledgeFound = true;
        }

        void BeginPerch(MoveDef m)
        {
            if (!ledgeFound) return;   // Allow checked it a moment ago; the ledge can vanish between checks
            Vector3 pos = rb.position;
            float offset = pos.y - Evolve.World.GroundHeight.At(pos.x, pos.z, pos.y);   // how high the pivot sits above the ground
            perchOrigin = pos; perchFrom = pos;
            perchTo = new Vector3(ledgePoint.x, ledgePoint.y + offset, ledgePoint.z);
            perchFlight = Mathf.Max(0.1f, m.Active);
            perchVel = ThrowRules.Launch(perchFrom.x, perchFrom.y, perchFrom.z, perchTo.x, perchTo.y, perchTo.z, perchFlight);
            perchT = 0f; perch = PerchState.Jumping;
            perchUntil = Time.time + m.Active + m.PerchSeconds;
        }

        void TickPerch()
        {
            if (perch == PerchState.None) return;
            if (perch == PerchState.Perched)
            {
                if (!scheduler.Busy && (Time.time >= perchUntil || noTargetFor > 3f)) StartDrop();
                return;
            }
            perchT += Time.fixedDeltaTime;
            var p = ThrowRules.PositionAt(perchFrom.x, perchFrom.y, perchFrom.z, perchVel, Mathf.Min(perchT, perchFlight));
            rb.MovePosition(new Vector3(p.x, p.y, p.z));
            if (perchT < perchFlight) return;
            if (perch == PerchState.Jumping) { perch = PerchState.Perched; return; }
            perch = PerchState.None;   // back on the ground
            nextLedgeCheck = 0f;
        }

        void StartDrop()
        {
            perchFrom = rb.position; perchTo = perchOrigin; perchFlight = DropSeconds;
            perchVel = ThrowRules.Launch(perchFrom.x, perchFrom.y, perchFrom.z, perchTo.x, perchTo.y, perchTo.z, perchFlight);
            perchT = 0f; perch = PerchState.Dropping;
        }

        void Gust(MoveDef m)
        {
            if (target == null) return;
            Vector3 away = Flat(target.transform.position - transform.position);
            if (away.magnitude > m.MaxRange) return;
            bool holds = (target.TryGetComponent(out BodyState bs) && (bs.Rooted || bs.Climbing)) || LatchSession.Active != null;   // Thu Mình, Bám Vách, or already gripping something
            float mass = target.TryGetComponent(out Evolve.Swallow.SlimeBody body) ? body.Mass : 10f;
            float speed = WindRules.PushSpeed(m.Force, mass, holds);
            if (speed > 0f) WindPush.Apply(target, away.normalized * speed, 0.15f);
        }

        /// <summary>The ice block lands where the ring was: anyone still standing in it is hurt and frozen.</summary>
        void IceImpact(MoveDef m)
        {
            if (!iceArmed) return;
            iceArmed = false;
            foreach (var p in SurvivalStats.All)
            {
                if (p == null || p.IsRemote || p.State.IsDead || Flat(p.transform.position - iceSpot).magnitude > m.Radius) continue;
                p.State.Damage(m.Damage * DamageScale);
                BodyState.On(p.gameObject).Root(m.Freeze);
                if (p.TryGetComponent(out Evolve.Player.SlimeController sc)) sc.FreezeFor(m.Freeze);
            }
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
                Impulse.Kick(prb, away.normalized * 5f + Vector3.up * 2f);
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
