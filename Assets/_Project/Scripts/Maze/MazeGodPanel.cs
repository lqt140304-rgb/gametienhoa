using Evolve.Player;
using Evolve.Survival;
using UnityEngine;

namespace Evolve.Maze
{
    /// <summary>
    /// Bảng dịch chuyển nhanh tới từng khoang mê cung & kích hoạt thử tình huống môi trường,
    /// tự động cài vào game và bật/tắt cùng phím F1 (nằm cạnh Menu God, không sửa file trong Survival/ hay Dev/).
    /// </summary>
    public class MazeGodPanel : MonoBehaviour
    {
        const int WindowId = 0x60E;
        bool open;
        Rect window = new Rect(415, 60, 390, 640);
        Vector2 scroll;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (!Application.isEditor && !Debug.isDebugBuild) return;
            if (FindAnyObjectByType<MazeGodPanel>() != null) return;
            var go = new GameObject("Maze God Panel (F1)");
            DontDestroyOnLoad(go);
            go.AddComponent<MazeGodPanel>();
        }

        void Update()
        {
            if (KeyPressed.F1()) open = !open;
        }

        void OnGUI()
        {
            if (!open) return;
            window = GUILayout.Window(WindowId, window, DrawWindow, "Dịch Chuyển Mê Cung (F1)");
        }

        void DrawWindow(int id)
        {
            var manager = FindAnyObjectByType<MazeWorldManager>();
            if (manager == null || manager.Graph == null)
            {
                GUILayout.Label("Chưa có Maze World trong scene.");
                GUILayout.Label("Hãy bấm nút dưới hoặc Evolve -> Build Maze World (Step 3).");
#if UNITY_EDITOR
                if (GUILayout.Button("⚡ Dựng Mê Cung ngay (Build Maze World)", GUILayout.Height(36)))
                {
                    var type = System.Type.GetType("Evolve.EditorTools.MazeBuilder, Assembly-CSharp-Editor");
                    var method = type?.GetMethod("Build", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    method?.Invoke(null, null);
                }
#endif
                GUI.DragWindow();
                return;
            }

            var player = FindLocalPlayerTransform();
            GUILayout.Label($"Đang ở: Khoang {manager.CurrentChamberId:00} — {manager.Graph.GetChamber(manager.CurrentChamberId).Name}");

            // Phím tắt nhanh cho 2 khoang mẫu Handcrafted
            GUILayout.Space(2);
            GUILayout.BeginHorizontal();
            GUI.enabled = player != null;
            foreach (var ch in manager.Graph.Chambers)
            {
                if (ch.Theme == ChamberTheme.LumaMushroomGrove)
                {
                    if (GUILayout.Button("🍄 Rừng Nấm (Luma)", GUILayout.Height(26))) manager.TeleportPlayerToChamber(player, ch.Id);
                }
                else if (ch.Theme == ChamberTheme.CrystalVein)
                {
                    if (GUILayout.Button("💎 Vực Thẳm Tinh Thể", GUILayout.Height(26))) manager.TeleportPlayerToChamber(player, ch.Id);
                }
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal();

            var curChamber = manager.CurrentChamber;
            var curEncounter = curChamber != null ? curChamber.GetComponentInChildren<ChamberEncounterZone>(true) : null;
            if (curEncounter != null && curEncounter.Kind != ChamberEncounterKind.None)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"Tình huống: {curEncounter.Kind} ({curEncounter.CurrentSnapshot.Phase})");
                if (GUILayout.Button("Kích hoạt ngay", GUILayout.Width(115)))
                    curEncounter.DebugTriggerNow();
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(6);
            scroll = GUILayout.BeginScrollView(scroll);

            var chambers = manager.Graph.Chambers;
            for (int i = 0; i < chambers.Count; i++)
            {
                var c = chambers[i];
                string depthLabel = c.Depth switch
                {
                    ChamberDepth.Nong => "Nông",
                    ChamberDepth.Giua => "Giữa",
                    _ => "Sâu"
                };
                string marker = c.Id == manager.CurrentChamberId ? "★ " : "";
                string encShort = c.Encounter == ChamberEncounterKind.None ? "" : $" [{c.Encounter}]";

                GUILayout.BeginHorizontal();
                GUILayout.Label($"{marker}[{depthLabel}] {c.Id:00}. {c.Name}{encShort}", GUILayout.Width(270));
                GUI.enabled = player != null;
                if (GUILayout.Button("Tới ngay", GUILayout.Width(80)))
                {
                    manager.TeleportPlayerToChamber(player, c.Id);
                }
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();
            GUI.DragWindow();
        }

        static Transform FindLocalPlayerTransform()
        {
            foreach (var s in SurvivalStats.All)
            {
                if (s != null && !s.IsRemote && s.GetComponent<SlimeController>() != null)
                    return s.transform;
            }
            return null;
        }
    }
}
