using System.Collections.Generic;
using Evolve.Survival;
using UnityEngine;

namespace Evolve.World
{
    /// <summary>Tracks which TemperatureZone the body is in and feeds the air temperature to its stats.</summary>
    [RequireComponent(typeof(SurvivalStats))]
    public class ClimateSensor : MonoBehaviour
    {
        readonly List<TemperatureZone> zones = new List<TemperatureZone>();
        SurvivalStats stats;

        /// <summary>The zone the body is in (most recently entered), or null outdoors.</summary>
        public TemperatureZone Zone => zones.Count > 0 ? zones[zones.Count - 1] : null;

        void Awake() => stats = GetComponent<SurvivalStats>();

        void Update()
        {
            zones.RemoveAll(z => z == null);
            stats.State.Ambient = Zone != null ? Zone.Ambient : stats.Config.DefaultAmbient;
        }

        void OnTriggerEnter(Collider other)
        {
            var z = other.GetComponent<TemperatureZone>();
            if (z != null && !zones.Contains(z)) zones.Add(z);
        }

        void OnTriggerExit(Collider other)
        {
            var z = other.GetComponent<TemperatureZone>();
            if (z != null) zones.Remove(z);
        }
    }
}
