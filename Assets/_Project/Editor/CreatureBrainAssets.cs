using System.IO;
using System.Linq;
using Evolve.Creatures;
using Evolve.Ecology;
using Evolve.Organs;
using Evolve.Swallow;
using UnityEditor;
using UnityEngine;

namespace Evolve.EditorTools
{
    /// <summary>
    /// Evolve → Attach Creature Brains: puts CreatureBrain on every wildlife prefab (Assets/_Project/Prefabs/Wildlife) and PreyDefense on the prey that have
    /// a defence in PreyDefenseSeed, and fills EscapeChance on their SpeciesSO (the hunting model uses it). Species are matched by name in any spelling
    /// ("ThanLanBoHo" or "Thằn Lằn Bờ Hồ"). Also puts each species' drop table (the gene of its cave skill, made by Evolve → Create Cave Skills) in the `drops`
    /// slot of its Prey / Predator: the 12 species of the first three caves so far, the others stay empty until their skills exist. Run Create Cave Skills first. Safe to run again.
    /// </summary>
    public static class CreatureBrainAssets
    {
        const string PrefabFolder = "Assets/_Project/Prefabs/Wildlife";
        const string DropFolder = "Assets/_Project/Resources/CaveSkills";
        const string SpeciesFolder = "Assets/_Project/ScriptableObjects/Ecology/Species";

        [MenuItem("Evolve/Attach Creature Brains")]
        public static void Attach()
        {
            int brains = 0, defenses = 0, skipped = 0, species = 0, drops = 0, noTable = 0;
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
                        var row = CaveSkillSeed.All.FirstOrDefault(d => SpeciesNames.Key(d.Species) == SpeciesNames.Key(root.name));
                        if (row != null)
                        {
                            var table = AssetDatabase.LoadAssetAtPath<DropTableSO>($"{DropFolder}/DropTable_{row.Id.Replace("skill.", "")}.asset");
                            Component comp = root.GetComponent<Prey>();
                            if (comp == null) comp = root.GetComponent<Predator>();
                            if (table == null) noTable++;
                            else
                            {
                                var so = new SerializedObject(comp);
                                var prop = so.FindProperty("drops");
                                if (prop != null && prop.objectReferenceValue != table) { prop.objectReferenceValue = table; so.ApplyModifiedPropertiesWithoutUndo(); drops++; changed = true; }
                            }
                        }
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
            if (noTable > 0) Debug.LogWarning($"{noTable} loài chưa có bảng rơi: chạy Evolve → Create Cave Skills trước rồi chạy lại menu này.");
            Debug.Log($"CreatureBrain gắn vào {brains} prefab, PreyDefense vào {defenses}, bảng rơi gene vào {drops} prefab, bỏ qua {skipped} prefab không phải sinh vật; điền xác suất thoát cho {species} loài.");
        }
    }
}
