using System.Collections.Generic;
using Evolve.Creatures;
using Evolve.Player;
using Evolve.Survival;
using UnityEngine;

namespace Evolve.Maze
{
    /// <summary>
    /// Bảng God Menu đầy đủ cho Mê Cung, Kỳ Ngộ, 32 Sinh Vật 3D và Animation (bật/tắt cùng phím F1).
    /// Nằm hoàn toàn trong Scripts/Maze/, không sửa Scripts/Dev/ hay các thư mục lõi khác.
    /// </summary>
    public class MazeGodPanel : MonoBehaviour
    {
        const int WindowId = 0x60E;

        struct CreatureEntry
        {
            public int Wave;
            public string Id;
            public string DisplayName;
            public string CaveName;
            public string Role;
            public string PrimaryAction;

            public CreatureEntry(int wave, string id, string displayName, string caveName, string role, string primaryAction)
            {
                Wave = wave;
                Id = id;
                DisplayName = displayName;
                CaveName = caveName;
                Role = role;
                PrimaryAction = primaryAction;
            }
        }

        static readonly CreatureEntry[] AllCreatures =
        {
            // Wave 1 (12 loài)
            new CreatureEntry(1, "BoCanhCungNho", "Bọ Cánh Cứng Nhỏ", "Khởi Sinh", "M", "Hit"),
            new CreatureEntry(1, "OcHo", "Ốc Hồ", "Khởi Sinh", "M", "Action"),
            new CreatureEntry(1, "ThanLanBoHo", "Thằn Lằn Bờ Hồ", "Khởi Sinh", "S", "Attack"),
            new CreatureEntry(1, "CocHoGia", "Cóc Hồ Già", "Khởi Sinh", "C", "Action"),
            new CreatureEntry(1, "DomDomSacMau", "Đom Đóm Sắc Màu", "Vườn Quặng", "M", "Action"),
            new CreatureEntry(1, "ThoLa", "Thỏ Lá", "Vườn Quặng", "M", "Action"),
            new CreatureEntry(1, "BoGiapQuang", "Bọ Giáp Quặng", "Vườn Quặng", "S", "Hit"),
            new CreatureEntry(1, "RuaPhaLe", "Rùa Pha Lê", "Vườn Quặng", "C", "Action"),
            new CreatureEntry(1, "DoiAmThoa", "Dơi Âm Thoa", "Vực Nứt Tím", "M", "Action"),
            new CreatureEntry(1, "BoGaiTim", "Bọ Gai Tím", "Vực Nứt Tím", "M", "Action"),
            new CreatureEntry(1, "NhenTinhThe", "Nhện Tinh Thể", "Vực Nứt Tím", "S", "Attack"),
            new CreatureEntry(1, "BoCapThachAnh", "Bọ Cạp Thạch Anh", "Vực Nứt Tím", "C", "Attack"),

            // Wave 2 (8 mẫu)
            new CreatureEntry(2, "SenReu", "Sên Rêu", "Hang Rêu Thở", "M", "Action"),
            new CreatureEntry(2, "MuoiReu", "Muỗi Rêu", "Hang Rêu Thở", "M", "Action"),
            new CreatureEntry(2, "RetReu", "Rết Rêu", "Hang Rêu Thở", "S", "Attack"),
            new CreatureEntry(2, "ReuMe", "Rêu Mẹ", "Hang Rêu Thở", "C", "Action"),
            new CreatureEntry(2, "ChimNon", "Chim Non", "Tổ Bỏ Hoang", "M", "Hit"),
            new CreatureEntry(2, "TrungLa", "Trứng Lạ (Kỳ Ngộ)", "Tổ Bỏ Hoang", "KN", "Idle"),
            new CreatureEntry(2, "RanTromTrung", "Rắn Trộm Trứng", "Tổ Bỏ Hoang", "S", "Attack"),
            new CreatureEntry(2, "ChimHangMe", "Chim Hang Mẹ", "Tổ Bỏ Hoang", "C", "Attack"),

            // Wave 3 (12 loài)
            new CreatureEntry(3, "NamDiBo", "Nấm Đi Bộ", "Hang Nấm HQ", "M", "Hit"),
            new CreatureEntry(3, "BuomBaoTu", "Bướm Bào Tử", "Hang Nấm HQ", "M", "Action"),
            new CreatureEntry(3, "EchDocDom", "Ếch Độc Đốm", "Hang Nấm HQ", "S", "Attack"),
            new CreatureEntry(3, "NamKhongLo", "Nấm Khổng Lồ", "Hang Nấm HQ", "C", "Action"),
            new CreatureEntry(3, "OcLuuHuynh", "Ốc Lưu Huỳnh", "Suối Nước Nóng", "M", "Action"),
            new CreatureEntry(3, "CuaDaNong", "Cua Đá Nóng", "Suối Nước Nóng", "M", "Hit"),
            new CreatureEntry(3, "KyNhongLua", "Kỳ Nhông Lửa", "Suối Nước Nóng", "S", "Attack"),
            new CreatureEntry(3, "RanDungNham", "Rắn Dung Nham", "Suối Nước Nóng", "C", "Attack"),
            new CreatureEntry(3, "DeGio", "Dế Gió", "Khe Gió Hú", "M", "Hit"),
            new CreatureEntry(3, "ChuotGio", "Chuột Gió", "Khe Gió Hú", "M", "Hit"),
            new CreatureEntry(3, "ChonKhe", "Chồn Khe", "Khe Gió Hú", "S", "Attack"),
            new CreatureEntry(3, "CaoGio", "Cáo Gió", "Khe Gió Hú", "C", "Action"),
        };

        static readonly (ChamberTheme theme, string label)[] QuickThemes =
        {
            (ChamberTheme.BirthLake, "Khởi Sinh"),
            (ChamberTheme.LumaMushroomGrove, "Vườn Quặng"),
            (ChamberTheme.BreathingMoss, "Hang Rêu Thở"),
            (ChamberTheme.AbandonedNest, "Tổ Bỏ Hoang"),
            (ChamberTheme.CrystalVein, "Vực Nứt Tím"),
            (ChamberTheme.GlowMushroomCavern, "Hang Nấm HQ"),
            (ChamberTheme.HotSpringCavern, "Suối Nóng"),
            (ChamberTheme.HowlingWindGorge, "Khe Gió Hú"),
        };

        bool open;
        int activeTab; // 0 = Khoang & Kỳ ngộ, 1 = Sinh vật 3D & Animation
        int waveFilter; // 0 = Tất cả, 1..3 = Wave
        Rect window = new Rect(415, 60, 430, 660);
        Vector2 scroll;

        readonly List<GameObject> spawnedTestCreatures = new List<GameObject>();
        GameObject lastSpawned;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (!Application.isEditor && !Debug.isDebugBuild) return;
            if (FindAnyObjectByType<MazeGodPanel>() != null) return;
            var go = new GameObject("Maze God Panel (F10)");   // F1 is the shared god menu (GodMenu): two windows on one key sat on top of each other
            DontDestroyOnLoad(go);
            go.AddComponent<MazeGodPanel>();
        }

        void Update()
        {
            if (KeyPressed.F10()) open = !open;
        }

        void OnGUI()
        {
            if (!open) return;
            window = GUILayout.Window(WindowId, window, DrawWindow, "God Menu — Mê Cung & Sinh Vật 3D (F10)");
        }

        void DrawWindow(int id)
        {
            activeTab = GUILayout.Toolbar(activeTab, new[] { "Khoang & Kỳ Ngộ", "Sinh Vật 3D & Animation (32)" });
            GUILayout.Space(4);

            if (activeTab == 0) DrawChambersTab();
            else DrawCreaturesTab();

            GUI.DragWindow();
        }

        void DrawChambersTab()
        {
            var manager = FindAnyObjectByType<MazeWorldManager>();
            if (manager == null || manager.Graph == null)
            {
                GUILayout.Label("Chưa có Maze World trong scene.");
                GUILayout.Label("Hãy bấm nút dưới hoặc Evolve -> Build Maze World (Step 3).");
#if UNITY_EDITOR
                if (GUILayout.Button("Dựng Mê Cung ngay (Build Maze World)", GUILayout.Height(34)))
                    MazeWorldManager.InvokeEditorRebuild();
#endif
                return;
            }

            var player = FindLocalPlayerTransform();
            GUILayout.Label($"Đang ở: Khoang {manager.CurrentChamberId:00} — {manager.Graph.GetChamber(manager.CurrentChamberId).Name}");

            // Lưới nút dịch chuyển nhanh tới 8 hang đã dựng
            GUI.enabled = player != null;
            for (int row = 0; row < QuickThemes.Length; row += 4)
            {
                GUILayout.BeginHorizontal();
                for (int col = 0; col < 4 && (row + col) < QuickThemes.Length; col++)
                {
                    var qt = QuickThemes[row + col];
                    int targetId = FindChamberByTheme(manager, qt.theme);
                    GUI.enabled = player != null && targetId >= 0;
                    if (GUILayout.Button(qt.label, GUILayout.Height(24)))
                        manager.TeleportPlayerToChamber(player, targetId);
                }
                GUILayout.EndHorizontal();
            }
            GUI.enabled = true;

#if UNITY_EDITOR
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Dựng lại Mê Cung (Rebuild)"))
                MazeWorldManager.InvokeEditorRebuild();
            if (GUILayout.Button("Cập nhật 31 Prefab Quái"))
            {
                UnityEditor.EditorApplication.ExecuteMenuItem("Evolve/Build Wave 1 Cave Creatures & Prefabs");
                UnityEditor.EditorApplication.ExecuteMenuItem("Evolve/Build Wave 2 Nong Creatures & Prefabs");
                UnityEditor.EditorApplication.ExecuteMenuItem("Evolve/Build Wave 3 Giua Creatures & Prefabs");
            }
            GUILayout.EndHorizontal();
#endif

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

            GUILayout.Space(4);
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
                GUILayout.Label($"{marker}[{depthLabel}] {c.Id:00}. {c.Name}{encShort}", GUILayout.Width(300));
                GUI.enabled = player != null;
                if (GUILayout.Button("Tới ngay", GUILayout.Width(80)))
                    manager.TeleportPlayerToChamber(player, c.Id);
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();
        }

        void DrawCreaturesTab()
        {
            var player = FindLocalPlayerTransform();
            waveFilter = GUILayout.Toolbar(waveFilter, new[] { "Tất cả (32)", "Wave 1 (12)", "Wave 2 (8)", "Wave 3 (12)" });

            GUILayout.BeginHorizontal();
            Prey.Frozen = GUILayout.Toggle(Prey.Frozen, "Đóng băng Con Mồi", "Button", GUILayout.Height(24));
            Predator.Frozen = GUILayout.Toggle(Predator.Frozen, "Đóng băng Kẻ Săn", "Button", GUILayout.Height(24));
            Predator.Passive = GUILayout.Toggle(Predator.Passive, "Hòa bình", "Button", GUILayout.Height(24));
            GUILayout.EndHorizontal();

            // Thanh test animation trực tiếp trên con vừa triệu hồi
            GUILayout.BeginVertical("box");
            string targetLabel = lastSpawned != null ? lastSpawned.name : "(Chưa triệu hồi con thử)";
            GUILayout.Label($"Test Animation: {targetLabel}");
            GUILayout.BeginHorizontal();
            GUI.enabled = lastSpawned != null;
            if (GUILayout.Button("Idle")) PlayClipOnTarget(lastSpawned, "Idle");
            if (GUILayout.Button("Walk")) PlayClipOnTarget(lastSpawned, "Walk");
            if (GUILayout.Button("Action")) PlayClipOnTarget(lastSpawned, "Action");
            if (GUILayout.Button("Attack")) PlayClipOnTarget(lastSpawned, "Attack");
            if (GUILayout.Button("Hit")) PlayClipOnTarget(lastSpawned, "Hit");
            GUI.enabled = spawnedTestCreatures.Count > 0;
            if (GUILayout.Button("Xoá hết", GUILayout.Width(65))) ClearSpawnedCreatures();
            GUI.enabled = true;
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.Space(4);
            scroll = GUILayout.BeginScrollView(scroll);

            for (int i = 0; i < AllCreatures.Length; i++)
            {
                var c = AllCreatures[i];
                if (waveFilter != 0 && c.Wave != waveFilter) continue;

                GUILayout.BeginHorizontal();
                GUILayout.Label($"[W{c.Wave}|{c.Role}] {c.DisplayName} ({c.CaveName})", GUILayout.Width(295));
                GUI.enabled = player != null;
                if (GUILayout.Button("Triệu hồi", GUILayout.Width(90)))
                    SpawnCreatureInFront(player, c);
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();
        }

        void SpawnCreatureInFront(Transform player, CreatureEntry entry)
        {
            Vector3 fwd = player.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.001f) fwd = Vector3.forward;
            fwd.Normalize();

            Vector3 spawnPos = player.position + fwd * 4.0f + Vector3.up * 0.25f;
            Quaternion facePlayer = Quaternion.LookRotation(-fwd, Vector3.up);

            GameObject prefab = null;
#if UNITY_EDITOR
            prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/_Project/Prefabs/Wildlife/{entry.Id}.prefab");
            if (prefab == null)
                prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/_Project/Models/Wildlife/Creature_{entry.Id}.fbx");
#endif
            if (prefab == null)
            {
                Debug.LogWarning($"[MazeGodPanel] Không tìm thấy Prefab/FBX cho {entry.Id}");
                return;
            }

            var inst = Instantiate(prefab, spawnPos, facePlayer);
            inst.name = $"[Test] {entry.DisplayName} ({entry.Id})";
            spawnedTestCreatures.Add(inst);
            lastSpawned = inst;
        }

        static void PlayClipOnTarget(GameObject go, string clipKey)
        {
            if (go == null) return;
            var anim = go.GetComponentInChildren<Animator>();
            if (anim == null) return;

            // Tắt tạm CreatureAnimDriver nếu muốn xem clip thủ công không bị Speed=0 đè về Idle
            var driver = go.GetComponent<CreatureAnimDriver>();
            if (driver != null) driver.enabled = false;

            if (anim.runtimeAnimatorController != null)
            {
                var clips = anim.runtimeAnimatorController.animationClips;
                for (int i = 0; i < clips.Length; i++)
                {
                    if (clips[i] != null && clips[i].name.IndexOf(clipKey, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        anim.Play(clips[i].name, 0, 0f);
                        return;
                    }
                }
            }

            // Fallback dùng tham số Animator tiêu chuẩn
            if (clipKey == "Walk") anim.SetFloat("Speed", 1.5f);
            else if (clipKey == "Idle") anim.SetFloat("Speed", 0f);
            else anim.SetTrigger(clipKey);
        }

        void ClearSpawnedCreatures()
        {
            for (int i = 0; i < spawnedTestCreatures.Count; i++)
            {
                if (spawnedTestCreatures[i] != null)
                    Destroy(spawnedTestCreatures[i]);
            }
            spawnedTestCreatures.Clear();
            lastSpawned = null;
        }

        static int FindChamberByTheme(MazeWorldManager manager, ChamberTheme theme)
        {
            var chambers = manager.Graph.Chambers;
            for (int i = 0; i < chambers.Count; i++)
                if (chambers[i].Theme == theme) return chambers[i].Id;
            return -1;
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
