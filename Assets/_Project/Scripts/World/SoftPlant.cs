using Evolve.Player;
using UnityEngine;

namespace Evolve.World
{
    /// <summary>
    /// A soft plant (fern, reed, bush): a slime pushing through it is slowed while it is inside, as if wading through leaves. It does not block the way (a trigger).
    /// <see cref="Stiffness"/> 0..1: how thick the plant is (see <see cref="PlantDragMath"/>).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class SoftPlant : MonoBehaviour
    {
        [Range(0f, 1f)] public float Stiffness = 0.4f;

        void Reset() => GetComponent<Collider>().isTrigger = true;
        void Awake() => GetComponent<Collider>().isTrigger = true;

        void OnTriggerStay(Collider other)
        {
            var slime = other.GetComponentInParent<SlimeController>();
            if (slime != null) slime.NotePlantDrag(PlantDragMath.SpeedFor(Stiffness));
        }
    }
}
