using System.IO;
using Evolve.Creatures;
using Evolve.Ecology;
using UnityEditor;
using UnityEngine;

namespace Evolve.EditorTools
{
    /// <summary>
    /// Evolve → Attach Creature Brains: puts CreatureBrain on every wildlife prefab (Assets/_Project/Prefabs/Wildlife) and PreyDefense on the prey that have
    /// a defence in PreyDefenseSeed, and fills EscapeChance on their SpeciesSO (the hunting model uses it). Species are matched by name in any spelling
    /// ("ThanLanBoHo" or "Thằn Lằn Bờ Hồ"). Safe to run again.
    /// </summary>
    public static class CreatureBrainAssets
    {
        const string PrefabFolder = "Assets/_Project/Prefabs/Wildlife";
        const string SpeciesFolder = "Assets/_Project/ScriptableObjects/Ecology/Species";

        [MenuItem("Evolve/Attach Creature Brains")]
        public static void Attach()
        {
            int brains = 0, defenses = 0, skipped = 0, species = 0;
            if (Directory.Exists(PrefabFolder))
                foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolder }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var root = PrefabUtility.LoadPrefabContents(path);
                    try
                    {
                        bool isPrey = root.GetComponent<Prey>() != null, isHunter = root.GetComponent<Predator>() != null;
                        if (!isPrey && !isHunter) { skipped++; continue; }
                        bool changed = false;
                        if (root.GetComponent<CreatureBrain>() == null) { root.AddComponent<CreatureBrain>(); brains++; changed = true; }
                        if (isPrey && root.GetComponent<PreyDefense>() == null && PreyDefenseSeed.FindBySpecies(root.name) != null) { root.AddComponent<PreyDefense>(); defenses++; changed = true; }
                        if (changed) PrefabUtility.SaveAsPrefabAsset(root, path);
                    }
                    finally { PrefabUtility.UnloadPrefabContents(root); }
                }

            if (Directory.Exists(SpeciesFolder))
                foreach (var guid in AssetDatabase.FindAssets("t:SpeciesSO", new[] { SpeciesFolder }))
                {
                    var so = AssetDatabase.LoadAssetAtPath<SpeciesSO>(AssetDatabase.GUIDToAssetPath(guid));
                    var def = so != null ? (PreyDefenseSeed.FindBySpecies(so.Id) ?? PreyDefenseSeed.FindBySpecies(so.name)) : null;
                    if (def == null) continue;
                    so.EscapeChance = def.EscapeChance;
                    EditorUtility.SetDirty(so);
                    species++;
                }
            AssetDatabase.SaveAssets();
            Debug.Log($"CreatureBrain gắn vào {brains} prefab, PreyDefense vào {defenses}, bỏ qua {skipped} prefab không phải sinh vật; điền xác suất thoát cho {species} loài.");
        }
    }
}
