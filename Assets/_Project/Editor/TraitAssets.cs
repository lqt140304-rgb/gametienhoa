using Evolve.Traits;
using UnityEditor;
using UnityEngine;

namespace Evolve.EditorTools
{
    /// <summary>
    /// Evolve → Create Traits: makes (or updates in place) the 12 core and 12 flaw assets in Assets/_Project/Resources/Traits
    /// from the table in <see cref="TraitSeed"/>. Running it again never duplicates anything.
    /// </summary>
    public static class TraitAssets
    {
        const string Folder = "Assets/_Project/Resources/Traits";

        [MenuItem("Evolve/Create Traits")]
        public static void Create()
        {
            PrototypeSceneBuilder.EnsureFolder("Assets/_Project/Resources");
            PrototypeSceneBuilder.EnsureFolder(Folder);

            int flaws = 0, cores = 0;
            foreach (var def in TraitSeed.Flaws)
            {
                var so = LoadOrCreate<FlawSO>($"{Folder}/Flaw_{def.Id}.asset");
                so.Id = def.Id;
                so.DisplayName = def.Name;
                so.Description = def.Description;
                so.CompensatedBy = def.CompensatedBy;
                so.Implemented = def.Implemented;
                EditorUtility.SetDirty(so);
                flaws++;
            }
            for (int i = 0; i < TraitSeed.Cores.Count; i++)
            {
                var def = TraitSeed.Cores[i];
                var so = LoadOrCreate<CoreSO>($"{Folder}/Core_{def.Id}.asset");
                so.Id = def.Id;
                so.DisplayName = def.Name;
                so.Summary = def.Summary;
                so.Detail = def.Detail;
                so.OrbColor = Color.HSVToRGB(i / (float)TraitSeed.Cores.Count, 0.55f, 0.95f);   // placeholder orb: one hue per core
                so.Heavy = def.Heavy;
                so.Implemented = def.Implemented;
                so.Options = new FlawOption[def.Options.Length];
                for (int k = 0; k < def.Options.Length; k++) so.Options[k] = new FlawOption { FlawIds = def.Options[k] };
                EditorUtility.SetDirty(so);
                cores++;
            }
            AssetDatabase.SaveAssets();
            TraitCatalog.Reload();
            Debug.Log($"Đã tạo hoặc cập nhật {cores} Lõi và {flaws} Khuyết Điểm trong {Folder}.");
        }

        static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;
            var created = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(created, path);
            return created;
        }
    }
}
