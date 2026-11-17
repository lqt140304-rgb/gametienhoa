using UnityEngine;

namespace Evolve.Creatures
{
    /// <summary>Keeps a number of predators alive in their territory, respawning after a delay.</summary>
    public class PredatorSpawner : MonoBehaviour
    {
        [SerializeField] Predator prefab;
        [SerializeField] int count = 1;
        [Tooltip("Territory radius around this object")]
        [SerializeField] float radius = 10f;
        [SerializeField] float respawnSeconds = 60f;

        int alive;
        float nextSpawn;

        void Start()
        {
            if (prefab == null)
            {
                Debug.LogError("PredatorSpawner: chưa gán Prefab. Chạy lại Evolve → Build Prototype Scene.", this);
                return;
            }
            for (int i = 0; i < count; i++) SpawnOne();
        }

        void Update()
        {
            if (prefab != null && alive < count && Time.time >= nextSpawn) SpawnOne();
        }

        void SpawnOne()
        {
            Vector2 c = Random.insideUnitCircle * radius * 0.6f;
            Vector3 pos = transform.position + new Vector3(c.x, prefab.transform.position.y, c.y);
            var p = Instantiate(prefab, pos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), transform);
            p.HomeCenter = transform.position;
            p.HomeRadius = radius;
            p.Killed += _ => { alive--; nextSpawn = Time.time + respawnSeconds; };
            alive++;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.6f, 0.2f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
