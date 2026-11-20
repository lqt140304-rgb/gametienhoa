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

        void DrawTestSection(SurvivalStats stats, SlimeBody body, Swallower swallower)
        {
            Header("Kiểm thử kỹ năng, sinh vật, phần thưởng (tài liệu: docs/TEST_CHECKLIST_UNITY.md)");
            if (testNotice.Length > 0) GUILayout.Label(testNotice);
            var traits = player.GetComponent<PlayerTraits>();
            var go = player.gameObject;

            GUILayout.Label($"Máu {stats.State.Health:0}/{stats.State.MaxHealth:0} · kháng nhiệt +{stats.State.TotalHeatResistance:0.#}°C · Lõi {(traits != null ? traits.CoreId : "-")} ×{(traits != null ? traits.CoreBoost : 1f):0.##} · Khuyết Điểm {(traits != null ? string.Join(", ", traits.FlawIds) : "-")}");

            // ---- slime size ----
            GUILayout.Label($"Kích cỡ slime (khối {(body != null ? body.Mass : 0f):0.#}): đổi để thử ai săn ai, ai chạy ai");
            GUILayout.BeginHorizontal();
            foreach (float m in new[] { 3f, 10f, 20f, 40f, 80f })
                if (GUILayout.Button($"{m:0}") && body != null) body.SetMass(m);
            GUILayout.EndHorizontal();

            // ---- skills ----
            var caveGenes = Resources.LoadAll<OrganSO>("CaveSkills");
            GUILayout.Label($"Kỹ năng hang: {caveGenes.Length} gene (chạy Evolve → Create Cave Skills nếu bằng 0)");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Học kỹ năng chủ động ★1") && swallower != null)
                foreach (var o in caveGenes) if (o.Use == GeneUse.Active) LearnGene(swallower, o);
            if (GUILayout.Button("Lên ★3 hết") && swallower != null)
                foreach (var o in caveGenes) if (o.Use == GeneUse.Active) { LearnGene(swallower, o); RaiseStars(swallower, o, 3); }
            GUILayout.EndHorizontal();
            if (GUILayout.Button("Gắn tối đa bị động hang (Da Dày, Vỏ Cứng, Sóng Dò Mạnh)") && swallower != null && host != null)
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

            // ---- creatures ----
            LoadNames();
            Evolve.Creatures.CreatureMotor.Enabled = GUILayout.Toggle(Evolve.Creatures.CreatureMotor.Enabled, " Di chuyển mới cho quái (va vách, bước bậc, không vọt lên cầu); tắt = kiểu cũ");
            GUILayout.Label($"Cỡ quái ×{Evolve.Creatures.CaveTier.SizeScale:0.00} (chỉ áp cho quái tạo sau khi đổi)");
            Evolve.Creatures.CaveTier.SizeScale = GUILayout.HorizontalSlider(Evolve.Creatures.CaveTier.SizeScale, 0.5f, 2f);
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Độ khó: {(Evolve.Creatures.CaveTier.TierOverride < 0 ? "theo hang" : "tầng " + Evolve.Creatures.CaveTier.TierOverride)}", GUILayout.Width(140));
            if (GUILayout.Button("Theo hang")) Evolve.Creatures.CaveTier.TierOverride = -1;
            for (int t = 0; t <= 3; t++) if (GUILayout.Button(t.ToString())) Evolve.Creatures.CaveTier.TierOverride = t;
            GUILayout.EndHorizontal();
            GUILayout.Label($"Sinh vật thử (đặt trước mặt slime, {testCreatures.Count(c => c != null)} con)");
            showPrey = GUILayout.Toggle(showPrey, $" Con mồi ({preyNames.Count})");
            if (showPrey) foreach (var n in preyNames) if (GUILayout.Button(n)) SpawnTest(n, 3f, 12f, false);
            showHunters = GUILayout.Toggle(showHunters, $" Kẻ săn ({hunterNames.Count})");
            if (showHunters) foreach (var n in hunterNames) if (GUILayout.Button(n)) SpawnTest(n, 15f, 40f, true);
            showLords = GUILayout.Toggle(showLords, $" Chúa Hang ({lordNames.Count})");
            if (showLords) foreach (var n in lordNames) if (GUILayout.Button(n)) SpawnTest(n, 40f, 120f, true);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Xác lớn gần (Chó Xương canh)")) SpawnCarcass(2.2f, 90f);
            if (GUILayout.Button("Xác nhỏ gần")) SpawnCarcass(0.7f, 20f);
            if (GUILayout.Button("Xóa sinh vật thử")) { foreach (var c in testCreatures) if (c != null) Destroy(c); testCreatures.Clear(); foreach (var c in Carcass.Active.ToList()) if (c != null) Destroy(c.gameObject); }
            GUILayout.EndHorizontal();

            // ---- rewards ----
            GUILayout.Label("Phần thưởng kỳ ngộ (EncounterRewards)");
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
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Nhiễm bệnh thử")) { stats.State.Infect(DiseaseKind.RottenGel); stats.State.Infect(DiseaseKind.SporeBlur); }
            if (GUILayout.Button("Hết bệnh")) testNotice = EncounterRewards.CureDiseases(go) ? "đã chữa" : "không được";
            if (GUILayout.Button("Hồi đầy")) testNotice = EncounterRewards.RefillStats(go) ? "đã hồi đầy" : "không được";
            GUILayout.EndHorizontal();
            if (GUILayout.Button("Bỏ 2 món vào Túi"))
                testNotice = $"Vào Túi: {EncounterRewards.GiveItems(go, new[] { new EncounterItem { Label = "Mẩu xương cũ", Mass = 1f, Nutrition = 10f, Food = FoodKind.PreyMeat }, new EncounterItem { Label = "Hạt lạ", Mass = 0.5f, Nutrition = 5f, Food = FoodKind.PreyMeat } })} món";

            // ---- ecosystem ----
            if (GUILayout.Button("Mô phỏng sinh thái 2 giờ chơi (kết quả ở Console)")) RunEcologySim();
            if (GUILayout.Button("In quần thể đang sống của mọi hang (Console)"))
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

        void SpawnTest(string displayName, float mass, float health, bool predator)
        {
            Vector3 pos = player.transform.position + player.transform.forward * 5f + Vector3.right * Random.Range(-2f, 2f);
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = displayName;   // the species name: PreyDefense and CreatureBrain find their data by it
            float size = predator ? Mathf.Lerp(0.9f, 1.8f, Mathf.InverseLerp(15f, 40f, mass)) : 0.6f;
            sphere.transform.localScale = Vector3.one * size;
            pos.y = Evolve.World.GroundHeight.At(pos.x, pos.z, pos.y) + size * 0.5f;
            sphere.transform.position = pos;
            sphere.GetComponent<Renderer>().material.color = predator ? Color.Lerp(new Color(0.85f, 0.4f, 0.3f), new Color(0.5f, 0.1f, 0.2f), Mathf.InverseLerp(15f, 40f, mass)) : new Color(0.9f, 0.8f, 0.4f);
            sphere.AddComponent<Rigidbody>();   // made kinematic by Prey / Predator
            if (predator) sphere.AddComponent<Predator>().SetStats(mass, health);
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
