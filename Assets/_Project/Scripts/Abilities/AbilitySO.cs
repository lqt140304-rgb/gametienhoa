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
    }
}
