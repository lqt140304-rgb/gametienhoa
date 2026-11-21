using Evolve.Creatures;
using UnityEngine;

namespace Evolve.Combat
{
    /// <summary>
    /// Body language and chest core of the Meshy Golem Thạch Anh ("GolemThachAnh (Meshy)"). Its walk and run are Meshy's clips (the Animator); in LateUpdate this adds
    /// the moves on top by turning the rig's arm and spine bones (Spine01, LeftArm/RightArm...) as <see cref="GolemRules"/> says for the move in progress, and drives a warm light in the
    /// chest (the core), brightest in the core blast and glowing while the crystal armour holds. The direction each bone must turn is measured at start (the hand's
    /// height and sideways position), so no bone axis is assumed. A hit makes the core flicker and the golem grunt.
    /// </summary>
    public class GolemRigFx : MonoBehaviour
    {
        public static bool Has(string name) => !string.IsNullOrEmpty(name) && name.Contains("(Meshy)") && SpeciesNames.Key(name) == "golemthachanh";

        public static GolemRigFx Attach(GameObject creature) =>
            creature == null || creature.GetComponent<GolemRigFx>() != null ? null : creature.AddComponent<GolemRigFx>();

        class Part { public Transform T; public Quaternion Rest, Base, Written; public bool Has; public Vector3 Right, Forward, Up; }

        Part spine, armL, armR, foreL, foreR;
        Transform handL, handR, chest;
        float raiseSign = 1f, spreadSign = 1f, hunchSign = 1f;
        ArmPose pose;
        Light coreLight;
        float lastHealth = -1f, flickerUntil;
        CreatureCombat combat;
        Predator pred;
        Animator animator;
        bool hasRates;
        Vector3 lastPos;
        float speed;
        const float WalkStride = 1.9f, RunStride = 3.0f;   // metres one walk / run cycle covers at the model's own scale

        /// <summary>
        /// Turn a bone by <paramref name="extra"/> on top of what the Animator wrote this frame. If the Animator left the bone alone (a state with no clip), it still holds
        /// last frame's turn, so start again from the pose we remembered: the turns must never stack up frame after frame.
        /// </summary>
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

        void Start()
        {
            combat = GetComponent<CreatureCombat>();
            pred = GetComponent<Predator>();
            animator = GetComponentInChildren<Animator>(true);
            lastPos = transform.position;
            if (animator != null && animator.runtimeAnimatorController != null) foreach (var pa in animator.parameters) if (pa.name == "WalkRate") hasRates = true;
            Transform sp = null, sp1 = null, aL = null, aR = null, fL = null, fR = null;
            foreach (var t in GetComponentsInChildren<Transform>(true))
                switch (t.name)
                {
                    case "Spine01": sp1 = t; break;
                    case "Spine": sp = t; break;
                    case "LeftArm": aL = t; break;
                    case "RightArm": aR = t; break;
                    case "LeftForeArm": fL = t; break;
                    case "RightForeArm": fR = t; break;
                    case "LeftHand": handL = t; break;
                    case "RightHand": handR = t; break;
                }
            chest = sp1 != null ? sp1 : sp;
            spine = Make(sp1 != null ? sp1 : sp); armL = Make(aL); armR = Make(aR); foreL = Make(fL); foreR = Make(fR);
            if (pred != null) lastHealth = pred.Health;
            if (armL != null && handL != null)
            {
                // raising: turn about the creature's right axis so the hand goes up; spreading: about its forward axis so the hand goes outward
                float y = handL.position.y;
                armL.T.localRotation = armL.Rest * Quaternion.AngleAxis(10f, armL.Right);
                raiseSign = handL.position.y > y ? 1f : -1f;
                armL.T.localRotation = armL.Rest;
                float x = Vector3.Dot(handL.position - transform.position, transform.right);
                armL.T.localRotation = armL.Rest * Quaternion.AngleAxis(10f, armL.Forward);
                spreadSign = Mathf.Abs(Vector3.Dot(handL.position - transform.position, transform.right)) > Mathf.Abs(x) ? 1f : -1f;
                armL.T.localRotation = armL.Rest;
            }
            if (spine != null && chest != null)
            {
                float z = Vector3.Dot(handL != null ? handL.position : chest.position + transform.up, transform.forward);
                spine.T.localRotation = spine.Rest * Quaternion.AngleAxis(10f, spine.Right);
                hunchSign = Vector3.Dot(handL != null ? handL.position : chest.position + transform.up, transform.forward) > z ? 1f : -1f;   // + leans forward
                spine.T.localRotation = spine.Rest;
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

        /// <summary>The walk and run clips play as fast as the golem really moves (a cave lord is scaled up and sped up, so a fixed clip speed would make the feet slide).</summary>
        void Update()
        {
            if (!hasRates) return;
            float dt = Mathf.Max(Time.deltaTime, 1e-4f);
            float sp = (transform.position - lastPos).magnitude / dt; lastPos = transform.position;
            speed = Mathf.Lerp(speed, sp, 1f - Mathf.Exp(-8f * dt));
            float scale = Mathf.Max(0.1f, animator.transform.lossyScale.y);
            animator.SetFloat("WalkRate", Mathf.Clamp(speed / (WalkStride * scale), 0.3f, 3f));
            animator.SetFloat("RunRate", Mathf.Clamp(speed / (RunStride * scale), 0.3f, 3f));
        }

        void LateUpdate()
        {
            float dt = Mathf.Max(Time.deltaTime, 1e-4f), t = Time.time;
            if (pred != null)
            {
                float hp = pred.Health;
                if (lastHealth > 0f && hp < lastHealth - 0.01f) { flickerUntil = t + 0.35f; GolemFx.Hurt(transform.position); BossMusic.Ping("golem_battle"); }
                lastHealth = hp;
            }
            ArmPose target = default;
            if (combat != null && combat.CurrentMove != null && combat.Phase != MovePhase.Idle)
            {
                target = GolemRules.Evaluate(combat.CurrentMove.Anim, combat.Phase, combat.PhaseProgress);
                BossMusic.Ping("golem_battle");   // any move in progress is a blow in the fight
            }
            float k = 1f - Mathf.Exp(-10f * dt);
            pose = ArmPose.Lerp(pose, target, k);

            if (spine != null) Put(spine, Quaternion.AngleAxis(hunchSign * pose.Hunch, spine.Right));
            Arm(armL, foreL, +1f);
            Arm(armR, foreR, -1f);

            if (coreLight != null)
            {
                float flicker = t < flickerUntil ? 0.6f + 0.4f * Mathf.Sin(t * 60f) : 0f;
                float idle = 0.25f + 0.1f * Mathf.Sin(t * 2f);   // a slow pulse even at rest
                coreLight.intensity = Mathf.Max(idle, pose.Glow) * 4f + flicker * 2f;
            }
        }

        /// <summary>The arm's extra turn: raise (both arms), back (the right arm only: the throw), spread; the forearm follows a little so the arm bends.</summary>
        void Arm(Part arm, Part fore, float side)
        {
            if (arm == null) return;
            float raise = pose.Raise - (side < 0f ? pose.Back : 0f);   // back (cocking the right, throwing arm) is the opposite of raising
            var q = Quaternion.AngleAxis(raiseSign * raise, arm.Right) * Quaternion.AngleAxis(spreadSign * pose.Spread, arm.Forward);
            Put(arm, q);
            if (fore != null) Put(fore, Quaternion.AngleAxis(-raiseSign * Mathf.Clamp(raise, 0f, 140f) * 0.25f, fore.Right));
        }
    }
}
