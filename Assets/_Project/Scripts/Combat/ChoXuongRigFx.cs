using Evolve.Creatures;
using UnityEngine;

namespace Evolve.Combat
{
    /// <summary>
    /// Body language of the Meshy Chó Xương ("ChoXuong (Meshy)"), turned from its rig bones by code (it has no animation clips): the neck chain
    /// spine.009-.011 lifts or lowers the head, the jaw bone opens (a tiny JawTip child tells which way is open), a forepaw (Shin_FL/FR) rises and
    /// rakes for the claw, following <see cref="HoundRules"/> for the move in progress. It breathes at rest, rolls its back as it runs, and flinches when
    /// hit; a heavy hit staggers it for a moment. Its tells are read 1.5 times slower than the other creatures'. CreatureLocomotion keeps driving the
    /// legs, Root, Head and the tail chain. Every bone gets its own axis (the same world axis written in that bone's frame); directions are measured at start.
    /// </summary>
    public class ChoXuongRigFx : MonoBehaviour
    {
        public static bool Has(string name) => !string.IsNullOrEmpty(name) && name.Contains("(Meshy)") && SpeciesNames.Key(name) == "choxuong";

        public static ChoXuongRigFx Attach(GameObject creature) =>
            creature == null || creature.GetComponent<ChoXuongRigFx>() != null ? null : creature.AddComponent<ChoXuongRigFx>();

        const float TellSlowdown = 1.5f;

        class Part { public Transform T; public Quaternion Rest; public Vector3 Pitch, Yaw; }

        Part[] neck;
        Part jaw, back;
        Part[] paw = new Part[2];   // 0 = left, 1 = right
        Transform jawTip, head;
        float neckSign = 1f, jawSign = 1f, pawSign = 1f;
        float pitch, jawDeg, quiver, pawDeg, flinchT = 99f, lastHealth = -1f, runPhase, moving;
        bool flinchHeavy, wasClaw;
        int clawCount;
        CreatureCombat combat;
        Predator pred;
        Vector3 lastPos;

        Part Make(Transform t) => t == null ? null : new Part { T = t, Rest = t.localRotation, Pitch = t.InverseTransformDirection(transform.right), Yaw = t.InverseTransformDirection(transform.up) };

        void Start()
        {
            combat = GetComponent<CreatureCombat>();
            if (combat != null) combat.TellScale *= TellSlowdown;
            pred = GetComponent<Predator>();
            Transform n9 = null, n10 = null, n11 = null, back7 = null, jawT = null, shinL = null, shinR = null, footL = null, footR = null;
            foreach (var t in GetComponentsInChildren<Transform>(true))
                switch (t.name)
                {
                    case "spine.009": n9 = t; break;
                    case "spine.010": n10 = t; break;
                    case "spine.011": n11 = t; break;
                    case "spine.007": back7 = t; break;
                    case "jaw": jawT = t; break;
                    case "JawTip": jawTip = t; break;
                    case "Head": head = t; break;
                    case "Shin_FL": shinL = t; break;
                    case "Shin_FR": shinR = t; break;
                    case "Foot_FL": footL = t; break;
                    case "Foot_FR": footR = t; break;
                }
            lastPos = transform.position;
            if (pred != null) lastHealth = pred.Health;
            neck = new[] { Make(n9), Make(n10), Make(n11) };
            jaw = Make(jawT); back = Make(back7); paw[0] = Make(shinL); paw[1] = Make(shinR);
            if (neck[0] != null && head != null)
            {
                float y = head.position.y;
                neck[0].T.localRotation = neck[0].Rest * Quaternion.AngleAxis(10f, neck[0].Pitch);
                neckSign = head.position.y > y ? 1f : -1f;   // which way lifts the head
                neck[0].T.localRotation = neck[0].Rest;
            }
            if (jaw != null && jawTip != null)
            {
                float y = jawTip.position.y;
                jaw.T.localRotation = jaw.Rest * Quaternion.AngleAxis(10f, jaw.Pitch);
                jawSign = jawTip.position.y < y ? 1f : -1f;   // which way opens the mouth (the tip goes down)
                jaw.T.localRotation = jaw.Rest;
            }
            if (paw[0] != null && footL != null)
            {
                float y = footL.position.y;
                paw[0].T.localRotation = paw[0].Rest * Quaternion.AngleAxis(10f, paw[0].Pitch);
                pawSign = footL.position.y > y ? 1f : -1f;   // which way raises the paw
                paw[0].T.localRotation = paw[0].Rest;
            }
        }

        void LateUpdate()
        {
            float dt = Mathf.Max(Time.deltaTime, 1e-4f), t = Time.time;
            float speed = (transform.position - lastPos).magnitude / dt; lastPos = transform.position;
            moving = Mathf.MoveTowards(moving, Mathf.Clamp01(speed / 2f), dt * 3f);

            // a hit: the head kicks; a heavy one staggers it
            if (pred != null)
            {
                float hp = pred.Health;
                if (lastHealth > 0f && hp < lastHealth - 0.01f)
                {
                    flinchT = 0f;
                    flinchHeavy = HoundRules.IsHeavy(lastHealth - hp, pred.MaxHealth);
                    if (flinchHeavy && combat != null) combat.Stagger(HoundRules.HeavyFlinchSeconds);
                }
                lastHealth = hp;
            }
            flinchT += dt;
            float flinch = HoundRules.Flinch(flinchT, flinchHeavy);

            HoundPose target = default;
            bool claw = false;
            if (combat != null && combat.CurrentMove != null && combat.Phase != MovePhase.Idle)
            {
                string anim = combat.CurrentMove.Anim;
                target = HoundRules.Evaluate(anim, combat.Phase, combat.PhaseProgress);
                claw = anim == "Claw";
            }
            if (claw && !wasClaw) clawCount++;   // alternate the paw: left, right, left...
            wasClaw = claw;
            float k = 1f - Mathf.Exp(-9f * dt);   // eased, so a quick phase does not snap the bones
            pitch = Mathf.Lerp(pitch, target.NeckPitch, k);
            jawDeg = Mathf.Lerp(jawDeg, target.JawDeg, k);
            quiver = Mathf.Lerp(quiver, target.Quiver, k);
            pawDeg = Mathf.Lerp(pawDeg, target.PawDeg, k);

            float breath = Mathf.Sin(t * 1.3f) * 1.2f * (1f - moving);
            float shake = quiver > 0.01f ? Mathf.Sin(t * 22f) * 2f * quiver : 0f;   // a slow tremble, not a buzz
            float neckTotal = pitch + breath + flinch * 18f + shake;
            if (neck != null)
                for (int i = 0; i < 3; i++)
                    if (neck[i] != null)
                        neck[i].T.localRotation = neck[i].Rest * Quaternion.AngleAxis(neckSign * neckTotal / 3f, neck[i].Pitch) * Quaternion.AngleAxis(flinch * (i == 0 ? 8f : 3f), neck[i].Yaw);
            if (jaw != null)
                jaw.T.localRotation = jaw.Rest * Quaternion.AngleAxis(jawSign * Mathf.Clamp(jawDeg + (jawDeg > 5f ? shake * 0.5f : 0f), 0f, HoundRules.MaxJawDeg), jaw.Pitch);

            int side = clawCount % 2 == 1 ? 0 : 1;
            for (int i = 0; i < 2; i++)
                if (paw[i] != null)
                    paw[i].T.localRotation = paw[i].Rest * Quaternion.AngleAxis(i == side ? pawSign * pawDeg : 0f, paw[i].Pitch);

            // running: the back rolls with the stride
            if (back != null)
            {
                runPhase += speed * dt * 2.0f;
                float roll = Mathf.Sin(runPhase) * 2.5f * moving + flinch * 4f;
                back.T.localRotation = back.Rest * Quaternion.AngleAxis(neckSign * roll, back.Pitch);
            }
        }
    }
}
