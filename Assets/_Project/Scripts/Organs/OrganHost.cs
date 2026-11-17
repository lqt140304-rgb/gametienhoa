using System;
using System.Collections.Generic;
using Evolve.Survival;
using UnityEngine;

namespace Evolve.Organs
{
    /// <summary>
    /// Holds the organs a creature has evolved. A new organ starts as a small bud and
    /// grows over time while the creature is fed; its visual and effects scale with growth.
    /// Sockets are found among children automatically.
    /// </summary>
    public class OrganHost : MonoBehaviour
    {
        [Tooltip("Growth pauses while hunger is below this value")]
        [SerializeField] float minHungerToGrow = 20f;

        class Entry
        {
            public OrganSO Organ;
            public GameObject Visual;
            public Vector3 VisualScale;
            public float Growth; // 0..1
        }

        readonly Dictionary<OrganSlot, Entry> organs = new Dictionary<OrganSlot, Entry>();
        readonly Dictionary<OrganSlot, Transform> sockets = new Dictionary<OrganSlot, Transform>();
        SurvivalStats stats;

        public float SpeedMultiplier { get; private set; } = 1f;
        /// <summary>0 = blind, 1 = fully developed eyes.</summary>
        public float Sight { get; private set; }
        public bool HasSight => Sight >= 1f;
        public IEnumerable<OrganSO> Organs { get { foreach (var e in organs.Values) yield return e.Organ; } }
        /// <summary>True while any organ is still growing but blocked by hunger.</summary>
        public bool GrowthStarved { get; private set; }

        /// <summary>Raised when an organ is attached and starts growing.</summary>
        public event Action<OrganSO> Attached;
        /// <summary>Raised when an organ finishes growing.</summary>
        public event Action<OrganSO> Completed;
        /// <summary>Raised whenever the combined effects change.</summary>
        public event Action Changed;

        void Awake()
        {
            stats = GetComponent<SurvivalStats>();
            foreach (var s in GetComponentsInChildren<OrganSocket>(true))
                sockets[s.Slot] = s.transform;
        }

        public bool Has(OrganSO organ) => organ != null && organs.TryGetValue(organ.Slot, out var e) && e.Organ == organ;

        /// <summary>Growth 0..1 of the organ, or -1 if not owned.</summary>
        public float GetGrowth(OrganSO organ) => Has(organ) ? organs[organ.Slot].Growth : -1f;

        /// <summary>Attach (or replace) the organ in its slot as a bud. Returns false if already owned.</summary>
        public bool TryAttach(OrganSO organ)
        {
            if (organ == null || Has(organ)) return false;

            if (organs.TryGetValue(organ.Slot, out var old) && old.Visual != null) Destroy(old.Visual);

            var entry = new Entry { Organ = organ, Growth = organ.GrowthSeconds <= 0f ? 1f : 0f };
            if (organ.VisualPrefab != null)
            {
                Transform parent = sockets.TryGetValue(organ.Slot, out var s) ? s : transform;
                entry.Visual = Instantiate(organ.VisualPrefab, parent, false);
                entry.VisualScale = entry.Visual.transform.localScale;
            }
            organs[organ.Slot] = entry;

            ApplyGrowth(entry);
            Recalculate();
            Attached?.Invoke(organ);
            if (entry.Growth >= 1f) Completed?.Invoke(organ);
            return true;
        }

        void Update()
        {
            bool changed = false, starved = false;
            foreach (var e in organs.Values)
            {
                if (e.Growth >= 1f) continue;
                if (stats != null && (stats.State.IsDead || stats.State.Hunger < minHungerToGrow)) { starved = true; continue; }

                float step = Time.deltaTime / e.Organ.GrowthSeconds;
                if (stats != null) stats.State.ConsumeFood(e.Organ.GrowthFoodCost * step);
                e.Growth = Mathf.Min(1f, e.Growth + step);
                ApplyGrowth(e);
                changed = true;
                if (e.Growth >= 1f)
                {
                    Debug.Log($"{name} tiến hóa xong: {e.Organ.DisplayName}");
                    Completed?.Invoke(e.Organ);
                }
            }
            GrowthStarved = starved;
            if (changed) Recalculate();
        }

        static void ApplyGrowth(Entry e)
        {
            if (e.Visual != null)
                e.Visual.transform.localScale = e.VisualScale * Mathf.Lerp(0.15f, 1f, e.Growth);
        }

        void Recalculate()
        {
            float speed = 1f, sight = 0f;
            foreach (var e in organs.Values)
            {
                speed *= Mathf.Lerp(1f, e.Organ.SpeedMultiplier, e.Growth);
                if (e.Organ.GrantsSight) sight = Mathf.Max(sight, e.Growth);
            }
            SpeedMultiplier = speed;
            Sight = sight;
            Changed?.Invoke();
        }
    }
}
