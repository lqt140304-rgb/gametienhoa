using Evolve.Organs;
using UnityEngine;

namespace Evolve.Traits
{
    /// <summary>A flaw (Khuyết Điểm): a permanent bad rule that only annoys. Organs in <see cref="CompensatedBy"/> slots can offset it.</summary>
    [CreateAssetMenu(menuName = "Evolve/Flaw", fileName = "Flaw")]
    public class FlawSO : ScriptableObject
    {
        public string Id;
        public string DisplayName;
        [TextArea] public string Description;
        [Tooltip("Organ slots whose organs compensate this flaw (empty = not compensable yet).")]
        public OrganSlot[] CompensatedBy = new OrganSlot[0];
        [Tooltip("False while the effect is not coded yet: the card is shown but nothing happens.")]
        public bool Implemented;
    }
}
