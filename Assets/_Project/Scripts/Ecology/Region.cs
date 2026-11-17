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
        readonly List<EmbodiedCreature> embodied = new List<EmbodiedCreature>();
        readonly List<Den> candidates = new List<Den>();
        float timer;

        public RegionPopulation Population { get; private set; }
        public bool IsActive { get; private set; }
        public Vector3 Size => size;

        void Awake()
        {
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
                }));
            }
            RecountDens();
        }

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
                pops[i].Dens = n;
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
            Population.Tick(tickSeconds / 60f);
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
                    Spawn(so, pop, denPos + new Vector3(off.x, 0f, off.y), denPos);
                }
            }
        }

        EmbodiedCreature Spawn(SpeciesSO so, SpeciesPopulation pop, Vector3 position, Vector3 home)
        {
            var go = Instantiate(so.Prefab, position, Quaternion.identity);
            var ec = go.GetComponent<EmbodiedCreature>();
            if (ec == null) ec = go.AddComponent<EmbodiedCreature>();
            ec.Init(this, pop);
            ec.SetHome(home);
            embodied.Add(ec);
            return ec;
        }

        /// <summary>
        /// Embody one head of <paramref name="so"/> at <paramref name="position"/> (used by tier-B incursions).
        /// The creature is tracked by this region, so StoreAll stores it when the region goes inactive.
        /// Returns null when no head is available or the global cap is full.
        /// </summary>
        public EmbodiedCreature TryEmbodyAt(SpeciesSO so, Vector3 position, Vector3 home)
        {
            if (Population == null || so == null || so.Prefab == null) return null;
            var pop = Population.Get(so.Id);
            if (pop == null || !pop.TryEmbody()) return null;
            if (!PopulationCap.TryReserve()) { pop.Store(); return null; }
            return Spawn(so, pop, position, home);
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
