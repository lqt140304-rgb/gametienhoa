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
            "Bắt con mồi (chạm vào) để nhận cơ quan.\nCon mồi chạy mệt sẽ phải dừng nghỉ.\nCơ quan lớn dần, tốn thức ăn.";

        void Start()
        {
            if (stats == null) return;
            caster = stats.GetComponent<AbilityCaster>();
            organs = stats.GetComponent<OrganHost>();
            if (organs != null)
            {
                organs.Attached += o => Toast($"Mầm {o.DisplayName} bắt đầu phát triển\n(tốn thức ăn, cần đói > 20)");
                organs.Completed += o => Toast($"Tiến hóa: {o.DisplayName}!\n{o.Description}");
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
            string organList = organs != null && organs.Organs.Any()
                ? string.Join(", ", organs.Organs.Select(o =>
                {
                    float g = organs.GetGrowth(o);
                    return g < 1f ? $"{o.DisplayName} ({g * 100f:0}%)" : o.DisplayName;
                })) : "chưa có";
            if (organs != null && organs.GrowthStarved) organList += "  [đói quá, ngừng phát triển]";
            GUI.Label(new Rect(20, y, 500, 22), $"Cơ quan: {organList}");
            if (s.IsDead) GUI.Label(new Rect(20, y + 24, 300, 22), "Đã chết");

            if (box == null) box = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, fontSize = 14, padding = new RectOffset(10, 10, 8, 8) };
            if (Time.time < toastUntil)
            {
                var big = new GUIStyle(box) { alignment = TextAnchor.MiddleCenter, fontSize = 20, wordWrap = true };
                GUI.Box(new Rect(Screen.width / 2f - 220, Screen.height * 0.2f, 440, 90), toast, big);
            }

            if (showControls)
                GUI.Box(new Rect(Screen.width - 310, 20, 290, 360), Controls, box);
            else
                GUI.Label(new Rect(Screen.width - 160, 20, 150, 22), "H: hướng dẫn");
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
