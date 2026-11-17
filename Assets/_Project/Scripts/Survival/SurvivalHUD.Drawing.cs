using System.Collections.Generic;
using Evolve.Abilities;
using Evolve.Organs;
using Evolve.World;
using UnityEngine;

namespace Evolve.Survival
{
    public partial class SurvivalHUD
    {
        const string FollowPreference = "Evolve.HudFollowPlayer";
        static readonly Dictionary<GameObject, SurvivalHUD> owners = new Dictionary<GameObject, SurvivalHUD>();
        HudMenu menu;
        bool followPlayer;
        int selectedSkill;
        Vector2 organScroll;
        GUIStyle text, small, heading, action;
        Texture2D circle;
        readonly Color ink = new Color(.86f, .91f, .81f);
        readonly Color muted = new Color(.65f, .75f, .61f);
        readonly Color surface = new Color(.055f, .12f, .095f, .96f);
        Vector2 canvas;

        public bool MenuOpen => menu != HudMenu.None;
        public static bool HasFor(GameObject owner) => owner != null && owners.TryGetValue(owner, out var hud) && hud != null && hud.isActiveAndEnabled;
        public static bool BlocksInput(GameObject owner) => HasFor(owner) && owners[owner].MenuOpen;

        void RegisterHud()
        {
            owners[stats.gameObject] = this;
            followPlayer = PlayerPrefs.GetInt(FollowPreference, 0) == 1;
        }

        void OnDisable()
        {
            if (stats != null && owners.TryGetValue(stats.gameObject, out var hud) && hud == this)
                owners.Remove(stats.gameObject);
            menu = HudMenu.None;
            if (interactor != null) interactor.CloseBodyMenu();
        }

        void OnEnable() { if (stats != null) RegisterHud(); }
        void OnDestroy() { if (circle != null) Destroy(circle); }

        void Update()
        {
            if (stats == null) return;
            if (stats.State.IsDead) { SetMenu(HudMenu.None); return; }
            if (KeyPressed.Escape()) SetMenu(HudMenu.None);
            else if (KeyPressed.K()) SetMenu(HudLayout.Toggle(menu, HudMenu.Skills));
            else if (KeyPressed.G()) SetMenu(HudLayout.Toggle(menu, HudMenu.Organs));
            else if (KeyPressed.H()) SetMenu(HudLayout.Toggle(menu, HudMenu.Help));
        }

        void SetMenu(HudMenu next)
        {
            if (stats != null && stats.State.IsDead) next = HudMenu.None;
            menu = next;
            if (interactor == null) return;
            if (next == HudMenu.Organs) interactor.OpenBodyMenu();
            else interactor.CloseBodyMenu();
        }

        void EnsureStyles()
        {
            if (text != null) return;
            text = new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true };
            text.normal.textColor = ink;
            small = new GUIStyle(text) { fontSize = 12 };
            small.normal.textColor = muted;
            heading = new GUIStyle(text) { fontSize = 21, fontStyle = FontStyle.Bold };
            action = new GUIStyle(GUI.skin.button) { fontSize = 13, padding = new RectOffset(8, 8, 7, 7) };
            circle = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            var pixels = new Color[64 * 64];
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(32, 32));
                    pixels[y * 64 + x] = new Color(1, 1, 1, Mathf.Clamp01(32 - d));
                }
            circle.SetPixels(pixels); circle.Apply();
        }

        float UiScale => Mathf.Max(.1f, Mathf.Min(1.4f, Mathf.Min(Screen.width / 960f, Screen.height / 600f)));

        void DrawGameplayHud()
        {
            EnsureStyles();
            var old = GUI.matrix;
            float scale = UiScale;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * scale);
            canvas = new Vector2(Screen.width / scale, Screen.height / scale);
            var s = stats.State;
            var anchor = PlayerAnchor(scale);
            bool floating = followPlayer && anchor.HasValue;
            float width = floating ? 132 : 230;
            float x = floating ? HudLayout.ClampCenter(anchor.Value.x, width, canvas.x) - width / 2 : 24;
            float y = floating ? HudLayout.ClampCenter(anchor.Value.y + 55, 92, canvas.y) - 46 : canvas.y - 162;
            StatLine(new Rect(x, y, width, floating ? 7 : 32), "Máu", s.Health, s.MaxHealth, new Color(.85f, .56f, .47f), !floating);
            StatLine(new Rect(x, y + (floating ? 12 : 39), width, floating ? 7 : 32), "Năng lượng", s.Energy, s.MaxEnergy, new Color(.69f, .83f, .61f), !floating);
            if (!floating || s.Hunger <= s.MaxHunger * .25f)
                StatLine(new Rect(x, y + (floating ? 24 : 78), width, floating ? 7 : 32), "Độ no", s.Hunger, s.MaxHunger, new Color(.84f, .73f, .46f), !floating);
            bool unsafeTemp = s.Temperature > s.HeatLimit(stats.Config) || s.Temperature < stats.Config.ComfortMin;
            GUI.Label(new Rect(x, y + (floating ? 35 : 117), width, 28),
                $"{s.Temperature:0}°C{(unsafeTemp ? " !" : "")}    •    {s.Lives}/{s.MaxLives} mạng", small);

            var right = new Rect(canvas.x - 254, canvas.y - 130, 230, 110);
            GUI.Label(new Rect(right.x, right.y, right.width, 25), $"{s.EvolutionExp:0} EXP", text);
            var sense = stats.GetComponent<VibrationSense>();
            float skillX = canvas.x / 2 - 187;
            float skillY = canvas.y - 86;
            var active = caster != null ? caster.Ability : null;
            DrawSkillChip(new Rect(skillX, skillY, 50, 58), "Q", active != null ? active.DisplayName : null,
                caster != null ? caster.CooldownRemaining : 0, active != null ? active.EnergyCost : 0);
            for (int slot = 0; slot < SkillBar.SlotCount; slot++)
            {
                var organ = skills != null ? skills.GetSlot(slot) : null;
                var ability = organ != null ? organ.GrantedAbility : null;
                DrawSkillChip(new Rect(skillX + (slot + 1) * 54, skillY, 50, 58), (slot + 1).ToString(),
                    ability != null ? ability.DisplayName : null, skills != null ? skills.CooldownRemaining(slot) : 0,
                    ability != null ? ability.EnergyCost : 0);
            }
            DrawSkillChip(new Rect(skillX + 324, skillY, 50, 58), "C", sense != null ? "Cảm nhận rung" : null,
                sense != null ? sense.CooldownRemaining : 0, sense != null ? sense.EnergyCost : 0);
            if (GUI.Button(new Rect(right.x, right.y + 55, 108, 32), "K · Kỹ năng", action)) SetMenu(HudLayout.Toggle(menu, HudMenu.Skills));
            if (GUI.Button(new Rect(right.x + 116, right.y + 55, 114, 32), "G · Cơ quan", action)) SetMenu(HudLayout.Toggle(menu, HudMenu.Organs));
            if (GUI.Button(new Rect(canvas.x - 136, 18, 112, 28), "Giao diện", action)) SetMenu(HudLayout.Toggle(menu, HudMenu.Settings));
            if (!string.IsNullOrEmpty(GUI.tooltip))
                GUI.Label(new Rect(canvas.x / 2 - 187, skillY - 28, 374, 24), GUI.tooltip, small);
            GUI.matrix = old;
        }

        void DrawSkillChip(Rect chip, string key, string name, float cooldown, float cost)
        {
            Paint(chip, name != null ? surface : new Color(.055f, .12f, .095f, .3f));
            var centered = new GUIStyle(name != null ? text : small) { alignment = TextAnchor.MiddleCenter };
            GUI.Label(new Rect(chip.x, chip.y + 3, chip.width, 24), new GUIContent(key, name ?? "Ô trống"), centered);
            var status = new GUIStyle(small) { alignment = TextAnchor.MiddleCenter, wordWrap = false, clipping = TextClipping.Clip };
            GUI.Label(new Rect(chip.x + 2, chip.y + 29, chip.width - 4, 22),
                new GUIContent(name == null ? "—" : cooldown > 0 ? $"{cooldown:0.0}s" : stats.State.Energy < cost ? "NL!" : "Sẵn", name ?? "Ô trống"), status);
        }

        string Ready(float cooldown, float cost) => cooldown > 0 ? $"{cooldown:0.0}s" : stats.State.Energy < cost ? "thiếu NL" : "sẵn sàng";

        Vector2? PlayerAnchor(float scale)
        {
            var cam = Camera.main;
            if (cam == null) return null;
            Vector3 p = cam.WorldToScreenPoint(stats.transform.position);
            if (p.z <= 0 || p.x < 0 || p.x > Screen.width || p.y < 0 || p.y > Screen.height) return null;
            return new Vector2(p.x / scale, (Screen.height - p.y) / scale);
        }

        void Paint(Rect r, Color color, Texture texture = null)
        {
            var previous = GUI.color;
            GUI.color = color; GUI.DrawTexture(r, texture != null ? texture : Texture2D.whiteTexture);
            GUI.color = previous;
        }

        void StatLine(Rect r, string name, float value, float max, Color color, bool labels)
        {
            if (labels)
            {
                GUI.Label(new Rect(r.x, r.y, r.width, 23), name, small);
                var number = new GUIStyle(small) { alignment = TextAnchor.UpperRight };
                GUI.Label(new Rect(r.x, r.y, r.width, 23), $"{value:0}/{max:0}", number);
            }
            var track = new Rect(r.x, r.y + (labels ? 24 : 0), r.width, 4);
            Paint(track, new Color(.04f, .09f, .07f, .8f));
            track.width *= max > 0 ? Mathf.Clamp01(value / max) : 0;
            Paint(track, color);
        }

        void DrawOverlayMenu()
        {
            if (!MenuOpen) return;
            var old = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * UiScale);
            if (menu == HudMenu.Skills) DrawSkillRing();
            else if (menu == HudMenu.Organs) DrawOrgansPanel();
            else if (menu == HudMenu.Settings) DrawSettings();
            else if (menu == HudMenu.Help)
            {
                var r = new Rect(24, 60, 370, Mathf.Min(600, canvas.y - 90));
                Paint(r, surface);
                GUILayout.BeginArea(new Rect(r.x + 16, r.y + 12, r.width - 32, r.height - 24));
                if (GUILayout.Button("Đóng · H / Esc", action)) SetMenu(HudMenu.None);
                organScroll = GUILayout.BeginScrollView(organScroll);
                GUILayout.Label(Controls, text);
                GUILayout.EndScrollView(); GUILayout.EndArea();
            }
            GUI.matrix = old;
        }

        void DrawSettings()
        {
            var r = new Rect(canvas.x - 334, 56, 310, 190);
            Paint(r, surface);
            GUILayout.BeginArea(new Rect(r.x + 16, r.y + 12, r.width - 32, r.height - 24));
            GUILayout.Label("Vị trí chỉ số", heading);
            bool fixedMode = GUILayout.Toggle(!followPlayer, "Cố định ở hai góc dưới");
            bool followMode = GUILayout.Toggle(followPlayer, "Đi theo nhân vật");
            bool next = followPlayer;
            if (fixedMode && followPlayer) next = false;
            if (followMode && !followPlayer) next = true;
            if (next != followPlayer)
            {
                followPlayer = next;
                PlayerPrefs.SetInt(FollowPreference, next ? 1 : 0); PlayerPrefs.Save();
            }
            if (GUILayout.Button("Hướng dẫn · H", action)) SetMenu(HudMenu.Help);
            if (GUILayout.Button("Đóng · Esc", action)) SetMenu(HudMenu.None);
            GUILayout.EndArea();
        }

        void DrawSkillRing()
        {
            Vector2 at = PlayerAnchor(UiScale) ?? canvas / 2;
            at.x = HudLayout.ClampCenter(at.x, 350, canvas.x);
            at.y = HudLayout.ClampCenter(at.y - 25, 510, canvas.y);
            for (int i = 0; i < 7; i++)
            {
                float angle = (-90 + i * 360f / 7) * Mathf.Deg2Rad;
                Vector2 p = at + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 120;
                Rect node = new Rect(p.x - 29, p.y - 29, 58, 58);
                bool available = i == 0 ? caster != null && caster.Ability != null
                    : i == 6 ? stats.GetComponent<VibrationSense>() != null
                    : skills != null && skills.GetSlot(i - 1) != null;
                Paint(node, selectedSkill == i ? new Color(.63f, .76f, .49f, .95f) : surface, circle);
                var label = new GUIStyle(text) { alignment = TextAnchor.MiddleCenter, fontSize = 19 };
                GUI.Label(node, i == 0 ? "Q" : i == 6 ? "C" : i.ToString(), label);
                if (!available) GUI.Label(new Rect(node.x - 6, node.yMax, 70, 18), "Chưa mở", small);
                else if (i > 0 && i < 6 && skills.CooldownRemaining(i - 1) > 0)
                    GUI.Label(new Rect(node.x, node.yMax, 70, 18), $"{skills.CooldownRemaining(i - 1):0.0}s", small);
                if (GUI.Button(node, new GUIContent("", available ? "Xem kỹ năng" : "Chưa có kỹ năng"), GUIStyle.none)) selectedSkill = i;
            }
            var info = new Rect(at.x - 170, at.y + 157, 340, 45);
            Paint(new Rect(info.x - 8, info.y - 4, info.width + 16, 90), surface);
            string title, detail;
            if (selectedSkill == 0 && caster != null && caster.Ability != null)
            {
                var a = caster.Ability;
                title = $"Q · {a.DisplayName}";
                detail = $"{a.Description}\n{a.EnergyCost:0} NL · {Ready(caster.CooldownRemaining, a.EnergyCost)}";
            }
            else if (selectedSkill == 6 && stats.GetComponent<VibrationSense>() is VibrationSense sense)
            {
                title = "C · Cảm nhận rung";
                detail = $"Phát hiện chuyển động quanh cơ thể.\n{sense.EnergyCost:0} NL · {Ready(sense.CooldownRemaining, sense.EnergyCost)}";
            }
            else if (selectedSkill >= 1 && selectedSkill <= 5 && skills != null && skills.GetSlot(selectedSkill - 1) is OrganSO source)
            {
                var a = source.GrantedAbility;
                title = $"{selectedSkill} · {a.DisplayName}";
                detail = $"{a.Description}\n{a.EnergyCost:0} NL · {Ready(skills.CooldownRemaining(selectedSkill - 1), a.EnergyCost)}";
            }
            else
            {
                title = selectedSkill == 0 || selectedSkill == 6 ? "Chưa có kỹ năng" : $"Ô {selectedSkill} · Chưa mở";
                detail = "Nâng cơ quan có kỹ năng đến cấp mở khóa.\nQ là Dị Năng riêng; C là giác quan.";
            }
            GUI.Label(new Rect(info.x, info.y, info.width, 25), title, text);
            GUI.Label(new Rect(info.x, info.y + 25, info.width, 60), detail, small);
            if (GUI.Button(new Rect(at.x - 32, at.y - 14, 64, 28), "Đóng", action)) SetMenu(HudMenu.None);
        }

        void DrawOrgansPanel()
        {
            if (organs == null || interactor == null) { SetMenu(HudMenu.None); return; }
            var r = new Rect(canvas.x - 404, 60, 380, canvas.y - 250);
            Paint(r, surface);
            GUILayout.BeginArea(new Rect(r.x + 16, r.y + 12, r.width - 32, r.height - 24));
            GUILayout.Label("Cơ quan", heading);
            GUILayout.Label($"{stats.State.EvolutionExp:0} EXP · {organs.Count}" + (organs.MaxOrgans > 0 ? $"/{organs.MaxOrgans}" : "") + " cơ quan", small);
            organScroll = GUILayout.BeginScrollView(organScroll);
            var list = organs.OrganList; // Snapshot: Drop may change the host while drawing.
            if (list.Count == 0) GUILayout.Label("Chưa có cơ quan. Tìm hoặc săn sinh vật để tiến hóa.", text);
            foreach (var o in list)
            {
                GUILayout.Space(12);
                int lv = organs.GetLevel(o);
                organs.GetProgress(o, out float progress, out float cost);
                GUILayout.Label($"{o.DisplayName} · {o.Tier} · {(lv == 0 ? "Mầm" : $"Lv{lv}")}", text);
                GUILayout.Label(OrganInteractor.SlotName(o.Slot) + (organs.IsMaxed(o) ? " · Tối đa" : $" · {progress:0}/{cost:0} EXP"), small);
                GUILayout.BeginHorizontal();
                GUI.enabled = !organs.IsMaxed(o) && stats.State.EvolutionExp > 0;
                if (GUILayout.Button(lv == 0 ? "Nuôi mầm" : "Nâng cấp", action)) organs.Invest(o);
                GUI.enabled = !organs.IsMaxed(o);
                bool priority = organs.Priority == o;
                if (GUILayout.Button(priority ? "Bỏ ưu tiên" : "Ưu tiên", action)) organs.Priority = priority ? null : o;
                GUI.enabled = true;
                if (GUILayout.Button("Tháo", action)) interactor.Drop(o);
                GUILayout.EndHorizontal();
                if (o.GrantedAbility != null && organs.Has(o))
                {
                    if (skills != null && skills.IsUnlocked(o))
                    {
                        GUILayout.Label($"{o.GrantedAbility.DisplayName} · Chọn ô:", small);
                        GUILayout.BeginHorizontal();
                        for (int slot = 0; slot < SkillBar.SlotCount; slot++)
                            if (GUILayout.Button(skills.SlotOf(o) == slot ? $"[{slot + 1}]" : (slot + 1).ToString(), action)) skills.Assign(slot, o);
                        GUILayout.EndHorizontal();
                    }
                    else GUILayout.Label($"Mở {o.GrantedAbility.DisplayName} ở Lv{o.AbilityUnlockLevel}", small);
                }
            }
            GUILayout.EndScrollView();
            if (GUILayout.Button("Đóng · G / Esc", action)) SetMenu(HudMenu.None);
            GUILayout.EndArea();
        }
    }
}
