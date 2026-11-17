using System.Linq;
using Evolve.Abilities;
using Evolve.Organs;
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
        string toast;
        float toastUntil;
        GUIStyle box;

        const string Controls =
            "WASD  Trườn\n" +
            "Shift  Chạy nhanh (tốn thức ăn)\n" +
            "Space  Nảy (tốn năng lượng)\n" +
            "Q  Dùng Dị Năng\n" +
            "Giữ chuột phải  Xoay camera\n" +
            "Lăn chuột  Phóng to/thu nhỏ\n" +
            "V  Bật/tắt mắt (thử nghiệm)\n" +
            "H  Ẩn/hiện hướng dẫn\n\n" +
            "Đứng yên: không đói, hồi năng lượng\n" +
            "Chấm xanh = thức ăn, chấm đỏ = con mồi\n(giác quan rung, thay cho mắt)\n\n" +
            "Bắt con mồi (chạm vào) để nhận cơ quan.\nChạm cơ quan rơi ra: E chọn, X bỏ qua.\nG  Bảng cơ thể (tháo cơ quan bằng 1, 2…)\nCon mồi chạy mệt sẽ phải dừng nghỉ.\nCơ quan lớn bằng thức ăn: ăn đủ thì tiến hóa\n(một nửa bữa ăn nuôi cơ quan).";

        void Start()
        {
            if (stats == null) return;
            caster = stats.GetComponent<AbilityCaster>();
            organs = stats.GetComponent<OrganHost>();
            interactor = stats.GetComponent<OrganInteractor>();
            if (organs != null)
            {
                organs.Attached += o => Toast($"Mầm {o.DisplayName}: ăn thêm {o.GrowthFoodRequired:0} thức ăn để phát triển");
                organs.Completed += o => Toast($"Tiến hóa: {o.DisplayName}!\n{o.Description}");
                organs.Removed += o => Toast($"Đã tháo {o.DisplayName}\n(rơi ra đất, nhặt lại phải nuôi từ đầu)");
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
            float y = 104;
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
            // Organs: finished ones as text, growing ones as food progress bars.
            var all = organs != null ? organs.Organs.ToList() : new System.Collections.Generic.List<OrganSO>();
            var done = all.Where(x => organs.GetGrowth(x) >= 1f).Select(x => x.DisplayName).ToList();
            GUI.Label(new Rect(20, y, 500, 22), $"Cơ quan: {(done.Count > 0 ? string.Join(", ", done) : "chưa có")}");
            y += 24;
            foreach (var o in all.Where(x => organs.GetGrowth(x) < 1f))
            {
                DrawBar(new Rect(20, y, 200, 20), organs.GetGrowth(o), new Color(0.75f, 0.5f, 1f),
                    $"{o.DisplayName}: {organs.GetFed(o):0}/{o.GrowthFoodRequired:0} thức ăn");
                y += 28;
            }
            if (s.IsDead) GUI.Label(new Rect(20, y, 300, 22), "Đã chết");

            if (box == null) box = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, fontSize = 14, padding = new RectOffset(10, 10, 8, 8) };
            if (Time.time < toastUntil)
            {
                var big = new GUIStyle(box) { alignment = TextAnchor.MiddleCenter, fontSize = 20, wordWrap = true };
                GUI.Box(new Rect(Screen.width / 2f - 220, Screen.height * 0.2f, 440, 90), toast, big);
            }

            DrawOrganPrompt();
            DrawBodyMenu();

            if (showControls)
                GUI.Box(new Rect(Screen.width - 310, 20, 290, 420), Controls, box);
            else
                GUI.Label(new Rect(Screen.width - 160, 20, 150, 22), "H: hướng dẫn");
        }

        void DrawBodyMenu()
        {
            if (interactor == null || !interactor.BodyMenuOpen) return;
            var list = organs.OrganList;
            string text = "<b><size=18>Cơ thể</size></b>\n\n";
            if (list.Count == 0) text += "Chưa có cơ quan nào.\n";
            for (int i = 0; i < list.Count; i++)
            {
                var o = list[i];
                float g = organs.GetGrowth(o);
                string state = g >= 1f ? "hoàn chỉnh" : $"{organs.GetFed(o):0}/{o.GrowthFoodRequired:0} thức ăn";
                text += $"[{i + 1}] Tháo {o.DisplayName}  ({OrganInteractor.SlotName(o.Slot)}, {state})\n";
            }
            if (organs.MaxOrgans > 0) text += $"\nSức chứa: {list.Count}/{organs.MaxOrgans}";
            text += "\n[G] Đóng";
            var style = new GUIStyle(box) { alignment = TextAnchor.UpperLeft, fontSize = 15, wordWrap = true, richText = true };
            GUI.Box(new Rect(Screen.width / 2f - 230, 80, 460, 70 + 24 * Mathf.Max(1, list.Count) + 40), text, style);
        }

        void DrawOrganPrompt()
        {
            if (interactor == null || interactor.Current == null) return;
            interactor.GetPrompt(out string title, out string body, out string accept);
            var r = new Rect(Screen.width / 2f - 230, Screen.height - 230, 460, 190);
            var style = new GUIStyle(box) { alignment = TextAnchor.UpperCenter, fontSize = 15, wordWrap = true, richText = true };
            GUI.Box(r, $"<b><size=18>{title}</size></b>\n\n{body}", style);
            string keys = accept != null ? $"[E] {accept}     [X] Bỏ qua" : "[X] Bỏ qua";
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
