using Evolve.Player;
using UnityEngine;

namespace Evolve.Abilities
{
    /// <summary>
    /// Skill of the Muscular Pseudopods (tier B): a hard charge forward. Fast enough to ram a predator
    /// (see Predator.ramSpeed). Proposed numbers, tune in the asset.
    /// </summary>
    [CreateAssetMenu(menuName = "Evolve/Abilities/Charge", fileName = "ChargeSO")]
    public class ChargeAbility : AbilitySO
    {
        public float Force = 9f;

        public override bool Activate(GameObject user) => Activate(user, 2);

        public override bool Activate(GameObject user, int level)
        {
            if (!user.TryGetComponent(out Rigidbody rb)) return false;
            // Charge toward the crosshair (aim mode) or the mouse cursor (mouse mode), level.
            Vector3 dir = AimController.AimDirection(user, user.transform.position, out _);
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f)
            {
                dir = rb.linearVelocity; dir.y = 0f;
            }
            if (dir.sqrMagnitude < 0.01f)
            {
                // Standing still: charge where the camera looks.
                var cam = Camera.main;
                dir = cam != null ? cam.transform.forward : user.transform.forward;
                dir.y = 0f;
            }
            rb.AddForce(dir.normalized * (Force * Power(level)) + Vector3.up * 1.2f, ForceMode.VelocityChange);
            return true;
        }
    }
}
