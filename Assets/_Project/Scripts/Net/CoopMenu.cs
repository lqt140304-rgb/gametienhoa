using System.Net;
using System.Net.Sockets;
using Evolve.Survival;
using UnityEngine;

namespace Evolve.Net
{
    /// <summary>
    /// F2 menu: name, address and port, Host / Join / Disconnect, plus a name and health label over every other player.
    /// While it is open the HUD treats it like any other menu (cursor is freed, skills and camera input are blocked).
    /// </summary>
    public class CoopMenu : MonoBehaviour
    {
        const int WindowId = 0xC00;

        CoopManager coop;
        bool open;
        Rect window = new Rect(20, 80, 380, 360);
        string portText = "7777";
        string localAddresses = "";

        void Awake() => coop = GetComponent<CoopManager>();

        void Update()
        {
            if (KeyPressed.F2())
            {
                open = !open;
                if (open) localAddresses = FindLocalAddresses();
            }
            SurvivalHUD.ExternalMenuOpen = open;
        }

        void OnDisable() => SurvivalHUD.ExternalMenuOpen = false;

        void OnGUI()
        {
            DrawStatusLine();
            DrawRemoteLabels();
            if (open) window = GUILayout.Window(WindowId, window, DrawWindow, "Co-op (F2 để đóng)");
        }

        void DrawStatusLine()
        {
            var style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 12 };
            string text = coop.IsActive ? "Co-op: " + coop.Status : "F2: Co-op";
            GUI.Label(new Rect(Screen.width / 2f - 200, 40, 400, 20), text, style);
        }

        void DrawWindow(int id)
        {
            GUILayout.Label(coop.Status);
            bool idle = !coop.IsActive;

            GUI.enabled = idle;
            Row("Tên", ref coop.PlayerName, 16);
            Row("Địa chỉ", ref coop.Address, 64);
            Row("Cổng", ref portText, 5);
            if (ushort.TryParse(portText, out ushort port)) coop.Port = port;

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Mở phòng (Host)")) coop.Host();
            if (GUILayout.Button("Vào phòng (Join)")) coop.Join();
            GUILayout.EndHorizontal();

            GUI.enabled = !idle;
            if (GUILayout.Button("Ngắt kết nối")) coop.Disconnect();
            GUI.enabled = true;

            GUILayout.Space(6);
            if (coop.IsActive)
            {
                GUILayout.Label("Người chơi khác:");
                int n = 0;
                foreach (var a in coop.Remotes)
                {
                    if (a == null) continue;
                    GUILayout.Label($"  {a.DisplayName}: máu {a.Health:0}/{a.MaxHealth:0}, mạng {a.Lives}{(a.Dead ? ", đã chết" : "")}");
                    n++;
                }
                if (n == 0) GUILayout.Label("  (chưa có ai)");
            }
            GUILayout.Space(4);
            GUILayout.Label("Cùng một máy: một bên Host, bên kia nhập 127.0.0.1.\nCùng mạng LAN: bên kia nhập địa chỉ của máy Host:\n" +
                            (string.IsNullOrEmpty(localAddresses) ? "(không tìm thấy)" : localAddresses) +
                            "\nBản này chỉ hiển thị người chơi khác; thế giới chưa chung.");
            GUI.DragWindow();
        }

        static void Row(string label, ref string value, int maxLength)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(70));
            value = GUILayout.TextField(value ?? "", maxLength);
            GUILayout.EndHorizontal();
        }

        void DrawRemoteLabels()
        {
            var cam = Camera.main;
            if (cam == null || !coop.IsActive) return;

            var style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 13, fontStyle = FontStyle.Bold };
            foreach (var a in coop.Remotes)
            {
                if (a == null) continue;
                Vector3 sp = cam.WorldToScreenPoint(a.transform.position + Vector3.up * 1.3f);
                if (sp.z <= 0f) continue; // behind the camera
                float x = sp.x, y = Screen.height - sp.y;

                GUI.Label(new Rect(x - 80, y - 24, 160, 20), a.DisplayName, style);
                var bar = new Rect(x - 30, y - 4, 60, 6);
                var prev = GUI.color;
                GUI.color = new Color(0f, 0f, 0f, 0.7f);
                GUI.DrawTexture(bar, Texture2D.whiteTexture);
                GUI.color = Color.Lerp(Color.red, Color.green, a.MaxHealth > 0f ? a.Health / a.MaxHealth : 0f);
                GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * Mathf.Clamp01(a.MaxHealth > 0f ? a.Health / a.MaxHealth : 0f), bar.height), Texture2D.whiteTexture);
                GUI.color = prev;
            }
        }

        static string FindLocalAddresses()
        {
            try
            {
                var sb = new System.Text.StringBuilder();
                foreach (var ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
                    if (ip.AddressFamily == AddressFamily.InterNetwork) sb.Append(ip).Append("  ");
                return sb.ToString().Trim();
            }
            catch { return ""; }
        }
    }
}
