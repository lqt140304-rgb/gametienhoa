using System;
using System.Collections.Generic;
using Evolve.Survival;
using UnityEngine;

namespace Evolve.Organs
{
    /// <summary>Result of asking whether an organ can be attached.</summary>
    public enum AttachCheck { Ok, AlreadyOwned, SlotOccupied, NoRoom, LackNutrient }

    /// <summary>
    /// Holds the organs a creature has evolved and grows them with evolution EXP.
    /// A new organ is a bud (Lv0). Spending GrowthExpRequired EXP grows it to Lv1 (full organ);
    /// Lv2 costs the same again and Lv3 twice as much; each level adds LevelBonus to its effects.
    /// EXP comes from eating (a little) and absorbing organs (a lot), into SurvivalState.EvolutionExp.
    /// The player spends it by hand (body menu) or marks one organ as priority to receive it automatically.
    /// Sockets are found among children automatically.
    /// </summary>
    public class OrganHost : MonoBehaviour
    {
        public const int MaxLevel = 3;

        [Tooltip("Max number of organs the body can hold (0 = unlimited, one per slot)")]
        [SerializeField] int maxOrgans = 0;
        [Tooltip("Evolution EXP gained per unit of food eaten")]
        [SerializeField] float foodExpRate = 0.2f;
        [Tooltip("Extra effect per level above 1 (0.25 = Lv2 +25%, Lv3 +50%)")]
        [SerializeField] float levelBonus = 0.25f;
        [Tooltip("How fast the visual catches up with real growth (per second)")]
        [SerializeField] float visualCatchUp = 0.5f;

        class Entry
        {
            public OrganSO Organ;
            public GameObject Visual;
            public Vector3 VisualScale;
            public int Level;        // 0 = bud, 1..MaxLevel
            public float Progress;   // EXP invested toward the next level
            public float Shown;      // smoothed 0..1 growth for visuals and effects
            public float Decay;      // 0 healthy .. 1 dissolved (starved of the nutrient it needs)
            public float Growth => Level >= 1 ? 1f : (Organ.GrowthExpRequired <= 0f ? 1f : Mathf.Clamp01(Progress / Organ.GrowthExpRequired));
            public float NextCost => Level == 0 ? Organ.GrowthExpRequired : Level < MaxLevel ? Organ.GrowthExpRequired * Level : 0f;
            public bool Maxed => Level >= MaxLevel;
        }

        readonly List<Entry> order = new List<Entry>();
        readonly Dictionary<OrganSlot, Entry> organs = new Dictionary<OrganSlot, Entry>();
        readonly Dictionary<OrganSlot, Transform> sockets = new Dictionary<OrganSlot, Transform>();
        SurvivalStats stats;
        OrganSO priority;

        public float SpeedMultiplier { get; private set; } = 1f;
        /// <summary>0 = blind, 1 = fully developed eyes, above 1 = levelled eyes (longer view).</summary>
        public float Sight { get; private set; }
        public bool HasSight => Sight >= 1f;
        /// <summary>Damage dealt back to attackers (spikes).</summary>
        public float ContactDamage { get; private set; }
        public float HeatResistance { get; private set; }
        public IEnumerable<OrganSO> Organs { get { foreach (var e in order) yield return e.Organ; } }
        public float Exp => stats != null ? stats.State.EvolutionExp : 0f;

        /// <summary>Organ that automatically receives EXP (null = spend by hand only).</summary>
        public OrganSO Priority
        {
            get => priority;
            set => priority = value != null && Has(value) && !organs[value.Slot].Maxed ? value : null;
        }

        /// <summary>Raised when an organ is attached as a bud.</summary>
        public event Action<OrganSO> Attached;
        /// <summary>Raised when a bud becomes a full organ (Lv1).</summary>
        public event Action<OrganSO> Completed;
        /// <summary>Raised when an organ reaches Lv2 or Lv3.</summary>
        public event Action<OrganSO, int> LeveledUp;
        /// <summary>Raised when an organ is detached (removed or replaced).</summary>
        public event Action<OrganSO> Removed;
        /// <summary>Raised when an organ dissolves because the body ran out of the nutrient it needs.</summary>
        public event Action<OrganSO> Dissolved;
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

        /// <summary>The organ currently in a slot, or null.</summary>
        public OrganSO GetInSlot(OrganSlot slot) => organs.TryGetValue(slot, out var e) ? e.Organ : null;

        public int Count => organs.Count;
        /// <summary>Owned organs in the order they were attached.</summary>
        public List<OrganSO> OrganList => order.ConvertAll(e => e.Organ);
        public int MaxOrgans => maxOrgans;

        /// <summary>What would happen if this organ were attached.</summary>
        public AttachCheck Check(OrganSO organ)
        {
            if (Has(organ)) return AttachCheck.AlreadyOwned;
            // The body must hold enough of the nutrient this organ needs (both for a free slot and for a replacement).
            var st = stats != null ? stats : GetComponent<SurvivalStats>(); // Awake may not have run yet (EditMode tests)
            bool lacks = st != null && !st.IsRemote && st.State.IsLacking(organ.EffectiveNutrient);
            if (organs.ContainsKey(organ.Slot)) return lacks ? AttachCheck.LackNutrient : AttachCheck.SlotOccupied;
            if (maxOrgans > 0 && organs.Count >= maxOrgans) return AttachCheck.NoRoom;
            return lacks ? AttachCheck.LackNutrient : AttachCheck.Ok;
        }

        /// <summary>Growth 0..1 from bud to full organ, or -1 if not owned.</summary>
        public float GetGrowth(OrganSO organ) => Has(organ) ? organs[organ.Slot].Growth : -1f;

        /// <summary>Level of the organ: 0 = bud, 1..MaxLevel; -1 if not owned.</summary>
        public int GetLevel(OrganSO organ) => Has(organ) ? organs[organ.Slot].Level : -1;

        /// <summary>EXP invested toward the next level and that level's cost (0 cost = max level).</summary>
        public void GetProgress(OrganSO organ, out float progress, out float cost)
        {
            progress = cost = 0f;
            if (!Has(organ)) return;
            var e = organs[organ.Slot];
            progress = e.Progress;
            cost = e.NextCost;
        }

        public bool IsMaxed(OrganSO organ) => Has(organ) && organs[organ.Slot].Maxed;

        /// <summary>Attach (or replace) the organ in its slot as a bud. Returns false if already owned.</summary>
        public bool TryAttach(OrganSO organ)
        {
            if (organ == null) return false;
            var check = Check(organ);
            if (check == AttachCheck.AlreadyOwned || check == AttachCheck.NoRoom || check == AttachCheck.LackNutrient) return false;

            if (organs.ContainsKey(organ.Slot)) Detach(organ.Slot);

            var entry = new Entry { Organ = organ };
            if (organ.VisualPrefab != null)
            {
                Transform parent = sockets.TryGetValue(organ.Slot, out var s) ? s : transform;
                entry.Visual = Instantiate(organ.VisualPrefab, parent, false);
                entry.VisualScale = entry.Visual.transform.localScale;
            }
            organs[organ.Slot] = entry;
            order.Add(entry);

            if (organ.GrowthExpRequired <= 0f) LevelUp(entry);
            ApplyVisual(entry);
            Recalculate();
            Attached?.Invoke(organ);
            return true;
        }

        /// <summary>Remove the organ in a slot (its growth and levels are lost). Returns it, or null if empty.</summary>
        public OrganSO Detach(OrganSlot slot)
        {
            if (!organs.TryGetValue(slot, out var e)) return null;
            if (e.Visual != null) Destroy(e.Visual);
            organs.Remove(slot);
            order.Remove(e);
            if (priority == e.Organ) priority = null;
            Recalculate();
            Removed?.Invoke(e.Organ);
            return e.Organ;
        }

        /// <summary>
        /// Spend pooled EXP on an organ, up to its next level (or <paramref name="untilMax"/> all the way).
        /// Returns the EXP spent.
        /// </summary>
        public float Invest(OrganSO organ, bool untilMax = false)
        {
            if (!Has(organ) || stats == null) return 0f;
            var e = organs[organ.Slot];
            var st = stats.State;
            float spent = 0f;
            while (!e.Maxed && st.EvolutionExp > 0f)
            {
                float give = Mathf.Min(e.NextCost - e.Progress, st.EvolutionExp);
                e.Progress += give;
                st.EvolutionExp -= give;
                spent += give;
                if (e.Progress < e.NextCost - 0.0001f) break;
                LevelUp(e);
                if (!untilMax) break;
            }
            if (e.Maxed && priority == organ) priority = null;
            if (spent > 0f) Recalculate();
            return spent;
        }

        void LevelUp(Entry e)
        {
            e.Progress = 0f;
            e.Level++;
            if (e.Level == 1)
            {
                Debug.Log($"{name} tiến hóa xong: {e.Organ.DisplayName}");
                Completed?.Invoke(e.Organ);
            }
            else
            {
                Debug.Log($"{name}: {e.Organ.DisplayName} lên cấp {e.Level}");
                LeveledUp?.Invoke(e.Organ, e.Level);
            }
        }

        /// <summary>Debug: make every bud a full organ (Lv1) immediately.</summary>
        public void DebugGrowAll()
        {
            foreach (var e in order)
            {
                if (e.Level == 0) LevelUp(e);
                e.Shown = 1f;
                ApplyVisual(e);
            }
            Recalculate();
        }

        /// <summary>
        /// Remote avatars: show this organ at exactly this level, without any EXP involved
        /// (the real owner spends the EXP on their own machine).
        /// </summary>
        public void MirrorOrgan(OrganSO organ, int level)
        {
            if (organ == null) return;
            level = Mathf.Clamp(level, 0, MaxLevel);
            if (Has(organ) && organs[organ.Slot].Level > level) Detach(organ.Slot); // went back to a bud: start over
            if (!Has(organ)) TryAttach(organ);
            if (!Has(organ)) return;

            var e = organs[organ.Slot];
            while (e.Level < level) LevelUp(e);
            e.Shown = level >= 1 ? 1f : e.Growth;
            Recalculate();
        }

        /// <summary>Debug: raise every organ to at least the given level (1..MaxLevel).</summary>
        public void DebugSetLevel(int level)
        {
            level = Mathf.Clamp(level, 1, MaxLevel);
            foreach (var e in order)
            {
                while (e.Level < level) LevelUp(e);
                e.Shown = 1f;
            }
            Recalculate();
        }

        /// <summary>FoodFilter: all food fills hunger; a little of it also becomes evolution EXP.</summary>
        float TakeFood(float amount)
        {
            if (amount > 0f && stats != null) stats.State.AddExp(amount * foodExpRate);
            return amount;
        }

        void Update()
        {
            // Priority organ soaks up EXP automatically.
            if (priority != null && Exp > 0f) Invest(priority, true);

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
            UpdateStarvation();
        }

        /// <summary>The nutrient an organ needs running out weakens it, and in the end dissolves it (it drops as a pickup for now).</summary>
        void UpdateStarvation()
        {
            if (stats == null || stats.IsRemote || stats.State.IsDead || stats.Config == null || order.Count == 0) return;
            var cfg = stats.Config;
            Entry dissolve = null;
            bool decayChanged = false;
            foreach (var e in order)
            {
                float value = stats.State.Nutrient(e.Organ.EffectiveNutrient);
                float next = OrganDecay.Step(e.Decay, value, cfg.NutrientEquipThreshold, cfg.NutrientWarnThreshold,
                    cfg.OrganDissolveSeconds, cfg.OrganRecoverSeconds, Time.deltaTime);
                if (!Mathf.Approximately(next, e.Decay)) { e.Decay = next; decayChanged = true; }
                if (e.Decay >= 1f && dissolve == null) dissolve = e;
            }
            if (decayChanged) Recalculate();
            if (dissolve == null) return;

            var organ = dissolve.Organ;
            Debug.Log($"{name}: {organ.DisplayName} tan vì thiếu {NutrientKinds.SlimeName(organ.EffectiveNutrient)}");
            Detach(organ.Slot);
            OrganPickup.Spawn(organ, new Vector3(transform.position.x, Evolve.World.GroundHeight.At(transform.position.x, transform.position.z, 0f) + 0.25f, transform.position.z));
            Dissolved?.Invoke(organ);
        }

        /// <summary>0 healthy .. 1 dissolved; -1 if not owned.</summary>
        public float GetDecay(OrganSO organ) => Has(organ) ? organs[organ.Slot].Decay : -1f;

        void ApplyVisual(Entry e)
        {
            if (e.Visual == null) return;
            // Buds grow to full size; each extra level makes the organ a bit bigger.
            float levelScale = 1f + 0.1f * Mathf.Max(0, e.Level - 1);
            e.Visual.transform.localScale = e.VisualScale * Mathf.Lerp(0.15f, 1f, e.Shown) * levelScale;
        }

        /// <summary>Effect strength of an organ: growth × level bonus.</summary>
        float Strength(Entry e) => e.Shown * (1f + levelBonus * Mathf.Max(0, e.Level - 1)) * OrganDecay.StrengthFactor(e.Decay);

        void Recalculate()
        {
            float speed = 1f, sight = 0f, contact = 0f, heat = 0f;
            foreach (var e in order)
            {
                ApplyVisual(e);
                float k = Strength(e);
                speed *= 1f + (e.Organ.SpeedMultiplier - 1f) * k;
                if (e.Organ.GrantsSight) sight = Mathf.Max(sight, k);
                contact += e.Organ.ContactDamage * k;
                heat += e.Organ.HeatResistance * k;
            }
            if (stats != null && stats.State.NutrientLoad != null)
            {
                for (int k = 0; k < stats.State.NutrientLoad.Length; k++) stats.State.NutrientLoad[k] = 0;
                foreach (var e in order) stats.State.NutrientLoad[(int)e.Organ.EffectiveNutrient]++;
            }
            SpeedMultiplier = speed;
            Sight = sight;
            ContactDamage = contact;
            HeatResistance = heat;
            if (stats != null) stats.State.HeatResistance = heat;
            Changed?.Invoke();
        }
    }
}
