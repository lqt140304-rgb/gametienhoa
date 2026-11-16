using UnityEngine;

namespace Evolve.Abilities
{
    /// <summary>Nảy Bật (C): violent membrane contraction propels the slime forward.</summary>
    [CreateAssetMenu(menuName = "Evolve/Abilities/Burst Dash", fileName = "BurstDashSO")]
    public class BurstDashAbility : AbilitySO
    {
        public float Force = 10f;

        void Reset() { DisplayName = "Nảy Bật"; EnergyCost = 20f; Cooldown = 4f; }

        public override bool Activate(GameObject user)
        {
            if (!user.TryGetComponent(out Rigidbody rb)) return false;
            Vector3 dir = rb.linearVelocity; dir.y = 0;
            if (dir.sqrMagnitude < 0.01f) dir = user.transform.forward;
            rb.AddForce(dir.normalized * Force + Vector3.up * 1.5f, ForceMode.VelocityChange);
            return true;
        }
    }
}
