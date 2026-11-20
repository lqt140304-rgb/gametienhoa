using Evolve.Organs;
using UnityEditor;
using UnityEngine;

namespace Evolve.EditorTools
{
    /// <summary>
    /// Evolve → Create Encounter Genes: the genes only an encounter can give (EncounterRewards.GrantUniqueGene), as assets in Resources/EncounterGenes.
    /// These two are PLACEHOLDERS for the owner to rename and tune: Gene Lạ (from the egg of the Tổ Bỏ Hoang) and Gene Loài Cổ (from the banquet of the Nghĩa Địa Cự Thú).
    /// Running it again updates them in place.
    /// </summary>
    public static class EncounterGeneAssets
    {
        const string Folder = "Assets/_Project/Resources/EncounterGenes";

        [MenuItem("Evolve/Create Encounter Genes")]
        public static void Create()
        {
            PrototypeSceneBuilder.EnsureFolder("Assets/_Project/Resources");
            PrototypeSceneBuilder.EnsureFolder(Folder);
            Make("gene_la", "Gene Lạ", "Một dòng gene lạ nở ra từ quả trứng còn sống: thân sống dai hơn. (tạm, chờ chủ dự án chỉnh)", OrganTier.A, 15f, 0f);
            Make("gene_loai_co", "Gene Loài Cổ", "Gene của một loài khổng lồ đã tuyệt chủng: lớp thạch cứng hơn. (tạm, chờ chủ dự án chỉnh)", OrganTier.S, 0f, 0.12f);
            AssetDatabase.SaveAssets();
            Debug.Log("Đã tạo hoặc cập nhật 2 gene kỳ ngộ (tạm) trong " + Folder);
        }

        static void Make(string id, string name, string description, OrganTier tier, float maxHealth, float reduction)
        {
            string path = $"{Folder}/{id}.asset";
            var o = AssetDatabase.LoadAssetAtPath<OrganSO>(path);
            if (o == null) { o = ScriptableObject.CreateInstance<OrganSO>(); AssetDatabase.CreateAsset(o, path); }
            o.DisplayName = name; o.Description = description; o.Tier = tier; o.Slot = OrganSlot.Skin;
            o.DropWeight = 0f;   // never drops from a creature: only an encounter gives it
            o.MaxHealthBonus = maxHealth; o.DamageReduction = reduction;
            EditorUtility.SetDirty(o);
        }
    }
}
