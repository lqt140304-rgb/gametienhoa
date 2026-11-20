namespace Evolve.Ecology
{
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
