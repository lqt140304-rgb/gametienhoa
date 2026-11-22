using Evolve.Creatures;

namespace Evolve.Combat
{
    /// <summary>What a phase changes about a boss, as multipliers on how it was (phase 1 is exactly that).</summary>
    public struct PhaseMods
    {
        /// <summary>Speed, cooldowns, the idle gap between moves (it circles less), damage: multipliers.</summary>
        public float Speed, Cooldown, Idle, Damage;
        /// <summary>Extra stone spikes for the Golem's Giáng Thạch; extra blows at the end of a combo chain.</summary>
        public int ExtraSpikes, ComboBonus;
        public static PhaseMods None => new PhaseMods { Speed = 1f, Cooldown = 1f, Idle = 1f, Damage = 1f };
    }

    /// <summary>
    /// The phases of a boss fight: below 66% health and below 33% the boss gets harder and different (shorter cooldowns, less circling, more damage and speed, longer combos, more spikes),
    /// and at each change it stops and roars (<see cref="TransitionMove"/>) during which it cannot be hurt, so the change is felt. The numbers are provisional.
    /// </summary>
    public static class PhaseRules
    {
        public const float Phase2Below = 0.66f, Phase3Below = 0.33f;

        static string Key(string species) => SpeciesNames.Key(species ?? "");
        public static bool Has(string species) { string k = Key(species); return k == "golemthachanh" || k == "chimhangme" || k == "choxuong"; }

        public static int PhaseOf(float health01) => health01 <= Phase3Below + 1e-4f ? 3 : health01 <= Phase2Below + 1e-4f ? 2 : 1;

        public static PhaseMods For(string species, int phase)
        {
            var m = PhaseMods.None;
            if (phase <= 1) return m;
            switch (Key(species))
            {
                case "golemthachanh":
                    return phase == 2 ? new PhaseMods { Speed = 1f, Cooldown = 0.85f, Idle = 0.85f, Damage = 1.1f, ExtraSpikes = 1, ComboBonus = 1 }
                                      : new PhaseMods { Speed = 1.25f, Cooldown = 0.7f, Idle = 0.6f, Damage = 1.2f, ExtraSpikes = 2, ComboBonus = 2 };
                case "chimhangme":
                    return phase == 2 ? new PhaseMods { Speed = 1.1f, Cooldown = 0.85f, Idle = 0.85f, Damage = 1.1f }
                                      : new PhaseMods { Speed = 1.25f, Cooldown = 0.7f, Idle = 0.6f, Damage = 1.2f, ComboBonus = 1 };
                case "choxuong":
                    return phase == 2 ? new PhaseMods { Speed = 1.05f, Cooldown = 0.85f, Idle = 0.85f, Damage = 1.1f }
                                      : new PhaseMods { Speed = 1.15f, Cooldown = 0.7f, Idle = 0.6f, Damage = 1.2f };
                default: return m;
            }
        }

        /// <summary>The roar at a phase change: a wide shove, no real damage, and the boss cannot be hurt while it lasts. Null for species without phases.</summary>
        public static MoveDef TransitionMove(string species) => !Has(species) ? null : new MoveDef
        {
            Name = "Gầm Chuyển Pha", Shape = MoveShape.Spin, Windup = 1.2f, Active = 0.3f, Recover = 1f, Cooldown = 0f, MinRange = 0f, MaxRange = 99f,
            Radius = 8f, Damage = 6f, Force = 9f, Freeze = 0.5f, Armored = true, Invulnerable = true, Anim = "PhaseRoar",
        };
    }

    /// <summary>Follows a boss's health through its phases: a phase is announced once when first reached and never left again (mending does not undo it).</summary>
    public sealed class PhaseTracker
    {
        public int Phase { get; private set; } = 1;
        /// <summary>A new phase was reached and its roar has not yet been done.</summary>
        public bool Pending { get; private set; }

        /// <summary>True when this call entered a new phase.</summary>
        public bool Update(float health01)
        {
            int p = PhaseRules.PhaseOf(health01);
            if (p <= Phase) return false;
            Phase = p; Pending = true; return true;
        }

        public void Ack() => Pending = false;
    }
}
