using Evolve.Organs;
using UnityEngine;

namespace Evolve.Ecology
{
    [CreateAssetMenu(menuName = "Evolve/Species", fileName = "SpeciesSO")]
    public class SpeciesSO : ScriptableObject
    {
        public string Id;
        public GameObject Prefab;
        public OrganTier Tier;
        public int StartCount = 10;
        public int Capacity = 30;
        public float BirthsPerMinute = 0.5f;
        public float FoodPerHeadPerMinute = 0.5f;
        [Tooltip("null = eats region food")]
        public SpeciesSO Prey;
        public float KillsPerHeadPerMinute;
        [Tooltip("Share of hunts on it that fail (its defence, see PreyDefenseSeed)")]
        [Range(0, 0.95f)] public float EscapeChance;
    }
}
