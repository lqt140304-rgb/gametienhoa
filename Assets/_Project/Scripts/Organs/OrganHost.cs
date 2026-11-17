using System;
using System.Collections.Generic;
using UnityEngine;

namespace Evolve.Organs
{
    /// <summary>
    /// Holds the organs a creature has evolved, spawns their visuals on sockets and
    /// exposes the combined effects. Sockets are found among children automatically.
    /// </summary>
    public class OrganHost : MonoBehaviour
    {
        readonly Dictionary<OrganSlot, OrganSO> organs = new Dictionary<OrganSlot, OrganSO>();
        readonly Dictionary<OrganSlot, GameObject> visuals = new Dictionary<OrganSlot, GameObject>();
        readonly Dictionary<OrganSlot, Transform> sockets = new Dictionary<OrganSlot, Transform>();

        public float SpeedMultiplier { get; private set; } = 1f;
        public bool HasSight { get; private set; }
        public IEnumerable<OrganSO> Organs => organs.Values;

        /// <summary>Raised after an organ is attached (the new organ).</summary>
        public event Action<OrganSO> Attached;
        /// <summary>Raised whenever the combined effects change.</summary>
        public event Action Changed;

        void Awake()
        {
            foreach (var s in GetComponentsInChildren<OrganSocket>(true))
                sockets[s.Slot] = s.transform;
        }

        public bool Has(OrganSO organ) => organ != null && organs.TryGetValue(organ.Slot, out var o) && o == organ;

        /// <summary>Attach (or replace) the organ in its slot. Returns false if already owned.</summary>
        public bool TryAttach(OrganSO organ)
        {
            if (organ == null || Has(organ)) return false;

            if (visuals.TryGetValue(organ.Slot, out var old) && old != null) Destroy(old);
            organs[organ.Slot] = organ;

            if (organ.VisualPrefab != null)
            {
                Transform parent = sockets.TryGetValue(organ.Slot, out var s) ? s : transform;
                visuals[organ.Slot] = Instantiate(organ.VisualPrefab, parent, false);
            }

            Recalculate();
            Debug.Log($"{name} tiến hóa: {organ.DisplayName}");
            Attached?.Invoke(organ);
            return true;
        }

        void Recalculate()
        {
            float speed = 1f;
            bool sight = false;
            foreach (var o in organs.Values)
            {
                speed *= o.SpeedMultiplier;
                sight |= o.GrantsSight;
            }
            SpeedMultiplier = speed;
            HasSight = sight;
            Changed?.Invoke();
        }
    }
}
