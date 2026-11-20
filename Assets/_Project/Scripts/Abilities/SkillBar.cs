using System;
using System.Collections.Generic;
using Evolve.Organs;
using Evolve.Survival;
using Evolve.Swallow;
using UnityEngine;

namespace Evolve.Abilities
{
    /// <summary>
    /// The 5 skill slots (keys 1-5) for the active moves Giải Mã has taught (a gene whose SkillRules.UseOf is Active).
    /// A move is usable from ★1 and is upgraded at ★3. A newly learned move goes into the first free slot; the player can
    /// move it in the skill menu (K) anywhere, at no cost. The starting Dị Năng (key Q) is separate (AbilityCaster).
    /// Cooldown belongs to the gene, so swapping slots does not reset it. The body part of a move appears only while it is used (SkillVisuals).
    /// </summary>
    [RequireComponent(typeof(SurvivalStats), typeof(OrganHost))]
    public class SkillBar : MonoBehaviour
    {
        public const int SlotCount = 5;

        readonly OrganSO[] slots = new OrganSO[SlotCount];
        readonly Dictionary<OrganSO, float> readyAt = new Dictionary<OrganSO, float>();
        readonly HashSet<OrganSO> unequipped = new HashSet<OrganSO>();
        readonly Dictionary<OrganSO, SprintAbility> sprints = new Dictionary<OrganSO, SprintAbility>();
        readonly List<OrganSO> unlockedList = new List<OrganSO>();
        SurvivalStats stats;
        OrganHost host;
        SkillVisuals visuals;

        /// <summary>Why the last skill key press did nothing (empty after a success). The HUD shows it for a moment.</summary>
        public string FailureReason { get; private set; } = "";
        public float FailureTime { get; private set; } = -10f;

        void Fail(string reason)
        {
            FailureReason = reason;
            FailureTime = Time.time;
        }

        /// <summary>Raised when a skill unlocks and is put in a slot (organ, slot index).</summary>
        public event Action<OrganSO, int> SkillUnlocked;

        void Awake()
        {
            stats = GetComponent<SurvivalStats>();
            host = GetComponent<OrganHost>();
        }

        void OnDestroy() { foreach (var s in sprints.Values) if (s != null) Destroy(s); sprints.Clear(); }

        public OrganSO GetSlot(int slot) => slot >= 0 && slot < SlotCount ? slots[slot] : null;

        AnalysisBook Book => host != null ? host.Book : null;

        /// <summary>True when Giải Mã has learned the gene and it is an active move.</summary>
        public bool IsUnlocked(OrganSO organ) =>
            organ != null && organ.Use == GeneUse.Active && Book != null && Book.Knows(organ);

        /// <summary>The move behind a gene: its own ability, or Chạy Nước Rút for a leg gene with extra speed.</summary>
        public AbilitySO AbilityFor(OrganSO organ)
        {
            if (organ == null) return null;
            if (organ.GrantedAbility != null) return organ.GrantedAbility;
            if (organ.Use != GeneUse.Active) return null;
            if (!sprints.TryGetValue(organ, out var sprint) || sprint == null)
            {
                sprint = ScriptableObject.CreateInstance<SprintAbility>();
                sprint.Setup(organ);
                sprints[organ] = sprint;
            }
            return sprint;
        }

        /// <summary>Stars of the gene (1..3), or 0 when it is not learned.</summary>
        public int StarsOf(OrganSO organ) => Book != null ? Book.StarsOf(organ) : 0;

        /// <summary>The level the move runs at: its stars for Sprint, the old Lv2 / Lv3 scale for the others.</summary>
        public int LevelFor(OrganSO organ)
        {
            int stars = Mathf.Max(1, StarsOf(organ));
            return AbilityFor(organ) is SprintAbility ? stars : SkillRules.AbilityLevel(stars);
        }

        /// <summary>Every learned active move (what the skill menu lists).</summary>
        public List<OrganSO> Unlocked()
        {
            unlockedList.Clear();
            var book = Book;
            if (book != null) foreach (var o in book.Known) if (o.Use == GeneUse.Active) unlockedList.Add(o);
            return unlockedList;
        }

        /// <summary>Slot the organ's skill is in, or -1.</summary>
        public int SlotOf(OrganSO organ)
        {
            for (int i = 0; i < SlotCount; i++) if (slots[i] == organ) return i;
            return -1;
        }

        /// <summary>Put the organ's skill in a slot; swaps with whatever is there (or leaves the old slot empty).</summary>
        public void Assign(int slot, OrganSO organ)
        {
            if (slot < 0 || slot >= SlotCount || !IsUnlocked(organ)) return;
            int old = SlotOf(organ);
            unequipped.Remove(organ);
            if (old == slot) return;
            if (old >= 0) slots[old] = slots[slot];
            slots[slot] = organ;
        }

        public void Clear(int slot)
        {
            if (slot >= 0 && slot < SlotCount)
            {
                if (slots[slot] != null) unequipped.Add(slots[slot]);
                slots[slot] = null;
            }
        }

        public float CooldownRemaining(int slot)
        {
            var organ = GetSlot(slot);
            if (organ == null || !readyAt.TryGetValue(organ, out float t)) return 0f;
            return Mathf.Max(0f, t - Time.time);
        }

        /// <summary>0 = just used, 1 = ready (or empty slot).</summary>
        public float CooldownProgress(int slot)
        {
            var organ = GetSlot(slot);
            if (organ == null) return 1f;
            float cd = AbilityFor(organ).CooldownFor(LevelFor(organ));
            return cd <= 0f ? 1f : 1f - Mathf.Clamp01(CooldownRemaining(slot) / cd);
        }

        /// <summary>Debug: make every skill ready.</summary>
        public void ResetCooldowns() => readyAt.Clear();

        void Update()
        {
            Sync();
            int pressed = -1;
            for (int i = 0; i < SlotCount; i++)
                if (KeyPressed.Digit(i + 1)) { pressed = i; break; }
            if (pressed < 0) return;

            if (stats.State.IsDead) { Fail("đang chết"); return; }
            if (SurvivalHUD.BlocksInput(gameObject)) { Fail("đang mở một menu (bấm Esc để đóng)"); return; }
            Use(pressed);
        }

        /// <summary>Drop moves that are no longer learned (a new reincarnation) and put newly learned ones in the first free slot.</summary>
        void Sync()
        {
            unequipped.RemoveWhere(o => !IsUnlocked(o));
            for (int i = 0; i < SlotCount; i++)
                if (slots[i] != null && !IsUnlocked(slots[i])) slots[i] = null;

            foreach (var organ in Unlocked())
            {
                if (!IsUnlocked(organ) || SlotOf(organ) >= 0 || unequipped.Contains(organ)) continue;
                int free = Array.IndexOf(slots, null);
                if (free < 0) return; // all slots full: the player assigns it in the body menu
                slots[free] = organ;
                SkillUnlocked?.Invoke(organ, free);
            }
        }

        void Use(int slot)
        {
            var organ = slots[slot];
            if (organ == null) { Fail($"ô {slot + 1} trống (nuốt sinh vật có kỹ năng, để Giải Mã học)"); return; }
            var ability = AbilityFor(organ);
            int level = LevelFor(organ);

            if (readyAt.TryGetValue(organ, out float t) && Time.time < t)
            {
                Fail($"{ability.DisplayName} đang hồi chiêu ({t - Time.time:0.0}s)");
                return;
            }
            if (stats.State.Energy < ability.EnergyCost)
            {
                Fail($"thiếu năng lượng cho {ability.DisplayName} ({stats.State.Energy:0}/{ability.EnergyCost:0})");
                return;
            }
            if (pending) { Fail("đang tụ lực"); return; }

            // The body winds up first; the skill leaves at the moment the animation releases.
            var motion = ability is BurstDashAbility || ability is ChargeAbility || ability is SprintAbility
                ? Evolve.Visuals.GelMotion.Dash : Evolve.Visuals.GelMotion.Release;
            Evolve.Visuals.SlimeGelAnimator.Play(gameObject, motion);
            StartCoroutine(FireAfter(Evolve.Visuals.SlimeGelAnimator.WindUp(motion), organ, ability, level));
        }

        bool pending;

        System.Collections.IEnumerator FireAfter(float delay, OrganSO organ, AbilitySO ability, int level)
        {
            pending = true;
            if (delay > 0f) yield return new WaitForSeconds(delay);
            pending = false;
            if (stats.State.IsDead) yield break;
            if (stats.State.Energy < ability.EnergyCost) { Fail($"thiếu năng lượng cho {ability.DisplayName}"); yield break; }
            if (!ability.Activate(gameObject, level)) { Fail($"{ability.DisplayName} không dùng được lúc này"); yield break; }
            stats.State.TrySpendEnergy(ability.EnergyCost);
            readyAt[organ] = Time.time + ability.CooldownFor(level);
            FailureReason = "";
            // The body part (spikes, legs) grows out for the move and melts back in while it cools down.
            if (visuals == null) visuals = GetComponent<SkillVisuals>();
            if (visuals == null) visuals = gameObject.AddComponent<SkillVisuals>();
            visuals.Play(organ, SkillVisuals.HoldFor(ability, Mathf.Max(1, StarsOf(organ))));
        }
    }
}
