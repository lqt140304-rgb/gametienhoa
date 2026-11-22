using System.IO;
using UnityEditor;
using UnityEngine;

namespace Evolve.EditorTools
{
    /// <summary>
    /// Menu Evolve → Build Golem Axe Prefabs. Makes two prefabs of the giant axe of Golem Thạch Anh (a placeholder built in Blender by tools/blender/build_golem_axe.py: 3 m long, the head 1.1 m across):
    /// "GolemAxe_Planted" (driven into the heart stone, the origin is the middle of the cutting edge: put it on the map's Axe_Anchor, at floor height, and it stands in the stone) and "GolemAxe"
    /// (upright, the way the golem holds it; it has empties Grip, Grip2 and Embed_Point). Both have no colliders: the player must not be able to touch or block on the axe. Materials: vertex-coloured
    /// (Evolve/RockVertexColor), the crystals glowing. Run with Play stopped.
    /// </summary>
    public static class GolemAxeBuilder
    {
        const string ModelDir = "Assets/_Project/Models/Props/GolemAxe", PrefabDir = "Assets/_Project/Resources/Props", MatDir = "Assets/_Project/Materials";

        [MenuItem("Evolve/Build Golem Axe Prefabs")]
        static void Build()
        {
            if (EditorApplication.isPlaying) { Debug.LogError("[Rìu Golem] Dừng Play trước khi chạy menu này."); return; }
            var steel = Upsert(MatDir + "/GolemAxe_Steel.mat", 0f);
            var crystal = Upsert(MatDir + "/GolemAxe_Crystal.mat", 0.9f);
            Directory.CreateDirectory(PrefabDir);
            AssetDatabase.Refresh();
            foreach (var (fbx, prefab) in new[] { ("GolemAxe_Planted", "GolemAxe_Planted"), ("GolemAxe", "GolemAxe") })
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelDir}/{fbx}.fbx");
                if (model == null) { Debug.LogError($"[Rìu Golem] Chưa có {ModelDir}/{fbx}.fbx"); continue; }
                var root = (GameObject)PrefabUtility.InstantiatePrefab(model);
                root.name = prefab;
                foreach (var r in root.GetComponentsInChildren<MeshRenderer>(true)) r.sharedMaterial = r.name.Contains("Crystal") ? crystal : steel;
                foreach (var c in root.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);   // nobody may touch or stand on the axe
                PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabDir}/{prefab}.prefab");
                Object.DestroyImmediate(root);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Rìu Golem] Đã tạo " + PrefabDir + "/GolemAxe_Planted.prefab và GolemAxe.prefab (đặt Planted ở Axe_Anchor, y = mặt sàn).");
        }

        static Material Upsert(string path, float glow)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                var shader = Shader.Find("Evolve/RockVertexColor") ?? Shader.Find("Universal Render Pipeline/Lit");
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            if (mat.HasProperty("_Tint")) mat.SetColor("_Tint", Color.white);
            if (mat.HasProperty("_Fill")) mat.SetFloat("_Fill", 0.3f);
            if (mat.HasProperty("_Glow")) mat.SetFloat("_Glow", glow);
            EditorUtility.SetDirty(mat);
            return mat;
        }
    }
}
