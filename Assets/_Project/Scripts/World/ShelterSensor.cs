using Evolve.Player;
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
        SlimeController ctrl;

        public bool InShelter { get; private set; }

        public static bool Fits(float maxBodyScale, float bodyScale) => maxBodyScale + 0.001f >= bodyScale;

        void Awake()
        {
            stats = GetComponent<SurvivalStats>();
            ctrl = GetComponent<SlimeController>();
        }

        void Update()
        {
            if (stats.IsRemote) return;
            zones.Prune(ZoneSet.IsAliveUnityObject);
            float scale = ctrl != null ? ctrl.BodyScale : 1f;
            bool fits = false;
            for (int i = 0; i < zones.Count && !fits; i++)
            {
                var z = zones[i] as ShelterZone;
                if (z != null && Fits(z.MaxBodyScale, scale)) fits = true;
            }
            InShelter = fits && !stats.State.IsDead;
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
