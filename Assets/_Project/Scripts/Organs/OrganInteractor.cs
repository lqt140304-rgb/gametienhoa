using Evolve.Survival;
using UnityEngine;

namespace Evolve.Organs
{
    /// <summary>
    /// Lets the player decide what to do with organs:
    /// touching a dropped organ asks E = attach / replace / absorb, X = leave it;
    /// G opens the body menu where 1..9 detaches an organ and drops it on the ground.
    /// Replaced organs are dropped too, so the player can change their mind.
    /// The HUD reads Current, GetPrompt and BodyMenuOpen to draw the dialogs.
    /// </summary>
    [RequireComponent(typeof(OrganHost))]
    public class OrganInteractor : MonoBehaviour
    {
        [Tooltip("Food gained when absorbing an organ you already have")]
        [SerializeField] float absorbNutrition = 10f;
        [Tooltip("How far from the body detached organs are dropped")]
        [SerializeField] float dropDistance = 1.5f;

        OrganHost host;
        SurvivalStats stats;

        public OrganPickup Current { get; private set; }
        public bool BodyMenuOpen { get; private set; }

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
            title = $"Tìm thấy: {OrganTiers.Tag(o.Tier)} {o.DisplayName}";
            string effect = $"{o.Description}\nCần {o.GrowthFoodRequired:0} thức ăn để phát triển.";
            switch (CurrentCheck)
            {
                case AttachCheck.Ok:
                    body = $"Lắp vào vị trí {SlotName(o.Slot)}?\n{effect}";
                    accept = "Lắp";
                    break;
                case AttachCheck.SlotOccupied:
                    var cur = host.GetInSlot(o.Slot);
                    body = $"Vị trí {SlotName(o.Slot)} đã có {OrganTiers.Tag(cur.Tier)} {cur.DisplayName}.\nThay bằng {o.DisplayName}? {cur.DisplayName} sẽ rơi ra đất\n(nhặt lại phải nuôi từ đầu).\n{effect}";
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

        /// <summary>Detach the organ and drop it on the ground next to the body.</summary>
        public void Drop(OrganSO organ)
        {
            if (organ == null || !host.Has(organ)) return;
            host.Detach(organ.Slot);
            SpawnDropped(organ);
        }

        void SpawnDropped(OrganSO organ)
        {
            float a = Random.Range(0f, Mathf.PI * 2f);
            Vector3 p = transform.position + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * dropDistance;
            p.y = 0.25f;
            OrganPickup.Spawn(organ, p);
        }

        void Update()
        {
            UpdateBodyMenu();
            UpdatePrompt();
        }

        void UpdateBodyMenu()
        {
            if (KeyPressed.G()) BodyMenuOpen = !BodyMenuOpen;
            if (!BodyMenuOpen) return;
            var list = host.OrganList;
            for (int i = 0; i < list.Count && i < 9; i++)
                if (KeyPressed.Digit(i + 1)) { Drop(list[i]); break; }
        }

        void UpdatePrompt()
        {
            if (Current == null) return;
            if (KeyPressed.X()) { Current = null; return; } // leave it; touch again to be asked again

            if (!KeyPressed.E()) return;
            var pickup = Current;
            switch (CurrentCheck)
            {
                case AttachCheck.Ok:
                    if (!host.TryAttach(pickup.Organ)) return;
                    break;
                case AttachCheck.SlotOccupied:
                    var old = host.Detach(pickup.Organ.Slot);
                    if (!host.TryAttach(pickup.Organ)) return;
                    if (old != null) SpawnDropped(old);
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
