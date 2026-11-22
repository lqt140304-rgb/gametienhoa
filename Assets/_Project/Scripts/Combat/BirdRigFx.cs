using UnityEngine;

namespace Evolve.Combat
{
    /// <summary>
    /// The body of the Meshy Chim Hang Mẹ (prefab "ChimHangMe (Meshy)"), turned from its rig bones: Pinion_L1/L2/L3 and Pinion_R1/R2/R3 (three segments per wing, shoulder to tip; called Pinion, not Wing,
    /// so CreatureLocomotion keeps stepping on the legs), Root, Head, Tail1, Leg_L/Leg_R, Beak (the lower beak, modelled open) with a tiny BeakTip child.
    /// What the bones do comes from a <see cref="BirdPose"/> (from <see cref="BirdMoves"/> in a move or in the air, otherwise folded wings and a flutter when it walks):
    /// the wings beat and draw in on every up stroke (<see cref="WingRules"/>), the legs tuck up under the belly or reach out for a grab, the body pitches nose down in a dive and nose up in a flare,
    /// the head stays more level than the body, the tail balances against it, the beak opens. While it flies in from far away the whole model is lifted off the ground.
    /// Which way each bone must turn is measured at start, so no bone axis is assumed. Runs after the other animation (LateUpdate).
    /// </summary>
    [DefaultExecutionOrder(70)]   // after CreatureLocomotion (50) and CreatureCombat (60), which restore Model's transform each LateUpdate
    public class BirdRigFx : MonoBehaviour
    {
        public static bool Has(string name) => !string.IsNullOrEmpty(name) && name.Contains("(Meshy)") && Creatures.SpeciesNames.Key(name) == "chimhangme";

        public static BirdRigFx Attach(GameObject creature) =>
            creature == null || creature.GetComponent<BirdRigFx>() != null ? null : creature.AddComponent<BirdRigFx>();

        class Seg { public Transform T; public Quaternion Rest; public Vector3 Lift, Swing; }
        class Wing { public Seg[] S = new Seg[3]; public float LiftSign = 1f, SwingSign = 1f; }
        /// <summary>A bone turned on top of what CreatureLocomotion wrote this frame (it writes the root, head, tail and legs itself); if it left the bone alone, it still holds our last turn, so we start from what we remembered.</summary>
        class Part { public Transform T; public Quaternion Base, Written; public bool Has; public Vector3 Axis; }

        const float BeakClosedDeg = 26f, TuckDeg = 85f, ReachDeg = 55f;
        // the jet pose (BirdPose.Stretch): with the body flat the hind legs go straight back, level with it (checked in Blender: hip 40, knee and ankle straightened)
        const float HipStretchDeg = 40f, KneeStretchDeg = 45f, TarsusStretchDeg = 45f, WalkNaturalSpeed = 1.3f;   // WalkNaturalSpeed: metres a second the walk clips cover at scale 1 (animator speed = real speed / this)
        // a rig with a hip and a knee per leg (Thigh_L/R + Shin_L/R, the Dreadwing model): CreatureLocomotion does not step these (it only knows Leg_*), so we walk them ourselves
        const float HipTuckDeg = 60f, HipReachDeg = 40f, KneeTuckDeg = 55f, WalkSwingDeg = 24f, WalkKneeDeg = 30f, StrideMeters = 1.6f;
        Wing[] wings;
        Transform beak, beakTip, root, model;
        Part rootPart, headPart, tailPart, legL, legR, shinL, shinR, tarsL, tarsR;
        Animator animator;
        Vector3 updPos;
        bool hasMoveParams;
        static readonly int MoveXHash = Animator.StringToHash("MoveX"), MoveYHash = Animator.StringToHash("MoveY");
        bool ownGait, kneeRig, clipWalk;   // kneeRig: hip + knee per leg (Dreadwing); clipWalk: its Walk clip drives the legs, so we do not
        float stride, yawSign = 1f, headYaw;   // yawSign: + turns the head to the creature's right
        Vector3 headUp;
        Vector3 rootRest, rootUp;
        Quaternion beakRest;
        Vector3 beakAxis;
        float beakSign = 1f, pitchSign = 1f, open, nextScreech, screechT = -1f, phase, moving, lastHealth = -1f, lastCycle;
        Evolve.Creatures.Predator pred;
        BirdPose pose;
        CreatureCombat combat;
        BirdMoves bird;
        Vector3 lastPos, lastLift, lastWritten;

        static Part Make(Transform t, Transform creature) => t == null ? null : new Part { T = t, Axis = t.InverseTransformDirection(creature.right).normalized };

        static void Put(Part p, Quaternion extra)
        {
            if (p == null) return;
            var cur = p.T.localRotation;
            Quaternion b = p.Has && Quaternion.Angle(cur, p.Written) < 0.01f ? p.Base : cur;
            p.Base = b;
            p.T.localRotation = b * extra;
            p.Written = p.T.localRotation; p.Has = true;
        }

        void Start()
        {
            combat = GetComponent<CreatureCombat>();
            bird = GetComponent<BirdMoves>();
            pred = GetComponent<Evolve.Creatures.Predator>();
            model = transform.Find("Model");
            Transform[,] w = new Transform[2, 3];
            Transform head = null, tail = null, lL = null, lR = null, sL = null, sR = null, tsL = null, tsR = null;
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                string n = t.name;
                if (n.Length == 9 && n.StartsWith("Pinion_") && (n[7] == 'L' || n[7] == 'R') && n[8] >= '1' && n[8] <= '3') w[n[7] == 'L' ? 0 : 1, n[8] - '1'] = t;
                else if (n == "Beak") beak = t;
                else if (n == "BeakTip") beakTip = t;
                else if (n == "Root") root = t;
                else if (n == "Head") head = t;
                else if (n == "Tail1") tail = t;
                else if (n == "Leg_L" || n == "Thigh_L") lL = t;
                else if (n == "Leg_R" || n == "Thigh_R") lR = t;
                else if (n == "Shin_L") sL = t;
                else if (n == "Shin_R") sR = t;
                else if (n == "Tarsus_L") tsL = t;
                else if (n == "Tarsus_R") tsR = t;
            }
            kneeRig = lL != null && lR != null && lL.name == "Thigh_L";
            animator = GetComponentInChildren<Animator>(true);
            var ctrl = animator != null ? animator.runtimeAnimatorController : null;
            clipWalk = kneeRig && ctrl != null && ctrl.name.Contains("Dreadwing");   // the Blender walk clip (Creature_ChimHangMe_Dreadwing.controller) moves the legs and the pelvis
            ownGait = kneeRig && !clipWalk;
            if (animator != null && clipWalk) foreach (var prm in animator.parameters) if (prm.name == "MoveX") hasMoveParams = true;
            updPos = transform.position;
            lastPos = transform.position;
            wings = new Wing[2];
            for (int i = 0; i < 2; i++) wings[i] = MakeWing(w[i, 0], w[i, 1], w[i, 2]);
            if (root != null) { rootRest = root.localPosition; rootUp = root.parent != null ? root.parent.InverseTransformDirection(transform.up) : transform.up; }
            rootPart = Make(root, transform); headPart = Make(head, transform); tailPart = Make(tail, transform); legL = Make(lL, transform); legR = Make(lR, transform); shinL = Make(sL, transform); shinR = Make(sR, transform); tarsL = Make(tsL, transform); tarsR = Make(tsR, transform);
            if (head != null)   // the head turn: + about the creature's up axis turns the beak toward its right or its left; measured on the far end of the head
            {
                headUp = head.InverseTransformDirection(transform.up).normalized;
                Transform tip = beakTip;
                if (tip == null) { float far = 0f; foreach (var c in head.GetComponentsInChildren<Transform>()) { float d = (c.position - head.position).sqrMagnitude; if (d > far) { far = d; tip = c; } } }
                if (tip != null && tip != head)
                {
                    var rest = head.localRotation;
                    float before = Vector3.Dot(tip.position, transform.right);
                    head.localRotation = rest * Quaternion.AngleAxis(10f, headUp);
                    yawSign = Vector3.Dot(tip.position, transform.right) > before ? 1f : -1f;
                    head.localRotation = rest;
                }
            }
            if (beak != null && beakTip != null)
            {
                beakRest = beak.localRotation;
                beakAxis = beak.InverseTransformDirection(transform.right);
                float y = beakTip.position.y;
                beak.localRotation = beakRest * Quaternion.AngleAxis(10f, beakAxis);
                beakSign = beakTip.position.y > y ? 1f : -1f;   // which way raises the lower beak, i.e. shuts it
                beak.localRotation = beakRest;
            }
            // pitch: turning the root about the creature's right axis by +angle either drops the head (nose down) or raises it; the same turn swings the feet backwards (tucks the legs)
            Transform probe = beakTip != null ? beakTip : head;
            if (root != null && probe != null)
            {
                var rest = root.localRotation;
                float y = probe.position.y;
                root.localRotation = rest * Quaternion.AngleAxis(10f, rootPart.Axis);
                pitchSign = probe.position.y < y ? 1f : -1f;   // + = nose down
                root.localRotation = rest;
            }
            nextScreech = Time.time + Random.Range(4f, 8f);
            Debug.Log($"[BirdRigFx] {name}: cánh L(lift {(wings[0] != null ? wings[0].LiftSign : 0)}, sweep {(wings[0] != null ? wings[0].SwingSign : 0)}) R(lift {(wings[1] != null ? wings[1].LiftSign : 0)}, sweep {(wings[1] != null ? wings[1].SwingSign : 0)}), cúi {pitchSign}, chân {(legL != null && legR != null ? (kneeRig ? (clipWalk ? "có (clip đi bộ Blender" : "có (tự đi") + ", gối " + (shinL != null && shinR != null ? "có" : "THIẾU") + ")" : "có") : "THIẾU")}, đầu {(headPart != null ? "có" : "thiếu")}, đuôi {(tailPart != null ? "có" : "thiếu")}");
        }

        Wing MakeWing(Transform a, Transform b, Transform c)
        {
            if (a == null || b == null) return null;
            var wing = new Wing();
            var ts = new[] { a, b, c };
            for (int i = 0; i < 3; i++)
                if (ts[i] != null)
                    wing.S[i] = new Seg { T = ts[i], Rest = ts[i].localRotation, Lift = ts[i].InverseTransformDirection(transform.forward), Swing = ts[i].InverseTransformDirection(transform.up) };
            // lift: turning about the creature's forward axis raises or lowers the tip; swing: turning about up sweeps the tip toward the tail or the head
            float y = b.position.y;
            a.localRotation = wing.S[0].Rest * Quaternion.AngleAxis(10f, wing.S[0].Lift);
            wing.LiftSign = b.position.y > y ? 1f : -1f;
            a.localRotation = wing.S[0].Rest;
            float f = Vector3.Dot(b.position, transform.forward);
            a.localRotation = wing.S[0].Rest * Quaternion.AngleAxis(10f, wing.S[0].Swing);
            wing.SwingSign = Vector3.Dot(b.position, transform.forward) < f ? 1f : -1f;   // + sweeps the wing back
            a.localRotation = wing.S[0].Rest;
            return wing;
        }

        /// <summary>
        /// Feeds the walk blend tree: MoveX / MoveY are the unit direction it is moving in, in its own frame (X right, Y forward), so it steps sideways and diagonally when it circles the player while facing them.
        /// The Animator's speed follows the real speed so the feet do not slide. Runs after CreatureLocomotion's Update (which sets the Speed parameter and a speed of its own) and before the Animator evaluates.
        /// </summary>
        void Update()
        {
            if (animator == null || !clipWalk || !hasMoveParams) return;
            float dt = Mathf.Max(Time.deltaTime, 1e-4f);
            Vector3 v = (transform.position - updPos) / dt; updPos = transform.position; v.y = 0f;
            float speed = v.magnitude;
            Vector3 local = transform.InverseTransformDirection(v);
            Vector2 dir = speed > 0.15f ? new Vector2(local.x, local.z).normalized : Vector2.zero;
            animator.SetFloat(MoveXHash, dir.x, 0.12f, dt);
            animator.SetFloat(MoveYHash, dir.y, 0.12f, dt);
            animator.speed = speed > 0.15f ? Mathf.Clamp(speed / (WalkNaturalSpeed * Mathf.Max(0.5f, transform.lossyScale.y)), 0.5f, 1.6f) : 1f;
        }

        void LateUpdate()
        {
            float dt = Mathf.Max(Time.deltaTime, 1e-4f), t = Time.time;
            float speed = (transform.position - lastPos).magnitude / dt; lastPos = transform.position;
            moving = Mathf.MoveTowards(moving, Mathf.Clamp01(speed / 1.5f), dt * 4f);

            bool scripted = bird != null && bird.PoseActive;
            bool busy = combat != null && combat.Busy;
            BirdPose target;
            if (scripted) target = bird.Pose;                                                        // a move, or flying
            else if (busy) target = new BirdPose { Spread = 1f, Beat = 1f, Beak = 1f };             // some move this rig has no pose for
            else target = new BirdPose();                                                            // on the ground, standing or walking: the wings stay folded like an ordinary bird's
            pose = BirdPose.Lerp(pose, target, 1f - Mathf.Exp(-(scripted ? 12f : 4f) * dt));
            float wingMoving = scripted ? moving : 0f;                                                // no flutter of the folded wings while it walks

            if (busy || scripted) BossMusic.Ping("bird_battle");   // a move in progress is a blow in the fight: the boss music
            if (pred != null)
            {
                if (lastHealth > 0f && pred.Health < lastHealth - 0.01f) { ProceduralSfx.PlayFile("Bird/bird_hurt", transform.position, 1f, ProceduralSfx.Kind.HoundHurt); BossMusic.Ping("bird_battle"); }
                lastHealth = pred.Health;
            }
            phase += 6.2832f * WingRules.Frequency(pose.Beat, wingMoving) * dt;
            float cycle = Mathf.Floor(phase / 6.2832f);
            if (cycle != lastCycle) { lastCycle = cycle; if (pose.Beat > 0.4f) ProceduralSfx.PlayFile("Bird/bird_flap", transform.position, 0.7f, ProceduralSfx.Kind.HoundSwipe); }   // a wingbeat in a fight is heard
            if (phase > 1000f) phase %= 6.2832f;

            if (wings != null)
                foreach (var wing in wings)
                {
                    if (wing == null) continue;
                    for (int i = 0; i < 3; i++)
                    {
                        var seg = wing.S[i];
                        if (seg == null) continue;
                        WingRules.Segment(i, phase, pose, wingMoving, out float lift, out float sweep);
                        seg.T.localRotation = seg.Rest * Quaternion.AngleAxis(wing.SwingSign * sweep, seg.Swing) * Quaternion.AngleAxis(wing.LiftSign * lift, seg.Lift);
                    }
                }

            // the body dips and lifts with the beats
            Vector3 bob = pose.Beat > 0.01f ? rootUp * (0.05f * pose.Beat * Mathf.Max(0f, Mathf.Sin(phase)) * (1f / Mathf.Max(0.0001f, root != null && root.parent != null ? root.parent.lossyScale.y : 1f))) : Vector3.zero;
            if (root != null)
            {
                if (clipWalk) root.localPosition += bob;   // the Animator rewrites the pelvis' position every frame (the walk clip keeps the lowest foot on the floor), so the bob is added to it
                else root.localPosition = rootRest + bob;
            }

            // the body bends and the legs tuck: nose down in a dive, up in a flare; the head stays more level, the tail balances
            float pitch = pose.Pitch * pitchSign;
            if (rootPart != null) Put(rootPart, Quaternion.AngleAxis(pitch, rootPart.Axis));
            if (headPart != null)
            {
                // the head always turns toward the player (the body faces its walking direction, or only partly the player while it circles): up to 80 degrees; in a move the body already faces them
                var prey = combat != null ? combat.EngagedTarget(60f) : null;
                float yawWant = 0f;
                if (prey != null)
                {
                    Vector3 to = prey.transform.position - transform.position; to.y = 0f;
                    if (to.sqrMagnitude > 0.25f) yawWant = Mathf.Clamp(Vector3.SignedAngle(transform.forward, to, Vector3.up), -80f, 80f) * (scripted ? 0.35f : 1f);   // + = the player is on its right
                }
                headYaw = Mathf.LerpAngle(headYaw, yawWant, 1f - Mathf.Exp(-8f * dt));
                float headK = 0.5f + 0.5f * Mathf.Clamp01((pose.Pitch - 40f) / 45f);   // half the body's pitch is undone at the head, all of it when the body lies flat: the head looks ahead
                Put(headPart, Quaternion.AngleAxis(-headK * pitch, headPart.Axis) * Quaternion.AngleAxis(yawSign * headYaw, headUp));
            }
            if (tailPart != null) Put(tailPart, Quaternion.AngleAxis(-0.3f * pitch, tailPart.Axis));
            // legs: + (in the nose-down sense) swings the feet back: tucked; - swings them forward: reaching
            float tuck = kneeRig ? (pose.Legs >= 0f ? pose.Legs * HipTuckDeg : pose.Legs * HipReachDeg) : (pose.Legs >= 0f ? pose.Legs * TuckDeg : pose.Legs * ReachDeg);
            float kneeTuck = pose.Legs >= 0f ? pose.Legs * KneeTuckDeg : 0f;
            float stretch = kneeRig ? pose.Stretch : 0f;                                     // the jet pose: the hip goes back, the knee and ankle straighten
            tuck += stretch * HipStretchDeg; kneeTuck -= stretch * KneeStretchDeg;
            float swingL = 0f, swingR = 0f, kneeL = 0f, kneeR = 0f;
            if (ownGait)   // walking: the legs swing in turn, the knee lifts the foot while the leg goes forward; less and less as it takes off
            {
                stride += 6.2832f * speed * dt / (StrideMeters * Mathf.Max(0.5f, transform.lossyScale.y));
                if (stride > 1000f) stride %= 6.2832f;
                float walk = moving * (1f - Mathf.Clamp01(Mathf.Abs(pose.Legs)));
                float sw = Mathf.Sin(stride), cw = Mathf.Cos(stride);
                swingL = sw * WalkSwingDeg * walk; swingR = -swingL;
                kneeL = Mathf.Max(0f, -cw) * WalkKneeDeg * walk; kneeR = Mathf.Max(0f, cw) * WalkKneeDeg * walk;
            }
            if (legL != null) Put(legL, Quaternion.AngleAxis((tuck + swingL) * pitchSign, legL.Axis));
            if (legR != null) Put(legR, Quaternion.AngleAxis((tuck + swingR) * pitchSign, legR.Axis));
            if (shinL != null) Put(shinL, Quaternion.AngleAxis((kneeTuck + kneeL) * pitchSign, shinL.Axis));
            if (shinR != null) Put(shinR, Quaternion.AngleAxis((kneeTuck + kneeR) * pitchSign, shinR.Axis));
            if (tarsL != null) Put(tarsL, Quaternion.AngleAxis(-stretch * TarsusStretchDeg * pitchSign, tarsL.Axis));
            if (tarsR != null) Put(tarsR, Quaternion.AngleAxis(-stretch * TarsusStretchDeg * pitchSign, tarsR.Axis));

            if (beak != null && beakTip != null)
            {
                if (screechT < 0f && !busy && !scripted && t > nextScreech) { screechT = 0f; nextScreech = t + Random.Range(7f, 12f); ProceduralSfx.PlayFile("Bird/bird_screech", transform.position, 0.9f, ProceduralSfx.Kind.HoundHowl); }
                float screech = 0f;
                if (screechT >= 0f) { screechT += dt; screech = Mathf.Sin(Mathf.Clamp01(screechT / 1.1f) * Mathf.PI); if (screechT > 1.1f) screechT = -1f; }
                float want = Mathf.Max(pose.Beak, Mathf.Clamp01(screech * 1.6f));
                open = Mathf.MoveTowards(open, want, dt * 8f);
                float quiver = open > 0.5f ? Mathf.Sin(t * 110f) * 2.5f * open : 0f;
                beak.localRotation = beakRest * Quaternion.AngleAxis(beakSign * (BeakClosedDeg * (1f - open) + quiver), beakAxis);
            }

            if (model != null) ApplyLift(bird != null ? bird.CruiseLift : 0f);
        }

        /// <summary>The model alone rises while the bird flies in from afar (its body still walks). CreatureLocomotion rewrites the model's position every frame, so this is added on top each frame; if it did not, ours is taken off first.</summary>
        void ApplyLift(float lift)
        {
            if (lastLift != Vector3.zero && (model.localPosition - lastWritten).sqrMagnitude < 1e-8f) model.localPosition -= lastLift;
            Vector3 add = Vector3.zero;
            if (lift > 0.001f) add = (model.parent != null ? model.parent : transform).InverseTransformVector(Vector3.up * lift);
            model.localPosition += add;
            lastLift = add; lastWritten = model.localPosition;
        }
    }
}
