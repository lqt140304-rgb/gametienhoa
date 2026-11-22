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
        float tellScale = 1f, poseStrength = 1f;
        public float TellScale { get => tellScale; set { tellScale = value; if (scheduler != null) scheduler.TellScale = value; } }
        public bool Busy => scheduler != null && (scheduler.Busy || (leech != null && leech.State != LeechState.Idle) || (ambush != null && ambush.State == AmbushState.Lurking) || perch != PerchState.None);
        /// <summary>For the god menu: the leech state, or the move phase and name.</summary>
        public MovePhase Phase => scheduler != null ? scheduler.Phase : MovePhase.Idle;
        /// <summary>The player this creature's moves are aimed at, if within <paramref name="radius"/> metres: the body must walk toward it even when the Predator's own sight has lost it (otherwise it wanders off while its moves still fire).</summary>
        public SurvivalStats EngagedTarget(float radius) =>
            target != null && Flat(target.transform.position - transform.position).sqrMagnitude <= radius * radius ? target : null;
                float standOff = -1f; bool standing;
        int circleSign = 1; float nextFlip;
        /// <summary>Between two moves a boss that sizes up its prey walks round it (see <see cref="CircleRules"/>): the speed and direction to walk, and where to face (mostly along the walk, partly at the prey); otherwise the plain approach.</summary>
        public float Steer(float chase, Vector3 to, out Vector3 dir, out Vector3 face)
        {
            dir = Flat(to).normalized; face = dir;
            if (scheduler == null || !scheduler.Circling || target == null) return ApproachSpeed(chase, to);
            if (Time.time >= nextFlip) { circleSign = Random.value < 0.5f ? 1 : -1; nextFlip = Time.time + Random.Range(CircleRules.FlipMin, CircleRules.FlipMax); }
            var d = CircleRules.Steer(to.x, to.z, circleSign, CircleRules.Ring);
            dir = new Vector3(d.x, 0f, d.z);
            face = Flat(to).normalized;   // it always looks at the prey while it walks round
            return CircleRules.Speed;
        }

        /// <summary>How fast the body should walk toward the player: 0 once it is inside striking distance (it stands and faces them), a run when far, otherwise <paramref name="chase"/>. The body used to push into the player at 1.4 m/s forever.</summary>
        public float ApproachSpeed(float chase, Vector3 to)
        {
            if (standOff < 0f)
            {
                standOff = 3f;
                foreach (var m in MovesetSeed.For(name)) if (m.Role == MoveRole.Normal && m.MinRange <= 0f) standOff = Mathf.Min(standOff == 3f ? 99f : standOff, m.MaxRange * 0.8f);
                if (standOff > 50f) standOff = 3f;
            }
            float d = Flat(to).magnitude;
            if (standing) { if (d > standOff * 1.3f) standing = false; else return 0f; }   // hysteresis: once it has stopped it does not creep forward and stop again every time the player shuffles
            else if (d <= standOff) { standing = true; return 0f; }
            return d > 9f ? Mathf.Max(chase, 3.8f) : d > 5f ? Mathf.Max(chase, 2.6f) : chase;
        }
                public float PhaseProgress => scheduler != null ? scheduler.Progress01 : 0f;
        float faintUntil, faintStart;
        /// <summary>A pounce that found nothing: the hound lies knocked out on its side for a moment.</summary>
        public bool IsFainted => Time.time < faintUntil;
        public float FaintTime => Time.time - faintStart;

        public void Faint(float seconds)
        {
            faintStart = Time.time; faintUntil = Time.time + seconds;
            Stagger(seconds);   // held still and open to a counter (x1.5 damage) the whole time
        }

        /// <summary>Keep only the moves that pass the filter (a summoned copy gets the bites and claws, not the summoning).</summary>
        public void LimitMoves(System.Func<MoveDef, bool> keep)
        {
            var kept = new List<MoveDef>();
            foreach (var mv in moves) if (keep(mv)) kept.Add(mv);
            if (kept.Count == 0) return;
            moves = kept;
            scheduler = new MoveScheduler(moves, Random.Range(int.MinValue, int.MaxValue)) { TellScale = tellScale, MinIdle = CircleRules.IdleSeconds(name) };
        }

        public MoveDef CurrentMove => scheduler != null ? scheduler.Current : null;

        /// <summary>A heavy blow staggers it: the move in progress ends in a recover and it is held still for a moment (open to a counter).</summary>
        public void Stagger(float seconds)
        {
            if (scheduler == null || seconds <= 0f) return;
            CreatureStatus.Stun(this, seconds);
            scheduler.Stun(seconds);
        }

        public string Status => scheduler == null ? "-" : leech != null ? leech.State.ToString() : scheduler.Phase + (scheduler.Current != null ? " " + scheduler.Current.Name : "");
        public bool IsArmored => scheduler != null && scheduler.IsArmored;
        public float DamageTakenFactor => scheduler != null ? scheduler.DamageTakenFactor * (pred != null ? PassiveRules.DamageFactor(name, pred.HealthFraction) : 1f) : 1f;

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
        float strikeDist;
        bool hound, golem;
        BirdMoves bird;   // Chim Hang Mẹ's flying moves (Swoop, WindSlash, Talon): it moves the body while they run
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

        // ---------- wind spears (Cáo Gió) ----------
        Transform spearRoot;
        readonly List<Transform> spears = new List<Transform>();
        int spearsLaunched;
        float orbitStart, nextOrbitSound;
        static Material spearMat;
        const float OrbitDegPerSecond = 540f;

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
            poseStrength = MovesetSeed.PoseStrength(name);
            hound = SpeciesNames.Key(name) == "choxuong";
            golem = SpeciesNames.Key(name) == "golemthachanh";
            if (ChoXuongRigFx.Has(name)) ChoXuongRigFx.Attach(gameObject);   // attach the rig effects here as well as in CreatureTuning: a creature that never passed through a cave still gets them
            if (GolemRigFx.Has(name)) GolemRigFx.Attach(gameObject);
            if (BirdRigFx.Has(name)) BirdRigFx.Attach(gameObject);
            if (SpeciesNames.Key(name) == "chimhangme" && !TryGetComponent(out bird)) bird = gameObject.AddComponent<BirdMoves>();
            scheduler = new MoveScheduler(moves, Random.Range(int.MinValue, int.MaxValue)) { TellScale = tellScale, MinIdle = CircleRules.IdleSeconds(name) };
            if (bird != null) scheduler.Allow = m => bird.CanUse(m, target);   // a grab needs a player it can carry
            SetupPhases();
            if (PhaseRules.Has(name) && !TryGetComponent<MoveVfx>(out _)) gameObject.AddComponent<MoveVfx>();   // the charge and aftermath of every move
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

        // ---------- phases and reading the player (PhaseRules, ReadRules) ----------
        readonly PhaseTracker phases = new PhaseTracker();
        readonly PlayerReader reader = new PlayerReader();
        PhaseMods phaseMods = PhaseMods.None;
        MoveDef transitionMove;
        float baseIdle, appliedDamage = 1f, appliedSpeed = 1f;
        Vector3 windupAt, windupTargetAt; bool windupTracked;
        Rigidbody targetBody;

        /// <summary>The boss's phase: 1 from the start, 2 below 66% health, 3 below 33%.</summary>
        public int BossPhase => phases.Phase;
        /// <summary>What the boss has made of the player's habits.</summary>
        public PlayerStyle ReadStyle => reader.Style;

        void SetupPhases()
        {
            transitionMove = PhaseRules.TransitionMove(name);
            baseIdle = CircleRules.IdleSeconds(name);
            scheduler.WeightScale = m => reader.Weight(m);
        }

        void TickPhases()
        {
            if (transitionMove != null && pred != null && phases.Update(pred.HealthFraction)) ApplyPhase();
            if (transitionMove != null && phases.Pending && target != null && scheduler.Phase == MovePhase.Idle && perch == PerchState.None && leech == null && scheduler.Force(transitionMove, transitionMove.Windup * tellScale))
                phases.Ack();   // it stops what it was doing and roars: the change of phase is felt
            if (target != null)
            {
                if (targetBody == null || targetBody.gameObject != target.gameObject) target.TryGetComponent(out targetBody);
                reader.Observe(Time.fixedDeltaTime, Flat(target.transform.position - transform.position).magnitude, targetBody != null ? Flat(targetBody.linearVelocity).magnitude : 0f);
            }
        }

        void ApplyPhase()
        {
            phaseMods = PhaseRules.For(name, phases.Phase);
            scheduler.CooldownScale = phaseMods.Cooldown; scheduler.ComboBonus = phaseMods.ComboBonus; scheduler.MinIdle = baseIdle * phaseMods.Idle;
            DamageScale *= phaseMods.Damage / appliedDamage; appliedDamage = phaseMods.Damage;
            if (pred != null && Mathf.Abs(phaseMods.Speed - appliedSpeed) > 1e-4f) { pred.ScaleDanger(1f, 1f, phaseMods.Speed / appliedSpeed); appliedSpeed = phaseMods.Speed; }
            Debug.Log("[Pha trùm] " + name + " vào pha " + phases.Phase);
        }

        /// <summary>Where to aim at the player: ahead of where they are going (more for a runner) and a little toward the side they have been sidestepping to.</summary>
        Vector3 AimAt(float seconds, float sideMeters)
        {
            Vector3 at = target.transform.position;
            if (targetBody != null) { var l = reader.Lead(targetBody.linearVelocity.x, targetBody.linearVelocity.z, seconds); at += new Vector3(l.x, 0f, l.z); }
            Vector3 to = Flat(target.transform.position - transform.position);
            if (to.sqrMagnitude > 1e-4f && sideMeters > 0f) at += Vector3.Cross(Vector3.up, to.normalized) * (reader.PredictedLateral() * -sideMeters);   // + lateral is the player's right, as seen from the boss
            return at;
        }

        /// <summary>How far the player moved sideways during the tell, as seen from the boss: that is a sidestep, and the boss remembers which way.</summary>
        void NoteSidestep()
        {
            if (!windupTracked || target == null) return;
            windupTracked = false;
            Vector3 to0 = Flat(windupTargetAt - windupAt);
            if (to0.sqrMagnitude < 1e-4f) return;
            Vector3 right = Vector3.Cross(Vector3.up, to0.normalized) * -1f;
            reader.NoteDodge(Vector3.Dot(target.transform.position - windupTargetAt, right));
        }

        // ---------- Giáng Thạch (the golem's special): the leap, the landing, the spikes and the rock volley ----------
        bool meteorActive;
        Vector3 meteorFrom, meteorLand;
        float leapOffset;
        VolleyFsm volley;
        readonly List<RockSpike> spikeObjs = new List<RockSpike>();
        RockSpike heldRock;
        Transform rightHand;
        const float WalkSpeed = 3.2f;

        /// <summary>True while it is up in the air (the leap and the fall): nothing that depends on walking (footsteps) should run.</summary>
        public bool Airborne => meteorActive && scheduler != null && (scheduler.Phase == MovePhase.Active || (scheduler.Phase == MovePhase.Windup && scheduler.Progress01 > 0.5f));
        /// <summary>It has pulled a spike out of the ground and is aiming it.</summary>
        public bool HoldingRock => volley != null && volley.Stage == VolleyStage.Hold;
        /// <summary>The rock volley's stage and the seconds spent in it, or null when it is not under way (the rig poses the body by it).</summary>
        public VolleyStage? VolleyNow => volley != null && meteorActive ? volley.Stage : (VolleyStage?)null;
        public float VolleyTime => volley != null ? volley.StageTime : 0f;
        /// <summary>Catching its breath at the end of a move (see <see cref="MoveDef.Rest"/>).</summary>
        public bool Resting => scheduler != null && scheduler.Resting;
        public float RestTime => scheduler != null ? scheduler.RestTime : 0f;
        public float RestLength => scheduler != null ? scheduler.RestLength : 0f;
        float dizzyUntil;
        /// <summary>Dazed after charging head first into a wall.</summary>
        public bool Dizzy => Time.time < dizzyUntil;

        float GroundY(float x, float z, float fallback) => Evolve.World.GroundHeight.At(x, z, fallback);

        void StartLeap(MoveDef m)
        {
            meteorActive = true;
            meteorFrom = rb.position;
            leapOffset = Evolve.World.GroundHeight.OffsetOf(rb.position);
            Vector3 t = target != null ? target.transform.position : transform.position + transform.forward * m.MinRange;
            meteorLand = new Vector3(t.x, 0f, t.z);
            SkillFx.Ring(new Vector3(t.x, GroundY(t.x, t.z, t.y) + 0.05f, t.z), m.Radius, TellColor, m.Windup * tellScale, 0.3f);   // the ring on the ground: where it will land (it does not follow the player)
            ClearMeteorObjects();
            leapSfx = false;
            GolemFx.Point(transform.position);
        }
        bool leapSfx;

        void TickLeapUp(MoveDef m)
        {
            if (!leapSfx && scheduler.Progress01 >= 0.45f) { leapSfx = true; GolemFx.Leap(transform.position); }   // the spring: the sound comes when it leaves the ground, not at the pointing
            float h = LeapRules.Height(MovePhase.Windup, scheduler.Progress01);
            rb.MovePosition(new Vector3(meteorFrom.x, meteorFrom.y + h, meteorFrom.z));
        }

        void TickLeapFall(MoveDef m)
        {
            float p = scheduler.Progress01;
            Vector3 flat = Vector3.Lerp(meteorFrom, meteorLand, LeapRules.Travel(MovePhase.Active, p));
            float gy = GroundY(flat.x, flat.z, meteorFrom.y) + leapOffset;
            rb.MovePosition(new Vector3(flat.x, gy + LeapRules.Height(MovePhase.Active, p), flat.z));
            if (!hitDone && p >= 0.95f) Land(m);
        }

        /// <summary>It hits the ground on the marked circle: damage and knock-back, a shock, and the stone spikes rise round it.</summary>
        void Land(MoveDef m)
        {
            hitDone = true;
            Vector3 spot = new Vector3(meteorLand.x, GroundY(meteorLand.x, meteorLand.z, meteorFrom.y) + leapOffset, meteorLand.z);
            rb.position = spot; transform.position = spot;
            Hurt(m, m.Radius);
            GolemFx.Land(spot, m.Radius);
            float startAngle = 0f;
            if (target != null) { Vector3 to = Flat(target.transform.position - spot); startAngle = Mathf.Atan2(to.z, to.x) * Mathf.Rad2Deg; }   // the first spike rises on the player's side
            float scale = Mathf.Max(0.5f, transform.lossyScale.y);
            foreach (var (x, z) in SpikeRules.Ring(spot.x, spot.z, m.SpikeCount + phaseMods.ExtraSpikes, m.SpikeRadius, startAngle))
                spikeObjs.Add(RockSpike.Spawn(new Vector3(x, GroundY(x, z, spot.y), z), 1.1f * scale, 3.2f * scale));
            GolemFx.SpikesRise(spot, m.SpikeRadius);
            volley = new VolleyFsm(spikeObjs.Count);
        }

        void TickVolley(MoveDef m)
        {
            if (volley == null) { SnapToGround(); return; }   // knocked out of the leap before it landed
            if (volley.Stage == VolleyStage.Done) return;
            if (volley.Stage == VolleyStage.Rise) { if (volley.Update(Time.fixedDeltaTime, false) == VolleyEvent.Roared) ShockRoar(m); return; }   // getting up from the landing crouch, then the roar
            if (volley.Stage == VolleyStage.Roar || volley.Stage == VolleyStage.Release) { volley.Update(Time.fixedDeltaTime, false); return; }   // roaring, or holding the follow-through of a throw
            var spike = volley.Index < spikeObjs.Count ? spikeObjs[volley.Index] : null;
            if (spike == null) { volley.Update(VolleyFsm.WalkTimeout, true); return; }   // the spike is gone: skip it
            Vector3 to = Flat(spike.transform.position - transform.position);
            bool arrived = to.magnitude <= Radius + 0.9f;
            if (volley.Stage == VolleyStage.Walk && !arrived)
            {
                if (target != null) FaceTarget();   // it keeps its eyes on the player while it walks to the spike
                if (motor != null) motor.Move(to.normalized, WalkSpeed, false);   // it walks to the next spike (no detours: they flip the heading every tick)
            }
            else if (volley.Stage == VolleyStage.Pull && target != null) FaceTarget();
            else if (volley.Stage == VolleyStage.Hold) FaceTarget();
            var e = volley.Update(Time.fixedDeltaTime, arrived);
            if (e == VolleyEvent.PullStart) { spike.BeginLift(VolleyFsm.PullSeconds); GolemFx.Pull(spike.transform.position); }   // both arms take the column and draw it out slowly
            else if (e == VolleyEvent.Picked)
            {
                if (rightHand == null) foreach (var t in GetComponentsInChildren<Transform>(true)) if (t.name == "RightHand") { rightHand = t; break; }
                heldRock = spike;
                spike.Grab(rightHand != null ? rightHand : transform);
            }
            else if (e == VolleyEvent.Threw) ThrowHeld(m);
        }

        /// <summary>The roar after the landing: a shock wave of sound throws every player in range back.</summary>
        void ShockRoar(MoveDef m)
        {
            Vector3 at = transform.position;
            GolemFx.Roar(at, m.RoarRadius);
            foreach (var p in SurvivalStats.All)
            {
                if (p == null || p.IsRemote || p.State.IsDead) continue;
                Vector3 away = Flat(p.transform.position - at);
                if (away.magnitude > m.RoarRadius) continue;
                if (away.sqrMagnitude < 1e-4f) away = transform.forward;
                float k = 1f - 0.5f * Mathf.Clamp01(away.magnitude / m.RoarRadius);   // the closer, the harder
                Impulse.Kick(p.GetComponent<Rigidbody>(), away.normalized * m.RoarForce * k + Vector3.up * m.RoarForce * 0.35f * k);
                if (p.TryGetComponent(out Evolve.Player.SlimeController sc)) sc.FreezeFor(0.6f);
            }
        }

        void ThrowHeld(MoveDef m)
        {
            if (heldRock == null) return;
            Vector3 from = heldRock.transform.position;
            Vector3 to = target != null ? AimAt(m.FlightSeconds, 0f) + Vector3.up * 0.2f : from + transform.forward * 8f;
            var rock = heldRock; heldRock = null;
            rock.Release();
            MonsterProjectile.LaunchBody(rock.gameObject, to, m.FlightSeconds, m.MissileDamage * DamageScale, 1f);   // the real stone column flies, not a stand-in
            GolemFx.Hurl(from);
        }

        void SnapToGround()
        {
            Vector3 p = rb.position;
            float gy = GroundY(p.x, p.z, p.y) + leapOffset;
            if (p.y > gy + 0.05f) { rb.position = new Vector3(p.x, gy, p.z); transform.position = rb.position; }
        }

        void ClearMeteorObjects()
        {
            foreach (var s in spikeObjs) if (s != null) s.Sink();
            spikeObjs.Clear();
            if (heldRock != null) { heldRock.transform.SetParent(null, true); heldRock.Sink(); heldRock = null; }
            volley = null;
        }

        /// <summary>The move ended or was cut short: the unused spikes sink, and the golem is back on the floor.</summary>
        void EndMeteor()
        {
            if (!meteorActive) return;
            meteorActive = false;
            ClearMeteorObjects();
            if (rb != null) SnapToGround();
        }

        // ---------- passive (PassiveRules): damage taken is in DamageTakenFactor; speed and mending are here ----------
        float quietSince, lastPassiveHealth = -1f;
        bool speedApplied;

        void TickPassive()
        {
            if (pred == null || !PassiveRules.Has(name)) return;
            float hp01 = pred.HealthFraction;
            if (lastPassiveHealth < 0f || pred.Health < lastPassiveHealth - 0.01f) quietSince = Time.time;   // it was hit: the quiet clock starts again
            lastPassiveHealth = pred.Health;
            float regen = PassiveRules.RegenPerSecond(name, Time.time - quietSince);
            if (regen > 0f && pred.Health < pred.MaxHealth) { pred.Heal(regen * pred.MaxHealth * Time.fixedDeltaTime); lastPassiveHealth = pred.Health; }
            float speed = PassiveRules.SpeedFactor(name, hp01);
            if (speed > 1f && !speedApplied) { speedApplied = true; pred.ScaleDanger(1f, 1f, speed); }   // blood lust: once, for good
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
            ClearSpears(); EndMeteor();
            bird?.Abort();
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
            TickPassive();
            if (HostPaused())
            {
                scheduler.Reset(); hopFlag = skillFlag = hitFlag = false; iceArmed = false; retreatLeft = 0f;
                ClearSpears(); EndMeteor();
                bird?.Abort();   // stunned or killed in the air: it lets go of the player and falls
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
            TickPhases();
            scheduler.HoldRecover = (meteorActive && volley != null && volley.Stage != VolleyStage.Done) || (bird != null && bird.Holding);   // the rock volley (or the bird's carry and rake) is not cut short by the recover time: the last rock is always pulled and thrown
            var started = scheduler.Update(Time.fixedDeltaTime, target != null, perch == PerchState.Perched ? dist * 0.875f : dist, health);   // from a ledge it throws up to 16 m, not 14
            if (scheduler.Phase != prevPhase) OnPhase(scheduler.Phase, scheduler.Current);
            if (started != null) BeginStrike(started);
            prevPhase = scheduler.Phase;

            if (meteorActive && scheduler.Current != null && scheduler.Current.Shape == MoveShape.Meteor)
            {
                if (scheduler.Phase == MovePhase.Windup) TickLeapUp(scheduler.Current);
                else if (scheduler.Phase == MovePhase.Active) TickLeapFall(scheduler.Current);
                else if (scheduler.Phase == MovePhase.Recover) TickVolley(scheduler.Current);
            }
            bird?.Tick(scheduler.Current, scheduler.Phase, scheduler.Progress01, scheduler.PhaseTime, target);
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
            if (target == null) return;   // the player died or left: keep the heading
            Vector3 d = Flat(target.transform.position - transform.position);
            if (d.sqrMagnitude > 1e-4f) rb.MoveRotation(Quaternion.Slerp(rb.rotation, Quaternion.LookRotation(d), 0.25f));
        }

        /// <summary>Animation triggers by phase, named after the move's Anim ("Tell_Attack", "Strike_Attack", "Recover_Attack"); a missing trigger is skipped.</summary>
        void OnPhase(MovePhase phase, MoveDef m)
        {
            if (phase == MovePhase.Idle) EndMeteor();   // the move is over (m is null here)
            if (m == null) return;
            if (hound) HoundFx.OnPhase(this, phase, m);
            if (golem) GolemFx.OnPhase(this, phase, m);
            if (bird != null) BirdFx.OnPhase(this, phase, m);
            if (m.Anim == "PhaseRoar") PhaseFx.OnPhase(this, phase, m);
            if (phase == MovePhase.Windup) BeginWindup(m);
            else if (phase == MovePhase.Active) loco?.PlayAction("Strike_" + m.Anim);
            else if (phase == MovePhase.Recover)
            {
                loco?.PlayAction("Recover_" + m.Anim);
                if (m.Pins && !hitDone) Faint(HoundRules.FaintSeconds);   // the pounce found nothing: it tumbles and lies knocked out
                ClearSpears();   // any spear not yet thrown fades away
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
            if (target != null && !m.Invulnerable) { windupAt = transform.position; windupTargetAt = target.transform.position; windupTracked = true; }
            loco?.PlayAction(m.Anim);
            loco?.PlayAction("Tell_" + m.Anim);
            if (m.Shape == MoveShape.Spears) MakeSpears(m);
            if (m.Shape == MoveShape.Meteor) StartLeap(m);
            if (m.Shape == MoveShape.Spin || m.Shape == MoveShape.Call || m.Shape == MoveShape.Roar || m.Shape == MoveShape.Summon)
                SkillFx.Ring(transform.position, m.Radius, TellColor, m.Windup * tellScale, 0.3f);   // a ring on the ground shows how far the blow reaches
        }

        void BeginStrike(MoveDef m)
        {
            hitDone = false;
            NoteSidestep();
            strikeDir = target != null ? Flat((m.Shape == MoveShape.Charge && m.Ramp ? AimAt(m.Active * 0.5f, 2f) : target.transform.position) - transform.position).normalized : transform.forward;   // the charge runs at where the player is going, not where they were
            if (strikeDir.sqrMagnitude < 1e-4f) strikeDir = transform.forward;
            // a pounce that must land covers the distance to the target (plus a little), not always its full length
            strikeDist = m.Pins && target != null ? HoundRules.LungeDistance(Flat(target.transform.position - transform.position).magnitude, m.Distance) : m.Distance;
            if (m.Shape == MoveShape.IceDrop && target != null)
            {
                iceSpot = target.transform.position; iceArmed = true;
                SkillFx.Ring(iceSpot, m.Radius, IceColor, m.Active, 0.3f);   // the ring under the player's feet: step out before the block falls
            }
            if (m.Shape == MoveShape.Throw && target != null)
                MonsterProjectile.Launch(transform.position + Vector3.up * Radius * 1.5f, AimAt(m.FlightSeconds, 0f) + Vector3.up * 0.2f, m.FlightSeconds, m.Damage * DamageScale, m.MissileSize > 0f ? 1f : 0.6f, m.Anim == "Spit", m.MissileSize);
            if (m.Shape == MoveShape.Perch) BeginPerch(m);
            if (m.Shape == MoveShape.Spin) Hurt(m, Radius + m.Radius);
            else if (m.Shape == MoveShape.Call) CallPack(m);
            else if (m.Shape == MoveShape.Roar) Roar(m);
            else if (m.Shape == MoveShape.Summon) HoundMinion.Summon(this, m, target);
        }

        void TickStrike(MoveDef m)
        {
            if (m.Shape == MoveShape.Gust) { Gust(m); return; }
            if (m.Shape == MoveShape.Spears) { TickSpears(m); return; }
            if (m.Shape != MoveShape.Lunge && m.Shape != MoveShape.Charge) return;
            float chargeSpeed = m.Active > 0f ? strikeDist / m.Active * (m.Ramp ? ChargeRules.Factor(scheduler.Progress01) : 1f) : 0f;   // a ramped charge starts slowly and builds up
            if (m.Active > 0f && motor != null && !motor.Move(strikeDir, chargeSpeed, !m.Ramp))   // a ramped charge never detours: it runs straight into the wall
            {
                if (m.Shape == MoveShape.Charge)
                {
                    float daze = m.Ramp ? 3f : 2f;
                    CreatureStatus.Stun(this, daze); scheduler.Stun(daze); loco?.PlayAction("Stun"); dizzyUntil = Time.time + daze;   // hit a wall: dazed
                    if (m.Ramp) GolemFx.WallBump(transform.position + strikeDir * Radius);
                    return;
                }
            }
            if (!hitDone && target != null && Flat(target.transform.position - transform.position).magnitude <= Radius + HitReach)
            {
                Vector3 to = Flat(target.transform.position - transform.position);
                if (m.Pins && Vector3.Dot(transform.forward, to.normalized) > 0.5f && HoundPin.Begin(this, target, m.Grab))
                    target.State.Damage(m.Damage * DamageScale);   // head first: it clamps on (HoundPin), no knock-back
                else Hurt(m, Radius + HitReach);
                hitDone = true;
            }
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

        /// <summary>Three spears of wind appear above the tails and circle there for the whole windup: the tell.</summary>
        void MakeSpears(MoveDef m)
        {
            ClearSpears();
            spearRoot = new GameObject("Giáo Gió").transform;
            spearRoot.SetParent(transform, false);
            spearRoot.localPosition = new Vector3(0f, Radius * 1.6f, -Radius * 0.5f);   // above the back, where the tails are
            if (spearMat == null) spearMat = SkillFx.Gel(new Color(0.58f, 0.8f, 1f), 1f, 0.9f, 3.4f);   // pale sky blue, like a rasengan
            float size = Mathf.Clamp(Radius, 0.4f, 2f);
            for (int i = 0; i < m.SpearCount; i++)
            {
                var sp = GameObject.CreatePrimitive(PrimitiveType.Capsule).transform;
                Destroy(sp.GetComponent<Collider>());
                sp.name = "Mũi Giáo";
                sp.SetParent(spearRoot, false);
                sp.localScale = new Vector3(0.09f, 0.5f, 0.09f) * size;
                sp.GetComponent<MeshRenderer>().sharedMaterial = spearMat;
                var trail = sp.gameObject.AddComponent<TrailRenderer>();
                trail.time = 0.3f; trail.startWidth = 0.14f * size; trail.endWidth = 0f; trail.sharedMaterial = spearMat; trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                spears.Add(sp);
            }
            spearsLaunched = 0; orbitStart = Time.time;
            ProceduralSfx.Play(ProceduralSfx.Kind.SpearOrbit, transform.position, 0.8f);
            nextOrbitSound = Time.time + 0.45f;
        }

        void UpdateSpears()
        {
            if (spearRoot == null || scheduler == null) return;
            float t = Time.time - orbitStart, radius = Radius * 0.9f;
            for (int i = spearsLaunched; i < spears.Count; i++)
            {
                if (spears[i] == null) continue;
                var o = SpearRules.OrbitOffset(i, spears.Count, t, radius, OrbitDegPerSecond, 0f);
                spears[i].localPosition = new Vector3(o.x, o.y, o.z);
                float h = SpearRules.OrbitHeadingDeg(i, spears.Count, t, OrbitDegPerSecond) * Mathf.Deg2Rad;
                spears[i].localRotation = Quaternion.FromToRotation(Vector3.up, new Vector3(Mathf.Cos(h), 0f, Mathf.Sin(h)));
            }
            if (Time.time >= nextOrbitSound && spearsLaunched < spears.Count) { ProceduralSfx.Play(ProceduralSfx.Kind.SpearOrbit, transform.position, 0.5f); nextOrbitSound = Time.time + 0.45f; }
        }

        /// <summary>Throws the spears one after another during the strike, each at where the player is at that moment.</summary>
        void TickSpears(MoveDef m)
        {
            float t = scheduler.PhaseTime;
            while (spearsLaunched < spears.Count && t + 1e-4f >= SpearRules.LaunchTime(spearsLaunched, m.LaunchInterval))
            {
                var sp = spears[spearsLaunched++];
                if (sp == null) continue;
                Vector3 aim = target != null ? target.transform.position + Vector3.up * 0.3f : sp.position + transform.forward * 10f;
                ProceduralSfx.Play(ProceduralSfx.Kind.SpearLaunch, sp.position, 1f);
                MonsterSpear.Launch(sp, aim, m.SpearSpeed, m.Damage * DamageScale, m.Force);
            }
        }

        /// <summary>Removes the spears still circling (the ones already thrown fly on by themselves) and the circle's pivot.</summary>
        void ClearSpears()
        {
            for (int i = spearsLaunched; i < spears.Count; i++) if (spears[i] != null) Destroy(spears[i].gameObject);
            spears.Clear(); spearsLaunched = 0;
            if (spearRoot != null) { Destroy(spearRoot.gameObject); spearRoot = null; }
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
                float push = m.Force > 0f ? m.Force : 5f;   // a heavy move (the golem's) throws the player much harder
                Impulse.Kick(prb, away.normalized * push + Vector3.up * (m.Force > 0f ? push * 0.45f : 2f));
                if (m.Freeze > 0f) { if (p.TryGetComponent(out Evolve.Player.SlimeController stunned)) stunned.FreezeFor(m.Freeze); }   // winded for a moment
                if (m.Shape != MoveShape.Spin) break;   // a lunge or a charge hits one player
            }
        }

        /// <summary>The roar: smaller creatures in range flee for FearSeconds; a smaller player is pushed back and frozen for Freeze seconds.</summary>
        void Roar(MoveDef m)
        {
            float mass = pred != null ? pred.BodyMass : prey != null ? prey.BodyMass : 50f;
            foreach (var o in Prey.Active)
                if (o != null && o != prey && o.BodyMass < mass && Flat(o.transform.position - transform.position).magnitude <= m.Radius)
                    o.ForceFleeUntil = Mathf.Max(o.ForceFleeUntil, Time.time + m.FearSeconds);
            foreach (var p in SurvivalStats.All)
            {
                if (p == null || p.IsRemote || p.State.IsDead) continue;
                Vector3 away = Flat(p.transform.position - transform.position);
                if (away.magnitude > m.Radius) continue;
                float pm = p.TryGetComponent(out Evolve.Swallow.SlimeBody body) ? body.Mass : 10f;
                if (pm >= mass) continue;   // too big to be scared
                if (away.sqrMagnitude < 1e-4f) away = transform.forward;
                WindPush.Apply(p, away.normalized * m.Force, 0.3f);
                if (p.TryGetComponent(out Evolve.Player.SlimeController sc)) sc.FreezeFor(m.Freeze);
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
            UpdateSpears();
            if (model == null || scheduler == null) return;
            Pose pose = Pose.Identity;
            var m = scheduler.Current;
            if (leech != null)
            {
                if (leech.State != LeechState.Idle) pose = TellPose.Evaluate(PoseKind.Hover, MovePhase.Windup, 1f);   // a quivering mosquito while it hunts and feeds
            }
            else if (m != null) pose = TellPose.Evaluate(MovesetSeed.PoseOf(m), scheduler.Phase, scheduler.Progress01);
            if (MovesetSeed.AuthoredClips.Contains(SpeciesNames.Key(name))) pose = Pose.Identity;   // the real clips carry the body language
            else if (poseStrength < 1f) pose = Pose.Lerp(Pose.Identity, pose, poseStrength);   // a rigged species turns its own bones, so the whole-body pose is softer
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
