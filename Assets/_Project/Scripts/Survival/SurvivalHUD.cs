using System.Linq;
using Evolve.Abilities;
using Evolve.Organs;
using Evolve.Player;
using Evolve.World;
using UnityEngine;

namespace Evolve.Survival
{
    /// <summary>Temporary IMGUI bars for prototyping. Replace with UI Toolkit later.</summary>
    public class SurvivalHUD : MonoBehaviour
    {
        [SerializeField] SurvivalStats stats;
        [SerializeField] bool showControls = true;

        AbilityCaster caster;
        OrganHost organs;
        OrganInteractor interactor;
        SkillBar skills;
        string toast;
        float toastUntil;
        GUIStyle box;

        const string Controls =
            "WASD  Trườn\n" +
            "Shift  Chạy nhanh (tốn thức ăn)\n" +
            "Space  Nảy (tốn năng lượng)\n" +
            "Q  Dùng Dị Năng\n" +
            "1–5  Kỹ năng gắn cơ quan (cơ quan bậc B, Lv2)\n" +
            "Alt  Đổi chuột ⇄ hồng tâm (ngắm): kỹ năng\n    bắn/lao về phía hồng tâm\n" +
            "Giữ chuột phải  Xoay camera\n" +
            "Lăn chuột  Phóng to/thu nhỏ\n" +
            "C  Quét rung: ánh sáng lan ra, thấy thức ăn\n    và sinh vật xung quanh trong chốc lát\nV  Bật/tắt mắt (thử nghiệm)\n" +
            "H  Ẩn/hiện hướng dẫn\nF1  Menu God (thử nghiệm)\n\n" +
            "Đứng yên: không đói, hồi năng lượng,\nkẻ săn mồi khó phát hiện\n" +
            "Khi quét: xanh = thức ăn, đỏ = con mồi,\ntím = kẻ săn mồi\n\n" +
            "Hoang mạc (phía đông) rất nóng.\nGai độc / lao vào (Q) để hạ kẻ săn mồi.\nR  Hồi sinh khi chết (3 mạng; chết thì cơ quan\nrơi tại chỗ, mỗi cái 50% bị mất)\n\n" +
            "Bắt con mồi (chạm vào) để nhận cơ quan.\nChạm cơ quan rơi ra: E lắp, F hấp thụ, X bỏ qua.\n\n" +
            "EXP tiến hóa: ăn được ít, hấp thụ cơ quan được\nnhiều. G  Bảng cơ thể: đổ EXP cho cơ quan\n(mầm → Lv1 → Lv3), chọn ưu tiên, tháo.";

        void Start()
        {
            if (stats == null) return;
            caster = stats.GetComponent<AbilityCaster>();
            organs = stats.GetComponent<OrganHost>();
            interactor = stats.GetComponent<OrganInteractor>();
            skills = stats.GetComponent<SkillBar>();
            if (skills != null)
                skills.SkillUnlocked += (o, slot) =>
                    Toast($"Mở kỹ năng: {o.GrantedAbility.DisplayName}\n(phím {slot + 1}, từ {o.DisplayName})");
            if (organs != null)
            {
                organs.Attached += o => Toast($"Mầm {o.DisplayName}: cần {o.GrowthExpRequired:0} EXP tiến hóa\n(G để đổ EXP hoặc đặt ưu tiên)");
                organs.LeveledUp += (o, lv) => Toast($"{OrganTiers.Tag(o.Tier)} {o.DisplayName} lên cấp {lv}!");
                organs.Completed += o => Toast($"Tiến hóa: {OrganTiers.Tag(o.Tier)} {o.DisplayName}!\n{o.Description}");
                organs.Removed += o => Toast($"Đã tháo {o.DisplayName}\n(rơi ra đất, nhặt lại phải nuôi từ đầu)");
                if (interactor != null) interactor.DroppedOnDeath += (kept, lost) =>
                {
                    if (kept + lost > 0) Toast($"Cơ quan rơi tại chỗ chết: còn {kept}, mất {lost}");
                };
            }
        }

        void Toast(string text)
        {
            toast = text;
            toastUntil = Time.time + 4f;
        }

        void Update()
        {
            if (KeyPressed.H()) showControls = !showControls;
        }

        void OnGUI()
        {
            if (stats == null) return;
            var s = stats.State;
            Bar(0, "Máu", s.Health, s.MaxHealth, Color.red);
            Bar(1, "Đói", s.Hunger, s.MaxHunger, new Color(1f, 0.6f, 0.1f));
            Bar(2, "Năng lượng", s.Energy, s.MaxEnergy, Color.cyan);
            DrawTemperature(s);
            float y = 132;
            GUI.Label(new Rect(20, y, 400, 22), $"Mạng: {s.Lives}/{s.MaxLives}");
            y += 24;
            var sense = stats.GetComponent<VibrationSense>();
            if (sense != null)
            {
                string scan = sense.CooldownRemaining > 0f ? $"hồi {sense.CooldownRemaining:0.0}s"
                    : s.Energy < sense.EnergyCost ? $"thiếu năng lượng ({sense.EnergyCost:0})" : $"sẵn sàng ({sense.EnergyCost:0} năng lượng)";
                GUI.Label(new Rect(20, y, 500, 22), $"Quét rung [C]: {scan}");
                y += 24;
            }
            string prio = organs != null && organs.Priority != null ? $"  → tự vào {organs.Priority.DisplayName}" : "";
            GUI.Label(new Rect(20, y, 500, 22), $"EXP tiến hóa: {s.EvolutionExp:0}{prio}   (G: bảng cơ thể)");
            y += 24;
            if (caster != null && caster.Ability != null)
            {
                var a = caster.Ability;
                GUI.Label(new Rect(20, y, 400, 22), $"Dị Năng [Q]: {a.DisplayName} (bậc {a.Tier}, {a.EnergyCost:0} năng lượng)");
                y += 24;
                float cd = caster.CooldownRemaining;
                bool noEnergy = s.Energy < a.EnergyCost;
                string status = cd > 0f ? $"Hồi chiêu: {cd:0.0}s" : noEnergy ? "Thiếu năng lượng" : "Sẵn sàng";
                Color c = cd > 0f ? new Color(0.6f, 0.6f, 0.6f) : noEnergy ? new Color(0.9f, 0.4f, 0.3f) : new Color(0.4f, 0.9f, 0.4f);
                DrawBar(new Rect(20, y, 200, 20), caster.CooldownProgress, c, status);
                y += 28;
            }
            // Organs: full ones as text with level, buds as EXP progress bars.
            var all = organs != null ? organs.Organs.ToList() : new System.Collections.Generic.List<OrganSO>();
            var done = all.Where(x => organs.GetLevel(x) >= 1).Select(x => $"{x.DisplayName} Lv{organs.GetLevel(x)}").ToList();
            GUI.Label(new Rect(20, y, 500, 22), $"Cơ quan: {(done.Count > 0 ? string.Join(", ", done) : "chưa có")}");
            y += 24;
            foreach (var o in all.Where(x => organs.GetLevel(x) == 0))
            {
                organs.GetProgress(o, out float prog, out float cost);
                DrawBar(new Rect(20, y, 200, 20), organs.GetGrowth(o), new Color(0.75f, 0.5f, 1f),
                    $"Mầm {o.DisplayName}: {prog:0}/{cost:0} EXP");
                y += 28;
            }
            if (s.IsDead)
            {
                var dead = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 28, fontStyle = FontStyle.Bold };
                dead.fontSize = 18; dead.fontStyle = FontStyle.Normal;
                if (s.IsCorpse)
                {
                    dead.fontSize = 28; dead.fontStyle = FontStyle.Bold;
                    GUI.Label(new Rect(0, Screen.height / 2f - 40, Screen.width, 40), "Hết mạng", dead);
                    dead.fontSize = 18; dead.fontStyle = FontStyle.Normal;
                    GUI.Label(new Rect(0, Screen.height / 2f, Screen.width, 30), "Thân xác nằm lại, chỉ đồng đội mới cứu được (chưa có trong bản thử)", dead);
                    GUI.Label(new Rect(0, Screen.height / 2f + 28, Screen.width, 30), "Cơ quan vẫn nằm trên xác. Chơi lại thì chúng rơi ra tại xác, mỗi cái 50% bị mất.", dead);
                    GUI.Label(new Rect(0, Screen.height / 2f + 56, Screen.width, 30), "Y  Chơi lại từ đầu (tạm thời)", dead);
                }
                else
                {
                    dead.fontSize = 28; dead.fontStyle = FontStyle.Bold;
                    GUI.Label(new Rect(0, Screen.height / 2f - 40, Screen.width, 40), "Đã chết", dead);
                    dead.fontSize = 18; dead.fontStyle = FontStyle.Normal;
                    GUI.Label(new Rect(0, Screen.height / 2f, Screen.width, 30), $"Bấm R để hồi sinh (còn {s.Lives} mạng)", dead);
                    if (interactor != null && interactor.LastDeathKept + interactor.LastDeathLost > 0)
                        GUI.Label(new Rect(0, Screen.height / 2f + 28, Screen.width, 30),
                            $"Cơ quan rơi tại chỗ chết: còn {interactor.LastDeathKept} trên đất, mất {interactor.LastDeathLost}. Quay lại để nhặt.", dead);
                }
            }

            if (box == null) box = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, fontSize = 14, padding = new RectOffset(10, 10, 8, 8) };
            if (Time.time < toastUntil)
            {
                var big = new GUIStyle(box) { alignment = TextAnchor.MiddleCenter, fontSize = 20, wordWrap = true, richText = true };
                GUI.Box(new Rect(Screen.width / 2f - 220, Screen.height * 0.2f, 440, 90), toast, big);
            }

            DrawOrganPrompt();
            DrawBodyMenu();
            DrawSkillBar();
            DrawAim();

            if (showControls)
                GUI.Box(new Rect(Screen.width - 310, 20, 290, 640), Controls, box);
            else
                GUI.Label(new Rect(Screen.width - 160, 20, 150, 22), "H: hướng dẫn");
        }

        void DrawTemperature(SurvivalState s)
        {
            float limit = s.HeatLimit(stats.Config);
            bool hot = s.Temperature > limit;
            bool cold = s.Temperature < stats.Config.ComfortMin;
            Color c = hot ? new Color(1f, 0.25f, 0.15f)
                : Color.Lerp(new Color(0.4f, 0.6f, 1f), new Color(1f, 0.6f, 0.2f), Mathf.InverseLerp(0f, 50f, s.Temperature));
            string label = $"Nhiệt độ: {s.Temperature:0}°C (chịu nóng đến {limit:0}°C)";
            if (hot) label += "  QUÁ NÓNG!";
            else if (cold) label += "  QUÁ LẠNH!";
            else if (s.Ambient > limit) label += "  đang nóng lên";
            DrawBar(new Rect(20, 104, 200, 20), Mathf.InverseLerp(-10f, 60f, s.Temperature), c, label);
        }

        /// <summary>Body menu (G): spend EXP, choose the priority organ, detach. Uses the mouse.</summary>
        void DrawBodyMenu()
        {
            if (interactor == null || !interactor.BodyMenuOpen) return;
            var list = organs.OrganList;
            var rich = new GUIStyle(GUI.skin.label) { richText = true, fontSize = 14, wordWrap = true };
            float h = 120 + 58 * Mathf.Max(1, list.Count);
            foreach (var org in list) if (org.GrantedAbility != null) h += 30;
            var r = new Rect(Screen.width / 2f - 280, 80, 560, h);
            GUI.Box(r, GUIContent.none, box);
            GUILayout.BeginArea(new Rect(r.x + 12, r.y + 8, r.width - 24, r.height - 16));
            GUILayout.Label($"<b><size=18>Cơ thể</size></b>    EXP tiến hóa: <b>{stats.State.EvolutionExp:0}</b>", rich);
            GUILayout.Label("Đổ EXP: nuôi mầm thành cơ quan (Lv1), rồi nâng lên Lv2, Lv3. Ưu tiên: EXP mới nhận tự chảy vào cơ quan đó.", rich);
            if (list.Count == 0) GUILayout.Label("Chưa có cơ quan nào.", rich);
            foreach (var o in list)
            {
                int lv = organs.GetLevel(o);
                organs.GetProgress(o, out float prog, out float cost);
                string state = lv == 0 ? $"mầm, {prog:0}/{cost:0} EXP"
                    : organs.IsMaxed(o) ? $"Lv{lv} (tối đa)"
                    : $"Lv{lv}, {prog:0}/{cost:0} EXP lên Lv{lv + 1}";
                GUILayout.Label($"{OrganTiers.Tag(o.Tier)} <b>{o.DisplayName}</b> ({OrganInteractor.SlotName(o.Slot)}): {state}", rich);
                if (o.GrantedAbility != null)
                {
                    if (skills == null || !skills.IsUnlocked(o))
                        GUILayout.Label($"   Kỹ năng: <b>{o.GrantedAbility.DisplayName}</b> (mở khi Lv{o.AbilityUnlockLevel})", rich);
                    else
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label($"   Kỹ năng: <b>{o.GrantedAbility.DisplayName}</b>, đặt vào ô:", rich, GUILayout.Width(300));
                        int current = skills.SlotOf(o);
                        for (int s = 0; s < SkillBar.SlotCount; s++)
                        {
                            bool on = current == s;
                            if (GUILayout.Toggle(on, (s + 1).ToString(), GUI.skin.button, GUILayout.Width(32)) && !on)
                                skills.Assign(s, o);
                        }
                        GUILayout.EndHorizontal();
                    }
                }
                GUILayout.BeginHorizontal();
                GUI.enabled = !organs.IsMaxed(o) && stats.State.EvolutionExp > 0f;
                if (GUILayout.Button(lv == 0 ? "Đổ EXP (nuôi lớn)" : "Đổ EXP (lên cấp)", GUILayout.Width(170))) organs.Invest(o);
                GUI.enabled = !organs.IsMaxed(o);
                bool isPrio = organs.Priority == o;
                if (GUILayout.Button(isPrio ? "Đang ưu tiên (bỏ)" : "Đặt ưu tiên", GUILayout.Width(150)))
                    organs.Priority = isPrio ? null : o;
                GUI.enabled = true;
                if (GUILayout.Button("Tháo (rơi ra đất)", GUILayout.Width(150))) interactor.Drop(o);
                GUILayout.EndHorizontal();
            }
            if (organs.MaxOrgans > 0) GUILayout.Label($"Sức chứa: {list.Count}/{organs.MaxOrgans}", rich);
            if (GUILayout.Button("Đóng (G)")) interactor.CloseBodyMenu();
            GUILayout.EndArea();
        }

        /// <summary>The 5 skill slots at the bottom of the screen (keys 1-5).</summary>
        void DrawSkillBar()
        {
            if (skills == null) return;
            const float w = 130f, h = 54f, gap = 6f;
            float x0 = Screen.width / 2f - (SkillBar.SlotCount * w + (SkillBar.SlotCount - 1) * gap) / 2f;
            float y = Screen.height - h - 16f;
            var small = new GUIStyle(GUI.skin.label) { fontSize = 12, clipping = TextClipping.Clip };
            for (int i = 0; i < SkillBar.SlotCount; i++)
            {
                var r = new Rect(x0 + i * (w + gap), y, w, h);
                GUI.Box(r, GUIContent.none);
                var organ = skills.GetSlot(i);
                GUI.Label(new Rect(r.x + 6, r.y + 2, r.width - 12, 20), organ == null ? $"[{i + 1}] trống" : $"[{i + 1}] {organ.GrantedAbility.DisplayName}", small);
                if (organ == null) continue;

                float cd = skills.CooldownRemaining(i);
                bool noEnergy = stats.State.Energy < organ.GrantedAbility.EnergyCost;
                Color c = cd > 0f ? new Color(0.6f, 0.6f, 0.6f) : noEnergy ? new Color(0.9f, 0.4f, 0.3f) : new Color(0.4f, 0.9f, 0.4f);
                string status = cd > 0f ? $"{cd:0.0}s" : noEnergy ? "thiếu NL" : "sẵn sàng";
                var bar = new Rect(r.x + 6, r.y + 28, r.width - 12, 18);
                var prev = GUI.color; GUI.color = c;
                GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * Mathf.Clamp01(skills.CooldownProgress(i)), bar.height), Texture2D.whiteTexture);
                GUI.color = prev;
                GUI.Label(bar, status, small);
            }
        }

        /// <summary>Crosshair in the middle of the screen in aim mode, plus a small mode label.</summary>
        void DrawAim()
        {
            var label = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 12 };
            GUI.Label(new Rect(Screen.width / 2f - 150, 22, 300, 20),
                AimController.Aiming ? "Chế độ ngắm (Alt: về chuột)" : "Chế độ chuột (Alt: ngắm)", label);
            if (!AimController.Aiming) return;

            float cx = Screen.width / 2f, cy = Screen.height / 2f;
            const float len = 9f, gap = 5f, t = 2f;
            var prev = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, 0.9f);
            GUI.DrawTexture(new Rect(cx - gap - len, cy - t / 2f, len, t), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cx + gap, cy - t / 2f, len, t), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cx - t / 2f, cy - gap - len, t, len), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cx - t / 2f, cy + gap, t, len), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cx - 1.5f, cy - 1.5f, 3f, 3f), Texture2D.whiteTexture);
            GUI.color = prev;
        }

        void DrawOrganPrompt()
        {
            if (interactor == null || interactor.Current == null) return;
            interactor.GetPrompt(out string title, out string body, out string accept);
            var r = new Rect(Screen.width / 2f - 230, Screen.height - 320, 460, 190);
            var style = new GUIStyle(box) { alignment = TextAnchor.UpperCenter, fontSize = 15, wordWrap = true, richText = true };
            GUI.Box(r, $"<b><size=18>{title}</size></b>\n\n{body}", style);
            string absorb = $"[F] Hấp thụ (+{interactor.CurrentAbsorbExp:0} EXP)";
            string keys = accept != null ? $"[E] {accept}    {absorb}    [X] Bỏ qua" : $"{absorb}    [X] Bỏ qua";
            var keyStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 16, fontStyle = FontStyle.Bold };
            GUI.Label(new Rect(r.x, r.yMax - 34, r.width, 26), keys, keyStyle);
        }

        static void Bar(int row, string label, float value, float max, Color c)
        {
            DrawBar(new Rect(20, 20 + row * 28, 200, 20), max > 0 ? value / max : 0, c, $"{label}: {value:0}/{max:0}");
        }

        static void DrawBar(Rect r, float fill, Color c, string label)
        {
            GUI.Box(r, GUIContent.none);
            var prev = GUI.color; GUI.color = c;
            GUI.DrawTexture(new Rect(r.x + 2, r.y + 2, (r.width - 4) * Mathf.Clamp01(fill), r.height - 4), Texture2D.whiteTexture);
            GUI.color = prev;
            GUI.Label(new Rect(r.xMax + 8, r.y, 260, 20), label);
        }
    }
}
