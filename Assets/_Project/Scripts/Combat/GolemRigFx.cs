using Evolve.Creatures;
using UnityEngine;

namespace Evolve.Combat
{
    /// <summary>
    /// Everything that moves on Meshy's Golem Thạch Anh ("GolemThachAnh (Meshy)") beyond its standing pose (the Animator only holds a standing frame; Meshy's walk clips never
    /// moved its legs): (1) its walk, written as code (<see cref="GaitRules"/>): hips swing, knees bend, the body sinks at each footfall (with the footfall sound, dust and a camera thump),
    /// all tied to the distance walked so the feet never slide; (2) its moves, added on top by turning the arm and spine bones as <see cref="GolemRules"/> says; (3) a warm light
    /// in the chest, the core; (4) the crystal armour of its passive (<see cref="PassiveRules"/>) that grows out of the shoulders and back below 66% health. The direction each bone must
    /// turn is measured at start (where the foot or hand goes), so no bone axis is assumed. Every turn is added to the pose the Animator wrote this frame and never stacks up.
    /// </summary>
    // after CreatureLocomotion (50) and CreatureCombat (60): the Walk/Run clips' root curves leave Model at the Armature's 270°/0.01 until they restore it, which turned a 15 cm hip drop into 14 m
    [DefaultExecutionOrder(70)]
    public class GolemRigFx : MonoBehaviour
    {
        public static bool Has(string name) => !string.IsNullOrEmpty(name) && name.Contains("(Meshy)") && SpeciesNames.Key(name) == "golemthachanh";

        public static GolemRigFx Attach(GameObject creature) =>
            creature == null || creature.GetComponent<GolemRigFx>() != null ? null : creature.AddComponent<GolemRigFx>();

        class Part { public Transform T; public Quaternion Rest, Base, Written; public bool Has; public Vector3 Right, Forward, Up; }

        const float Stride = 1.9f, HipDeg = 24f, KneeDeg = 40f, BobMeters = 0.05f;   // metres of one walk cycle at scale 1 (the model is 2.5 m tall), degrees, metres

        Part spine, armL, armR, foreL, foreR, thighL, thighR, kneeL, kneeR, footL, footR;
        Transform hips, handL, handR, chest, shoulderL, shoulderR, footTipL;
        float raiseSign = 1f, spreadSign = 1f, hunchSign = 1f, hipSign = 1f, kneeSign = 1f, twistSign = 1f;
        bool wasResting;
        float legLen, lastRun, flinchT = 9f, restAnkleL, restAnkleR; Quaternion footRestL, footRestR; bool restMeasured;   // ankle heights above the root and foot orientations in the standing pose   // seconds since the last blow it took
        ArmPose pose;
        Light coreLight;
        float lastHealth = -1f, flickerUntil;
        CreatureCombat combat;
        Predator pred;
        FootstepFx steps;
        Vector3 lastPos, hipsBase, hipsWritten; bool hipsHas;   // the hips' LOCAL position (a world position changes whenever the whole golem walks)
        float phase, walkWeight;

        /// <summary>Turn a bone by <paramref name="extra"/> on top of what the Animator wrote this frame; if the Animator left the bone alone it still holds last frame's turn, so start from the pose we remembered.</summary>
        static void Put(Part p, Quaternion extra)
        {
            var cur = p.T.localRotation;
            Quaternion b = p.Has && Quaternion.Angle(cur, p.Written) < 0.01f ? p.Base : cur;
            p.Base = b;
            p.T.localRotation = b * extra;
            p.Written = p.T.localRotation; p.Has = true;
        }

        Part Make(Transform t) => t == null ? null : new Part
        {
            T = t, Rest = t.localRotation,
            Right = t.InverseTransformDirection(transform.right), Forward = t.InverseTransformDirection(transform.forward), Up = t.InverseTransformDirection(transform.up),
        };

        /// <summary>+1 if turning the bone by +10 degrees about the creature's right axis moves <paramref name="probe"/> by a positive amount of <paramref name="measure"/>, otherwise -1.</summary>
        float SignFor(Part bone, Transform probe, System.Func<Vector3, float> measure)
        {
            if (bone == null || probe == null) return 1f;
            float before = measure(probe.position);
            bone.T.localRotation = bone.Rest * Quaternion.AngleAxis(10f, bone.Right);
            float after = measure(probe.position);
            bone.T.localRotation = bone.Rest;
            return after > before ? 1f : -1f;
        }

        void Start()
        {
            combat = GetComponent<CreatureCombat>();
            pred = GetComponent<Predator>();
            lastPos = transform.position;
            Transform sp = null, sp1 = null;
            Transform aL = null, aR = null, fL = null, fR = null, tL = null, tR = null, kL = null, kR = null, ftL = null, ftR = null;
            foreach (var t in GetComponentsInChildren<Transform>(true))
                switch (t.name)
                {
                    case "Hips": hips = t; break;
                    case "Spine01": sp1 = t; break;
                    case "Spine": sp = t; break;
                    case "LeftShoulder": shoulderL = t; break;
                    case "RightShoulder": shoulderR = t; break;
                    case "LeftArm": aL = t; break;
                    case "RightArm": aR = t; break;
                    case "LeftForeArm": fL = t; break;
                    case "RightForeArm": fR = t; break;
                    case "LeftHand": handL = t; break;
                    case "RightHand": handR = t; break;
                    case "LeftUpLeg": tL = t; break;
                    case "RightUpLeg": tR = t; break;
                    case "LeftLeg": kL = t; break;
                    case "RightLeg": kR = t; break;
                    case "LeftFoot": ftL = t; break;
                    case "RightFoot": ftR = t; break;
                }
            chest = sp1 != null ? sp1 : sp;
            spine = Make(chest); armL = Make(aL); armR = Make(aR); foreL = Make(fL); foreR = Make(fR);
            thighL = Make(tL); thighR = Make(tR); kneeL = Make(kL); kneeR = Make(kR); footL = Make(ftL); footR = Make(ftR);
            footTipL = ftL;
            if (hips != null && ftL != null) legLen = Mathf.Max(0.2f, hips.position.y - ftL.position.y);   // hip to ankle at rest, in world metres
            if (pred != null) lastHealth = pred.Health;
            steps = FootstepFx.Attach(gameObject, "Golem/golem_step", ProceduralSfx.Kind.HoundThud, new Color(0.58f, 0.52f, 0.45f), true);
            BuildCrystals(chest);

            // measure which way each joint must turn (where the foot or the hand goes)
            Vector3 fwd = transform.forward;
            hipSign = SignFor(thighL, footTipL, p => Vector3.Dot(p, fwd));            // + = the foot swings forward
            kneeSign = -SignFor(kneeL, footTipL, p => Vector3.Dot(p, fwd));           // + = the foot swings back and up (the knee bends)
            raiseSign = SignFor(armL, handL, p => p.y);                                // + = the hand goes up
            hunchSign = SignFor(spine, handL != null ? handL : chest, p => Vector3.Dot(p, fwd));   // + = leans forward
            if (spine != null && handR != null)   // + twist must turn the right hand forward (a punch or a throw goes through)
            {
                Vector3 fw = transform.forward;
                float before = Vector3.Dot(handR.position, fw);
                spine.T.localRotation = spine.Rest * Quaternion.AngleAxis(10f, spine.Up);
                float after = Vector3.Dot(handR.position, fw);
                spine.T.localRotation = spine.Rest;
                twistSign = after > before ? 1f : -1f;
            }
            if (armL != null && handL != null)
            {
                float x = Mathf.Abs(Vector3.Dot(handL.position - transform.position, transform.right));
                armL.T.localRotation = armL.Rest * Quaternion.AngleAxis(10f, armL.Forward);
                spreadSign = Mathf.Abs(Vector3.Dot(handL.position - transform.position, transform.right)) > x ? 1f : -1f;
                armL.T.localRotation = armL.Rest;
            }
            if (chest != null)
            {
                var go = new GameObject("Lõi Sáng");
                go.transform.SetParent(chest, false);
                go.transform.position = chest.position + transform.forward * 0.35f * Mathf.Max(0.3f, transform.lossyScale.y);
                coreLight = go.AddComponent<Light>();
                coreLight.type = LightType.Point; coreLight.color = new Color(1f, 0.75f, 0.35f); coreLight.range = 7f * Mathf.Max(0.5f, transform.lossyScale.y); coreLight.intensity = 0f; coreLight.shadows = LightShadows.None;
            }
        }

        // ---- crystal armour: spiked crystal crowns on the shoulders and the back that grow out below 66% health and shrink away if it heals
        readonly System.Collections.Generic.List<Transform> crystals = new System.Collections.Generic.List<Transform>();
        float crystalT, crystalSize = 1f;

        void BuildCrystals(Transform chestBone)
        {
            var mat = Evolve.Abilities.SkillFx.Gel(new Color(0.82f, 0.74f, 0.55f), 1f, 0.9f, 2.2f);
            float s = Mathf.Max(0.3f, transform.lossyScale.y);
            var mesh = LordRegalia.ToMesh(RegaliaMeshes.Crown(0.32f * s, 0.8f * s, 6), false);
            foreach (var (bone, up) in new[] { (shoulderL, 0.15f), (shoulderR, 0.15f), (chestBone, 0.35f) })
            {
                if (bone == null) continue;
                var go = new GameObject("Tinh Thể");
                go.transform.SetParent(bone, false);
                go.transform.position = bone.position + Vector3.up * up * s;
                go.transform.rotation = Quaternion.identity;
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = mat; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                go.transform.localScale = Vector3.zero;
                crystalSize = 1f / Mathf.Max(0.0001f, bone.lossyScale.y);
                crystals.Add(go.transform);
            }
        }

        void LateUpdate()
        {
            float dt = Mathf.Max(Time.deltaTime, 1e-4f), t = Time.time;
            if (!restMeasured && footL != null && footR != null)   // the standing pose the Animator just wrote, before any turn of ours
            {
                restAnkleL = footL.T.position.y - transform.position.y; restAnkleR = footR.T.position.y - transform.position.y;
                footRestL = Quaternion.Inverse(transform.rotation) * footL.T.rotation; footRestR = Quaternion.Inverse(transform.rotation) * footR.T.rotation;
                restMeasured = true;
            }
            float scale = Mathf.Max(0.3f, transform.lossyScale.y);

            // ---- health: flicker and grunt when hit, the passive's crystals
            float hp01 = pred != null ? pred.HealthFraction : 1f;
            if (pred != null)
            {
                float hp = pred.Health;
                if (lastHealth > 0f && hp < lastHealth - 0.01f) { flickerUntil = t + 0.35f; flinchT = 0f; GolemFx.Hurt(transform.position); BossMusic.Ping("golem_battle"); }
                lastHealth = hp;
            }
            bool armour = PassiveRules.CrystalsOut(name, hp01);
            crystalT = Mathf.MoveTowards(crystalT, armour ? 1f : 0f, dt * (armour ? 1.2f : 0.8f));
            for (int i = 0; i < crystals.Count; i++) if (crystals[i] != null) crystals[i].localScale = Vector3.one * crystalSize * Mathf.SmoothStep(0f, 1f, crystalT);

            // ---- the move in progress
            ArmPose target = default;
            bool gripping = TryGetComponent(out HoundPin hold) && hold.Chomping;
            if (gripping) target = GolemRules.Evaluate("Grab", MovePhase.Active, 1f);   // fists closed round the player while it squeezes
            else if (combat != null && combat.CurrentMove != null && combat.Phase != MovePhase.Idle)
            {
                target = GolemRules.Evaluate(combat.CurrentMove.Anim, combat.Phase, combat.PhaseProgress);
                BossMusic.Ping("golem_battle");   // any move in progress is a blow in the fight
            }
            if (gripping) BossMusic.Ping("golem_battle");
            if (combat != null && combat.VolleyNow is VolleyStage vs && combat.Phase == MovePhase.Recover) target = GolemRules.Volley(vs, combat.VolleyTime);   // the rock volley: getting up, hugging the column, aiming like a javelin
            if (combat != null && combat.Resting) { target = GolemRules.Winded(combat.RestTime, combat.RestLength); if (!wasResting) GolemFx.Pant(transform.position); wasResting = true; } else wasResting = false;   // kneeling, out of breath, after a move
            if (combat != null && combat.Dizzy) target = GolemRules.Evaluate("Dizzy", MovePhase.Active, 1f);                                              // dazed after hitting a wall head first
            flinchT += dt;
            if (!gripping && (combat == null || combat.CurrentMove == null || combat.Phase == MovePhase.Idle) && flinchT < 0.9f)   // a blow staggers it when it is not busy with a move
                target = flinchT < 0.2f ? GolemRules.Evaluate("Hurt", MovePhase.Windup, flinchT / 0.2f) : GolemRules.Evaluate("Hurt", MovePhase.Recover, (flinchT - 0.2f) / 0.7f);
            bool leaping = combat != null && combat.Airborne;
            float follow = leaping || (combat != null && combat.CurrentMove != null && combat.Phase == MovePhase.Active) ? 14f : 7f;   // the strike snaps, the wind-up is slow
            pose = ArmPose.Lerp(pose, target, 1f - Mathf.Exp(-follow * dt));   // heavy: the arms take their time
            if (combat != null && combat.CurrentMove != null && combat.CurrentMove.Anim == "CoreBlast" && combat.Phase == MovePhase.Windup)
                ImpactFx.Thump(transform.position, 0.015f + 0.05f * combat.PhaseProgress, 0.2f, 24f);   // the core charging: the ground rumbles more and more

            // ---- the walk: one phase that follows the distance walked
            Vector3 step = transform.position - lastPos; lastPos = transform.position; step.y = 0f;   // sideways distance only: a leap's height is not walking
            float moved = step.magnitude;
            bool airborne = combat != null && combat.Airborne;
            bool busy = combat != null && combat.Phase != MovePhase.Idle;
            float speed = moved / dt;
            walkWeight = Mathf.MoveTowards(walkWeight, speed > 0.15f && speed < 16f && !gripping && !airborne ? 1f : 0f, dt * 4f);   // not in the air, and not the fall to the marked spot (16+ m/s)
            float prev = phase;
            if (!airborne && speed < 16f) phase += GaitRules.PhaseAdvance(moved, legLen > 0.2f ? 4f * legLen * Mathf.Sin(HipDeg * (1f + 0.8f * lastRun) * Mathf.Deg2Rad) : Stride * scale);   // one cycle = two steps of 2 L sin(angle): the planted foot then moves back exactly as fast as the body goes forward, so it does not slide
            if (walkWeight > 0.3f) { if (FootstepRules.Contacts(prev, phase, new[] { 0.25f, 0.75f }) > 0 && steps != null) steps.StepNow(); }
            if (phase > 1000f) phase = phase % 1f;
            float ph = phase % 1f;
            float run = GaitRules.RunBlend(speed) * walkWeight; lastRun = run;   // the dash to a spike is a run: wider swing, deeper knees, leaning in
            float hipA = HipDeg * (1f + 0.8f * run), kneeA = KneeDeg * (1f + run);
            float hipL = GaitRules.Hip(ph, 0, hipA) * walkWeight, hipR = GaitRules.Hip(ph, 1, hipA) * walkWeight;
            float kneeLd = GaitRules.Knee(ph, 0, kneeA) * walkWeight, kneeRd = GaitRules.Knee(ph, 1, kneeA) * walkWeight;
            // body poses of the moves, over the walk: crouch (knees bent, body low), tuck (legs up in the air), stance (one foot forward)
            float crouch = pose.Crouch, tuck = pose.Tuck, stance = pose.Stance;
            float crouchHip = crouch * 35f + tuck * 55f, crouchKnee = crouch * 70f + tuck * 85f;
            hipL += crouchHip + stance * 22f; hipR += crouchHip - stance * 22f;
            kneeLd += crouchKnee + Mathf.Max(0f, -stance) * 15f; kneeRd += crouchKnee + Mathf.Max(0f, stance) * 15f;
            if (thighL != null) Put(thighL, Quaternion.AngleAxis(hipSign * hipL, thighL.Right));
            if (thighR != null) Put(thighR, Quaternion.AngleAxis(hipSign * hipR, thighR.Right));
            if (kneeL != null) Put(kneeL, Quaternion.AngleAxis(kneeSign * kneeLd, kneeL.Right));
            if (kneeR != null) Put(kneeR, Quaternion.AngleAxis(kneeSign * kneeRd, kneeR.Right));
            // the feet keep their standing orientation in the WORLD (flat on the ground) whatever the legs above them do (checked on the real model in Blender: this plus the body drop below keeps the soles within a few cm of the floor)
            if (footL != null && restMeasured) footL.T.rotation = transform.rotation * footRestL;
            if (footR != null && restMeasured) footR.T.rotation = transform.rotation * footRestR;
            if (hips != null)   // the body sinks at each footfall; kept in local space and capped, so it can never creep down into the floor
            {
                var cur = hips.localPosition;
                Vector3 b = hipsHas && (cur - hipsWritten).sqrMagnitude < 1e-10f ? hipsBase : cur;   // the Animator wrote it this frame, or it still holds our last value
                hipsBase = b;
                hips.localPosition = b; hipsHas = false;   // measure the feet with the hips at the animator's height
                float drop = 0f;
                if (restMeasured)
                {
                    float needL = restAnkleL - (footL.T.position.y - transform.position.y), needR = restAnkleR - (footR.T.position.y - transform.position.y);
                    // the body sinks so the lower-reaching foot is on the floor (the other leg hangs a little: it is the swinging one); up to +4% of a leg while the legs swing: skinning drags the heel a few cm below its bone
                    drop = Mathf.Max(needL, needR) + 0.04f * legLen * Mathf.Clamp01(Mathf.Max(Mathf.Abs(hipL), Mathf.Abs(hipR)) / 24f);   // standing still: no offset, the soles sit exactly on the floor
                    drop = Mathf.Clamp(drop, -0.6f * legLen, 0.1f * legLen) * (1f - Mathf.Clamp01(tuck * 1.5f));   // in the air (tucked) it is not on the ground: no sinking
                }
                Vector3 side = Vector3.zero;   // no sideways hip sway: it read as the golem weaving left and right as it walks
                Vector3 world = Vector3.up * drop + side;
                Vector3 down = hips.parent != null ? hips.parent.InverseTransformVector(world) : world;
                hips.localPosition = b + down;
                hipsWritten = hips.localPosition; hipsHas = true;
            }

            // ---- the spine: the move's lean plus a slow twist against the stride; the arms: the move, and a counter-swing of the stride
            float wobble = combat != null && combat.Dizzy ? Mathf.Sin(t * 5f) * 18f : 0f;   // swaying on its feet
            float twist = Mathf.Sin(ph * 2f * Mathf.PI) * (1.5f + 2f * run) * walkWeight + twistSign * (pose.Twist + wobble);   // against the stride, plus the move's wind-up and follow-through
            float lean = hunchSign * (pose.Hunch + 14f * run);
            if (spine != null) Put(spine, Quaternion.AngleAxis(lean, spine.Right) * Quaternion.AngleAxis(twist, spine.Up));
            float swing = 0.4f + 0.5f * run;
            Arm(armL, foreL, +1f, -(hipL - crouchHip - stance * 22f) * swing);
            Arm(armR, foreR, -1f, -(hipR - crouchHip + stance * 22f) * swing);

            if (coreLight != null)
            {
                float flicker = t < flickerUntil ? 0.6f + 0.4f * Mathf.Sin(t * 60f) : 0f;
                float idle = 0.25f + 0.1f * Mathf.Sin(t * 2f);   // a slow pulse even at rest
                coreLight.intensity = Mathf.Max(idle, Mathf.Max(pose.Glow, crystalT * 0.6f)) * 4f + flicker * 2f;
            }
        }

        /// <summary>The arm's extra turn: raise (both arms), back (the right arm only: the throw and the punch), spread, and a swing from the stride; the forearm follows a little so the arm bends.</summary>
        void Arm(Part arm, Part fore, float side, float swing)
        {
            if (arm == null) return;
            float raise = pose.Raise - (side < 0f ? pose.Back : 0f) + swing;   // back (cocking the right, throwing arm) is the opposite of raising
            var q = Quaternion.AngleAxis(raiseSign * raise, arm.Right) * Quaternion.AngleAxis(spreadSign * pose.Spread, arm.Forward);
            Put(arm, q);
            if (fore != null) Put(fore, Quaternion.AngleAxis(-raiseSign * Mathf.Clamp(raise, 0f, 140f) * 0.25f, fore.Right));
        }
    }
}
