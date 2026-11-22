using System;
using UnityEngine;
using Evolve.Survival;
using Evolve.Abilities;

namespace Evolve.Combat
{
    /// <summary>
    /// The thrown axe: the real axe object flies on the arc of <see cref="ThrowRules"/> to where the player is going, spinning end over end, hurts a local player it touches, and stops where it hits a wall, the floor or a pillar
    /// (or at the end of its flight). Then it calls back with where it came to rest and whether that spot can be reached (a pit or a place off the map cannot).
    /// </summary>
    public class AxeFlight : MonoBehaviour
    {
        (float vx, float vy, float vz) v;
        Vector3 from, prev, right;
        float t, flight, damage;
        Action<Vector3, bool, GameObject> onLand;

        public static void Launch(GameObject axe, Vector3 to, float flightSeconds, float damage, Action<Vector3, bool, GameObject> onLand)
        {
            axe.transform.SetParent(null, true);
            var f = axe.AddComponent<AxeFlight>();
            f.from = f.prev = axe.transform.position; f.flight = Mathf.Max(0.2f, flightSeconds); f.damage = damage; f.onLand = onLand;
            Vector3 d = to - f.from; d.y = 0f;
            f.right = d.sqrMagnitude > 1e-4f ? Vector3.Cross(Vector3.up, d.normalized) : Vector3.right;
            f.v = ThrowRules.Launch(f.from.x, f.from.y, f.from.z, to.x, to.y, to.z, f.flight);
        }

        void FixedUpdate()
        {
            t += Time.fixedDeltaTime;
            var p = ThrowRules.PositionAt(from.x, from.y, from.z, v, t);
            Vector3 now = new Vector3(p.x, p.y, p.z), step = now - prev;
            transform.rotation = Quaternion.AngleAxis(720f * t / flight, right) * Quaternion.identity;   // end over end
            foreach (var pl in SurvivalStats.All)
            {
                if (pl == null || pl.IsRemote || pl.State.IsDead) continue;
                Vector3 c = pl.transform.position + Vector3.up * 0.3f;
                if (!ThrowRules.Hits(p.x, p.y, p.z, c.x, c.y, c.z, 1.2f)) continue;
                pl.State.Damage(damage);
                Impulse.Kick(pl.GetComponent<Rigidbody>(), (pl.transform.position - transform.position).normalized * 6f + Vector3.up * 2f);
                Land(pl.transform.position); return;
            }
            if (step.sqrMagnitude > 1e-6f && Physics.SphereCast(prev, 0.5f, step.normalized, out var hit, step.magnitude, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) && hit.collider.attachedRigidbody == null)
            { Land(hit.point + hit.normal * 1.0f); return; }
            prev = now; transform.position = now;
            if (t >= flight + 0.1f) Land(now);
        }

        /// <summary>It came down at <paramref name="point"/>: the ground below it (within 8 m) is where it stands; none (a pit, off the map) means it cannot be reached.</summary>
        void Land(Vector3 point)
        {
            bool ok = Physics.Raycast(point + Vector3.up * 2f, Vector3.down, out var down, 10f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            var cb = onLand; onLand = null;
            cb?.Invoke(ok ? down.point : point, ok, gameObject);
        }
    }
}
