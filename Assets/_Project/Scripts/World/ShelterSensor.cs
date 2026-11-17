using Evolve.Survival;
using UnityEngine;

namespace Evolve.World
{
    /// <summary>Tracks overlapping ShelterZones and feeds InShelter to stats.</summary>
    [RequireComponent(typeof(SurvivalStats))]
    public class ShelterSensor : MonoBehaviour
    {
        readonly ZoneSet zones = new ZoneSet();
        SurvivalStats stats;

        public bool InShelter { get; private set; }

        void Awake() => stats = GetComponent<SurvivalStats>();

        void Update()
        {
            if (stats.IsRemote) return;
            zones.Prune(ZoneSet.IsAliveUnityObject);
            InShelter = zones.Count > 0 && !stats.State.IsDead;
            stats.State.InShelter = InShelter;
        }

        void OnTriggerEnter(Collider other)
        {
            var z = other.GetComponent<ShelterZone>();
            if (z != null) zones.Enter(z);
        }

        void OnTriggerExit(Collider other)
        {
            var z = other.GetComponent<ShelterZone>();
            if (z != null) zones.Exit(z);
        }
    }
}
