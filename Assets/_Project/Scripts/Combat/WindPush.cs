using Evolve.Survival;
using UnityEngine;

namespace Evolve.Combat
{
    /// <summary>A wind gust moving a slime (Gió Hú): shifts it a little each physics step while refreshed, stops at a wall, and removes itself.</summary>
    public class WindPush : MonoBehaviour
    {
        Vector3 velocity;
        float until;
        Rigidbody rb;

        /// <summary>Push <paramref name="slime"/> at <paramref name="velocity"/> (flat) for the next <paramref name="seconds"/>; call again each step to keep it going.</summary>
        public static void Apply(SurvivalStats slime, Vector3 velocity, float seconds)
        {
            if (slime == null) return;
            if (!slime.TryGetComponent(out WindPush w)) w = slime.gameObject.AddComponent<WindPush>();
            w.velocity = velocity; w.until = Time.time + seconds;
            w.rb = slime.GetComponent<Rigidbody>();
        }

        void FixedUpdate()
        {
            if (rb == null || Time.time >= until) { Destroy(this); return; }
            float d = velocity.magnitude * Time.fixedDeltaTime;
            if (d <= 0f) return;
            Vector3 dir = velocity.normalized;
            if (!rb.SweepTest(dir, out _, d + 0.02f, QueryTriggerInteraction.Ignore)) rb.MovePosition(rb.position + dir * d);   // a wall stops it
        }
    }
}
