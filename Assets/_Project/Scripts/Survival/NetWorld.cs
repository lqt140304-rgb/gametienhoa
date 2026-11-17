using System;
using UnityEngine;

namespace Evolve.Survival
{
    /// <summary>
    /// Hooks between the game world and the co-op layer (assembly Evolve.Net). In single player every hook is null
    /// and IsClient is false, so the world behaves exactly as before.
    /// The host runs the world (creatures, food, drops). A client only shows replicas of the host's world and sends
    /// the host what its player did (hits, picked-up drops).
    /// </summary>
    public static class NetWorld
    {
        static int lastId;
        /// <summary>Id for a world entity (creature, pickup). Ids are handed out by the host and used by clients.</summary>
        public static int NextId() => ++lastId;

        /// <summary>True while this machine is a client: creatures and pickups here are replicas of the host's.</summary>
        public static bool IsClient;

        // client -> host
        /// <summary>A hit on a creature (predator or prey) that lives on the host: its id and the damage.</summary>
        public static Action<int, float> ReportCreatureHit;
        public static Action<int> ReportPickupTaken;
        public static Action<string, Vector3> RequestPickupSpawn;

        // host -> one remote player
        public static Action<SurvivalStats, float, Vector3> BiteRemote;
        public static Action<SurvivalStats, float> FeedRemote;

        /// <summary>Give food to a player: directly for a local player, through the network for a remote one.</summary>
        public static void Feed(SurvivalStats stats, float amount)
        {
            if (stats == null || amount <= 0f) return;
            if (stats.IsRemote) FeedRemote?.Invoke(stats, amount);
            else stats.State.Feed(amount);
        }
    }
}
