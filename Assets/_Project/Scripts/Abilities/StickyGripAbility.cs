using UnityEngine;

namespace Evolve.Abilities
{
    /// <summary>Bám Dính (C): for a few seconds the membrane turns sticky.</summary>
    [CreateAssetMenu(menuName = "Evolve/Abilities/Sticky Grip", fileName = "StickyGripSO")]
    public class StickyGripAbility : AbilitySO
    {
        public float Duration = 4f;
        [Tooltip("Nutrient absorb rate multiplier while active")]
        public float AbsorbMultiplier = 3f;

        void Reset() { DisplayName = "Bám Dính"; EnergyCost = 15f; Cooldown = 8f; }

        public override bool Activate(GameObject user)
        {
            if (!user.TryGetComponent(out StickyGrip grip)) grip = user.AddComponent<StickyGrip>();
            grip.Begin(Duration, AbsorbMultiplier);
            return true;
        }
    }
}
