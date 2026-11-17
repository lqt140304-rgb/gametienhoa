using System.Collections.Generic;
using Evolve.Survival;
using UnityEngine;

namespace Evolve.Organs
{
    /// <summary>
    /// A dropped organ lying on the ground. Touching it offers it to the player's
    /// OrganInteractor, which asks before attaching. Without an interactor it attaches directly.
    /// </summary>
    public class OrganPickup : MonoBehaviour
    {
        static readonly List<OrganPickup> active = new List<OrganPickup>();
        public static IReadOnlyList<OrganPickup> Active => active;

        [SerializeField] OrganSO organ;
        float baseY;

        public OrganSO Organ => organ;

        /// <summary>Id shared with other machines (the host hands it out).</summary>
        public int NetId { get; set; }
        /// <summary>Client side: a copy of a pickup that really lies in the host's world.</summary>
        public bool IsReplica { get; set; }

        void OnEnable() => active.Add(this);
        void OnDisable() => active.Remove(this);

        /// <summary>
        /// Create a pickup for the organ at a position (no prefab needed). On a client the host owns the world,
        /// so the request goes to the host and the pickup comes back as a replica (returns null here).
        /// </summary>
        public static OrganPickup Spawn(OrganSO organ, Vector3 position)
        {
            if (NetWorld.IsClient)
            {
                NetWorld.RequestPickupSpawn?.Invoke(organ.name, position);
                return null;
            }
            var pickup = Create(organ, position);
            pickup.NetId = NetWorld.NextId();
            return pickup;
        }

        /// <summary>Client: show a pickup that exists on the host.</summary>
        public static OrganPickup CreateReplica(OrganSO organ, Vector3 position, int netId)
        {
            var pickup = Create(organ, position);
            pickup.NetId = netId;
            pickup.IsReplica = true;
            return pickup;
        }

        /// <summary>Tell the host this (replica) pickup was taken, so it disappears for everyone.</summary>
        public void MarkTaken()
        {
            if (IsReplica) NetWorld.ReportPickupTaken?.Invoke(NetId);
        }

        static OrganPickup Create(OrganSO organ, Vector3 position)
        {
            var go = new GameObject("Pickup_" + organ.name);
            go.transform.position = position;
            var col = go.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.5f;
            var pickup = go.AddComponent<OrganPickup>();
            pickup.organ = organ;
            if (organ.VisualPrefab != null)
            {
                var v = Instantiate(organ.VisualPrefab, go.transform, false);
                v.transform.localScale *= 1.5f;
            }
            // Glow in the tier colour so rare drops stand out.
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = OrganTiers.Color(organ.Tier);
            light.range = 2.5f;
            light.intensity = 1.5f + (int)organ.Tier;
            return pickup;
        }

        void Start() => baseY = transform.position.y;

        void Update()
        {
            var p = transform.position;
            p.y = baseY + 0.3f + Mathf.Sin(Time.time * 3f) * 0.1f;
            transform.position = p;
            transform.Rotate(0f, 90f * Time.deltaTime, 0f, Space.World);
        }

        void OnTriggerEnter(Collider other)
        {
            var interactor = other.GetComponentInParent<OrganInteractor>();
            if (interactor != null) { interactor.Offer(this); return; }

            var host = other.GetComponentInParent<OrganHost>();
            if (host != null && host.TryAttach(organ)) { MarkTaken(); Destroy(gameObject); }
        }

        void OnTriggerExit(Collider other)
        {
            var interactor = other.GetComponentInParent<OrganInteractor>();
            if (interactor != null) interactor.Withdraw(this);
        }
    }
}
