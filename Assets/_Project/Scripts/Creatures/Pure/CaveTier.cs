using System.Collections.Generic;
using Evolve.Ecology;

namespace Evolve.Creatures
{
    public enum CreatureRole { Prey, Hunter, Lord }

    /// <summary>How dangerous a tier's hunters are and how a tier's lord behaves (spec 2026-11-20, provisional numbers).</summary>
    public readonly struct Danger
    {
        public readonly float Damage, Detect, HunterCount, LordSpeed;
        public readonly bool LordGentle;
        public Danger(float damage, float detect, float hunterCount, float lordSpeed, bool lordGentle)
        { Damage = damage; Detect = detect; HunterCount = hunterCount; LordSpeed = lordSpeed; LordGentle = lordGentle; }
    }

    /// <summary>
    /// Depth tiers of the caves (0 start cavern, 1 shallow, 2 middle, 3 deep) and what they change: hunter damage, detection and number,
    /// the lord's temper, and how big each role is drawn. Pure; the scene reads it when a creature enters a cave.
    /// </summary>
    public static class CaveTier
    {
        public const float ProvokeSeconds = 30f, SlimeMaxWidth = 2f, LordMinWidth = 3f;

        /// <summary>Menu-god tweaks: &gt;= 0 forces every cave to that tier; <see cref="SizeScale"/> multiplies every size factor.</summary>
        public static int TierOverride = -1;
        public static float SizeScale = 1f;

        static readonly Dictionary<string, int> tiers = new Dictionary<string, int>
        {
            { "khoi_sinh", 0 },
            { "vuon_quang", 1 }, { "reu_tho", 1 }, { "to_bo_hoang", 1 }, { "vuon_da", 1 },
            { "vuc_nut_tim", 2 }, { "nam_huynh_quang", 2 }, { "suoi_nuoc_nong", 2 }, { "khe_gio_hu", 2 },
            { "nghia_dia", 3 }, { "hoa_thach", 3 }, { "tieng_tho", 3 },
        };

        static readonly Danger[] dangers =
        {
            new Danger(0.6f, 0.6f, 0.5f, 1f, true),
            new Danger(0.8f, 0.8f, 1f, 0.85f, false),
            new Danger(1f, 1f, 1f, 1f, false),
            new Danger(1.4f, 1.4f, 1.5f, 1.15f, false),
        };

        static readonly float[] hunterSize = { 1.2f, 1.3f, 1.4f, 1.5f }, lordSize = { 2.4f, 2.6f, 2.9f, 3.2f };

        /// <summary>The tier of a cave (an unknown or empty cave counts as the middle tier).</summary>
        public static int Of(string caveId)
        {
            if (TierOverride >= 0 && TierOverride <= 3) return TierOverride;
            return caveId != null && tiers.TryGetValue(caveId, out int t) ? t : 2;
        }

        public static Danger DangerOf(int tier) => dangers[tier < 0 ? 0 : tier > 3 ? 3 : tier];

        public static float SizeFactor(CreatureRole role, int tier)
        {
            tier = tier < 0 ? 0 : tier > 3 ? 3 : tier;
            float f = role == CreatureRole.Lord ? lordSize[tier] : role == CreatureRole.Hunter ? hunterSize[tier] : 0.85f;
            return f * SizeScale;
        }

        /// <summary>Prey, hunter or cave lord, from the ecology seed (hunters have 3 slots and a floor of one, lords one slot). Unknown = prey.</summary>
        public static CreatureRole RoleOf(string speciesIdOrName)
        {
            string key = speciesIdOrName ?? "";
            if (roles.TryGetValue(key, out var cached)) return cached;
            return roles[key] = FindRole(speciesIdOrName);
        }

        static readonly Dictionary<string, CreatureRole> roles = new Dictionary<string, CreatureRole>();   // the seed is built once per name, not per spawn

        static CreatureRole FindRole(string speciesIdOrName)
        {
            string id = CaveEcologySeed.IdByName(speciesIdOrName) ?? speciesIdOrName;
            string cave = CaveEcologySeed.CaveOfSpecies(id);
            if (cave == null) return CreatureRole.Prey;
            var s = CaveEcologySeed.Build().Find(cave).Get(id);
            return s == null || s.Floor < 1 ? CreatureRole.Prey : s.Capacity <= 1 ? CreatureRole.Lord : CreatureRole.Hunter;
        }

        /// <summary>Is the point inside a flat circle (height ignored, edge inclusive)? Used by <see cref="SafeZone"/>.</summary>
        public static bool InZone(float px, float pz, float cx, float cz, float radius)
        {
            float dx = px - cx, dz = pz - cz;
            return dx * dx + dz * dz <= radius * radius;
        }

        /// <summary>A gentle lord stays calm until a player hurts it, then fights for <see cref="ProvokeSeconds"/> after the last hit.</summary>
        public static bool IsCalm(bool gentle, float provokedUntil, float now) => gentle && now >= provokedUntil;
    }
}
