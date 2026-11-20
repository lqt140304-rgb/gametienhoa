namespace Evolve.Creatures
{
    /// <summary>How a hunter treats the slime, by size (spec 5.1 point 5: the slime climbs the food chain).</summary>
    public enum SlimeAttitude { Hunt, Ignore, Avoid }

    /// <summary>What a hunter does this moment.</summary>
    public enum HunterMode { Wander, ChasePlayer, ChasePrey, ToCarcass, Guard, ChaseIntruder, AvoidPlayer, AvoidRival, FightRival }

    /// <summary>Everything a hunter needs to choose (filled by CreatureBrain twice a second).</summary>
    public struct HunterInput
    {
        /// <summary>0 = fed, 1 = starving.</summary>
        public float Hunger;
        public bool PlayerSeen;
        public float PlayerDist;
        public SlimeAttitude Attitude;
        public bool HasPrey;
        public float PreyDist;
        public bool Scavenger, HasCarcass;
        public float CarcassDist;
        public bool Guardian, HasGuardedCarcass, IntruderNear;
        public float GuardedCarcassDist;
        public bool HasRival;
        public float RivalDist;
        /// <summary>The other hunter's mass over mine.</summary>
        public float RivalRatio;
    }

    /// <summary>
    /// The decisions of cave creatures in the scene (core brief B, points 4-6) as pure functions, so they can be tested without Unity.
    /// All numbers are provisional.
    /// </summary>
    public static class CreatureBehaviorRules
    {
        /// <summary>A slime up to 1.2 times a hunter's mass is prey; from 1.8 times it is avoided; between, ignored.</summary>
        public const float HuntUpToRatio = 1.2f, AvoidFromRatio = 1.8f;
        /// <summary>A prey fears a slime that could swallow it whole (the swallow rule: the prey is at most 80% of the slime's mass).</summary>
        public const float SwallowShare = 0.8f;

        public const float AvoidRange = 8f, PreySense = 12f, HungerToHunt = 0.4f, HungerToScavenge = 0.25f;
        public const float RivalRange = 6f, RivalAvoidRatio = 1.3f, RivalFightRatio = 1.0f, RivalFightHunger = 0.5f;
        public const float GuardHome = 3f, ScavengeRange = 20f;

        public static SlimeAttitude HunterToSlime(float slimeMass, float myMass)
        {
            if (myMass <= 0f) return SlimeAttitude.Hunt;
            float ratio = slimeMass / myMass;
            return ratio <= HuntUpToRatio ? SlimeAttitude.Hunt : ratio >= AvoidFromRatio ? SlimeAttitude.Avoid : SlimeAttitude.Ignore;
        }

        /// <summary>True when the slime could swallow this prey: then it runs; a prey too big to swallow ignores the slime.</summary>
        public static bool PreyFearsSlime(float slimeMass, float preyMass) => preyMass <= SwallowShare * slimeMass;

        public static HunterMode DecideHunter(HunterInput i)
        {
            if (i.PlayerSeen && i.Attitude == SlimeAttitude.Avoid && i.PlayerDist <= AvoidRange) return HunterMode.AvoidPlayer;

            if (i.Guardian && i.HasGuardedCarcass)
            {
                if (i.IntruderNear) return HunterMode.ChaseIntruder;
                return i.GuardedCarcassDist > GuardHome ? HunterMode.ToCarcass : HunterMode.Guard;
            }

            if (i.HasRival && i.RivalDist <= RivalRange)
            {
                if (i.RivalRatio >= RivalAvoidRatio) return HunterMode.AvoidRival;
                if (i.Hunger >= RivalFightHunger && i.RivalRatio <= RivalFightRatio) return HunterMode.FightRival;
            }

            bool wantsPrey = i.Hunger >= HungerToHunt && i.HasPrey && i.PreyDist <= PreySense;
            bool wantsPlayer = i.PlayerSeen && i.Attitude == SlimeAttitude.Hunt;
            if (wantsPlayer && (!wantsPrey || i.PlayerDist <= i.PreyDist)) return HunterMode.ChasePlayer;
            if (wantsPrey) return HunterMode.ChasePrey;

            if (i.Scavenger && i.HasCarcass && i.Hunger >= HungerToScavenge && i.CarcassDist <= ScavengeRange) return HunterMode.ToCarcass;
            return HunterMode.Wander;
        }

        /// <summary>A prey scavenger (Giòi Trắng, Kền Kền Hang) heads for a carcass when none of its threats is near.</summary>
        public static bool PreyScavenges(bool hasCarcass, float carcassDist, bool threatNear) => hasCarcass && !threatNear && carcassDist <= ScavengeRange;
    }
}
