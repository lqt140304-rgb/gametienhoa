using System.Collections.Generic;
using Evolve.Maze;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Evolve.EditorTools
{
    /// <summary>
    /// Evolve -> Build Maze World (Step 3).
    /// Dựng thế giới mê cung 14 khoang mô-đun trong cùng scene (cách nhau 250m), nối Hang Khởi Sinh tại (0,0,0),
    /// gắn hoán đổi ẩn giữa hầm, kích thước hầm/chướng ngại, Hang Trú thật và Chu Kỳ Lũ.
    /// Chạy lại được nhiều lần mà không làm hỏng scene.
    /// </summary>
    public static class MazeBuilder
    {
        const string Root = "Assets/_Project";
        const string WorldRootName = "Maze World";
        const int DefaultSeed = 2026;
        const int DefaultChambers = 14;
        const float ChamberRadius = MazeTerrainMath.DefaultChamberRadius;

        [MenuItem("Evolve/Build Maze World (Step 3)")]
        public static void Build()
        {
            PropInstaller.EnsureRocks();
            PropInstaller.EnsureLandmarks();

            var old = GameObject.Find(WorldRootName);
            if (old != null) Object.DestroyImmediate(old);

            var root = new GameObject(WorldRootName);
            var graph = MazeGenerator.Generate(DefaultSeed, DefaultChambers);

            var rockMat = CavernMazeWall.LoadOrMakeRockMaterial();
            var darkMat = CavernMazeWall.LoadOrMakeDarkInteriorMaterial();
            var mossMat = LoadOrMakeMossMat();
            var floodMat = LoadOrMakeFloodWaterMat();

            var instances = new List<ChamberInstance>(graph.Chambers.Count);

            for (int i = 0; i < graph.Chambers.Count; i++)
            {
                var node = graph.Chambers[i];
                var (ox, oz) = MazeActivationRules.ComputeChamberOrigin(node.Id);
                Vector3 origin = new Vector3(ox, 0f, oz);

                var chamberGo = new GameObject($"Khoang_{node.Id:00}_{node.Name}");
                chamberGo.transform.SetParent(root.transform, false);
                chamberGo.transform.position = origin;

                bool isBirth = node.Kind == ChamberKind.BirthCavern;
                int terrainSeed = 1000 + node.Id * 73;

                var edges = graph.GetTunnels(node.Id);
                var tunnelAngles = new List<float>(edges.Count);
                for (int e = 0; e < edges.Count; e++)
                {
                    float angleDeg = isBirth ? (320f + e * 120f) % 360f : (e * (360f / Mathf.Max(1, edges.Count)) + 30f);
                    tunnelAngles.Add(angleDeg);
                }

                var arrival = new GameObject("ArrivalPoint").transform;
                arrival.SetParent(chamberGo.transform, false);
                float arrivalY = isBirth
                    ? 0.6f
                    : MazeTerrainMath.GroundHeightAt(0f, 0f, terrainSeed, ChamberRadius, tunnelAngles, node.HasHighLedge) + 0.6f;
                arrival.localPosition = new Vector3(0f, arrivalY, 0f);

                var inst = chamberGo.AddComponent<ChamberInstance>();
                inst.Configure(node.Id, node.Name, node.Depth, node.Kind, alwaysActive: isBirth, arrival: arrival);
                inst.ConfigureTerrain(terrainSeed, ChamberRadius, node.HasHighLedge, tunnelAngles);

                if (!isBirth)
                {
                    MazeChamberDecorator.Decorate(chamberGo.transform, node, ChamberRadius, tunnelAngles, mossMat);
                }

                var profile = ChamberThemeCatalog.Get(node.Theme);
                Color wallTint = Color.Lerp(new Color(profile.WallColor.R, profile.WallColor.G, profile.WallColor.B), Color.white, 0.35f);
                var mouthMat = isBirth
                    ? rockMat
                    : MazeTerrainMesh.TexturedSurfaceMaterial($"Maze_Wall_{node.Theme}", wallTint, profile.WallSmoothness, true, "Cavern_Rock_v3.png");

                // Dựng các miệng hầm và TunnelPortal cho từng đường hầm nối với khoang này
                float baseRadius = isBirth ? 35.5f : 38.5f;
                for (int e = 0; e < edges.Count; e++)
                {
                    var edge = edges[e];
                    int otherId = edge.OtherEnd(node.Id);
                    BuildChamberTunnelEndpoint(
                        chamberGo.transform,
                        inst,
                        node,
                        otherId,
                        edge,
                        tunnelAngles[e],
                        baseRadius,
                        terrainSeed,
                        tunnelAngles,
                        mouthMat,
                        darkMat);
                }

                instances.Add(inst);
            }

            // Gắn MazeWorldManager
            var manager = root.AddComponent<MazeWorldManager>();
            manager.Configure(DefaultSeed, DefaultChambers, instances);
            manager.ApplyActiveChambers();

            // Gắn FloodCycleController + mặt nước lũ toàn cục di chuyển theo người chơi/khoang
            var floodWater = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floodWater.name = "Mặt Nước Lũ (Flood Water)";
            Object.DestroyImmediate(floodWater.GetComponent<Collider>());
            floodWater.transform.SetParent(root.transform, false);
            floodWater.transform.position = new Vector3(0f, 0.06f, 0f);
            floodWater.transform.localScale = new Vector3(16f, 1f, 16f);
            if (floodMat != null) floodWater.GetComponent<Renderer>().sharedMaterial = floodMat;

            var flood = root.AddComponent<FloodCycleController>();
            var so = new SerializedObject(flood);
            so.FindProperty("floodWaterSurface").objectReferenceValue = floodWater.transform;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(root.scene);
            Selection.activeGameObject = root;
            Debug.Log($"Maze World: đã dựng {graph.Chambers.Count} khoang (seed {DefaultSeed}), chỉ bật Hang Khởi Sinh và {graph.GetNeighborIds(0).Count} khoang kề.");
        }

        static void BuildChamberTunnelEndpoint(
            Transform parent,
            ChamberInstance inst,
            ChamberNode node,
            int targetChamberId,
            TunnelEdge edge,
            float angleDeg,
            float radius,
            int terrainSeed,
            IReadOnlyList<float> tunnelAngles,
            Material rockMat,
            Material darkMat)
        {
            float rad = angleDeg * Mathf.Deg2Rad;
            float gx = Mathf.Cos(rad) * radius;
            float gz = Mathf.Sin(rad) * radius;
            float baseY = node.Kind == ChamberKind.BirthCavern
                ? 0.3f
                : MazeTerrainMath.GroundHeightAt(gx, gz, terrainSeed, ChamberRadius, tunnelAngles, node.HasHighLedge) - 0.1f;

            (float width, float height) = edge.Size switch
            {
                TunnelSize.Small => (1.15f, 1.05f),
                TunnelSize.Medium => (2.5f, 2.3f),
                _ => (4.2f, 3.6f)
            };

            var mouth = MazeTerrainMesh.BuildNaturalTunnelMouth(
                parent,
                $"Hầm_tới_Khoang_{targetChamberId:00}_{edge.Size}",
                angleDeg,
                radius,
                baseY,
                width,
                height,
                rockMat,
                darkMat,
                seed: edge.Id * 31 + node.Id);

            // Điểm xuất hiện khi từ khoang đích bước sang khoang này
            var exitPoint = new GameObject("ExitSpawnPoint").transform;
            exitPoint.SetParent(mouth, false);
            exitPoint.localPosition = new Vector3(0f, 0.6f, -2.6f);
            exitPoint.localRotation = Quaternion.Euler(0f, 180f, 0f);

            // Cổng hoán đổi ẩn đặt sâu trong đoạn hầm khuất uốn cong (x = +0.55f, z = +6.4m)
            var portalGo = new GameObject("TunnelPortal");
            portalGo.transform.SetParent(mouth, false);
            portalGo.transform.localPosition = new Vector3(0.55f, height * 0.5f, 6.4f);
            var portalCol = portalGo.AddComponent<BoxCollider>();
            portalCol.isTrigger = true;
            portalCol.size = new Vector3(width, height, 1.8f);

            var portal = portalGo.AddComponent<TunnelPortal>();
            portal.Configure(node.Id, targetChamberId, edge.Id, exitPoint);
            inst.RegisterPortal(portal);

            // Bộ kiểm tra kích thước khe & chướng ngại tự nhiên ở cửa hầm (z = +1.2m)
            if (edge.Size != TunnelSize.Large || edge.Obstacle != ObstacleKind.None)
            {
                var blockGo = new GameObject("Cửa Chắn Hầm");
                blockGo.transform.SetParent(mouth, false);
                blockGo.transform.localPosition = new Vector3(0f, height * 0.5f, 1.2f);
                var solidCol = blockGo.AddComponent<BoxCollider>();
                solidCol.isTrigger = false;
                solidCol.size = new Vector3(width, height, 0.4f);

                var checkGo = new GameObject("Kiểm Tra Điều Kiện Hầm");
                checkGo.transform.SetParent(mouth, false);
                checkGo.transform.localPosition = new Vector3(0f, height * 0.5f, 0.5f);
                var triggerCol = checkGo.AddComponent<BoxCollider>();
                triggerCol.isTrigger = true;
                triggerCol.size = new Vector3(width + 1.4f, height + 1.0f, 2.4f);

                var passage = checkGo.AddComponent<TunnelPassage>();
                passage.Configure(edge.Size, edge.Obstacle);
                var so = new SerializedObject(passage);
                so.FindProperty("blockingCollider").objectReferenceValue = solidCol;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        static Material LoadOrMakeMossMat()
        {
            const string path = Root + "/Materials/Cavern_ShelterMoss.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.SetColor("_BaseColor", new Color(0.18f, 0.45f, 0.22f));
            m.SetFloat("_Smoothness", 0.15f);
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", new Color(0.12f, 0.5f, 0.2f));
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        static Material LoadOrMakeFloodWaterMat()
        {
            const string path = Root + "/Materials/Maze_FloodWater.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.SetColor("_BaseColor", new Color(0.12f, 0.34f, 0.42f, 0.72f));
            m.SetFloat("_Smoothness", 0.85f);
            m.SetFloat("_Surface", 1f); // Transparent
            AssetDatabase.CreateAsset(m, path);
            return m;
        }
    }
}
