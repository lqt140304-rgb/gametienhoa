using System.Collections.Generic;
using Evolve.World;
using NUnit.Framework;
using UnityEngine;

namespace Evolve.Tests.EditMode
{
    public class PlantCommunitiesTests
    {
        static Dictionary<Plant, int> Count(Vector2 centre, float radius, int samples, int seed)
        {
            var counts = new Dictionary<Plant, int>();
            var rnd = new System.Random(seed);
            for (int i = 0; i < samples; i++)
            {
                float a = (float)rnd.NextDouble() * Mathf.PI * 2f, r = radius * Mathf.Sqrt((float)rnd.NextDouble());
                var p = centre + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                var plant = PlantCommunities.Pick(p.x, p.y, rnd.NextDouble());
                if (plant == null) continue;
                counts.TryGetValue(plant.Value, out int c);
                counts[plant.Value] = c + 1;
            }
            return counts;
        }

        static Plant MostPicked(Dictionary<Plant, int> counts)
        {
            Plant best = Plant.WetMoss; int n = -1;
            foreach (var kv in counts) if (kv.Value > n) { n = kv.Value; best = kv.Key; }
            return best;
        }

        [Test] public void CreamMushroomsRuleTheSporeGarden()
        {
            Assert.AreEqual(Plant.CreamMushroom, MostPicked(Count(ZoneLayout.Spore.Center, 30f, 2000, 1)));
        }

        [Test] public void LeavesRuleTheHumusBedAndBareMudTheHuntingEdge()
        {
            Assert.AreEqual(Plant.LeafLitter, MostPicked(Count(ZoneLayout.Humus.Center, 30f, 2000, 2)));
            Assert.AreEqual(Plant.BareMud, MostPicked(Count(ZoneLayout.HuntEdge.Center, 25f, 2000, 3)));
        }

        [Test] public void CommunitiesBlendBetweenNeighbours()
        {
            // across the gap between the humus bed (03) and the hunting edge (06) both communities show up
            var a = ZoneLayout.Humus.Center; var b = ZoneLayout.HuntEdge.Center;
            var counts = new Dictionary<Plant, int>();
            var rnd = new System.Random(4);
            for (int i = 0; i < 4000; i++)
            {
                var p = Vector2.Lerp(a, b, 0.25f + 0.5f * (float)rnd.NextDouble()) + new Vector2((float)rnd.NextDouble() - 0.5f, (float)rnd.NextDouble() - 0.5f) * 30f;
                var plant = PlantCommunities.Pick(p.x, p.y, rnd.NextDouble());
                if (plant != null) { counts.TryGetValue(plant.Value, out int c); counts[plant.Value] = c + 1; }
            }
            Assert.IsTrue(counts.ContainsKey(Plant.LeafLitter), "leaves from the humus bed");
            Assert.IsTrue(counts.ContainsKey(Plant.BareMud), "bare mud from the hunting edge");
        }

        [Test] public void NothingGrowsInTheWater()
        {
            var c = ZoneLayout.LakeCenter;
            for (int i = 0; i < PlantCommunities.PlantCount; i++) Assert.AreEqual(0f, PlantCommunities.Density((Plant)i, c.x, c.y));
            Assert.IsNull(PlantCommunities.Pick(c.x, c.y, 0.0));
        }

        [Test] public void PlantsGrowInPatchesWithGaps()
        {
            var c = ZoneLayout.Spore.Center;
            int low = 0, n = 0;
            for (float dx = -30f; dx <= 30f; dx += 2f)
                for (float dz = -30f; dz <= 30f; dz += 2f)
                {
                    n++;
                    if (PlantCommunities.Density(Plant.CreamMushroom, c.x + dx, c.y + dz) < 0.1f) low++;
                }
            Assert.GreaterOrEqual(low / (float)n, 0.2f, "there are gaps between the mushroom clusters");
            Assert.Less(low / (float)n, 0.95f, "but there are clusters");
        }

        [Test] public void SameSpotSamePlant()
        {
            Assert.AreEqual(PlantCommunities.Pick(100f, 90f, 0.3), PlantCommunities.Pick(100f, 90f, 0.3));
        }
    }
}
