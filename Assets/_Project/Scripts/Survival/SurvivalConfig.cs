using UnityEngine;

namespace Evolve.Survival
{
    [CreateAssetMenu(menuName = "Evolve/Survival Config", fileName = "SurvivalConfigSO")]
    public class SurvivalConfig : ScriptableObject
    {
        [Tooltip("Hunger lost per second while moving (none while still)")] public float HungerDecayPerSec = 0.5f;
        [Tooltip("Energy regained per second while standing still")] public float EnergyRegenPerSec = 10f;
        [Tooltip("Energy regained per second while moving (slower than resting)")] public float EnergyRegenMovingPerSec = 3f;
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

        [Header("Thirst")]
        [Tooltip("Thirst lost per second (only where thirst is enabled)")] public float ThirstDecayPerSec = 0.35f;
        [Tooltip("Extra thirst lost per second while sprinting")] public float ThirstSprintExtraPerSec = 1.0f;
        [Tooltip("Extra thirst lost per second when the air is hot (Ambient > ComfortMax - 5)")] public float ThirstHotExtraPerSec = 0.3f;
        [Tooltip("Thirst regained per second while drinking")] public float DrinkPerSec = 25f;
        [Tooltip("Health lost per second at 0 thirst")] public float DehydrationDamagePerSec = 3f;
        [Tooltip("Health regenerates only when thirst >= this fraction (if thirst is enabled)")] [Range(0, 1)] public float HealThirstThreshold = 0.5f;

        [Header("Stress")]
        [Tooltip("Stress gained per point of damage taken from outside (e.g. a predator bite)")] public float StressPerDamage = 0.5f;
        [Tooltip("Stress gained per second while hunger or thirst is under 25%")] public float StressLowNeedsPerSec = 0.4f;
        [Tooltip("Stress gained per second while body temperature is outside the comfort range")] public float StressBadTempPerSec = 0.4f;
        [Tooltip("Stress lost per second when well fed and watered (hunger >= 70%, thirst >= 50%)")] public float StressCalmDecayPerSec = 0.3f;
        [Tooltip("Stress lost per second while inside a shelter")] public float StressShelterDecayPerSec = 2f;
        [Tooltip("Energy regen multiplier while inside a shelter")] public float ShelterEnergyRegenMultiplier = 2f;
        [Tooltip("Energy regen multiplier at high stress")] public float StressHighEnergyRegenMultiplier = 0.5f;
        [Tooltip("Stress above which the camera starts to shake")] public float StressShakeStart = 40f;
        [Tooltip("Stress above which the sense radius starts to shrink")] public float StressSenseStart = 60f;
        [Tooltip("Stress at which energy regen is reduced")] public float StressEnergyStart = 80f;
    }
}
