using UnityEngine;

namespace Evolve.Survival
{
    /// <summary>
    /// Tells the main assembly whether the co-op code (assembly Evolve.Net) is compiled in. That assembly only
    /// builds once the Netcode for GameObjects package is installed, so the rest of the game never depends on it.
    /// </summary>
    public static class NetBridge
    {
        public static bool Available;
    }

    /// <summary>
    /// If F2 is pressed while the Netcode package is missing, explain how to install it instead of doing nothing.
    /// Added to the scene automatically when entering Play mode.
    /// </summary>
    public class CoopHint : MonoBehaviour
    {
        float showUntil;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (FindAnyObjectByType<CoopHint>() != null) return;
            var go = new GameObject("Co-op Hint");
            DontDestroyOnLoad(go);
            go.AddComponent<CoopHint>();
        }

        void Update()
        {
            if (KeyPressed.F2() && !NetBridge.Available) showUntil = Time.unscaledTime + 10f;
        }

        void OnGUI()
        {
            if (NetBridge.Available || Time.unscaledTime > showUntil) return;
            var style = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter, fontSize = 15, wordWrap = true };
            GUI.Box(new Rect(Screen.width / 2f - 260, 80, 520, 110),
                "Co-op chưa dùng được: chưa cài gói Netcode for GameObjects.\n" +
                "Vào Window → Package Manager → Unity Registry, tìm \"Netcode for GameObjects\" rồi bấm Install.\n" +
                "Xem docs/COOP_SETUP.md.", style);
        }
    }
}
