using System;
using System.Collections.Generic;
using Evolve.Organs;
using Evolve.Survival;
using UnityEngine;

namespace Evolve.Creatures
{
    /// <summary>
    /// Small creature that wanders, flees from nearby players and tires after a
    /// sprint. Touch it to catch it: gives food and drops an organ the catcher lacks.
    /// Needs a kinematic Rigidbody and a non-trigger collider.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Prey : MonoBehaviour
    {
        static readonly List<Prey> active = new List<Prey>();
        public static IReadOnlyList<Prey> Active => active;

        [SerializeField] float wanderSpeed = 0.5f;
        [SerializeField] float fleeSpeed = 1.8f;
        [SerializeField] float fleeRadius = 6f;
        [Tooltip("Seconds it can flee before getting tired")]
        [SerializeField] float stamina = 5f;
        [Tooltip("Seconds it must rest when tired")]
        [SerializeField] float restTime = 1.5f;
        [SerializeField] float nutrition = 15f;
        [SerializeField] OrganSO[] dropTable;

        /// <summary>Area it stays inside (set by the spawner).</summary>
        public Vector3 HomeCenter { get; set; }
        public float HomeRadius { get; set; } = 25f;
        public bool IsTired => tiredUntil > Time.time;

        public event Action<Prey> Caught;

        Rigidbody rb;
        Vector3 wanderDir;
        float nextTurn, fleeTime, tiredUntil;
        bool caught;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        void OnEnable() => active.Add(this);
        void OnDisable() => active.Remove(this);

        void FixedUpdate()
        {
            Vector3 pos = rb.position;
            Vector3 dir;
            float speed;

            Transform threat = NearestPlayer(pos, out float dist);
            if (IsTired)
            {
                dir = Vector3.zero; speed = 0f;
            }
            else if (threat != null && dist < fleeRadius)
            {
                dir = pos - threat.position; dir.y = 0f; dir.Normalize();
                speed = fleeSpeed;
                fleeTime += Time.fixedDeltaTime;
                if (fleeTime >= stamina) { tiredUntil = Time.time + restTime; fleeTime = 0f; }
            }
            else
            {
                fleeTime = Mathf.Max(0f, fleeTime - Time.fixedDeltaTime);
                if (Time.time >= nextTurn)
                {
                    float a = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                    wanderDir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                    nextTurn = Time.time + UnityEngine.Random.Range(2f, 4f);
                }
                dir = wanderDir; speed = wanderSpeed;
            }

            // Stay near home: steer back toward the centre when near the edge.
            Vector3 fromHome = pos - HomeCenter; fromHome.y = 0f;
            if (fromHome.magnitude > HomeRadius * 0.8f)
                dir = (dir - fromHome.normalized * 1.5f).normalized;

            if (dir.sqrMagnitude > 0.001f)
            {
                rb.MovePosition(pos + dir * speed * Time.fixedDeltaTime);
                rb.MoveRotation(Quaternion.Slerp(rb.rotation, Quaternion.LookRotation(dir), 0.2f));
            }
        }

        static Transform NearestPlayer(Vector3 pos, out float dist)
        {
            Transform best = null; dist = float.MaxValue;
            foreach (var p in SurvivalStats.All)
            {
                if (p.State.IsDead) continue;
                float d = Vector3.Distance(pos, p.transform.position);
                if (d < dist) { dist = d; best = p.transform; }
            }
            return best;
        }

        void OnCollisionEnter(Collision c)
        {
            if (caught) return;
            var stats = c.collider.GetComponentInParent<SurvivalStats>();
            if (stats == null || stats.State.IsDead) return;
            caught = true;

            stats.State.Feed(nutrition);
            var host = stats.GetComponent<OrganHost>();
            var drop = PickDrop(host);
            if (drop != null) OrganPickup.Spawn(drop, new Vector3(transform.position.x, 0.25f, transform.position.z));

            Caught?.Invoke(this);
            Destroy(gameObject);
        }

        /// <summary>Random organ from the table that the catcher does not own yet.</summary>
        OrganSO PickDrop(OrganHost host)
        {
            if (dropTable == null) return null;
            var options = new List<OrganSO>();
            foreach (var o in dropTable)
                if (o != null && (host == null || !host.Has(o))) options.Add(o);
            return options.Count == 0 ? null : options[UnityEngine.Random.Range(0, options.Count)];
        }
    }
}
