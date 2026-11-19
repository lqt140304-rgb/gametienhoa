using Evolve.Organs;
using Evolve.Swallow;
using UnityEngine;

namespace Evolve.Survival
{
    /// <summary>Túi Không Đáy (B), Biến Hình body panel (G), the Tiếng Vọng line and the dissolve bar.</summary>
    public partial class SurvivalHUD
    {
        Swallower swallowerRef;
        SlimeBody bodyRef;
        int chosenItem = -1;
        OrganSlot chosenSlot = OrganSlot.Eyes;
        string sculptNotice = "";
        string echoText;
        float echoUntil;
        GUIStyle echoStyle, hintStyle;

        // Passives only (spec 6.1): eyes and skin (heat membrane). Legs and back genes are active moves, on keys 1-5 (K).
        static readonly OrganSlot[] BodySlots = { OrganSlot.Eyes, OrganSlot.Skin };

        Swallower SwallowerRef => swallowerRef != null ? swallowerRef : swallowerRef = stats != null ? stats.GetComponent<Swallower>() : null;
        SlimeBody BodyRef => bodyRef != null ? bodyRef : bodyRef = stats != null ? stats.GetComponent<SlimeBody>() : null;
        float BodyMass => BodyRef != null ? BodyRef.Mass : MassRules.StartMass;

        static string Stars(int n) => n <= 0 ? "" : new string('★', Mathf.Clamp(n, 0, 3)) + new string('☆', 3 - Mathf.Clamp(n, 0, 3));

        static Color RarityColor(GeneRarity r) => r switch
        {
            GeneRarity.Rare => new Color(0.45f, 0.9f, 0.5f),
            GeneRarity.Epic => new Color(0.45f, 0.65f, 1f),
            GeneRarity.Legendary => new Color(1f, 0.82f, 0.25f),
            _ => new Color(0.88f, 0.9f, 0.92f)
        };

        static string ModeName(StomachMode m) => m switch
        {
            StomachMode.Analyze => "Phân tích",
            StomachMode.Keep => "Giữ",
            _ => "Tiêu hóa"
        };

        /// <summary>Túi Không Đáy: what is inside, what each item is used for, and the genome Giải Mã can see.</summary>
        void DrawStomachPanel()
        {
            var sw = SwallowerRef;
            if (sw == null) { Label(new Rect(30, 140, 900, 50), "Không có Túi Không Đáy."); return; }
            var st = sw.Stomach; var book = sw.Book;
            Label(new Rect(25, 128, 400, 25), $"TÚI KHÔNG ĐÁY · {st.Items.Count}/{st.Capacity}", small);
            if (chosenItem >= st.Items.Count) chosenItem = st.Items.Count - 1;
            if (chosenItem < 0 && st.Items.Count > 0) chosenItem = 0;
            for (int i = 0; i < st.Items.Count && i < 6; i++)
            {
                var it = st.Items[i]; float y = 160 + i * 72;
                if (Button(new Rect(25, y, 330, 60), $"{it.Label}\n{ModeName(it.Mode)} · {it.Progress * 100f:0}%", chosenItem == i)) chosenItem = i;
                Paint(new Rect(25, y + 62, 330, 4), new Color(.15f, .24f, .32f), Texture2D.whiteTexture);
                Paint(new Rect(25, y + 62, 330 * Mathf.Clamp01(it.Progress), 4), it.Mode == StomachMode.Analyze ? cyan : coral, Texture2D.whiteTexture);
            }
            if (st.Items.Count == 0)
            {
                Label(new Rect(25, 165, 330, 120), "Bụng rỗng.\nĐâm vào con mồi để nuốt sống, hoặc F vào xác (giữ F nếu xác to hơn bạn).", text);
            }
            else if (chosenItem >= 0)
            {
                var it = st.Items[chosenItem]; float x = 380, w = 830;
                Label(new Rect(x, 128, w, 40), it.Label, heading);
                Label(new Rect(x, 172, w, 22), $"Chất lượng mẫu ×{it.Quality:0.#} · khối {it.Mass:0.#} · dinh dưỡng {it.Nutrition:0}", small);
                int index = chosenItem;
                if (Button(new Rect(x, 200, 150, 34), "Tiêu hóa", it.Mode == StomachMode.Digest)) st.SetMode(index, StomachMode.Digest);
                WithEnabled(it.IsSample, () => { if (Button(new Rect(x + 160, 200, 150, 34), "Phân tích", it.Mode == StomachMode.Analyze)) st.SetMode(index, StomachMode.Analyze); });
                if (Button(new Rect(x + 320, 200, 150, 34), "Giữ", it.Mode == StomachMode.Keep)) st.SetMode(index, StomachMode.Keep);
                bool canSpit = SwallowRules.CanSpit(NetWorld.IsClient);
                bool spat = false;
                WithEnabled(canSpit, () => { if (Button(new Rect(x + 480, 200, 120, 34), "Nhả ra")) { sw.Spit(index); chosenItem = -1; spat = true; } });
                if (spat) return;
                DrawGenome(it, book, x, 250, w);
            }
            bool shelter = stats.State.InShelter;
            Label(new Rect(25, 578, 1180, 25), $"Giải Mã cấp {book.DecoderLevel} · đã phân tích {book.AnalysesDone} mẫu · " +
                (shelter ? "đang ở hang trú: phân tích nhanh ×4" : "vào hang trú để phân tích nhanh ×4"), small);
        }

        void DrawGenome(StomachItem it, AnalysisBook book, float x, float y, float w)
        {
            if (!it.IsSample) { Label(new Rect(x, y, w, 50), "Không còn gì để Giải Mã: chỉ còn là thức ăn.", text); return; }
            int level = book.DecoderLevel;
            Label(new Rect(x, y, w, 22), "BẢNG DI TRUYỀN", small);
            for (int i = 0; i < it.Genes.Count && i < 7; i++)
            {
                var g = it.Genes[i]; float ry = y + 28 + i * 40;
                var prev = GUI.color; GUI.color = RarityColor(g.Rarity);
                string name = book.Knows(g.Organ) || AnalysisRules.ShowsName(level) ? g.Organ.DisplayName : "???";
                Label(new Rect(x, ry, 300, 24), name, text);
                GUI.color = prev;
                if (book.Knows(g.Organ))
                {
                    Label(new Rect(x + 310, ry, 300, 24), $"Đã học {Stars(book.StarsOf(g.Organ))}", text);
                    continue;
                }
                float pity = book.PityOf(g.Organ);
                Paint(new Rect(x + 310, ry + 8, 300, 6), new Color(.15f, .24f, .32f), Texture2D.whiteTexture);
                Paint(new Rect(x + 310, ry + 8, 300 * Mathf.Clamp01(pity), 6), RarityColor(g.Rarity), Texture2D.whiteTexture);
                Label(new Rect(x + 620, ry, 200, 24), AnalysisRules.ShowsChance(level)
                    ? $"{AnalysisRules.Chance(g.Rarity) * it.Quality * AnalysisRules.DecoderChanceMultiplier(level) * 100f:0.#}% mỗi mẫu"
                    : $"tích lũy {pity * 100f:0}%", small);
            }
        }

        /// <summary>Biến Hình: four body slots on the left; on the right, what Giải Mã has learned for the chosen slot.</summary>
        void DrawOrgansPanel()
        {
            if (organs == null) { Label(new Rect(30, 140, 900, 50), "Không có dữ liệu cơ quan."); return; }
            var book = organs.Book; var body = BodyRef; bool shelter = stats.State.InShelter;
            Label(new Rect(25, 128, 330, 25), $"BỊ ĐỘNG {organs.Count}/{SkillRules.MaxPassives} · khối {BodyMass:0.#}", small);
            for (int i = 0; i < BodySlots.Length; i++)
            {
                var slot = BodySlots[i]; var o = organs.GetInSlot(slot);
                string label = $"{OrganInteractor.SlotName(slot)}\n{(o == null ? "— trống —" : $"{o.DisplayName} {Stars(organs.GetLevel(o))}")}";
                if (Button(new Rect(25, 169 + i * 83, 300, 74), label, chosenSlot == slot)) { chosenSlot = slot; sculptNotice = ""; }
            }

            float x = 360, w = 850;
            Label(new Rect(x, 128, w, 25), shelter ? "Đang ở hang trú: có thể nặn lại cơ thể." : "Chỉ nặn lại cơ thể được trong hang trú.", small);
            var cur = organs.GetInSlot(chosenSlot);
            float y = 160;
            if (cur != null)
            {
                float refund = MassRules.Refund(AnalysisRules.FromTier(cur.Tier));
                Label(new Rect(x, y, w - 240, 50), $"{OrganTiers.Tag(cur.Tier)} {cur.DisplayName} {Stars(organs.GetLevel(cur))}\n{cur.Description}", text);
                WithEnabled(shelter, () => { if (Button(new Rect(x + w - 230, y + 6, 230, 36), $"Tháo (+{refund:0.#} khối)")) { organs.Remove(chosenSlot, body, shelter); sculptNotice = ""; } });
                y += 70;
            }
            Line(x, y, w); y += 10;
            Label(new Rect(x, y, w, 22), "ĐÃ HỌC CHO VỊ TRÍ NÀY", small); y += 26;
            int rows = 0;
            if (book != null)
                foreach (var o in book.Known)
                {
                    if (o.Use != GeneUse.Passive || o.Slot != chosenSlot || o == cur || rows >= 5) continue;
                    var rarity = AnalysisRules.FromTier(o.Tier);
                    float cost = MassRules.SculptCost(rarity);
                    var prev = GUI.color; GUI.color = RarityColor(rarity);
                    Label(new Rect(x, y + 6, w - 240, 26), $"{o.DisplayName} {Stars(book.StarsOf(o))} · {cost:0.#} khối", text);
                    GUI.color = prev;
                    var organ = o;
                    float refundCur = cur != null ? MassRules.Refund(AnalysisRules.FromTier(cur.Tier)) : 0f;
                    string block = MassRules.SculptBlockReason(shelter, BodyMass, refundCur, rarity);
                    if (block != null && shelter) Label(new Rect(x + w - 560, y + 6, 320, 26), block, small);
                    WithEnabled(block == null, () =>
                    {
                        if (!Button(new Rect(x + w - 230, y, 230, 36), cur == null ? "Nặn" : "Nặn thay")) return;
                        var r = organs.TrySculpt(organ, book, body, shelter);
                        sculptNotice = r switch
                        {
                            SculptResult.Ok => "",
                            SculptResult.NoMass => $"Không đủ khối (cần {cost:0.#}, giữ tối thiểu {MassRules.MinMass:0}).",
                            SculptResult.NotInShelter => "Chỉ nặn lại cơ thể được trong hang trú.",
                            SculptResult.NotPassive => "Chiêu chủ động gắn phím ở bảng K, không nặn lên người.",
                            _ => organs.Check(organ) == AttachCheck.NoRoom
                                ? $"Đã đủ {SkillRules.MaxPassives} kỹ năng bị động: tháo bớt một cái."
                                : "Không nặn được (thiếu chất dinh dưỡng)."
                        };
                    });
                    y += 44; rows++;
                }
            if (rows == 0) Label(new Rect(x, y, w, 50), "Chưa học được kỹ năng bị động nào cho vị trí này.\nChiêu chủ động (gai, chạy nhanh...) ở bảng K, đổi ở đâu cũng được.", text);
            if (sculptNotice.Length > 0) Label(new Rect(x, 545, w, 26), sculptNotice, text);
        }

        /// <summary>Tiếng Vọng: one line at a time near the top of the screen, 3 seconds each, fading out.</summary>
        void DrawEchoLine()
        {
            if (Time.time > echoUntil && Echo.TryNext(out var next)) { echoText = next; echoUntil = Time.time + 3f; }
            if (echoText == null) return;
            float fade = Mathf.Clamp01((echoUntil + 0.6f - Time.time) / 0.6f);
            if (fade <= 0f) { echoText = null; return; }
            if (echoStyle == null) echoStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 20, fontStyle = FontStyle.Italic, wordWrap = true };
            var prev = GUI.color;
            GUI.color = new Color(0.75f, 0.95f, 1f, fade);
            GUI.Label(new Rect(Screen.width / 2f - 400, Screen.height * 0.11f, 800, 60), echoText, echoStyle);
            GUI.color = prev;
        }

        /// <summary>The carcass hint (F) and the dissolve bar while F is held on a carcass too big to swallow whole.</summary>
        void DrawSwallowHint()
        {
            var sw = SwallowerRef;
            if (sw == null || sw.Touching == null || MenuOpen) return;
            if (hintStyle == null) hintStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 16, fontStyle = FontStyle.Bold };
            var sample = sw.Touching.GetComponent<CarcassSample>();
            bool whole = MassRules.CanSwallowWhole(BodyMass, sample != null ? sample.Mass : 5f);
            float cx = Screen.width / 2f, y = Screen.height - 200;
            GUI.Label(new Rect(cx - 250, y, 500, 26), whole ? "F · Nuốt xác" : "Giữ F · Hòa tan xác (to hơn bạn)", hintStyle);
            float p = sw.DissolveProgress;
            if (p < 0f) return;
            GUI.DrawTexture(new Rect(cx - 150, y + 30, 300, 8), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, new Color(0.1f, 0.15f, 0.2f, 0.8f), 0, 0);
            GUI.DrawTexture(new Rect(cx - 150, y + 30, 300 * Mathf.Clamp01(p), 8), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, new Color(0.5f, 0.9f, 0.95f, 1f), 0, 0);
        }
    }
}
