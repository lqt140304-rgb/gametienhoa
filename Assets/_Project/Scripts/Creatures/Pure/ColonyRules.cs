using System;

namespace Evolve.Creatures
{
    /// <summary>What a member of a crowded nest does: forage in pairs, scout the edge, or keep (stay home and watch for danger).</summary>
    public enum ColonyRole { Forager, Scout, Keeper }

    /// <summary>Pure rules for splitting a nest's members into jobs and picking where they go.</summary>
    public static class ColonyRules
    {
        /// <summary>Below this many, a group is too small for jobs: everyone just forages.</summary>
        public const int MinColony = 5;

        /// <summary>Role of the <paramref name="index"/>-th member (stable order, e.g. by net id) in a nest of <paramref name="count"/>:
        /// 2 foraging pairs (3 from 10 members), 1 scout (2 from 8), the rest keepers.</summary>
        public static ColonyRole RoleOf(int index, int count)
        {
            if (count < MinColony) return ColonyRole.Forager;
            int foragers = (count >= 10 ? 3 : 2) * 2;
            int scouts = count >= 8 ? 2 : 1;
            if (index < foragers) return ColonyRole.Forager;
            if (index < foragers + scouts) return ColonyRole.Scout;
            return ColonyRole.Keeper;
        }

        /// <summary>Foragers go out two by two: members 0 and 1 are pair 0, and so on.</summary>
        public static int PairOf(int index) => index / 2;

        /// <summary>Angle (radians) a foraging pair heads out at: the same for both partners, a new one every <paramref name="slotSeconds"/>.</summary>
        public static float ForageAngle(int pair, float time, float slotSeconds = 20f)
        {
            int slot = (int)Math.Floor(time / slotSeconds);
            uint h = (uint)(pair * 73856093) ^ (uint)(slot * 19349663);
            h ^= h >> 13; h *= 0x5bd1e995; h ^= h >> 15;
            return (h % 3600) / 3600f * 2f * (float)Math.PI;
        }

        /// <summary>Angle (radians) a scout has reached on its loop round the nest: one lap a minute, scouts spread evenly.</summary>
        public static float ScoutAngle(int scout, int scouts, float time) =>
            (float)((time / 60f + (double)scout / Math.Max(1, scouts)) % 1.0 * 2.0 * Math.PI);
    }
}
