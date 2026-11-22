using Evolve.Abilities;
using Evolve.Survival;
using UnityEngine;

namespace Evolve.Combat
{
    /// <summary>
    /// The flying moves of Chim Hang Mẹ, run by <see cref="CreatureCombat"/> (which calls <see cref="Tick"/> every physics step with the move's phase): Chém Gió (two chops of the wings, a blade of wind from each wing tip each time),
    /// Bổ Nhào (flaps up over the player, a ring marks the spot, then it folds its wings and drops on it) and Quắp (dashes in low; if its talons catch the player it climbs with them, throws them into the sky, flies through them
    /// raking, then comes down: see <see cref="TalonFsm"/>). It moves the body by hand while a move of these shapes is on (the host's own walking is paused then), measures the ground under it, and lets it fall if a move is cut short.
    /// It also lifts the model off the ground while the bird flies in from far away, and says which <see cref="BirdPose"/> the rig should take.
    /// </summary>
    [DisallowMultipleComponent, DefaultExecutionOrder(65)]
    public class BirdMoves : MonoBehaviour
    {
        const float Gravity = 24f, GrabReach = 1.1f, CarryTopMeters = 6.5f, TossSpeed = 12f, RakeSpan = 3.4f, RakeReach = 3.4f;
        static readonly Color WindColor = new Color(0.58f, 0.8f, 1f), TellColor = new Color(1f, 0.35f, 0.3f), BloodColor = new Color(0.9f, 0.25f, 0.2f), DustColor = new Color(0.62f, 0.56f, 0.48f);
        static Material bladeMat;

        /// <summary>The pose the rig should take now, if <see cref="PoseActive"/> (otherwise the rig does its own idle and walking look).</summary>
        public BirdPose Pose { get; private set; }
        public bool PoseActive { get; private set; }
        /// <summary>Metres the body is above its walking height (a real height: the collider is up there too).</summary>
        public float Height { get; private set; }
        /// <summary>Metres the model alone is lifted while the bird flies in from far away (the body still walks on the ground).</summary>
        public float CruiseLift { get; private set; }
        /// <summary>The grab has caught the player and is not over: the move's recover must wait for it.</summary>
        public bool Holding => fsm != null && fsm.Caught && !fsm.Finished;

        CreatureCombat combat;
        Rigidbody rb;
        Collider col;
        Collider[] mine;
        bool ignoring;
        Transform tipL, tipR;
        float groundOffset, baseGround, fallSpeed, tickedAt = -1f;
        MoveDef prevMove; MovePhase prevPhase;
        Vector3 lastPos;
        bool cruising;

        // the move in progress
        Vector3 flat;                    // x and z of the body (y is 0)
        float h0, apex, diveH, dashH0;
        Vector3 hoverFlat, spot, diveFrom, dashFrom, dashTo;
        bool marked, hitDone;
        int slashes;

        // the grab
        TalonFsm fsm;
        TalonStage lastStage;
        SurvivalStats victim;
        Rigidbody victimBody;
        Evolve.Player.SlimeController victimCtl;
        float carryH0, leaveH0, rakeAngle;

        float Radius => col != null ? Mathf.Max(col.bounds.extents.x, col.bounds.extents.z) : 0.5f;
        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }
        static bool IsFlyingShape(MoveShape s) => s == MoveShape.Swoop || s == MoveShape.WindSlash || s == MoveShape.Talon;

        void Awake()
        {
            combat = GetComponent<CreatureCombat>();
            rb = GetComponent<Rigidbody>();
            col = GetComponentInChildren<Collider>();
            mine = GetComponentsInChildren<Collider>(true);
            lastPos = transform.position;
            Transform pinL = null, pinR = null;
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "WingTip_L") tipL = t;       // the hand end, if the rig has one (the Dreadwing model)
                else if (t.name == "WingTip_R") tipR = t;
                else if (t.name == "Pinion_L3") pinL = t;
                else if (t.name == "Pinion_R3") pinR = t;
            }
            if (tipL == null) tipL = pinL;
            if (tipR == null) tipR = pinR;
        }

        /// <summary>Can this move be started now? A grab needs a local player who is alive, not already in some other grip, and a body that physics can carry.</summary>
        public bool CanUse(MoveDef m, SurvivalStats target)
        {
            if (m.Shape != MoveShape.Talon) return true;
            return CanGrab(target);
        }

        static bool CanGrab(SurvivalStats t)
        {
            if (t == null || t.IsRemote || t.State.IsDead || HoundPin.Active != null || LatchSession.Active != null) return false;
            return t.TryGetComponent(out Rigidbody body) && !body.isKinematic;
        }

        /// <summary>The creature was stunned, killed or switched off: let go of the player and drop to the ground.</summary>
        public void Abort()
        {
            IgnorePlayers(false);
            ReleaseVictim();
            if (fsm != null) fsm.Abort();
            fsm = null;
            prevMove = null; prevPhase = MovePhase.Idle;
            PoseActive = false;
        }

        void ReleaseVictim() { victim = null; victimBody = null; victimCtl = null; }

        /// <summary>A sound of Resources/Sfx/Bird (recordings made by tools/voice/generate_bird_sfx.sh); the procedural <paramref name="fallback"/> plays if the file is missing.</summary>
        static void Sfx(string name, Vector3 at, float volume, ProceduralSfx.Kind fallback) => ProceduralSfx.PlayFile("Bird/" + name, at, volume, fallback);

        /// <summary>
        /// While the bird rushes (a strike, a dive, the whole grab) its big kinematic collider must not collide with the players: it is moved by hand, so touching a player counts as a ram at speed
        /// (<c>Predator.Contact</c>: the bird killed itself on its own dive), and the player tossed upward hit the bird's belly and hung there instead of flying. Off again when the strike is over.
        /// </summary>
        void IgnorePlayers(bool on)
        {
            if (ignoring == on || mine == null) return;
            ignoring = on;
            foreach (var p in SurvivalStats.All)
            {
                if (p == null) continue;
                foreach (var pc in p.GetComponentsInChildren<Collider>(true))
                    foreach (var bc in mine)
                        if (bc != null && pc != null) Physics.IgnoreCollision(bc, pc, on);
            }
        }

        /// <summary>The strike (and what follows it) in which the bird must pass through the player: a peck or any flying move while it strikes, the grab all the way, and the first moments after a dive or a missed dash.</summary>
        static bool Rushing(MoveDef m, MovePhase phase, float phaseTime, bool holding)
        {
            if (holding) return true;
            if (m == null || phase == MovePhase.Idle || phase == MovePhase.Windup) return false;
            if (phase == MovePhase.Active) return true;
            return IsFlyingShape(m.Shape) && m.Shape != MoveShape.WindSlash && phaseTime < 0.5f;   // it lands on the spot: let it settle before it is solid again
        }

        // ---------- called by CreatureCombat every physics step ----------

        public void Tick(MoveDef m, MovePhase phase, float progress, float phaseTime, SurvivalStats target)
        {
            float dt = Time.fixedDeltaTime;
            tickedAt = Time.fixedTime;
            if (!ReferenceEquals(m, prevMove) || phase != prevPhase) OnChange(m, phase, target);
            prevMove = m; prevPhase = phase;
            IgnorePlayers(Rushing(m, phase, phaseTime, Holding));

            if (m != null && phase != MovePhase.Idle && IsFlyingShape(m.Shape))
            {
                switch (m.Shape)
                {
                    case MoveShape.Swoop: TickSwoop(m, phase, progress, target, dt); break;
                    case MoveShape.WindSlash: TickSlash(m, phase, progress, phaseTime, target, dt); break;
                    case MoveShape.Talon: TickTalon(m, phase, progress, target, dt); break;
                }
            }
            else IdleTick(target, dt);
            UpdatePose(m, phase, progress);
        }

        /// <summary>A move cut short in the air (or a step in which the combat did not tick: stunned): the bird falls.</summary>
        void FixedUpdate()
        {
            if (Mathf.Approximately(tickedAt, Time.fixedTime)) return;
            FallTick(Time.fixedDeltaTime);
            if (Height <= 0.05f && CruiseLift > 0f) CruiseLift = Mathf.MoveTowards(CruiseLift, 0f, 4f * Time.fixedDeltaTime);
            if (!PoseActive || Height > 0.05f) { Pose = BirdPoseRules.Cruise(0.5f); PoseActive = Height > 0.05f || CruiseLift > 0.05f; }
        }

        // ---------- phases ----------

        void OnChange(MoveDef m, MovePhase phase, SurvivalStats target)
        {
            bool wasFlying = prevMove != null && prevPhase != MovePhase.Idle && IsFlyingShape(prevMove.Shape);
            bool nowFlying = m != null && phase != MovePhase.Idle && IsFlyingShape(m.Shape);
            if (wasFlying && (!nowFlying || !ReferenceEquals(m, prevMove))) { ReleaseVictim(); fsm = null; }   // the move is over
            if (m != null && m.Shape == MoveShape.Lunge && phase == MovePhase.Active) Sfx("bird_peck", transform.position, 1f, ProceduralSfx.Kind.HoundSnap);
            if (!nowFlying) return;
            if (phase == MovePhase.Windup) BeginMove(m, target);
            else if (phase == MovePhase.Active) BeginStrike(m, target);
            else if (phase == MovePhase.Recover)
            {
                if (m.Shape == MoveShape.Swoop && !hitDone) Impact(m);
                else if (m.Shape == MoveShape.Talon && fsm != null && fsm.Stage == TalonStage.Dash) fsm.Miss();
            }
        }

        void BeginMove(MoveDef m, SurvivalStats target)
        {
            Vector3 pos = rb.position;
            float realH = Height;
            Height = realH + CruiseLift; CruiseLift = 0f; cruising = false;   // the hover of the flight in becomes real height, so nothing jumps
            baseGround = Evolve.World.GroundHeight.At(pos.x, pos.z, pos.y - realH);
            groundOffset = Mathf.Max(0f, pos.y - baseGround - realH);
            h0 = Height; flat = Flat(pos); fallSpeed = 0f;
            marked = false; hitDone = false; slashes = 0;
            if (m.Shape == MoveShape.Swoop)
            {
                apex = ClearHeight(SwoopRules.ApexMeters);
                Vector3 to = target != null ? Flat(target.transform.position) - flat : Vector3.zero;
                float d = to.magnitude;
                hoverFlat = d > SwoopRules.HoverStandOff ? flat + to.normalized * (d - SwoopRules.HoverStandOff) : flat;   // it closes to a few metres of the player while it climbs
            }
            else if (m.Shape == MoveShape.Talon) { fsm = new TalonFsm(); lastStage = TalonStage.Dash; }
            if (m.Shape == MoveShape.Swoop) Sfx("bird_wind", transform.position, 0.8f, ProceduralSfx.Kind.SpearOrbit);   // the wind of its climb
        }

        void BeginStrike(MoveDef m, SurvivalStats target)
        {
            if (m.Shape == MoveShape.Swoop)
            {
                diveFrom = flat; diveH = Height;
                if (!marked) spot = SpotOf(target, 0.35f);
                Sfx("bird_dive", transform.position, 1f, ProceduralSfx.Kind.SpearLaunch);
            }
            else if (m.Shape == MoveShape.Talon)
            {
                dashFrom = flat; dashH0 = Height;
                Vector3 aim = SpotOf(target, 0.2f);
                Vector3 to = aim - flat;
                Vector3 dir = to.sqrMagnitude > 1e-4f ? to.normalized : Flat(transform.forward).normalized;
                dashTo = flat + dir * (to.magnitude + 1.5f);   // it flies on past the player: the catch happens on the way, never short of it
                Sfx("bird_swoop", transform.position, 1f, ProceduralSfx.Kind.SpearLaunch);
            }
            else if (m.Shape == MoveShape.WindSlash) slashes = 0;
        }

        // ---------- Bổ Nhào ----------

        void TickSwoop(MoveDef m, MovePhase phase, float p, SurvivalStats target, float dt)
        {
            if (phase == MovePhase.Windup)
            {
                Height = Mathf.Lerp(h0, apex, SwoopRules.Rise(p));
                flat = Advance(flat, Vector3.MoveTowards(flat, hoverFlat, 7f * dt));
                if (!marked && p >= SwoopRules.MarkAt)
                {
                    marked = true;
                    spot = SpotOf(target, 0.35f);
                    float left = m.Windup * combat.TellScale * (1f - p) + m.Active;
                    SkillFx.Ring(new Vector3(spot.x, target != null ? target.transform.position.y : baseGround, spot.z), m.Radius, TellColor, left, 0.3f);   // where it will land: step out
                }
            }
            else if (phase == MovePhase.Active)
            {
                flat = Advance(flat, Vector3.Lerp(diveFrom, spot, p));
                Height = diveH * SwoopRules.Fall(p);
                LookAlong(spot - flat);
                if (!hitDone && p >= SwoopRules.ImpactAt) Impact(m);
            }
            else Height = Mathf.MoveTowards(Height, 0f, 25f * dt);
            Place(flat, Height);
        }

        void Impact(MoveDef m)
        {
            hitDone = true; Height = 0f;
            Place(spot, 0f); flat = spot;
            HurtArea(spot, m.Radius, m.Damage, m.Force, m.Freeze);
            Vector3 at = new Vector3(spot.x, baseGround, spot.z);
            SkillFx.Ring(at, m.Radius, WindColor, 0.5f, 0.2f);
            SkillFx.Dust(at, m.Radius, DustColor, 28, 0.6f);
            ImpactFx.Thump(at, 0.07f, 0.4f, 25f);
            ProceduralSfx.Play(ProceduralSfx.Kind.HoundThud, at, 1f);
        }

        // ---------- Chém Gió ----------

        void TickSlash(MoveDef m, MovePhase phase, float p, float t, SurvivalStats target, float dt)
        {
            float hover = 0.9f + Radius * 0.6f;
            if (phase == MovePhase.Windup) Height = Mathf.Lerp(h0, hover, p * p * (3f - 2f * p));
            else if (phase == MovePhase.Active)
            {
                Height = hover;
                if (target != null) LookAlong(target.transform.position - transform.position);
                while (slashes < 2 && t + 1e-4f >= m.Active * (0.25f + 0.5f * slashes)) { slashes++; LaunchBlades(m, target); }   // at the bottom of each chop
            }
            else Height = Mathf.MoveTowards(Height, 0f, 5f * dt);
            Place(flat, Height);
        }

        void LaunchBlades(MoveDef m, SurvivalStats target)
        {
            Vector3 aim = target != null ? target.transform.position + Vector3.up * 0.3f : transform.position + transform.forward * 10f;
            Sfx("bird_wing_slap", transform.position, 1f, ProceduralSfx.Kind.HoundSwipe);                  // the chop of the wings
            Sfx("bird_feather_volley", transform.position, 0.8f, ProceduralSfx.Kind.SpearLaunch);          // and the blades leaving them
            for (int side = -1; side <= 1; side += 2)
            {
                Transform tip = side < 0 ? tipL : tipR;
                Vector3 from = tip != null ? tip.position : transform.position + transform.right * (side * Radius * 1.3f) + Vector3.up * Radius * 0.9f;
                Vector3 d = aim - from;
                if (d.sqrMagnitude < 1e-3f) d = transform.forward;
                d = Quaternion.AngleAxis(side * 7f, Vector3.up) * d;   // the pair fans out a little so one of them is sure to find the player
                var blade = MakeBlade();
                blade.position = from;
                MonsterSpear.Launch(blade, from + d.normalized * 10f, m.SpearSpeed, m.Damage * combat.DamageScale, m.Force);
            }
        }

        /// <summary>A flat lens of wind that lies across its flight (MonsterSpear points the capsule's long axis along the way it flies, so the thin axis ends up vertical).</summary>
        Transform MakeBlade()
        {
            if (bladeMat == null) bladeMat = SkillFx.Gel(WindColor, 1f, 0.9f, 3.4f);
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Destroy(go.GetComponent<Collider>());
            go.name = "Lưỡi Gió";
            float s = Mathf.Clamp(Radius, 0.5f, 1.6f);
            go.transform.localScale = new Vector3(1.5f, 0.45f, 0.07f) * s;
            go.GetComponent<MeshRenderer>().sharedMaterial = bladeMat;
            var trail = go.AddComponent<TrailRenderer>();
            trail.time = 0.25f; trail.startWidth = 0.5f * s; trail.endWidth = 0f; trail.sharedMaterial = bladeMat; trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go.transform;
        }

        // ---------- Quắp ----------

        void TickTalon(MoveDef m, MovePhase phase, float p, SurvivalStats target, float dt)
        {
            if (fsm == null) return;
            if (fsm.Stage == TalonStage.Dash)
            {
                if (phase == MovePhase.Windup) Height = Mathf.Lerp(h0, 1.6f + Radius * 0.4f, p * p * (3f - 2f * p));   // hovering low, aiming
                else if (phase == MovePhase.Active)
                {
                    flat = Advance(flat, Vector3.Lerp(dashFrom, dashTo, p));
                    Height = Mathf.Lerp(dashH0, 0.15f, Mathf.Clamp01(p * 1.6f));   // down to the player's level
                    LookAlong(dashTo - dashFrom);
                    if (target != null && Flat(target.transform.position - flat).magnitude <= Radius + GrabReach) Catch(m, target);
                }
            }
            if (fsm.Stage != TalonStage.Dash && fsm.Stage != TalonStage.Done && fsm.Caught) RunCaught(m, dt);
            else if (fsm.Finished || (fsm.Stage == TalonStage.Dash && phase == MovePhase.Recover)) Height = Mathf.MoveTowards(Height, 0f, 12f * dt);   // a miss: down it comes
            Place(flat, Height);
        }

        void Catch(MoveDef m, SurvivalStats target)
        {
            if (!CanGrab(target)) { HurtArea(Flat(target.transform.position), 1f, m.Damage, 5f, 0f); fsm.Miss(); return; }   // nothing to hold: it is only a blow
            victim = target; victimBody = target.GetComponent<Rigidbody>(); target.TryGetComponent(out victimCtl);
            fsm.Grab(); lastStage = TalonStage.Carry;
            carryH0 = Height;
            target.State.Damage(m.Damage * combat.DamageScale);
            Vector3 at = target.transform.position;
            SkillFx.Burst(at + Vector3.up * 0.3f, BloodColor, 12, 3f, 0.1f, 0.4f);
            Sfx("bird_grab", at, 1f, ProceduralSfx.Kind.HoundSnap);
            ImpactFx.Thump(at, 0.05f, 0.3f, 20f);
        }

        void RunCaught(MoveDef m, float dt)
        {
            if (victim == null || victim.State.IsDead || victimBody == null) { ReleaseVictim(); fsm.GiveUp(); }
            var e = fsm.Update(dt);
            if (fsm.Stage != lastStage) { OnStage(fsm.Stage); lastStage = fsm.Stage; }
            switch (fsm.Stage)
            {
                case TalonStage.Carry:
                    Height = Mathf.Lerp(carryH0, ClearHeight(CarryTopMeters), Smooth(fsm.StageTime / TalonFsm.CarrySeconds));
                    Hold();
                    break;
                case TalonStage.Toss:
                    break;
                case TalonStage.Rake:
                    FlyPass(dt);
                    break;
                case TalonStage.Leave:
                    Height = Mathf.Lerp(leaveH0, 0f, Smooth(fsm.StageTime / TalonFsm.LeaveSeconds));
                    break;
            }
            if (e == TalonEvent.Tossed) Toss();
            else if (e == TalonEvent.Raked) Rake(m);
        }

        void OnStage(TalonStage s)
        {
            if (s == TalonStage.Rake) rakeAngle = Random.Range(0f, 360f);
            else if (s == TalonStage.Leave) leaveH0 = Height;
        }

        static float Smooth(float t) { t = Mathf.Clamp01(t); return t * t * (3f - 2f * t); }

        /// <summary>The player hangs in its feet: placed under the body every step, held still.</summary>
        void Hold()
        {
            if (victimBody == null) return;
            Vector3 pivot = Pivot(flat, Height);
            float y = Mathf.Max(pivot.y - (Radius + 0.35f), baseGround + 0.15f);
            victimBody.position = new Vector3(pivot.x, y, pivot.z);
            victimBody.linearVelocity = Vector3.zero;
            if (victimCtl != null) victimCtl.FreezeFor(0.3f);
        }

        void Toss()
        {
            if (victimBody != null)
            {
                victimBody.linearVelocity = Vector3.zero;
                Impulse.Kick(victimBody, Vector3.up * TossSpeed + Flat(transform.forward).normalized * 2.5f);
            }
            if (victimCtl != null) victimCtl.FreezeFor(1f);   // no steering for a moment: it is flying
            Vector3 at = transform.position;
            Sfx("bird_stun_screech", at, 1f, ProceduralSfx.Kind.HoundHowl);   // the screech as it flings the player
            ImpactFx.Thump(at, 0.06f, 0.3f, 20f);
        }

        /// <summary>The raking: it swings back and forth through the player on a line that turns slowly (<see cref="TalonFsm.SwingOffset"/>), so it flies straight through them in the middle of every pass.</summary>
        void FlyPass(float dt)
        {
            if (victim == null) return;
            Vector3 vp = victim.transform.position;
            float secs = fsm.StageTime;
            Vector3 axis = Quaternion.AngleAxis(rakeAngle + secs * 25f, Vector3.up) * Vector3.forward;
            Vector3 want = Flat(vp) + axis * (TalonFsm.SwingOffset(secs) * RakeSpan);
            flat = Advance(flat, Vector3.Lerp(flat, want, 1f - Mathf.Exp(-22f * dt)));
            float wantY = vp.y + 0.9f + Radius * 0.5f;   // a little above them, so the talons come down on them
            float gy = Evolve.World.GroundHeight.At(flat.x, flat.z, baseGround) + groundOffset;
            Height = Mathf.Max(0f, Mathf.Lerp(Height, wantY - gy, 1f - Mathf.Exp(-14f * dt)));
            LookAlong(axis * (fsm.RakeIndex % 2 == 0 ? -1f : 1f));   // along the way it is flying: toward the far side on the even passes, back on the odd ones
        }

        void Rake(MoveDef m)
        {
            if (victim == null || victim.State.IsDead) return;
            Vector3 vp = victim.transform.position;
            if ((Pivot(flat, Height) - vp).sqrMagnitude > RakeReach * RakeReach) return;   // the player got away from this pass
            victim.State.Damage(m.MissileDamage * combat.DamageScale);
            if (victimBody != null && victimBody.linearVelocity.y < 1.5f) Impulse.Kick(victimBody, Vector3.up * 3.5f);   // every rake flings them up again
            SkillFx.Burst(vp + Vector3.up * 0.3f, BloodColor, 10, 3f, 0.1f, 0.4f);
            Sfx("bird_wing_slap", vp, 0.6f, ProceduralSfx.Kind.HoundSwipe);
        }

        // ---------- the body ----------

        /// <summary>Where the body's pivot is at flat position <paramref name="f"/> and height <paramref name="h"/>.</summary>
        Vector3 Pivot(Vector3 f, float h) => new Vector3(f.x, Evolve.World.GroundHeight.At(f.x, f.z, baseGround) + groundOffset + h, f.z);

        void Place(Vector3 f, float h) => rb.MovePosition(Pivot(f, h));

        /// <summary>The step toward <paramref name="to"/>, unless a wall stands in the way (other creatures and the player never block).</summary>
        Vector3 Advance(Vector3 from, Vector3 to)
        {
            Vector3 d = to - from;
            float len = d.magnitude;
            if (len < 1e-4f) return to;
            Vector3 c = Pivot(from, Height);
            float r = Mathf.Clamp(Radius * 0.6f, 0.25f, 0.9f);
            if (Physics.SphereCast(c, r, d / len, out var hit, len + 0.05f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                && hit.distance > 0f && hit.collider.attachedRigidbody == null && hit.normal.y < 0.6f) return from;
            return to;
        }

        /// <summary>How high it may climb: <paramref name="want"/> metres, less under a low ceiling.</summary>
        float ClearHeight(float want)
        {
            Vector3 o = rb.position + Vector3.up * 0.5f;
            if (Physics.Raycast(o, Vector3.up, out var hit, want + 1f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) && hit.collider.attachedRigidbody == null)
                return Mathf.Clamp(hit.distance - (Radius + 0.4f), 1.8f, want);
            return want;
        }

        void LookAlong(Vector3 dir)
        {
            dir.y = 0f;
            if (dir.sqrMagnitude > 1e-4f) rb.MoveRotation(Quaternion.Slerp(rb.rotation, Quaternion.LookRotation(dir.normalized), 0.3f));
        }

        Vector3 SpotOf(SurvivalStats t, float lead)
        {
            if (t == null) return flat + Flat(transform.forward).normalized * 6f;
            Vector3 v = t.TryGetComponent(out Rigidbody body) ? Flat(body.linearVelocity) : Vector3.zero;
            return Flat(t.transform.position) + v * lead;
        }

        /// <summary>Damages and throws back every local player within <paramref name="radius"/> of <paramref name="at"/> (flat).</summary>
        void HurtArea(Vector3 at, float radius, float damage, float force, float freeze)
        {
            foreach (var p in SurvivalStats.All)
            {
                if (p == null || p.IsRemote || p.State.IsDead) continue;
                Vector3 away = Flat(p.transform.position - at);
                if (away.magnitude > radius) continue;
                p.State.Damage(damage * combat.DamageScale);
                if (away.sqrMagnitude < 1e-4f) away = transform.forward;
                Impulse.Kick(p.GetComponent<Rigidbody>(), away.normalized * force + Vector3.up * (force * 0.5f));
                if (freeze > 0f && p.TryGetComponent(out Evolve.Player.SlimeController sc)) sc.FreezeFor(freeze);
            }
        }

        // ---------- no flying move: falling and flying in from afar ----------

        void IdleTick(SurvivalStats target, float dt)
        {
            Vector3 pos = rb.position;
            float speed = Flat(pos - lastPos).magnitude / dt; lastPos = pos;
            if (Height > 0.001f) { FallTick(dt); return; }
            float dist = target != null ? Flat(target.transform.position - pos).magnitude : 0f;
            bool far = target != null && dist > (cruising ? 8f : 11f);
            cruising = far && (speed > 0.4f || CruiseLift > 0.05f);
            float lift = Mathf.Clamp(Radius * 2f, 1.3f, 2.8f);
            CruiseLift = Mathf.MoveTowards(CruiseLift, cruising ? lift : 0f, 2.4f * dt);
        }

        void FallTick(float dt)
        {
            if (Height <= 0.001f) { fallSpeed = 0f; return; }
            fallSpeed += Gravity * dt;
            Height = Mathf.Max(0f, Height - fallSpeed * dt);
            Vector3 pos = rb.position;
            Place(Flat(pos), Height);
            if (Height <= 0f)
            {
                if (fallSpeed > 6f) ImpactFx.Thump(pos, 0.02f, 0.2f, 15f);
                fallSpeed = 0f;
            }
        }

        void UpdatePose(MoveDef m, MovePhase phase, float p)
        {
            if (m != null && phase != MovePhase.Idle)
            {
                Pose = m.Shape == MoveShape.Talon && fsm != null && fsm.Caught && !fsm.Finished
                    ? BirdPoseRules.Talon(fsm.Stage, fsm.StageTime)
                    : BirdPoseRules.Evaluate(m.Anim, phase, p);
                PoseActive = true;
            }
            else if (Height > 0.05f || CruiseLift > 0.05f)
            {
                Pose = BirdPoseRules.Cruise(CruiseLift > 0.05f ? 0.7f : 0.3f);
                PoseActive = true;
            }
            else PoseActive = false;
        }
    }
}
