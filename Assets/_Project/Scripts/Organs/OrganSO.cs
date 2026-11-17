using UnityEngine;

namespace Evolve.Organs
{
    /// <summary>Where on the body an organ grows. One organ per slot.</summary>
    public enum OrganSlot { Eyes, Legs }

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
        [Tooltip("Model spawned on the slime's socket for this slot")]
        public GameObject VisualPrefab;

        [Header("Growth")]
        [Tooltip("Seconds to grow from bud to full organ (0 = instant)")]
        public float GrowthSeconds = 20f;
        [Tooltip("Total food (hunger) consumed while growing")]
        public float GrowthFoodCost = 10f;

        [Header("Effects (at full growth)")]
        [Tooltip("Gives colour vision and long view range")]
        public bool GrantsSight;
        [Tooltip("Multiplies movement speed")]
        public float SpeedMultiplier = 1f;
    }
}
