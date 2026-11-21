using Evolve.Abilities;
using Evolve.Survival;
using UnityEngine;

namespace Evolve.Combat
{
    /// <summary>A rock thrown by a monster (Khỉ Hang): flies on the arc of <see cref="ThrowRules"/> to where the player stood when it was thrown, hurts a local player it touches, and cleans itself up.</summary>
    public class MonsterProjectile : MonoBehaviour
    {
        static Material rockMat, boneMat;
        bool bone, big;
        (float vx, float vy, float vz) v;
        Vector3 from;
        float t, flight, damage, radius;

        /// <summary>A rock (Khỉ Hang), or with <paramref name="bone"/> a spinning ivory bone shard with a pale trail (Chó Xương's spit).</summary>
        public static void Launch(Vector3 from, Vector3 to, float flightSeconds, float damage, float radius = 0.6f, bool bone = false, float size = 0f)
        {
            var go = GameObject.CreatePrimitive(bone ? PrimitiveType.Capsule : PrimitiveType.Sphere);
            Destroy(go.GetComponent<Collider>());
            go.name = bone ? "Xương Phóng" : "Đá Ném";
            go.transform.localScale = bone ? new Vector3(0.14f, 0.32f, 0.14f) : Vector3.one * (size > 0f ? size : 0.25f);
            go.transform.position = from;
            if (rockMat == null) rockMat = SkillFx.Gel(new Color(0.45f, 0.35f, 0.25f), 1f, 0.3f, 1f);
            if (boneMat == null) boneMat = SkillFx.Gel(new Color(0.9f, 0.86f, 0.74f), 1f, 0.5f, 1.6f);
            go.GetComponent<MeshRenderer>().sharedMaterial = bone ? boneMat : rockMat;
            if (bone)
            {
                var trail = go.AddComponent<TrailRenderer>();
                trail.time = 0.35f; trail.startWidth = 0.16f; trail.endWidth = 0f; trail.sharedMaterial = boneMat; trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            var p = go.AddComponent<MonsterProjectile>();
            p.bone = bone; p.big = size > 0f;
            p.from = from; p.flight = flightSeconds; p.damage = damage; p.radius = radius;
            p.v = ThrowRules.Launch(from.x, from.y, from.z, to.x, to.y, to.z, flightSeconds);
        }

        void FixedUpdate()
        {
            t += Time.fixedDeltaTime;
            var pos = ThrowRules.PositionAt(from.x, from.y, from.z, v, t);
            transform.position = new Vector3(pos.x, pos.y, pos.z);
            if (bone) transform.Rotate(540f * Time.fixedDeltaTime, 220f * Time.fixedDeltaTime, 0f, Space.Self);   // it tumbles through the air
            foreach (var p in SurvivalStats.All)
            {
                if (p == null || p.IsRemote || p.State.IsDead) continue;
                Vector3 c = p.transform.position + Vector3.up * 0.3f;
                if (!ThrowRules.Hits(pos.x, pos.y, pos.z, c.x, c.y, c.z, radius)) continue;
                p.State.Damage(damage);
                var prb = p.GetComponent<Rigidbody>();
                Impulse.Kick(prb, (p.transform.position - transform.position).normalized * 3f + Vector3.up);
                if (bone) HoundFx.ShardImpact(transform.position); else if (big) GolemFx.BoulderImpact(transform.position);
                Destroy(gameObject);
                return;
            }
            if (t >= flight + 0.1f) { if (bone) HoundFx.ShardImpact(transform.position); else if (big) GolemFx.BoulderImpact(transform.position); Destroy(gameObject); }   // it landed
        }
    }
}
