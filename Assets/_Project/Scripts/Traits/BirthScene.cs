using System;
using System.Collections.Generic;
using Evolve.Survival;
using UnityEngine;

namespace Evolve.Traits
{
    /// <summary>
    /// The birth scene (spec 2026-11-19-birth-scene-design, section 8). A player with no core is taken out of the world into a dark "system space":
    /// the slime, the cave and the HUD are not seen at all. Tiếng Vọng speaks and types its lines in system windows, announces the six gifts, then
    /// deals face-down cards. The player picks one blindly (mouse or arrow keys + Enter) for the core, then one for the flaw; what each card hides was
    /// decided by FateRoll before the scene began. Replaces the old choosing screen. Runs once per run; dying and respawning never plays it again.
    /// </summary>
    public class BirthScene : MonoBehaviour
    {
        enum State { Waiting, Running, Done }

        const int DeckSize = 8;   // face-down core cards

        State state;
        bool ranThisRun;
        PlayerTraits player;
        float nextFind;

        BirthFlow flow;
        int shownIndex = -1;
        int seed;
        FateCard[] deck = new FateCard[0];
        string[][] flawOrder = new string[0][];
        int pickedCore = -1, pickedFlaw = -1, focus;
        float clock;
        BirthVoice voice;

        // the frozen state while the scene plays (the same three flags the old picker used), and the cursor
        SurvivalStats frozenStats;
        bool freezing, prevInvulnerable, prevExternalMenu, prevCursorVisible;
        CursorLockMode prevCursorLock;

        GUIStyle titleStyle, bodyStyle, nameStyle, lineStyle, smallStyle;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (FindAnyObjectByType<BirthScene>() != null) return;
            var go = new GameObject("Birth Scene");
            DontDestroyOnLoad(go);
            go.AddComponent<BirthScene>();
        }

        void Awake() => voice = gameObject.AddComponent<BirthVoice>();

        void OnDisable() { Unfreeze(); }

        void OnDestroy() { BirthUi.Release(); }

        // ---------------------------------------------------------------- flow

        void Update()
        {
            if (state == State.Waiting)
            {
                if (Time.unscaledTime < nextFind) return;
                nextFind = Time.unscaledTime + 0.5f;
                player = TraitNet.Local;
                if (player != null && flow == null && !ranThisRun && player.CoreId == null) Begin();
                return;
            }
            if (state != State.Running) return;

            float dt = Time.unscaledDeltaTime;
            clock += dt;
            flow.Tick(dt, voice.Busy);
            if (flow.Finished) { Finish(); return; }
            if (flow.Index != shownIndex) EnterStep();
        }

        void Begin()
        {
            seed = FateRoll.PlayerSeed(Environment.TickCount, 0);
            deck = FateRoll.DealDeck(seed, TraitCatalog.FateCards(), DeckSize);
            if (deck.Length == 0)
            {
                Debug.LogWarning("Cảnh ra đời: kho Lõi rỗng, bỏ qua (chạy Evolve → Create Traits).");
                ranThisRun = true;
                return;
            }
            flow = new BirthFlow(BirthScript.BuildSteps());
            flawOrder = new string[0][];
            pickedCore = pickedFlaw = -1;
            shownIndex = -1;
            clock = 0f;
            state = State.Running;
            Freeze();
        }

        /// <summary>A new step began: say its line (the reveal steps read the name of what the card hid).</summary>
        void EnterStep()
        {
            shownIndex = flow.Index;
            focus = 0;
            var step = flow.Current;
            switch (step.Kind)
            {
                case BirthStepKind.RevealCore:
                    voice.Play(deck[pickedCore].Id);
                    break;
                case BirthStepKind.RevealFlaw:
                    voice.Stop();
                    foreach (var id in flawOrder[pickedFlaw]) voice.Enqueue(id);
                    break;
                default:
                    if (string.IsNullOrEmpty(step.VoiceId)) voice.Stop(); else voice.Play(step.VoiceId);
                    break;
            }
        }

        void PickCard(int i)
        {
            var step = flow.Current;
            if (step == null || !step.Interactive) return;
            if (step.Kind == BirthStepKind.PickCore)
            {
                if (i < 0 || i >= deck.Length) return;
                pickedCore = i;
                flawOrder = FateRoll.ShuffleOptions(seed + 17, deck[i].Options);
            }
            else
            {
                if (i < 0 || i >= flawOrder.Length) return;
                pickedFlaw = i;
            }
            flow.Pick();
        }

        void Finish()
        {
            if (player != null && pickedCore >= 0 && pickedFlaw >= 0)
            {
                string coreId = deck[pickedCore].Id;
                var flaws = flawOrder[pickedFlaw];
                player.Set(coreId, flaws);
                if (TraitNet.IsClient) TraitNet.ClientPick?.Invoke(coreId, flaws);   // co-op: tell the host what fate gave
            }
            voice.Stop();
            Unfreeze();
            ranThisRun = true;
            state = State.Done;
        }

        void Freeze()
        {
            if (freezing) return;
            var stats = player != null ? player.GetComponent<SurvivalStats>() : null;
            if (stats != null)
            {
                frozenStats = stats;
                prevInvulnerable = stats.State.Invulnerable;
                stats.State.Invulnerable = true;
                stats.Frozen = true;
            }
            prevExternalMenu = SurvivalHUD.ExternalMenuOpen;
            SurvivalHUD.ExternalMenuOpen = true;
            prevCursorVisible = Cursor.visible;
            prevCursorLock = Cursor.lockState;
            Cursor.visible = true;                     // the player picks cards with the mouse
            Cursor.lockState = CursorLockMode.None;
            freezing = true;
        }

        void Unfreeze()
        {
            if (!freezing) return;
            freezing = false;
            if (frozenStats != null)
            {
                frozenStats.Frozen = false;
                frozenStats.State.Invulnerable = prevInvulnerable;
            }
            SurvivalHUD.ExternalMenuOpen = prevExternalMenu;
            Cursor.visible = prevCursorVisible;
            Cursor.lockState = prevCursorLock;
            frozenStats = null;
        }

        // ---------------------------------------------------------------- names and colours

        static (string name, string summary, bool implemented, Color orb) CoreInfo(string id)
        {
            var so = TraitCatalog.Core(id);
            if (so != null) return (so.DisplayName, so.Summary, so.Implemented, so.OrbColor);
            foreach (var d in TraitSeed.Cores) if (d.Id == id) return (d.Name, d.Summary, d.Implemented, BirthUi.Cyan);
            return (id, "", false, BirthUi.Cyan);
        }

        static (string name, string description, bool implemented) FlawInfo(string id)
        {
            var so = TraitCatalog.Flaw(id);
            if (so != null) return (so.DisplayName, so.Description, so.Implemented);
            foreach (var d in TraitSeed.Flaws) if (d.Id == id) return (d.Name, d.Description, d.Implemented);
            return (id, "", false);
        }

        static Color RarityColor(TraitRarity r) => r switch
        {
            TraitRarity.Rare => new Color(0.3f, 0.62f, 1f),
            TraitRarity.Mythic => new Color(1f, 0.8f, 0.25f),
            _ => new Color(0.78f, 0.88f, 0.98f)
        };

        static string RarityName(TraitRarity r) => r switch
        {
            TraitRarity.Rare => "HIẾM",
            TraitRarity.Mythic => "CỰC HIẾM",
            _ => "THƯỜNG"
        };

        static readonly Color FlawColor = new Color(0.95f, 0.3f, 0.36f);

        // ---------------------------------------------------------------- layout

        static float Ui(float h) => Mathf.Clamp(h / 1080f, 0.7f, 1.6f);

        static int Cols(int n) => n <= 4 ? Mathf.Max(1, n) : (n + 1) / 2;

        static bool IsFlawStep(BirthStepKind k) => k == BirthStepKind.DealFlaw || k == BirthStepKind.PickFlaw || k == BirthStepKind.RevealFlaw;

        /// <summary>Where the face-down cards lie: evenly spread over the screen (two rows for the cores, one row for the flaws).</summary>
        static Rect[] Slots(int n, bool flaw, float w, float h)
        {
            var rects = new Rect[n];
            if (n == 0) return rects;
            int cols = Cols(n), rows = (n + cols - 1) / cols;
            float cw = flaw ? Mathf.Min(0.19f * w, 0.30f * h) : Mathf.Min(0.17f * w, 0.2f * h);
            float ch = cw * 1.42f, gap = cw * 0.16f;
            float totalW = cols * cw + (cols - 1) * gap, totalH = rows * ch + (rows - 1) * gap;
            float x0 = (w - totalW) / 2f, y0 = (flaw ? h * 0.54f : h * 0.58f) - totalH / 2f;
            for (int i = 0; i < n; i++)
                rects[i] = new Rect(x0 + (i % cols) * (cw + gap), y0 + (i / cols) * (ch + gap), cw, ch);
            return rects;
        }

        int HoverIndex(Vector2 mouse)
        {
            var step = flow != null ? flow.Current : null;
            if (step == null || !step.Interactive) return -1;
            bool flaw = IsFlawStep(step.Kind);
            var slots = Slots(flaw ? flawOrder.Length : deck.Length, flaw, Screen.width, Screen.height);
            for (int i = 0; i < slots.Length; i++) if (slots[i].Contains(mouse)) return i;
            return -1;
        }

        // ---------------------------------------------------------------- input and drawing

        void OnGUI()
        {
            if (state != State.Running || flow == null || flow.Finished) return;
            Setup();
            GUI.depth = -10000;   // on top of every other menu

            var e = Event.current;
            if (e.type == EventType.KeyDown) HandleKey(e);
            else if (e.type == EventType.MouseDown && e.button == 0)
            {
                int i = HoverIndex(e.mousePosition);
                if (i >= 0) { PickCard(i); e.Use(); }
            }
            if (e.type != EventType.Repaint) return;
            var step = flow.Current;
            if (step == null) return;

            float w = Screen.width, h = Screen.height, ui = Ui(h);
            BirthUi.EnsureTextures();
            UpdateStyles(ui);
            BirthUi.Background(w, h, clock, ui);

            switch (step.Kind)
            {
                case BirthStepKind.Boot: DrawBoot(w, h, ui); break;
                case BirthStepKind.Line: DrawLine(step, w, h, ui); break;
                case BirthStepKind.Gift: DrawGifts(step, w, h, ui); break;
                case BirthStepKind.Summary: DrawSummary(step, w, h, ui); break;
                default: DrawDealing(step, w, h, ui); break;
            }
            DrawHint(step, w, h);
        }

        void HandleKey(Event e)
        {
            var step = flow.Current;
            if (step == null) return;
            int n = IsFlawStep(step.Kind) ? flawOrder.Length : deck.Length;
            int cols = Cols(n);
            switch (e.keyCode)
            {
                case KeyCode.Space:
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    if (step.Interactive) PickCard(focus); else flow.Skip();
                    e.Use();
                    break;
                case KeyCode.Tab: flow.SkipIntro(); e.Use(); break;
                case KeyCode.LeftArrow: case KeyCode.A: if (focus > 0) focus--; e.Use(); break;
                case KeyCode.RightArrow: case KeyCode.D: if (focus < n - 1) focus++; e.Use(); break;
                case KeyCode.UpArrow: case KeyCode.W: if (focus - cols >= 0) focus -= cols; e.Use(); break;
                case KeyCode.DownArrow: case KeyCode.S: if (focus + cols < n) focus += cols; e.Use(); break;
            }
        }

        void DrawHint(BirthStep step, float w, float h)
        {
            string text = null;
            if (step.Interactive) text = "Chuột: rê và nhấp để chọn   ·   ← → ↑ ↓ rồi Enter";
            else if (flow.CanSkip) text = flow.Index < flow.IndexOf(BirthStepKind.DealCore) ? "Space / Enter: tiếp tục   ·   Tab: bỏ qua phần mở đầu" : "Space / Enter: tiếp tục";
            if (text == null) return;
            smallStyle.fontSize = Mathf.RoundToInt(15f * Ui(h));   // DrawFace resizes it for the cards
            GUI.color = new Color(0.7f, 0.9f, 1f, 0.55f);
            GUI.Label(new Rect(0, h - 44f, w, 26f), text, smallStyle);
            GUI.color = Color.white;
        }

        // ---- boot, lines, gifts

        void DrawBoot(float w, float h, float ui)
        {
            var win = new Rect(w * 0.3f, h * 0.3f, w * 0.4f, h * 0.36f);
            BirthUi.Window(win, "[HỆ THỐNG]", titleStyle, clock);
            float s = flow.StepTime;
            var lines = new (float at, string text)[]
            {
                (0.2f, "HỆ THỐNG ĐANG KHỞI ĐỘNG"),
                (1.0f, "Kết nối linh hồn .......... [OK]"),
                (2.0f, "Nạp dữ liệu sinh thể .......... [OK]"),
                (3.0f, "Mở cổng chuyển sinh .......... [OK]"),
            };
            for (int i = 0; i < lines.Length; i++)
            {
                if (s < lines[i].at) continue;
                GUI.Label(new Rect(win.x + 24f, win.y + 52f * ui + i * 40f * ui, win.width - 48f, 36f * ui), BirthUi.Typewriter(lines[i].text, s - lines[i].at, 40f), bodyStyle);
            }
            float k = Mathf.Clamp01(s / Mathf.Max(0.1f, flow.Current.MinSeconds));
            var bar = new Rect(win.x + 24f, win.yMax - 38f * ui, win.width - 48f, 12f * ui);
            BirthUi.Rounded(bar, new Color(0.2f, 0.5f, 0.7f, 0.2f), 6f);
            BirthUi.Rounded(new Rect(bar.x, bar.y, bar.width * k, bar.height), new Color(BirthUi.Cyan.r, BirthUi.Cyan.g, BirthUi.Cyan.b, 0.85f), 6f);
        }

        void DrawLine(BirthStep step, float w, float h, float ui)
        {
            var win = new Rect(w * 0.17f, h * 0.34f, w * 0.66f, h * 0.3f);
            BirthUi.Window(win, "[HỆ THỐNG]  THÔNG BÁO", titleStyle, clock);
            GUI.Label(new Rect(win.x + 30f, win.y + 56f * ui, win.width - 60f, win.height - 110f * ui), BirthUi.Typewriter(step.Text, flow.StepTime - 0.2f), bodyStyle);
            DrawWave(new Rect(win.x + 30f, win.yMax - 46f * ui, win.width - 60f, 28f * ui));
        }

        /// <summary>A small voice meter: it moves while Tiếng Vọng is speaking.</summary>
        void DrawWave(Rect r)
        {
            bool talking = voice != null && voice.Busy;
            int bars = 36;
            float bw = r.width / bars;
            for (int i = 0; i < bars; i++)
            {
                float amp = talking ? 0.25f + 0.75f * Mathf.Abs(Mathf.Sin(clock * 9f + i * 0.7f) * Mathf.Sin(clock * 3.1f + i * 0.31f)) : 0.08f;
                float bh = Mathf.Max(2f, r.height * amp);
                BirthUi.Fill(new Rect(r.x + i * bw + 1f, r.center.y - bh / 2f, bw - 3f, bh), new Color(BirthUi.Cyan.r, BirthUi.Cyan.g, BirthUi.Cyan.b, talking ? 0.7f : 0.25f));
            }
        }

        void DrawGifts(BirthStep step, float w, float h, float ui)
        {
            var win = new Rect(w * 0.27f, h * 0.17f, w * 0.46f, h * 0.62f);
            BirthUi.Window(win, "[HỆ THỐNG]  ÂN ĐIỂN KHỞI SINH", titleStyle, clock);
            for (int i = 0; i < BirthScript.Gifts.Count; i++)
            {
                var g = BirthScript.Gifts[i];
                var row = new Rect(win.x + 20f, win.y + 52f * ui + i * 70f * ui, win.width - 40f, 62f * ui);
                bool now = i == step.GiftIndex, done = i < step.GiftIndex;
                if (now)
                {
                    float pulse = 0.5f + 0.5f * Mathf.Sin(clock * 5f);
                    BirthUi.Rounded(row, new Color(BirthUi.Cyan.r, BirthUi.Cyan.g, BirthUi.Cyan.b, 0.10f + 0.06f * pulse), 8f);
                    BirthUi.Outline(row, new Color(BirthUi.Cyan.r, BirthUi.Cyan.g, BirthUi.Cyan.b, 0.8f), 1.5f, 8f);
                }
                var icon = new Vector2(row.x + 30f * ui, row.center.y);
                BirthUi.Ring(icon, 15f * ui, new Color(BirthUi.Cyan.r, BirthUi.Cyan.g, BirthUi.Cyan.b, now ? 1f : done ? 0.6f : 0.2f), 2f);
                if (now) BirthUi.Glow(icon, 54f * ui, new Color(BirthUi.Cyan.r, BirthUi.Cyan.g, BirthUi.Cyan.b, 0.7f));
                if (done) BirthUi.Fill(new Rect(icon.x - 4f, icon.y - 4f, 8f, 8f), new Color(BirthUi.Cyan.r, BirthUi.Cyan.g, BirthUi.Cyan.b, 0.8f));

                string name = (now || done) ? g.Name : "???";
                string line = now ? BirthUi.Typewriter(g.Line + (g.Implemented ? "" : "  (chưa có hiệu ứng)"), flow.StepTime - 0.2f, 30f)
                            : done ? g.Line : "— — —";
                GUI.color = new Color(1f, 1f, 1f, now ? 1f : done ? 0.75f : 0.35f);
                GUI.Label(new Rect(row.x + 64f * ui, row.y + 4f * ui, row.width - 80f * ui, 28f * ui), name, nameStyleLeft);
                GUI.Label(new Rect(row.x + 64f * ui, row.y + 32f * ui, row.width - 80f * ui, 26f * ui), line, smallLeft);
                GUI.color = Color.white;
            }
        }

        // ---- cards

        void DrawDealing(BirthStep step, float w, float h, float ui)
        {
            bool flaw = IsFlawStep(step.Kind);
            bool dealing = step.Kind == BirthStepKind.DealCore || step.Kind == BirthStepKind.DealFlaw;
            bool picking = step.Interactive;
            bool reveal = step.Kind == BirthStepKind.RevealCore || step.Kind == BirthStepKind.RevealFlaw;
            int n = flaw ? flawOrder.Length : deck.Length;
            var slots = Slots(n, flaw, w, h);
            float s = flow.StepTime;
            int chosen = flaw ? pickedFlaw : pickedCore;

            int hover = picking ? HoverIndex(Event.current.mousePosition) : -1;
            if (hover >= 0) focus = hover;
            int sel = picking ? Mathf.Clamp(focus, 0, Mathf.Max(0, n - 1)) : -1;

            // the prompt (and the result, once a card is turned)
            string title = flaw ? "[HỆ THỐNG]  KHUYẾT ĐIỂM" : "[HỆ THỐNG]  LÕI";
            var top = new Rect(w * 0.2f, h * 0.035f, w * 0.6f, h * 0.15f);
            if (!reveal)
            {
                BirthUi.Window(top, title, titleStyle, clock);
                string prompt = picking ? "Chọn một lá bài." : step.Text;
                GUI.Label(new Rect(top.x + 24f, top.y + 44f * ui, top.width - 48f, top.height - 54f * ui), BirthUi.Typewriter(prompt, s - 0.2f), bodyStyle);
            }

            float growth = flaw ? 0.25f : 0.5f;
            for (int i = 0; i < n; i++)
            {
                float p = dealing ? BirthUi.Smooth((s - i * 0.22f) / 0.55f) : 1f;
                if (p <= 0f) continue;
                Vector2 center = slots[i].center;
                if (dealing) center = Vector2.Lerp(new Vector2(w / 2f, h + slots[i].height), center, p);
                else center.y += Mathf.Sin(clock * 1.3f + i * 0.9f) * 4f * ui;
                float size = 1f, flip = 0f, alpha = 1f, lift = 0f;
                if (picking && i == sel) { lift = -14f * ui; size = 1.06f; }
                if (reveal)
                {
                    if (i == chosen)
                    {
                        float t = BirthUi.Smooth(s / 0.6f);
                        center = Vector2.Lerp(center, new Vector2(w / 2f, h * 0.46f), t);
                        size = 1f + growth * t;
                        flip = BirthUi.Smooth((s - 0.6f) / 0.9f);
                    }
                    else alpha = 1f - 0.88f * BirthUi.Smooth(s / 0.5f);
                }
                var r = new Rect(center.x - slots[i].width * size / 2f, center.y + lift - slots[i].height * size / 2f, slots[i].width * size, slots[i].height * size);
                bool isChosen = reveal && i == chosen;
                DrawCard(r, flip, alpha, flaw, isChosen ? FaceOf(flaw, chosen) : null, picking && i == sel, i, Mathf.Max(0f, s - 1.5f));
            }

            if (reveal)
            {
                float t0 = 1.5f;   // the moment the card is face up
                if (s > t0)
                {
                    var color = flaw ? FlawColor : RarityColor(deck[pickedCore].Rarity);
                    DrawBurst(new Vector2(w / 2f, h * 0.46f), color, s - t0, !flaw && deck[pickedCore].Rarity == TraitRarity.Mythic, w, h, ui);
                }
                DrawResultWindow(flaw, w, h, ui, Mathf.Max(0f, s - t0));
            }
        }

        sealed class Face
        {
            public string Title = "", Body = "", Footer = "";
            public Color Accent = BirthUi.Cyan, Orb = BirthUi.Cyan;
            public bool Dark;
        }

        Face FaceOf(bool flaw, int index)
        {
            if (index < 0) return null;
            if (!flaw)
            {
                var card = deck[index];
                var info = CoreInfo(card.Id);
                return new Face
                {
                    Title = info.name,
                    Body = info.summary + (info.implemented ? "" : "\n(chưa có hiệu ứng)"),
                    Footer = RarityName(card.Rarity),
                    Accent = RarityColor(card.Rarity),
                    Orb = card.Rarity == TraitRarity.Common ? info.orb : RarityColor(card.Rarity)
                };
            }
            var sb = new System.Text.StringBuilder();
            foreach (var id in flawOrder[index])
            {
                var f = FlawInfo(id);
                sb.AppendLine("• " + f.name);
                sb.AppendLine(f.description + (f.implemented ? "" : " (chưa có hiệu ứng)"));
            }
            return new Face { Title = "KHUYẾT ĐIỂM", Body = sb.ToString(), Footer = "CÁI GIÁ", Accent = FlawColor, Dark = true };
        }

        /// <summary>
        /// One card. flip 0 = face down (every back is identical), 1 = face up; in between it is squeezed to a line and widened again, turning
        /// over in the middle. <paramref name="face"/> is what is on its front.
        /// </summary>
        void DrawCard(Rect r, float flip, float alpha, bool flawStyle, Face face, bool highlight, int index, float sinceFace)
        {
            var old = GUI.matrix;
            var pivot = r.center;
            float sx = Mathf.Max(0.03f, Mathf.Abs(Mathf.Cos(Mathf.PI * flip)));
            GUI.matrix = GUI.matrix * Matrix4x4.TRS(pivot, Quaternion.identity, new Vector3(sx, 1f, 1f)) * Matrix4x4.TRS(-pivot, Quaternion.identity, Vector3.one);
            if (flip >= 0.5f && face != null) DrawFace(r, alpha, face, sinceFace);
            else DrawBack(r, alpha, flawStyle, highlight, index);
            GUI.matrix = old;
        }

        void DrawBack(Rect r, float alpha, bool flawStyle, bool highlight, int index)
        {
            var accent = flawStyle ? FlawColor : BirthUi.Cyan;
            float glow = highlight ? 0.30f : 0.07f;
            BirthUi.Rounded(new Rect(r.x - 6f, r.y - 6f, r.width + 12f, r.height + 12f), new Color(accent.r, accent.g, accent.b, glow * alpha), 18f);
            BirthUi.Rounded(r, flawStyle ? new Color(0.075f, 0.02f, 0.03f, 0.97f * alpha) : new Color(0.02f, 0.05f, 0.095f, 0.97f * alpha), 14f);
            BirthUi.Outline(r, new Color(accent.r, accent.g, accent.b, (highlight ? 1f : 0.55f) * alpha), highlight ? 3f : 2f, 14f);
            var inner = new Rect(r.x + 9f, r.y + 9f, r.width - 18f, r.height - 18f);
            BirthUi.Outline(inner, new Color(accent.r, accent.g, accent.b, 0.22f * alpha), 1f, 9f);
            BirthUi.Sigil(r.center, r.width * 0.33f, clock + index * 1.7f, new Color(accent.r, accent.g, accent.b, 0.6f * alpha));
            BirthUi.Glow(r.center, r.width * 0.5f, new Color(accent.r, accent.g, accent.b, (highlight ? 0.35f : 0.12f) * alpha));
        }

        void DrawFace(Rect r, float alpha, Face face, float sinceFace)
        {
            var a = face.Accent;
            BirthUi.Rounded(new Rect(r.x - 10f, r.y - 10f, r.width + 20f, r.height + 20f), new Color(a.r, a.g, a.b, 0.10f * alpha), 22f);
            BirthUi.Rounded(new Rect(r.x - 4f, r.y - 4f, r.width + 8f, r.height + 8f), new Color(a.r, a.g, a.b, 0.16f * alpha), 17f);
            BirthUi.Rounded(r, face.Dark ? new Color(0.085f, 0.025f, 0.035f, 0.98f * alpha) : new Color(0.022f, 0.055f, 0.1f, 0.98f * alpha), 14f);
            BirthUi.Rounded(new Rect(r.x + 5f, r.y + 5f, r.width - 10f, r.height * 0.36f), new Color(1f, 1f, 1f, 0.04f * alpha), 11f);
            BirthUi.Outline(r, new Color(a.r, a.g, a.b, 0.95f * alpha), 2.5f, 14f);
            BirthUi.Outline(new Rect(r.x + 9f, r.y + 9f, r.width - 18f, r.height - 18f), new Color(a.r, a.g, a.b, 0.35f * alpha), 1f, 9f);

            if (!face.Dark)   // the orb of a core
            {
                var c = new Vector2(r.center.x, r.y + r.height * 0.22f);
                BirthUi.Glow(c, r.width * 0.8f, new Color(a.r, a.g, a.b, 0.35f * alpha));
                BirthUi.Ring(c, r.width * 0.17f, new Color(a.r, a.g, a.b, 0.9f * alpha), 2f);
                BirthUi.Glow(c, r.width * 0.3f, new Color(face.Orb.r, face.Orb.g, face.Orb.b, 0.95f * alpha));
            }
            float titleTop = face.Dark ? r.y + r.height * 0.07f : r.y + r.height * 0.38f;
            nameStyle.fontSize = Mathf.Max(10, Mathf.RoundToInt(r.width * (face.Dark ? 0.085f : 0.092f)));
            lineStyle.fontSize = Mathf.Max(9, Mathf.RoundToInt(r.width * (face.Dark ? 0.058f : 0.064f)));
            smallStyle.fontSize = Mathf.Max(9, Mathf.RoundToInt(r.width * 0.06f));
            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.Label(new Rect(r.x + 10f, titleTop, r.width - 20f, r.height * 0.14f), face.Title, nameStyle);
            BirthUi.Fill(new Rect(r.x + r.width * 0.2f, titleTop + r.height * 0.14f, r.width * 0.6f, 1f), new Color(a.r, a.g, a.b, 0.5f * alpha));
            GUI.Label(new Rect(r.x + 12f, titleTop + r.height * 0.16f, r.width - 24f, r.height * (face.Dark ? 0.7f : 0.34f)), face.Body, lineStyle);
            GUI.color = Color.white;
            var band = new Rect(r.x + 12f, r.yMax - r.height * 0.105f, r.width - 24f, r.height * 0.075f);
            BirthUi.Rounded(band, new Color(a.r, a.g, a.b, 0.22f * alpha), 6f);
            GUI.color = new Color(a.r, a.g, a.b, alpha);
            GUI.Label(band, face.Footer, smallStyle);
            GUI.color = Color.white;

            float u = Mathf.Clamp01(sinceFace * 0.9f);   // one shine sweeps across when the card turns up
            if (u > 0f && u < 1f) BirthUi.Fill(new Rect(r.x + 6f + u * (r.width - 26f), r.y + 8f, 14f, r.height - 16f), new Color(1f, 1f, 1f, 0.16f * (1f - u) * alpha));
        }

        /// <summary>The flash when a card turns up: a ring, sparks and, for a mythic card, a golden flash of the whole screen.</summary>
        void DrawBurst(Vector2 center, Color color, float t, bool mythic, float w, float h, float ui)
        {
            if (t > 2f) return;
            BirthUi.Ring(center, t * 560f * ui, new Color(color.r, color.g, color.b, Mathf.Clamp01(1f - t / 1.1f) * 0.7f), 3f);
            if (mythic) BirthUi.Ring(center, Mathf.Max(0f, t - 0.15f) * 760f * ui, new Color(1f, 0.9f, 0.5f, Mathf.Clamp01(1f - t / 1.3f) * 0.6f), 2f);
            for (int i = 0; i < (mythic ? 46 : 26); i++)
            {
                float ang = BirthUi.Hash(i * 2 + 1) * Mathf.PI * 2f;
                float speed = (160f + 300f * BirthUi.Hash(i * 2 + 2)) * ui;
                float life = Mathf.Clamp01(1f - t / 1.5f);
                var p = center + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * speed * t;
                float sz = (4f + 5f * BirthUi.Hash(i + 99)) * ui * life;
                BirthUi.Fill(new Rect(p.x - sz / 2f, p.y - sz / 2f, sz, sz), new Color(color.r, color.g, color.b, life));
            }
            if (mythic && t < 0.5f) BirthUi.Fill(new Rect(0, 0, w, h), new Color(1f, 0.85f, 0.4f, 0.4f * (1f - t / 0.5f)));
        }

        void DrawResultWindow(bool flaw, float w, float h, float ui, float t)
        {
            var win = new Rect(w * 0.2f, h * 0.76f, w * 0.6f, h * 0.2f);
            BirthUi.Window(win, flaw ? "[HỆ THỐNG]  KHUYẾT ĐIỂM" : "[HỆ THỐNG]  LÕI ĐỘC NHẤT", titleStyle, clock);
            string text;
            if (!flaw)
            {
                var card = deck[pickedCore];
                var info = CoreInfo(card.Id);
                text = $"{info.name}   ·   {RarityName(card.Rarity)}\n{info.summary}" + (info.implemented ? "" : "\n(chưa có hiệu ứng)");
            }
            else
            {
                var sb = new System.Text.StringBuilder();
                foreach (var id in flawOrder[pickedFlaw]) { var f = FlawInfo(id); sb.AppendLine(f.name + ": " + f.description); }
                text = sb.ToString();
            }
            GUI.Label(new Rect(win.x + 24f, win.y + 44f * ui, win.width - 48f, win.height - 54f * ui), BirthUi.Typewriter(text, t, 44f), bodyStyleSmall);
        }

        // ---- summary

        void DrawSummary(BirthStep step, float w, float h, float ui)
        {
            float s = flow.StepTime;
            float a = BirthUi.Smooth(s / 0.8f);
            BirthUi.Sigil(new Vector2(w / 2f, h * 0.4f), Mathf.Min(w, h) * 0.34f * (0.9f + 0.1f * a), clock * 1.5f, new Color(BirthUi.Cyan.r, BirthUi.Cyan.g, BirthUi.Cyan.b, 0.25f * a));

            float cw = Mathf.Min(0.15f * w, 0.24f * h), ch = cw * 1.42f, gap = cw * 0.2f;
            float y = h * 0.4f - ch / 2f;
            var coreRect = new Rect(w / 2f - cw - gap / 2f, y, cw, ch);
            var flawRect = new Rect(w / 2f + gap / 2f, y, cw, ch);
            DrawCard(coreRect, 1f, a, false, FaceOf(false, pickedCore), false, 0, 3f);
            DrawCard(flawRect, 1f, a, true, FaceOf(true, pickedFlaw), false, 1, 3f);

            var core = CoreInfo(deck[pickedCore].Id);
            var names = new List<string>();
            foreach (var id in flawOrder[pickedFlaw]) names.Add(FlawInfo(id).name);
            var win = new Rect(w * 0.2f, h * 0.74f, w * 0.6f, h * 0.2f);
            BirthUi.Window(win, "[HỆ THỐNG]  SỐ MỆNH ĐÃ ĐỊNH", titleStyle, clock, a);
            GUI.Label(new Rect(win.x + 24f, win.y + 44f * ui, win.width - 48f, win.height - 54f * ui),
                BirthUi.Typewriter(BirthScript.SummaryLine(core.name, names) + "\n" + step.Text, s - 0.4f, 38f), bodyStyleSmall);
        }

        // ---------------------------------------------------------------- styles

        GUIStyle nameStyleLeft, smallLeft, bodyStyleSmall;

        void Setup()
        {
            if (titleStyle != null) return;
            titleStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.5f, 0.92f, 1f) } };
            bodyStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperLeft, wordWrap = true, normal = { textColor = new Color(0.9f, 0.97f, 1f) } };
            bodyStyleSmall = new GUIStyle(bodyStyle);
            nameStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperCenter, fontStyle = FontStyle.Bold, wordWrap = true, normal = { textColor = Color.white } };
            nameStyleLeft = new GUIStyle(nameStyle) { alignment = TextAnchor.UpperLeft };
            lineStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperCenter, wordWrap = true, normal = { textColor = new Color(0.78f, 0.9f, 1f) } };
            smallStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.7f, 0.85f, 0.95f) } };
            smallLeft = new GUIStyle(smallStyle) { alignment = TextAnchor.UpperLeft, fontStyle = FontStyle.Normal };
        }

        void UpdateStyles(float ui)
        {
            titleStyle.fontSize = Mathf.RoundToInt(15f * ui);
            bodyStyle.fontSize = Mathf.RoundToInt(26f * ui);
            bodyStyleSmall.fontSize = Mathf.RoundToInt(21f * ui);
            nameStyleLeft.fontSize = Mathf.RoundToInt(22f * ui);
            smallLeft.fontSize = Mathf.RoundToInt(16f * ui);
            smallStyle.fontSize = Mathf.RoundToInt(15f * ui);
        }
    }
}
