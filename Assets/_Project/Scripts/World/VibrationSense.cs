using Evolve.Creatures;
using UnityEngine;

namespace Evolve.World
{
    /// <summary>
    /// Substitute sense for the eyeless stage: periodically emits a particle burst
    /// at every nearby Nutrient (cyan), Prey (red) and Predator (purple) so the player can "feel" them through the fog.
    /// Assign a ParticleSystem prefab (short burst, not looping).
    /// </summary>
    public class VibrationSense : MonoBehaviour
    {
        [SerializeField] float radius = 25f;
        [SerializeField] float interval = 3f;
        [SerializeField] ParticleSystem pulsePrefab;
        [SerializeField] Color foodColor = new Color(0.5f, 0.9f, 1f);
        [SerializeField] Color preyColor = new Color(1f, 0.4f, 0.35f);
        [SerializeField] Color predatorColor = new Color(0.75f, 0.35f, 1f);

        float next;

        void Update()
        {
            if (pulsePrefab == null || Time.time < next) return;
            next = Time.time + interval;
            foreach (var n in Nutrient.Active) Pulse(n.transform.position, foodColor);
            foreach (var p in Prey.Active) Pulse(p.transform.position, preyColor);
            foreach (var p in Predator.Active) Pulse(p.transform.position + Vector3.up * 0.5f, predatorColor);
        }

        void Pulse(Vector3 at, Color c)
        {
            if ((at - transform.position).sqrMagnitude > radius * radius) return;
            var p = Instantiate(pulsePrefab, at, Quaternion.identity);
            var main = p.main;
            main.startColor = c;
            Destroy(p.gameObject, main.duration + main.startLifetime.constantMax);
        }
    }
}
