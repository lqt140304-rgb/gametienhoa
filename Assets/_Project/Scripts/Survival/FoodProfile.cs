namespace Evolve.Survival
{
    /// <summary>Nutrient points gained per 1 unit of food eaten, for one kind of food.</summary>
    public readonly struct FoodProfile
    {
        public readonly float Protein, Calcium, Iron, Fat, Vitamin;

        public FoodProfile(float protein, float calcium, float iron, float fat, float vitamin)
        {
            Protein = protein; Calcium = calcium; Iron = iron; Fat = fat; Vitamin = vitamin;
        }

        public float Of(NutrientKind k) => k switch
        {
            NutrientKind.Protein => Protein,
            NutrientKind.Calcium => Calcium,
            NutrientKind.Iron => Iron,
            NutrientKind.Fat => Fat,
            _ => Vitamin
        };

        public FoodProfile Scaled(float factor) => new FoodProfile(Protein * factor, Calcium * factor, Iron * factor, Fat * factor, Vitamin * factor);
    }

    /// <summary>What a food source is; maps to its <see cref="FoodProfile"/>.</summary>
    public enum FoodKind { Generic, PreyMeat, PredatorMeat, GlowMoss, AmberBud, SporePod, RootDew, EmberBerry }

    public static class FoodProfiles
    {
        /// <summary>Plain food with no particular character (also what remote players get, see the 0.5c plan).</summary>
        public static readonly FoodProfile Mixed = new FoodProfile(0.25f, 0.25f, 0.25f, 0.25f, 0.25f);

        static readonly FoodProfile PreyMeat = new FoodProfile(1.0f, 0f, 0.4f, 0.3f, 0f);
        static readonly FoodProfile PredatorMeat = new FoodProfile(1.2f, 0.2f, 0.8f, 0.5f, 0f);
        static readonly FoodProfile GlowMoss = new FoodProfile(0f, 0f, 0f, 0f, 1.0f);
        static readonly FoodProfile AmberBud = new FoodProfile(0f, 0f, 0f, 0.8f, 0.3f);
        static readonly FoodProfile SporePod = new FoodProfile(0f, 0.4f, 0f, 0f, 0.6f);
        static readonly FoodProfile RootDew = new FoodProfile(0f, 1.0f, 0f, 0f, 0.2f);
        static readonly FoodProfile EmberBerry = new FoodProfile(0f, 0f, 0.8f, 0f, 0.2f);

        public static FoodProfile For(FoodKind kind) => kind switch
        {
            FoodKind.PreyMeat => PreyMeat,
            FoodKind.PredatorMeat => PredatorMeat,
            FoodKind.GlowMoss => GlowMoss,
            FoodKind.AmberBud => AmberBud,
            FoodKind.SporePod => SporePod,
            FoodKind.RootDew => RootDew,
            FoodKind.EmberBerry => EmberBerry,
            _ => Mixed
        };

        /// <summary>A carcass feeds like predator meat while fresh; rotten meat gives much less.</summary>
        public static FoodProfile Carcass(bool rotten) => rotten ? PredatorMeat.Scaled(0.3f) : PredatorMeat;
    }
}
