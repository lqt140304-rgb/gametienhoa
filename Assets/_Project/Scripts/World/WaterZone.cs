using UnityEngine;

namespace Evolve.World
{
    /// <summary>A body of water the slime can drink from. Needs a trigger collider.</summary>
    [RequireComponent(typeof(Collider))]
    public class WaterZone : MonoBehaviour
    {
        public bool Clean = true;
        public string DisplayName;

        /// <summary>Enabled water zones in the scene.</summary>
        public static int Count { get; private set; }
        public static bool AnyInScene => Count > 0;

        // Domain reload may be off: clear leaked state when entering Play mode.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Count = 0;

        void Reset() => GetComponent<Collider>().isTrigger = true;
        void Awake() => GetComponent<Collider>().isTrigger = true;
        void OnEnable() => Count++;
        void OnDisable() { if (Count > 0) Count--; }
    }
}
