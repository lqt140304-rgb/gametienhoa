using UnityEngine;

namespace Evolve.World
{
    /// <summary>An area with its own air temperature. Needs a trigger collider.</summary>
    [RequireComponent(typeof(Collider))]
    public class TemperatureZone : MonoBehaviour
    {
        [Tooltip("Air temperature inside, °C")] public float Ambient = 50f;
        public string DisplayName = "Hoang mạc";

        void Reset() => GetComponent<Collider>().isTrigger = true;
        void Awake() => GetComponent<Collider>().isTrigger = true;
    }
}
