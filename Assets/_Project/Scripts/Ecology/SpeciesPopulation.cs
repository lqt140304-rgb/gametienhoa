using System.Collections.Generic;

namespace Evolve.Ecology
{
    /// <summary>One food of a species: another species (weighted), or a pool of the region's life (moss, fungi, ore, carcasses).</summary>
    public sealed class DietEntry
    {
        public string Id;
        public float Weight = 1f;
        /// <summary>True: Id names a pool of the region; false: a species.</summary>
        public bool IsPool;
        /// <summary>A species or pool of another region (long-range hunters: Dơi, Chim Hang Mẹ, Cáo Gió); null = this region.</summary>
        public string RegionId;
        /// <summary>Share of the food there it can reach (0.5 = only half of the far prey is within its range; what it reaches feeds it normally).</summary>
        public float Efficiency = 1f;
    }

    /// <summary>Abstract (L2) population of one species in a region.</summary>
    public class SpeciesPopulation
    {
        public string Id;
        public int Count;
        public int Embodied;
        public int Capacity;
        public float BirthsPerMinute;
        public float FoodPerHeadPerMinute;
        public int Dens;
        /// <summary>null = eats region food.</summary>
        public string PreyId;
        public float KillsPerHeadPerMinute;
        /// <summary>Share of hunts on this species that fail because it escapes (0..0.95); see Creatures/Pure/PreyDefenseSeed.</summary>
        public float EscapeChance;

        // Fractional accumulators (floored when they reach a whole number).
        /// <summary>Set during Tick; whether the species had enough food/prey for births.</summary>
        [System.NonSerialized] public bool Fed = true;

        public float BirthAccum;
        public float DeathAccum;
        public float KillAccum;

        // ---- rich model (used when the region has pools or any species has a Diet; see RegionPopulation.Rich) ----
        public readonly List<DietEntry> Diet = new List<DietEntry>();
        /// <summary>Meals (kills or units of a pool) one head needs per minute; 0 = does not eat (Golem Thạch Anh).</summary>
        public float MealsPerHeadPerMinute;
        /// <summary>Hunting saturates: with this much of the prey's capacity left, hunters catch half as much. Rarer prey is caught less, so it is never wiped out. 0 = off.</summary>
        public float HalfSaturation = 0.25f;
        /// <summary>Share of the population that dies per minute when it gets no food at all.</summary>
        public float StarveRatePerMinute = 0.3f;
        /// <summary>Hunger never kills below this many (a hunter dwindles to one, then the nest breeds it back).</summary>
        public int Floor;
        /// <summary>With dens, an empty species returns with one animal after this many minutes whatever the food (the nest never dies). Lords: about 9.</summary>
        public float RecoverMinutes;
        /// <summary>Units added to the region's ReturnPool when one dies (carcass, then mould, then food again).</summary>
        public float BodyFood;
        /// <summary>May move to a neighbouring cave when crowded or hungry (see EcologyWorld).</summary>
        public bool Migrates;
        /// <summary>A migrant colony in a cave that is not its home: no dens, so it only dwindles.</summary>
        public bool Guest;
        [System.NonSerialized] public float Satiety = 1f, RecoverTimer, HungryMinutes, MigrateWait, EatenMeals, PredationAccum;

        public int Available => Count - Embodied;

        public bool TryEmbody()
        {
            if (Available <= 0) return false;
            Embodied++;
            return true;
        }

        public void Store()
        {
            if (Embodied > 0) Embodied--;
        }

        public void Kill(bool wasEmbodied)
        {
            if (Count > 0) Count--;
            if (wasEmbodied && Embodied > 0) Embodied--;
            if (Embodied > Count) Embodied = Count;
        }
    }
}
