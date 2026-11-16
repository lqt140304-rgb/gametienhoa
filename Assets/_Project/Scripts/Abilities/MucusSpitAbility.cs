using UnityEngine;

namespace Evolve.Abilities
{
    /// <summary>Phun Dịch Nhầy (C): spit a mucus glob that slows what it hits.</summary>
    [CreateAssetMenu(menuName = "Evolve/Abilities/Mucus Spit", fileName = "MucusSpitSO")]
    public class MucusSpitAbility : AbilitySO
    {
        public float Speed = 14f;
        public float Lifetime = 2f;
        public float GlobRadius = 0.2f;
        [Range(0f, 1f)] public float SlowFactor = 0.4f;
        public float SlowDuration = 3f;
        public Color GlobColor = new Color(0.6f, 0.9f, 0.4f);

        void Reset() { DisplayName = "Phun Dịch Nhầy"; EnergyCost = 15f; Cooldown = 3f; }

        public override bool Activate(GameObject user)
        {
            Vector3 dir = user.transform.forward;
            if (Camera.main != null) { dir = Camera.main.transform.forward; dir.y = Mathf.Max(dir.y, 0.05f); }
            dir.Normalize();

            float userRadius = user.TryGetComponent(out Collider c) ? c.bounds.extents.magnitude : 0.5f;
            Vector3 spawn = user.transform.position + dir * (userRadius + GlobRadius + 0.05f);

            var glob = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            glob.name = "MucusGlob";
            glob.transform.SetPositionAndRotation(spawn, Quaternion.identity);
            glob.transform.localScale = Vector3.one * GlobRadius * 2f;
            if (glob.TryGetComponent(out Renderer r)) r.material.color = GlobColor;

            var rb = glob.AddComponent<Rigidbody>();
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.linearVelocity = dir * Speed;

            var proj = glob.AddComponent<MucusProjectile>();
            proj.Init(user, SlowFactor, SlowDuration, Lifetime);
            return true;
        }
    }
}
