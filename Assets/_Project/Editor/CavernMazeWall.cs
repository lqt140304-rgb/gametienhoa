using Evolve.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Evolve.EditorTools
{
    /// <summary>
    /// Evolve -> Dress Cavern: Maze Wall & Flood Ledges.
    /// Bổ sung lên vách Hang Khởi Sinh bằng đá tự nhiên (Cliff/Rock + ArchTunnel, không dùng Cube thô):
    /// gờ đá trú lũ (>= 5.4m), khe nứt vách đá, và miệng hầm nhiều cỡ (khe nhỏ, hầm vừa, hầm lớn).
    /// </summary>
    public static class CavernMazeWall
    {
        const string Root = "Assets/_Project";
        const string ModelFolder = Root + "/Models/Underroot/";
        const string RootName = "Cavern Maze Wall";

        [MenuItem("Evolve/Dress Cavern: Maze Wall & Flood Ledges")]
        public static void Dress()
        {
            PropInstaller.EnsureRocks();
            PropInstaller.EnsureLandmarks();

            var old = GameObject.Find(RootName);
            if (old != null) Object.DestroyImmediate(old);

            var root = new GameObject(RootName).transform;
            var rockMat = LoadOrMakeRockMaterial();
            var darkMat = LoadOrMakeDarkInteriorMaterial();

            // 1. Các gờ đá trú lũ tự nhiên (cao >= 5.5m, ghép từ tảng đá & bậc đá ăn khớp với vách)
            BuildNaturalFloodLedge(root, "Gờ Trú Lũ Đông", 20f, 34f, 5.6f, rockMat, seed: 11);
            BuildNaturalFloodLedge(root, "Gờ Trú Lũ Bắc", 110f, 34f, 5.8f, rockMat, seed: 22);
            BuildNaturalFloodLedge(root, "Gờ Trú Lũ Tây Bắc", 160f, 34f, 5.6f, rockMat, seed: 33);

            // 2. Các khe nứt tự nhiên trên vách hang (ghép từ các khối Cliff)
            BuildNaturalWallCrack(root, "Khe Nứt Đông Bắc", 58f, 37.5f, 2.5f, rockMat, darkMat, seed: 44);
            BuildNaturalWallCrack(root, "Khe Nứt Tây", 188f, 37.5f, 2.0f, rockMat, darkMat, seed: 55);
            BuildNaturalWallCrack(root, "Khe Nứt Nam", 295f, 37.5f, 3.0f, rockMat, darkMat, seed: 66);

            // 3. Miệng hầm nhiều cỡ quanh vách Hang Khởi Sinh (vòm đá ArchTunnel + tảng đá viền cửa)
            BuildTunnelMouthPreview(root, "Miệng Hầm Lớn (Đông Nam)", 320f, 35.5f, 0.2f, 4.4f, 3.8f, rockMat, darkMat);
            BuildTunnelMouthPreview(root, "Miệng Hầm Vừa (Bắc)", 82f, 35.5f, 0.3f, 2.6f, 2.4f, rockMat, darkMat);
            BuildTunnelMouthPreview(root, "Khe Hầm Nhỏ (Tây)", 196f, 35.2f, 0.2f, 1.15f, 1.05f, rockMat, darkMat);

            EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
            Debug.Log("Vách Hang Khởi Sinh: đã dựng 3 gờ đá trú lũ tự nhiên, 3 khe nứt vách và 3 miệng hầm nhiều cỡ (không khối hộp thô).");
        }

        static void BuildNaturalFloodLedge(Transform parent, string name, float angleDeg, float radius, float height, Material rockMat, int seed)
        {
            float rad = angleDeg * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad));
            Vector3 pos = dir * radius;
            pos.y = CavernLayout.GroundHeight(pos.x, pos.z) + height;

            var ledge = new GameObject(name);
            ledge.transform.SetParent(parent, false);
            ledge.transform.position = pos;
            ledge.transform.rotation = Quaternion.LookRotation(-dir, Vector3.up);

            // Đĩa đá tự nhiên làm mặt gờ đứng tránh lũ
            var topGo = new GameObject("Đỉnh Đá Trú Lũ");
            topGo.transform.SetParent(ledge.transform, false);
            topGo.transform.localPosition = new Vector3(0f, -0.35f, 0f);
            var discMesh = CaveMeshes.Disc(3.8f, 0.45f, 14);
            topGo.AddComponent<MeshFilter>().sharedMesh = discMesh;
            topGo.AddComponent<MeshRenderer>().sharedMaterial = rockMat;
            topGo.AddComponent<MeshCollider>().sharedMesh = discMesh;

            // Tảng đá chống bên dưới gờ để cắm vào vách đất
            MazeTerrainMesh.PlaceFramingBoulder(ledge.transform, new Vector3(0f, -height * 0.55f, 0f), 4.6f, seed + 100, rockMat);

            // Các bậc đá tự nhiên leo từ sàn hang lên đỉnh gờ
            for (int i = 0; i < 5; i++)
            {
                float t = i / 4f;
                Vector3 stepPos = new Vector3(
                    (i % 2 == 0 ? -0.45f : 0.45f),
                    Mathf.Lerp(-height + 0.2f, -0.55f, t),
                    Mathf.Lerp(6.0f, 1.8f, t));
                MazeTerrainMesh.PlaceFramingBoulder(ledge.transform, stepPos, 2.1f - 0.15f * i, seed + i, rockMat);
            }
        }

        static void BuildNaturalWallCrack(Transform parent, string name, float angleDeg, float radius, float baseY, Material rockMat, Material darkMat, int seed)
        {
            float rad = angleDeg * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad));
            Vector3 pos = dir * radius;
            pos.y = baseY;

            var crack = new GameObject(name);
            crack.transform.SetParent(parent, false);
            crack.transform.position = pos;
            crack.transform.rotation = Quaternion.LookRotation(-dir, Vector3.up);

            var cliffPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFolder + $"Cliff_{seed % 4}.fbx");
            if (cliffPrefab != null)
            {
                var left = (GameObject)PrefabUtility.InstantiatePrefab(cliffPrefab, crack.transform);
                left.name = "Vách Nứt Trái";
                left.transform.localPosition = new Vector3(-1.35f, 0f, 0f);
                left.transform.localRotation = Quaternion.Euler(0f, 15f, -8f);
                left.transform.localScale = new Vector3(0.65f, 1.2f, 0.65f);

                var right = (GameObject)PrefabUtility.InstantiatePrefab(cliffPrefab, crack.transform);
                right.name = "Vách Nứt Phải";
                right.transform.localPosition = new Vector3(1.35f, 0f, 0f);
                right.transform.localRotation = Quaternion.Euler(0f, 195f, 8f);
                right.transform.localScale = new Vector3(0.65f, 1.2f, 0.65f);
            }
            else
            {
                MazeTerrainMesh.PlaceFramingBoulder(crack.transform, new Vector3(-1.1f, 2.0f, 0f), 2.5f, seed + 1, rockMat);
                MazeTerrainMesh.PlaceFramingBoulder(crack.transform, new Vector3(1.1f, 2.0f, 0f), 2.5f, seed + 2, rockMat);
            }

            var core = new GameObject("Lõi Tối Khe Nứt");
            core.transform.SetParent(crack.transform, false);
            core.transform.localPosition = new Vector3(0f, 2.8f, -0.7f);
            core.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            core.AddComponent<MeshFilter>().sharedMesh = CaveMeshes.Disc(1.2f, 0.25f, 10);
            core.AddComponent<MeshRenderer>().sharedMaterial = darkMat;
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
