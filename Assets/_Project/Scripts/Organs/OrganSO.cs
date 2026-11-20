using Evolve.Abilities;
using UnityEngine;
using UnityEngine.Serialization;

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

        [Header("Nutrition")]
        [Tooltip("Nutrient the body needs to equip this organ and keep it healthy. Auto = by slot: Eyes→Tinh Chất Sống, Legs→Sợi Nhầy, Skin→Mỡ Nhờn, Back→Tinh Vôi")]
        public OrganNutrient NutrientNeed = OrganNutrient.Auto;
        public Evolve.Survival.NutrientKind EffectiveNutrient => OrganNutrients.Resolve(NutrientNeed, Slot);

        [Header("Growth")]
        [Tooltip("Evolution EXP needed to grow from bud to full organ (Lv1). Lv2 costs the same again, Lv3 twice as much. 0 = instant.")]
        [FormerlySerializedAs("GrowthFoodRequired")]
        public float GrowthExpRequired = 30f;

        [Header("Active skill")]
        [Tooltip("Active skill this organ grants (empty = passive only). Tier C organs have none; skills start from tier B.")]
        public AbilitySO GrantedAbility;
        [Tooltip("Unused since skills replaced worn organs (a learned move is usable from ★1)")]
        [Min(1)] public int AbilityUnlockLevel = 2;

        /// <summary>What this gene is as a skill: a passive that is always on (eyes, heat membrane) or an active move on keys 1-5.</summary>
        public Evolve.Swallow.GeneUse Use =>
            Evolve.Swallow.SkillRules.UseOf(GrantedAbility != null, GrantsSight, HeatResistance, SpeedMultiplier, ContactDamage,
                MaxHealthBonus > 0f || DamageReduction > 0f || SonarRangeMultiplier > 1.01f);

        [Tooltip("Skill family, for fusing two skills after chapter 1")]
        public Evolve.Swallow.SkillFamily Family;

        [Header("Effects (passives only; an active move's body part appears only while it is used)")]
        [Tooltip("Gives colour vision and long view range")]
        public bool GrantsSight;
        [Tooltip("Multiplies movement speed")]
        public float SpeedMultiplier = 1f;
        [Tooltip("Extra °C of heat the body tolerates")]
        public float HeatResistance;
        [Tooltip("Damage dealt back to a creature that bites this body")]
        public float ContactDamage;
        [Tooltip("Extra maximum health (Da Dày)")]
        public float MaxHealthBonus;
        [Tooltip("Share of incoming damage blocked, 0..1 (Vỏ Cứng)")]
        [Range(0, 0.8f)] public float DamageReduction;
        [Tooltip("Multiplies how far Sóng Dò reaches (Sóng Dò Mạnh)")]
        public float SonarRangeMultiplier = 1f;
    }
}
