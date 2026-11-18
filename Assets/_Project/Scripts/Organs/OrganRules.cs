namespace Evolve.Organs
{
    /// <summary>Pure rules for reshaping the body with organ blueprints. Numbers are proposals.</summary>
    public static class OrganRules
    {
        /// <summary>Evolution EXP to simulate a known blueprint again (same as absorbing a copy of that tier).</summary>
        public static float ResimulateCost(OrganTier tier) => OrganTiers.AbsorbExp(tier);
    }
}
