using System.Collections.Generic;

namespace Evolve.Combat
{
    /// <summary>
    /// The push-away round the golem while it strains to pull its axe: whoever comes within <see cref="Radius"/> is thrown back (<see cref="Force"/>), takes <see cref="Damage"/> and is stunned
    /// <see cref="Freeze"/> seconds, but each player at most once per <see cref="Cooldown"/>. Keeps the player to ranged attacks during the pull. Pure.
    /// </summary>
    public sealed class PushAura
    {
        public const float Radius = 4f, Cooldown = 1.5f, Damage = 6f, Force = 10f, Freeze = 1f;

        readonly Dictionary<int, float> lastPush = new Dictionary<int, float>();

        /// <summary>True when this player is to be pushed now (inside the radius and past their own cooldown); the push is then recorded.</summary>
        public bool TryPush(int playerId, float distance, float now)
        {
            if (distance > Radius) return false;
            if (lastPush.TryGetValue(playerId, out float last) && now - last < Cooldown - 1e-4f) return false;
            lastPush[playerId] = now;
            return true;
        }
    }
}
