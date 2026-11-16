using UnityEngine;

namespace Evolve.World
{
    /// <summary>
    /// Substitute sense for the eyeless stage: periodically emits a particle burst
    /// at every nearby Nutrient so the player can "feel" food through the fog.
    /// Assign a ParticleSystem prefab (short burst, not looping).
    /// </summary>
    public class VibrationSense : MonoBehaviour
    {
        [SerializeField] float radius = 25f;
        [SerializeField] float interval = 3f;
        [SerializeField] ParticleSystem pulsePrefab;

        float next;

        void Update()
        {
            if (Time.time < next) return;
            next = Time.time + interval;
            Pulse(radius);  // passive sense: food only
        }

        /// <summary>Emit one pulse revealing food (and creatures if asked) within r. Rung Màng passes true.</summary>
        public void Pulse(float r, bool includeCreatures = false)
        {
            if (pulsePrefab == null) return;
            foreach (var n in FindObjectsByType<Nutrient>(FindObjectsSortMode.None))
                Emit(n.transform.position, r);
            if (!includeCreatures) return;
            foreach (var c in FindObjectsByType<Evolve.Creatures.CreatureMotor>(FindObjectsSortMode.None))
            {
                if (c.TryGetComponent(out Evolve.Creatures.Prey prey) && !prey.Alive) continue;
                Emit(c.transform.position, r);
            }
        }

        void Emit(Vector3 at, float r)
        {
            if ((at - transform.position).sqrMagnitude > r * r) return;
            var p = Instantiate(pulsePrefab, at, Quaternion.identity);
            Destroy(p.gameObject, p.main.duration + p.main.startLifetime.constantMax);
        }
    }
}
