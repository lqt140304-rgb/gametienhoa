using System.Collections.Generic;
using System.Linq;
using Evolve.Abilities;
using Evolve.Creatures;
using Evolve.Organs;
using Evolve.Player;
using Evolve.Survival;
using Evolve.Swallow;
using Evolve.Traits;
using Evolve.World;
using UnityEngine;

namespace Evolve.DevTools
{
    /// <summary>
    /// Testing cheats, toggled with F1. Added automatically when entering Play mode
    /// (Editor and development builds only), so no scene change is needed.
    /// </summary>
    public partial class GodMenu : MonoBehaviour
    {
        static readonly Vector3 DesertCenter = new Vector3(37.5f, 1f, 0f);
        const int WindowId = 0x60D; // any fixed id unique among IMGUI windows

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            // Static cheat flags survive between Play sessions when domain reload is off: reset them.
            Predator.Passive = false;
            Predator.Frozen = false;
            Prey.Frozen = false;
            Time.timeScale = 1f;

            if (!Application.isEditor && !Debug.isDebugBuild) return;
            if (FindAnyObjectByType<GodMenu>() != null) return;
            var go = new GameObject("God Menu (F1)");
            DontDestroyOnLoad(go);
            go.AddComponent<GodMenu>();
        }

        bool open;
        Rect window = new Rect(20, 20, 700, 760);
        Vector2 scroll;

        bool invulnerable, infiniteFood, infiniteEnergy, noCooldown, growInstantly = true;

        SurvivalStats player;
        OrganHost host;
        OrganInteractor interactor;
        SlimeController controller;
        AbilityCaster caster;
        SkillBar skillBar;

        void Update()
        {
            if (KeyPressed.F1()) open = !open;
            AimController.MouseUiOpen = open; // free the cursor while the menu is open
            FindPlayer();
            if (player == null) return;

            var s = player.State;
            s.Invulnerable = invulnerable;
            if (!s.IsDead)
            {
                if (infiniteFood) s.Hunger = s.MaxHunger;
                if (infiniteEnergy) s.Energy = s.MaxEnergy;
            }
            if (noCooldown)
            {
                if (caster != null) caster.ResetCooldown();
                if (skillBar != null) skillBar.ResetCooldowns();
            }
        }

        void FindPlayer()
        {
            if (player != null) return;
            foreach (var s in SurvivalStats.All)
            {
                var c = s.GetComponent<SlimeController>();
                if (c == null) continue;
                player = s;
                controller = c;
                host = s.GetComponent<OrganHost>();
                interactor = s.GetComponent<OrganInteractor>();
                caster = s.GetComponent<AbilityCaster>();
                skillBar = s.GetComponent<SkillBar>();
                break;
            }
        }

        static readonly string[] TabNames = { "Bản thân", "Kỹ năng", "Sinh vật", "Combat", "Thế giới", "Khác" };
        int tab;
        float userScale = 1f;
        bool scaleLoaded, showOrganList, showRewards, showDropTests;
        static GUIStyle wrapLabel, wrapToggle, headerStyle;

        /// <summary>The menu is drawn bigger on big screens (and with A- / A+ to taste), so it is readable.</summary>
        float UiScale
        {
            get
            {
                if (!scaleLoaded) { userScale = PlayerPrefs.GetFloat("GodMenuScale", 1f); scaleLoaded = true; }
                return Mathf.Clamp(Screen.height / 820f, 1f, 2.4f) * userScale;
            }
        }

        void SetScale(float v) { userScale = Mathf.Clamp(v, 0.7f, 1.8f); PlayerPrefs.SetFloat("GodMenuScale", userScale); }

        void OnGUI()
        {
            var oldMatrix = GUI.matrix;
            float scale = UiScale;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float sw = Screen.width / scale, sh = Screen.height / scale;
            if (!open) GUI.Label(new Rect(sw / 2f - 60, 4, 200, 22), "F1: Menu God");
            else
            {
                float w = Mathf.Min(700f, sw - 20f), h = sh - 40f;
                window.width = w; window.height = h;
                window.x = Mathf.Clamp(window.x, 0f, Mathf.Max(0f, sw - w));
                window.y = Mathf.Clamp(window.y, 0f, Mathf.Max(0f, sh - 60f));
                window = GUILayout.Window(WindowId, window, DrawWindow, "Menu God (F1 để đóng)", GUILayout.Width(w), GUILayout.Height(h));
            }
            GUI.matrix = oldMatrix;
        }

        static void EnsureStyles()
        {
            if (wrapLabel != null) return;
            wrapLabel = new GUIStyle(GUI.skin.label) { wordWrap = true };
            wrapToggle = new GUIStyle(GUI.skin.toggle) { wordWrap = true };
            headerStyle = new GUIStyle(GUI.skin.label) { richText = true, fontSize = 15, fontStyle = FontStyle.Bold };
        }

        void DrawWindow(int id)
        {
            EnsureStyles();
            if (player == null) { Say("Không tìm thấy slime trong scene."); GUI.DragWindow(); return; }
            GUILayout.BeginHorizontal();
            tab = GUILayout.Toolbar(tab, TabNames, GUILayout.Height(34));
            if (GUILayout.Button("A−", GUILayout.Width(40), GUILayout.Height(34))) SetScale(userScale - 0.1f);
            if (GUILayout.Button("A+", GUILayout.Width(40), GUILayout.Height(34))) SetScale(userScale + 0.1f);
            GUILayout.EndHorizontal();
            if (testNotice.Length > 0) Say("» " + testNotice);

            scroll = GUILayout.BeginScrollView(scroll);
            var slimeBody = player.GetComponent<Evolve.Swallow.SlimeBody>();
            var swallower = player.GetComponent<Evolve.Swallow.Swallower>();
            switch (tab)
            {
                case 0: DrawSelf(player.State, slimeBody); break;
                case 1: DrawSkills(player, slimeBody, swallower); break;
                case 2: DrawCreatures(); break;
                case 3: DrawCombat(player); break;
                case 4: DrawWorld(); break;
                default: DrawMisc(player, swallower); break;
            }
            GUILayout.EndScrollView();
            GUI.DragWindow();
        }

        // ---------- small layout helpers ----------

        static void Say(string text) => GUILayout.Label(text, wrapLabel);
        static bool Check(bool value, string text) => GUILayout.Toggle(value, " " + text, wrapToggle);
        static bool Fold(bool open, string text) => GUILayout.Toggle(open, (open ? "▼ " : "▶ ") + text, GUI.skin.button, GUILayout.Height(30));

        static void Header(string text)
        {
            GUILayout.Space(10);
            GUILayout.Label(text, headerStyle);
        }

        // ---------- tab: bản thân ----------

        void DrawSelf(SurvivalState s, Evolve.Swallow.SlimeBody slimeBody)
        {
            Header("Bản thân");
            invulnerable = Check(invulnerable, "Bất tử (không mất máu)");
            infiniteFood = Check(infiniteFood, "Vô hạn thức ăn");
            infiniteEnergy = Check(infiniteEnergy, "Vô hạn năng lượng");
            noCooldown = Check(noCooldown, "Dị Năng không hồi chiêu");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Hồi đầy chỉ số", GUILayout.Height(30))) { s.Health = s.MaxHealth; s.Hunger = s.MaxHunger; s.Energy = s.MaxEnergy; s.Temperature = s.Ambient; }
            if (GUILayout.Button("Tự sát", GUILayout.Height(30))) { bool inv = s.Invulnerable; s.Invulnerable = false; s.Damage(99999f); s.Invulnerable = invulnerable && inv; }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Mạng: {s.Lives}/{s.MaxLives}", GUILayout.Width(110));
            if (GUILayout.Button("+1 mạng")) s.Lives = Mathf.Min(s.Lives + 1, s.MaxLives);
            if (GUILayout.Button("Hồi đủ mạng")) s.ResetLives();
            GUILayout.EndHorizontal();
            if (s.IsDead && GUILayout.Button("Hồi sinh ngay (giữ mạng)")) { s.Lives = Mathf.Max(1, s.Lives); controller.Respawn(); }

            var traits = player.GetComponent<Evolve.Traits.PlayerTraits>();
            Say($"Máu {s.Health:0}/{s.MaxHealth:0} · năng lượng {s.Energy:0} · kháng nhiệt +{s.TotalHeatResistance:0.#}°C · Lõi {(traits != null ? traits.CoreId : "-")} ×{(traits != null ? traits.CoreBoost : 1f):0.##} · Khuyết Điểm {(traits != null ? string.Join(", ", traits.FlawIds) : "-")}");

            Header($"Kích cỡ slime (khối {(slimeBody != null ? slimeBody.Mass : 0f):0.#})");
            Say("Đổi để thử ai săn ai, ai chạy ai.");
            GUILayout.BeginHorizontal();
            foreach (float m in new[] { 3f, 10f, 20f, 40f, 80f })
                if (GUILayout.Button($"{m:0}", GUILayout.Height(28)) && slimeBody != null) slimeBody.SetMass(m);
            if (GUILayout.Button("+10 khối", GUILayout.Height(28)) && slimeBody != null) slimeBody.AddMass(10f);
            GUILayout.EndHorizontal();

            Header("Tốc độ game");
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{Time.timeScale:0.#}×", GUILayout.Width(60));
            foreach (float t in new[] { 0.25f, 0.5f, 1f, 2f, 4f })
                if (GUILayout.Button($"{t}×", GUILayout.Height(28))) Time.timeScale = t;
            GUILayout.EndHorizontal();
            if (GUILayout.Button("Về điểm xuất phát", GUILayout.Height(30))) controller.Teleport(controller.SpawnPoint);
        }

        // ---------- tab: thế giới ----------

        void DrawWorld()
        {
            Header("Dịch chuyển");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Tới hoang mạc", GUILayout.Height(30))) controller.Teleport(DesertCenter);
            if (GUILayout.Button("Về điểm xuất phát", GUILayout.Height(30))) controller.Teleport(controller.SpawnPoint);
            GUILayout.EndHorizontal();

            var mazeMgr = FindAnyObjectByType<Evolve.Maze.MazeWorldManager>();
            if (mazeMgr != null && mazeMgr.Graph != null)
            {
                GUILayout.BeginHorizontal();
                foreach (var c in mazeMgr.Graph.Chambers)
                {
                    if (c.Theme == Evolve.Maze.ChamberTheme.LumaMushroomGrove)
                    {
                        if (GUILayout.Button("Tới Rừng Nấm (Luma)", GUILayout.Height(30)))
                            mazeMgr.TeleportPlayerToChamber(controller.transform, c.Id);
                    }
                    else if (c.Theme == Evolve.Maze.ChamberTheme.CrystalVein)
                    {
                        if (GUILayout.Button("Tới Vực Thẳm Tinh Thể", GUILayout.Height(30)))
                            mazeMgr.TeleportPlayerToChamber(controller.transform, c.Id);
                    }
                }
                GUILayout.EndHorizontal();
            }
#if UNITY_EDITOR
            string rebuildLabel = mazeMgr != null && mazeMgr.Graph != null
                ? "Dựng lại Mê Cung (Rebuild Maze World)"
                : "Dựng Mê Cung ngay (Build Maze World)";
            if (GUILayout.Button(rebuildLabel, GUILayout.Height(30)))
                Evolve.Maze.MazeWorldManager.InvokeEditorRebuild();
#endif
            var vision = FindAnyObjectByType<MonochromeVision>();
            if (vision != null && GUILayout.Button(vision.Sight >= 1f ? "Tắt mắt (đen trắng)" : "Bật mắt (có màu)", GUILayout.Height(30)))
                vision.SetSight(vision.Sight >= 1f ? 0f : 1f);

            DrawEcology();
        }

        // ---------- tab: khác ----------

        void DrawMisc(SurvivalStats stats, Evolve.Swallow.Swallower swallower)
        {
            Header("Bệnh và Túi");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Nhiễm bệnh thử", GUILayout.Height(28))) { stats.State.Infect(DiseaseKind.RottenGel); stats.State.Infect(DiseaseKind.SporeBlur); }
            if (GUILayout.Button("Hết bệnh", GUILayout.Height(28))) testNotice = EncounterRewards.CureDiseases(stats.gameObject) ? "đã chữa" : "không được";
            GUILayout.EndHorizontal();
            if (GUILayout.Button("Bỏ 2 món vào Túi", GUILayout.Height(28)))
                testNotice = $"Vào Túi: {EncounterRewards.GiveItems(stats.gameObject, new[] { new EncounterItem { Label = "Mẩu xương cũ", Mass = 1f, Nutrition = 10f, Food = FoodKind.PreyMeat }, new EncounterItem { Label = "Hạt lạ", Mass = 0.5f, Nutrition = 5f, Food = FoodKind.PreyMeat } })} món";
            if (interactor != null && GUILayout.Button("Thử luật mất xác: mất hết cơ quan đang gắn", GUILayout.Height(28))) interactor.DropAllOnDeath();

            showDropTests = Fold(showDropTests, "Thử tỉ lệ rơi (kết quả ở Console)");
            if (showDropTests)
                foreach (var table in Resources.FindObjectsOfTypeAll<DropTableSO>())
                    if (GUILayout.Button($"Bốc thử 1000 lần: {table.name}")) RollTest(table);
        }

        static IEnumerable<OrganSO> AllOrgans() =>
            Resources.LoadAll<OrganSO>("CaveSkills").Concat(Resources.FindObjectsOfTypeAll<OrganSO>()).Distinct()   // cave skill genes are not in any scene yet
                .Where(o => !string.IsNullOrEmpty(o.DisplayName))
                .OrderBy(o => o.Tier).ThenBy(o => o.DisplayName);

        void RollTest(DropTableSO table)
        {
            const int n = 1000;
            var counts = new Dictionary<string, int>();
            int none = 0;
            for (int i = 0; i < n; i++)
            {
                var o = table.Roll(x => host != null && host.Has(x));
                if (o == null) { none++; continue; }
                string key = $"[{o.Tier}] {o.DisplayName}";
                counts[key] = counts.TryGetValue(key, out int c) ? c + 1 : 1;
            }
            var sb = new System.Text.StringBuilder($"{table.name}: bốc {n} lần (tính cả cơ quan đang có)\n");
            foreach (var kv in counts.OrderByDescending(k => k.Value))
                sb.AppendLine($"  {kv.Key}: {kv.Value} ({kv.Value * 100f / n:0.0}%)");
            sb.Append($"  Không rơi: {none} ({none * 100f / n:0.0}%)");
            Debug.Log(sb.ToString());
        }
    }
}
