using UnityEngine;

namespace Evolve.Abilities
{
    /// <summary>Mucus glob: applies Slowed to the first Rigidbody it hits, then splats.</summary>
    public class MucusProjectile : MonoBehaviour
    {
        GameObject owner;
        float slowFactor, slowDuration;

        public void Init(GameObject owner, float slowFactor, float slowDuration, float lifetime)
        {
            this.owner = owner;
            this.slowFactor = slowFactor;
            this.slowDuration = slowDuration;
            if (owner.TryGetComponent(out Collider oc) && TryGetComponent(out Collider mine))
                Physics.IgnoreCollision(oc, mine);
            Destroy(gameObject, lifetime);
        }

        void OnCollisionEnter(Collision c)
        {
            Rigidbody target = c.rigidbody;
            if (target != null && target.gameObject != owner)
            {
                if (!target.TryGetComponent(out Slowed s)) s = target.gameObject.AddComponent<Slowed>();
                s.Apply(slowFactor, slowDuration);
            }
            Destroy(gameObject);
        }
    }
}
