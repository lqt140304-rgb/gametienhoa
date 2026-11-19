using Evolve.Organs;
using Evolve.Swallow;
using UnityEngine;

namespace Evolve.Abilities
{
    /// <summary>
    /// Chạy Nước Rút: the move behind every leg gene (Chân giả, Chân chạy). The legs grow out, the body runs faster for a few
    /// seconds (4 / 5 / 6 by stars), then they melt back in. Made at run time by SkillBar from the leg organ's data, so no asset is needed.
    /// As with every move, the "level" passed to Activate and CooldownFor is the gene's stars (1..3) for this one.
    /// </summary>
    public class SprintAbility : AbilitySO
    {
        public float Multiplier = 1.4f;

        /// <summary>Fill this move from the leg organ it belongs to.</summary>
        public void Setup(OrganSO organ)
        {
            hideFlags = HideFlags.HideAndDontSave;
            name = "Sprint_" + organ.name;
            DisplayName = "Chạy Nước Rút";
            Description = $"Chân mọc ra, chạy nhanh ×{organ.SpeedMultiplier:0.#} trong vài giây (lâu hơn khi lên sao), rồi thu vào thân.";
            Tier = organ.Tier switch { OrganTier.A => AbilityTier.A, OrganTier.S => AbilityTier.S, OrganTier.B => AbilityTier.B, _ => AbilityTier.C };
            EnergyCost = SkillRules.SprintEnergy;
            Cooldown = SkillRules.SprintCooldown;
            Multiplier = organ.SpeedMultiplier;
        }

        public override bool Activate(GameObject user) => Activate(user, 1);

        public override bool Activate(GameObject user, int stars)
        {
            var boost = user.GetComponent<SprintBoost>();
            if (boost == null) boost = user.AddComponent<SprintBoost>();
            boost.Apply(Multiplier, SkillRules.SprintSeconds(stars));
            return true;
        }
    }
}
