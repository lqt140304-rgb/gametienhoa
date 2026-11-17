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

        Evolve.Player.SlimeController controller;

        void Update()
        {
            if (stats.IsRemote) return;
            // Wading: water slows the slime down (it pushes through it).
            if (controller == null) controller = GetComponent<Evolve.Player.SlimeController>();
            if (controller != null) controller.SpeedScale = zones.Count > 0 ? 0.65f : 1f;
            zones.Prune(ZoneSet.IsAliveUnityObject);
            stats.State.ThirstEnabled = WaterZone.AnyInScene;
            if (zones.Count > 0 && !stats.State.IsDead)
                stats.State.Drink(stats.Config.DrinkPerSec * Time.deltaTime);
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
