using Evolve.Creatures;
using Evolve.Survival;
using UnityEngine;

namespace Evolve.Abilities
{
    /// <summary>
    /// A spike flying straight. It hurts the first creature (Predator or Prey) it touches and vanishes on anything else
    /// (ground, walls) or at the end of its range. Created at runtime by SpikeBurstAbility, no prefab needed.
    /// </summary>
    public class SpikeProjectile : MonoBehaviour
    {
        const float Radius = 0.3f; // generous, so small creatures are not missed

        Vector3 direction;
        float speed, maxDistance, damage, travelled;
        SurvivalStats owner;
        Transform ownerRoot;

        public static void Spawn(Vector3 position, Vector3 direction, float speed, float range, float damage, SurvivalStats owner)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "Spike";
            go.layer = 2; // Ignore Raycast, so aiming rays and other casts pass through it
            DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetPositionAndRotation(position, Quaternion.FromToRotation(Vector3.up, direction));
            go.transform.localScale = new Vector3(0.08f, 0.25f, 0.08f);

            var color = new Color(0.75f, 0.4f, 1f);
            var mat = go.GetComponent<Renderer>().material;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color); else mat.color = color;

            // A small glow so the spike is visible in the dark while the character is blind.
            var glow = new GameObject("Glow").AddComponent<Light>();
            glow.transform.SetParent(go.transform, false);
            glow.type = LightType.Point;
            glow.color = color;
            glow.range = 3f;
            glow.intensity = 3f;
            glow.shadows = LightShadows.None;

            var p = go.AddComponent<SpikeProjectile>();
            p.direction = direction.normalized;
            p.speed = speed;
            p.maxDistance = range;
            p.damage = damage;
            p.owner = owner;
            p.ownerRoot = owner != null ? owner.transform : null;
        }

        void Update()
        {
            float step = speed * Time.deltaTime;
            if (Physics.SphereCast(transform.position, Radius, direction, out RaycastHit hit, step + 0.05f,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                && !(ownerRoot != null && hit.transform.IsChildOf(ownerRoot)))
            {
                var predator = hit.collider.GetComponentInParent<Predator>();
                if (predator != null) predator.TakeDamage(damage, owner);
                else
                {
                    var prey = hit.collider.GetComponentInParent<Prey>();
                    if (prey != null) prey.TakeDamage(damage, owner);
                }
                Destroy(gameObject);
                return;
            }

            transform.position += direction * step;
            travelled += step;
            if (travelled >= maxDistance) Destroy(gameObject);
        }
    }
}
