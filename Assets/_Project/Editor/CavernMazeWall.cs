using Evolve.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Evolve.EditorTools
{
    /// <summary>
    /// Evolve -> Dress Cavern: Maze Wall & Flood Ledges.
    /// Bổ sung lên vách Hang Khởi Sinh: gờ đá trú lũ (>= 5.4m), khe nứt vách đá, và miệng hầm nhiều cỡ (khe nhỏ, hầm vừa, hầm lớn).
    /// Chạy lại được nhiều lần (xóa và dựng lại sạch sẽ).
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

            var root = new GameObject(RootName).transform;
            var rockMat = LoadOrMakeRockMaterial();
            var darkMat = LoadOrMakeDarkInteriorMaterial();

            // 1. Các gờ đá trú lũ (cao >= 5.4m, an toàn trước đỉnh lũ 4.5m) kèm dốc leo
            BuildFloodLedge(root, "Gờ Trú Lũ Đông", 20f, 36f, 5.5f, rockMat);
            BuildFloodLedge(root, "Gờ Trú Lũ Bắc", 110f, 36f, 5.8f, rockMat);
            BuildFloodLedge(root, "Gờ Trú Lũ Tây Bắc", 160f, 36f, 5.5f, rockMat);

            // 2. Các khe nứt trên vách hang
            BuildWallCrack(root, "Khe Nứt Đông Bắc", 58f, 38.5f, 3.2f, rockMat, darkMat);
            BuildWallCrack(root, "Khe Nứt Tây", 188f, 38.5f, 2.4f, rockMat, darkMat);
            BuildWallCrack(root, "Khe Nứt Nam", 295f, 38.5f, 4.0f, rockMat, darkMat);

            // 3. Miệng hầm nhiều cỡ quanh vách Hang Khởi Sinh
            // (hướng về tâm hồ, đặt sát chân vách ở bán kính ~35m)
            BuildTunnelMouthPreview(root, "Miệng Hầm Lớn (Đông Nam)", 320f, 35.5f, 0.4f, 4.4f, 3.8f, rockMat, darkMat);
            BuildTunnelMouthPreview(root, "Miệng Hầm Vừa (Bắc)", 82f, 35.5f, 0.5f, 2.6f, 2.4f, rockMat, darkMat);
            BuildTunnelMouthPreview(root, "Khe Hầm Nhỏ (Tây)", 196f, 35.2f, 0.3f, 1.15f, 1.05f, rockMat, darkMat);

            EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
            Debug.Log("Vách Hang Khởi Sinh: đã dựng 3 gờ đá trú lũ, 3 khe nứt vách và 3 miệng hầm nhiều cỡ (nhỏ, vừa, lớn).");
        }

        static void BuildFloodLedge(Transform parent, string name, float angleDeg, float radius, float height, Material rockMat)
        {
            float rad = angleDeg * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad));
            Vector3 pos = dir * radius;
            pos.y = height;

            var ledge = new GameObject(name);
            ledge.transform.SetParent(parent, false);
            ledge.transform.position = pos;
            ledge.transform.rotation = Quaternion.LookRotation(-dir, Vector3.up);

            // Bệ đá phẳng rộng để slime đứng tránh lũ
            CreateBox(ledge.transform, "Mặt Gờ An Toàn", new Vector3(0f, -0.35f, 0f), new Vector3(6.5f, 0.7f, 5.0f), Vector3.zero, rockMat);
            // Dốc đá nối xuống sàn
            CreateBox(ledge.transform, "Dốc Leo Gờ", new Vector3(0f, -height * 0.5f, 4.5f), new Vector3(2.8f, 0.5f, height * 1.45f), new Vector3(36f, 0f, 0f), rockMat);
        }

        static void BuildWallCrack(Transform parent, string name, float angleDeg, float radius, float baseY, Material rockMat, Material darkMat)
        {
            float rad = angleDeg * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad));
            Vector3 pos = dir * radius;
            pos.y = baseY;

            var crack = new GameObject(name);
            crack.transform.SetParent(parent, false);
            crack.transform.position = pos;
            crack.transform.rotation = Quaternion.LookRotation(-dir, Vector3.up);

            CreateBox(crack.transform, "Mép Nứt Trái", new Vector3(-0.75f, 3.0f, 0f), new Vector3(0.9f, 7.2f, 2.2f), new Vector3(0f, 0f, 7f), rockMat);
            CreateBox(crack.transform, "Mép Nứt Phải", new Vector3(0.75f, 3.0f, 0f), new Vector3(0.9f, 7.2f, 2.2f), new Vector3(0f, 0f, -7f), rockMat);
            CreateBox(crack.transform, "Lõi Tối Khe Nứt", new Vector3(0f, 3.0f, -0.9f), new Vector3(0.9f, 6.8f, 0.4f), Vector3.zero, darkMat);
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
            float rad = angleDeg * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad));
            Vector3 pos = dir * radius;
            pos.y = baseY;

            var mouth = new GameObject(name);
            mouth.transform.SetParent(parent, false);
            mouth.transform.position = pos;
            mouth.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);

            float wallThick = 1.1f;
            float depth = 8.0f;
            CreateBox(mouth.transform, "Trụ Trái", new Vector3(-(width * 0.5f + wallThick * 0.5f), height * 0.5f, depth * 0.5f), new Vector3(wallThick, height + 1.2f, depth), Vector3.zero, rockMat);
            CreateBox(mouth.transform, "Trụ Phải", new Vector3(width * 0.5f + wallThick * 0.5f, height * 0.5f, depth * 0.5f), new Vector3(wallThick, height + 1.2f, depth), Vector3.zero, rockMat);
            CreateBox(mouth.transform, "Vòm Trên", new Vector3(0f, height + 0.55f, depth * 0.5f), new Vector3(width + wallThick * 2f, 1.1f, depth), Vector3.zero, rockMat);
            CreateBox(mouth.transform, "Nền Hầm", new Vector3(0f, -0.25f, depth * 0.5f), new Vector3(width + 1.0f, 0.5f, depth), Vector3.zero, rockMat);
            CreateBox(mouth.transform, "Màn Tối Cuối Hầm", new Vector3(0f, height * 0.5f, depth - 0.3f), new Vector3(width, height, 0.4f), Vector3.zero, darkMat);

            return mouth.transform;
        }

        static void CreateBox(Transform parent, string name, Vector3 localPos, Vector3 scale, Vector3 euler, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            go.transform.localEulerAngles = euler;
            if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
        }

        public static Material LoadOrMakeRockMaterial()
        {
            const string path = Root + "/Materials/Cavern_Rock.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.SetColor("_BaseColor", new Color(0.28f, 0.27f, 0.25f));
            m.SetFloat("_Smoothness", 0.1f);
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        public static Material LoadOrMakeDarkInteriorMaterial()
        {
            const string path = Root + "/Materials/Maze_DarkInterior.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.SetColor("_BaseColor", new Color(0.03f, 0.03f, 0.04f));
            m.SetFloat("_Smoothness", 0f);
            AssetDatabase.CreateAsset(m, path);
            return m;
        }
    }
}
