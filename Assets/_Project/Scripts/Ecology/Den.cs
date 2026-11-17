using UnityEngine;

namespace Evolve.Ecology
{
    /// <summary>Nest of a species. Births need at least one living den; creatures embody here.</summary>
    public class Den : MonoBehaviour
    {
        public SpeciesSO Species;
        public Region Region;
        public bool Destroyed { get; private set; }

        void OnEnable()
        {
            if (Region == null) Region = GetComponentInParent<Region>();
            if (Region == null) Region = FindFirstObjectByType<Region>();
            if (Region != null) Region.RegisterDen(this);
        }

        void OnDisable()
        {
            if (Region != null) Region.UnregisterDen(this);
        }

        public void Destroy()
        {
            if (Destroyed) return;
            Destroyed = true;
            foreach (var r in GetComponentsInChildren<Renderer>())
                r.material.color = new Color(0.12f, 0.12f, 0.12f);
            if (Region != null) Region.RecountDens();
        }
    }
}
