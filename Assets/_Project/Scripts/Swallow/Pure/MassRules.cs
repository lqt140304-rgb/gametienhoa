namespace Evolve.Swallow
{
    /// <summary>
    /// Body mass (khối lượng) is the slime's health and its swallow limit (spec 5.1).
    /// Sculpting organs spends mass; removing one gives half back. Numbers are proposals.
    /// </summary>
    public static class MassRules
    {
        public const float StartMass = 10f;
        /// <summary>Sculpting never takes the body below this.</summary>
        public const float MinMass = 5f;
        public const float HealthPerMass = 10f;
        /// <summary>Things up to this share of the slime's own mass go down whole.</summary>
        public const float SwallowWholeRatio = 0.8f;
        public const float DissolveSecondsPerRatio = 3f;
        /// <summary>Share of a digested item's mass that becomes the slime's mass.</summary>
        public const float DigestMassShare = 0.3f;

        public static bool CanSwallowWhole(float slimeMass, float targetMass) => targetMass <= slimeMass * SwallowWholeRatio + 1e-4f;

        /// <summary>Seconds of holding on to dissolve something too big to swallow whole.</summary>
        public static float DissolveSeconds(float slimeMass, float targetMass) =>
            DissolveSecondsPerRatio * targetMass / (System.Math.Max(0.01f, slimeMass) * SwallowWholeRatio);

        public static float SculptCost(GeneRarity r) => r switch
        {
            GeneRarity.Rare => 4f,
            GeneRarity.Epic => 7f,
            GeneRarity.Legendary => 12f,
            _ => 2f
        };

        public static float Refund(GeneRarity r) => SculptCost(r) * 0.5f;

        /// <summary>Mass left after spending, or -1 when that would go below MinMass.</summary>
        public static float AfterSpend(float mass, float cost) => mass - cost >= MinMass - 1e-4f ? mass - cost : -1f;

        /// <summary>Organs are reshaped only in a shelter, only from learned genes, and only with mass to spare.</summary>
        public static bool CanSculpt(bool inShelter, bool known, float mass, GeneRarity r) =>
            inShelter && known && AfterSpend(mass, SculptCost(r)) >= 0f;

        public static float MaxHealth(float mass) => mass * HealthPerMass;

        /// <summary>
        /// Body scale used to check whether the slime fits a shelter. A gel body squeezes through: until shelters are
        /// rebuilt for body size (step 3), it never counts as bigger than scale 1, so growing never locks it out of Biến Hình.
        /// </summary>
        public static float ShelterFitScale(float bodyScale) => bodyScale < 1f ? bodyScale : 1f;

        /// <summary>Why the sculpt button is locked (null = it can be pressed). <paramref name="refund"/> is what the organ in that slot gives back.</summary>
        public static string SculptBlockReason(bool inShelter, float mass, float refund, GeneRarity r)
        {
            if (!inShelter) return "Chỉ nặn lại cơ thể được trong hang trú.";
            float cost = SculptCost(r);
            if (AfterSpend(mass + refund, cost) < 0f) return $"Không đủ khối (cần {cost:0.#}, giữ tối thiểu {MinMass:0}).";
            return null;
        }
    }
}
