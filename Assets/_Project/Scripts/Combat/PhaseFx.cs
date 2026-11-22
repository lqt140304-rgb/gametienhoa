using Evolve.Abilities;
using UnityEngine;

namespace Evolve.Combat
{
    /// <summary>The roar at a boss's phase change (<see cref="PhaseRules.TransitionMove"/>): a bellow that fits the species, rings of sound running outward, the ground shaking.</summary>
    public static class PhaseFx
    {
        static readonly Color Ring = new Color(1f, 0.82f, 0.5f);

        public static void OnPhase(CreatureCombat boss, MovePhase phase, MoveDef m)
        {
            if (boss == null || m == null) return;
            Vector3 p = boss.transform.position;
            string key = Evolve.Creatures.SpeciesNames.Key(boss.name);
            if (phase == MovePhase.Windup)
            {
                if (key == "golemthachanh") ProceduralSfx.PlayFile("Golem/golem_roar", p, 1f, ProceduralSfx.Kind.HoundRoar);
                else if (key == "chimhangme") ProceduralSfx.PlayFile("Bird/bird_stun_screech", p, 1f, ProceduralSfx.Kind.HoundRoar);
                else ProceduralSfx.PlayFile("Hound/hound_roar", p, 1f, ProceduralSfx.Kind.HoundRoar);
                ImpactFx.Thump(p, 0.03f, 1.4f, 26f);
                BossMusic.Ping(key == "golemthachanh" ? "golem_battle" : key == "chimhangme" ? "bird_battle" : "hound_battle");
            }
            else if (phase == MovePhase.Active)
            {
                ImpactFx.Thump(p, 0.45f, 1.2f, 32f);
                for (int i = 1; i <= 4; i++) SkillFx.Ring(p + Vector3.up * 0.2f, m.Radius * i / 4f, Ring, 0.4f + 0.2f * i, 0.1f);
                SkillFx.Dust(p, m.Radius * 0.7f, new Color(0.6f, 0.55f, 0.48f), 50);
            }
        }
    }
}
