using System.Collections.Generic;
using System.Linq;
using Evolve.Creatures;
using Evolve.Ecology;
using Evolve.Organs;
using Evolve.Survival;
using Evolve.Swallow;
using Evolve.Traits;
using Evolve.Visuals;
using UnityEngine;

namespace Evolve.DevTools
{
    /// <summary>
    /// The testing section of the god menu for the cave skills, prey defence, creature behaviour, encounter rewards and the ecosystem
    /// (see docs/TEST_CHECKLIST_UNITY.md). Everything is made from code: test creatures are plain spheres named after their species,
    /// so CreatureBrain and PreyDefense pick them up by name.
    /// </summary>
    public partial class GodMenu
    {
        bool showPrey, showHunters, showLords;
        string testNotice = "";
        readonly List<GameObject> testCreatures = new List<GameObject>();
        static List<string> preyNames, hunterNames, lordNames;

        static void LoadNames()
        {
            if (preyNames != null) return;
            preyNames = PreyDefenseSeed.All.Select(d => d.Species).ToList();
            preyNames.Add("Trứng Lạ");
            hunterNames = new List<string>(); lordNames = new List<string>();
            foreach (var r in CaveEcologySeed.Build().Regions.Values)
            {
                hunterNames.Add(CaveEcologySeed.NameOfId(r.Species[2].Id));
                lordNames.Add(CaveEcologySeed.NameOfId(r.Species[3].Id));
            }
        }

        static void LearnGene(Swallower sw, OrganSO o)
        {
            var g = new List<Gene> { new Gene(o, AnalysisRules.FromTier(o.Tier)) };
            for (int i = 0; i < 60 && !sw.Book.Knows(o); i++) sw.Book.Submit(g, 1f, () => 0f);
        }

        static void RaiseStars(Swallower sw, OrganSO o, int stars)
        {
            var g = new List<Gene> { new Gene(o, AnalysisRules.FromTier(o.Tier)) };
            for (int i = 0; i < 80 && sw.Book.StarsOf(o) < stars; i++) sw.Book.Submit(g, 1f, () => 0f);
        }

        // ---------- tab: kỹ năng ----------

        void DrawSkills(SurvivalStats stats, SlimeBody body, Swallower swallower)
        {
            Header("Kỹ năng hang");
            var caveGenes = Resources.LoadAll<OrganSO>("CaveSkills");
            Say($"{caveGenes.Length} gene (chạy Evolve → Create Cave Skills nếu bằng 0). Cho Lách Né để thử né.");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Học kỹ năng chủ động ★1", GUILayout.Height(30)) && swallower != null)
                foreach (var o in caveGenes) if (o.Use == GeneUse.Active) LearnGene(swallower, o);
            if (GUILayout.Button("Lên ★3 hết", GUILayout.Height(30)) && swallower != null)
                foreach (var o in caveGenes) if (o.Use == GeneUse.Active) { LearnGene(swallower, o); RaiseStars(swallower, o, 3); }
            GUILayout.EndHorizontal();
            if (GUILayout.Button("Gắn tối đa bị động hang (Da Dày, Vỏ Cứng, Sóng Dò Mạnh)", GUILayout.Height(30)) && swallower != null && host != null)
            {
                int n = 0;
                foreach (var o in caveGenes)
                {
                    if (o.Use != GeneUse.Passive) continue;
                    LearnGene(swallower, o);
                    if (host.TryAttach(o)) n++;
                }
                host.DebugGrowAll();
                testNotice = $"Đã gắn {n} bị động hang.";
            }

            showOrganList = Fold(showOrganList, "Danh sách cơ quan và gene (nhận, học, gắn)");
            if (showOrganList) DrawOrganList(swallower);
            showRewards = Fold(showRewards, "Phần thưởng kỳ ngộ (EncounterRewards)");
            if (showRewards) DrawRewards(stats);
        }

        void DrawOrganList(Swallower swallower)
        {
            growInstantly = Check(growInstantly, "Cho lớn đầy ngay khi nhận (Lv1)");
            if (GUILayout.Button("Học hết cơ quan") && swallower != null)
                foreach (var o in AllOrgans())
                    while (!swallower.Book.Knows(o))
                        swallower.Book.Submit(new List<Gene> { new Gene(o, AnalysisRules.FromTier(o.Tier)) }, 1f, () => 0f);
            foreach (var o in AllOrgans())
            {
                bool has = host != null && host.Has(o);
                bool active = o.Use == GeneUse.Active;
                bool learned = swallower != null && swallower.Book.Knows(o);
                GUILayout.BeginHorizontal();
                GUILayout.Label($"[{o.Tier}] {o.DisplayName} ({OrganInteractor.SlotName(o.Slot)})", wrapLabel, GUILayout.ExpandWidth(true));
                GUI.enabled = host != null && (active ? !learned : !has);
                if (GUILayout.Button(active ? (learned ? "Đã học" : "Học chiêu") : (has ? "Đang có" : "Nhận bị động"), GUILayout.Width(120)))
                {
                    if (active)
                    {
                        // an active move is learned, not worn: it appears in the skill menu (K) and on keys 1-5
                        if (swallower != null)
                            while (!swallower.Book.Knows(o))
                                swallower.Book.Submit(new List<Gene> { new Gene(o, AnalysisRules.FromTier(o.Tier)) }, 1f, () => 0f);
                    }
                    else
                    {
                        host.TryAttach(o);
                        if (growInstantly) host.DebugGrowAll();
                    }
                }
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Lớn đầy (Lv1)") && host != null) host.DebugGrowAll();
            if (GUILayout.Button("Lv2") && host != null) host.DebugSetLevel(2);
            if (GUILayout.Button("Lv3") && host != null) host.DebugSetLevel(3);
            if (GUILayout.Button("Tháo hết") && host != null)
                foreach (var o in host.OrganList) host.Detach(o);
            GUILayout.EndHorizontal();
        }

        void DrawRewards(SurvivalStats stats)
        {
            var traits = player.GetComponent<PlayerTraits>();
            var go = player.gameObject;
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Gene Hiếm+")) testNotice = "Gene: " + (EncounterRewards.GrantGene(go, (int)GeneRarity.Rare) ?? "không còn gene nào");
            if (GUILayout.Button("Gene Lạ")) testNotice = "Gene riêng: " + (EncounterRewards.GrantUniqueGene(go, "gene_la") ?? "không có (chạy Evolve → Create Encounter Genes) hoặc đã biết");
            if (GUILayout.Button("Gene Cổ")) testNotice = "Gene riêng: " + (EncounterRewards.GrantUniqueGene(go, "gene_loai_co") ?? "không có hoặc đã biết");
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+5 khối")) testNotice = EncounterRewards.AddMass(go, 5f) ? "+5 khối" : "không được";
            if (GUILayout.Button("Đổi Khuyết Điểm")) testNotice = EncounterRewards.RerollFlaw(go) ? "Khuyết Điểm: " + string.Join(", ", traits.FlawIds) : "không đổi được (không có Khuyết Điểm?)";
            if (GUILayout.Button("+8°C chịu nhiệt")) testNotice = EncounterRewards.AddHeatResistance(go, 8f) ? "kháng nhiệt tăng" : "đã tối đa";
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Lõi mạnh lên")) testNotice = EncounterRewards.BoostCore(go) ? "Lõi mạnh lên" : "đã tối đa hoặc không có Lõi";
            if (GUILayout.Button("Mở màu Cầu Vồng")) testNotice = EncounterRewards.UnlockFinish(go, ColorUnlocks.Rainbow) ? "Đã mở khóa Cầu Vồng (F3 để bật)" : "đã mở từ trước";
            if (GUILayout.Button("Câu Tiếng Vọng")) EncounterRewards.Say("«Thử: Tiếng Vọng nói một câu.»");
            GUILayout.EndHorizontal();
            if (GUILayout.Button("Hồi đầy chỉ số (phần thưởng)")) testNotice = EncounterRewards.RefillStats(go) ? "đã hồi đầy" : "không được";
        }

        // ---------- tab: sinh vật ----------

        void DrawCreatures()
        {
            LoadNames();
            Header("Hành vi sinh vật");
            useRealModels = Check(useRealModels, "Dùng mô hình thật của loài khi có (tắt = quả cầu đặt tên)");
            CreatureBrain.Enabled = Check(CreatureBrain.Enabled, "Sinh vật săn nhau, ăn xác, theo kích cỡ slime (B2)");
            Predator.Passive = Check(Predator.Passive, "Hòa bình: kẻ săn mồi không tấn công");
            bool frozen = Check(Predator.Frozen && Prey.Frozen, "Đóng băng mọi sinh vật");
            Predator.Frozen = frozen; Prey.Frozen = frozen;
            Evolve.Creatures.CreatureMotor.Enabled = Check(Evolve.Creatures.CreatureMotor.Enabled, "Di chuyển mới cho quái (va vách, bước bậc, không vọt lên cầu); tắt = kiểu cũ");

            Header("Cỡ và độ khó (chỉ áp cho quái tạo sau khi đổi)");
            Say($"Cỡ quái ×{Evolve.Creatures.CaveTier.SizeScale:0.00}");
            Evolve.Creatures.CaveTier.SizeScale = GUILayout.HorizontalSlider(Evolve.Creatures.CaveTier.SizeScale, 0.5f, 2f);
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Độ khó: {(Evolve.Creatures.CaveTier.TierOverride < 0 ? "theo hang" : "tầng " + Evolve.Creatures.CaveTier.TierOverride)}", GUILayout.Width(150));
            if (GUILayout.Button("Theo hang")) Evolve.Creatures.CaveTier.TierOverride = -1;
            for (int t = 0; t <= 3; t++) if (GUILayout.Button(t.ToString(), GUILayout.Width(36))) Evolve.Creatures.CaveTier.TierOverride = t;
            GUILayout.EndHorizontal();

            Header($"Tạo sinh vật thử ({testCreatures.Count(c => c != null)} con)");
            Vector3 front = player.transform.position + player.transform.forward * 3f + Vector3.right * 2f;
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Con mồi gần (spawner)")) FindAnyObjectByType<PreySpawner>()?.SpawnAt(front);
            if (GUILayout.Button("Kẻ săn gần (spawner)")) FindAnyObjectByType<PredatorSpawner>()?.SpawnAt(player.transform.position + Vector3.forward * 8f);
            GUILayout.EndHorizontal();
            showPrey = Fold(showPrey, $"Con mồi ({preyNames.Count})");
            if (showPrey) foreach (var n in preyNames) if (GUILayout.Button(n)) SpawnTest(n, 3f, 12f, false);
            showHunters = Fold(showHunters, $"Kẻ săn ({hunterNames.Count})");
            if (showHunters) foreach (var n in hunterNames) if (GUILayout.Button(n)) SpawnTest(n, 15f, 40f, true);
            showLords = Fold(showLords, $"Chúa Hang ({lordNames.Count})");
            if (showLords) foreach (var n in lordNames) if (GUILayout.Button(n)) SpawnTest(n, 40f, 120f, true);

            Header("Xác và dọn dẹp");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Xác lớn gần (Chó Xương canh)")) SpawnCarcass(2.2f, 90f);
            if (GUILayout.Button("Xác nhỏ gần")) SpawnCarcass(0.7f, 20f);
            GUILayout.EndHorizontal();
            if (GUILayout.Button($"Hạ hết kẻ săn mồi ({Predator.Active.Count}) (vẫn rơi đồ)"))
                foreach (var p in Predator.Active.ToList()) p.TakeDamage(99999f, player);
            if (GUILayout.Button("Xóa sinh vật thử và xác")) { foreach (var c in testCreatures) if (c != null) Destroy(c); testCreatures.Clear(); foreach (var c in Carcass.Active.ToList()) if (c != null) Destroy(c.gameObject); }
        }

        // ---------- tab: combat ----------

        void DrawCombat(SurvivalStats stats)
        {
            Header("Combat của slime: Vồ và Ghì Chặt");
            Say("Vồ = chuột trái (đóng menu này trước). Tạo quái thử ngay trước mặt slime:");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Con mồi to (khối 14)", GUILayout.Height(30))) SpawnTest("Ốc Hồ", 14f, 40f, false);
            if (GUILayout.Button("Kẻ săn (khối 18)", GUILayout.Height(30))) SpawnTest("Thằn Lằn Bờ Hồ", 18f, 60f, true);
            if (GUILayout.Button("Chúa Hang (khối 35)", GUILayout.Height(30))) SpawnTest("Cóc Hồ Già", 35f, 120f, true);
            GUILayout.EndHorizontal();
            var latch = Evolve.Combat.LatchSession.Active;
            Say(latch == null ? "Không đang bám." : $"Đang bám: lỗi {latch.Game.Faults}/{latch.Game.MaxFaults}, vùng xanh {latch.Game.ZoneWidth * 100f:0}%, năng lượng {stats.State.Energy:0}");
            if (GUILayout.Button("Hồi đầy năng lượng")) { stats.State.Energy = stats.State.MaxEnergy; }

            Header("Chiêu của quái (báo trước; né bằng Lách Né, phản công lúc quái kiệt sức)");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Thằn Lằn", GUILayout.Height(30))) SpawnTest("Thằn Lằn Bờ Hồ", 10f, 34f, true);
            if (GUILayout.Button("Bọ Giáp", GUILayout.Height(30))) SpawnTest("Bọ Giáp Quặng", 18f, 52f, true);
            if (GUILayout.Button("Rùa Pha Lê", GUILayout.Height(30))) SpawnTest("Rùa Pha Lê", 42f, 115f, true);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Bầy Muỗi (4)", GUILayout.Height(30))) for (int i = 0; i < 4; i++) SpawnTest("Muỗi Rêu", 1f, 8f, false);
            if (GUILayout.Button("Chồn Khe (rình, 2)", GUILayout.Height(30))) for (int i = 0; i < 2; i++) SpawnTest("Chồn Khe", 16f, 54f, true);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Cáo Gió (Chúa Hang)", GUILayout.Height(30))) SpawnTest("Cáo Gió", 48f, 155f, true);
            if (GUILayout.Button("Khỉ Hang (3)", GUILayout.Height(30))) for (int i = 0; i < 3; i++) SpawnTest("Khỉ Hang", 12f, 40f, true);
            GUILayout.EndHorizontal();
            if (GUILayout.Button("Xóa sinh vật thử")) { foreach (var c in testCreatures) if (c != null) Destroy(c); testCreatures.Clear(); }

            Header("Trang phục trùm (Cáo Gió): chỉnh vị trí, áp cho cáo tạo sau khi đổi");
            Say($"Cao đầu {Evolve.Combat.LordRegalia.HeadHeight:0.00}, đầu ra trước {Evolve.Combat.LordRegalia.HeadForward:0.00}, cổ ra trước {Evolve.Combat.LordRegalia.NeckForward:0.00}, cỡ ×{Evolve.Combat.LordRegalia.Size:0.00}");
            Evolve.Combat.LordRegalia.HeadHeight = GUILayout.HorizontalSlider(Evolve.Combat.LordRegalia.HeadHeight, 0.5f, 1.1f);
            Evolve.Combat.LordRegalia.HeadForward = GUILayout.HorizontalSlider(Evolve.Combat.LordRegalia.HeadForward, 0.4f, 1.1f);
            Evolve.Combat.LordRegalia.NeckForward = GUILayout.HorizontalSlider(Evolve.Combat.LordRegalia.NeckForward, 0.3f, 1.0f);
            Evolve.Combat.LordRegalia.Size = GUILayout.HorizontalSlider(Evolve.Combat.LordRegalia.Size, 0.5f, 2f);

            Header("Pha hiện tại của quái trong 25 m");
            bool any = false;
            foreach (var cc in Evolve.Combat.CreatureCombat.All)
                if (cc != null && (cc.transform.position - player.transform.position).sqrMagnitude < 625f) { Say($"  {cc.name.Replace("(Clone)", "")}: {cc.Status}"); any = true; }
            if (!any) Say("  (không có quái có bộ chiêu quanh đây)");
        }

        // ---------- ecosystem (in the world tab) ----------

        void DrawEcology()
        {
            Header("Sinh thái");
            if (GUILayout.Button("Mô phỏng sinh thái 2 giờ chơi (kết quả ở Console)", GUILayout.Height(30))) RunEcologySim();
            if (GUILayout.Button("In quần thể đang sống của mọi hang (Console)", GUILayout.Height(30)))
            {
                var sb = new System.Text.StringBuilder("Quần thể đang sống (số con / sức chứa, no %):\n");
                foreach (var r in EcologyRuntime.World.Regions)
                {
                    sb.Append($"  {r.Key} ({string.Join(", ", EcologyRuntime.World.Neighbours(r.Key))}):");
                    foreach (var sp in r.Value.Species) sb.Append($" {CaveEcologySeed.NameOfId(sp.Id) ?? sp.Id}{(sp.Guest ? " (khách)" : "")} {sp.Count}/{sp.Capacity} {sp.Satiety * 100f:0}%;");
                    sb.AppendLine();
                }
                Debug.Log(sb.ToString());
            }
        }

        static bool useRealModels = true;
        static readonly Dictionary<string, GameObject> prefabCache = new Dictionary<string, GameObject>();

        /// <summary>The real creature prefab of a species (Assets/_Project/Prefabs/Wildlife, matched by SpeciesNames.Key), or null: editor only.</summary>
        static GameObject FindPrefab(string displayName)
        {
#if UNITY_EDITOR
            string key = SpeciesNames.Key(displayName);
            if (prefabCache.TryGetValue(key, out var cached) && cached != null) return cached;
            foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Project/Prefabs/Wildlife" }))
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                if (SpeciesNames.Key(System.IO.Path.GetFileNameWithoutExtension(path)) != key) continue;
                if (path.Contains("(Meshy)")) { var meshy = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path); if (meshy != null) return prefabCache[key] = meshy; }   // the Meshy variant wins for testing
                var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null && (prefab.GetComponent<Predator>() != null || prefab.GetComponent<Prey>() != null)) return prefabCache[key] = prefab;
            }
#endif
            return null;
        }

        /// <summary>Spawns the species' real prefab (with its model, stats and components) in front of the slime; false when there is none.</summary>
        bool SpawnPrefab(string displayName)
        {
            var prefab = useRealModels ? FindPrefab(displayName) : null;
            if (prefab == null) return false;
            Vector3 pos = player.transform.position + player.transform.forward * 5f + Vector3.right * Random.Range(-2f, 2f);
            pos.y = Evolve.World.GroundHeight.At(pos.x, pos.z, pos.y) + prefab.transform.position.y;
            var go = Instantiate(prefab, pos, Quaternion.identity);
            if (go.TryGetComponent(out Prey prey)) prey.HomeCenter = pos;
            CreatureTuning.Apply(go, CaveEcologySeed.CaveOfSpecies(CaveEcologySeed.IdByName(displayName)));   // the size, danger and costume it would have in its cave
            if (go.TryGetComponent(out Predator hunter)) hunter.SetHunger01(0.8f);   // hungry, so it goes hunting at once
            if (go.GetComponent<CreatureBrain>() == null) go.AddComponent<CreatureBrain>();
            if (go.GetComponent<Prey>() != null && go.GetComponent<PreyDefense>() == null && PreyDefenseSeed.FindBySpecies(displayName) != null) go.AddComponent<PreyDefense>();
            testCreatures.Add(go);
            testNotice = $"Đã tạo {displayName} (mô hình thật)";
            return true;
        }

        void SpawnTest(string displayName, float mass, float health, bool predator)
        {
            if (SpawnPrefab(displayName)) return;   // the real model when the species has a prefab; otherwise the plain sphere below
            Vector3 pos = player.transform.position + player.transform.forward * 5f + Vector3.right * Random.Range(-2f, 2f);
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = displayName;   // the species name: PreyDefense and CreatureBrain find their data by it
            float size = predator ? Mathf.Lerp(0.9f, 1.8f, Mathf.InverseLerp(15f, 40f, mass)) : 0.6f;
            sphere.transform.localScale = Vector3.one * size;
            pos.y = Evolve.World.GroundHeight.At(pos.x, pos.z, pos.y) + size * 0.5f;
            sphere.transform.position = pos;
            sphere.GetComponent<Renderer>().material.color = predator ? Color.Lerp(new Color(0.85f, 0.4f, 0.3f), new Color(0.5f, 0.1f, 0.2f), Mathf.InverseLerp(15f, 40f, mass)) : new Color(0.9f, 0.8f, 0.4f);
            sphere.AddComponent<Rigidbody>();   // made kinematic by Prey / Predator
            if (predator) { var hunter = sphere.AddComponent<Predator>(); hunter.SetStats(mass, health); hunter.SetHunger01(0.8f); }   // hungry, so it goes hunting at once
            else
            {
                var prey = sphere.AddComponent<Prey>();
                prey.SetStats(mass, health);
                prey.HomeCenter = pos;
                if (PreyDefenseSeed.FindBySpecies(displayName) != null) sphere.AddComponent<PreyDefense>();
            }
            sphere.AddComponent<CreatureBrain>();
            testCreatures.Add(sphere);
            testNotice = $"Đã tạo {displayName} (khối {mass:0})";
        }

        void SpawnCarcass(float size, float food)
        {
            Vector3 pos = player.transform.position + player.transform.forward * 7f;
            pos.y = Evolve.World.GroundHeight.At(pos.x, pos.z, pos.y) + 0.2f;
            var c = Carcass.Spawn(pos, size, null);
            if (c != null) c.ResetFood(food);
            testNotice = c != null ? "Đã tạo xác" : "không tạo được xác (đang là máy khách?)";
        }

        static void RunEcologySim()
        {
            var w = CaveEcologySeed.Build();
            var maze = FindAnyObjectByType<Evolve.Maze.MazeWorldManager>();
            int linked = maze != null && maze.Graph != null ? MazeEcologyLinks.Apply(w, maze.Graph) : -1;   // the real tunnels when a maze is loaded
            var sum = new Dictionary<string, float>(); var min = new Dictionary<string, int>(); var max = new Dictionary<string, int>();
            const int steps = 240;
            for (int i = 0; i < steps; i++)
            {
                w.Tick(0.5f);
                foreach (var r in w.Regions)
                    foreach (var s in r.Value.Species.Where(x => !x.Guest))
                    {
                        string k = r.Key + "/" + (CaveEcologySeed.NameOfId(s.Id) ?? s.Id);
                        sum[k] = (sum.TryGetValue(k, out var v) ? v : 0f) + s.Count;
                        min[k] = min.TryGetValue(k, out var a) ? Mathf.Min(a, s.Count) : s.Count;
                        max[k] = max.TryGetValue(k, out var b) ? Mathf.Max(b, s.Count) : s.Count;
                    }
            }
            var sb = new System.Text.StringBuilder($"Mô phỏng sinh thái 120 phút, {(linked < 0 ? "đường nối tạm" : $"đường nối theo mê cung thật ({linked} cặp hang)")} (trung bình / ít nhất / nhiều nhất):\n");
            foreach (var kv in sum.OrderBy(k => k.Key)) sb.AppendLine($"  {kv.Key}: {kv.Value / steps:0.0} / {min[kv.Key]} / {max[kv.Key]}");
            Debug.Log(sb.ToString());
        }
    }
}
