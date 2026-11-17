using UnityEngine;

namespace Evolve.Survival
{
    [CreateAssetMenu(menuName = "Evolve/Survival Config", fileName = "SurvivalConfigSO")]
    public class SurvivalConfig : ScriptableObject
    {
        [Tooltip("Hunger lost per second while moving (none while still)")] public float HungerDecayPerSec = 0.5f;
        [Tooltip("Energy regained per second while resting")] public float EnergyRegenPerSec = 10f;
        [Tooltip("Health lost per second at 0 hunger")] public float StarveDamagePerSec = 4f;
        [Tooltip("Regenerate health when hunger >= this fraction")] [Range(0, 1)] public float HealHungerThreshold = 0.7f;
        public float HealPerSec = 1.5f;
        [Tooltip("Energy cost of one hop")] public float HopEnergyCost = 8f;
        [Tooltip("Extra hunger lost per second while sprinting")] public float SprintHungerPerSec = 1.5f;

        [Header("Temperature (°C)")]
        [Tooltip("Air temperature outside any zone")] public float DefaultAmbient = 22f;
        [Tooltip("Below this the body takes cold damage")] public float ComfortMin = 5f;
        [Tooltip("Above this the body takes heat damage (plus organ heat resistance)")] public float ComfortMax = 35f;
        [Tooltip("How fast body temperature follows the air, °C per second")] public float TemperatureChangePerSec = 1.5f;
        [Tooltip("Damage per second for each degree outside the comfort range")] public float DamagePerDegreePerSec = 0.2f;
    }
}
