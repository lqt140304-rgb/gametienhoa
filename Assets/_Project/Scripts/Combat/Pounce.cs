using System.Collections.Generic;
using Evolve.Abilities;
using Evolve.Creatures;
using Evolve.Player;
using Evolve.Survival;
using Evolve.Swallow;
using Evolve.Visuals;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Evolve.Combat
{
    /// <summary>
    /// Vồ (pounce), the basic slime action from the start (spec 2026-11-20): left mouse button lunges at the nearest creature in front
    /// (about 4 m). A creature the slime can swallow whole is caught at once; a bigger one is latched on to (grip mini game); one over
    /// four times the slime's mass bounces it off. Nothing in reach: nothing happens and no energy is spent.
    /// A hint on screen says what a click would do, and the Console says why a click did nothing.
    /// </summary>
    public class Pounce : MonoBehaviour
    {
        const float Reach = 4f, Lunge = 8f;

        SurvivalStats stats;
        Rigidbody rb;
        SlimeController controller;
        SlimeBody body;
        readonly List<Candidate> candidates = new List<Candidate>();
        readonly List<Component> creatures = new List<Component>();
        int picked = -1;
        string hint = "";
        GUIStyle hintStyle;

        void Awake()
        {
            stats = GetComponent<SurvivalStats>();
            rb = GetComponent<Rigidbody>();
            controller = GetComponent<SlimeController>();
            body = GetComponent<SlimeBody>();
            Debug.Log("[Vồ] sẵn sàng: chuột trái để vồ quái trong 4 m phía trước camera (đóng menu thần F1 trước).");
        }

        string BlockedBy()
        {
            if (SwallowCinematic.IsPlaying) return "đang nuốt chửng";
            if (stats.Frozen) return "slime đang bị đóng băng hoặc chưa bắt đầu";
            if (LatchSession.Active != null) return "đang ghì chặt một quái";
            if (AimController.MouseUiOpen) return "menu thần (F1) đang mở: đóng nó rồi bấm lại";
            if (SurvivalHUD.BlocksInput(gameObject)) return "một bảng HUD đang mở (Túi, màu, bảng cơ thể)";
            return null;
        }

        void Update()
        {
            hint = "";
            if (stats == null || stats.IsRemote || stats.State.IsDead) return;
            string blocked = BlockedBy();
            if (blocked == null) Scan();
            bool click = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            if (!click) return;
            if (blocked != null) { Debug.Log("[Vồ] bị chặn: " + blocked); return; }
            if (picked < 0) { Debug.Log("[Vồ] không có quái nào trong 4 m phía trước camera (hoặc quái đang ẩn)."); return; }
            DoPounce();
        }

        void Scan()
        {
            candidates.Clear(); creatures.Clear();
            foreach (var p in Prey.Active) Add(p, p.IsReplica, p.Latched);
            foreach (var h in Predator.Active) Add(h, h.IsReplica, h.Latched);
            Vector3 face = Camera.main != null ? Camera.main.transform.forward : transform.forward;
            Vector3 pos = transform.position;
            picked = PounceRules.PickTarget(candidates, pos.x, pos.z, face.x, face.z, Reach);
            if (picked < 0) return;
            var t = creatures[picked];
            string who = t.name.Replace("(Clone)", "");
            switch (Outcome(t))
            {
                case PounceOutcome.SwallowWhole: hint = $"Chuột trái: Vồ {who} (nuốt sống)"; break;
                case PounceOutcome.Latch: hint = $"Chuột trái: Vồ {who} (Ghì Chặt)"; break;
                default: hint = $"{who} quá to hoặc đang có giáp: vồ sẽ bật ra"; break;
            }
        }

        PounceOutcome Outcome(Component target)
        {
            float slimeMass = body != null ? body.Mass : 10f;
            float monsterMass = target is Prey pr ? pr.BodyMass : ((Predator)target).BodyMass;
            var armor = target.GetComponent<CreatureCombat>();
            return armor != null && armor.IsArmored ? PounceOutcome.Bounce : PounceRules.Decide(slimeMass, monsterMass);   // an armoured monster cannot be pounced
        }

        void DoPounce()
        {
            if (!stats.State.TrySpendEnergy(stats.Config.HopEnergyCost)) { Debug.Log("[Vồ] không đủ năng lượng."); return; }
            var target = creatures[picked];
            Vector3 to = target.transform.position - transform.position; to.y = 0f;
            var outcome = Outcome(target);
            Debug.Log($"[Vồ] {target.name.Replace("(Clone)", "")}: {outcome}");

            if (outcome == PounceOutcome.Bounce)
            {
                Impulse.Kick(rb, -to.normalized * 5f + Vector3.up * 3f);
                if (controller != null) controller.FreezeFor(0.4f);
                SlimeGelAnimator.Play(gameObject, GelMotion.Attack);
                var tint = GetComponentInChildren<SlimeGelTint>();
                Color gelColor = tint != null ? tint.CoreColor : new Color(0.35f, 0.85f, 0.65f);
                SkillFx.Shed(transform, gelColor, 8, 2.5f, 0.08f, 0.3f);
                Echo.Say("«Quá to để vồ.»");
                return;
            }
            Impulse.Kick(rb, to.normalized * Lunge + Vector3.up * 2f);
            if (outcome == PounceOutcome.SwallowWhole && target is Prey prey)
            {
                if (prey.PounceCatch(stats))
                    LatchGelFx.Begin(gameObject, target.transform, false)?.Absorb();
                return;
            }
            // a hunter small enough to swallow is still held and digested (hunters leave a carcass to learn from)
            LatchSession.Begin(stats, target, StruggleSeed.For(target.name));
        }

        void Add(Component c, bool replica, bool latched)
        {
            if (replica || (c.TryGetComponent(out CreatureCombat cc) && cc.IsHidden)) return;   // a lurking creature cannot be seen, so it cannot be pounced
            candidates.Add(new Candidate(c.transform.position.x, c.transform.position.z, latched));
            creatures.Add(c);
        }

        void OnGUI()
        {
            if (hint.Length == 0) return;
            if (hintStyle == null) hintStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 18, fontStyle = FontStyle.Bold };
            var r = new Rect(0f, Screen.height * 0.64f, Screen.width, 32f);
            var old = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.6f); GUI.Label(new Rect(r.x + 1.5f, r.y + 1.5f, r.width, r.height), hint, hintStyle);   // a soft shadow
            GUI.color = new Color(1f, 0.95f, 0.6f); GUI.Label(r, hint, hintStyle);
            GUI.color = old;
        }
    }
}
