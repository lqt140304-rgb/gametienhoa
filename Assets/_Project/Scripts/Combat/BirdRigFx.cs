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
        // a rig with a hip and a knee per leg (Thigh_L/R + Shin_L/R, the Dreadwing model): CreatureLocomotion does not step these (it only knows Leg_*), so we walk them ourselves
        const float HipTuckDeg = 60f, HipReachDeg = 40f, KneeTuckDeg = 55f, WalkSwingDeg = 24f, WalkKneeDeg = 30f, StrideMeters = 1.6f;
        Wing[] wings;
        Transform beak, beakTip, root, model;
        Part rootPart, headPart, tailPart, legL, legR, shinL, shinR;
        bool ownGait;
        float stride;
        Vector3 rootRest, rootUp;
        Quaternion beakRest;
        Vector3 beakAxis;
        float beakSign = 1f, pitchSign = 1f, open, nextScreech, screechT = -1f, phase, moving;
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
            model = transform.Find("Model");
            Transform[,] w = new Transform[2, 3];
            Transform head = null, tail = null, lL = null, lR = null, sL = null, sR = null;
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
            }
            ownGait = lL != null && lR != null && lL.name == "Thigh_L";
            lastPos = transform.position;
            wings = new Wing[2];
            for (int i = 0; i < 2; i++) wings[i] = MakeWing(w[i, 0], w[i, 1], w[i, 2]);
            if (root != null) { rootRest = root.localPosition; rootUp = root.parent != null ? root.parent.InverseTransformDirection(transform.up) : transform.up; }
            rootPart = Make(root, transform); headPart = Make(head, transform); tailPart = Make(tail, transform); legL = Make(lL, transform); legR = Make(lR, transform); shinL = Make(sL, transform); shinR = Make(sR, transform);
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
            Debug.Log($"[BirdRigFx] {name}: cánh L(lift {(wings[0] != null ? wings[0].LiftSign : 0)}, sweep {(wings[0] != null ? wings[0].SwingSign : 0)}) R(lift {(wings[1] != null ? wings[1].LiftSign : 0)}, sweep {(wings[1] != null ? wings[1].SwingSign : 0)}), cúi {pitchSign}, chân {(legL != null && legR != null ? (ownGait ? "có (tự đi, gối " + (shinL != null && shinR != null ? "có" : "THIẾU") + ")" : "có") : "THIẾU")}, đầu {(headPart != null ? "có" : "thiếu")}, đuôi {(tailPart != null ? "có" : "thiếu")}");
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
            else target = new BirdPose { Spread = 0.55f * moving };                                  // folded at rest, half open walking
            pose = BirdPose.Lerp(pose, target, 1f - Mathf.Exp(-(scripted ? 12f : 4f) * dt));

            phase += 6.2832f * WingRules.Frequency(pose.Beat, moving) * dt;
            if (phase > 1000f) phase %= 6.2832f;

            if (wings != null)
                foreach (var wing in wings)
                {
                    if (wing == null) continue;
                    for (int i = 0; i < 3; i++)
                    {
                        var seg = wing.S[i];
                        if (seg == null) continue;
                        WingRules.Segment(i, phase, pose, moving, out float lift, out float sweep);
                        seg.T.localRotation = seg.Rest * Quaternion.AngleAxis(wing.SwingSign * sweep, seg.Swing) * Quaternion.AngleAxis(wing.LiftSign * lift, seg.Lift);
                    }
                }

            // the body dips and lifts with the beats
            if (root != null && pose.Beat > 0.01f)
                root.localPosition = rootRest + rootUp * (0.05f * pose.Beat * Mathf.Max(0f, Mathf.Sin(phase)) * (1f / Mathf.Max(0.0001f, root.parent != null ? root.parent.lossyScale.y : 1f)));
            else if (root != null) root.localPosition = rootRest;

            // the body bends and the legs tuck: nose down in a dive, up in a flare; the head stays more level, the tail balances
            float pitch = pose.Pitch * pitchSign;
            if (rootPart != null) Put(rootPart, Quaternion.AngleAxis(pitch, rootPart.Axis));
            if (headPart != null) Put(headPart, Quaternion.AngleAxis(-0.5f * pitch, headPart.Axis));
            if (tailPart != null) Put(tailPart, Quaternion.AngleAxis(-0.4f * pitch, tailPart.Axis));
            // legs: + (in the nose-down sense) swings the feet back: tucked; - swings them forward: reaching
            float tuck = ownGait ? (pose.Legs >= 0f ? pose.Legs * HipTuckDeg : pose.Legs * HipReachDeg) : (pose.Legs >= 0f ? pose.Legs * TuckDeg : pose.Legs * ReachDeg);
            float kneeTuck = pose.Legs >= 0f ? pose.Legs * KneeTuckDeg : 0f;
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

            if (beak != null && beakTip != null)
            {
                if (screechT < 0f && !busy && !scripted && t > nextScreech) { screechT = 0f; nextScreech = t + Random.Range(7f, 12f); }
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
