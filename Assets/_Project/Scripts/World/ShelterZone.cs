using UnityEngine;

namespace Evolve.World
{
    /// <summary>A sheltered area (cave, burrow). Needs a trigger collider.</summary>
    [RequireComponent(typeof(Collider))]
    public class ShelterZone : MonoBehaviour
    {
        [Tooltip("Largest body scale that fits inside")] public float MaxBodyScale = 1.3f;

        static int active;
        /// <summary>True while at least one shelter is enabled in the scene.</summary>
        public static bool AnyActive => active > 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => active = 0;

        void OnEnable() => Track(true);
        void OnDisable() => Track(false);

        /// <summary>Counts a shelter as present or gone (called by OnEnable/OnDisable; tests call it directly because EditMode never enables components).</summary>
        public static void Track(bool on) => active = on ? active + 1 : Mathf.Max(0, active - 1);

        void Reset() => GetComponent<Collider>().isTrigger = true;
        void Awake() => GetComponent<Collider>().isTrigger = true;
    }
}
