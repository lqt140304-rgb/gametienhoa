using System.Collections.Generic;
using Evolve.Organs;
using Evolve.Survival;
using Evolve.Swallow;
using Evolve.Visuals;
using UnityEngine;

namespace Evolve.Traits
{
    /// <summary>One thing handed to the Túi Không Đáy by an encounter (a dead predecessor's belongings).</summary>
    public struct EncounterItem
    {
        public string Label;
        public float Mass, Nutrition;
        public FoodKind Food;
    }

    /// <summary>
    /// What a cave encounter hands out (spec 2026-11-20 section 3): the cave team calls these when an encounter's condition is met.
    /// Every call works on the player's own machine: <paramref name="player"/> is the local player (see <see cref="LocalPlayer"/>); a remote
    /// avatar is ignored (it gets its own rewards on its own machine). Each returns what it gave (null / false: nothing happened) so the encounter
    /// can fall back or stay quiet. The decisions are in EncounterRewardRules (tested).
    /// </summary>
    public static class EncounterRewards
    {
        /// <summary>The local player's body, or null.</summary>
        public static GameObject LocalPlayer => TraitNet.Local != null ? TraitNet.Local.gameObject : null;

        static List<OrganSO> geneCache;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => geneCache = null;

        static bool IsLocal(GameObject player) => player != null && !(player.TryGetComponent(out SurvivalStats s) && s.IsRemote);

        /// <summary>Every gene that can be handed out: the organ assets in any Resources folder (cave skills are in Resources/CaveSkills).</summary>
        static List<OrganSO> AllGenes()
        {
            if (geneCache != null) return geneCache;
            geneCache = new List<OrganSO>();
            var seen = new HashSet<string>();
            foreach (var o in Resources.LoadAll<OrganSO>(""))
                if (o != null && o.Use != GeneUse.None && !string.IsNullOrEmpty(o.name) && seen.Add(o.name)) geneCache.Add(o);
            return geneCache;
        }

        /// <summary>Learns a gene at once (a guaranteed Giải Mã result). Note: it counts as analyses done, so it also raises the decoder level a little.</summary>
        static bool Learn(Swallower swallower, OrganSO organ)
        {
            var genes = new List<Gene> { new Gene(organ, AnalysisRules.FromTier(organ.Tier)) };
            for (int i = 0; i < 60 && !swallower.Book.Knows(organ); i++) swallower.Book.Submit(genes, 1f, () => 0f);
            return swallower.Book.Knows(organ);
        }

        static int Seed(int seed) => seed != 0 ? seed : Random.Range(1, int.MaxValue);

        /// <summary>Teaches a gene the player does not know yet, of at least this rarity. Returns its name, or null when none is left (then give mass instead).</summary>
        public static string GrantGene(GameObject player, GeneRarity minRarity, int seed = 0)
        {
            if (!IsLocal(player) || !player.TryGetComponent(out Swallower swallower)) return null;
            var all = AllGenes();
            var pool = new List<GeneCandidate>(all.Count);
            foreach (var o in all) pool.Add(new GeneCandidate(o.name, AnalysisRules.FromTier(o.Tier)));
            var known = new HashSet<string>();
            foreach (var o in swallower.Book.Known) known.Add(o.name);
            string id = EncounterRewardRules.PickGene(pool, known, minRarity, Seed(seed));
            var organ = id == null ? null : all.Find(o => o.name == id);
            return organ != null && Learn(swallower, organ) ? organ.DisplayName : null;
        }

        /// <summary>Teaches the encounter's own gene (an asset named <paramref name="id"/> in Resources/EncounterGenes). Null when missing or already known.</summary>
        public static string GrantUniqueGene(GameObject player, string id)
        {
            if (!IsLocal(player) || !player.TryGetComponent(out Swallower swallower)) return null;
            foreach (var o in Resources.LoadAll<OrganSO>("EncounterGenes"))
                if (o != null && o.name == id) return !swallower.Book.Knows(o) && Learn(swallower, o) ? o.DisplayName : null;
            return null;
        }

        /// <summary>Adds body mass (kg).</summary>
        public static bool AddMass(GameObject player, float kg)
        {
            if (!IsLocal(player) || kg <= 0f || !player.TryGetComponent(out SlimeBody body)) return false;
            body.AddMass(kg);
            return true;
        }

        /// <summary>
        /// Re-draws one flaw at random: never the same one again, can be worse. Co-op: the host is told like at birth. Returns false when nothing changed
        /// (no flaws, or nothing else to draw).
        /// </summary>
        public static bool RerollFlaw(GameObject player, int seed = 0)
        {
            if (!IsLocal(player) || !player.TryGetComponent(out PlayerTraits traits)) return false;
            var pool = new List<string>();
            foreach (var f in TraitSeed.Flaws) if (f.Implemented) pool.Add(f.Id);   // a flaw with no effect yet would be a free gift
            var current = new List<string>(traits.FlawIds).ToArray();
            var next = EncounterRewardRules.RerollFlaw(current, pool, Seed(seed));
            if (System.Linq.Enumerable.SequenceEqual(current, next)) return false;
            traits.Set(traits.CoreId, next);
            if (TraitNet.IsClient) TraitNet.ClientPick?.Invoke(traits.CoreId, next);
            return true;
        }

        /// <summary>Tôi Luyện: more heat resistance for the rest of the round (never above +20 °C in total).</summary>
        public static bool AddHeatResistance(GameObject player, float degrees)
        {
            if (!IsLocal(player) || !player.TryGetComponent(out SurvivalStats stats) || degrees <= 0f) return false;
            float before = stats.State.BonusHeatResistance;
            stats.State.AddBonusHeat(degrees);
            return stats.State.BonusHeatResistance > before;
        }

        /// <summary>Health, hunger, energy and thirst full again.</summary>
        public static bool RefillStats(GameObject player)
        {
            if (!IsLocal(player) || !player.TryGetComponent(out SurvivalStats stats) || stats.State.IsDead) return false;
            stats.State.RefillAll();
            return true;
        }

        public static bool CureDiseases(GameObject player)
        {
            if (!IsLocal(player) || !player.TryGetComponent(out SurvivalStats stats)) return false;
            stats.State.CureAll();
            return true;
        }

        /// <summary>The core a little stronger for the rest of the round (up to three times). False when already at the top or without a core.</summary>
        public static bool BoostCore(GameObject player)
        {
            return IsLocal(player) && player.TryGetComponent(out PlayerTraits traits) && traits.CoreId != null && traits.AddCoreBoost();
        }

        /// <summary>Unlocks a special slime colour for good (<see cref="ColorUnlocks.Rainbow"/>): it shows in the colour menu (F3). True when new.</summary>
        public static bool UnlockColor(GameObject player, string colorId) => IsLocal(player) && SlimeColor.Unlock(colorId);

        /// <summary>Puts things in the Túi; returns how many fitted (the rest are lost: the Túi was full).</summary>
        public static int GiveStomachItems(GameObject player, IEnumerable<EncounterItem> items)
        {
            if (!IsLocal(player) || items == null || !player.TryGetComponent(out Swallower swallower)) return 0;
            int added = 0;
            foreach (var it in items)
                if (swallower.Stomach.TryAdd(new StomachItem { Label = it.Label, Mass = it.Mass, Nutrition = it.Nutrition, Food = it.Food, Mode = StomachMode.Digest })) added++;
            return added;
        }

        /// <summary>Tiếng Vọng says one line (Vietnamese).</summary>
        public static void Say(string line) => Echo.Say(line);
    }
}
