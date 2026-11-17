using UnityEngine;

namespace Evolve.Creatures
{
    /// <summary>Keeps a number of prey alive in a circular area, respawning after a delay.</summary>
    public class PreySpawner : MonoBehaviour
    {
        [SerializeField] Prey prefab;
        [SerializeField] int count = 6;
        [SerializeField] float radius = 20f;
        [SerializeField] float minDistanceFromCenter = 6f;
        [SerializeField] float respawnSeconds = 15f;

        int alive;
        float nextSpawn;

        void Start()
        {
            for (int i = 0; i < count; i++) SpawnOne();
        }

        void Update()
        {
            if (alive < count && Time.time >= nextSpawn) SpawnOne();
        }

        void SpawnOne()
        {
            if (prefab == null) return;
            float a = Random.Range(0f, Mathf.PI * 2f);
            float d = Random.Range(minDistanceFromCenter, radius);
            Vector3 pos = transform.position + new Vector3(Mathf.Cos(a) * d, prefab.transform.position.y, Mathf.Sin(a) * d);
            var prey = Instantiate(prefab, pos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), transform);
            prey.HomeCenter = transform.position;
            prey.HomeRadius = radius;
            prey.Caught += OnCaught;
            alive++;
        }

        void OnCaught(Prey p)
        {
            alive--;
            nextSpawn = Time.time + respawnSeconds;
        }
    }
}
