using System;
using System.Collections.Generic;
using Evolve.Organs;
using Evolve.Survival;
using UnityEngine;

namespace Evolve.Abilities
{
    /// <summary>
    /// The 5 skill slots (keys 1-5) for skills granted by organs. A skill unlocks when its organ reaches
    /// AbilityUnlockLevel (Lv2) and is upgraded at Lv3. It goes into the first free slot; the player can move
    /// it in the body menu (G). Losing the organ loses the skill. The starting Dị Năng (key Q) is separate
    /// (AbilityCaster) and does not use these slots. Cooldown belongs to the organ, so swapping slots does not reset it.
    /// </summary>
    [RequireComponent(typeof(SurvivalStats), typeof(OrganHost))]
    public class SkillBar : MonoBehaviour
    {
        public const int SlotCount = 5;

        readonly OrganSO[] slots = new OrganSO[SlotCount];
        readonly Dictionary<OrganSO, float> readyAt = new Dictionary<OrganSO, float>();
        readonly HashSet<OrganSO> unequipped = new HashSet<OrganSO>();
        SurvivalStats stats;
        OrganHost host;

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

        public OrganSO GetSlot(int slot) => slot >= 0 && slot < SlotCount ? slots[slot] : null;

        /// <summary>True when the organ is worn and high enough level to use its skill.</summary>
        public bool IsUnlocked(OrganSO organ) =>
            organ != null && organ.GrantedAbility != null && host.Has(organ) && host.GetLevel(organ) >= organ.AbilityUnlockLevel;

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
            float cd = organ.GrantedAbility.CooldownFor(host.GetLevel(organ));
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

        /// <summary>Drop skills whose organ is gone and put newly unlocked ones in the first free slot.</summary>
        void Sync()
        {
            unequipped.RemoveWhere(o => !IsUnlocked(o));
            for (int i = 0; i < SlotCount; i++)
                if (slots[i] != null && !IsUnlocked(slots[i])) slots[i] = null;

            foreach (var organ in host.OrganList)
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
            if (organ == null) { Fail($"ô {slot + 1} trống (cần cơ quan bậc B lên Lv2)"); return; }
            var ability = organ.GrantedAbility;
            int level = host.GetLevel(organ);

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
            var motion = ability is BurstDashAbility || ability is ChargeAbility
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
        }
    }
}
