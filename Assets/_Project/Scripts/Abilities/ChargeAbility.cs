using Evolve.Player;
using Evolve.Visuals;
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

        internal static void Prepare(GameObject user, float delay)
        {
            var animator = user.GetComponentInChildren<SlimeGelAnimator>();
            if (animator != null && user.TryGetComponent(out Rigidbody rb))
                animator.PrepareCharge(Direction(user, rb), delay);
        }

        public override bool Activate(GameObject user, int level)
        {
            if (!user.TryGetComponent(out Rigidbody rb)) return false;
            Vector3 direction = Direction(user, rb);
            rb.AddForce(direction * (Force * Power(level)) + Vector3.up * 1.2f, ForceMode.VelocityChange);
            var animator = user.GetComponentInChildren<SlimeGelAnimator>();
            if (animator != null) animator.LaunchCharge(direction);
            if (direction.sqrMagnitude > 0.01f)
            {
                if (!user.TryGetComponent(out ChargeGelContact contact)) contact = user.AddComponent<ChargeGelContact>();
                contact.Arm(direction, animator);
            }
            return true;
        }

        static Vector3 Direction(GameObject user, Rigidbody rb)
        {
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
            return dir.normalized;
        }
    }
}
