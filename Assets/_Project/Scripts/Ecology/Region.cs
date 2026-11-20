using System.Collections.Generic;
using Evolve.Organs;
using Evolve.Survival;
using UnityEngine;

namespace Evolve.Ecology
{
    /// <summary>
    /// A region owning an abstract population (always ticking, L2). While a living player is near
    /// a den or the centre it embodies creatures (L0) at dens; otherwise it stores them back.
    /// </summary>
    public class Region : MonoBehaviour
    {
        const int MaxEmbodyPerTick = 2;
        const float SpawnOffset = 3f;

        [SerializeField] Vector3 size = new Vector3(120f, 40f, 120f);
        [SerializeField] float activateRadius = 45f;
        [SerializeField] float deactivateRadius = 60f;
        [SerializeField] float tickSeconds = 5f;
        [SerializeField] float startFood = 200f;
        [SerializeField] float foodRegenPerMinute = 20f;
        [SerializeField] float foodCapacity = 400f;
        [SerializeField] List<SpeciesSO> species = new List<SpeciesSO>();

        readonly List<Den> dens = new List<Den>();
        readonly List<SpeciesPopulation> pops = new List<SpeciesPopulation>();
        readonly Dictionary<SpeciesPopulation, int> seedDens = new Dictionary<SpeciesPopulation, int>();   // cave model: dens the balance was tuned for
        readonly List<EmbodiedCreature> embodied = new List<EmbodiedCreature>();
        readonly List<Den> candidates = new List<Den>();
        float timer;

        public RegionPopulation Population { get; private set; }
        /// <summary>Ecology cave this region runs (B1 rich model from <see cref="EcologyRuntime"/>), or null for the old single-food model.</summary>
        public string CaveId { get; private set; }
        public bool IsActive { get; private set; }
        public Vector3 Size => size;

        void Awake()
        {
            if (!NetWorld.IsClient && TryUseCaveModel()) { RecountDens(); return; }
            Population = new RegionPopulation
            {
                Food = startFood,
                FoodRegenPerMinute = foodRegenPerMinute,
                FoodCapacity = foodCapacity,
            };
            for (int i = 0; i < species.Count; i++)
            {
                var so = species[i];
                if (so == null) { pops.Add(null); continue; }
                pops.Add(Population.Add(new SpeciesPopulation
                {
                    Id = so.Id,
                    Count = so.StartCount,
                    Capacity = so.Capacity,
                    BirthsPerMinute = so.BirthsPerMinute,
                    FoodPerHeadPerMinute = so.FoodPerHeadPerMinute,
                    PreyId = so.Prey != null ? so.Prey.Id : null,
                    KillsPerHeadPerMinute = so.KillsPerHeadPerMinute,
                    EscapeChance = so.EscapeChance,
                }));
            }
            RecountDens();
        }

        /// <summary>
        /// When the species list belongs to a known cave (CaveEcologySeed, matched by SpeciesSO id or name, e.g. "ThanLanBoHo"),
        /// run that cave's rich population. Species of the list the seed lacks get an old-style entry in it so they still embody.
        /// </summary>
        bool TryUseCaveModel()
        {
            var ids = new string[species.Count];
            string cave = null;
            for (int i = 0; i < species.Count; i++)
            {
                var so = species[i];
                if (so == null) continue;
                ids[i] = CaveEcologySeed.IdByName(so.Id) ?? CaveEcologySeed.IdByName(so.name);
                cave ??= CaveEcologySeed.CaveOfSpecies(ids[i]);
            }
            var pop = EcologyRuntime.Claim(cave);
            if (pop == null) return false;
            CaveId = cave;
            Population = pop;
            for (int i = 0; i < species.Count; i++)
            {
                var so = species[i];
                if (so == null) { pops.Add(null); continue; }
                var p = ids[i] != null ? pop.Get(ids[i]) : null;
                if (p == null)
                    p = pop.Get(so.Id) ?? pop.Add(new SpeciesPopulation
                    {
                        Id = so.Id, Count = so.StartCount, Capacity = so.Capacity, BirthsPerMinute = so.BirthsPerMinute,
                        FoodPerHeadPerMinute = so.FoodPerHeadPerMinute, KillsPerHeadPerMinute = so.KillsPerHeadPerMinute, EscapeChance = so.EscapeChance,
                    });
                if (!seedDens.ContainsKey(p)) seedDens[p] = Mathf.Max(1, p.Dens);
                pops.Add(p);
            }
            return true;
        }

        void OnDestroy() => EcologyRuntime.Release(CaveId);

        public void AddFood(float amount)
        {
            if (Population == null) return;
            Population.Food = Mathf.Clamp(Population.Food + amount, 0f, Population.FoodCapacity);
        }

        public void RegisterDen(Den den)
        {
            if (!dens.Contains(den)) dens.Add(den);
            RecountDens();
        }

        public void UnregisterDen(Den den)
        {
            dens.Remove(den);
            RecountDens();
        }

        /// <summary>Recompute SpeciesPopulation.Dens from living registered dens.</summary>
        public void RecountDens()
        {
            if (Population == null) return; // Awake will recount
            for (int i = 0; i < pops.Count; i++)
            {
                if (pops[i] == null) continue;
                int n = 0;
                for (int d = 0; d < dens.Count; d++)
                    if (dens[d] != null && !dens[d].Destroyed && dens[d].Species == species[i]) n++;
                // cave model: a standing den keeps the tuned birth rate, razing them all stops births
                pops[i].Dens = seedDens.TryGetValue(pops[i], out int tuned) ? (n > 0 ? tuned : 0) : n;
            }
        }

        internal void Forget(EmbodiedCreature c) => embodied.Remove(c);

        void Update()
        {
            if (Population == null || NetWorld.IsClient) return;
            timer += Time.deltaTime;
            if (timer < tickSeconds) return;
            timer -= tickSeconds;
            Step();
        }

        void Step()
        {
            if (CaveId != null) EcologyRuntime.Tick(); else Population.Tick(tickSeconds / 60f);
            IsActive = EvaluateActive();
            if (IsActive) EmbodyStep();
            else StoreAll();
        }

        bool EvaluateActive()
        {
            float r = IsActive ? deactivateRadius : activateRadius;
            float r2 = r * r;
            var all = SurvivalStats.All;
            for (int i = 0; i < all.Count; i++)
            {
                var p = all[i];
                if (p == null || p.State.IsDead) continue;
                Vector3 pos = p.transform.position;
                if (Near(pos, transform.position, r2)) return true;
                for (int d = 0; d < dens.Count; d++)
                {
                    var den = dens[d];
                    if (den == null || den.Destroyed) continue;
                    if (Near(pos, den.transform.position, r2)) return true;
                }
            }
            return false;
        }

        static bool Near(Vector3 a, Vector3 b, float r2)
        {
            float dx = a.x - b.x, dz = a.z - b.z;
            return dx * dx + dz * dz <= r2;
        }

        void EmbodyStep()
        {
            for (int i = 0; i < species.Count; i++)
            {
                var so = species[i];
                var pop = pops[i];
                if (so == null || pop == null || so.Prefab == null) continue;
                if (so.Tier == OrganTier.B) continue; // Task 4 handles tier B

                candidates.Clear();
                for (int d = 0; d < dens.Count; d++)
                    if (dens[d] != null && !dens[d].Destroyed && dens[d].Species == so) candidates.Add(dens[d]);
                if (candidates.Count == 0) continue;

                for (int n = 0; n < MaxEmbodyPerTick; n++)
                {
                    if (!pop.TryEmbody()) break;
                    if (!PopulationCap.TryReserve()) { pop.Store(); return; }
                    var den = candidates[Random.Range(0, candidates.Count)];
                    Vector2 off = Random.insideUnitCircle * SpawnOffset;
                    Vector3 denPos = den.transform.position;
                    Spawn(so, pop, denPos + new Vector3(off.x, so.Prefab.transform.position.y, off.y), denPos, 0f);
                }
            }
        }

        /// <summary>
        /// A creature a spawner placed in the scene becomes one head of the species' population: it takes an embodied slot
        /// (so <see cref="EmbodyStep"/> only tops up what is missing), a kill lowers the count, and it is stored back when the region sleeps.
        /// False (the creature stays a plain scene creature) for an unknown species, no free head, or a full global cap.
        /// </summary>
        public bool Adopt(GameObject go)
        {
            if (Population == null || go == null || NetWorld.IsClient) return false;
            string key = Evolve.Creatures.SpeciesNames.Key(go.name);
            for (int i = 0; i < species.Count; i++)
            {
                var so = species[i];
                if (so == null || (Evolve.Creatures.SpeciesNames.Key(so.Id) != key && Evolve.Creatures.SpeciesNames.Key(so.name) != key)) continue;
                var pop = PopulationOf(so);
                if (pop == null || !pop.TryEmbody()) return false;
                if (!PopulationCap.TryReserve()) { pop.Store(); return false; }
                var ec = go.GetComponent<EmbodiedCreature>();
                if (ec == null) ec = go.AddComponent<EmbodiedCreature>();
                ec.Init(this, pop);
                embodied.Add(ec);
                Evolve.Creatures.CreatureTuning.Apply(go, CaveId);
                return true;
            }
            return false;
        }

        EmbodiedCreature Spawn(SpeciesSO so, SpeciesPopulation pop, Vector3 position, Vector3 home, float homeRadius)
        {
            float baseY = Evolve.World.GroundHeight.At(position.x, position.z, home.y) + so.Prefab.transform.position.y;
            position.y = Mathf.Max(position.y, baseY);
            var go = Instantiate(so.Prefab, position, Quaternion.identity);
            var ec = go.GetComponent<EmbodiedCreature>();
            if (ec == null) ec = go.AddComponent<EmbodiedCreature>();
            ec.Init(this, pop);
            if (homeRadius > 0f) ec.SetHome(home, homeRadius); else ec.SetHome(home);
            embodied.Add(ec);
            Evolve.Creatures.CreatureTuning.Apply(go, CaveId);
            return ec;
        }

        /// <summary>
        /// Embody one head of <paramref name="so"/> at <paramref name="position"/> (used by tier-B incursions).
        /// The creature is tracked by this region, so StoreAll stores it when the region goes inactive.
        /// Returns null when no head is available or the global cap is full.
        /// </summary>
        public EmbodiedCreature TryEmbodyAt(SpeciesSO so, Vector3 position, Vector3 home, float homeRadius = 0f)
        {
            if (Population == null || so == null || so.Prefab == null) return null;
            var pop = PopulationOf(so);
            if (pop == null || !pop.TryEmbody()) return null;
            if (!PopulationCap.TryReserve()) { pop.Store(); return null; }
            return Spawn(so, pop, position, home, homeRadius);
        }

        /// <summary>The population entry of a species of this region (cave ids differ from SpeciesSO ids, so do not use Population.Get(so.Id)).</summary>
        public SpeciesPopulation PopulationOf(SpeciesSO so)
        {
            int i = species.IndexOf(so);
            return so == null || Population == null ? null : i >= 0 && i < pops.Count && pops[i] != null ? pops[i] : Population.Get(so.Id);
        }

        void StoreAll()
        {
            for (int i = embodied.Count - 1; i >= 0; i--)
            {
                if (i >= embodied.Count) continue;
                var c = embodied[i];
                if (c == null) { embodied.RemoveAt(i); continue; }
                c.StoreBack(); // removes itself from the list
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 0.4f, 0.4f);
            Gizmos.DrawWireCube(transform.position, size);
        }
    }
}
