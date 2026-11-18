using System.Collections.Generic;
using UnityEngine;

namespace Evolve.Traits
{
    /// <summary>Finds the core and flaw assets in Resources/Traits (made by Evolve → Create Traits). No assets = empty lists, never an error.</summary>
    public static class TraitCatalog
    {
        static List<CoreSO> cores;
        static Dictionary<string, CoreSO> coreById;
        static Dictionary<string, FlawSO> flawById;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Reload();

        public static void Reload()
        {
            cores = null; coreById = null; flawById = null;
        }

        static void Load()
        {
            if (cores != null) return;
            cores = new List<CoreSO>(Resources.LoadAll<CoreSO>("Traits"));
            cores.RemoveAll(c => c == null || string.IsNullOrEmpty(c.Id));
            cores.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            coreById = new Dictionary<string, CoreSO>();
            foreach (var c in cores) coreById[c.Id] = c;
            flawById = new Dictionary<string, FlawSO>();
            foreach (var f in Resources.LoadAll<FlawSO>("Traits"))
                if (f != null && !string.IsNullOrEmpty(f.Id)) flawById[f.Id] = f;
        }

        public static IReadOnlyList<CoreSO> Cores { get { Load(); return cores; } }

        public static CoreSO Core(string id)
        {
            Load();
            return id != null && coreById.TryGetValue(id, out var c) ? c : null;
        }

        public static FlawSO Flaw(string id)
        {
            Load();
            return id != null && flawById.TryGetValue(id, out var f) ? f : null;
        }
    }
}
