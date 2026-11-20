using UnityEngine;

namespace Evolve.Creatures
{
    /// <summary>
    /// Moves a kinematic creature over real colliders: reads the floor with a ray that starts only a step above its feet
    /// (so a bridge overhead or a tall platform is never "the ground"), stops at walls and ledges its species cannot take
    /// (<see cref="MovementRules"/>), tries a detour, and eases its height so floor bumps do not jolt it.
    /// Prey and Predator add it themselves. Turn <see cref="Enabled"/> off to get the old height-snapping walk back.
    /// </summary>
    [DisallowMultipleComponent]
    public class CreatureMotor : MonoBehaviour
    {
        public static bool Enabled = true;

        public MoveProfile Profile;
        Rigidbody rb;
        float footOffset, radius;
        readonly RaycastHit[] hits = new RaycastHit[8];

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            Profile = MovementRules.For(name);
            var col = GetComponent<Collider>();
            radius = col != null ? Mathf.Clamp(Mathf.Max(col.bounds.extents.x, col.bounds.extents.z), 0.1f, 2f) : 0.3f;
            Vector3 p = transform.position;
            footOffset = Floor(p, p.y + 1f, 6f, out float g) ? p.y - g : World.GroundHeight.OffsetOf(p);
        }

        /// <summary>Move along flat <paramref name="dir"/> at <paramref name="speed"/> for one physics step. False when every way was blocked.</summary>
        public bool Move(Vector3 dir, float speed, bool detour = true)
        {
            Vector3 pos = rb.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 1e-6f || speed <= 0f) return true;
            dir.Normalize();
            float step = speed * Time.fixedDeltaTime;

            if (!Enabled)
            {
                rb.MovePosition(World.GroundHeight.Follow(pos + dir * step, footOffset));
                return true;
            }

            float footY = pos.y - footOffset;
            int tries = detour ? MovementRules.DetourAngles.Length : 1;
            for (int i = 0; i < tries; i++)
            {
                Vector3 d = Quaternion.Euler(0f, MovementRules.DetourAngles[i], 0f) * dir;
                if (Wall(pos, footY, d, step)) continue;
                Vector3 next = pos + d * step;
                float groundY = Floor(next, footY + Profile.MaxStepUp, Profile.MaxStepUp + Profile.MaxDrop + 0.2f, out float g)
                    ? g : World.GroundHeight.At(next.x, next.z, footY);
                if (!MovementRules.CanEnter(footY, groundY, Profile)) continue;
                next.y = MovementRules.SmoothY(pos.y, groundY + footOffset, Time.fixedDeltaTime);
                rb.MovePosition(next);
                return true;
            }
            return false;
        }

        /// <summary>Something too tall to step onto just ahead (a sphere swept above step height).</summary>
        bool Wall(Vector3 pos, float footY, Vector3 dir, float step)
        {
            float r = radius * 0.7f;
            Vector3 from = new Vector3(pos.x, footY + Profile.MaxStepUp + r + 0.02f, pos.z);
            int n = Physics.SphereCastNonAlloc(from, r, dir, hits, step + radius * 0.4f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var h = hits[i];
                if (h.collider.attachedRigidbody != null) continue;     // other creatures and the slime are not walls
                if (h.distance <= 0f) continue;                          // already overlapping (bad spawn): let it walk out
                if (h.normal.y > 0.7f) continue;                         // a slope it can walk up
                return true;
            }
            return false;
        }

        /// <summary>Highest static surface below <paramref name="fromY"/> within <paramref name="length"/>.</summary>
        bool Floor(Vector3 at, float fromY, float length, out float y)
        {
            y = 0f;
            int n = Physics.RaycastNonAlloc(new Vector3(at.x, fromY, at.z), Vector3.down, hits, length, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue;
            for (int i = 0; i < n; i++)
                if (hits[i].collider.attachedRigidbody == null && hits[i].distance < best) best = hits[i].distance;
            if (best == float.MaxValue) return false;
            y = fromY - best;
            return true;
        }
    }
}
