using System.Collections.Generic;
using System.Linq;
using Evolve.Abilities;
using Evolve.Creatures;
using Evolve.Organs;
using Evolve.Player;
using Evolve.Survival;
using Evolve.World;
using UnityEngine;

namespace Evolve.DevTools
{
    /// <summary>
    /// Testing cheats, toggled with F1. Added automatically when entering Play mode
    /// (Editor and development builds only), so no scene change is needed.
    /// </summary>
    public class GodMenu : MonoBehaviour
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
        Rect window = new Rect(20, 60, 380, 640);
        Vector2 scroll;

        bool invulnerable, infiniteFood, infiniteEnergy, noCooldown, growInstantly = true;

        SurvivalStats player;
        OrganHost host;
        OrganInteractor interactor;
        SlimeController controller;
        AbilityCaster caster;

        void Update()
        {
            if (KeyPressed.F1()) open = !open;
            FindPlayer();
            if (player == null) return;

            var s = player.State;
            s.Invulnerable = invulnerable;
            if (!s.IsDead)
            {
                if (infiniteFood) s.Hunger = s.MaxHunger;
                if (infiniteEnergy) s.Energy = s.MaxEnergy;
            }
            if (noCooldown && caster != null) caster.ResetCooldown();
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
                break;
            }
        }

        void OnGUI()
        {
            if (!open)
            {
                GUI.Label(new Rect(Screen.width / 2f - 60, 4, 200, 22), "F1: Menu God");
                return;
            }
            window = GUILayout.Window(WindowId, window, DrawWindow, "Menu God (F1 để đóng)");
        }

        void DrawWindow(int id)
        {
            scroll = GUILayout.BeginScrollView(scroll);
            if (player == null)
            {
                GUILayout.Label("Không tìm thấy slime trong scene.");
                GUILayout.EndScrollView();
                GUI.DragWindow();
                return;
            }
            var s = player.State;

            Header("Bản thân");
            invulnerable = GUILayout.Toggle(invulnerable, " Bất tử (không mất máu)");
            infiniteFood = GUILayout.Toggle(infiniteFood, " Vô hạn thức ăn");
            infiniteEnergy = GUILayout.Toggle(infiniteEnergy, " Vô hạn năng lượng");
            noCooldown = GUILayout.Toggle(noCooldown, " Dị Năng không hồi chiêu");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Hồi đầy chỉ số")) { s.Health = s.MaxHealth; s.Hunger = s.MaxHunger; s.Energy = s.MaxEnergy; s.Temperature = s.Ambient; }
            if (GUILayout.Button("Tự sát")) { bool inv = s.Invulnerable; s.Invulnerable = false; s.Damage(99999f); s.Invulnerable = invulnerable && inv; }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Mạng: {s.Lives}/{s.MaxLives}", GUILayout.Width(90));
            if (GUILayout.Button("+1 mạng")) s.Lives = Mathf.Min(s.Lives + 1, s.MaxLives);
            if (GUILayout.Button("Hồi đủ mạng")) s.ResetLives();
            GUILayout.EndHorizontal();
            if (s.IsDead && GUILayout.Button("Hồi sinh ngay (giữ mạng)")) { s.Lives = Mathf.Max(1, s.Lives); controller.Respawn(); }

            Header("Cơ quan");
            growInstantly = GUILayout.Toggle(growInstantly, " Cho lớn đầy ngay khi nhận");
            foreach (var o in AllOrgans())
            {
                bool has = host != null && host.Has(o);
                GUILayout.BeginHorizontal();
                GUILayout.Label($"[{o.Tier}] {o.DisplayName} ({OrganInteractor.SlotName(o.Slot)})", GUILayout.Width(230));
                GUI.enabled = host != null && !has;
                if (GUILayout.Button(has ? "Đang có" : "Nhận"))
                {
                    if (host.GetInSlot(o.Slot) != null) host.Detach(o.Slot);
                    host.TryAttach(o);
                    if (growInstantly) host.DebugGrowAll();
                }
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Lớn đầy tất cả") && host != null) host.DebugGrowAll();
            if (GUILayout.Button("Tháo hết (biến mất)") && host != null)
                foreach (var o in host.OrganList) host.Detach(o.Slot);
            GUILayout.EndHorizontal();
            if (GUILayout.Button("Thử luật chết: rơi hết cơ quan tại chỗ") && interactor != null) interactor.DropAllOnDeath();

            Header("Sinh vật");
            Vector3 front = player.transform.position + player.transform.forward * 3f + Vector3.right * 2f;
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Tạo con mồi gần")) FindAnyObjectByType<PreySpawner>()?.SpawnAt(front);
            if (GUILayout.Button("Tạo kẻ săn mồi gần")) FindAnyObjectByType<PredatorSpawner>()?.SpawnAt(player.transform.position + Vector3.forward * 8f);
            GUILayout.EndHorizontal();
            if (GUILayout.Button($"Hạ hết kẻ săn mồi ({Predator.Active.Count}) (vẫn rơi đồ)"))
                foreach (var p in Predator.Active.ToList()) p.TakeDamage(99999f, player);
            Predator.Passive = GUILayout.Toggle(Predator.Passive, " Hòa bình: kẻ săn mồi không tấn công");
            bool frozen = GUILayout.Toggle(Predator.Frozen && Prey.Frozen, " Đóng băng mọi sinh vật");
            Predator.Frozen = frozen; Prey.Frozen = frozen;

            Header("Thế giới");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Tới hoang mạc")) controller.Teleport(DesertCenter);
            if (GUILayout.Button("Về điểm xuất phát")) controller.Teleport(controller.SpawnPoint);
            GUILayout.EndHorizontal();
            var vision = FindAnyObjectByType<MonochromeVision>();
            if (vision != null && GUILayout.Button(vision.Sight >= 1f ? "Tắt mắt (đen trắng)" : "Bật mắt (có màu)"))
                vision.SetSight(vision.Sight >= 1f ? 0f : 1f);
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Tốc độ game: {Time.timeScale:0.#}×", GUILayout.Width(120));
            foreach (float t in new[] { 0.5f, 1f, 2f, 4f })
                if (GUILayout.Button($"{t}×")) Time.timeScale = t;
            GUILayout.EndHorizontal();

            Header("Thử tỉ lệ rơi (kết quả ở Console)");
            foreach (var table in Resources.FindObjectsOfTypeAll<DropTableSO>())
                if (GUILayout.Button($"Bốc thử 1000 lần: {table.name}")) RollTest(table);

            GUILayout.EndScrollView();
            GUI.DragWindow();
        }

        static void Header(string text)
        {
            GUILayout.Space(8);
            GUILayout.Label($"<b>{text}</b>", new GUIStyle(GUI.skin.label) { richText = true, fontSize = 14 });
        }

        static IEnumerable<OrganSO> AllOrgans() =>
            Resources.FindObjectsOfTypeAll<OrganSO>()
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
