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

        public MoveProfile(MoveStyle style, float up, float drop) { Style = style; MaxStepUp = up; MaxDrop = drop; }
    }

    /// <summary>Pure movement rules for creatures (kept out of Unity so they can be tested).</summary>
    public static class MovementRules
    {
        public static readonly MoveProfile Walker = new MoveProfile(MoveStyle.Walker, 0.35f, 1.0f);
        public static readonly MoveProfile Hopper = new MoveProfile(MoveStyle.Hopper, 0.9f, 1.5f);
        public static readonly MoveProfile Climber = new MoveProfile(MoveStyle.Climber, 1.5f, 2.0f);
        public static readonly MoveProfile Flyer = new MoveProfile(MoveStyle.Flyer, 2.5f, 100f);

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
            if (!styles.TryGetValue(SpeciesNames.Key(speciesOrObjectName), out var s)) return Walker;
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
