using UnityEngine;

namespace Evolve.Creatures
{
    /// <summary>Keeps a number of prey alive in a circular area, respawning after a delay.</summary>
    public class PreySpawner : MonoBehaviour
    {
        [SerializeField] Prey prefab;
        /// <summary>The prey this spawner makes (clients copy it to show replicas).</summary>
        public Prey Prefab => prefab;
        [SerializeField] int count = 6;
        [SerializeField] float radius = 20f;
        [SerializeField] float minDistanceFromCenter = 6f;
        [SerializeField] float respawnSeconds = 15f;

        int alive;
        float nextSpawn;

        void Start()
        {
            if (prefab == null)
            {
                Debug.LogError("PreySpawner: chưa gán Prefab con mồi. Chạy lại Evolve → Build Prototype Scene.", this);
                return;
            }
            for (int i = 0; i < count; i++) SpawnOne();
            Debug.Log($"PreySpawner: đã thả {count} con mồi.");
        }

        void Update()
        {
            if (alive < count && Time.time >= nextSpawn) SpawnOne();
        }

        /// <summary>Debug: spawn one extra prey at a position (counts toward the alive total).</summary>
        public void SpawnAt(Vector3 position)
        {
            if (prefab == null) return;
            var prey = Instantiate(prefab, OnGround(position.x, position.z), Quaternion.identity, transform);
            prey.HomeCenter = transform.position;
            prey.HomeRadius = radius;
            prey.Caught += OnCaught;
            alive++;
        }

        void SpawnOne()
        {
            if (prefab == null) return;
            float a = Random.Range(0f, Mathf.PI * 2f);
            float d = Random.Range(minDistanceFromCenter, radius);
            Vector3 pos = OnGround(transform.position.x + Mathf.Cos(a) * d, transform.position.z + Mathf.Sin(a) * d);
            var prey = Instantiate(prefab, pos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), transform);
            prey.HomeCenter = transform.position;
            prey.HomeRadius = radius;
            prey.Caught += OnCaught;
            alive++;
        }

        /// <summary>The prefab's height above the ground at (x, z); on a flat scene without a Terrain the ground is y = 0, as before.</summary>
        Vector3 OnGround(float x, float z) => new Vector3(x, Evolve.World.GroundHeight.At(x, z, 0f) + prefab.transform.position.y, z);

        void OnCaught(Prey p)
        {
            alive--;
            nextSpawn = Time.time + respawnSeconds;
        }
    }
}
