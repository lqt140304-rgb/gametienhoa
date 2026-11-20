using Evolve.Abilities;
using Evolve.Survival;
using UnityEngine;

namespace Evolve.Combat
{
    /// <summary>A rock thrown by a monster (Khỉ Hang): flies on the arc of <see cref="ThrowRules"/> to where the player stood when it was thrown, hurts a local player it touches, and cleans itself up.</summary>
    public class MonsterProjectile : MonoBehaviour
    {
        static Material rockMat;
        (float vx, float vy, float vz) v;
        Vector3 from;
        float t, flight, damage, radius;

        public static void Launch(Vector3 from, Vector3 to, float flightSeconds, float damage, float radius = 0.6f)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(go.GetComponent<Collider>());
            go.name = "Đá Ném";
            go.transform.localScale = Vector3.one * 0.25f;
            go.transform.position = from;
            if (rockMat == null) rockMat = SkillFx.Gel(new Color(0.45f, 0.35f, 0.25f), 1f, 0.3f, 1f);
            go.GetComponent<MeshRenderer>().sharedMaterial = rockMat;
            var p = go.AddComponent<MonsterProjectile>();
            p.from = from; p.flight = flightSeconds; p.damage = damage; p.radius = radius;
            p.v = ThrowRules.Launch(from.x, from.y, from.z, to.x, to.y, to.z, flightSeconds);
        }

        void FixedUpdate()
        {
            t += Time.fixedDeltaTime;
            var pos = ThrowRules.PositionAt(from.x, from.y, from.z, v, t);
            transform.position = new Vector3(pos.x, pos.y, pos.z);
            foreach (var p in SurvivalStats.All)
            {
                if (p == null || p.IsRemote || p.State.IsDead) continue;
                Vector3 c = p.transform.position + Vector3.up * 0.3f;
                if (!ThrowRules.Hits(pos.x, pos.y, pos.z, c.x, c.y, c.z, radius)) continue;
                p.State.Damage(damage);
                var prb = p.GetComponent<Rigidbody>();
                Impulse.Kick(prb, (p.transform.position - transform.position).normalized * 3f + Vector3.up);
                Destroy(gameObject);
                return;
            }
            if (t >= flight + 0.1f) Destroy(gameObject);   // it landed
        }
    }
}
