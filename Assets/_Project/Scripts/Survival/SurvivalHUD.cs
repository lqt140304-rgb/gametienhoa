using Evolve.Abilities;
using Evolve.Organs;
using Evolve.Player;
using UnityEngine;

namespace Evolve.Survival
{
    /// <summary>Minimal survival HUD and contextual gameplay menus.</summary>
    [DefaultExecutionOrder(-100)]
    public partial class SurvivalHUD : MonoBehaviour
    {
        [SerializeField] SurvivalStats stats;

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
            "Chuột: kỹ năng bắn/lao theo hướng con trỏ\nAlt  Chế độ ngắm: hồng tâm giữa màn hình,\n    camera lệch qua vai, kỹ năng theo hồng tâm\nF5  Góc nhìn thứ nhất (có hồng tâm)\n" +
            "Giữ chuột phải  Xoay camera\n" +
            "Lăn chuột  Phóng to/thu nhỏ\n" +
            "C  Quét rung: ánh sáng lan ra, thấy thức ăn\n    và sinh vật xung quanh trong chốc lát\nV  Bật/tắt mắt (thử nghiệm)\n" +
            "H  Ẩn/hiện hướng dẫn\nF1  Menu God (thử nghiệm)\n\n" +
            "Đứng yên: không đói, hồi năng lượng nhanh,\nkẻ săn mồi khó phát hiện. Đi vẫn hồi chậm.\n" +
            "Khi quét: xanh = thức ăn, đỏ = con mồi,\ntím = kẻ săn mồi\n\n" +
            "Hoang mạc (phía đông) rất nóng.\nGai độc / lao vào (Q) để hạ kẻ săn mồi.\nR  Hồi sinh khi chết (3 mạng; chết thì cơ quan\nrơi tại chỗ, mỗi cái 50% bị mất)\n\n" +
            "Bắt con mồi (chạm vào) để nhận cơ quan.\nChạm cơ quan rơi ra: E lắp, F hấp thụ, X bỏ qua.\n\n" +
            "EXP tiến hóa: ăn được ít, hấp thụ cơ quan được\nnhiều. G  Bảng cơ thể: đổ EXP cho cơ quan\n(mầm → Lv1 → Lv3), chọn ưu tiên, tháo.";

        void Start()
        {
            if (stats == null) return;
            RegisterHud();
            caster = stats.GetComponent<AbilityCaster>();
            organs = stats.GetComponent<OrganHost>();
            interactor = stats.GetComponent<OrganInteractor>();
            skills = stats.GetComponent<SkillBar>();
            // Upgrade existing scenes in memory; no scene rebuild or saved asset changes.
            if (skills == null && organs != null) skills = stats.gameObject.AddComponent<SkillBar>();
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

        void OnGUI()
        {
            if (stats == null) return;
            var s = stats.State;
            DrawGameplayHud();
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

            if (!MenuOpen) DrawOrganPrompt();

            DrawAim();
            DrawOverlayMenu();
        }

        /// <summary>Crosshair in the middle of the screen in aim mode, plus a small mode label.</summary>
        void DrawAim()
        {
            var label = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 12 };
            GUI.Label(new Rect(Screen.width / 2f - 150, 22, 300, 20),
                AimController.FirstPerson ? "Góc nhìn thứ nhất (F5: góc ngoài)"
                : AimController.Aiming ? "Chế độ ngắm (Alt: về chuột, F5: góc nhìn thứ nhất)"
                : "Chế độ chuột (Alt: ngắm, F5: góc nhìn thứ nhất)", label);
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

    }
}
