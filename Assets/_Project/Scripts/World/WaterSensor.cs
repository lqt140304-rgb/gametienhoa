using Evolve.Survival;
using UnityEngine;

namespace Evolve.World
{
    /// <summary>Tracks overlapping WaterZones, quenches thirst, and tells stats whether thirst applies.</summary>
    [RequireComponent(typeof(SurvivalStats))]
    public class WaterSensor : MonoBehaviour
    {
        readonly ZoneSet zones = new ZoneSet();
        SurvivalStats stats;

        public bool InWater => zones.Count > 0;

        /// <summary>In dirty water only: at least one dirty zone and no clean one.</summary>
        public bool InDirtyWater
        {
            get
            {
                bool dirty = false;
                for (int i = 0; i < zones.Count; i++)
                {
                    var z = zones[i] as WaterZone;
                    if (z == null) continue;
                    if (z.Clean) return false;
                    dirty = true;
                }
                return dirty;
            }
        }

        void Awake() => stats = GetComponent<SurvivalStats>();

        void Update()
        {
            if (stats.IsRemote) return;
            zones.Prune(ZoneSet.IsAliveUnityObject);
            stats.State.ThirstEnabled = WaterZone.AnyInScene;
            stats.State.InCleanWater = zones.Count > 0 && !InDirtyWater; // clean water helps against Nhiễm Nước Đọng
            if (zones.Count > 0 && !stats.State.IsDead)
            {
                stats.State.Drink(stats.Config.DrinkPerSec * Time.deltaTime);
                if (InDirtyWater) stats.State.TryInfect(DiseaseKind.StagnantWater, DiseaseRules.DirtyWaterPerSecond * Time.deltaTime);
            }
        }

        void OnTriggerEnter(Collider other)
        {
            var z = other.GetComponent<WaterZone>();
            if (z != null) zones.Enter(z);
        }

        void OnTriggerExit(Collider other)
        {
            var z = other.GetComponent<WaterZone>();
            if (z != null) zones.Exit(z);
        }
    }
}
