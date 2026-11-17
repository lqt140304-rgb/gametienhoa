using UnityEngine;

namespace Evolve.Organs
{
    /// <summary>
    /// A dropped organ lying on the ground. Touching it offers it to the player's
    /// OrganInteractor, which asks before attaching. Without an interactor it attaches directly.
    /// </summary>
    public class OrganPickup : MonoBehaviour
    {
        [SerializeField] OrganSO organ;
        float baseY;

        public OrganSO Organ => organ;

        /// <summary>Create a pickup for the organ at a position (no prefab needed).</summary>
        public static OrganPickup Spawn(OrganSO organ, Vector3 position)
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
            if (host != null && host.TryAttach(organ)) Destroy(gameObject);
        }

        void OnTriggerExit(Collider other)
        {
            var interactor = other.GetComponentInParent<OrganInteractor>();
            if (interactor != null) interactor.Withdraw(this);
        }
    }
}
