using Evolve.Abilities;
using Evolve.Survival;
using UnityEngine;

namespace Evolve.Combat
{
    /// <summary>A rock thrown by a monster (Khỉ Hang): flies on the arc of <see cref="ThrowRules"/> to where the player stood when it was thrown, hurts a local player it touches, and cleans itself up.</summary>
    public class MonsterProjectile : MonoBehaviour
    {
        static Material rockMat, boneMat;
        bool bone, big, spearBody;
        (float vx, float vy, float vz) v;
        Vector3 from;
        float t, flight, damage, radius, bodyRadius;
        Vector3 prev;

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
            p.prev = from; p.bodyRadius = bone ? 0.1f : (size > 0f ? size * 0.4f : 0.12f);
            p.from = from; p.flight = flightSeconds; p.damage = damage; p.radius = radius;
            p.v = ThrowRules.Launch(from.x, from.y, from.z, to.x, to.y, to.z, flightSeconds);
        }

        /// <summary>
        /// Throws an existing object (the stone spike the golem pulled out of the ground) as the missile itself: no stand-in sphere. It keeps its size, flies on the same arc,
        /// points its tip along the flight and turns slowly about that axis like a javelin, and bursts where it lands or hits.
        /// </summary>
        public static void LaunchBody(GameObject body, Vector3 to, float flightSeconds, float damage, float radius = 1f)
        {
            body.transform.SetParent(null, true);   // keeps its world size and place
            Vector3 from = body.transform.position;
            var p = body.AddComponent<MonsterProjectile>();
            p.spearBody = true; p.big = true; p.prev = from; p.bodyRadius = Mathf.Max(0.2f, body.transform.lossyScale.x * 0.5f);
            p.from = from; p.flight = flightSeconds; p.damage = damage; p.radius = radius;
            p.v = ThrowRules.Launch(from.x, from.y, from.z, to.x, to.y, to.z, flightSeconds);
        }

        /// <summary>A heavy boulder that lands (rather than hits) still hurts everyone near the crater: 60% of its damage within 2.5 m.</summary>
        void Splash(Vector3 at)
        {
            foreach (var p in SurvivalStats.All)
            {
                if (p == null || p.IsRemote || p.State.IsDead) continue;
                if ((p.transform.position - at).sqrMagnitude > 2.5f * 2.5f) continue;
                p.State.Damage(damage * 0.6f);
                Impulse.Kick(p.GetComponent<Rigidbody>(), (p.transform.position - at).normalized * 5f + Vector3.up * 2f);
            }
        }

        void FixedUpdate()
        {
            t += Time.fixedDeltaTime;
            var pos = ThrowRules.PositionAt(from.x, from.y, from.z, v, t);
            Vector3 now = new Vector3(pos.x, pos.y, pos.z), step = now - prev;
            if (step.sqrMagnitude > 1e-6f && Physics.SphereCast(prev, bodyRadius, step.normalized, out var wall, step.magnitude, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) && wall.collider.attachedRigidbody == null)
            {
                transform.position = wall.point + wall.normal * 0.2f;   // it struck a wall, the floor or a pillar (not a creature or the player: those have bodies): it lands there and bursts
                if (bone) HoundFx.ShardImpact(transform.position); else if (big) { GolemFx.BoulderImpact(transform.position); Splash(transform.position); }
                Destroy(gameObject);
                return;
            }
            if (spearBody && step.sqrMagnitude > 1e-8f)   // tip first along the flight, turning about its own axis
                transform.rotation = Quaternion.AngleAxis(300f * t, step.normalized) * Quaternion.FromToRotation(Vector3.up, step.normalized);
            prev = now;
            transform.position = now;
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
            if (t >= flight + 0.1f) { if (bone) HoundFx.ShardImpact(transform.position); else if (big) { GolemFx.BoulderImpact(transform.position); Splash(transform.position); } Destroy(gameObject); }   // it landed
        }
    }
}
