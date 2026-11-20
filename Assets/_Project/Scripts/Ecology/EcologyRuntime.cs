using System.Collections.Generic;
using UnityEngine;

namespace Evolve.Ecology
{
    /// <summary>
    /// The one live <see cref="EcologyWorld"/> (B1 rich model, all caves) for the running game. A cave's <see cref="Region"/>
    /// claims its <see cref="RegionPopulation"/> from here instead of building the old single-food model, and every Region
    /// step asks it to tick; the whole world ticks at most once per <see cref="TickSeconds"/>, so caves the player is far from keep living.
    /// Tunnels follow the real maze once one is loaded (<see cref="MazeEcologyLinks"/>), the placeholder chain until then.
    /// </summary>
    public static class EcologyRuntime
    {
        public const float TickSeconds = 5f;

        static EcologyWorld world;
        static readonly HashSet<string> claimed = new HashSet<string>();
        static float lastTick = -1f;
        static Evolve.Maze.MazeGraph linkedGraph;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { world = null; claimed.Clear(); lastTick = -1f; linkedGraph = null; }

        /// <summary>The start cavern is small; every other cave is a wide maze chamber, so it holds proportionally more life.</summary>
        public static float CaveScale(string caveId) => caveId == "khoi_sinh" ? 1f : Evolve.Maze.ChamberDensity.Multiplier(Evolve.Maze.MazeTerrainMath.DefaultChamberRadius);

        public static EcologyWorld World => world ??= CaveEcologySeed.Build(CaveScale, id => Evolve.Creatures.CaveTier.DangerOf(Evolve.Creatures.CaveTier.Of(id)).HunterCount);

        /// <summary>The population of <paramref name="caveId"/> for the first Region that asks; null for an unknown cave or one already claimed
        /// (a second Region of the same cave keeps the old model so the two do not fight over den counts).</summary>
        public static RegionPopulation Claim(string caveId)
        {
            var r = caveId != null ? World.Find(caveId) : null;
            if (r == null || !claimed.Add(caveId)) return null;
            return r;
        }

        public static void Release(string caveId) { if (caveId != null) claimed.Remove(caveId); }

        /// <summary>Ticks the world for the time since the last tick (called by every Region step; extra calls are free).</summary>
        public static void Tick()
        {
            float now = Time.time;
            if (lastTick < 0f) { lastTick = now; return; }
            if (now - lastTick < TickSeconds) return;
            RelinkToMaze();
            World.Tick((now - lastTick) / 60f);
            lastTick = now;
        }

        static void RelinkToMaze()
        {
            var maze = Object.FindAnyObjectByType<Evolve.Maze.MazeWorldManager>();
            if (maze == null || maze.Graph == null || maze.Graph == linkedGraph) return;
            MazeEcologyLinks.Apply(World, maze.Graph);
            linkedGraph = maze.Graph;
        }
    }
}
