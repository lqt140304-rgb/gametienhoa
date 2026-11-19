using System.Collections.Generic;
using Evolve.Maze;
using Evolve.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Evolve.EditorTools
{
    /// <summary>
    /// Evolve -> Build Maze World (Step 3).
    /// Dựng thế giới mê cung 14 khoang quy mô lớn (đường kính 150m, cách nhau 360m),
    /// miệng hang rộng rãi không bị lấp, bỏ phần nước lũ và chỗ trú lơ lửng.
    /// Chạy lại được nhiều lần sạch sẽ mà không làm hỏng scene.
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

            // Dọn cả Cavern Maze Wall cũ (nếu có) để không bị 2 bộ miệng hang trùng nhau
            var oldWall = GameObject.Find("Cavern Maze Wall");
            if (oldWall != null) Object.DestroyImmediate(oldWall);

            var old = GameObject.Find(WorldRootName);
            if (old != null) Object.DestroyImmediate(old);

            var root = new GameObject(WorldRootName);
            var graph = MazeGenerator.Generate(DefaultSeed, DefaultChambers);

            var rockMat = CavernMazeWall.LoadOrMakeRockMaterial();
            var darkMat = CavernMazeWall.LoadOrMakeDarkInteriorMaterial();
            var mossMat = LoadOrMakeMossMat();

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
                    float angleDeg = isBirth ? (315f + e * 115f) % 360f : (e * (360f / Mathf.Max(1, edges.Count)) + 30f);
                    tunnelAngles.Add(angleDeg);
                }

                var arrival = new GameObject("ArrivalPoint").transform;
                arrival.SetParent(chamberGo.transform, false);
                if (isBirth)
                {
                    arrival.position = CavernLayout.SpawnPoint();
                }
                else
                {
                    float arrivalY = MazeTerrainMath.GroundHeightAt(0f, 0f, terrainSeed, ChamberRadius, tunnelAngles, node.HasHighLedge) + 0.8f;
                    arrival.localPosition = new Vector3(0f, arrivalY, 0f);
                }

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

                // Đặt miệng hang ở vùng đất phẳng trước chân vách (27.5m ở Hang Khởi Sinh, 56m ở khoang 75m)
                float baseRadius = isBirth ? 27.5f : 56.0f;
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

            // Gắn MazeWorldManager (chỉ bật khoang hiện tại và các khoang kề)
            var manager = root.AddComponent<MazeWorldManager>();
            manager.Configure(DefaultSeed, DefaultChambers, instances);
            manager.ApplyActiveChambers();

            EditorSceneManager.MarkSceneDirty(root.scene);
            Selection.activeGameObject = root;
            Debug.Log($"Maze World: đã dựng {graph.Chambers.Count} khoang (bán kính {ChamberRadius}m = đường kính {ChamberRadius * 2f}m, bỏ nước lũ & chỗ trú), chỉ bật Hang Khởi Sinh và {graph.GetNeighborIds(0).Count} khoang kề.");
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
                ? CavernLayout.GroundHeight(gx, gz) - 0.1f
                : MazeTerrainMath.GroundHeightAt(gx, gz, terrainSeed, ChamberRadius, tunnelAngles, node.HasHighLedge) - 0.1f;

            // Miệng hang rộng rãi đồ sộ để slime đi vào mượt 100% không bị lấp hay vướng
            (float width, float height) = edge.Size switch
            {
                TunnelSize.Small => (4.6f, 4.2f),
                TunnelSize.Medium => (6.8f, 5.8f),
                _ => (9.5f, 7.8f)
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

            // Điểm xuất hiện khi từ khoang đích bước ra (đứng ngay trước cửa hang rộng)
            var exitPoint = new GameObject("ExitSpawnPoint").transform;
            exitPoint.SetParent(mouth, false);
            exitPoint.localPosition = new Vector3(0f, 0.8f, -4.0f);
            exitPoint.localRotation = Quaternion.Euler(0f, 180f, 0f);

            // Cổng hoán đổi đặt ngay trong lòng cửa hang (z = +2.4m), thông thoáng không có collider cản
            var portalGo = new GameObject("TunnelPortal");
            portalGo.transform.SetParent(mouth, false);
            portalGo.transform.localPosition = new Vector3(0f, height * 0.45f, 2.4f);
            var portalCol = portalGo.AddComponent<BoxCollider>();
            portalCol.isTrigger = true;
            portalCol.size = new Vector3(width * 0.85f, height * 0.85f, 2.8f);

            var portal = portalGo.AddComponent<TunnelPortal>();
            portal.Configure(node.Id, targetChamberId, edge.Id, exitPoint);
            inst.RegisterPortal(portal);
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
    }
}
