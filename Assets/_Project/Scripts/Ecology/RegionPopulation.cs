using System.Collections.Generic;

namespace Evolve.Ecology
{
    /// <summary>Pure-C# population model of a region. Tick order: food, starvation, predation, births.</summary>
    public class RegionPopulation
    {
        const float Eps = 1e-4f;

        public float Food;
        public float FoodRegenPerMinute;
        public float FoodCapacity;

        readonly List<SpeciesPopulation> species = new List<SpeciesPopulation>();
        public IReadOnlyList<SpeciesPopulation> Species => species;

        public SpeciesPopulation Add(SpeciesPopulation s)
        {
            species.Add(s);
            return s;
        }

        public SpeciesPopulation Get(string id)
        {
            for (int i = 0; i < species.Count; i++)
                if (species[i].Id == id) return species[i];
            return null;
        }

        public void Tick(float minutes)
        {
            if (minutes <= 0f) return;

            // 1. Food regeneration.
            Food += FoodRegenPerMinute * minutes;
            if (Food > FoodCapacity) Food = FoodCapacity;
            if (Food < 0f) Food = 0f;

            var fed = new Dictionary<SpeciesPopulation, bool>();

            // 2. Region-food eaters; shortage kills (shortage% * 0.5) of the unembodied.
            for (int i = 0; i < species.Count; i++)
            {
                var s = species[i];
                if (s.PreyId != null) continue;
                float need = s.Count * s.FoodPerHeadPerMinute * minutes;
                bool isFed = true;
                if (need > 0f)
                {
                    float eaten = need <= Food ? need : Food;
                    Food -= eaten;
                    float shortage = (need - eaten) / need;
                    if (shortage > Eps)
                    {
                        isFed = false;
                        s.DeathAccum += shortage * 0.5f * s.Available;
                        int deaths = (int)System.Math.Floor(s.DeathAccum + Eps);
                        if (deaths > s.Available) deaths = s.Available;
                        for (int k = 0; k < deaths; k++) s.Kill(false);
                        s.DeathAccum -= deaths;
                        if (s.Available <= 0) s.DeathAccum = 0f;
                    }
                }
                fed[s] = isFed;
            }

            // 3. Predation on unembodied prey.
            for (int i = 0; i < species.Count; i++)
            {
                var s = species[i];
                if (s.PreyId == null) continue;
                fed[s] = true;
                var prey = Get(s.PreyId);
                if (prey == null || prey == s) continue;
                s.KillAccum += s.Count * s.KillsPerHeadPerMinute * minutes;
                int whole = (int)System.Math.Floor(s.KillAccum + Eps);
                int kills = whole < prey.Available ? whole : prey.Available;
                for (int k = 0; k < kills; k++) prey.Kill(false);
                s.KillAccum -= kills;
                if (kills < whole) s.KillAccum = 0f; // no banking when prey ran out
            }

            // 4. Births: only with dens and when fed; clamp to Capacity.
            for (int i = 0; i < species.Count; i++)
            {
                var s = species[i];
                if (s.Dens <= 0 || !fed[s]) continue;
                s.BirthAccum += s.BirthsPerMinute * s.Dens * minutes;
                int births = (int)System.Math.Floor(s.BirthAccum + Eps);
                int room = s.Capacity - s.Count;
                if (room < 0) room = 0;
                if (births >= room)
                {
                    s.Count += room;
                    s.BirthAccum = 0f;
                }
                else
                {
                    s.Count += births;
                    s.BirthAccum -= births;
                }
            }
        }
    }
}
