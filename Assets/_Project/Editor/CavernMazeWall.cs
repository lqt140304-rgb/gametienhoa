using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Evolve.EditorTools
{
    /// <summary>
    /// Evolve -> Dress Cavern: Maze Wall & Flood Ledges.
    /// Dọn dẹp sạch phần "Cavern Maze Wall" cũ (đã bỏ gờ lũ, khe nứt và tránh trùng lặp miệng hang với MazeBuilder).
    /// </summary>
    public static class CavernMazeWall
    {
        const string Root = "Assets/_Project";
        const string RootName = "Cavern Maze Wall";

        [MenuItem("Evolve/Dress Cavern: Maze Wall & Flood Ledges")]
        public static void Dress()
        {
            var old = GameObject.Find(RootName);
            if (old != null) Object.DestroyImmediate(old);

            var scene = EditorSceneManager.GetActiveScene();
            if (scene.IsValid()) EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("Đã dọn sạch Cavern Maze Wall cũ (miệng hang nối sẽ được dựng đồng bộ trong Evolve -> Build Maze World (Step 3)).");
        }

        public static Transform BuildTunnelMouthPreview(
            Transform parent,
            string name,
            float angleDeg,
            float radius,
            float baseY,
            float width,
            float height,
            Material rockMat,
            Material darkMat)
        {
            return MazeTerrainMesh.BuildNaturalTunnelMouth(
                parent,
                name,
                angleDeg,
                radius,
                baseY,
                width,
                height,
                rockMat,
                darkMat,
                seed: Mathf.RoundToInt(angleDeg) + 77);
        }

        public static Material LoadOrMakeRockMaterial()
        {
            return MazeTerrainMesh.TexturedSurfaceMaterial(
                "Maze_CavernRock",
                new Color(0.38f, 0.36f, 0.33f),
                smoothness: 0.08f,
                twoSided: true,
                texFile: "Cavern_Rock_v3.png");
        }

        public static Material LoadOrMakeDarkInteriorMaterial()
        {
            const string path = Root + "/Materials/Maze_DarkInterior.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.SetColor("_BaseColor", new Color(0.02f, 0.02f, 0.03f));
            m.SetFloat("_Smoothness", 0f);
            AssetDatabase.CreateAsset(m, path);
            return m;
        }
    }
}
