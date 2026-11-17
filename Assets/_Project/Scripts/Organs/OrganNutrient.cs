using Evolve.Survival;

namespace Evolve.Organs
{
    /// <summary>Which nutrient an organ needs to be equipped. Auto = decided by the slot it grows in.</summary>
    public enum OrganNutrient { Auto = 0, Protein, Calcium, Iron, Fat, Vitamin }

    public static class OrganNutrients
    {
        /// <summary>Eyes need Tinh Chất Sống, legs Sợi Nhầy, skin Mỡ Nhờn, back (spikes, shell) Tinh Vôi.</summary>
        public static NutrientKind DefaultFor(OrganSlot slot) => slot switch
        {
            OrganSlot.Eyes => NutrientKind.Vitamin,
            OrganSlot.Legs => NutrientKind.Protein,
            OrganSlot.Skin => NutrientKind.Fat,
            _ => NutrientKind.Calcium
        };

        public static NutrientKind Resolve(OrganNutrient need, OrganSlot slot)
            => need == OrganNutrient.Auto ? DefaultFor(slot) : (NutrientKind)((int)need - 1);
    }
}
