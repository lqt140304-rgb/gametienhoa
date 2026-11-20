using System.Collections.Generic;
using Evolve.Creatures;
using Evolve.Survival;
using UnityEngine;

namespace Evolve.Abilities
{
    /// <summary>A patch of crystal spikes (Cắm Gai). Each creature that steps in is hurt once (and stunned at ★3); the patch melts away after its time.</summary>
    public class SpikeTrap : MonoBehaviour
    {
        float radius, damage, stun, until;
        SurvivalStats owner;
        readonly HashSet<Component> hurt = new HashSet<Component>();

        public static SpikeTrap Spawn(Vector3 at, float radius, float damage, float seconds, float stun, SurvivalStats owner)
        {
            var go = new GameObject("SpikeTrap");
            go.transform.position = at;
            // placeholder look: a ring of small purple cones (primitive cylinders, no colliders)
            for (int i = 0; i < 6; i++)
            {
                var spike = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Destroy(spike.GetComponent<Collider>());
                spike.transform.SetParent(go.transform, false);
                float a = i * Mathf.PI / 3f;
                spike.transform.localPosition = new Vector3(Mathf.Cos(a), 0.15f, Mathf.Sin(a)) * radius * 0.5f;
                spike.transform.localScale = new Vector3(0.08f, 0.15f, 0.08f);
                spike.GetComponent<Renderer>().material.color = new Color(0.62f, 0.38f, 0.95f);
            }
            var trap = go.AddComponent<SpikeTrap>();
            trap.radius = radius; trap.damage = damage; trap.stun = stun; trap.owner = owner; trap.until = Time.time + seconds;
            return trap;
        }

        void Update()
        {
            if (Time.time >= until) { Destroy(gameObject); return; }
            Vector3 c = transform.position;
            foreach (var p in Predator.Active) Check(p, c);
            foreach (var p in Prey.Active) Check(p, c);
        }

        void Check(Component creature, Vector3 c)
        {
            if (creature == null || hurt.Contains(creature)) return;
            Vector3 d = creature.transform.position - c; d.y = 0f;
            if (d.sqrMagnitude > radius * radius) return;
            hurt.Add(creature);
            if (stun > 0f) CreatureStatus.Stun(creature, stun);
            if (creature is IDamageable target) target.TakeDamage(damage, owner);
        }
    }
}
