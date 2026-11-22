using Evolve.Survival;
using Evolve.Swallow;
using Evolve.Visuals;
using UnityEngine;

namespace Evolve.Abilities
{
    /// <summary>Plays the visual reaction to the first solid forward contact after a charge.</summary>
    sealed class ChargeGelContact : MonoBehaviour
    {
        float armedUntil = -1f;
        Vector3 direction;
        SlimeGelAnimator animator;
        SurvivalStats stats;
        Rigidbody body;
        SlimeGelTint tint;

        void Awake()
        {
            stats = GetComponent<SurvivalStats>();
            body = GetComponent<Rigidbody>();
            tint = GetComponentInChildren<SlimeGelTint>();
        }

        public void Arm(Vector3 forward, SlimeGelAnimator gel)
        {
            direction = forward;
            animator = gel;
            armedUntil = Time.time + SlimeGelAnimator.ChargeDriveSeconds;
        }

        void OnCollisionEnter(Collision collision) => CheckContact(collision);
        void OnCollisionStay(Collision collision) => CheckContact(collision);

        void CheckContact(Collision collision)
        {
            if (Time.time >= armedUntil || (stats != null && stats.State.IsDead)) return;
            var relativeVelocity = collision.relativeVelocity;

            ContactPoint best = default;
            float bestFacing = 0f;
            for (int i = 0; i < collision.contactCount; i++)
            {
                var contact = collision.GetContact(i);
                float facing = Vector3.Dot(direction, -contact.normal);
                if (facing <= bestFacing || !SkillFxMath.IsChargeContact(facing, contact.normal.y)) continue;
                float closingSpeed = Mathf.Abs(Vector3.Dot(relativeVelocity, contact.normal));
                float normalImpulse = Mathf.Abs(Vector3.Dot(collision.impulse, contact.normal));
                if (closingSpeed < 1f && (body == null || normalImpulse < body.mass)) continue;
                best = contact;
                bestFacing = facing;
            }
            if (bestFacing <= 0f) return;

            armedUntil = -1f;
            if (animator != null) animator.HitCharge(-best.normal);
            var across = Vector3.Cross(Vector3.up, best.normal).normalized;
            var color = tint != null ? tint.CoreColor : new Color(0.35f, 0.85f, 0.65f);
            SkillFx.ChargeDroplets(best.point, best.normal, across, color);
        }

        void OnDisable() => armedUntil = -1f;
    }
}
