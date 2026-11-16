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
            if (pulsePrefab == null || Time.time < next) return;
            next = Time.time + interval;
            foreach (var n in FindObjectsByType<Nutrient>(FindObjectsSortMode.None))
            {
                if ((n.transform.position - transform.position).sqrMagnitude > radius * radius) continue;
                var p = Instantiate(pulsePrefab, n.transform.position, Quaternion.identity);
                Destroy(p.gameObject, p.main.duration + p.main.startLifetime.constantMax);
            }
        }
    }
}
