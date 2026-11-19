using Evolve.Ecology;

namespace Evolve.Swallow
{
    /// <summary>Nuốt Chửng rules that do not need the engine.</summary>
    public static class SwallowRules
    {
        /// <summary>A Fresh carcass counts as freshly killed for this long, then as an old carcass.</summary>
        public const float FreshMinutes = 1.5f;

        /// <summary>Sample quality of a carcass: just killed ×1, old ×0.5, rotten ×0.2, humus or gone 0.</summary>
        public static float CarcassQuality(CarcassStage stage, float ageMinutes) => stage switch
        {
            CarcassStage.Fresh => ageMinutes < FreshMinutes ? AnalysisRules.Fresh : AnalysisRules.Old,
            CarcassStage.Rotten => AnalysisRules.Rotten,
            _ => 0f
        };

        /// <summary>
        /// Swallowing into the stomach only works for a local player that has one. A remote (client) player is fed the old way:
        /// its stomach lives on its own machine and catches are resolved on the host (co-op learning comes later).
        /// </summary>
        public static bool ShouldUseStomach(bool catcherIsRemote, bool hasSwallower) => !catcherIsRemote && hasSwallower;
    }
}
