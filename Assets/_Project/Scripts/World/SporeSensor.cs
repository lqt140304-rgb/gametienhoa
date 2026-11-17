using Evolve.Survival;
using UnityEngine;

namespace Evolve.World
{
    /// <summary>Tracks the spore clouds the body is in; breathing them risks Mờ Bào Tử.</summary>
    [RequireComponent(typeof(SurvivalStats))]
    public class SporeSensor : MonoBehaviour
    {
        readonly ZoneSet zones = new ZoneSet();
        SurvivalStats stats;

        public bool InSpores => zones.Count > 0;

        void Awake() => stats = GetComponent<SurvivalStats>();

        void Update()
        {
            if (stats.IsRemote) return;
            zones.Prune(ZoneSet.IsAliveUnityObject);
            if (zones.Count > 0 && !stats.State.IsDead)
                stats.State.TryInfect(DiseaseKind.SporeBlur, DiseaseRules.SporePerSecond * Time.deltaTime);
        }

        void OnTriggerEnter(Collider other)
        {
            var z = other.GetComponent<SporeZone>();
            if (z != null) zones.Enter(z);
        }

        void OnTriggerExit(Collider other)
        {
            var z = other.GetComponent<SporeZone>();
            if (z != null) zones.Exit(z);
        }
    }
}
