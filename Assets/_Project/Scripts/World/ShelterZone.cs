using UnityEngine;

namespace Evolve.World
{
    /// <summary>A sheltered area (cave, burrow). Needs a trigger collider.</summary>
    [RequireComponent(typeof(Collider))]
    public class ShelterZone : MonoBehaviour
    {
        [Tooltip("Largest body scale that fits inside")] public float MaxBodyScale = 1.3f;

        void Reset() => GetComponent<Collider>().isTrigger = true;
        void Awake() => GetComponent<Collider>().isTrigger = true;
    }
}
