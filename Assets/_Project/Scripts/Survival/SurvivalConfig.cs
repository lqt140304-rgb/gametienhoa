using UnityEngine;

namespace Evolve.Survival
{
    [CreateAssetMenu(menuName = "Evolve/Survival Config", fileName = "SurvivalConfigSO")]
    public class SurvivalConfig : ScriptableObject
    {
        [Tooltip("Hunger lost per second")] public float HungerDecayPerSec = 0.5f;
        [Tooltip("Energy regained per second while resting")] public float EnergyRegenPerSec = 10f;
        [Tooltip("Health lost per second at 0 hunger")] public float StarveDamagePerSec = 4f;
        [Tooltip("Regenerate health when hunger >= this fraction")] [Range(0, 1)] public float HealHungerThreshold = 0.7f;
        public float HealPerSec = 1.5f;
        [Tooltip("Energy cost of one hop")] public float HopEnergyCost = 8f;
        [Tooltip("Energy drained per second while sprinting")] public float SprintEnergyPerSec = 15f;
    }
}
