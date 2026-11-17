using UnityEngine;

namespace Evolve.Creatures
{
    /// <summary>Moves a replica (a copy of a host creature shown on a client) smoothly toward the last reported position.</summary>
    public class ReplicaMotion : MonoBehaviour
    {
        Vector3 targetPosition;
        float targetYaw;
        bool placed;

        /// <summary>Time (unscaled) of the last update from the host; used to drop replicas that stopped being reported.</summary>
        public float LastSeen { get; private set; }

        public void SetTarget(Vector3 position, float yaw, float now)
        {
            targetPosition = position;
            targetYaw = yaw;
            LastSeen = now;
            if (!placed)
            {
                transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
                placed = true;
            }
        }

        void Update()
        {
            if (!placed) return;
            float k = 1f - Mathf.Exp(-12f * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, targetPosition, k);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0f, targetYaw, 0f), k);
        }
    }
}
