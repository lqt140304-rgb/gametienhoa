using Evolve.Creatures;
using UnityEngine;

namespace Evolve.Ecology
{
    /// <summary>Marks a scene creature as the physical body of one head of a SpeciesPopulation. Reports only once.</summary>
    public class EmbodiedCreature : MonoBehaviour
    {
        public const float HomeRadius = 15f;

        Region region;
        SpeciesPopulation population;
        bool reported;
        bool initialized;

        public SpeciesPopulation Population => population;
        public Region Region => region;

        public void Init(Region region, SpeciesPopulation population)
        {
            this.region = region;
            this.population = population;
            reported = false;
            initialized = true;
        }

        /// <summary>Anchor the creature's wandering area (Prey/Predator) to the den.</summary>
        public void SetHome(Vector3 center) => SetHome(center, HomeRadius);

        /// <summary>Home with an explicit roaming radius (the tier-B hunter patrols a whole quadrant).</summary>
        public void SetHome(Vector3 center, float radius)
        {
            var prey = GetComponent<Prey>();
            if (prey != null) { prey.HomeCenter = center; prey.HomeRadius = radius; }
            var predator = GetComponent<Predator>();
            if (predator != null) { predator.HomeCenter = center; predator.HomeRadius = radius; }
        }

        /// <summary>The creature was killed in the scene: the population loses one head.</summary>
        public void NotifyKilled()
        {
            if (reported || !initialized) return;
            reported = true;
            if (population != null) population.Kill(true);
            PopulationCap.Release();
            if (region != null) region.Forget(this);
        }

        /// <summary>Region deactivated: return the head to the abstract population.</summary>
        public void StoreBack()
        {
            if (reported || !initialized) return;
            reported = true;
            if (population != null) population.Store();
            PopulationCap.Release();
            if (region != null) region.Forget(this);
            Destroy(gameObject);
        }

        void OnDestroy()
        {
            // Destroyed by something else without reporting: treat as stored back.
            if (reported || !initialized) return;
            reported = true;
            if (population != null) population.Store();
            PopulationCap.Release();
            if (region != null) region.Forget(this);
        }
    }
}
