using Evolve.Abilities;
using UnityEngine;

namespace Evolve.Combat
{
    /// <summary>Sound and effects for the bird's two close moves that <see cref="BirdMoves"/> does not run: Vả Cánh (a wing swept across: a whoosh, a gust ring) and the ultimate Rít Gầm (a shriek: three teal shock rings, the ground shakes). Recordings from Resources/Sfx/Bird.</summary>
    public static class BirdFx
    {
        static readonly Color Wind = new Color(0.45f, 0.85f, 0.85f), Dust = new Color(0.62f, 0.56f, 0.48f);

        static void Sfx(string file, Vector3 p, float vol, ProceduralSfx.Kind fallback) => ProceduralSfx.PlayFile("Bird/" + file, p, vol, fallback);

        public static void OnPhase(CreatureCombat bird, MovePhase phase, MoveDef m)
        {
            if (bird == null || m == null) return;
            Vector3 p = bird.transform.position;
            switch (m.Anim)
            {
                case "WingSlap":
                    if (phase == MovePhase.Windup) Sfx("bird_flap", p, 0.8f, ProceduralSfx.Kind.HoundSwipe);
                    else if (phase == MovePhase.Active)
                    {
                        Sfx("bird_wing_slap", p, 1f, ProceduralSfx.Kind.HoundSwipe);
                        SkillFx.Ring(p + Vector3.up * 0.3f, m.Radius, Wind, 0.4f, 0.2f);
                        SkillFx.Dust(p, m.Radius * 0.6f, Dust, 22);
                    }
                    break;
                case "Screech":
                    if (phase == MovePhase.Windup) { Sfx("bird_screech", p, 1f, ProceduralSfx.Kind.HoundHowl); ImpactFx.Thump(p, 0.03f, 1.5f, 24f); }
                    else if (phase == MovePhase.Active)
                    {
                        Sfx("bird_stun_screech", p, 1f, ProceduralSfx.Kind.HoundRoar);
                        ImpactFx.Thump(p, 0.35f, 1.2f, 30f);
                        for (int i = 1; i <= 3; i++) SkillFx.Ring(p + Vector3.up * 0.4f, m.Radius * i / 3f, Wind, 0.4f + 0.2f * i, 0.1f);   // the shriek runs out as three rings
                        SkillFx.Dust(p, m.Radius * 0.7f, Dust, 60);
                    }
                    break;
            }
        }
    }
}
