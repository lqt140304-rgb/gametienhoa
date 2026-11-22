using Evolve.Abilities;
using UnityEngine;

namespace Evolve.Combat
{
    /// <summary>
    /// The first and the last of the three stages of a boss's moves (<see cref="VfxRules"/>): during the tell, dust and sparks gather and grow, particles orbit the body for the big moves and rings
    /// close in on the ground; during the recover, embers rise and dust settles over the spot of the hit and a fading mark is left on the ground. The impact in the middle is drawn by the species' own
    /// FX (GolemFx, BirdFx, HoundFx). Runs on every kitted boss.
    /// </summary>
    [DisallowMultipleComponent]
    public class MoveVfx : MonoBehaviour
    {
        static readonly Color[] Tints = { new Color(0.62f, 0.56f, 0.48f), new Color(1f, 0.7f, 0.3f), new Color(0.45f, 0.85f, 0.85f), new Color(0.9f, 0.86f, 0.74f) };   // Dust, Ember, Wind, Bone
        static readonly Color Scorch = new Color(0.12f, 0.1f, 0.09f);

        CreatureCombat combat;
        MoveDef lastMove; MovePhase lastPhase;
        VfxPlan plan;
        float puffs, stage, embersTimer;
        int ringsDone, embersDone;
        Vector3 impactAt;

        void Awake() => combat = GetComponent<CreatureCombat>();

        Color Tint => Tints[(int)plan.Tint];
        float BodyScale => Mathf.Max(0.5f, transform.lossyScale.y);

        void Update()
        {
            if (combat == null) return;
            var m = combat.CurrentMove; var ph = combat.Phase;
            if (!ReferenceEquals(m, lastMove) || ph != lastPhase) { Enter(m, ph); lastMove = m; lastPhase = ph; }
            if (m == null || ph == MovePhase.Idle) return;
            stage += Time.deltaTime;
            if (ph == MovePhase.Windup) Charge(m, combat.PhaseProgress);
            else if (ph == MovePhase.Recover) Aftermath(m);
        }

        void Enter(MoveDef m, MovePhase ph)
        {
            stage = 0f; puffs = 0f; embersTimer = 0f;
            if (m == null || ph == MovePhase.Idle) return;
            if (ph == MovePhase.Windup)
            {
                plan = VfxRules.PlanFor(name, m); ringsDone = 0;
                if (plan.OrbitCount > 0) SkillFx.Orbit(transform, Tint, plan.OrbitCount, Mathf.Max(0.5f, m.Windup));   // the big moves: energy gathers round the body for the whole tell
            }
            else if (ph == MovePhase.Active) impactAt = transform.position;
            else if (ph == MovePhase.Recover)
            {
                embersDone = 0;
                if (plan.GroundMark) SkillFx.Ring(impactAt + Vector3.up * 0.05f, Mathf.Max(m.Radius, 1.5f * BodyScale) * 0.8f, Scorch, plan.AfterSeconds, 0.95f);   // a dark mark that fades slowly where it hit
            }
        }

        void Charge(MoveDef m, float progress)
        {
            float k = VfxRules.ChargeIntensity(progress), s = BodyScale;
            puffs += plan.ChargePuffsPerSecond * k * Time.deltaTime;
            while (puffs >= 1f)
            {
                puffs -= 1f;
                Vector3 at = transform.position + transform.forward * (0.6f + 0.6f * k) * s + Random.insideUnitSphere * 0.4f * s;
                at.y = transform.position.y + 0.15f;
                SkillFx.Dust(at, 0.5f * s, Tints[0], 3);
                SkillFx.Burst(at + Vector3.up * 0.5f * s, Tint, 2, 1.2f + 2f * k, 0.06f + 0.06f * k, 0.5f, 0.9f);   // sparks that rise faster as the charge builds
            }
            float radius = Mathf.Max(m.Radius, 2f * s);
            while (ringsDone < plan.ChargeRings && progress >= (ringsDone + 1f) / (plan.ChargeRings + 1f))
            {
                ringsDone++;
                SkillFx.Ring(transform.position + Vector3.up * 0.08f, radius * (0.35f + 0.65f * progress), Tint, 0.35f, 0.6f);   // warning rings, each wider than the last
            }
        }

        void Aftermath(MoveDef m)
        {
            float length = plan.AfterSeconds;
            if (embersDone >= plan.AfterEmbers || stage > length) return;
            embersTimer += Time.deltaTime;
            if (embersTimer < length / Mathf.Max(1, plan.AfterEmbers)) return;
            embersTimer = 0f; embersDone++;
            float k = VfxRules.AfterIntensity(stage, length), s = BodyScale;
            Vector3 at = impactAt + new Vector3(Random.Range(-1f, 1f), 0.1f, Random.Range(-1f, 1f)) * (0.8f + Mathf.Max(0f, m.Radius) * 0.25f);
            SkillFx.Burst(at, Tint, Mathf.RoundToInt(4 + 6 * k), 1.2f, 0.09f, 1.4f, 0.9f);   // embers that rise slowly and go out
            SkillFx.Dust(at, 0.9f * s, Tints[0], Mathf.RoundToInt(3 + 5 * k));
        }
    }
}
