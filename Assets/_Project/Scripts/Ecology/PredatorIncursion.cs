using System.Collections.Generic;
using Evolve.Survival;
using UnityEngine;

namespace Evolve.Ecology
{
    /// <summary>Lets one tier-B predator walk into a region from its edge, never near a living player. Host-only.</summary>
    public class PredatorIncursion : MonoBehaviour
    {
        [SerializeField] Region region;
        [SerializeField] SpeciesSO predator;
        [SerializeField] Transform[] entries;
        [SerializeField] int preyThreshold = 4;
        [SerializeField] float patrolEveryMinutes = 4f;
        [SerializeField] float noSpawnRadius = 40f;
        [SerializeField] float checkSeconds = 10f;

        readonly List<Vector3> playerPositions = new List<Vector3>();
        readonly List<Vector3> entryPositions = new List<Vector3>();
        EmbodiedCreature current;
        float timer;
        float minutesSinceLastHunt;

        void Update()
        {
            if (NetWorld.IsClient || region == null || predator == null || entries == null) return;
            minutesSinceLastHunt += Time.deltaTime / 60f;
            timer += Time.deltaTime;
            if (timer < checkSeconds) return;
            timer -= checkSeconds;
            Check();
        }

        void Check()
        {
            if (!region.IsActive)
            {
                // Region.StoreAll stores the tracked B (it is registered via TryEmbodyAt).
                return;
            }
            if (current != null) return; // at most one B embodied at a time
            var pop = region.Population;
            if (pop == null) return;
            var predPop = pop.Get(predator.Id);
            if (predPop == null) return;

            int prey = 0;
            if (predator.Prey != null)
            {
                var preyPop = pop.Get(predator.Prey.Id);
                if (preyPop != null) prey = preyPop.Count;
            }
            if (!IncursionRule.ShouldEnter(predPop, prey, preyThreshold, minutesSinceLastHunt, patrolEveryMinutes)) return;

            playerPositions.Clear();
            var all = SurvivalStats.All;
            for (int i = 0; i < all.Count; i++)
            {
                var p = all[i];
                if (p == null || p.State.IsDead) continue;
                playerPositions.Add(p.transform.position);
            }
            entryPositions.Clear();
            for (int i = 0; i < entries.Length; i++)
                entryPositions.Add(entries[i] != null ? entries[i].position : new Vector3(float.MaxValue, 0f, float.MaxValue));

            int idx = IncursionRule.PickEntry(entryPositions, playerPositions, noSpawnRadius);
            if (idx < 0 || entries[idx] == null) return;

            var ec = region.TryEmbodyAt(predator, entries[idx].position, region.transform.position);
            if (ec == null) return;
            current = ec;
            minutesSinceLastHunt = 0f;
        }
    }
}
