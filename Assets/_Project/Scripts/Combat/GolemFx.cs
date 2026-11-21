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
                case "Meteor": break;   // its sounds come from Point (the aim) and Leap (the spring), at the right moments
                case "Sweep":
                    if (phase == MovePhase.Windup) Sfx("golem_grunt", p, 0.7f, ProceduralSfx.Kind.HoundGrowlShort);
                    else if (phase == MovePhase.Active) { Sfx("golem_sweep", p, 1f, ProceduralSfx.Kind.HoundSwipe); ImpactFx.Thump(p, 0.18f, 0.4f, 18f); SkillFx.Dust(p, m.Radius * 0.6f, Dust, 28); SkillFx.Ring(p, m.Radius, Dust, 0.4f, 0.2f); }
                    break;
                case "Rush":
                    if (phase == MovePhase.Windup) { Sfx("golem_rush_charge", p, 1f, ProceduralSfx.Kind.HoundHowl); ImpactFx.Thump(p, 0.05f, 0.5f, 20f); }
                    else if (phase == MovePhase.Active) { Sfx("golem_rush_run", p, 1f, ProceduralSfx.Kind.HoundLunge); SkillFx.Dust(p, 2.5f * scale, Dust, 40); }
                    break;
                case "Punch":
                    if (phase == MovePhase.Windup) Sfx("golem_punch_wind", p, 0.9f, ProceduralSfx.Kind.HoundGrowlShort);
                    else if (phase == MovePhase.Active) { Sfx("golem_punch_hit", p, 1f, ProceduralSfx.Kind.HoundThud); ImpactFx.Thump(p, 0.2f, 0.5f, 20f); SkillFx.Dust(p + golem.transform.forward * 1.5f * scale, 1.2f * scale, Dust, 24); }
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

        /// <summary>The roar after the landing: a huge bellow and a sound wave that runs out as rings and shakes the ground.</summary>
        public static void Roar(Vector3 at, float radius)
        {
            Sfx("golem_roar", at, 1f, ProceduralSfx.Kind.HoundRoar);
            ImpactFx.Thump(at, 0.45f, 1.8f, 36f);
            for (int i = 1; i <= 4; i++) SkillFx.Ring(at + Vector3.up * 0.15f, radius * i / 4f, new Color(0.9f, 0.85f, 0.7f), 0.5f + 0.2f * i, 0.1f);   // the sound wave: rings going out one after another
            SkillFx.Dust(at, radius * 0.7f, Dust, 80, radius * 0.2f);
        }

        /// <summary>It sinks to its knees and gasps for breath after a move.</summary>
        public static void Pant(Vector3 at) => Sfx("golem_pant", at, 0.9f, ProceduralSfx.Kind.HoundGrowlShort);

        /// <summary>It stretches an arm toward the player: the aim before it leaps.</summary>
        public static void Point(Vector3 at) => Sfx("golem_point", at, 1f, ProceduralSfx.Kind.HoundGrowlShort);

        /// <summary>It springs off the ground.</summary>
        public static void Leap(Vector3 at) { Sfx("golem_leap", at, 1f, ProceduralSfx.Kind.HoundLunge); ImpactFx.Thump(at, 0.2f, 0.5f, 22f); SkillFx.Dust(at, 3f, Dust, 40); }

        /// <summary>It charges head first into a wall: a crack, the ground shakes, rock chips fly.</summary>
        public static void WallBump(Vector3 at)
        {
            Sfx("golem_rush_crash", at, 1f, ProceduralSfx.Kind.HoundThud);
            ImpactFx.Thump(at, 0.55f, 1.1f, 34f);
            SkillFx.Dust(at, 3f, Dust, 60);
            SkillFx.Burst(at + Vector3.up * 1.2f, Crystal, 26, 6f, 0.12f, 0.9f, 0.6f);
        }

        /// <summary>It crashes down on the marked circle.</summary>
        public static void Land(Vector3 spot, float radius)
        {
            Sfx("golem_land", spot, 1f, ProceduralSfx.Kind.HoundThud);
            ImpactFx.Thump(spot, 0.5f, 1.0f, 32f);
            SkillFx.Dust(spot, radius * 0.8f, Dust, 70);
            SkillFx.Ring(spot, radius * 0.5f, Dust, 0.35f, 0.1f); SkillFx.Ring(spot, radius, Dust, 0.6f, 0.1f);
            SkillFx.Burst(spot + Vector3.up * 0.2f, Crystal, 20, 6f, 0.12f, 0.9f, 0.6f);
        }

        /// <summary>The stone spikes burst out of the ground round the landing.</summary>
        public static void SpikesRise(Vector3 spot, float radius)
        {
            Sfx("golem_spikes", spot, 1f, ProceduralSfx.Kind.HoundSnap);
            ImpactFx.Thump(spot, 0.25f, 0.6f, 26f);
            SkillFx.Dust(spot, radius, Dust, 50, radius * 0.7f);
        }

        /// <summary>It wrenches a spike out of the ground.</summary>
        public static void Pull(Vector3 at)
        {
            Sfx("golem_strain", at, 0.9f, ProceduralSfx.Kind.HoundGrowlShort);
            SkillFx.Burst(at, Dust, 12, 3f, 0.1f, 0.6f);
        }

        /// <summary>It hurls the rock it holds.</summary>
        public static void Hurl(Vector3 at)
        {
            Sfx("golem_hurl", at, 1f, ProceduralSfx.Kind.HoundLunge);
            ImpactFx.Thump(at, 0.1f, 0.3f, 18f);
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
