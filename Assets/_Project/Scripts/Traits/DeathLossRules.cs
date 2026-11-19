namespace Evolve.Traits
{
    /// <summary>Dạ Dày Bất Diệt (was Nhân Giữ Gốc): dying with lives left keeps what is in the stomach; the last death (no lives left) is not covered.</summary>
    public static class DeathLossRules
    {
        public const string CoreId = "core.giu_goc";

        /// <param name="livesAfterDeath">Lives left once this death is counted (SurvivalState.Lives when its Died event fires): 2 after the first death, 1 after the second, 0 after the third.</param>
        public static bool KeepsStomach(bool hasGiuGoc, int livesAfterDeath) => hasGiuGoc && livesAfterDeath >= 1;
    }
}
