using Evolve.Abilities;
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
        Transform tailTip;
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
        int rain;

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
            foreach (var t in GetComponentsInChildren<Transform>(true)) if (t.name == "TailTip") tailTip = t;
            lastPos = transform.position;
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
                if (combat == null || combat.Phase == MovePhase.Idle) anim.speed = speed > 0.1f ? ScorpionRules.WalkAnimSpeed(speed) : 1f;
                if (pred != null)
                {
                    if (lastHealth > 0f && pred.Health < lastHealth - 0.01f && pred.Health > 0f) { Trigger("Hit"); ProceduralSfx.PlayFile(Sfx + "scorpion_hurt", transform.position, 1f, ProceduralSfx.Kind.HoundHurt); BossMusic.Ping("scorpion_battle"); }
                    if (pred.Health <= 0f && !dead) { dead = true; Trigger("Die"); anim.speed = 1f; Abort(); BossMusic.Silence(1.5f); ProceduralSfx.PlayFile(Sfx + "scorpion_death", transform.position, 1f, ProceduralSfx.Kind.HoundThud); }
                    lastHealth = pred.Health;
                }
            }
            if (combat != null && combat.Phase != MovePhase.Idle) BossMusic.Ping("scorpion_battle");   // a move in progress is a blow in the fight
            if (glow != null) glow.intensity = Mathf.MoveTowards(glow.intensity, glowTarget, 14f * dt);
        }

        bool InLocomotion() => anim == null || anim.runtimeAnimatorController == null || (anim.GetCurrentAnimatorStateInfo(0).IsName("Locomotion") && !anim.IsInTransition(0));

        void Trigger(string name)
        {
            if (anim == null || anim.runtimeAnimatorController == null) return;
            foreach (var p in anim.parameters) if (p.type == AnimatorControllerParameterType.Trigger && p.name == name) { anim.SetTrigger(name); return; }
        }

        static float ClipSeconds(MovePhase phase, string moveAnim)
        {
            if (phase == MovePhase.Windup) return moveAnim == "Burst" ? 1.8f : 0.8f;     // Tell clips: 24 frames at 30 fps (54 for the ultimate)
            if (phase == MovePhase.Active) return moveAnim == "Burrow" ? 1.2f : 0.4f;    // Strike clips: 12 frames (36 for the burrow)
            return moveAnim == "Burst" ? 1.2f : 0.8f;                                    // Recover clips: 24 frames (36 for the ultimate)
        }

        // ---------- the three phases of every move ----------
        /// <summary>A move's phase began (m is null when it is over). Fires the phase's clip, stretched over the phase, with its effects and sound.</summary>
        public void OnPhase(MovePhase phase, MoveDef m)
        {
            if (dead) return;
            if (m == null || phase == MovePhase.Idle)
            {
                glowTarget = 0f;
                if (anim != null) { anim.speed = 1f; if (!InLocomotion()) Trigger("Reset"); }   // a move cut short goes back to walking
                return;
            }
            if (anim != null)
            {
                foreach (var stale in new[] { "Reset", "Hit" }) anim.ResetTrigger(stale);   // a trigger nobody could take must not fire later, in the middle of the next move
                float seconds = phase == MovePhase.Windup ? m.Windup * combat.TellScale : phase == MovePhase.Active ? m.Active : m.Recover;
                anim.speed = ScorpionRules.PhaseAnimSpeed(ClipSeconds(phase, m.Anim), seconds);
            }
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
            if (m == null) return;
            if (m.Shape == MoveShape.Burrow && phase == MovePhase.Active) TickBurrow(m, phaseTime, target);
            else if (m.Shape == MoveShape.Shatter && phase == MovePhase.Active) TickBurst(m, phaseTime, target);
            else if (underground && phase != MovePhase.Active) Surface();   // cut short (stunned): it comes up where it is
        }

        /// <summary>The move was cut short (stunned, killed): whatever it was doing underground or in the burst ends.</summary>
        public void Abort()
        {
            if (underground) Surface();
            if (mound != null) { Destroy(mound.gameObject); mound = null; }
            glowTarget = 0f;
        }

        // ---------- Đào Chui Trồi Lên ----------
        void BeginUnderground(MoveDef m)
        {
            underground = true; marked = false; burrowHit = false; burrowFrom = rb != null ? rb.position : transform.position; burrowSpot = burrowFrom;
            SetHidden(true);
            Say("burrow_travel", transform.position, 1f);
            if (mound == null)
            {
                var g = new GameObject("Gò Đất"); mound = g.transform;
                var c = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(c.GetComponent<Collider>());
                c.transform.SetParent(mound, false); c.transform.localScale = new Vector3(1.6f, 0.7f, 1.6f); c.transform.localPosition = new Vector3(0f, 0.1f, 0f);
                c.GetComponent<MeshRenderer>().sharedMaterial = SkillFx.Gel(Dust, 1f, 0.2f, 0.2f);
            }
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
            bool any = HurtArea(at, BurrowRules.Dig > 0f ? m.Radius : m.Radius, m.Damage, m.Force, m.Freeze, true);
            Say("burrow_burst", at, 1f);
            SkillFx.Ring(at, m.Radius, Dust, 0.5f, 0.2f); SkillFx.Dust(at, m.Radius, Dust, 60, 0.5f); SkillFx.Burst(at + Vector3.up * 0.3f, Crystal, 24, 6f, 0.14f, 0.9f, 0.7f);
            ImpactFx.Thump(at, 0.4f, 0.9f, 28f);
            if (!any && BurrowRules.ExposedFactor(true) > 1f) combat.Stagger(1.0f);   // a miss: it lies open for a moment (counter window)
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
            burstT = lastBurstT = 0f; rain = 0; aimBearing = 0f;
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
            // the spikes thrown up and dropped round the player once the rings have gone out
            float rainAt = ShatterRules.RingStart(ShatterRules.RingCount - 1) + 0.7f;
            while (rain < m.SpikeCount && t >= rainAt + rain * 0.18f)
            {
                rain++;
                Vector3 around = (target != null ? target.transform.position : c) + new Vector3(Random.Range(-2.5f, 2.5f), 0f, Random.Range(-2.5f, 2.5f));
                MonsterProjectile.Launch(tailTip != null ? tailTip.position : c + Vector3.up * 2f, around, 0.9f, m.MissileDamage * combat.DamageScale, 0.8f, false, 0.4f);
                Say("volley_fire", c, 0.6f);
            }
            lastBurstT = t;
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

        void OnDisable() { if (mound != null) Destroy(mound.gameObject); if (underground) SetHidden(false); }
    }
}
