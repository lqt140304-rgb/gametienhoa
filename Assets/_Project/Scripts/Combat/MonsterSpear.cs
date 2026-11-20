using Evolve.Survival;
using UnityEngine;

namespace Evolve.Combat
{
    /// <summary>
    /// A wind spear thrown by Cáo Gió: flies straight along the direction it was thrown in (toward where the player stood at that moment),
    /// stops at a wall, and on touching a local player deals heavy damage and throws them back (<see cref="WindPush"/>).
    /// </summary>
    public class MonsterSpear : MonoBehaviour
    {
        const float HitRadius = 0.7f, MaxDistance = 24f;
        Vector3 dir;
        float speed, damage, force, travelled;

        /// <summary>Detaches the circling spear <paramref name="visual"/> and sends it toward <paramref name="aimAt"/>.</summary>
        public static void Launch(Transform visual, Vector3 aimAt, float speed, float damage, float force)
        {
            visual.SetParent(null, true);
            Vector3 d = aimAt - visual.position;
            if (d.sqrMagnitude < 1e-4f) d = visual.forward;
            var s = visual.gameObject.AddComponent<MonsterSpear>();
            s.dir = d.normalized; s.speed = speed; s.damage = damage; s.force = force;
            visual.rotation = Quaternion.FromToRotation(Vector3.up, s.dir);   // the capsule's long axis is Y: point it along the flight
        }

        static Vector3 ClosestOnSegment(Vector3 a, Vector3 b, Vector3 p)
        {
            Vector3 ab = b - a; float len2 = ab.sqrMagnitude;
            return len2 < 1e-8f ? a : a + ab * Mathf.Clamp01(Vector3.Dot(p - a, ab) / len2);
        }

        void FixedUpdate()
        {
            float step = speed * Time.fixedDeltaTime;
            Vector3 from = transform.position, to = from + dir * step;
            if (Physics.Raycast(from, dir, out var wall, step, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) && wall.collider.attachedRigidbody == null) { Destroy(gameObject); return; }   // a wall stops it
            transform.position = to;
            travelled += step;
            foreach (var p in SurvivalStats.All)
            {
                if (p == null || p.IsRemote || p.State.IsDead) continue;
                Vector3 c = p.transform.position + Vector3.up * 0.3f;
                if ((ClosestOnSegment(from, to, c) - c).sqrMagnitude > HitRadius * HitRadius) continue;
                p.State.Damage(damage);
                Vector3 flat = dir; flat.y = 0f;
                WindPush.Apply(p, flat.normalized * force, 0.35f);   // thrown back along the flight
                Destroy(gameObject);
                return;
            }
            if (travelled >= MaxDistance) Destroy(gameObject);
        }
    }
}
