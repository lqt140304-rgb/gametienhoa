using System.Collections.Generic;

namespace Evolve.Ecology
{
    /// <summary>A pool of a region's life that species eat from (moss, fungi, ore, carcasses): it grows back by itself.</summary>
    public sealed class Pool
    {
        public float Amount, RegenPerMinute, Capacity;
    }

    /// <summary>
    /// The richer population model (spec 5): several foods per species (weighted), several pools per region, real hunger, hunting that saturates,
    /// nests that never die, and long-range hunters. Used when the region has pools or any species has a Diet; otherwise the old model runs.
    /// Tick order: pools grow, every species asks for its meals (rationed fairly when many want the same thing), hunger and deaths, births, nest recovery.
    /// </summary>
    public partial class RegionPopulation
    {
        public readonly Dictionary<string, Pool> Pools = new Dictionary<string, Pool>();
        /// <summary>Pool that gets a dead animal's BodyFood (carcass → mould → food); null = none.</summary>
        public string ReturnPool;
        /// <summary>Finds another region by id (set by EcologyWorld); long-range diets use it.</summary>
        public System.Func<string, RegionPopulation> Resolver;

        public Pool AddPool(string id, float start, float regenPerMinute, float capacity)
        {
            var p = new Pool { Amount = start, RegenPerMinute = regenPerMinute, Capacity = capacity };
            Pools[id] = p;
            return p;
        }

        bool UsesRichModel
        {
            get
            {
                if (Pools.Count > 0) return true;
                for (int i = 0; i < species.Count; i++) if (species[i].Diet.Count > 0) return true;
                return false;
            }
        }

        struct Ask { public SpeciesPopulation Eater; public object Target; public RegionPopulation TargetRegion; public float Amount, Reachable; }
        readonly List<Ask> asks = new List<Ask>();
        readonly Dictionary<object, float> totals = new Dictionary<object, float>();

        /// <summary>What a diet entry points at, and how much of it there is right now.</summary>
        object Resolve(DietEntry e, out float supply, out RegionPopulation region)
        {
            if (e.IsPool)
            {
                Pools.TryGetValue(e.Id, out var p);
                supply = p != null ? p.Amount : 0f; region = this;
                return p;
            }
            region = e.RegionId == null ? this : Resolver?.Invoke(e.RegionId);
            var prey = region?.Get(e.Id);
            supply = prey != null ? prey.Available : 0f;
            return prey;
        }

        bool DietHasSupply(SpeciesPopulation s)
        {
            for (int i = 0; i < s.Diet.Count; i++) { Resolve(s.Diet[i], out float sup, out _); if (sup > Eps) return true; }
            return false;
        }

        void TickRich(float dt)
        {
            foreach (var p in Pools.Values) p.Amount = System.Math.Min(p.Capacity, System.Math.Max(0f, p.Amount + p.RegenPerMinute * dt));

            // 1. Every species asks for its meals, spread over the foods that exist right now by weight.
            asks.Clear(); totals.Clear();
            for (int i = 0; i < species.Count; i++)
            {
                var s = species[i];
                s.EatenMeals = 0f;
                if (s.Count <= 0 || s.MealsPerHeadPerMinute <= 0f || s.Diet.Count == 0) continue;
                float need = s.Count * s.MealsPerHeadPerMinute * dt, weights = 0f;
                for (int k = 0; k < s.Diet.Count; k++) { Resolve(s.Diet[k], out float sup, out _); if (sup > Eps) weights += s.Diet[k].Weight; }
                if (weights <= 0f) continue;
                for (int k = 0; k < s.Diet.Count; k++)
                {
                    var e = s.Diet[k];
                    var target = Resolve(e, out float sup, out var region);
                    if (target == null || sup <= Eps) continue;
                    float ask = need * e.Weight / weights;
                    if (target is SpeciesPopulation prey)
                    {
                        float escape = prey.EscapeChance < 0f ? 0f : prey.EscapeChance > 0.95f ? 0.95f : prey.EscapeChance;
                        float rate = 1f;
                        if (s.HalfSaturation > 0f && prey.Capacity > 0)
                        {
                            float a = System.Math.Min(1f, prey.Available / (float)prey.Capacity);
                            rate = a / (a + s.HalfSaturation) * (1f + s.HalfSaturation);   // 1 when prey is plentiful, less as it gets rare
                        }
                        ask *= (1f - escape) * rate;
                    }
                    asks.Add(new Ask { Eater = s, Target = target, TargetRegion = region, Amount = ask, Reachable = sup * e.Efficiency });   // far prey: only part of it can be reached
                    totals[target] = (totals.TryGetValue(target, out float t) ? t : 0f) + ask;
                }
            }

            // 2. Fair rationing: when more is asked than there is, everyone gets the same share of what they asked.
            for (int i = 0; i < asks.Count; i++)
            {
                var a = asks[i];
                float supply = a.Reachable;
                float total = totals[a.Target];
                float scale = total <= supply ? 1f : supply / total;
                float eaten = a.Amount * scale;
                a.Eater.EatenMeals += eaten;
                if (a.Target is Pool) continue;   // pools are drained below, in one go
                var prey = (SpeciesPopulation)a.Target;
                prey.PredationAccum += eaten;
            }
            foreach (var kv in totals)
                if (kv.Key is Pool p) p.Amount = System.Math.Max(0f, p.Amount - System.Math.Min(p.Amount, kv.Value));
            for (int i = 0; i < asks.Count; i++)
            {
                if (!(asks[i].Target is SpeciesPopulation prey) || prey.PredationAccum < 1f - Eps) continue;
                int kills = (int)System.Math.Floor(prey.PredationAccum + Eps);
                if (kills > prey.Available) kills = prey.Available;
                for (int k = 0; k < kills; k++) { prey.Kill(false); ReturnBody(asks[i].TargetRegion, prey); }
                prey.PredationAccum -= kills;
                if (prey.PredationAccum < 0f || prey.Available <= 0) prey.PredationAccum = 0f;
            }

            // 3. Hunger, deaths, births, and the nest that never dies.
            for (int i = 0; i < species.Count; i++)
            {
                var s = species[i];
                if (s.Count <= 0) s.Satiety = DietHasSupply(s) ? 1f : 0f;
                else
                {
                    float need = s.Count * s.MealsPerHeadPerMinute * dt;
                    s.Satiety = need <= 0f ? 1f : System.Math.Min(1f, s.EatenMeals / need);
                    s.HungryMinutes = s.Satiety < 0.5f ? s.HungryMinutes + dt : 0f;
                    if (s.Satiety < 1f - Eps && s.Count > s.Floor)
                    {
                        s.DeathAccum += (1f - s.Satiety) * s.StarveRatePerMinute * dt * s.Available;
                        int deaths = (int)System.Math.Floor(s.DeathAccum + Eps);
                        int room = s.Count - s.Floor;
                        if (deaths > room) deaths = room;
                        if (deaths > s.Available) deaths = s.Available;
                        for (int k = 0; k < deaths; k++) { s.Kill(false); ReturnBody(this, s); }
                        s.DeathAccum -= deaths;
                        if (s.DeathAccum < 0f || s.Available <= 0) s.DeathAccum = 0f;
                    }
                }

                if (s.Dens > 0 && s.BirthsPerMinute > 0f && s.Satiety >= 0.5f)
                {
                    s.BirthAccum += s.BirthsPerMinute * s.Dens * dt * s.Satiety;
                    int births = (int)System.Math.Floor(s.BirthAccum + Eps);
                    int free = s.Capacity - s.Count;
                    if (free < 0) free = 0;
                    if (births >= free) { s.Count += free; s.BirthAccum = 0f; }
                    else { s.Count += births; s.BirthAccum -= births; if (s.BirthAccum < 0f) s.BirthAccum = 0f; }
                }

                if (s.Count > 0) s.RecoverTimer = 0f;
                else if (s.Dens > 0 && s.RecoverMinutes > 0f && s.Capacity > 0)
                {
                    s.RecoverTimer += dt;
                    if (s.RecoverTimer >= s.RecoverMinutes - Eps) { s.Count = 1; s.RecoverTimer = 0f; }
                }
            }
        }

        static void ReturnBody(RegionPopulation region, SpeciesPopulation dead)
        {
            if (region == null || region.ReturnPool == null || dead.BodyFood <= 0f || !region.Pools.TryGetValue(region.ReturnPool, out var pool)) return;
            pool.Amount = System.Math.Min(pool.Capacity, pool.Amount + dead.BodyFood);
        }
    }
}
