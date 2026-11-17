using UnityEngine;

namespace Evolve.Organs
{
    /// <summary>Marks the point on the body where organs of one slot are attached.</summary>
    public class OrganSocket : MonoBehaviour
    {
        public OrganSlot Slot;

        void OnDrawGizmos()
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, 0.05f);
        }
    }
}
