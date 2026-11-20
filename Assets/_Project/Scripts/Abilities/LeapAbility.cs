using Evolve.Player;
using UnityEngine;

namespace Evolve.Abilities
{
    /// <summary>
    /// Shared shape "dash / jump" (Lách Né; later Nhảy Cao, Vồ, Cuộn Lăn): a burst of speed toward the aim, or sideways to it,
    /// covering about <see cref="Distance"/> metres, with optional no-damage time while it lasts.
    /// </summary>
    [CreateAssetMenu(menuName = "Evolve/Abilities/Leap", fileName = "LeapSO")]
    public class LeapAbility : CaveAbility
    {
        public float Distance = 2f;
        [Tooltip("Dash at a right angle to the aim, to the side the body already drifts (right if still)")]
        public bool Sideways;
        [Tooltip("Seconds of no damage at the start of the dash")]
        public float InvulnSeconds;

        const float DashSeconds = 0.25f;

        public override bool Activate(GameObject user, int stars)
        {
            if (!user.TryGetComponent(out Rigidbody rb)) return false;
            Vector3 aim = AimController.AimDirection(user, user.transform.position, out _);
            aim.y = 0f;
            if (aim.sqrMagnitude < 0.01f) aim = user.transform.forward;
            aim.Normalize();
            Vector3 dir = aim;
            if (Sideways)
            {
                Vector3 right = Vector3.Cross(Vector3.up, aim);
                dir = Vector3.Dot(rb.linearVelocity, right) < -0.1f ? -right : right;
            }
            rb.AddForce(dir * (Distance * StarPower(stars) / DashSeconds) + Vector3.up * 0.5f, ForceMode.VelocityChange);
            if (InvulnSeconds > 0f) BodyState.On(user).TakeLessDamage(0f, InvulnSeconds);
            SkillFx.Afterimage(user, Evolve.Swallow.SkillFxMath.FamilyColor(Family), dir);
            return true;
        }
    }
}
