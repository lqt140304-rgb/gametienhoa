using UnityEngine;

namespace Evolve.Organs
{
    /// <summary>Where on the body an organ grows. One organ per slot.</summary>
    public enum OrganSlot { Eyes, Legs, Skin, Back }

    /// <summary>
    /// Data for one evolvable organ. Effects are plain fields so new organs can be
    /// made as assets without code (Create → Evolve → Organ).
    /// </summary>
    [CreateAssetMenu(menuName = "Evolve/Organ", fileName = "OrganSO")]
    public class OrganSO : ScriptableObject
    {
        public string DisplayName;
        [TextArea] public string Description;
        public OrganSlot Slot;
        [Tooltip("Rarity: C common … S legendary")]
        public OrganTier Tier = OrganTier.C;
        [Tooltip("Extra rarity factor inside its tier (1 = normal, 0.5 = half as likely)")]
        [Min(0)] public float DropWeight = 1f;
        [Tooltip("Model spawned on the slime's socket for this slot")]
        public GameObject VisualPrefab;

        [Header("Growth")]
        [Tooltip("Food that must be eaten to grow from bud to full organ (0 = instant)")]
        public float GrowthFoodRequired = 30f;

        [Header("Effects (at full growth)")]
        [Tooltip("Gives colour vision and long view range")]
        public bool GrantsSight;
        [Tooltip("Multiplies movement speed")]
        public float SpeedMultiplier = 1f;
        [Tooltip("Extra °C of heat the body tolerates")]
        public float HeatResistance;
        [Tooltip("Damage dealt back to a creature that bites this body")]
        public float ContactDamage;
    }
}
