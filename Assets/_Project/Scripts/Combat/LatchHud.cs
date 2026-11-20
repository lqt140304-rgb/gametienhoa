using UnityEngine;

namespace Evolve.Combat
{
    /// <summary>Draws the grip mini game while a <see cref="LatchSession"/> runs: the bar, green zone, golden weak point, the dot, the heavy-struggle warning and the faults left.</summary>
    public class LatchHud : MonoBehaviour
    {
        GUIStyle centered, warning;

        static void Fill(Rect r, Color c)
        {
            var old = GUI.color; GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = old;
        }

        void OnGUI()
        {
            var session = LatchSession.Active;
            if (session == null || session.Game == null) return;
            var g = session.Game;
            if (centered == null)
            {
                centered = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 16 };
                warning = new GUIStyle(centered) { fontSize = 24, fontStyle = FontStyle.Bold };
            }
            float w = Mathf.Min(560f, Screen.width * 0.6f), h = 26f, x = (Screen.width - w) * 0.5f, y = Screen.height - 130f;

            Fill(new Rect(x - 3, y - 3, w + 6, h + 6), new Color(0f, 0f, 0f, 0.55f));
            Fill(new Rect(x, y, w, h), new Color(0.15f, 0.15f, 0.18f, 0.9f));
            Fill(new Rect(x + w * (0.5f - g.ZoneWidth * 0.5f), y, w * g.ZoneWidth, h), new Color(0.25f, 0.8f, 0.4f, 0.85f));
            Fill(new Rect(x + w * (g.WeakCenter - LatchGame.WeakWidth * 0.5f), y, w * LatchGame.WeakWidth, h), new Color(1f, 0.85f, 0.2f, 0.95f));
            Fill(new Rect(x + w * g.Dot - 6f, y - 5f, 12f, h + 10f), Color.white);

            GUI.Label(new Rect(x, y - 52f, w, 26f), $"Ghì chặt · còn {Mathf.Max(0, g.MaxFaults - g.Faults)} lần sai", centered);
            GUI.Label(new Rect(x, y + h + 6f, w, 24f), "Giữ chấm trong vùng xanh: A/D hoặc kéo chuột · Space: buông", centered);
            if (g.Telegraphing)
            {
                var old = GUI.color; GUI.color = new Color(1f, 0.25f, 0.2f);
                GUI.Label(new Rect(x, y - 92f, w, 36f), "GIÃY MẠNH! BẤM SPACE", warning);
                GUI.color = old;
            }
        }
    }
}
