using System.Collections.Generic;
using UnityEngine;

namespace Evolve.Traits
{
    /// <summary>
    /// The picking screen at the start (spec 2026-11-18-starting-choices-design, section 6): three small gel cards of cores (name on top, a short
    /// line below, Tab shows the detail), then the flaw options that go with the chosen core, then Confirm.
    /// Keyboard: 1-3 select, Tab detail, Enter next/confirm, Backspace back, Esc skip (play without a core).
    /// Mouse works while the cursor is free. Solo uses a local TraitRoom; as host it uses the host's room; as client it waits for the host's hand (TraitNet).
    /// </summary>
    public class TraitPicker : MonoBehaviour
    {
        enum Phase { Idle, Waiting, Core, Flaw, Done }
        enum Mode { Solo, Host, Client }

        Phase phase = Phase.Idle;
        Mode mode;
        PlayerTraits player;
        TraitRoom soloRoom;
        string[] hand = new string[0];
        int selected;
        bool detail;
        string notice = "";
        string chosenCore;
        bool skipped;
        float nextFind;
        Texture2D disc, pixel;
        GUIStyle nameStyle, lineStyle, smallStyle, titleStyle;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (FindAnyObjectByType<TraitPicker>() != null) return;
            var go = new GameObject("Trait Picker");
            DontDestroyOnLoad(go);
            go.AddComponent<TraitPicker>();
        }

        void OnEnable() { TraitNet.HandReceived += OnHandReceived; TraitNet.ResultReceived += OnResultReceived; }
        void OnDisable() { TraitNet.HandReceived -= OnHandReceived; TraitNet.ResultReceived -= OnResultReceived; }

        static Mode CurrentMode() => TraitNet.IsClient ? Mode.Client : TraitNet.HostHand != null ? Mode.Host : Mode.Solo;

        void Update()
        {
            if (phase == Phase.Done) return;
            if (player == null)
            {
                if (Time.unscaledTime < nextFind) return;
                nextFind = Time.unscaledTime + 0.5f;
                player = FindAnyObjectByType<PlayerTraits>();
                if (player == null) return;
            }
            if (player.CoreId != null || skipped) { phase = Phase.Done; return; }
            if (TraitCatalog.Cores.Count == 0)
            {
                phase = Phase.Idle;
                if (Time.unscaledTime >= nextFind) { nextFind = Time.unscaledTime + 30f; Debug.Log("Chưa có dữ liệu Lõi: chạy menu Evolve → Create Traits."); }
                return;
            }
            // (Re)start whenever idle, or when the room type changed before anything was confirmed (solo -> host/client).
            var now = CurrentMode();
            if (phase == Phase.Idle || (now != mode && chosenCore == null)) Begin(now);
        }

        void Begin(Mode m)
        {
            mode = m;
            selected = 0; detail = false; chosenCore = null; notice = "";
            hand = new string[0];
            if (m == Mode.Solo)
            {
                var ids = new List<string>();
                foreach (var c in TraitCatalog.Cores) ids.Add(c.Id);
                soloRoom = new TraitRoom(System.Environment.TickCount, ids);
                hand = soloRoom.Join(0);
            }
            else if (m == Mode.Host) hand = TraitNet.HostHand != null ? TraitNet.HostHand() : new string[0];
            else hand = TraitNet.LastHand ?? new string[0];
            phase = hand.Length > 0 ? Phase.Core : Phase.Waiting;
        }

        void OnHandReceived(string[] h)
        {
            if (mode != Mode.Client || phase == Phase.Done) return;
            hand = h ?? new string[0];
            if (phase == Phase.Waiting && hand.Length > 0) phase = Phase.Core;
        }

        void OnResultReceived(ConfirmResult result)
        {
            if (mode != Mode.Client || phase == Phase.Done) return;
            HandleResult(result);
        }

        void HandleResult(ConfirmResult result)
        {
            if (result.Outcome == ConfirmOutcome.Accepted) { Finish(chosenCore, pendingFlaws); return; }
            hand = result.NewHand ?? hand;
            chosenCore = null; selected = 0; phase = hand.Length > 0 ? Phase.Core : Phase.Waiting;
            notice = result.Outcome == ConfirmOutcome.TakenReplaced ? "Lõi đó vừa có người khác chọn. Hãy chọn lại." : "Lựa chọn không hợp lệ. Hãy chọn lại.";
        }

        string[] pendingFlaws = new string[0];

        void Finish(string coreId, string[] flaws)
        {
            player.Set(coreId, flaws);
            phase = Phase.Done;
        }

        CoreSO CardAt(int i) => i >= 0 && i < hand.Length ? TraitCatalog.Core(hand[i]) : null;

        void Confirm()
        {
            var core = TraitCatalog.Core(chosenCore);
            if (core == null || selected < 0 || selected >= core.Options.Length || core.Options[selected].FlawIds == null || core.Options[selected].FlawIds.Length == 0)
            {
                notice = "Dữ liệu Khuyết Điểm của Lõi này bị lỗi. Bấm Esc để chơi không Lõi.";
                return;
            }
            pendingFlaws = core.Options[selected].FlawIds;
            if (mode == Mode.Solo) HandleResult(soloRoom.Confirm(0, chosenCore));
            else if (mode == Mode.Host) HandleResult(TraitNet.HostConfirm(chosenCore));
            else TraitNet.ClientPick?.Invoke(chosenCore, pendingFlaws);   // the answer comes back through ResultReceived
        }

        // ---------------------------------------------------------------- drawing

        void OnGUI()
        {
            if (phase == Phase.Idle || phase == Phase.Done) return;
            Setup();
            var e = Event.current;
            if (e.type == EventType.KeyDown) HandleKey(e);

            float w = Screen.width, h = Screen.height;
            GUI.color = new Color(0.02f, 0.05f, 0.1f, 0.78f);
            GUI.DrawTexture(new Rect(0, 0, w, h), pixel);
            GUI.color = Color.white;

            if (phase == Phase.Waiting)
            {
                GUI.Label(new Rect(0, h * 0.45f, w, 40), "Đang chờ chủ phòng chia thẻ…   (Esc: chơi không Lõi)", titleStyle);
                return;
            }

            GUI.Label(new Rect(0, h * 0.12f, w, 30), phase == Phase.Core ? "CHỌN LÕI CỦA BẠN" : "CHỌN KHUYẾT ĐIỂM", titleStyle);
            if (!string.IsNullOrEmpty(notice)) GUI.Label(new Rect(0, h * 0.12f + 28, w, 24), notice, smallStyle);

            if (phase == Phase.Core) DrawCards(w, h); else DrawFlaws(w, h);

            GUI.Label(new Rect(0, h - 44, w, 24), phase == Phase.Core
                ? "1–3 chọn   ·   Tab chi tiết   ·   Enter tiếp   ·   Esc chơi không Lõi"
                : "1–3 chọn   ·   Enter xác nhận   ·   Backspace quay lại", smallStyle);
        }

        void DrawCards(float w, float h)
        {
            const float cw = 190f, ch = 240f, gap = 24f;
            int n = hand.Length;
            float total = n * cw + (n - 1) * gap, x0 = (w - total) / 2f, y0 = h * 0.5f - ch / 2f;
            for (int i = 0; i < n; i++)
            {
                var core = CardAt(i);
                if (core == null) continue;
                var r = new Rect(x0 + i * (cw + gap), y0 - (i == selected ? 8f : 0f), cw, ch);
                bool on = i == selected;
                GUI.color = on ? new Color(0.3f, 0.7f, 1f, 0.95f) : new Color(0.25f, 0.45f, 0.75f, 0.55f);
                GUI.DrawTexture(r, pixel);
                GUI.color = new Color(0.04f, 0.1f, 0.22f, 0.95f);
                GUI.DrawTexture(new Rect(r.x + 2, r.y + 2, r.width - 4, r.height - 4), pixel);
                GUI.color = core.OrbColor;
                GUI.DrawTexture(new Rect(r.center.x - 28, r.y + 22, 56, 56), disc);   // placeholder orb
                GUI.color = Color.white;
                GUI.Label(new Rect(r.x + 8, r.y + 96, r.width - 16, 50), core.DisplayName, nameStyle);
                GUI.Label(new Rect(r.x + 10, r.y + 150, r.width - 20, 80), core.Summary + (core.Implemented ? "" : "\n(chưa có hiệu ứng)"), lineStyle);
                if (GUI.Button(r, GUIContent.none, GUIStyle.none)) { selected = i; }
            }
            if (detail && CardAt(selected) != null)
            {
                var core = CardAt(selected);
                var box = new Rect(w / 2f - 320, y0 + ch + 24, 640, 130);
                GUI.color = new Color(0.04f, 0.1f, 0.22f, 0.95f);
                GUI.DrawTexture(box, pixel);
                GUI.color = Color.white;
                var text = core.Detail + "\n\nKhuyết Điểm đi kèm: " + FlawNames(core);
                GUI.Label(new Rect(box.x + 12, box.y + 8, box.width - 24, box.height - 16), text, lineStyle);
            }
        }

        string FlawNames(CoreSO core)
        {
            var parts = new List<string>();
            foreach (var o in core.Options)
            {
                var names = new List<string>();
                if (o.FlawIds != null) foreach (var id in o.FlawIds) { var f = TraitCatalog.Flaw(id); names.Add(f != null ? f.DisplayName : id); }
                parts.Add(string.Join(" + ", names));
            }
            return string.Join("  ·  ", parts);
        }

        void DrawFlaws(float w, float h)
        {
            var core = TraitCatalog.Core(chosenCore);
            if (core == null) return;
            GUI.Label(new Rect(0, h * 0.12f + 52, w, 24), "Lõi: " + core.DisplayName, smallStyle);
            const float pw = 260f, ph = 120f, gap = 20f;
            int n = core.Options.Length;
            float x0 = (w - (n * pw + (n - 1) * gap)) / 2f, y0 = h * 0.5f - ph / 2f;
            for (int i = 0; i < n; i++)
            {
                var r = new Rect(x0 + i * (pw + gap), y0, pw, ph);
                bool on = i == selected;
                GUI.color = on ? new Color(0.3f, 0.7f, 1f, 0.95f) : new Color(0.25f, 0.45f, 0.75f, 0.55f);
                GUI.DrawTexture(r, pixel);
                GUI.color = new Color(0.04f, 0.1f, 0.22f, 0.95f);
                GUI.DrawTexture(new Rect(r.x + 2, r.y + 2, r.width - 4, r.height - 4), pixel);
                GUI.color = Color.white;
                var text = new System.Text.StringBuilder();
                var ids = core.Options[i].FlawIds;
                if (ids != null)
                    foreach (var id in ids)
                    {
                        var f = TraitCatalog.Flaw(id);
                        text.AppendLine(f != null ? "• " + f.DisplayName : "• " + id);
                        if (f != null) text.AppendLine(f.Description + (f.Implemented ? "" : " (chưa có hiệu ứng)"));
                    }
                GUI.Label(new Rect(r.x + 10, r.y + 8, r.width - 20, r.height - 16), text.ToString(), lineStyle);
                if (GUI.Button(r, GUIContent.none, GUIStyle.none)) selected = i;
            }
        }

        void HandleKey(Event e)
        {
            int count = phase == Phase.Core ? hand.Length : (TraitCatalog.Core(chosenCore) != null ? TraitCatalog.Core(chosenCore).Options.Length : 0);
            if (e.keyCode >= KeyCode.Alpha1 && e.keyCode <= KeyCode.Alpha3) { int i = e.keyCode - KeyCode.Alpha1; if (i < count) selected = i; e.Use(); return; }
            switch (e.keyCode)
            {
                case KeyCode.Tab: if (phase == Phase.Core) detail = !detail; e.Use(); break;
                case KeyCode.Escape: skipped = true; phase = Phase.Done; e.Use(); break;
                case KeyCode.Backspace: if (phase == Phase.Flaw) { phase = Phase.Core; chosenCore = null; selected = 0; } e.Use(); break;
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    if (phase == Phase.Core && selected < hand.Length) { chosenCore = hand[selected]; selected = 0; phase = Phase.Flaw; notice = ""; }
                    else if (phase == Phase.Flaw) Confirm();
                    e.Use();
                    break;
                case KeyCode.LeftArrow: if (selected > 0) selected--; e.Use(); break;
                case KeyCode.RightArrow: if (selected < count - 1) selected++; e.Use(); break;
            }
        }

        void Setup()
        {
            if (pixel != null) return;
            pixel = Texture2D.whiteTexture;
            disc = new Texture2D(64, 64, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            var px = new Color[64 * 64];
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(32f, 32f)) / 32f;
                    float shade = 0.65f + 0.35f * (1f - d);
                    px[y * 64 + x] = new Color(shade, shade, shade, Mathf.Clamp01((1f - d) * 14f));
                }
            disc.SetPixels(px); disc.Apply();
            titleStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 20, fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.85f, 0.95f, 1f) } };
            nameStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperCenter, fontSize = 17, fontStyle = FontStyle.Bold, wordWrap = true, normal = { textColor = Color.white } };
            lineStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperCenter, fontSize = 13, wordWrap = true, normal = { textColor = new Color(0.75f, 0.88f, 1f) } };
            smallStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 12, normal = { textColor = new Color(0.65f, 0.78f, 0.9f) } };
        }

        void OnDestroy() { if (disc != null) Destroy(disc); }
    }
}
