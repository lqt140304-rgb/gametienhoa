using System.Collections.Generic;
using Evolve.Survival;
using UnityEngine;

namespace Evolve.World
{
    public enum ForageHabitat { ShoreWater, Shallows, WetEdge, ShoreBand, MidBand, WallFoot, Ore }

    /// <summary>One kind of thing in the cavern a slime can absorb.</summary>
    public sealed class ForageSpec
    {
        public string Id, Name;
        public FoodKind Food;
        public ForageHabitat Habitat;
        public int Count;
        public bool Mineral;
        public float Nutrition, AbsorbRate, Respawn;
    }

    /// <summary>
    /// What can be eaten in the Slime Cavern and where it grows (spec 2026-11-19-slime-cavern-design). Most of the cavern's plants are NOT food
    /// (the grass carpet, the plain rocks and the teal mushrooms are only scenery); the edible ones are a dozen kinds, each in its own habitat
    /// and each with its own nutrient recipe (World/FoodProfile): some give protein, some fat, some vitamins, some calcium, some iron.
    /// Minerals feed more per bite, absorb slowly and take long to form again. Pure data and maths, so tests can check it.
    /// </summary>
    public static class CavernForageData
    {
        static ForageSpec Plant(string id, string name, FoodKind food, ForageHabitat habitat, int count, float nutrition, float respawn = 150f) =>
            new ForageSpec { Id = id, Name = name, Food = food, Habitat = habitat, Count = count, Mineral = false, Nutrition = nutrition, AbsorbRate = 5f, Respawn = respawn };

        static ForageSpec Mineral(string id, string name, FoodKind food, int count) =>
            new ForageSpec { Id = id, Name = name, Food = food, Habitat = ForageHabitat.Ore, Count = count, Mineral = true, Nutrition = 24f, AbsorbRate = 2.5f, Respawn = 600f };

        public static readonly IReadOnlyList<ForageSpec> Specs = new List<ForageSpec>
        {
            Plant("reed", "Lau Ven Hồ", FoodKind.Cattail, ForageHabitat.ShoreWater, 14, 12f),           // protein, fat, a little vitamin
            Plant("lily", "Súng Nước", FoodKind.WaterLily, ForageHabitat.Shallows, 10, 10f),              // vitamin and a little calcium
            Plant("moss", "Rêu Sáng", FoodKind.GlowMoss, ForageHabitat.WetEdge, 18, 8f, 120f),            // pure vitamin
            Plant("sweet", "Cỏ Ngọt", FoodKind.SweetRoot, ForageHabitat.ShoreBand, 26, 14f),              // protein and fat
            Plant("fern", "Dương Xỉ Đắng", FoodKind.BitterFern, ForageHabitat.MidBand, 14, 14f),          // iron and vitamin
            Plant("berry", "Quả Than Hồng", FoodKind.EmberBerry, ForageHabitat.MidBand, 10, 16f, 200f),  // iron
            Plant("fatcap", "Nấm Béo", FoodKind.FatMushroom, ForageHabitat.WallFoot, 12, 18f),            // fat
            Plant("pod", "Túi Bào Tử", FoodKind.SporePod, ForageHabitat.WallFoot, 10, 14f),               // calcium and vitamin
            Mineral("quartz", "Thạch Anh Sáng", FoodKind.GlowQuartz, 8),                                  // iron and vitamin
            Mineral("lime", "Đá Vôi", FoodKind.LimeCrystal, 4),                                           // calcium
            Mineral("iron", "Quặng Sắt", FoodKind.IronNodule, 4),                                         // iron
            Mineral("salt", "Muối Đá", FoodKind.SaltCrust, 4),                                            // calcium, a little iron and vitamin
        };

        public static ForageSpec ByFood(FoodKind food)
        {
            foreach (var s in Specs) if (s.Food == food) return s;
            return null;
        }

        /// <summary>How well a habitat suits a point: 0 (no) .. 1 (yes), clumped by a little noise where the plants grow in patches.</summary>
        public static float Density(ForageHabitat habitat, float x, float z)
        {
            float depth = CavernLayout.DepthAt(x, z);
            float d = CavernLayout.ShoreOffset(x, z);
            float r = CavernLayout.RadiusFromCenter(x, z);
            float clump = 0.35f + 0.65f * ZoneLayout.Noise(x / 6f + 90f, z / 6f + 44f);
            switch (habitat)
            {
                case ForageHabitat.Shallows: return depth > 0.08f && depth < 0.42f ? 1f : 0f;
                case ForageHabitat.ShoreWater: return depth > 0f ? (depth < 0.35f ? 1f : 0f) : (d < 2.5f ? 1f : 0f);
                case ForageHabitat.WetEdge: return depth <= 0f && d < 7f ? clump : 0f;
                case ForageHabitat.ShoreBand: return depth <= 0f && d >= 3f && d <= 14f ? 1f : 0f;
                case ForageHabitat.MidBand: return depth <= 0f && d >= 7f && d <= 24f ? clump : 0f;
                case ForageHabitat.WallFoot: return depth <= 0f && r >= 24f && r <= 38f && CavernLayout.GroundHeight(x, z) < 6f ? clump : 0f;
                default: return 0f;
            }
        }

        /// <summary>The ore sites (the rare clusters of CavernLayout.OreDensity) and which mineral each one is: quartz first, then lime, iron, salt.</summary>
        public static List<(Vector3 pos, ForageSpec spec)> PlaceOre(int seed = 27)
        {
            var sites = CavernLayout.ScatterSpaced(seed, 20000, CavernLayout.OreDensity, 10f, 20);
            // the minerals take turns (quartz most often), so even with few sites every kind shows up
            var order = new List<(ForageSpec spec, float turn)>();
            foreach (var spec in Specs)
                if (spec.Mineral) for (int k = 0; k < spec.Count; k++) order.Add((spec, (k + 0.5f) / spec.Count));
            order.Sort((a, b) => a.turn != b.turn ? a.turn.CompareTo(b.turn) : string.CompareOrdinal(a.spec.Id, b.spec.Id));
            var result = new List<(Vector3, ForageSpec)>();
            for (int i = 0; i < sites.Count && i < order.Count; i++) result.Add((sites[i], order[i].spec));
            return result;
        }

        /// <summary>Every edible thing in the cavern: the minerals on their ore sites, then each plant kind in its habitat, never closer than 2.2 m to another.</summary>
        public static List<(Vector3 pos, ForageSpec spec)> PlaceAll(int seed = 5151)
        {
            var result = PlaceOre();
            var rnd = new System.Random(seed);
            const float spacing = 2.2f;
            foreach (var spec in Specs)
            {
                if (spec.Mineral) continue;
                int got = 0;
                for (int attempt = 0; attempt < 12000 && got < spec.Count; attempt++)
                {
                    float x = ((float)rnd.NextDouble() * 2f - 1f) * 44f, z = ((float)rnd.NextDouble() * 2f - 1f) * 44f;
                    if (rnd.NextDouble() >= Density(spec.Habitat, x, z)) continue;
                    bool clear = true;
                    foreach (var (p, _) in result)
                        if ((p.x - x) * (p.x - x) + (p.z - z) * (p.z - z) < spacing * spacing) { clear = false; break; }
                    if (!clear) continue;
                    float y = spec.Habitat == ForageHabitat.Shallows ? CavernLayout.WaterLevel : CavernLayout.GroundHeight(x, z);
                    result.Add((new Vector3(x, y, z), spec));
                    got++;
                }
            }
            return result;
        }
    }
}
