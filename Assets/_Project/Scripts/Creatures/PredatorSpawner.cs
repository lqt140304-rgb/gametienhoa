using UnityEngine;

namespace Evolve.Creatures
{
    /// <summary>Keeps a number of predators alive in their territory, respawning after a delay.</summary>
    public class PredatorSpawner : MonoBehaviour
    {
        [SerializeField] Predator prefab;
        /// <summary>The predator this spawner makes (clients copy it to show replicas).</summary>
        public Predator Prefab => prefab;
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

        /// <summary>Debug: spawn one extra predator at a position; its territory is centred there.</summary>
        public void SpawnAt(Vector3 position)
        {
            if (prefab == null) return;
            var p = Instantiate(prefab, OnGround(position.x, position.z), Quaternion.identity, transform);
            p.HomeCenter = p.transform.position;
            p.HomeRadius = radius;
            p.Killed += _ => { alive--; nextSpawn = Time.time + respawnSeconds; };
            alive++;
            var region = GetComponentInParent<Evolve.Ecology.Region>();
            if (region != null) region.Adopt(p.gameObject);
        }

        void SpawnOne()
        {
            Vector2 c = Random.insideUnitCircle * radius * 0.6f;
            Vector3 pos = OnGround(transform.position.x + c.x, transform.position.z + c.y);
            var p = Instantiate(prefab, pos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), transform);
            p.HomeCenter = transform.position;
            p.HomeRadius = radius;
            p.Killed += _ => { alive--; nextSpawn = Time.time + respawnSeconds; };
            alive++;
            var region = GetComponentInParent<Evolve.Ecology.Region>();
            if (region != null) region.Adopt(p.gameObject);
        }

        /// <summary>The prefab's height above the ground at (x, z); on a flat scene without a Terrain the ground is y = 0, as before.</summary>
        Vector3 OnGround(float x, float z) => new Vector3(x, Evolve.World.GroundHeight.At(x, z, 0f) + prefab.transform.position.y, z);

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.6f, 0.2f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
