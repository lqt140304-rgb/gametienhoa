namespace Evolve.Organs
{
    /// <summary>Pure rules for reshaping the body with organ blueprints. Numbers are proposals.</summary>
    public static class OrganRules
    {
        /// <summary>Attach / replace / detach is allowed anywhere when the world has no shelter, otherwise only inside one. Never while dead.</summary>
        public static bool CanReshape(bool worldHasShelter, bool inShelter, bool dead)
            => !dead && (!worldHasShelter || inShelter);

        /// <summary>Evolution EXP to simulate a known blueprint again (same as absorbing a copy of that tier).</summary>
        public static float ResimulateCost(OrganTier tier) => OrganTiers.AbsorbExp(tier);

        public const string ReshapeBlockedMessage = "Chỉ đổi cơ thể được khi ở nơi trú.";
    }
}
