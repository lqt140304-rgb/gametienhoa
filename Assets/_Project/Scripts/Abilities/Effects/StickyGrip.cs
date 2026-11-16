using UnityEngine;

namespace Evolve.Abilities
{
    /// <summary>
    /// Runtime effect of Bám Dính: boosts absorption and glues any dynamic Rigidbody
    /// the slime touches (future prey) with a FixedJoint until the effect ends.
    /// </summary>
    public class StickyGrip : MonoBehaviour
    {
        float endsAt = -1f;
        float multiplier = 1f;
        FixedJoint joint;

        public bool Active => Time.time < endsAt;

        public static float AbsorbMultiplierFor(GameObject user) =>
            user.TryGetComponent(out StickyGrip g) && g.Active ? g.multiplier : 1f;

        public void Begin(float duration, float absorbMultiplier)
        {
            endsAt = Time.time + duration;
            multiplier = absorbMultiplier;
        }

        void Update()
        {
            if (!Active && joint != null) Release();
        }

        void OnCollisionEnter(Collision c)
        {
            if (!Active || joint != null) return;
            Rigidbody other = c.rigidbody;
            if (other == null || other.isKinematic || other.gameObject == gameObject) return;
            joint = gameObject.AddComponent<FixedJoint>();
            joint.connectedBody = other;
        }

        void Release()
        {
            Destroy(joint);
            joint = null;
        }

        void OnDisable() { if (joint != null) Release(); }
    }
}
