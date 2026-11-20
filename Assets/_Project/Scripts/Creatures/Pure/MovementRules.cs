using System;
using System.Collections.Generic;

namespace Evolve.Creatures
{
    /// <summary>How a species gets around: how high a ledge it can step or hop up, and how far it will drop down.</summary>
    public enum MoveStyle { Walker, Hopper, Climber, Flyer }

    public readonly struct MoveProfile
    {
        public readonly MoveStyle Style;
        /// <summary>Highest ledge it gets onto (a wall taller than this blocks it).</summary>
        public readonly float MaxStepUp;
        /// <summary>Deepest drop it walks off; deeper is a ledge it turns away from.</summary>
        public readonly float MaxDrop;
        /// <summary>True when this creature avoids water (turns back at shore).</summary>
        public readonly bool AvoidsWater;
        /// <summary>True when this creature can swim in water.</summary>
        public readonly bool CanSwim;
        /// <summary>Normal.y threshold for walkable surfaces (0.7f = 45 deg, 0.88f = 28 deg avoids steep cliffs).</summary>
        public readonly float MaxClimbSlope;

        public MoveProfile(MoveStyle style, float up, float drop, bool avoidsWater = false, bool canSwim = false, float maxClimbSlope = 0.7f)
        {
            Style = style;
            MaxStepUp = up;
            MaxDrop = drop;
            AvoidsWater = avoidsWater;
            CanSwim = canSwim;
            MaxClimbSlope = maxClimbSlope;
        }
    }

    /// <summary>Pure movement rules for creatures (kept out of Unity so they can be tested).</summary>
    public static class MovementRules
    {
        public static readonly MoveProfile Walker = new MoveProfile(MoveStyle.Walker, 0.35f, 1.0f);
        public static readonly MoveProfile Hopper = new MoveProfile(MoveStyle.Hopper, 0.9f, 1.5f);
        public static readonly MoveProfile Climber = new MoveProfile(MoveStyle.Climber, 1.5f, 2.0f);
        public static readonly MoveProfile Flyer = new MoveProfile(MoveStyle.Flyer, 2.5f, 100f);

        // Custom profiles for specific species
        public static readonly MoveProfile ThanLanBoHoProfile = new MoveProfile(MoveStyle.Climber, 1.5f, 2.0f, avoidsWater: true);
        public static readonly MoveProfile ThoLaProfile = new MoveProfile(MoveStyle.Hopper, 0.9f, 1.5f, avoidsWater: true);
        public static readonly MoveProfile CocHoGiaProfile = new MoveProfile(MoveStyle.Hopper, 0.55f, 1.2f, avoidsWater: false, canSwim: true, maxClimbSlope: 0.88f);

        static readonly Dictionary<string, MoveProfile> customProfiles = new Dictionary<string, MoveProfile>
        {
            { "thanlanboho", ThanLanBoHoProfile },
            { "thola", ThoLaProfile },
            { "cochogia", CocHoGiaProfile },
        };

        // species key (SpeciesNames.Key) → style; anything not listed walks
        static readonly Dictionary<string, MoveStyle> styles = new Dictionary<string, MoveStyle>
        {
            { "domdomsacmau", MoveStyle.Flyer }, { "muoireu", MoveStyle.Flyer }, { "doiamthoa", MoveStyle.Flyer }, { "buombaotu", MoveStyle.Flyer },
            { "kenkenhang", MoveStyle.Flyer }, { "buomdem", MoveStyle.Flyer }, { "chimhangme", MoveStyle.Flyer }, { "chimnon", MoveStyle.Hopper },
            { "thola", MoveStyle.Hopper }, { "degio", MoveStyle.Hopper }, { "cochogia", MoveStyle.Hopper }, { "echdocdom", MoveStyle.Hopper }, { "chuotgio", MoveStyle.Hopper },
            { "nhentinhthe", MoveStyle.Climber }, { "tackeda", MoveStyle.Climber }, { "retmu", MoveStyle.Climber }, { "retreu", MoveStyle.Climber },
            { "thanlanboho", MoveStyle.Climber }, { "kynhonglua", MoveStyle.Climber }, { "bocapthachanh", MoveStyle.Climber },
        };

        public static MoveProfile For(string speciesOrObjectName)
        {
            string key = SpeciesNames.Key(speciesOrObjectName);
            if (customProfiles.TryGetValue(key, out var cp)) return cp;
            if (!styles.TryGetValue(key, out var s)) return Walker;
            return s switch { MoveStyle.Flyer => Flyer, MoveStyle.Hopper => Hopper, MoveStyle.Climber => Climber, _ => Walker };
        }

        /// <summary>May a creature whose feet are at <paramref name="footY"/> move onto ground at <paramref name="groundY"/>?</summary>
        public static bool CanEnter(float footY, float groundY, MoveProfile p)
        {
            float rise = groundY - footY;
            return rise <= p.MaxStepUp && -rise <= p.MaxDrop;
        }

        /// <summary>Directions to try, in degrees from the wanted one: straight, then ever wider detours.</summary>
        public static readonly float[] DetourAngles = { 0f, 40f, -40f, 80f, -80f };

        /// <summary>Eases the body's height toward the ground so a bump in the floor mesh is not a jolt: fast up, a bit slower down.</summary>
        public static float SmoothY(float current, float target, float dt)
        {
            float rate = target > current ? 18f : 10f;
            return current + (target - current) * (1f - (float)Math.Exp(-rate * dt));
        }
    }
}
