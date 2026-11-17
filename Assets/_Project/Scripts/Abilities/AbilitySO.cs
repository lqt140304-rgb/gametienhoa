using UnityEngine;

namespace Evolve.Abilities
{
    public enum AbilityTier { C, B, A, S }

    /// <summary>Base data for an active Dị Năng. Subclass and override Activate.</summary>
    public abstract class AbilitySO : ScriptableObject
    {
        public string DisplayName;
        [TextArea] public string Description;
        public AbilityTier Tier = AbilityTier.C;
        public float EnergyCost = 20f;
        public float Cooldown = 5f;

        /// <summary>Apply the effect. Return false if it could not be used.</summary>
        public abstract bool Activate(GameObject user);

        /// <summary>
        /// Apply the effect for a skill granted by an organ: level 2 = just unlocked, 3 = upgraded.
        /// By default the level is ignored.
        /// </summary>
        public virtual bool Activate(GameObject user, int level) => Activate(user);

        /// <summary>Cooldown at an organ level (proposed: Lv3 is 25% shorter).</summary>
        public virtual float CooldownFor(int level) => level >= 3 ? Cooldown * 0.75f : Cooldown;

        /// <summary>Effect strength multiplier at an organ level (proposed: Lv3 is 50% stronger).</summary>
        public static float Power(int level) => level >= 3 ? 1.5f : 1f;
    }
}
