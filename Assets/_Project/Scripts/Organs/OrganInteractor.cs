using Evolve.Survival;
using UnityEngine;

namespace Evolve.Organs
{
    /// <summary>
    /// Lets the player decide what to do with organs:
    /// touching a dropped organ asks E = attach / replace, F = absorb it for evolution EXP, X = leave it;
    /// G opens the body menu (drawn by the HUD) to spend EXP, pick a priority organ or detach organs.
    /// Replaced organs are dropped too, so the player can change their mind.
    /// The HUD reads Current, GetPrompt and BodyMenuOpen to draw the dialogs.
    /// </summary>
    [RequireComponent(typeof(OrganHost))]
    public class OrganInteractor : MonoBehaviour
    {
        [Tooltip("How far from the body detached organs are dropped")]
        [SerializeField] float dropDistance = 1.5f;
        [Tooltip("On death every organ falls off where you died; each one is lost with this chance")]
        [Range(0, 1)] [SerializeField] float deathLossChance = 0.5f;

        OrganHost host;
        SurvivalStats stats;

        Evolve.World.ShelterSensor shelter;

        /// <summary>Changing the body is only allowed in a shelter when the world has any.</summary>
        public bool CanReshapeNow => OrganRules.CanReshape(Evolve.World.ShelterZone.AnyActive,
            shelter != null && shelter.InShelter, stats != null && stats.State.IsDead);
        /// <summary>Short message for the HUD (empty = none); cleared by the HUD after showing.</summary>
        public string Notice { get; set; } = "";

        public OrganPickup Current { get; private set; }
        public bool BodyMenuOpen { get; private set; }
        /// <summary>Result of the last death drop: organs left on the ground / destroyed.</summary>
        public int LastDeathKept { get; private set; }
        public int LastDeathLost { get; private set; }

        /// <summary>Raised after organs fell off on death (kept, lost).</summary>
        public event System.Action<int, int> DroppedOnDeath;

        void Awake()
        {
            host = GetComponent<OrganHost>();
            stats = GetComponent<SurvivalStats>();
            shelter = GetComponent<Evolve.World.ShelterSensor>();
        }

        void OnEnable() { if (stats != null) stats.State.Died += OnDied; }
        void OnDisable() { if (stats != null) stats.State.Died -= OnDied; }

        // Death with lives left: organs fall off right away.
        // Out of lives (corpse): organs stay on the body awaiting rescue; DropAllOnDeath is
        // called later when the corpse is finally lost (prototype: when starting a new run).
        void OnDied()
        {
            Current = null;
            BodyMenuOpen = false;
            if (!stats.State.IsCorpse) DropAllOnDeath();
        }

        /// <summary>Every organ falls off where the body is; each is lost with deathLossChance.</summary>
        public void DropAllOnDeath()
        {
            int kept = 0, lost = 0;
            var list = host.OrganList;
            for (int i = 0; i < list.Count; i++)
            {
                host.Detach(list[i].Slot);
                if (Random.value < deathLossChance) { lost++; continue; }
                kept++;
                // Spread them in a small ring so pickups don't overlap.
                float a = (i + Random.value * 0.5f) / Mathf.Max(1, list.Count) * Mathf.PI * 2f;
                Vector3 p = transform.position + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * dropDistance;
                p.y = 0.25f;
                OrganPickup.Spawn(list[i], p);
            }
            LastDeathKept = kept;
            LastDeathLost = lost;
            DroppedOnDeath?.Invoke(kept, lost);
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

        /// <summary>EXP gained by absorbing the organ currently offered.</summary>
        public float CurrentAbsorbExp => Current != null ? OrganTiers.AbsorbExp(Current.Organ.Tier) : 0f;

        /// <summary>Dialog title, body and the label of the E action (null = E does nothing; F always absorbs).</summary>
        public void GetPrompt(out string title, out string body, out string accept)
        {
            title = body = accept = null;
            if (Current == null) return;
            var o = Current.Organ;
            title = $"Bản sao bản mẫu: {OrganTiers.Tag(o.Tier)} {o.DisplayName}";
            string effect = $"{o.Description}\nCần {o.GrowthExpRequired:0} EXP tiến hóa để phát triển (đang có {host.Exp:0}).";
            if (o.GrantedAbility != null)
                effect += $"\nKỹ năng: {o.GrantedAbility.DisplayName} (mở ở Lv{o.AbilityUnlockLevel}).";
            if (!CanReshapeNow) effect += "\nChỉ lắp được ở nơi trú (F hấp thụ thì làm ở đâu cũng được).";
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
                    body = $"Bạn đã có {o.DisplayName}.\nHấp thụ để nhận {CurrentAbsorbExp:0} EXP tiến hóa?";
                    break;
                case AttachCheck.NoRoom:
                    body = $"Cơ thể không đủ chỗ ({host.Count}/{host.MaxOrgans} cơ quan).\nHấp thụ để nhận {CurrentAbsorbExp:0} EXP tiến hóa?";
                    break;
                case AttachCheck.LackNutrient:
                    var lack = o.EffectiveNutrient;
                    body = $"Cơ thể thiếu {Evolve.Survival.NutrientKinds.SlimeName(lack)} ({Evolve.Survival.NutrientKinds.RealName(lack)}) để mô phỏng {o.DisplayName}.\nĂn thêm thức ăn giàu {Evolve.Survival.NutrientKinds.RealName(lack)}.\nHoặc hấp thụ để nhận {CurrentAbsorbExp:0} EXP tiến hóa.";
                    break;
            }
        }

        public static string SlotName(OrganSlot slot) => slot switch
        {
            OrganSlot.Eyes => "Mắt",
            OrganSlot.Legs => "Chân",
            OrganSlot.Skin => "Da",
            OrganSlot.Back => "Lưng",
            _ => slot.ToString()
        };

        /// <summary>Detach the organ and drop it on the ground next to the body.</summary>
        public void Drop(OrganSO organ)
        {
            if (organ == null || !host.Has(organ)) return;
            if (!CanReshapeNow) { Notice = OrganRules.ReshapeBlockedMessage; return; }
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
            if (!SurvivalHUD.HasFor(gameObject) && KeyPressed.G()) BodyMenuOpen = !BodyMenuOpen;
        }

        public void OpenBodyMenu() { if (stats == null || !stats.State.IsDead) BodyMenuOpen = true; }

        public void CloseBodyMenu() => BodyMenuOpen = false;

        void UpdatePrompt()
        {
            if (Current == null || SurvivalHUD.BlocksInput(gameObject)) return;
            if (KeyPressed.X()) { Current = null; return; } // leave it; touch again to be asked again

            var pickup = Current;
            if (KeyPressed.F())
            {
                // Absorb: always possible, turns the organ into evolution EXP.
                host.Learn(pickup.Organ);
                if (stats != null) stats.State.AddExp(OrganTiers.AbsorbExp(pickup.Organ.Tier));
                Evolve.Visuals.SlimeGelAnimator.Play(gameObject, Evolve.Visuals.GelMotion.Absorb);
                Current = null;
                pickup.MarkTaken();
                Destroy(pickup.gameObject);
                return;
            }

            if (!KeyPressed.E()) return;
            if (!CanReshapeNow) { Notice = OrganRules.ReshapeBlockedMessage; return; }
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
                    return; // nothing to attach; use F to absorb
            }
            Current = null;
            pickup.MarkTaken();
            Destroy(pickup.gameObject);
        }
    }
}
