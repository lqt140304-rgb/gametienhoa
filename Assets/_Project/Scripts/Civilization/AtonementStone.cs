using Evolve.Survival;
using UnityEngine;

namespace Evolve.Civilization
{
    /// <summary>
    /// The carved stone outside the treefolk's cave. Standing next to it and pressing E offers food (25 hunger) to the tribe:
    /// a grudge softens by 12, never past "Lạnh nhạt" (spec 2026-11-17-treefolk-grudge-design). Placed outside the guards'
    /// reach so a hated player can still come and atone.
    /// </summary>
    public class AtonementStone : MonoBehaviour
    {
        public TreefolkTribe Tribe;
        public float Reach = 3f;
        const float Cooldown = 3f;
        float nextOffer;
        bool near;
        static GUIStyle hintStyle;

        void Update()
        {
            var me = Tribe != null ? Tribe.LocalPlayer : null;
            near = me != null && !me.State.IsDead && (me.transform.position - transform.position).sqrMagnitude <= Reach * Reach;
            if (!near || !KeyPressed.E() || Time.time < nextOffer) return;
            nextOffer = Time.time + Cooldown;

            float value = Tribe.IsReplica ? Tribe.ClientValue : Tribe.Ledger.Get(me);
            if (value >= ReputationRules.AtonementCap) { Tribe.Say("Họ không cần lễ vật của bạn."); return; }
            if (me.State.Hunger < ReputationRules.OfferingCost) { Tribe.Say("Bạn không còn đủ thức ăn để dâng."); return; }
            me.State.Hunger -= ReputationRules.OfferingCost;
            if (Tribe.IsReplica) { NetWorld.ReportOffering?.Invoke(Tribe.TribeId); Tribe.Say("Bạn đặt lễ vật lên đá…"); return; }
            Tribe.Offer(me);
            Tribe.Say($"Lễ vật được nhận. {Tribe.DisplayName}: {ReputationRules.Name(Tribe.Ledger.StandingOf(me))}");
        }

        void OnGUI()
        {
            if (!near || Event.current.type != EventType.Repaint) return;
            if (hintStyle == null) hintStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(1f, 0.92f, 0.7f) } };
            GUI.Label(new Rect(Screen.width * 0.5f - 220f, Screen.height * 0.62f, 440f, 26f), $"E: dâng lễ vật cho {Tribe.DisplayName} (−{ReputationRules.OfferingCost:0} no)", hintStyle);
        }
    }
}
