using UnityEngine;

namespace Evolve.Abilities
{
    /// <summary>
    /// Slows a Rigidbody by capping its horizontal speed. Future creature AI should
    /// also read SpeedMultiplier when choosing its own move speed.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Slowed : MonoBehaviour
    {
        float factor = 1f;
        float endsAt = -1f;
        Rigidbody rb;
        float prevSpeed;

        public float SpeedMultiplier => Time.time < endsAt ? factor : 1f;

        void Awake() => rb = GetComponent<Rigidbody>();

        public void Apply(float speedFactor, float duration)
        {
            factor = Mathf.Clamp01(speedFactor);
            endsAt = Mathf.Max(endsAt, Time.time + duration);
            prevSpeed = HorizontalSpeed();
        }

        void FixedUpdate()
        {
            if (Time.time >= endsAt) { Destroy(this); return; }
            // Allow at most `factor` of the speed the body had when hit (or now, if slower).
            float cap = Mathf.Max(prevSpeed * factor, 0.5f);
            Vector3 v = rb.linearVelocity;
            Vector3 h = new Vector3(v.x, 0f, v.z);
            if (h.magnitude > cap) { h = h.normalized * cap; rb.linearVelocity = new Vector3(h.x, v.y, h.z); }
        }

        float HorizontalSpeed() { Vector3 v = rb.linearVelocity; v.y = 0f; return v.magnitude; }
    }
}
