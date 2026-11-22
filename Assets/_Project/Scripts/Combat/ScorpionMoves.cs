using Evolve.Abilities;
using Evolve.Creatures;
using Evolve.Survival;
using UnityEngine;

namespace Evolve.Combat
{
    /// <summary>
    /// Bọ Cạp Thạch Anh in play (the numbers and clock are in <see cref="ScorpionRules"/>, <see cref="BurrowRules"/>, <see cref="ShatterRules"/>; the moves in MovesetSeed). It is animated by an Animator built by
    /// the menu Evolve → Build Bọ Cạp Thạch Anh (Meshy) Prefab, from the clips baked into its FBX by tools/blender/rig_scorpion.py:
    ///  - walking: the blend tree "Walk" on MoveX / MoveY (the four directions) and the animator speed follows the ground speed;
    ///  - every move has three clips, Tell_&lt;Anim&gt; (charge-up), Strike_&lt;Anim&gt; (cast) and Recover_&lt;Anim&gt; (after-cast): <see cref="OnPhase"/> fires the trigger and stretches the clip over the phase's seconds,
    ///    and adds that phase's light, particles and sound (a glow gathers at the stinger while it charges, flashes when it casts, and dies down after);
    ///  - Đào Chui Trồi Lên (<see cref="MoveShape.Burrow"/>) and Bùng Pha Lê (<see cref="MoveShape.Shatter"/>) are run here, by <see cref="Tick"/>, which CreatureCombat calls every physics step.
    /// Everything here is untested in Unity (written and compiled without it): see the checklist H13.
    /// </summary>
    [DisallowMultipleComponent, DefaultExecutionOrder(65)]
    public class ScorpionMoves : MonoBehaviour
    {
        static readonly Color Crystal = new Color(0.66f, 0.38f, 0.98f), Glow = new Color(0.82f, 0.58f, 1f), Dust = new Color(0.5f, 0.42f, 0.34f), Tell = new Color(1f, 0.3f, 0.3f);
        const string Sfx = "Scorpion/";

        CreatureCombat combat;
        Predator pred;
        Rigidbody rb;
        Animator anim;
        Renderer[] renderers;
        Collider[] mine;
        bool ignoring;
        Transform tailTip;
        float desiredSpeed = 1f;   // the animator speed this frame; applied in LateUpdate because CreatureLocomotion (Predator adds one to every creature) resets it to 1 in its Update
        Light glow;
        float glowTarget, lastHealth = -1f;
        bool dead;
        Vector3 lastPos;
        float moveX, moveY;
        static Material spikeMat;

        // the burrow
        bool underground, marked, burrowHit;
        Vector3 burrowFrom, burrowSpot;
        Transform mound;
        float nextMoundPuff;

        // the burst
        float burstT, lastBurstT;
        bool[] ringHit = new bool[ShatterRules.RingCount];
        float[] ringShown = new float[ShatterRules.RingCount];
        float aimBearing;
        bool[] flickStarted = new bool[ShatterRules.FlickCount], flickThrown = new bool[ShatterRules.FlickCount];
        CrystalColumn[] flickColumn = new CrystalColumn[ShatterRules.FlickCount];

        // the second dive of Đào Chui (health at two thirds or less): it goes down again right after coming up, and comes up where the player ran to
        bool secondActive, secondMarked, secondHit;
        float secondT;
        Vector3 secondFrom, secondSpot, lastMound;

        // the volley's crystal columns (planted where each spike lands), the claws stuck in a wall, and the lie-open after a big move
        struct Pending { public float at; public Vector3 pos; }
        readonly System.Collections.Generic.List<Pending> pendingColumns = new System.Collections.Generic.List<Pending>();
        int spearsPlanted;
        float stuckUntil;
        bool wasResting;

        /// <summary>The second dive is on: CreatureCombat must not end the move's recover yet.</summary>
        public bool Holding => secondActive;

        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }
        static float Bearing(Vector3 from, Vector3 to) { Vector3 d = Flat(to - from); return d.sqrMagnitude < 1e-6f ? 0f : Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg; }

        public static bool Has(string species) => Evolve.Creatures.SpeciesNames.Key(species ?? "") == "bocapthachanh";

        void Awake()
        {
            combat = GetComponent<CreatureCombat>();
            pred = GetComponent<Predator>();
            rb = GetComponent<Rigidbody>();
            anim = GetComponentInChildren<Animator>(true);
            renderers = GetComponentsInChildren<Renderer>(true);
            mine = GetComponentsInChildren<Collider>(true);
            foreach (var t in GetComponentsInChildren<Transform>(true)) if (t.name == "StingTip") tailTip = t;
            lastPos = transform.position;
            Evolve.World.ScorpionRoom.Install(transform);   // in a boss room its boulders stop the mound; elsewhere it digs anywhere
            if (tailTip != null)
            {
                var g = new GameObject("TailGlow");
                g.transform.SetParent(tailTip, false);
                glow = g.AddComponent<Light>();
                glow.type = LightType.Point; glow.color = Glow; glow.range = 4.5f; glow.intensity = 0f; glow.shadows = LightShadows.None;
            }
        }

        // ---------- walking, the glow, hurt and death ----------
        void Update()
        {
            float dt = Mathf.Max(0.001f, Time.deltaTime);
            Vector3 v = Flat(transform.position - lastPos) / dt; lastPos = transform.position;
            float speed = v.magnitude;
            if (anim != null && anim.runtimeAnimatorController != null)
            {
                ScorpionRules.LocalMove(transform.eulerAngles.y, v.x, v.z, out float x, out float y);
                float mag = Mathf.Sqrt(x * x + y * y); if (mag > 1f) { x /= mag; y /= mag; }   // the blend tree's walk clips sit at length 1: faster only speeds the clip up (WalkAnimSpeed)
                float k = 1f - Mathf.Exp(-10f * dt);
                moveX = Mathf.Lerp(moveX, x, k); moveY = Mathf.Lerp(moveY, y, k);
                anim.SetFloat("MoveX", moveX); anim.SetFloat("MoveY", moveY);
                if (combat == null || combat.Phase == MovePhase.Idle) desiredSpeed = speed > 0.1f ? ScorpionRules.WalkAnimSpeed(speed) : 1f;
                if (pred != null)
                {
                    if (lastHealth > 0f && pred.Health < lastHealth - 0.01f && pred.Health > 0f) { Trigger("Hit"); ProceduralSfx.PlayFile(Sfx + "scorpion_hurt", transform.position, 1f, ProceduralSfx.Kind.HoundHurt); BossMusic.Ping("scorpion_battle"); }
                    if (pred.Health <= 0f && !dead) { dead = true; Trigger("Die"); desiredSpeed = 1f; Abort(); BossMusic.Silence(1.5f); ProceduralSfx.PlayFile(Sfx + "scorpion_death", transform.position, 1f, ProceduralSfx.Kind.HoundThud); }
                    lastHealth = pred.Health;
                }
            }
            if (combat != null && !dead)
            {
                bool rest = combat.Resting;   // after a big move it lies open (no panting): the player's time to hit back
                if (rest && !wasResting) { Trigger("Exposed"); desiredSpeed = 1f; Say("exposed_hiss", transform.position, 0.7f); SkillFx.Dust(transform.position, 1.4f, Dust, 12); }
                wasResting = rest;
                if (stuckUntil > 0f && Time.time >= stuckUntil)   // the claws come free: on with the after-cast of the charge
                {
                    stuckUntil = 0f;
                    var cur = combat.CurrentMove;
                    if (cur != null && combat.Phase == MovePhase.Recover) { desiredSpeed = ScorpionRules.PhaseAnimSpeed(ClipSeconds(MovePhase.Recover, cur.Anim), cur.Recover); Trigger("Recover_" + cur.Anim); }
                }
            }
            if (combat != null && combat.Phase != MovePhase.Idle) BossMusic.Ping("scorpion_battle");   // a move in progress is a blow in the fight
            if (glow != null) glow.intensity = Mathf.MoveTowards(glow.intensity, glowTarget, 14f * dt);
        }

        void LateUpdate() { if (anim != null) anim.speed = dead ? 1f : desiredSpeed; }

        bool InLocomotion() => anim == null || anim.runtimeAnimatorController == null || (anim.GetCurrentAnimatorStateInfo(0).IsName("Walk") && !anim.IsInTransition(0));

        void Trigger(string name)
        {
            if (anim == null || anim.runtimeAnimatorController == null) return;
            foreach (var p in anim.parameters) if (p.type == AnimatorControllerParameterType.Trigger && p.name == name) { anim.SetTrigger(name); return; }
        }

        /// <summary>Every move clip (Tell, Strike, Recover, Flick) has 30 keys: 29/30 s at animator speed 1; the game stretches it over the phase's seconds.</summary>
        static float ClipSeconds(MovePhase phase, string moveAnim) => ScorpionRules.ClipSeconds;

        // ---------- the three phases of every move ----------
        /// <summary>A move's phase began (m is null when it is over). Fires the phase's clip, stretched over the phase, with its effects and sound.</summary>
        public void OnPhase(MovePhase phase, MoveDef m)
        {
            if (dead) return;
            if (m == null || phase == MovePhase.Idle)
            {
                glowTarget = 0f;
                desiredSpeed = 1f;
                if (anim != null && !InLocomotion()) Trigger("Reset");   // a move cut short goes back to walking
                return;
            }
            bool stuck = phase == MovePhase.Recover && Time.time < stuckUntil;   // the charge jammed its claws in a wall: the stuck clip plays until they come free
            bool second = phase == MovePhase.Recover && m.Anim == "Burrow" && !stuck && pred != null && BurrowRules.WantsDouble(pred.HealthFraction);   // up once, straight down again
            if (anim != null && !stuck)
            {
                foreach (var stale in new[] { "Reset", "Hit" }) anim.ResetTrigger(stale);   // a trigger nobody could take must not fire later, in the middle of the next move
                float seconds = phase == MovePhase.Windup ? m.Windup * combat.TellScale : phase == MovePhase.Active ? m.Active : m.Recover;
                desiredSpeed = ScorpionRules.PhaseAnimSpeed(ClipSeconds(phase, m.Anim), seconds);
            }
            if (stuck) { desiredSpeed = 1f; return; }
            if (phase == MovePhase.Windup && m.Shape == MoveShape.Spears) spearsPlanted = 0;
            if (second) { BeginSecond(m); return; }
            Trigger((phase == MovePhase.Windup ? "Tell_" : phase == MovePhase.Active ? "Strike_" : "Recover_") + m.Anim);
            Vector3 p = transform.position;
            Vector3 tip = tailTip != null ? tailTip.position : p + Vector3.up * 1.2f;
            switch (m.Anim)
            {
                case "Sting":
                    if (phase == MovePhase.Windup) { Say("sting_charge", p, 0.9f); glowTarget = 2.5f; SkillFx.Orbit(transform, Glow, 8, m.Windup); }
                    else if (phase == MovePhase.Active) { Say("sting_hit", p, 1f); glowTarget = 6f; SkillFx.Burst(p + transform.forward * 2f + Vector3.up * 0.4f, Crystal, 18, 4f); ImpactFx.Thump(p, 0.12f, 0.35f, 22f); }
                    else { glowTarget = 0f; SkillFx.Dust(p + transform.forward * 2f, 1f, Dust, 14); }
                    break;
                case "TailSlam":
                    if (phase == MovePhase.Windup) { Say("tailslam_raise", p, 0.9f); glowTarget = 2f; }
                    else if (phase == MovePhase.Active)
                    {
                        Say("tailslam_hit", p, 1f); glowTarget = 7f; Vector3 at = p + transform.forward * m.Radius * 0.55f;
                        SkillFx.Ring(at, m.Radius, Dust, 0.45f, 0.15f); SkillFx.Dust(at, m.Radius * 0.7f, Dust, 36); SkillFx.Burst(at + Vector3.up * 0.2f, Crystal, 16, 5f, 0.12f, 0.8f, 0.6f); ImpactFx.Thump(at, 0.3f, 0.7f, 26f);
                    }
                    else { glowTarget = 0f; SkillFx.Dust(p, 1.6f, Dust, 16); }
                    break;
                case "Volley":
                    if (phase == MovePhase.Windup) { Say("volley_charge", p, 1f); glowTarget = 4f; SkillFx.Orbit(transform, Glow, 12, m.Windup); }
                    else if (phase == MovePhase.Active) { Say("volley_fire", p, 1f); glowTarget = 7f; SkillFx.Burst(tip, Crystal, 20, 5f, 0.1f, 0.5f, 0.2f); }
                    else glowTarget = 0f;
                    break;
                case "Burrow":
                    if (phase == MovePhase.Windup) { Say("burrow_dig", p, 1f); SkillFx.Dust(p, 1.8f, Dust, 40); glowTarget = 0f; }
                    else if (phase == MovePhase.Active) BeginUnderground(m);
                    else { Say("burrow_shake", p, 0.8f); SkillFx.Dust(p, 2.4f, Dust, 30); }
                    break;
                case "Pinch":
                    if (phase == MovePhase.Windup) { Say("pinch_charge", p, 1f); SkillFx.Dust(p, 1.5f, Dust, 20); }
                    else if (phase == MovePhase.Active) { Say("pinch_run", p, 1f); SkillFx.Dust(p, 2f, Dust, 30); }
                    else { Say("pinch_snap", p, 1f); ImpactFx.Thump(p, 0.15f, 0.4f, 20f); SkillFx.Burst(p + transform.forward * 1.6f + Vector3.up * 0.5f, Crystal, 14, 4f); }
                    break;
                case "Burst":
                    if (phase == MovePhase.Windup) { Say("burst_charge", p, 1f); glowTarget = 9f; SkillFx.Orbit(transform, Glow, 24, m.Windup * combat.TellScale); ImpactFx.Thump(p, 0.04f, m.Windup, 18f); }
                    else if (phase == MovePhase.Active) BeginBurst(m);
                    else { Say("burst_gasp", p, 1f); glowTarget = 0f; SkillFx.ShieldBreak(transform, Crystal); SkillFx.Dust(p, 3f, Dust, 40); }
                    break;
            }
        }

        static void Say(string file, Vector3 p, float vol) => ProceduralSfx.PlayFile(Sfx + file, p, vol, ProceduralSfx.Kind.HoundThud);

        /// <summary>Called every physics step by CreatureCombat with the move in progress.</summary>
        public void Tick(MoveDef m, MovePhase phase, float phaseTime, SurvivalStats target)
        {
            IgnorePlayers(Rushing(m, phase, phaseTime));
            FlushColumns();
            if (m == null) return;
            if (secondActive) { TickSecond(m, target); return; }
            if (m.Shape == MoveShape.Spears && phase == MovePhase.Active) PlantColumns(m, phaseTime, target);
            if (m.Shape == MoveShape.Burrow && phase == MovePhase.Active) TickBurrow(m, phaseTime, target);
            else if (m.Shape == MoveShape.Shatter && phase == MovePhase.Active) TickBurst(m, phaseTime, target);
            else if (underground && phase != MovePhase.Active) Surface();   // cut short (stunned): it comes up where it is
        }

        /// <summary>The move was cut short (stunned, killed): whatever it was doing underground or in the burst ends.</summary>
        public void Abort()
        {
            IgnorePlayers(false);
            secondActive = false; stuckUntil = 0f; pendingColumns.Clear();
            for (int i = 0; i < flickColumn.Length; i++) if (flickColumn[i] != null) { flickColumn[i].Shatter(); flickColumn[i] = null; }
            if (underground) Surface();
            if (mound != null) { Destroy(mound.gameObject); mound = null; }
            glowTarget = 0f;
        }

        /// <summary>
        /// While a move is being cast (a charge, a stab, the burst out of the ground) the scorpion's collider is moved by hand and must not collide with the players: touching one at speed counts
        /// as a ram in <c>Predator.Contact</c> and the scorpion hurt itself on its own charge (the same bug the vulture had). Hits are still dealt by the moves themselves, by distance.
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

        /// <summary>The cast, underground, and the first moments of the after-cast of a move that rushes or bursts, while the bodies may overlap.</summary>
        bool Rushing(MoveDef m, MovePhase phase, float phaseTime)
        {
            if (underground || secondActive) return true;
            if (m == null || phase == MovePhase.Idle || phase == MovePhase.Windup) return false;
            if (phase == MovePhase.Active) return true;
            return phaseTime < 0.6f;   // the player may still be inside it, or thrown up against it: let them part before it is solid again
        }

        // ---------- Đào Chui Trồi Lên ----------
        void BeginUnderground(MoveDef m)
        {
            underground = true; marked = false; burrowHit = false; burrowFrom = rb != null ? rb.position : transform.position; burrowSpot = burrowFrom; lastMound = burrowFrom;
            SetHidden(true);
            Say("burrow_travel", transform.position, 1f);
            if (mound == null) BeginMoundVisual();
            mound.position = burrowFrom;
        }

        void TickBurrow(MoveDef m, float activeTime, SurvivalStats target)
        {
            float t = BurrowRules.Dig + activeTime;   // the clock of the whole move: the windup was the dig
            Vector3 aim = target != null ? Flat(target.transform.position) : Flat(burrowFrom);
            if (BurrowRules.CircleFollows(t)) burrowSpot = aim;
            else if (!marked)
            {
                marked = true; burrowSpot = aim;
                SkillFx.Ring(new Vector3(burrowSpot.x, target != null ? target.transform.position.y : transform.position.y, burrowSpot.z), m.Radius, Tell, BurrowRules.Total - t, 0.3f);   // where it comes up: step out
                Say("burrow_mark", burrowSpot, 0.9f);
            }
            Vector3 from = Flat(burrowFrom), to = burrowSpot - from;
            Vector3 pos = from + (to.sqrMagnitude < 1e-6f ? Vector3.zero : to.normalized * BurrowRules.MoundDistance(t, to.magnitude));
            pos.y = burrowFrom.y;
            if (!ScorpionArena.CanBurrowAt(pos.x, pos.z)) pos = lastMound; else lastMound = pos;   // a boss room can say where the ground is too hard to dig
            if (underground && rb != null) rb.MovePosition(pos);
            if (mound != null) mound.position = pos;
            if (underground && Time.time >= nextMoundPuff) { nextMoundPuff = Time.time + 0.18f; SkillFx.Dust(pos, 1.1f, Dust, 8); }
            if (!burrowHit && t >= BurrowRules.EmergeTime) Emerge(m);
        }

        void Emerge(MoveDef m)
        {
            burrowHit = true;
            Vector3 at = new Vector3(burrowSpot.x, burrowFrom.y, burrowSpot.z);
            if (rb != null) rb.MovePosition(at);
            Surface();
            bool any = BurstOut(at, m.Radius, m.Damage, m.Force, m.Freeze, 1f);
            if (!any && BurrowRules.ExposedFactor(true) > 1f) combat.Stagger(1.0f);   // a miss: it lies open for a moment (counter window)
        }

        /// <summary>The scorpion bursts out of the ground at <paramref name="at"/>: damage and a toss for anyone in the circle, earth and shards flying. True if it caught someone.</summary>
        bool BurstOut(Vector3 at, float radius, float damage, float force, float freeze, float scale)
        {
            bool any = HurtArea(at, radius, damage, force, freeze, true);
            Say("burrow_burst", at, 1f);
            SkillFx.Ring(at, radius, Dust, 0.5f, 0.2f); SkillFx.Dust(at, radius, Dust, Mathf.RoundToInt(60 * scale), 0.5f); SkillFx.Burst(at + Vector3.up * 0.3f, Crystal, Mathf.RoundToInt(24 * scale), 6f, 0.14f, 0.9f, 0.7f);
            ImpactFx.Thump(at, 0.4f * scale, 0.9f, 28f);
            return any;
        }

        // ---------- the second dive: only at two thirds of its health or less ----------
        void BeginSecond(MoveDef m)
        {
            secondActive = true; secondT = 0f; secondMarked = false; secondHit = false;
            secondFrom = rb != null ? rb.position : transform.position; secondSpot = secondFrom; lastMound = secondFrom;
            desiredSpeed = ScorpionRules.PhaseAnimSpeed(ClipSeconds(MovePhase.Windup, "Burrow"), BurrowRules.SecondDive);
            Trigger("Tell_Burrow");   // it sinks into the ground again
            Say("burrow_redive", transform.position, 1f); SkillFx.Dust(transform.position, 1.8f, Dust, 30);
        }

        void TickSecond(MoveDef m, SurvivalStats target)
        {
            secondT += Time.fixedDeltaTime;
            var stage = BurrowRules.SecondStageAt(secondT);
            if (stage != BurrowStage.Dig && !underground)   // down: gone from sight, the mound runs
            {
                underground = true; SetHidden(true);
                if (mound == null) { BeginMoundVisual(); }
                desiredSpeed = ScorpionRules.PhaseAnimSpeed(ClipSeconds(MovePhase.Active, "Burrow"), BurrowRules.SecondTravel + BurrowRules.SecondMarked + BurrowRules.SecondEmerge);
                Trigger("Strike_Burrow");
                Say("burrow_travel", transform.position, 0.9f);
            }
            Vector3 aim = target != null ? Flat(target.transform.position) : Flat(secondFrom);
            if (BurrowRules.CircleFollowsSecond(secondT)) secondSpot = aim;
            else if (!secondMarked)
            {
                secondMarked = true; secondSpot = aim;
                SkillFx.Ring(new Vector3(secondSpot.x, target != null ? target.transform.position.y : transform.position.y, secondSpot.z), BurrowRules.SecondRadius, Tell, BurrowRules.SecondTotal - secondT, 0.3f);
                Say("burrow_mark", secondSpot, 0.9f);
            }
            if (stage != BurrowStage.Dig)
            {
                Vector3 from = Flat(secondFrom), to = secondSpot - from;
                float run = BurrowRules.MoundDistance(secondT - BurrowRules.SecondDive + BurrowRules.Dig, to.magnitude);   // the same mound speed as the first dive
                Vector3 pos = from + (to.sqrMagnitude < 1e-6f ? Vector3.zero : to.normalized * run); pos.y = secondFrom.y;
                if (!ScorpionArena.CanBurrowAt(pos.x, pos.z)) pos = lastMound; else lastMound = pos;
                if (rb != null && underground) rb.MovePosition(pos);
                if (mound != null) mound.position = pos;
                if (Time.time >= nextMoundPuff) { nextMoundPuff = Time.time + 0.18f; SkillFx.Dust(pos, 1.1f, Dust, 8); }
            }
            if (!secondHit && secondT >= BurrowRules.SecondEmergeTime)
            {
                secondHit = true;
                Vector3 at = new Vector3(secondSpot.x, secondFrom.y, secondSpot.z);
                if (rb != null) rb.MovePosition(at);
                Surface();
                bool any = BurstOut(at, BurrowRules.SecondRadius, m.Damage * 0.6f, m.Force, m.Freeze, 0.7f);
                if (!any) combat.Stagger(0.8f);
            }
            if (stage == BurrowStage.Done)   // up for good: the after-cast clip, then the lie-open
            {
                secondActive = false;
                desiredSpeed = ScorpionRules.PhaseAnimSpeed(ClipSeconds(MovePhase.Recover, "Burrow"), m.Recover);
                Trigger("Recover_Burrow");
                Say("burrow_shake", transform.position, 0.8f);
            }
        }

        void BeginMoundVisual()
        {
            var g = new GameObject("Gò Đất"); mound = g.transform;
            var c = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(c.GetComponent<Collider>());
            c.transform.SetParent(mound, false); c.transform.localScale = new Vector3(1.6f, 0.7f, 1.6f); c.transform.localPosition = new Vector3(0f, 0.1f, 0f);
            c.GetComponent<MeshRenderer>().sharedMaterial = SkillFx.Gel(Dust, 1f, 0.2f, 0.2f);
            mound.position = transform.position;
        }

        void Surface()
        {
            underground = false;
            SetHidden(false);
            if (mound != null) { Destroy(mound.gameObject); mound = null; }
        }

        void SetHidden(bool hidden)
        {
            if (renderers == null) return;
            foreach (var r in renderers) if (r != null && r.gameObject != (glow != null ? glow.gameObject : null)) r.enabled = !hidden;
        }

        // ---------- Bùng Pha Lê ----------
        void BeginBurst(MoveDef m)
        {
            burstT = lastBurstT = 0f; aimBearing = 0f;
            for (int i = 0; i < ShatterRules.FlickCount; i++) { flickStarted[i] = flickThrown[i] = false; flickColumn[i] = null; }
            for (int i = 0; i < ringHit.Length; i++) { ringHit[i] = false; ringShown[i] = 0f; }
            var target = combat.EngagedTarget(40f);
            aimBearing = target != null ? Bearing(transform.position, target.transform.position) : transform.eulerAngles.y;
            Say("burst_ring", transform.position, 1f);
            ImpactFx.Thump(transform.position, 0.5f, 1.2f, 30f);
            SkillFx.Ring(transform.position, 3f, Crystal, 0.5f, 0.1f);
        }

        void TickBurst(MoveDef m, float t, SurvivalStats target)
        {
            burstT = t;
            Vector3 c = transform.position;
            for (int ring = 0; ring < ShatterRules.RingCount; ring++)
            {
                float r = ShatterRules.RingRadius(ring, t);
                // a crown of spikes rises each 1.5 m the wave passes, except in the gap
                while (r > 0f && ringShown[ring] + 1.5f <= r + 1e-3f)
                {
                    ringShown[ring] += 1.5f;
                    SpikesAt(c, ringShown[ring], ring);
                }
                if (target != null && !target.IsRemote && !target.State.IsDead)
                {
                    Vector3 d = Flat(target.transform.position - c);
                    if (ShatterRules.RingHits(ring, t, d.magnitude, Bearing(c, target.transform.position), aimBearing, ringHit[ring]))
                    {
                        ringHit[ring] = true;
                        HurtOne(target, d.sqrMagnitude < 1e-4f ? transform.forward : d.normalized, ShatterRules.RingDamage, m.Force, 0.3f);
                    }
                }
            }
            // after the rings the tail stabs three crystals that rise in front of it and flicks each at the player (the Flick clip: dip, stab, whip)
            FaceTarget(target, 220f);
            for (int i = 0; i < ShatterRules.FlickCount; i++)
            {
                if (!flickStarted[i] && t >= ShatterRules.FlickTime(i))
                {
                    flickStarted[i] = true;
                    float a = (transform.eulerAngles.y + ShatterRules.FlickAngle(i)) * Mathf.Deg2Rad;
                    Vector3 at = c + new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * 2.6f;
                    flickColumn[i] = CrystalColumn.Spawn(at, 0.6f, 1.5f, 3f);
                    desiredSpeed = ScorpionRules.PhaseAnimSpeed(ScorpionRules.ClipSeconds, ShatterRules.FlickInterval); if (anim != null) anim.ResetTrigger("Flick"); Trigger("Flick");
                    Say("flick_stab", at, 1f); glowTarget = 8f;
                }
                if (flickStarted[i] && !flickThrown[i] && t >= ShatterRules.FlickTime(i) + ShatterRules.FlickThrow)
                {
                    flickThrown[i] = true;
                    var col = flickColumn[i]; flickColumn[i] = null;
                    Vector3 from = col != null ? col.Top : (tailTip != null ? tailTip.position : c + Vector3.up * 2f);
                    if (col != null) col.Shatter();
                    Vector3 to = (target != null ? target.transform.position : c + transform.forward * 8f) + Vector3.up * 0.3f;
                    MonsterProjectile.Launch(from, to, 0.7f, m.MissileDamage * combat.DamageScale, 0.8f, false, 0.45f);
                    Say("flick_throw", from, 1f); SkillFx.Burst(from, Crystal, 14, 5f, 0.1f, 0.5f, 0.2f);
                }
            }
            lastBurstT = t;
        }

        void FaceTarget(SurvivalStats target, float degreesPerSecond)
        {
            if (target == null || rb == null) return;
            Vector3 d = Flat(target.transform.position - transform.position);
            if (d.sqrMagnitude < 1e-4f) return;
            rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, Quaternion.LookRotation(d), degreesPerSecond * Time.fixedDeltaTime));
        }

        void SpikesAt(Vector3 centre, float radius, int ring)
        {
            if (spikeMat == null) spikeMat = SkillFx.Gel(Crystal, 1f, 0.9f, 2.6f);
            int slots = Mathf.Max(8, Mathf.RoundToInt(radius * 3.2f));
            for (int i = 0; i < slots; i++)
            {
                float bearing = 360f * i / slots;
                if (ShatterRules.InGap(ring, bearing, aimBearing)) continue;
                float a = bearing * Mathf.Deg2Rad;
                Vector3 pos = centre + new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * radius;
                var s = GameObject.CreatePrimitive(PrimitiveType.Capsule); Destroy(s.GetComponent<Collider>());
                s.name = "Gai Pha Lê"; s.transform.position = pos + Vector3.up * 0.05f; s.transform.rotation = Quaternion.Euler(Random.Range(-12f, 12f), Random.Range(0f, 360f), Random.Range(-12f, 12f));
                s.transform.localScale = new Vector3(0.22f, Random.Range(0.45f, 0.8f), 0.22f);
                s.GetComponent<MeshRenderer>().sharedMaterial = spikeMat;
                Destroy(s, 0.7f);
            }
            SkillFx.Dust(centre + Vector3.forward * radius, 0.8f, Dust, 4);
        }

        // ---------- the environment: columns, and claws stuck in a wall ----------
        /// <summary>Each spike of the volley, when it is thrown, will plant a crystal column where it lands: a little way behind where the player stands, fanned out (<see cref="ScorpionRules.FanAngle"/>). They block later spikes and the claw charge.</summary>
        void PlantColumns(MoveDef m, float phaseTime, SurvivalStats target)
        {
            while (spearsPlanted < m.SpearCount && phaseTime + 1e-4f >= SpearRules.LaunchTime(spearsPlanted, m.LaunchInterval))
            {
                Vector3 origin = tailTip != null ? tailTip.position : transform.position;
                Vector3 aim = target != null ? target.transform.position : origin + transform.forward * 10f;
                Vector3 dir = Flat(aim - origin); float dist = dir.magnitude + 2.5f;
                dir = dir.sqrMagnitude < 1e-4f ? transform.forward : dir.normalized;
                dir = Quaternion.Euler(0f, ScorpionRules.FanAngle(spearsPlanted, m.SpearCount, 24f), 0f) * dir;
                Vector3 land = new Vector3(origin.x, 0f, origin.z) + dir * dist;
                pendingColumns.Add(new Pending { at = Time.time + ScorpionRules.ColumnDelay(dist, m.SpearSpeed), pos = land });
                spearsPlanted++;
            }
        }

        void FlushColumns()
        {
            for (int i = pendingColumns.Count - 1; i >= 0; i--)
            {
                if (Time.time < pendingColumns[i].at) continue;
                Vector3 pos = pendingColumns[i].pos; pendingColumns.RemoveAt(i);
                if (Physics.Raycast(pos + Vector3.up * 6f, Vector3.down, out var hit, 14f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) pos = hit.point; else continue;
                bool onPlayer = false;
                foreach (var p in SurvivalStats.All) if (p != null && Flat(p.transform.position - pos).sqrMagnitude < 1.3f * 1.3f) onPlayer = true;
                if (onPlayer) continue;   // never plant it inside the player
                CrystalColumn.Spawn(pos, 0.7f, 1.6f, ScorpionRules.ColumnLife);
                Say("crystal_plant", pos, 0.8f);
            }
        }

        /// <summary>The charge ran into a wall or a column: the claws jam for <paramref name="seconds"/> (it is never stunned, but it lies open), a column breaks, rock and shards fall.</summary>
        public void WallStuck(float seconds, Vector3 point)
        {
            if (dead) return;
            stuckUntil = Time.time + seconds;
            desiredSpeed = 1f; Trigger("Stuck");
            bool column = CrystalColumn.ShatterNear(point, 2.5f);
            Say(column ? "crystal_shatter" : "claws_stuck", point, 1f);
            SkillFx.Dust(point, 1.6f, Dust, 30); SkillFx.Burst(point + Vector3.up * 0.5f, Crystal, 14, 4f); ImpactFx.Thump(point, 0.25f, 0.6f, 24f);
        }

        // ---------- hurting the player ----------
        bool HurtArea(Vector3 at, float radius, float damage, float force, float freeze, bool kickUp)
        {
            bool any = false;
            foreach (var p in SurvivalStats.All)
            {
                if (p == null || p.IsRemote || p.State.IsDead) continue;
                Vector3 away = Flat(p.transform.position - at);
                if (!BurrowRules.Hits(away.magnitude, radius)) continue;
                any = true;
                HurtOne(p, away.sqrMagnitude < 1e-4f ? transform.forward : away.normalized, damage, force, freeze, kickUp);
            }
            return any;
        }

        void HurtOne(SurvivalStats p, Vector3 away, float damage, float force, float freeze, bool kickUp = false)
        {
            p.State.Damage(damage * combat.DamageScale);
            Impulse.Kick(p.GetComponent<Rigidbody>(), away * force + Vector3.up * (kickUp ? force * 1.1f : force * 0.5f));
            if (freeze > 0f && p.TryGetComponent(out Evolve.Player.SlimeController sc)) sc.FreezeFor(freeze);
        }

        void OnDestroy() { Evolve.World.ScorpionRoom.Uninstall(); }

        void OnDisable() { IgnorePlayers(false); if (mound != null) Destroy(mound.gameObject); if (underground) SetHidden(false); }
    }
}
