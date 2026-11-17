using UnityEngine;

namespace Evolve.Survival
{
    /// <summary>The five nutrients the slime body needs, named the slime way (spec section 5).</summary>
    public enum NutrientKind { Protein = 0, Calcium = 1, Iron = 2, Fat = 3, Vitamin = 4 }

    public static class NutrientKinds
    {
        public const int Count = 5;

        /// <summary>Name in the game: Sợi Nhầy, Tinh Vôi, Huyết Thiết, Mỡ Nhờn, Tinh Chất Sống.</summary>
        public static string SlimeName(NutrientKind k) => k switch
        {
            NutrientKind.Protein => "Sợi Nhầy",
            NutrientKind.Calcium => "Tinh Vôi",
            NutrientKind.Iron => "Huyết Thiết",
            NutrientKind.Fat => "Mỡ Nhờn",
            _ => "Tinh Chất Sống"
        };

        /// <summary>The real nutrient behind the slime name, for tooltips.</summary>
        public static string RealName(NutrientKind k) => k switch
        {
            NutrientKind.Protein => "đạm",
            NutrientKind.Calcium => "canxi",
            NutrientKind.Iron => "sắt",
            NutrientKind.Fat => "chất béo",
            _ => "vitamin"
        };

        public static Color ColorOf(NutrientKind k) => k switch
        {
            NutrientKind.Protein => new Color(0.95f, 0.65f, 0.7f),
            NutrientKind.Calcium => new Color(0.92f, 0.9f, 0.8f),
            NutrientKind.Iron => new Color(0.85f, 0.25f, 0.2f),
            NutrientKind.Fat => new Color(0.95f, 0.85f, 0.35f),
            _ => new Color(0.45f, 0.9f, 0.5f)
        };
    }
}
