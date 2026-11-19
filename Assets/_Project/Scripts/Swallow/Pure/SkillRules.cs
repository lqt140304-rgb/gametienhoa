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
        public static GeneUse UseOf(bool hasAbility, bool grantsSight, float heatResistance, float speedMultiplier, float contactDamage)
        {
            if (hasAbility) return GeneUse.Active;
            if (grantsSight || heatResistance > 0f) return GeneUse.Passive;
            if (speedMultiplier > 1.01f) return GeneUse.Active;
            if (contactDamage > 0f) return GeneUse.Passive;
            return GeneUse.None;
        }

        /// <summary>Level the old ability code expects: its Lv2 is the base move, Lv3 the upgrade. ★1 and ★2 are the base, ★3 the upgrade.</summary>
        public static int AbilityLevel(int stars) => stars >= 3 ? 3 : 2;

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
    /// <summary>Time-limited Sprint boost: overlapping Sprints take the stronger factor and the later end, never multiplying.</summary>
    public class SprintTimer
    {
        float mult = 1f, until;

        public float Multiplier(float now) => now < until ? mult : 1f;

        public void Apply(float now, float multiplier, float seconds)
        {
            float end = now + seconds;
            if (now < until)
            {
                mult = SkillRules.CombineBoost(mult, multiplier);
                until = System.Math.Max(until, end);
            }
            else
            {
                mult = multiplier;
                until = end;
            }
        }
    }
}
