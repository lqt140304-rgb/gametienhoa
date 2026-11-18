using Evolve.Survival;

namespace Evolve.Creatures
{
    /// <summary>Anything a player's attack can hurt (prey, predators, treefolk). <paramref name="by"/> is the attacker, or null.</summary>
    public interface IDamageable
    {
        void TakeDamage(float amount, SurvivalStats by);
    }
}
