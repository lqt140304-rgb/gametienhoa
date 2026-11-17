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
        Vector2 skillScroll;
        Vector2 organScroll;
        GUIStyle text, small, heading, action;

        readonly Color ink = new Color(.86f, .91f, .81f);
        readonly Color muted = new Color(.65f, .75f, .61f);
        readonly Color surface = new Color(.055f, .12f, .095f, .96f);
        Vector2 canvas;

        public bool MenuOpen => menu != HudMenu.None;
        public static bool HasFor(GameObject owner) => owner != null && owners.TryGetValue(owner, out var hud) && hud != null && hud.isActiveAndEnabled;
        /// <summary>Set by tools outside the HUD (such as the co-op menu) while they are open: they behave like a HUD menu.</summary>
        public static bool ExternalMenuOpen;
        public static bool BlocksInput(GameObject owner) => ExternalMenuOpen || Evolve.Visuals.SlimeColorMenu.IsOpen || Evolve.DevTools.GhostMode.PanelOpen || (HasFor(owner) && owners[owner].MenuOpen);

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
            bool full = KeyPressed.TabHeld();
            bool thirstOn = stats.State.ThirstEnabled;
            bool unsafeTemp = s.Temperature > s.HeatLimit(stats.Config) || s.Temperature < stats.Config.ComfortMin;
            bool showHealth = HudVisibility.ShowHealth(Frac(s.Health, s.MaxHealth), full);
            bool showEnergy = HudVisibility.ShowEnergy(Frac(s.Energy, s.MaxEnergy), full);
            bool showHunger = HudVisibility.ShowHunger(Frac(s.Hunger, s.MaxHunger), full);
            bool showThirst = HudVisibility.ShowThirst(Frac(s.Thirst, s.MaxThirst), thirstOn, full);
            bool showStress = HudVisibility.ShowStress(s.Stress, full);
            int rows = (showHealth ? 1 : 0) + (showEnergy ? 1 : 0) + (showHunger ? 1 : 0) + (showThirst ? 1 : 0) + (showStress ? 1 : 0);
            for (int k = 0; k < NutrientKinds.Count; k++)
                if (HudVisibility.ShowNutrient(s.Nutrient((NutrientKind)k), full)) rows++; // only the lacking nutrients (all with Tab)
            float rowStep = floating ? 12 : 39;
            float rowHeight = floating ? 7 : 32;
            float width = floating ? 132 : 230;
            float blockHeight = rows * rowStep + (floating ? 28 : 40);
            float x = floating ? HudLayout.ClampCenter(anchor.Value.x, width, canvas.x) - width / 2 : 24;
            float y = floating ? HudLayout.ClampCenter(anchor.Value.y + 55, blockHeight, canvas.y) - blockHeight / 2 : canvas.y - 45 - rows * rowStep;
            float row = 0;
            if (showHealth) StatLine(new Rect(x, y + row++ * rowStep, width, rowHeight), "Máu", s.Health, s.MaxHealth, new Color(.85f, .56f, .47f), !floating);
            if (showEnergy) StatLine(new Rect(x, y + row++ * rowStep, width, rowHeight), "Năng lượng", s.Energy, s.MaxEnergy, new Color(.69f, .83f, .61f), !floating);
            if (showHunger) StatLine(new Rect(x, y + row++ * rowStep, width, rowHeight), "Độ no", s.Hunger, s.MaxHunger, new Color(.84f, .73f, .46f), !floating);
            if (showThirst) StatLine(new Rect(x, y + row++ * rowStep, width, rowHeight), "Khát", s.Thirst, s.MaxThirst, new Color(.47f, .7f, .86f), !floating);
            if (showStress) StatLine(new Rect(x, y + row++ * rowStep, width, rowHeight), "Căng thẳng", s.Stress, SurvivalState.MaxStress, new Color(.67f, .55f, .82f), !floating);
            for (int k = 0; k < NutrientKinds.Count; k++)
            {
                var kind = (NutrientKind)k;
                if (!HudVisibility.ShowNutrient(s.Nutrient(kind), full)) continue;
                string label = NutrientKinds.SlimeName(kind) + (s.IsWarning(kind) ? " !" : "");
                StatLine(new Rect(x, y + row++ * rowStep, width, rowHeight), label, s.Nutrient(kind), SurvivalState.MaxNutrient, NutrientKinds.ColorOf(kind), !floating);
            }
            GUI.Label(new Rect(x, y + rows * rowStep, width, 28),
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
            if (GUI.Button(new Rect(right.x, right.y + 55, 108, 32), "K · Kho skill", action)) SetMenu(HudLayout.Toggle(menu, HudMenu.Skills));
            if (GUI.Button(new Rect(right.x + 116, right.y + 55, 114, 32), "G · Cơ quan", action)) SetMenu(HudLayout.Toggle(menu, HudMenu.Organs));
            if (GUI.Button(new Rect(canvas.x - 136, 18, 112, 28), "Giao diện", action)) SetMenu(HudLayout.Toggle(menu, HudMenu.Settings));
            if (!string.IsNullOrEmpty(GUI.tooltip))
                GUI.Label(new Rect(canvas.x / 2 - 187, skillY - 28, 374, 24), GUI.tooltip, small);
            // A skill key that did nothing says why (it used to fail silently).
            if (skills != null && !string.IsNullOrEmpty(skills.FailureReason) && Time.time - skills.FailureTime < 2.5f)
                GUI.Label(new Rect(canvas.x / 2 - 187, skillY - 54, 374, 24), "Không dùng được: " + skills.FailureReason, small);
            GUI.matrix = old;
        }

        static float Frac(float value, float max) => max > 0 ? value / max : 1f;

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
            if (menu == HudMenu.Skills) DrawSkillInventory();
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

        void DrawSkillInventory()
        {
            var r = new Rect(canvas.x / 2 - 245, 55, 490, canvas.y - 165);
            Paint(r, surface);
            GUILayout.BeginArea(new Rect(r.x + 18, r.y + 14, r.width - 36, r.height - 28));
            GUILayout.Label("Kho kỹ năng", heading);
            GUILayout.Label("Chọn kỹ năng từ cơ quan đang gắn để trang bị vào 5 ô.", small);
            skillScroll = GUILayout.BeginScrollView(skillScroll);
            int count = 0;
            if (organs != null && skills != null)
            {
                foreach (var organ in organs.OrganList)
                {
                    if (!skills.IsUnlocked(organ)) continue;
                    count++;
                    var ability = organ.GrantedAbility;
                    int equipped = skills.SlotOf(organ);
                    GUILayout.Space(12);
                    GUILayout.Label(ability.DisplayName, text);
                    GUILayout.Label($"{organ.DisplayName} · Lv{organs.GetLevel(organ)} · " +
                        (equipped >= 0 ? $"Đang ở ô {equipped + 1}" : "Chưa trang bị"), small);
                    GUILayout.Label(ability.Description, small);
                    GUILayout.Label($"{ability.EnergyCost:0} NL · Hồi {ability.CooldownFor(organs.GetLevel(organ)):0.#}s", small);
                    GUILayout.BeginHorizontal();
                    for (int slot = 0; slot < SkillBar.SlotCount; slot++)
                    {
                        var occupied = skills.GetSlot(slot);
                        string label = equipped == slot ? $"[{slot + 1}]" : (slot + 1).ToString();
                        string hint = occupied != null ? $"Đổi với {occupied.GrantedAbility.DisplayName}" : "Trang bị vào ô trống";
                        if (GUILayout.Button(new GUIContent(label, hint), action)) skills.Assign(slot, organ);
                    }
                    GUILayout.EndHorizontal();
                }
            }
            if (count == 0)
                GUILayout.Label("Chưa có kỹ năng cơ quan được mở. Nâng cơ quan có kỹ năng đến cấp yêu cầu để thêm vào kho.", text);
            GUILayout.Space(16);
            GUILayout.Label("Khả năng riêng", text);
            if (caster != null && caster.Ability != null)
                GUILayout.Label($"Q · {caster.Ability.DisplayName} (không chiếm ô)", small);
            if (stats.GetComponent<VibrationSense>() != null)
                GUILayout.Label("C · Cảm nhận rung (giác quan riêng)", small);
            GUILayout.EndScrollView();
            if (GUILayout.Button("Đóng · K / Esc", action)) SetMenu(HudMenu.None);
            GUILayout.EndArea();
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
                float decay = organs.GetDecay(o);
                var need = o.EffectiveNutrient;
                GUILayout.Label($"Cần: {NutrientKinds.SlimeName(need)} ({NutrientKinds.RealName(need)})" + (decay > 0.01f ? $" · đang yếu {decay * 100f:0}%" : ""), small);
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
