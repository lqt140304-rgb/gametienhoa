using System;
using System.Collections.Generic;
using Evolve.Survival;
using UnityEngine;

namespace Evolve.Traits
{
    /// <summary>
    /// The birth scene (spec 2026-11-19-birth-scene-design): when a player has no core yet, the slime is frozen under a dark screen and
    /// Tiếng Vọng announces the six gifts, then fate deals a core card (by rarity) and a flaw card, each face down and turned up.
    /// The result is drawn before the first frame (FateRoll); the scene only shows it. Space or Enter skips ahead to the summary after 3 seconds.
    /// It replaces the old choosing screen. Runs once per run: dying and respawning never plays it again.
    /// </summary>
    public class BirthScene : MonoBehaviour
    {
        enum State { Waiting, Running, Done }

        const int CoreDecoys = 7;   // face-down cards in the fan (the middle one is the fate card)

        State state;
        float t;
        bool ranThisRun;
        FateResult result;
        int fateSlot = CoreDecoys / 2;   // which card of the fan is the fate card (random, so the middle one is not a giveaway)
        PlayerTraits player;
        float nextFind;
        BirthVoice voice;
        readonly HashSet<string> spoken = new HashSet<string>();

        // the frozen "space" while the scene plays (same three flags the old picker used)
        SurvivalStats frozenStats;
        bool freezing, prevInvulnerable, prevExternalMenu;

        Texture2D pixel, disc;
        GUIStyle titleStyle, nameStyle, lineStyle, smallStyle, echoStyle;

        // Tiếng Vọng's cues: when each line is said (seconds into the scene) and its id in BirthScript.EchoLines.
        static readonly (float time, string id)[] Cues =
        {
            (2.2f, "echo.born"), (3.5f, "echo.gift"), (7.2f, "echo.core"), (14.2f, "echo.flaw"), (19.2f, "echo.done")
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (FindAnyObjectByType<BirthScene>() != null) return;
            var go = new GameObject("Birth Scene");
            DontDestroyOnLoad(go);
            go.AddComponent<BirthScene>();
        }

        void Awake() => voice = gameObject.AddComponent<BirthVoice>();

        void OnDisable() => Unfreeze();

        void OnDestroy() { if (disc != null) Destroy(disc); }

        // ---------------------------------------------------------------- flow

        void Update()
        {
            if (state == State.Waiting)
            {
                if (Time.unscaledTime < nextFind) return;
                nextFind = Time.unscaledTime + 0.5f;
                player = TraitNet.Local;
                if (player != null && BirthTimeline.ShouldRun(player.CoreId != null, ranThisRun)) Begin();
                return;
            }
            if (state != State.Running) return;

            t += Time.unscaledDeltaTime;
            foreach (var cue in Cues)
            {
                if (t < cue.time || spoken.Contains(cue.id)) continue;
                spoken.Add(cue.id);
                if (t - cue.time < 1f) voice.Play(cue.id);   // after a skip the missed lines stay silent
            }
            if (t >= BirthTimeline.Total) Finish();
        }

        void Begin()
        {
            int seed = FateRoll.PlayerSeed(Environment.TickCount, 0);
            var r = FateRoll.Roll(seed, TraitCatalog.FateCards());
            if (!r.HasValue)
            {
                Debug.LogWarning("Cảnh ra đời: kho Lõi rỗng, bỏ qua (chạy Evolve → Create Traits).");
                ranThisRun = true;
                return;
            }
            result = r.Value;
            fateSlot = new System.Random(seed ^ 0x5bd1e995).Next(1, CoreDecoys - 1);   // never an end card
            t = 0f;
            spoken.Clear();
            state = State.Running;
            Freeze();
        }

        void Finish()
        {
            if (player != null)
            {
                player.Set(result.CoreId, result.FlawIds);
                if (TraitNet.IsClient) TraitNet.ClientPick?.Invoke(result.CoreId, result.FlawIds);   // co-op: tell the host what fate gave
            }
            Unfreeze();
            ranThisRun = true;
            state = State.Done;
        }

        void Freeze()
        {
            var stats = player != null ? player.GetComponent<SurvivalStats>() : null;
            if (freezing || stats == null) return;
            frozenStats = stats;
            prevInvulnerable = stats.State.Invulnerable;
            prevExternalMenu = SurvivalHUD.ExternalMenuOpen;
            stats.State.Invulnerable = true;
            stats.Frozen = true;
            SurvivalHUD.ExternalMenuOpen = true;
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
            frozenStats = null;
        }

        // ---------------------------------------------------------------- names

        static (string name, string summary, bool implemented, Color orb) CoreInfo(string id)
        {
            var so = TraitCatalog.Core(id);
            if (so != null) return (so.DisplayName, so.Summary, so.Implemented, so.OrbColor);
            foreach (var d in TraitSeed.Cores) if (d.Id == id) return (d.Name, d.Summary, d.Implemented, Color.cyan);
            return (id, "", false, Color.cyan);
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
            TraitRarity.Rare => new Color(0.35f, 0.62f, 1f),
            TraitRarity.Mythic => new Color(1f, 0.8f, 0.25f),
            _ => new Color(0.82f, 0.86f, 0.9f)
        };

        static string RarityName(TraitRarity r) => r switch
        {
            TraitRarity.Rare => "HIẾM",
            TraitRarity.Mythic => "CỰC HIẾM",
            _ => "THƯỜNG"
        };

        static string EchoText(string id)
        {
            foreach (var l in BirthScript.EchoLines) if (l.id == id) return l.text;
            return "";
        }

        // ---------------------------------------------------------------- drawing

        void OnGUI()
        {
            if (state != State.Running) return;
            Setup();
            GUI.depth = -10000;   // on top of every other menu

            var e = Event.current;
            if (e.type == EventType.KeyDown && (e.keyCode == KeyCode.Space || e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter))
            {
                t = BirthTimeline.Skip(t);
                e.Use();
            }
            if (e.type != EventType.Repaint && e.type != EventType.Layout) return;

            float w = Screen.width, h = Screen.height;
            var phase = BirthTimeline.PhaseAt(t);
            float u = BirthTimeline.PhaseFraction(t);

            GUI.color = new Color(0.012f, 0.03f, 0.07f, phase == BirthPhase.Drop ? 1f : 0.95f);
            GUI.DrawTexture(new Rect(0, 0, w, h), pixel);
            GUI.color = new Color(0.1f, 0.25f, 0.45f, 0.16f);
            float glow = Mathf.Max(w, h) * 1.1f;
            GUI.DrawTexture(new Rect(w / 2f - glow / 2f, h / 2f - glow / 2f, glow, glow), disc);
            GUI.color = Color.white;

            DrawDrop(w, h);
            if (phase == BirthPhase.Echo || phase == BirthPhase.DealCore || phase == BirthPhase.DealFlaw || phase == BirthPhase.Summary) DrawEchoLine(w, h);
            if (phase == BirthPhase.Echo || (phase == BirthPhase.DealCore && t < BirthTimeline.PhaseStart(BirthPhase.DealCore) + 0.6f)) DrawGifts(w, h);
            if (phase == BirthPhase.DealCore) DrawDealCore(w, h, u);
            if (phase == BirthPhase.DealFlaw) DrawDealFlaw(w, h, u);
            if (phase == BirthPhase.Summary) DrawSummary(w, h, u);

            if (t >= BirthTimeline.SkipAfter && phase != BirthPhase.Summary)
            {
                GUI.color = new Color(1f, 1f, 1f, 0.55f);
                GUI.Label(new Rect(0, h - 40, w, 24), "Space / Enter: bỏ qua", smallStyle);
                GUI.color = Color.white;
            }
        }

        /// <summary>A drop of slime falls from the top and ripples where it lands.</summary>
        void DrawDrop(float w, float h)
        {
            float land = BirthTimeline.DropSeconds;
            float cx = w / 2f, cy = h * 0.55f;
            if (t < land)
            {
                float k = t / land;
                float y = Mathf.Lerp(-30f, cy, k * k);   // accelerates as it falls
                for (int i = 5; i >= 0; i--)
                {
                    GUI.color = new Color(0.6f, 0.95f, 1f, 0.5f * (1f - i / 6f));
                    float s = 22f - i * 2.5f;
                    GUI.DrawTexture(new Rect(cx - s / 2f, y - i * 16f - s / 2f, s, s), disc);
                }
            }
            else if (t < land + 1.4f)
            {
                float k = (t - land) / 1.4f;
                GUI.color = new Color(0.6f, 0.95f, 1f, 0.5f * (1f - k));
                float s = Mathf.Lerp(30f, 360f, k);
                GUI.DrawTexture(new Rect(cx - s / 2f, cy - s / 8f, s, s / 4f), disc);   // a flat ripple
            }
            GUI.color = Color.white;
        }

        void DrawEchoLine(float w, float h)
        {
            string id = null; float time = 0f;
            foreach (var cue in Cues) if (t >= cue.time) { id = cue.id; time = cue.time; }
            if (id == null) return;
            float age = t - time;
            float alpha = Mathf.Clamp01(age / 0.4f) * Mathf.Clamp01((3.6f - age) / 0.6f);
            if (alpha <= 0f) return;
            GUI.color = new Color(0.75f, 0.95f, 1f, alpha);
            GUI.Label(new Rect(w * 0.1f, h * 0.17f, w * 0.8f, 44), "«" + EchoText(id) + "»", echoStyle);
            GUI.color = Color.white;
        }

        /// <summary>The six gifts of birth appear one by one.</summary>
        void DrawGifts(float w, float h)
        {
            float fadeOut = 1f - Mathf.Clamp01((t - BirthTimeline.PhaseStart(BirthPhase.DealCore)) / 0.6f);
            for (int i = 0; i < BirthScript.Gifts.Count; i++)
            {
                float appear = 4f + 0.45f * i;
                float a = Mathf.Clamp01((t - appear) / 0.4f) * fadeOut;
                if (a <= 0f) continue;
                var g = BirthScript.Gifts[i];
                float pulse = Mathf.Clamp01(1f - (t - appear) / 0.5f);   // a beat of light when it is carved into the body
                GUI.color = new Color(0.8f + 0.2f * pulse, 0.95f, 1f, a);
                string text = $"✦ {g.Name}  ·  {g.Line}" + (g.Implemented ? "" : "  (chưa có hiệu ứng)");
                GUI.Label(new Rect(w * 0.15f, h * 0.30f + i * 34f, w * 0.7f, 30), text, lineStyle);
            }
            GUI.color = Color.white;
        }

        void DrawDealCore(float w, float h, float u)
        {
            Title(w, h, "LÕI CỦA BẠN", u);
            float s = Mathf.Clamp(h / 720f, 0.7f, 1.4f), cw = 150f * s, ch = 210f * s;
            var deck = new Vector2(w / 2f, h * 0.84f);
            float flip = BirthTimeline.FlipProgress(u);
            var core = CoreInfo(result.CoreId);
            var rc = RarityColor(result.Rarity);
            int mid = CoreDecoys / 2;
            for (int i = 0; i < CoreDecoys; i++)
            {
                float p = Smooth(BirthTimeline.DealProgress(u, i, CoreDecoys));
                if (p <= 0f) continue;
                int off = i - mid;
                var slot = new Vector2(w / 2f + off * cw * 0.55f, h * 0.5f + Mathf.Abs(off) * 8f * s);
                float angle = off * 9f;
                if (u > 0.4f && u < 0.65f) angle += Mathf.Sin(Time.unscaledTime * 18f + i) * 3f;   // the shuffle
                var pos = Vector2.Lerp(deck, slot, p);
                float scale = 1f;
                float alpha = 1f;
                bool fate = i == fateSlot;
                if (fate)
                {
                    float lift = Smooth(flip);
                    pos = Vector2.Lerp(pos, new Vector2(w / 2f, h * 0.46f), lift);
                    scale = 1f + 0.5f * lift;
                    angle *= 1f - lift;
                }
                else alpha = 1f - 0.75f * Smooth(flip);
                var r = new Rect(pos.x - cw * scale / 2f, pos.y - ch * scale / 2f, cw * scale, ch * scale);
                // every back glows faintly in some rarity colour: the fate card's is its own, but it looks like the others
                var back = fate ? rc : RarityColor((TraitRarity)(i % 3));
                DrawCard(r, angle, fate ? flip : 0f, alpha, back, 0.3f, false,
                    core.name, core.summary + (core.implemented ? "" : "\n(chưa có hiệu ứng)"), RarityName(result.Rarity), rc);
                if (fate && flip > 0.9f && result.Rarity == TraitRarity.Mythic) DrawMythicGlow(r);
            }
        }

        void DrawDealFlaw(float w, float h, float u)
        {
            Title(w, h, "KHUYẾT ĐIỂM", u);
            float s = Mathf.Clamp(h / 720f, 0.7f, 1.4f), cw = 190f * s, ch = 210f * s, gap = 28f * s;
            var deck = new Vector2(w / 2f, h * 0.84f);
            float flip = BirthTimeline.FlipProgress(u);
            var flawIds = result.FlawIds ?? new string[0];
            var face = new System.Text.StringBuilder();
            foreach (var id in flawIds)
            {
                var f = FlawInfo(id);
                face.AppendLine("• " + f.name);
                face.AppendLine(f.description + (f.implemented ? "" : " (chưa có hiệu ứng)"));
            }
            var dark = new Color(0.8f, 0.25f, 0.25f);
            int n = 3;
            for (int i = 0; i < n; i++)
            {
                float p = Smooth(BirthTimeline.DealProgress(u, i, n));
                if (p <= 0f) continue;
                var slot = new Vector2(w / 2f + (i - 1) * (cw + gap), h * 0.5f);
                var pos = Vector2.Lerp(deck, slot, p);
                float angle = 0f;
                if (u > 0.4f && u < 0.65f) angle = Mathf.Sin(Time.unscaledTime * 16f + i * 2f) * 3f;
                bool fate = i == result.OptionIndex;
                float scale = 1f, alpha = 1f;
                if (fate)
                {
                    float lift = Smooth(flip);
                    pos = Vector2.Lerp(pos, new Vector2(w / 2f, h * 0.48f), lift);
                    scale = 1f + 0.35f * lift;
                }
                else alpha = 1f - 0.8f * Smooth(flip);
                var r = new Rect(pos.x - cw * scale / 2f, pos.y - ch * scale / 2f, cw * scale, ch * scale);
                DrawCard(r, angle, fate ? flip : 0f, alpha, dark, 0.45f, true, "Khuyết Điểm", face.ToString(), "", dark);
            }
        }

        void DrawSummary(float w, float h, float u)
        {
            float a = Mathf.Clamp01(u / 0.25f);
            Title(w, h, "SỐ MỆNH ĐÃ ĐỊNH", 0.5f);
            float s = Mathf.Clamp(h / 720f, 0.7f, 1.4f), cw = 170f * s, ch = 190f * s;
            var core = CoreInfo(result.CoreId);
            var rc = RarityColor(result.Rarity);
            var flawNames = new List<string>();
            var face = new System.Text.StringBuilder();
            foreach (var id in result.FlawIds ?? new string[0])
            {
                var f = FlawInfo(id);
                flawNames.Add(f.name);
                face.AppendLine("• " + f.name);
            }
            DrawCard(new Rect(w / 2f - cw - 16f, h * 0.45f - ch / 2f, cw, ch), 0f, 1f, a, rc, 0.8f, false, core.name, core.summary, RarityName(result.Rarity), rc);
            DrawCard(new Rect(w / 2f + 16f, h * 0.45f - ch / 2f, cw, ch), 0f, 1f, a, new Color(0.8f, 0.25f, 0.25f), 0.6f, true, "Khuyết Điểm", face.ToString(), "", new Color(0.8f, 0.25f, 0.25f));
            GUI.color = new Color(0.75f, 0.95f, 1f, a);
            GUI.Label(new Rect(w * 0.1f, h * 0.45f + ch / 2f + 26f, w * 0.8f, 40), "«" + BirthScript.SummaryLine(core.name, flawNames) + "»", echoStyle);
            GUI.color = Color.white;
        }

        void Title(float w, float h, string text, float u)
        {
            GUI.color = new Color(1f, 1f, 1f, Mathf.Clamp01(u / 0.1f));
            GUI.Label(new Rect(0, h * 0.1f, w, 32), text, titleStyle);
            GUI.color = Color.white;
        }

        /// <summary>
        /// A card. <paramref name="flip"/>: 0 = face down, 1 = face up (squeezed to a line in between, then widened again). Cards turn up in
        /// the middle of the turn. <paramref name="dark"/> draws the cracked, red-black look of a flaw.
        /// </summary>
        void DrawCard(Rect r, float angle, float flip, float alpha, Color border, float borderGlow, bool dark, string title, string body, string footer, Color footerColor)
        {
            var old = GUI.matrix;
            var pivot = r.center;
            GUIUtility.RotateAroundPivot(angle, pivot);
            float sx = Mathf.Max(0.03f, Mathf.Abs(Mathf.Cos(Mathf.PI * flip)));
            GUI.matrix = GUI.matrix * Matrix4x4.TRS(pivot, Quaternion.identity, new Vector3(sx, 1f, 1f)) * Matrix4x4.TRS(-pivot, Quaternion.identity, Vector3.one);
            bool up = flip >= 0.5f;

            GUI.color = new Color(border.r, border.g, border.b, (up ? 0.95f : borderGlow) * alpha);
            GUI.DrawTexture(r, pixel);
            GUI.color = dark ? new Color(0.09f, 0.03f, 0.04f, 0.97f * alpha) : new Color(0.04f, 0.1f, 0.22f, 0.97f * alpha);
            GUI.DrawTexture(new Rect(r.x + 2f, r.y + 2f, r.width - 4f, r.height - 4f), pixel);

            if (up)
            {
                GUI.color = new Color(1f, 1f, 1f, alpha);
                GUI.Label(new Rect(r.x + 8f, r.y + 14f, r.width - 16f, 56f), title, nameStyle);
                GUI.Label(new Rect(r.x + 10f, r.y + 74f, r.width - 20f, r.height - 110f), body, lineStyle);
                if (!string.IsNullOrEmpty(footer))
                {
                    GUI.color = new Color(footerColor.r, footerColor.g, footerColor.b, alpha);
                    GUI.Label(new Rect(r.x, r.yMax - 30f, r.width, 24f), footer, smallStyle);
                }
            }
            else
            {
                GUI.color = new Color(border.r, border.g, border.b, 0.25f * alpha);
                float d = Mathf.Min(r.width, r.height) * 0.45f;
                GUI.DrawTexture(new Rect(r.center.x - d / 2f, r.center.y - d / 2f, d, d), disc);   // a faint seal on the back
                if (dark)
                {
                    var save = GUI.matrix;
                    GUIUtility.RotateAroundPivot(35f, r.center);
                    GUI.color = new Color(0.8f, 0.25f, 0.25f, 0.35f * alpha);
                    GUI.DrawTexture(new Rect(r.center.x - 2f, r.center.y - r.height * 0.4f, 3f, r.height * 0.8f), pixel);   // a crack
                    GUI.matrix = save;
                }
            }
            GUI.color = Color.white;
            GUI.matrix = old;
        }

        /// <summary>A mythic card shines and shivers as it turns up.</summary>
        void DrawMythicGlow(Rect r)
        {
            float k = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 5f);
            GUI.color = new Color(1f, 0.82f, 0.3f, 0.25f + 0.2f * k);
            float s = r.width * (2.2f + 0.3f * k);
            GUI.DrawTexture(new Rect(r.center.x - s / 2f, r.center.y - s / 2f, s, s), disc);
            GUI.color = Color.white;
        }

        static float Smooth(float x) { x = Mathf.Clamp01(x); return x * x * (3f - 2f * x); }

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
            titleStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 22, fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.85f, 0.95f, 1f) } };
            nameStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperCenter, fontSize = 17, fontStyle = FontStyle.Bold, wordWrap = true, normal = { textColor = Color.white } };
            lineStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperCenter, fontSize = 14, wordWrap = true, normal = { textColor = new Color(0.78f, 0.9f, 1f) } };
            smallStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 13, fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.65f, 0.78f, 0.9f) } };
            echoStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 22, fontStyle = FontStyle.Italic, wordWrap = true, normal = { textColor = Color.white } };
        }
    }
}
