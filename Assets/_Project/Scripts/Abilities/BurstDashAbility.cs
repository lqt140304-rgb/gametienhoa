using Evolve.Player;
using UnityEngine;

namespace Evolve.Abilities
{
    /// <summary>Example C-tier ability: violent membrane contraction propels the slime forward.</summary>
    [CreateAssetMenu(menuName = "Evolve/Abilities/Burst Dash", fileName = "BurstDashSO")]
    public class BurstDashAbility : AbilitySO
    {
        public float Force = 10f;

        public override bool Activate(GameObject user)
        {
            if (!user.TryGetComponent(out Rigidbody rb)) return false;
            // Dash toward the crosshair (aim mode) or the mouse cursor (mouse mode), level.
            Vector3 dir = AimController.AimDirection(user, user.transform.position, out _);
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) { dir = rb.linearVelocity; dir.y = 0f; }
            if (dir.sqrMagnitude < 0.01f) dir = user.transform.forward;
            rb.AddForce(dir.normalized * Force + Vector3.up * 1.5f, ForceMode.VelocityChange);
            return true;
        }
    }
}
