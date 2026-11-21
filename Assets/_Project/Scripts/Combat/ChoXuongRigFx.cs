using Evolve.Creatures;
using UnityEngine;

namespace Evolve.Combat
{
    /// <summary>
    /// Body language of the Meshy Chó Xương ("ChoXuong (Meshy)"), turned from its rig bones by code (it has no animation clips): the neck chain
    /// spine.009-.011 lifts or lowers the head, the jaw bone opens (a tiny JawTip child tells which way is open), following <see cref="HoundRules"/>
    /// for the move in progress (pounce, bite, leap, roar, howl). It breathes at rest, rolls its back as it runs, and flinches when hit; a heavy hit
    /// staggers it for a moment. CreatureLocomotion keeps driving the legs, Root, Head and the tail chain. Directions are measured at start.
    /// </summary>
    public class ChoXuongRigFx : MonoBehaviour
    {
        public static bool Has(string name) => !string.IsNullOrEmpty(name) && name.Contains("(Meshy)") && SpeciesNames.Key(name) == "choxuong";

        public static ChoXuongRigFx Attach(GameObject creature) =>
            creature == null || creature.GetComponent<ChoXuongRigFx>() != null ? null : creature.AddComponent<ChoXuongRigFx>();

        Transform[] neck;
        Quaternion[] neckRest;
        Vector3 pitchAxis, yawAxis;
        Transform jaw, jawTip, back, head;
        Quaternion jawRest, backRest;
        Vector3 jawAxis, backAxis;
        float neckSign = 1f, jawSign = 1f, pitch, jawDeg, quiver, flinchT = 99f, lastHealth = -1f, runPhase, moving;
        bool flinchHeavy;
        CreatureCombat combat;
        Predator pred;
        Vector3 lastPos;

        void Start()
        {
            combat = GetComponent<CreatureCombat>();
            pred = GetComponent<Predator>();
            Transform n9 = null, n10 = null, n11 = null;
            foreach (var t in GetComponentsInChildren<Transform>(true))
                switch (t.name)
                {
                    case "spine.009": n9 = t; break;
                    case "spine.010": n10 = t; break;
                    case "spine.011": n11 = t; break;
                    case "spine.007": back = t; break;
                    case "jaw": jaw = t; break;
                    case "JawTip": jawTip = t; break;
                    case "Head": head = t; break;
                }
            lastPos = transform.position;
            if (pred != null) lastHealth = pred.Health;
            neck = new[] { n9, n10, n11 };
            neckRest = new Quaternion[3];
            for (int i = 0; i < 3; i++) if (neck[i] != null) neckRest[i] = neck[i].localRotation;
            Vector3 right = transform.right, up = transform.up;
            if (n9 != null)
            {
                pitchAxis = n9.InverseTransformDirection(right);
                yawAxis = n9.InverseTransformDirection(up);
                if (head != null)
                {
                    float y = head.position.y;
                    n9.localRotation = neckRest[0] * Quaternion.AngleAxis(10f, pitchAxis);
                    neckSign = head.position.y > y ? 1f : -1f;   // which way lifts the head
                    n9.localRotation = neckRest[0];
                }
            }
            if (back != null) { backRest = back.localRotation; backAxis = back.InverseTransformDirection(right); }
            if (jaw != null)
            {
                jawRest = jaw.localRotation;
                jawAxis = jaw.InverseTransformDirection(right);
                if (jawTip != null)
                {
                    float y = jawTip.position.y;
                    jaw.localRotation = jawRest * Quaternion.AngleAxis(10f, jawAxis);
                    jawSign = jawTip.position.y < y ? 1f : -1f;   // which way opens the mouth (the tip goes down)
                    jaw.localRotation = jawRest;
                }
            }
        }

        void LateUpdate()
        {
            float dt = Mathf.Max(Time.deltaTime, 1e-4f), t = Time.time;
            float speed = (transform.position - lastPos).magnitude / dt; lastPos = transform.position;
            moving = Mathf.MoveTowards(moving, Mathf.Clamp01(speed / 2f), dt * 5f);

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
            if (combat != null && combat.CurrentMove != null && combat.Phase != MovePhase.Idle)
                target = HoundRules.Evaluate(combat.CurrentMove.Anim, combat.Phase, combat.PhaseProgress);
            float k = 1f - Mathf.Exp(-16f * dt);
            pitch = Mathf.Lerp(pitch, target.NeckPitch, k);
            jawDeg = Mathf.Lerp(jawDeg, target.JawDeg, k);
            quiver = Mathf.Lerp(quiver, target.Quiver, k);

            float breath = Mathf.Sin(t * 1.3f) * 1.2f * (1f - moving);
            float shake = quiver > 0.01f ? Mathf.Sin(t * 60f) * 3f * quiver : 0f;
            float neckTotal = pitch + breath + flinch * 22f + shake;
            if (neck != null)
                for (int i = 0; i < 3; i++)
                    if (neck[i] != null)
                        neck[i].localRotation = neckRest[i] * Quaternion.AngleAxis(neckSign * neckTotal / 3f, pitchAxis) * Quaternion.AngleAxis(flinch * (i == 0 ? 10f : 4f), yawAxis);
            if (jaw != null)
                jaw.localRotation = jawRest * Quaternion.AngleAxis(jawSign * Mathf.Min(HoundRules.MaxJawDeg, jawDeg + (jawDeg > 5f ? shake : 0f) * 0.5f), jawAxis);

            // running: the back rolls with the stride
            if (back != null)
            {
                runPhase += speed * dt * 2.6f;
                float roll = Mathf.Sin(runPhase) * 3.5f * moving + flinch * 6f;
                back.localRotation = backRest * Quaternion.AngleAxis(neckSign * roll, backAxis);
            }
        }
    }
}
