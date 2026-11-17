using System.Collections.Generic;
using UnityEngine;

namespace Evolve.Ecology
{
    /// <summary>Pure rules for when and where a tier-B predator enters a region.</summary>
    public static class IncursionRule
    {
        public static bool ShouldEnter(SpeciesPopulation predator, int preyCountInTerritory, int threshold,
            float minutesSinceLastHunt, float patrolEveryMinutes)
        {
            return predator != null && predator.Available > 0
                && (preyCountInTerritory <= threshold || minutesSinceLastHunt >= patrolEveryMinutes);
        }

        /// <summary>Index of the entry farthest from players among entries at least noSpawnRadius from every player; -1 if none.</summary>
        public static int PickEntry(IReadOnlyList<Vector3> entries, IReadOnlyList<Vector3> players, float noSpawnRadius)
        {
            int best = -1;
            float bestDist = -1f;
            float r2 = noSpawnRadius * noSpawnRadius;
            for (int i = 0; i < entries.Count; i++)
            {
                float nearest = float.MaxValue;
                for (int p = 0; p < players.Count; p++)
                {
                    float dx = entries[i].x - players[p].x, dz = entries[i].z - players[p].z;
                    float d2 = dx * dx + dz * dz;
                    if (d2 < nearest) nearest = d2;
                }
                if (nearest < r2) continue;
                if (nearest > bestDist) { bestDist = nearest; best = i; }
            }
            return best;
        }
    }
}
