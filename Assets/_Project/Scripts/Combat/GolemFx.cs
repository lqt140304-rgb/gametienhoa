using Evolve.Abilities;
using UnityEngine;

namespace Evolve.Combat
{
    /// <summary>Sound and visual effects for Golem Thạch Anh's moves (recordings from Resources/Sfx/Golem, made with ElevenLabs by tools/voice/generate_golem_sfx.sh; code sounds as a fallback): the ground slam and its shock ring, the boulder's throw and crash, the crystal armour, the core's charge and blast.</summary>
    public static class GolemFx
    {
        static readonly Color Dust = new Color(0.58f, 0.52f, 0.45f), Crystal = new Color(0.78f, 0.7f, 0.55f), Core = new Color(1f, 0.78f, 0.35f);

        static void Sfx(string file, Vector3 p, float vol, ProceduralSfx.Kind fallback) => ProceduralSfx.PlayFile("Golem/" + file, p, vol, fallback);

        public static void OnPhase(CreatureCombat golem, MovePhase phase, MoveDef m)
        {
            if (golem == null || m == null) return;
            Vector3 p = golem.transform.position;
            float scale = Mathf.Max(0.5f, golem.transform.lossyScale.y);
            switch (m.Anim)
            {
                case "Slam":
                    if (phase == MovePhase.Windup) Sfx("golem_grunt", p, 0.8f, ProceduralSfx.Kind.HoundGrowlShort);
                    else if (phase == MovePhase.Active)
                    {
                        Sfx("golem_slam", p, 1f, ProceduralSfx.Kind.HoundThud);
                        ImpactFx.Thump(p, 0.4f, 0.9f, 28f);
                        SkillFx.Dust(p, m.Radius * 0.7f, Dust, 60);
                        SkillFx.Ring(p, m.Radius * 0.45f, Dust, 0.35f, 0.1f); SkillFx.Ring(p, m.Radius * 0.75f, Dust, 0.5f, 0.1f); SkillFx.Ring(p, m.Radius, Dust, 0.7f, 0.1f);   // the shock runs outward in three rings
                        SkillFx.Burst(p + Vector3.up * 0.2f, Crystal, 18, 5f, 0.12f, 0.8f, 0.6f);   // crystal chips jump off the floor
                    }
                    break;
                case "Punch":
                    if (phase == MovePhase.Windup) Sfx("golem_grunt", p, 0.8f, ProceduralSfx.Kind.HoundGrowlShort);
                    else if (phase == MovePhase.Active) { Sfx("golem_slam", p, 0.8f, ProceduralSfx.Kind.HoundThud); ImpactFx.Thump(p, 0.2f, 0.5f, 20f); SkillFx.Dust(p + golem.transform.forward * 1.5f * scale, 1.2f * scale, Dust, 24); }
                    break;
                case "Grab":
                    if (phase == MovePhase.Windup) Sfx("golem_grunt", p, 0.9f, ProceduralSfx.Kind.HoundGrowlShort);
                    else if (phase == MovePhase.Active) { Sfx("golem_throw", p, 0.8f, ProceduralSfx.Kind.HoundLunge); ImpactFx.Thump(p, 0.15f, 0.4f, 18f); }
                    break;
                case "Throw":
                    if (phase == MovePhase.Windup) Sfx("golem_grunt", p, 0.8f, ProceduralSfx.Kind.HoundGrowlShort);
                    else if (phase == MovePhase.Active) { Sfx("golem_throw", p, 1f, ProceduralSfx.Kind.HoundLunge); ImpactFx.Thump(p, 0.1f, 0.4f, 18f); }
                    break;
                case "Harden":
                    if (phase == MovePhase.Windup) Sfx("golem_harden", p, 0.9f, ProceduralSfx.Kind.HoundSnap);
                    else if (phase == MovePhase.Active) { SkillFx.Burst(p + Vector3.up * 1.2f * scale, Crystal, 30, 3.5f, 0.1f, 0.9f); SkillFx.Ring(p, 2.5f * scale, Crystal, 0.5f, 0.3f); ImpactFx.Thump(p, 0.12f, 0.5f, 20f); }
                    break;
                case "CoreBlast":
                    if (phase == MovePhase.Windup) Sfx("golem_core_charge", p, 1f, ProceduralSfx.Kind.HoundHowl);
                    else if (phase == MovePhase.Active)
                    {
                        Sfx("golem_core_blast", p, 1f, ProceduralSfx.Kind.HoundThud);
                        ImpactFx.Thump(p, 0.6f, 1.4f, 34f);
                        SkillFx.Ring(p, m.Radius * 0.5f, Core, 0.4f, 0.1f); SkillFx.Ring(p, m.Radius, Core, 0.8f, 0.1f);
                        SkillFx.Dust(p, m.Radius * 0.6f, Dust, 60);
                        SkillFx.Burst(p + Vector3.up * 1.3f * scale, Core, 40, 7f, 0.14f, 0.9f, 0.5f);
                    }
                    break;
            }
        }

        static float lastHurt = -10f;
        public static void Hurt(Vector3 p)
        {
            if (Time.time - lastHurt < 0.5f) return;
            lastHurt = Time.time;
            Sfx("golem_hurt", p, 0.9f, ProceduralSfx.Kind.HoundHurt);
        }

        /// <summary>A boulder crashes (it hit, or landed).</summary>
        public static void BoulderImpact(Vector3 p)
        {
            Sfx("golem_boulder", p, 1f, ProceduralSfx.Kind.HoundThud);
            ImpactFx.Thump(p, 0.25f, 0.6f, 22f);
            SkillFx.Burst(p, Dust, 20, 5f, 0.12f, 0.8f);
            SkillFx.Dust(p, 1.5f, Dust, 20);
        }
    }
}
