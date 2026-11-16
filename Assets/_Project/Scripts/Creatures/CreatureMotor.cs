using Evolve.Abilities;
using Evolve.Survival;
using UnityEngine;

namespace Evolve.Creatures
{
    /// <summary>
    /// Shared movement for simple creatures: steers a Rigidbody toward a desired
    /// horizontal direction, wanders around its spawn point, and respects Slowed.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class CreatureMotor : MonoBehaviour
    {
        public float WalkSpeed = 1.5f;
        public float RunSpeed = 4f;
        public float Acceleration = 10f;
        public float WanderRadius = 8f;

        Rigidbody rb;
        Vector3 home;
        Vector3 wanderTarget;
        float nextWanderPick;

        public Vector3 Home => home;
        Vector3 desiredDir;
        float desiredSpeed;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.freezeRotation = true;
            home = transform.position;
            wanderTarget = home;
        }

        public void Move(Vector3 dir, bool run)
        {
            dir.y = 0f;
            desiredDir = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.zero;
            desiredSpeed = run ? RunSpeed : WalkSpeed;
        }

        public void Wander()
        {
            if (Time.time >= nextWanderPick || Flat(wanderTarget - transform.position).sqrMagnitude < 0.5f)
            {
                Vector2 r = Random.insideUnitCircle * WanderRadius;
                wanderTarget = home + new Vector3(r.x, 0f, r.y);
                nextWanderPick = Time.time + Random.Range(3f, 6f);
            }
            Move(wanderTarget - transform.position, false);
        }

        public void Stop() => Move(Vector3.zero, false);

        public void ResetTo(Vector3 position)
        {
            rb.position = position;
            rb.linearVelocity = Vector3.zero;
            wanderTarget = position;
        }

        void FixedUpdate()
        {
            if (rb.isKinematic) return;
            float mult = TryGetComponent(out Slowed s) ? s.SpeedMultiplier : 1f;
            Vector3 v = rb.linearVelocity;
            Vector3 target = desiredDir * desiredSpeed * mult;
            Vector3 h = Vector3.MoveTowards(new Vector3(v.x, 0f, v.z), target, Acceleration * Time.fixedDeltaTime);
            rb.linearVelocity = new Vector3(h.x, v.y, h.z);
            if (h.sqrMagnitude > 0.05f) transform.rotation = Quaternion.LookRotation(h);
        }

        /// <summary>Closest living slime within range, or null.</summary>
        public static SurvivalStats NearestSlime(Vector3 from, float range, bool skipHidden)
        {
            SurvivalStats best = null;
            float bestSq = range * range;
            foreach (var s in FindObjectsByType<SurvivalStats>(FindObjectsSortMode.None))
            {
                if (s.State.IsDead) continue;
                if (skipHidden && PlayingDead.IsHidden(s.gameObject)) continue;
                float d = (s.transform.position - from).sqrMagnitude;
                if (d < bestSq) { bestSq = d; best = s; }
            }
            return best;
        }

        public static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }
    }
}
