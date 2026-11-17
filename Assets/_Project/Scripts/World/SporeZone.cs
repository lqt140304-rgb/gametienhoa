using UnityEngine;

namespace Evolve.World
{
    /// <summary>A cloud of spores (Vườn Bào Tử): breathing it in can give Mờ Bào Tử. Needs a trigger collider.</summary>
    [RequireComponent(typeof(Collider))]
    public class SporeZone : MonoBehaviour
    {
        void Reset() => GetComponent<Collider>().isTrigger = true;
        void Awake() => GetComponent<Collider>().isTrigger = true;
    }
}
