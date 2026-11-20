using UnityEngine;

namespace Evolve.Swallow
{
    /// <summary>What a learned gene is for: nothing, an always-on passive, or an active move on keys 1-5.</summary>
    public enum GeneUse { None, Passive, Active }

    /// <summary>
    /// Rules for turning learned genes into skills (spec 6.1). The slime has no worn organs: passives are always on
    /// (at most 3, changed only in a shelter); active moves grow their body part only while they are used.
    /// </summary>
    public static class SkillRules
    {
        public const int MaxPassives = 3;
        public const float SprintCooldown = 10f;
        public const float SprintEnergy = 15f;
        /// <summary>A body part grows out in this long, stays out while the move lasts, and melts back in this long.</summary>
        public const float GrowSeconds = 0.15f, RetractSeconds = 0.35f;

        /// <summary>
        /// Sorts a gene by what its organ data says: an own move is always Active; eyes and heat resistance are Passive;
        /// extra speed becomes the Sprint move (Active); spikes without a move are a Passive thorn skin.
        /// </summary>
        public static GeneUse UseOf(bool hasAbility, bool grantsSight, float heatResistance, float speedMultiplier, float contactDamage, bool otherPassive = false)
        {
            if (hasAbility) return GeneUse.Active;
            if (grantsSight || heatResistance > 0f || otherPassive) return GeneUse.Passive;
            if (speedMultiplier > 1.01f) return GeneUse.Active;
            if (contactDamage > 0f) return GeneUse.Passive;
            return GeneUse.None;
        }

        /// <summary>Level the old ability code expects: its Lv2 is the base move, Lv3 the upgrade. ★1 is the base, ★2 and ★3 the upgrade (min(3, stars + 1)).</summary>
        public static int AbilityLevel(int stars) => stars >= 2 ? 3 : 2;

        /// <summary>Seconds the Sprint lasts: 4 / 5 / 6 for ★1 / ★2 / ★3.</summary>
        public static float SprintSeconds(int stars) => 3f + Mathf.Clamp(stars, 1, 3);

        public static float ShowSeconds(float hold) => GrowSeconds + Mathf.Max(0f, hold) + RetractSeconds;

        /// <summary>0..1 size of a body part <paramref name="elapsed"/> seconds after the move started: grows, holds, melts back.</summary>
        public static float VisualScale(float elapsed, float hold)
        {
            if (elapsed <= 0f) return 0f;
            if (elapsed < GrowSeconds) return elapsed / GrowSeconds;
            float afterHold = elapsed - GrowSeconds - Mathf.Max(0f, hold);
            if (afterHold <= 0f) return 1f;
            return Mathf.Clamp01(1f - afterHold / RetractSeconds);
        }

        /// <summary>Two Sprints overlapping do not multiply: the stronger one counts.</summary>
        public static float CombineBoost(float a, float b) => Mathf.Max(a, b);
    }
}

namespace Evolve.Swallow
{
    /// <summary>
    /// Time-limited Sprint boost. Overlapping Sprints never multiply: the strongest one still running counts.
    /// When the strong one ends, a weaker one that lasts longer carries on at its own factor.
    /// </summary>
    public class SprintTimer
    {
        readonly System.Collections.Generic.List<(float mult, float until)> running = new System.Collections.Generic.List<(float, float)>();

        public float Multiplier(float now)
        {
            float best = 1f;
            for (int i = running.Count - 1; i >= 0; i--)
            {
                if (now >= running[i].until) { running.RemoveAt(i); continue; }
                best = SkillRules.CombineBoost(best, running[i].mult);
            }
            return best;
        }

        public void Apply(float now, float multiplier, float seconds) => running.Add((multiplier, now + seconds));

        public void Reset() => running.Clear();
    }
}
