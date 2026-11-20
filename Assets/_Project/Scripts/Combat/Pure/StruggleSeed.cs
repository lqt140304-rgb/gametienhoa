using Evolve.Creatures;

namespace Evolve.Combat
{
    /// <summary>How each kind of creature struggles in the grip mini game (spec table, provisional): weak prey, ordinary hunters, cave lords.</summary>
    public static class StruggleSeed
    {
        static readonly StruggleProfile Prey = new StruggleProfile(0.40f, 5, StruggleKind.Even, 0.35f, 0f);
        static readonly StruggleProfile Hunter = new StruggleProfile(0.28f, 4, StruggleKind.Jerky, 0.55f, 0.08f);
        static readonly StruggleProfile Lord = new StruggleProfile(0.18f, 3, StruggleKind.TwoPhase, 0.8f, 0.25f);

        public static StruggleProfile For(string speciesIdOrName)
        {
            switch (CaveTier.RoleOf(speciesIdOrName))
            {
                case CreatureRole.Lord: return Lord;
                case CreatureRole.Hunter: return Hunter;
                default: return Prey;
            }
        }
    }
}
