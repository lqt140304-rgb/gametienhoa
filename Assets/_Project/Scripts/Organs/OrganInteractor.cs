using System.Collections.Generic;
using Evolve.Survival;
using UnityEngine;

namespace Evolve.Organs
{
    /// <summary>
    /// The body menu (G, drawn by the HUD) and the few organ pickups still in the world (debug spawns, network
    /// leftovers): touching one asks E = swallow it as a sample for Giải Mã, X = leave it.
    /// Organs no longer fall off on death: they stay on the body. Only when the corpse is lost for good
    /// (out of lives) are they gone; the next reincarnation starts from nothing.
    /// The HUD reads Current, GetPrompt and BodyMenuOpen to draw the dialogs.
    /// </summary>
    [RequireComponent(typeof(OrganHost))]
    public class OrganInteractor : MonoBehaviour
    {
        OrganHost host;
        SurvivalStats stats;

        public OrganPickup Current { get; private set; }
        public bool BodyMenuOpen { get; private set; }
        /// <summary>Result of the last corpse loss: organs kept on the ground (always 0 now) / lost.</summary>
        public int LastDeathKept { get; private set; }
        public int LastDeathLost { get; private set; }

        /// <summary>Raised when the corpse was lost and its organs with it (kept, lost).</summary>
        public event System.Action<int, int> DroppedOnDeath;

        void Awake()
        {
            host = GetComponent<OrganHost>();
            stats = GetComponent<SurvivalStats>();
        }

        void OnEnable() { if (stats != null) stats.State.Died += OnDied; }
        void OnDisable() { if (stats != null) stats.State.Died -= OnDied; }

        void OnDied()
        {
            Current = null;
            BodyMenuOpen = false;
        }

        /// <summary>The corpse is lost for good (out of lives): every organ goes with it. They stay learned until the next reincarnation.</summary>
        public void DropAllOnDeath()
        {
            var list = host.OrganList;
            foreach (var o in list) host.Detach(o);
            LastDeathKept = 0;
            LastDeathLost = list.Count;
            DroppedOnDeath?.Invoke(0, list.Count);
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

        /// <summary>Dialog title, body and the label of the E action (null = E does nothing).</summary>
        public void GetPrompt(out string title, out string body, out string accept)
        {
            title = body = accept = null;
            if (Current == null) return;
            var o = Current.Organ;
            title = $"{OrganTiers.Tag(o.Tier)} {o.DisplayName}";
            body = $"{o.Description}\nNuốt vào Túi Không Đáy để Giải Mã học cấu trúc này?";
            accept = "Nuốt";
        }

        public static string SlotName(OrganSlot slot) => slot switch
        {
            OrganSlot.Eyes => "Mắt",
            OrganSlot.Legs => "Chân",
            OrganSlot.Skin => "Da",
            OrganSlot.Back => "Lưng",
            _ => slot.ToString()
        };

        void Update()
        {
            UpdatePrompt();
        }

        public void OpenBodyMenu() { if (stats == null || !stats.State.IsDead) BodyMenuOpen = true; }

        public void CloseBodyMenu() => BodyMenuOpen = false;

        void UpdatePrompt()
        {
            if (Current == null || SurvivalHUD.BlocksInput(gameObject)) return;
            if (KeyPressed.X()) { Current = null; return; } // leave it; touch again to be asked again
            if (!KeyPressed.E()) return;

            var pickup = Current;
            var swallower = GetComponent<Evolve.Swallow.Swallower>();
            if (swallower == null) return;
            var genes = new List<Evolve.Swallow.Gene> { new Evolve.Swallow.Gene(pickup.Organ, Evolve.Swallow.AnalysisRules.FromTier(pickup.Organ.Tier)) };
            if (!swallower.TrySwallowAlive(pickup.Organ.DisplayName, 0.5f, 2f, FoodKind.Generic, genes))
            {
                Evolve.Swallow.Echo.Say("«Túi Không Đáy đã đầy.»");
                return;
            }
            Current = null;
            pickup.MarkTaken();
            Destroy(pickup.gameObject);
        }
    }
}
