using UnityEngine;

namespace Evolve.Creatures
{
    /// <summary>What a prey's counter-attack can infect with (mapped to a disease in the scene).</summary>
    public enum InfectKind { None, Spore, Rot, Burn }

    /// <summary>What a prey decides to do this moment (flags: several can happen at once).</summary>
    [System.Flags] public enum DefenseAction { None = 0, Escape = 1, Counter = 2, Alarm = 4, CallHelp = 8 }

    /// <summary>
    /// How one prey species fights back (spec: prey are not just food). One escape (used when a threat comes close) and 0-1 counter (used when
    /// it is grabbed, hit, cornered or worn out). All numbers are provisional.
    /// </summary>
    public sealed class PreyDefenseDef
    {
        public string Id, Species, Cave, HelperName;
        /// <summary>1 = a nuisance, 2 = hurts, 3 = dangerous (rare prey only).</summary>
        public int Difficulty = 1;
        /// <summary>How close a threat may come before it escapes (metres).</summary>
        public float AlertRange = 6f;
        /// <summary>Chance that an escape saves it from a hunter in the ecosystem model (0..1).</summary>
        public float EscapeChance;

        // ---- escape ----
        public float EscapeCooldown = 8f;
        /// <summary>Run: speed multiplier for SpeedSeconds (Zigzag weaves; Silent makes no vibration).</summary>
        public float SpeedBoost = 1f, SpeedSeconds;
        public bool Zigzag, Silent;
        /// <summary>Guard: stand still for GuardSeconds taking GuardDamageTaken of the damage; BlocksCatch stops a slime swallowing it meanwhile.</summary>
        public float GuardSeconds, GuardDamageTaken = 1f;
        public bool BlocksCatch;
        /// <summary>Threats within ObscureRadius lose track of it for ObscureSeconds (flash, smoke, camouflage, hiding).</summary>
        public float ObscureRadius, ObscureSeconds;
        /// <summary>A puddle that slows whoever is in it.</summary>
        public float SlowRadius, SlowFraction, SlowSeconds;
        /// <summary>Spikes left behind on the ground.</summary>
        public float TrapDamage, TrapSeconds;
        /// <summary>Leaves a decoy where it stood (Tắc Kè Đá). EscapesGrab also frees it when a slime has already touched it.</summary>
        public bool Decoy, EscapesGrab;

        // ---- counter ----
        public float CounterCooldown = 4f;
        public float CounterDamage, CounterPush, CounterStun, CounterSlow, CounterSlowSeconds;
        public InfectKind CounterInfect;
        /// <summary>Share of the damage it deals that heals itself (Muỗi Rêu).</summary>
        public float CounterDrain;

        // ---- special mechanics ----
        /// <summary>Calls the herd: prey within this many metres run too (Dế Gió).</summary>
        public float AlarmRadius;
        /// <summary>Cries for HelperName (a Predator) within this many metres (Chim Non calls Chim Hang Mẹ).</summary>
        public float CallHelpRadius;

        public bool HasEscape => SpeedBoost > 1f || GuardSeconds > 0f || ObscureSeconds > 0f || (SlowSeconds > 0f && SlowRadius > 0f) || TrapSeconds > 0f || Decoy;
        public bool HasCounter => CounterDamage > 0f || CounterPush > 0f || CounterStun > 0f || CounterSlow > 0f || CounterInfect != InfectKind.None;
    }

    /// <summary>Everything the decision needs; filled each moment by the creature.</summary>
    public struct DefenseInput
    {
        public float Now, ThreatDistance, ThreatRatio, EscapeReadyAt, CounterReadyAt, Roll;
        public bool ThreatSeen, JustHit, Cornered, Tired, Guarding, AlarmSent, CallSent;
    }

    /// <summary>Pure rules for prey defence, so they can be tested without Unity.</summary>
    public static class PreyDefenseRules
    {
        public const float CounterFullUntil = 1.5f, CounterNoneFrom = 4f, MaxFleeStretch = 0.5f;

        /// <summary>Threat mass over own mass (1 = equal). A bigger threat is noticed (and fled from) sooner.</summary>
        public static float FleeRange(float alertRange, float ratio) => alertRange * (1f + MaxFleeStretch * Mathf.Clamp01((ratio - 1f) / 2f));

        /// <summary>Willingness to fight back: always against threats up to 1.5x its mass, never against ones 4x or more.</summary>
        public static float CounterWillingness(float ratio) =>
            ratio <= CounterFullUntil ? 1f : ratio >= CounterNoneFrom ? 0f : 1f - (ratio - CounterFullUntil) / (CounterNoneFrom - CounterFullUntil);

        /// <summary>A bigger threat is harder to escape: 15% less chance per extra body of mass, at most 45% less.</summary>
        public static float EscapeChanceVs(float baseChance, float ratio) => Mathf.Clamp01(baseChance * (1f - 0.15f * Mathf.Clamp(ratio - 1f, 0f, 3f)));

        public static DefenseAction Decide(PreyDefenseDef def, DefenseInput i)
        {
            var act = DefenseAction.None;
            if (def == null) return act;
            if (i.ThreatSeen && def.AlarmRadius > 0f && !i.AlarmSent) act |= DefenseAction.Alarm;
            if (i.JustHit && def.CallHelpRadius > 0f && !i.CallSent) act |= DefenseAction.CallHelp;

            // worn out replaces the old "tired and helpless": a prey that cannot run any more bites back
            bool pressed = i.JustHit || i.Cornered || i.Tired;
            if (def.HasCounter && pressed && i.Now >= i.CounterReadyAt && i.Roll < CounterWillingness(i.ThreatRatio)) act |= DefenseAction.Counter;

            bool near = i.ThreatSeen && i.ThreatDistance <= FleeRange(def.AlertRange, i.ThreatRatio);
            if (def.HasEscape && near && !i.Guarding && i.Now >= i.EscapeReadyAt) act |= DefenseAction.Escape;
            return act;
        }
    }
}
