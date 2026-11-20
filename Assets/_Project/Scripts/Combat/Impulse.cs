namespace Evolve.Combat
{
    /// <summary>Velocity kicks that skip a kinematic body (ghost mode, frozen): setting velocity on one only logs a warning every time.</summary>
    public static class Impulse
    {
        public static void Kick(UnityEngine.Rigidbody rb, UnityEngine.Vector3 velocityChange)
        {
            if (rb != null && !rb.isKinematic) rb.AddForce(velocityChange, UnityEngine.ForceMode.VelocityChange);
        }
    }
}
