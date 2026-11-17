using System;
using System.Collections.Generic;
using Evolve.Survival;
using UnityEngine;

namespace Evolve.Organs
{
    /// <summary>
    /// Holds the organs a creature has evolved. A new organ starts as a small bud and
    /// grows by being fed: part of every meal goes into growing organs until each has
    /// received its GrowthFoodRequired. Visual size and effects scale with growth.
    /// Sockets are found among children automatically.
    /// </summary>
    public class OrganHost : MonoBehaviour
    {
        [Tooltip("Share of each meal that feeds growing organs (the rest fills hunger). When hunger is full, everything goes to organs.")]
        [Range(0, 1)] [SerializeField] float growthShare = 0.5f;
        [Tooltip("How fast the visual catches up with real growth (per second)")]
        [SerializeField] float visualCatchUp = 0.5f;

        class Entry
        {
            public OrganSO Organ;
            public GameObject Visual;
            public Vector3 VisualScale;
            public float Fed;    // food received so far
            public float Shown;  // smoothed 0..1 used for visuals and effects
            public bool Done;
            public float Growth => Organ.GrowthFoodRequired <= 0f ? 1f : Mathf.Clamp01(Fed / Organ.GrowthFoodRequired);
        }

        readonly List<Entry> order = new List<Entry>();
        readonly Dictionary<OrganSlot, Entry> organs = new Dictionary<OrganSlot, Entry>();
        readonly Dictionary<OrganSlot, Transform> sockets = new Dictionary<OrganSlot, Transform>();
        SurvivalStats stats;

        public float SpeedMultiplier { get; private set; } = 1f;
        /// <summary>0 = blind, 1 = fully developed eyes.</summary>
        public float Sight { get; private set; }
        public bool HasSight => Sight >= 1f;
        public IEnumerable<OrganSO> Organs { get { foreach (var e in order) yield return e.Organ; } }

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

        void OnEnable() { if (stats != null) stats.State.FoodFilter = TakeFood; }
        void OnDisable() { if (stats != null && stats.State.FoodFilter == TakeFood) stats.State.FoodFilter = null; }

        public bool Has(OrganSO organ) => organ != null && organs.TryGetValue(organ.Slot, out var e) && e.Organ == organ;

        /// <summary>Growth 0..1 of the organ, or -1 if not owned.</summary>
        public float GetGrowth(OrganSO organ) => Has(organ) ? organs[organ.Slot].Growth : -1f;

        /// <summary>Food received so far by the organ (0 if not owned).</summary>
        public float GetFed(OrganSO organ) => Has(organ) ? organs[organ.Slot].Fed : 0f;

        /// <summary>Attach (or replace) the organ in its slot as a bud. Returns false if already owned.</summary>
        public bool TryAttach(OrganSO organ)
        {
            if (organ == null || Has(organ)) return false;

            if (organs.TryGetValue(organ.Slot, out var old))
            {
                if (old.Visual != null) Destroy(old.Visual);
                order.Remove(old);
            }

            var entry = new Entry { Organ = organ };
            if (organ.VisualPrefab != null)
            {
                Transform parent = sockets.TryGetValue(organ.Slot, out var s) ? s : transform;
                entry.Visual = Instantiate(organ.VisualPrefab, parent, false);
                entry.VisualScale = entry.Visual.transform.localScale;
            }
            organs[organ.Slot] = entry;
            order.Add(entry);

            ApplyVisual(entry);
            Recalculate();
            Attached?.Invoke(organ);
            CheckDone(entry);
            return true;
        }

        /// <summary>FoodFilter: split a meal between growing organs and hunger.</summary>
        float TakeFood(float amount)
        {
            var growing = order.FindAll(e => e.Growth < 1f);
            if (growing.Count == 0 || amount <= 0f) return amount;

            var st = stats.State;
            float share = st.Hunger >= st.MaxHunger ? 1f : growthShare;
            float forOrgans = amount * share;
            float used = 0f;

            // Feed organs in the order they were attached; leftovers roll to the next.
            foreach (var e in growing)
            {
                float need = e.Organ.GrowthFoodRequired - e.Fed;
                float give = Mathf.Min(need, forOrgans - used);
                if (give <= 0f) break;
                e.Fed += give;
                used += give;
                CheckDone(e);
            }
            return amount - used;
        }

        void CheckDone(Entry e)
        {
            if (e.Done || e.Growth < 1f) return;
            e.Done = true;
            Debug.Log($"{name} tiến hóa xong: {e.Organ.DisplayName}");
            Completed?.Invoke(e.Organ);
        }

        void Update()
        {
            bool changed = false;
            foreach (var e in order)
            {
                float target = e.Growth;
                if (Mathf.Approximately(e.Shown, target)) continue;
                e.Shown = Mathf.MoveTowards(e.Shown, target, visualCatchUp * Time.deltaTime);
                ApplyVisual(e);
                changed = true;
            }
            if (changed) Recalculate();
        }

        static void ApplyVisual(Entry e)
        {
            if (e.Visual != null)
                e.Visual.transform.localScale = e.VisualScale * Mathf.Lerp(0.15f, 1f, e.Shown);
        }

        void Recalculate()
        {
            float speed = 1f, sight = 0f;
            foreach (var e in order)
            {
                speed *= Mathf.Lerp(1f, e.Organ.SpeedMultiplier, e.Shown);
                if (e.Organ.GrantsSight) sight = Mathf.Max(sight, e.Shown);
            }
            SpeedMultiplier = speed;
            Sight = sight;
            Changed?.Invoke();
        }
    }
}
