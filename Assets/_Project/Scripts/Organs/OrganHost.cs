using System;
using System.Collections.Generic;
using Evolve.Survival;
using UnityEngine;

namespace Evolve.Organs
{
    /// <summary>Result of asking whether an organ can be attached.</summary>
    public enum AttachCheck { Ok, AlreadyOwned, NoRoom, LackNutrient }

    /// <summary>Result of asking Biến Hình to sculpt an organ.</summary>
    public enum SculptResult { Ok, NotInShelter, Unknown, NoMass, Blocked, NotPassive }

    /// <summary>
    /// Holds the organs on the body (Biến Hình). Organs are learned by Giải Mã (AnalysisBook on the Swallower) and
    /// sculpted from body mass, only in a shelter; removing one gives half the mass back. An organ's level is its
    /// stars in the book (1..3) and rises as more samples are analysed. A new organ grows in visibly (Shown 0 → 1).
    /// The nutrient an organ needs running out weakens it and finally dissolves it (it stays learned).
    /// Sockets are found among children automatically.
    /// </summary>
    public class OrganHost : MonoBehaviour
    {
        public const int MaxLevel = 3;

        /// <summary>Passives (eyes included) are hidden: the body stays smooth. Flip to true to show their models again.</summary>
        public const bool ShowsOnBody = false;

        [Tooltip("Max number of organs the body can hold (0 = only the SkillRules.MaxPassives cap)")]
        [SerializeField] int maxOrgans = 0;
        [Tooltip("Extra effect per level above 1 (0.25 = Lv2 +25%, Lv3 +50%)")]
        [SerializeField] float levelBonus = 0.25f;
        [Tooltip("How fast a freshly sculpted organ grows in (per second)")]
        [SerializeField] float visualCatchUp = 0.5f;

        class Entry
        {
            public OrganSO Organ;
            public GameObject Visual;
            public Vector3 VisualScale;
            public int Level;        // stars, 1..MaxLevel
            public float Shown;      // smoothed 0..1 growth for visuals and effects
            public float Decay;      // 0 healthy .. 1 dissolved (starved of the nutrient it needs)
            public bool Maxed => Level >= MaxLevel;
        }

        readonly List<Entry> order = new List<Entry>();
        readonly Dictionary<OrganSO, Entry> organs = new Dictionary<OrganSO, Entry>();   // passives fill passive slots, not body slots
        readonly Dictionary<OrganSlot, Transform> sockets = new Dictionary<OrganSlot, Transform>();
        SurvivalStats stats;
        Evolve.Swallow.Swallower swallower;
        Evolve.Swallow.AnalysisBook watchedBook;

        public float SpeedMultiplier { get; private set; } = 1f;
        /// <summary>0 = blind, 1 = fully developed eyes, above 1 = levelled eyes (longer view).</summary>
        public float Sight { get; private set; }
        public bool HasSight => Sight >= 1f;
        /// <summary>Damage dealt back to attackers: passive thorns plus the spikes of an active move while they are out.</summary>
        public float ContactDamage => passiveContact + (Visuals != null ? Visuals.ContactDamage : 0f);
        float passiveContact;
        /// <summary>Set by SkillVisuals when it is added to the body.</summary>
        public Evolve.Abilities.SkillVisuals Visuals { get; set; }
        public float HeatResistance { get; private set; }
        /// <summary>Passive cave skills: extra max health (Da Dày), share of damage blocked (Vỏ Cứng), Sóng Dò reach (Sóng Dò Mạnh).</summary>
        public float MaxHealthBonus { get; private set; }
        public float DamageReduction { get; private set; }
        public float SonarRangeMultiplier { get; private set; } = 1f;
        public IEnumerable<OrganSO> Organs { get { foreach (var e in order) yield return e.Organ; } }

        /// <summary>Raised when an organ is attached.</summary>
        public event Action<OrganSO> Attached;
        /// <summary>Raised when a freshly attached organ has fully grown in.</summary>
        public event Action<OrganSO> Completed;
        /// <summary>Raised when an organ reaches ★2 or ★3.</summary>
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

        void OnEnable() => WatchBook();
        void OnDisable() { if (watchedBook != null) watchedBook.StarsChanged -= OnStarsChanged; watchedBook = null; }

        /// <summary>What Giải Mã has learned (null when the body has no Swallower, e.g. a remote avatar).</summary>
        public Evolve.Swallow.AnalysisBook Book
        {
            get
            {
                if (swallower == null) swallower = GetComponent<Evolve.Swallow.Swallower>();
                return swallower != null ? swallower.Book : null;
            }
        }

        void WatchBook()
        {
            var b = Book;
            if (b == null || b == watchedBook) return;
            if (watchedBook != null) watchedBook.StarsChanged -= OnStarsChanged;
            watchedBook = b;
            watchedBook.StarsChanged += OnStarsChanged;
        }

        void OnStarsChanged(OrganSO organ, int stars) => SetLevel(organ, stars);

        public bool Knows(OrganSO organ) { var b = Book; return b != null && b.Knows(organ); }

        public bool Has(OrganSO organ) => organ != null && organs.ContainsKey(organ);


        /// <summary>
        /// The worn organs as (slot, integrity 0..1) pairs, for flaw compensation. Integrity is 1 minus the organ's Decay
        /// (the body's full integrity/trauma system is still only a design, docs/BIOLOGICAL_INTEGRITY.md).
        /// </summary>
        public void GetWorn(List<(OrganSlot slot, float integrity)> into)
        {
            into.Clear();
            foreach (var e in order) into.Add((e.Organ.Slot, Mathf.Clamp01(1f - e.Decay)));
        }

        public int Count => organs.Count;
        /// <summary>Owned organs in the order they were attached.</summary>
        public List<OrganSO> OrganList => order.ConvertAll(e => e.Organ);
        public int MaxOrgans => maxOrgans;

        /// <summary>What would happen if this organ were attached.</summary>
        public AttachCheck Check(OrganSO organ)
        {
            if (Has(organ)) return AttachCheck.AlreadyOwned;
            // The body must hold enough of the nutrient this organ needs.
            var st = stats != null ? stats : GetComponent<SurvivalStats>(); // Awake may not have run yet (EditMode tests)
            bool lacks = st != null && !st.IsRemote && st.State.IsLacking(organ.EffectiveNutrient);
            if (maxOrgans > 0 && organs.Count >= maxOrgans) return AttachCheck.NoRoom;
            if (organs.Count >= Evolve.Swallow.SkillRules.MaxPassives) return AttachCheck.NoRoom;   // at most SkillRules.MaxPassives
            return lacks ? AttachCheck.LackNutrient : AttachCheck.Ok;
        }

        /// <summary>
        /// Biến Hình: sculpt a learned passive from body mass into a free passive slot, only in a shelter. The organ comes in at its stars in the book.
        /// </summary>
        public SculptResult TrySculpt(OrganSO organ, Evolve.Swallow.AnalysisBook book, Evolve.Swallow.SlimeBody body, bool inShelter)
        {
            if (organ == null) return SculptResult.Blocked;
            if (organ.Use != Evolve.Swallow.GeneUse.Passive) return SculptResult.NotPassive;   // active moves go on keys 1-5 (SkillBar), not on the body
            if (!inShelter) return SculptResult.NotInShelter;
            if (book == null || !book.Knows(organ)) return SculptResult.Unknown;
            var check = Check(organ);
            if (check != AttachCheck.Ok) return SculptResult.Blocked;
            var rarity = Evolve.Swallow.AnalysisRules.FromTier(organ.Tier);
            if (body == null || !Evolve.Swallow.MassRules.CanSculpt(true, true, body.Mass, rarity)) return SculptResult.NoMass;
            body.TrySpend(Evolve.Swallow.MassRules.SculptCost(rarity));
            TryAttach(organ, Mathf.Max(1, book.StarsOf(organ)));
            return SculptResult.Ok;
        }

        /// <summary>Biến Hình: take an organ off (shelter only) and get half its mass back. The organ stays learned.</summary>
        public bool Remove(OrganSO organ, Evolve.Swallow.SlimeBody body, bool inShelter)
        {
            if (!inShelter) return false;
            var old = Detach(organ);
            if (old == null) return false;
            if (body != null) body.AddMass(Evolve.Swallow.MassRules.Refund(Evolve.Swallow.AnalysisRules.FromTier(old.Tier)));
            return true;
        }

        /// <summary>Raise every worn organ to its stars in the book (also done automatically when the book changes).</summary>
        public void SyncStars(Evolve.Swallow.AnalysisBook book)
        {
            if (book == null) return;
            foreach (var e in order.ToArray()) SetLevel(e.Organ, book.StarsOf(e.Organ));
        }

        void SetLevel(OrganSO organ, int stars)
        {
            if (!Has(organ)) return;
            var e = organs[organ];
            stars = Mathf.Clamp(stars, 1, MaxLevel);
            if (stars <= e.Level) return;
            e.Level = stars;
            Debug.Log($"{name}: {organ.DisplayName} lên ★{stars}");
            LeveledUp?.Invoke(organ, stars);
            Recalculate();
        }

        /// <summary>Growth 0..1 while a new organ grows in, or -1 if not owned.</summary>
        public float GetGrowth(OrganSO organ) => Has(organ) ? organs[organ].Shown : -1f;

        /// <summary>Stars of the organ (1..MaxLevel); -1 if not owned.</summary>
        public int GetLevel(OrganSO organ) => Has(organ) ? organs[organ].Level : -1;

        public bool IsMaxed(OrganSO organ) => Has(organ) && organs[organ].Maxed;

        /// <summary>Attach the passive at a level (stars). No mass, shelter or book check: use TrySculpt for play.</summary>
        public bool TryAttach(OrganSO organ, int level = 1)
        {
            if (organ == null) return false;
            var check = Check(organ);
            if (check == AttachCheck.AlreadyOwned || check == AttachCheck.NoRoom || check == AttachCheck.LackNutrient) return false;

            var entry = new Entry { Organ = organ, Level = Mathf.Clamp(level, 1, MaxLevel) };
            if (organ.VisualPrefab != null && ShowsOnBody)   // no passive shows on the body (spec 6.1): the slime stays smooth; eyes are felt as sight, not seen
            {
                Transform parent = sockets.TryGetValue(organ.Slot, out var s) ? s : transform;
                entry.Visual = Instantiate(organ.VisualPrefab, parent, false);
                entry.VisualScale = entry.Visual.transform.localScale;
            }
            organs[organ] = entry;
            order.Add(entry);
            ApplyVisual(entry);
            Recalculate();
            Attached?.Invoke(organ);
            return true;
        }

        /// <summary>Remove a passive. Returns it, or null if it was not worn.</summary>
        public OrganSO Detach(OrganSO organ)
        {
            if (organ == null || !organs.TryGetValue(organ, out var e)) return null;
            if (e.Visual != null) Destroy(e.Visual);
            organs.Remove(organ);
            order.Remove(e);
            Recalculate();
            Removed?.Invoke(e.Organ);
            return e.Organ;
        }

        /// <summary>Debug: make every new organ fully grown immediately.</summary>
        public void DebugGrowAll()
        {
            foreach (var e in order) { e.Shown = 1f; ApplyVisual(e); }
            Recalculate();
        }

        /// <summary>
        /// Remote avatars: show this organ at exactly this level (the real owner learns and sculpts on their own machine).
        /// </summary>
        public void MirrorOrgan(OrganSO organ, int level)
        {
            if (organ == null) return;
            level = Mathf.Clamp(level, 1, MaxLevel);
            if (Has(organ) && organs[organ].Level > level) Detach(organ);
            if (!Has(organ)) TryAttach(organ, level);
            if (!Has(organ)) return;
            var e = organs[organ];
            e.Level = level;
            e.Shown = 1f;
            Recalculate();
        }

        /// <summary>Debug: raise every organ to at least the given level (1..MaxLevel).</summary>
        public void DebugSetLevel(int level)
        {
            level = Mathf.Clamp(level, 1, MaxLevel);
            foreach (var e in order) { if (e.Level < level) e.Level = level; e.Shown = 1f; }
            Recalculate();
        }

        void Update()
        {
            WatchBook();
            bool changed = false;
            foreach (var e in order)
            {
                if (e.Shown >= 1f) continue;
                e.Shown = Mathf.MoveTowards(e.Shown, 1f, visualCatchUp * Time.deltaTime);
                ApplyVisual(e);
                changed = true;
                if (e.Shown >= 1f) Completed?.Invoke(e.Organ);
            }
            if (changed) Recalculate();
            UpdateStarvation();
        }

        /// <summary>The nutrient an organ needs running out weakens it, and in the end dissolves it (it stays learned).</summary>
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
                    cfg.OrganDissolveSeconds, cfg.OrganRecoverSeconds / DiseaseRules.OrganRecoverMultiplier(stats.State.Has(DiseaseKind.RottenGel)), Time.deltaTime);
                if (!Mathf.Approximately(next, e.Decay))
                {
                    if ((int)(next * 50f) != (int)(e.Decay * 50f)) decayChanged = true; // recalculate only per 2% step, not every frame
                    e.Decay = next;
                }
                if (e.Decay >= 1f && dissolve == null) dissolve = e;
            }
            if (decayChanged) Recalculate();
            if (dissolve == null) return;

            var organ = dissolve.Organ;
            Debug.Log($"{name}: {organ.DisplayName} tan vì thiếu {NutrientKinds.SlimeName(organ.EffectiveNutrient)}");
            Detach(organ);
            Dissolved?.Invoke(organ);
        }

        /// <summary>0 healthy .. 1 dissolved; -1 if not owned.</summary>
        public float GetDecay(OrganSO organ) => Has(organ) ? organs[organ].Decay : -1f;

        void ApplyVisual(Entry e)
        {
            if (e.Visual == null) return;
            // New organs grow in; each extra star makes the organ a bit bigger.
            float levelScale = 1f + 0.1f * Mathf.Max(0, e.Level - 1);
            e.Visual.transform.localScale = e.VisualScale * Mathf.Lerp(0.15f, 1f, e.Shown) * levelScale * (1f - 0.3f * e.Decay); // a starving organ visibly shrinks
        }

        /// <summary>Effect strength of an organ: growth × level bonus.</summary>
        float Strength(Entry e) => e.Shown * (1f + levelBonus * Mathf.Max(0, e.Level - 1)) * (e.Organ.GrantsSight ? 1f : OrganDecay.StrengthFactor(e.Decay)); // eyes keep their sight until they dissolve (no slow dimming of the world)

        void Recalculate()
        {
            float speed = 1f, sight = 0f, contact = 0f, heat = 0f, hp = 0f, reduce = 0f, sonar = 1f;
            foreach (var e in order)
            {
                ApplyVisual(e);
                if (e.Organ.Use == Evolve.Swallow.GeneUse.Active) continue;   // an active move changes nothing while it rests
                float k = Strength(e);
                speed *= 1f + (e.Organ.SpeedMultiplier - 1f) * k;
                if (e.Organ.GrantsSight) sight = Mathf.Max(sight, k);
                contact += e.Organ.ContactDamage * k;
                heat += e.Organ.HeatResistance * k;
                hp += e.Organ.MaxHealthBonus * k;
                reduce += e.Organ.DamageReduction * k;
                sonar = Mathf.Max(sonar, 1f + (e.Organ.SonarRangeMultiplier - 1f) * k);
            }
            if (stats != null && stats.State.NutrientLoad != null)
            {
                for (int k = 0; k < stats.State.NutrientLoad.Length; k++) stats.State.NutrientLoad[k] = 0;
                foreach (var e in order) stats.State.NutrientLoad[(int)e.Organ.EffectiveNutrient]++;
            }
            SpeedMultiplier = speed;
            Sight = sight;
            passiveContact = contact;
            HeatResistance = heat;
            if (stats != null) stats.State.HeatResistance = heat;
            DamageReduction = reduce;
            SonarRangeMultiplier = sonar;
            if (!Mathf.Approximately(hp, MaxHealthBonus)) { MaxHealthBonus = hp; if (TryGetComponent(out Evolve.Swallow.SlimeBody body)) body.Refresh(); }
            Changed?.Invoke();
        }
    }
}
