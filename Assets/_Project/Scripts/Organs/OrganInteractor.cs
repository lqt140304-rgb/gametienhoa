using Evolve.Survival;
using UnityEngine;

namespace Evolve.Organs
{
    /// <summary>
    /// Lets the player decide what to do with an organ they touch:
    /// E = attach / replace / absorb as food, X = leave it.
    /// The HUD reads Current/Prompt to draw the dialog.
    /// </summary>
    [RequireComponent(typeof(OrganHost))]
    public class OrganInteractor : MonoBehaviour
    {
        [Tooltip("Food gained when absorbing an organ you already have")]
        [SerializeField] float absorbNutrition = 10f;

        OrganHost host;
        SurvivalStats stats;

        public OrganPickup Current { get; private set; }

        void Awake()
        {
            host = GetComponent<OrganHost>();
            stats = GetComponent<SurvivalStats>();
        }

        public void Offer(OrganPickup pickup)
        {
            if (stats != null && stats.State.IsDead) return;
            Current = pickup;
        }

        public void Withdraw(OrganPickup pickup)
        {
            if (Current == pickup) Current = null;
        }

        public AttachCheck CurrentCheck => Current != null ? host.Check(Current.Organ) : AttachCheck.Ok;

        /// <summary>Dialog title, body and the label of the E action (null = no action).</summary>
        public void GetPrompt(out string title, out string body, out string accept)
        {
            title = body = accept = null;
            if (Current == null) return;
            var o = Current.Organ;
            title = $"Tìm thấy: {o.DisplayName}";
            string effect = $"{o.Description}\nCần {o.GrowthFoodRequired:0} thức ăn để phát triển.";
            switch (CurrentCheck)
            {
                case AttachCheck.Ok:
                    body = $"Lắp vào vị trí {SlotName(o.Slot)}?\n{effect}";
                    accept = "Lắp";
                    break;
                case AttachCheck.SlotOccupied:
                    var cur = host.GetInSlot(o.Slot);
                    body = $"Vị trí {SlotName(o.Slot)} đã có {cur.DisplayName}.\nThay bằng {o.DisplayName}? {cur.DisplayName} sẽ mất.\n{effect}";
                    accept = "Thay thế";
                    break;
                case AttachCheck.AlreadyOwned:
                    body = $"Bạn đã có {o.DisplayName}.\nHấp thụ để nhận {absorbNutrition:0} thức ăn?";
                    accept = "Hấp thụ";
                    break;
                case AttachCheck.NoRoom:
                    body = $"Cơ thể không đủ chỗ ({host.Count}/{host.MaxOrgans} cơ quan).\nHấp thụ để nhận {absorbNutrition:0} thức ăn?";
                    accept = "Hấp thụ";
                    break;
            }
        }

        public static string SlotName(OrganSlot slot) => slot switch
        {
            OrganSlot.Eyes => "Mắt",
            OrganSlot.Legs => "Chân",
            _ => slot.ToString()
        };

        void Update()
        {
            if (Current == null) return;
            if (KeyPressed.X()) { Current = null; return; } // leave it; touch again to be asked again

            if (!KeyPressed.E()) return;
            var pickup = Current;
            switch (CurrentCheck)
            {
                case AttachCheck.Ok:
                case AttachCheck.SlotOccupied:
                    if (!host.TryAttach(pickup.Organ)) return;
                    break;
                default:
                    if (stats != null) stats.State.Feed(absorbNutrition);
                    break;
            }
            Current = null;
            Destroy(pickup.gameObject);
        }
    }
}
