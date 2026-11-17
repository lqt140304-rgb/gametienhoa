using Evolve.Abilities;
using UnityEngine;

namespace Evolve.Survival
{
    /// <summary>Temporary IMGUI bars for prototyping. Replace with UI Toolkit later.</summary>
    public class SurvivalHUD : MonoBehaviour
    {
        [SerializeField] SurvivalStats stats;
        [SerializeField] bool showControls = true;

        AbilityCaster caster;
        GUIStyle box;

        const string Controls =
            "WASD  Trườn\n" +
            "Shift  Chạy nhanh (tốn năng lượng)\n" +
            "Space  Nảy (tốn năng lượng)\n" +
            "Q  Dùng Dị Năng\n" +
            "Giữ chuột phải  Xoay camera\n" +
            "Lăn chuột  Phóng to/thu nhỏ\n" +
            "V  Bật/tắt mắt (thử nghiệm)\n" +
            "H  Ẩn/hiện hướng dẫn\n\n" +
            "Chấm xanh nhấp nháy = thức ăn ở gần\n(giác quan rung, thay cho mắt)";

        void Start()
        {
            if (stats != null) caster = stats.GetComponent<AbilityCaster>();
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
            if (caster != null && caster.Ability != null)
                GUI.Label(new Rect(20, 104, 400, 22), $"Dị Năng: {caster.Ability.DisplayName} (bậc {caster.Ability.Tier})");
            if (s.IsDead) GUI.Label(new Rect(20, 128, 300, 22), "Đã chết");

            if (box == null) box = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, fontSize = 14, padding = new RectOffset(10, 10, 8, 8) };
            if (showControls)
                GUI.Box(new Rect(Screen.width - 300, 20, 280, 250), Controls, box);
            else
                GUI.Label(new Rect(Screen.width - 160, 20, 150, 22), "H: hướng dẫn");
        }

        static void Bar(int row, string label, float value, float max, Color c)
        {
            var r = new Rect(20, 20 + row * 28, 200, 20);
            GUI.Box(r, GUIContent.none);
            var prev = GUI.color; GUI.color = c;
            GUI.DrawTexture(new Rect(r.x + 2, r.y + 2, (r.width - 4) * (max > 0 ? value / max : 0), r.height - 4), Texture2D.whiteTexture);
            GUI.color = prev;
            GUI.Label(new Rect(r.xMax + 8, r.y, 200, 20), $"{label}: {value:0}/{max:0}");
        }
    }
}
