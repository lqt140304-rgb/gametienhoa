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
        const float ChamberRadius = 20f;

        [MenuItem("Evolve/Build Maze World (Step 3)")]
        public static void Build()
        {
            var old = GameObject.Find(WorldRootName);
            if (old != null) Object.DestroyImmediate(old);

            var root = new GameObject(WorldRootName);
            var graph = MazeGenerator.Generate(DefaultSeed, DefaultChambers);

            var rockMat = CavernMazeWall.LoadOrMakeRockMaterial();
            var darkMat = CavernMazeWall.LoadOrMakeDarkInteriorMaterial();
            var nongMat = LoadOrMakeMat("Maze_Nong.mat", new Color(0.22f, 0.30f, 0.20f));
            var giuaMat = LoadOrMakeMat("Maze_Giua.mat", new Color(0.20f, 0.24f, 0.30f));
            var sauMat = LoadOrMakeMat("Maze_Sau.mat", new Color(0.18f, 0.12f, 0.13f));
            var mossMat = LoadOrMakeMossMat();
            var boneMat = LoadOrMakeMat("Maze_Bone.mat", new Color(0.76f, 0.72f, 0.62f));
            var floodMat = LoadOrMakeFloodWaterMat();

            var instances = new List<ChamberInstance>(graph.Chambers.Count);

            for (int i = 0; i < graph.Chambers.Count; i++)
            {
                var node = graph.Chambers[i];
                var (ox, oz) = MazeActivationRules.ComputeChamberOrigin(node.Id);
                Vector3 origin = new Vector3(ox, 0f, oz);

                Material floorMat = node.Depth switch
                {
                    ChamberDepth.Nong => nongMat,
                    ChamberDepth.Giua => giuaMat,
                    _ => sauMat
                };

                var chamberGo = new GameObject($"Khoang_{node.Id:00}_{node.Name}");
                chamberGo.transform.SetParent(root.transform, false);
                chamberGo.transform.position = origin;

                var arrival = new GameObject("ArrivalPoint").transform;
                arrival.SetParent(chamberGo.transform, false);
                arrival.localPosition = new Vector3(0f, 0.6f, 0f);

                var inst = chamberGo.AddComponent<ChamberInstance>();
                // Khoang 0 (Hang Khởi Sinh) chỉ chứa phần cổng nối vì địa hình Hang Khởi Sinh đã có sẵn ở (0,0,0)
                bool isBirth = node.Kind == ChamberKind.BirthCavern;
                inst.Configure(node.Id, node.Name, node.Depth, node.Kind, alwaysActive: isBirth, arrival: arrival);

                if (!isBirth)
                {
                    BuildModularChamberGeometry(chamberGo.transform, node, floorMat, rockMat, mossMat, boneMat);
                }

                // Dựng các miệng hầm và TunnelPortal cho từng đường hầm nối với khoang này
                var edges = graph.GetTunnels(node.Id);
                float baseRadius = isBirth ? 35.5f : ChamberRadius;
                for (int e = 0; e < edges.Count; e++)
                {
                    var edge = edges[e];
                    int otherId = edge.OtherEnd(node.Id);
                    float angleDeg = isBirth ? (320f + e * 120f) % 360f : (e * (360f / Mathf.Max(1, edges.Count)) + 30f);
                    BuildChamberTunnelEndpoint(
                        chamberGo.transform,
                        inst,
                        node,
                        otherId,
                        edge,
                        angleDeg,
                        baseRadius,
                        rockMat,
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

        static void BuildModularChamberGeometry(
            Transform parent,
            ChamberNode node,
            Material floorMat,
            Material rockMat,
            Material mossMat,
            Material boneMat)
        {
            // 1. Sàn thấp (ngập khi lũ dâng 4.5m)
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            floor.name = "Sàn Thấp (Ngập Lũ)";
            floor.transform.SetParent(parent, false);
            floor.transform.localPosition = new Vector3(0f, -0.5f, 0f);
            floor.transform.localScale = new Vector3(ChamberRadius * 2f, 0.5f, ChamberRadius * 2f);
            if (floorMat != null) floor.GetComponent<Renderer>().sharedMaterial = floorMat;

            // 2. Vách bao & Trần hang (tối dần theo tầng sâu)
            const int wallSegments = 12;
            for (int i = 0; i < wallSegments; i++)
            {
                float a = i * (360f / wallSegments) * Mathf.Deg2Rad;
                Vector3 pos = new Vector3(Mathf.Cos(a) * (ChamberRadius + 1.5f), 6.5f, Mathf.Sin(a) * (ChamberRadius + 1.5f));
                var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.name = $"Vách_{i:00}";
                wall.transform.SetParent(parent, false);
                wall.transform.localPosition = pos;
                wall.transform.localRotation = Quaternion.LookRotation(-new Vector3(pos.x, 0f, pos.z), Vector3.up);
                wall.transform.localScale = new Vector3(11.5f, 14f, 2.5f);
                if (rockMat != null) wall.GetComponent<Renderer>().sharedMaterial = rockMat;
            }

            var ceiling = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ceiling.name = "Trần Khoang";
            ceiling.transform.SetParent(parent, false);
            ceiling.transform.localPosition = new Vector3(0f, 13.5f, 0f);
            ceiling.transform.localScale = new Vector3(ChamberRadius * 2.2f, 0.8f, ChamberRadius * 2.2f);
            if (rockMat != null) ceiling.GetComponent<Renderer>().sharedMaterial = rockMat;

            // 3. Gờ cao an toàn (y = 5.5m) + dốc leo
            if (node.HasHighLedge && node.Kind != ChamberKind.DeadEnd)
            {
                var ledge = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ledge.name = "Gờ Cao An Toàn";
                ledge.transform.SetParent(parent, false);
                ledge.transform.localPosition = new Vector3(10f, 5.3f, 0f);
                ledge.transform.localScale = new Vector3(7f, 0.6f, 7f);
                if (rockMat != null) ledge.GetComponent<Renderer>().sharedMaterial = rockMat;

                var ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ramp.name = "Dốc Lên Gờ Cao";
                ramp.transform.SetParent(parent, false);
                ramp.transform.localPosition = new Vector3(4.5f, 2.65f, 0f);
                ramp.transform.localRotation = Quaternion.Euler(0f, 0f, 38f);
                ramp.transform.localScale = new Vector3(8.5f, 0.5f, 3.2f);
                if (rockMat != null) ramp.GetComponent<Renderer>().sharedMaterial = rockMat;
            }

            // 4. Chi tiết riêng theo loại khoang
            if (node.Kind == ChamberKind.Shelter)
            {
                MazeShelterBuilder.BuildRealShelter(
                    parent,
                    "Hang Trú Trong Khoang",
                    new Vector3(-7f, 0f, 7f),
                    new Vector3(-1f, 0f, 1f),
                    rockMat,
                    mossMat,
                    ledgeHeight: 5.6f);
            }
            else if (node.Kind == ChamberKind.DeadEnd)
            {
                var slope = GameObject.CreatePrimitive(PrimitiveType.Cube);
                slope.name = "Đường Dốc Lên Ngõ Cụt";
                slope.transform.SetParent(parent, false);
                slope.transform.localPosition = new Vector3(0f, 3.5f, 6f);
                slope.transform.localRotation = Quaternion.Euler(-28f, 0f, 0f);
                slope.transform.localScale = new Vector3(4.5f, 0.6f, 14f);
                if (rockMat != null) slope.GetComponent<Renderer>().sharedMaterial = rockMat;
            }
            else if (node.Kind == ChamberKind.SealedCavern)
            {
                // Bộ xương rồng cổ đại gợi ý (mục 9.6)
                for (int b = 0; b < 5; b++)
                {
                    var rib = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    rib.name = $"Xương Rồng Cổ_{b}";
                    rib.transform.SetParent(parent, false);
                    rib.transform.localPosition = new Vector3(0f, 2.2f, -4f + b * 2.5f);
                    rib.transform.localRotation = Quaternion.Euler(0f, 0f, 35f * (b % 2 == 0 ? 1 : -1));
                    rib.transform.localScale = new Vector3(0.7f, 4.5f, 0.7f);
                    if (boneMat != null) rib.GetComponent<Renderer>().sharedMaterial = boneMat;
                }
            }

            // 5. Ánh sáng đặc trưng theo tầng sâu (càng sâu càng tối)
            var light = new GameObject("Ánh Sáng Khoang").AddComponent<Light>();
            light.transform.SetParent(parent, false);
            light.transform.localPosition = new Vector3(0f, 10.5f, 0f);
            light.type = LightType.Point;
            light.range = 32f;
            light.shadows = LightShadows.None;
            (light.color, light.intensity) = node.Depth switch
            {
                ChamberDepth.Nong => (new Color(0.55f, 0.85f, 0.65f), 2.4f),
                ChamberDepth.Giua => (new Color(0.45f, 0.65f, 0.90f), 1.6f),
                _ => (new Color(0.85f, 0.38f, 0.30f), 1.1f)
            };
        }

        static void BuildChamberTunnelEndpoint(
            Transform parent,
            ChamberInstance inst,
            ChamberNode node,
            int targetChamberId,
            TunnelEdge edge,
            float angleDeg,
            float radius,
            Material rockMat,
            Material darkMat)
        {
            var slope = edge.SlopeFrom(node.Id);
            float baseY = slope switch
            {
                TunnelSlope.Up => 4.2f,    // hầm đi lên đặt cao hơn, khô ráo hơn
                TunnelSlope.Down => 0.1f,  // hầm đi xuống ở tầng thấp, ngập trước khi lũ dâng
                _ => 1.5f
            };

            (float width, float height) = edge.Size switch
            {
                TunnelSize.Small => (1.15f, 1.05f),
                TunnelSize.Medium => (2.5f, 2.3f),
                _ => (4.2f, 3.6f)
            };

            var mouth = CavernMazeWall.BuildTunnelMouthPreview(
                parent,
                $"Hầm_tới_Khoang_{targetChamberId:00}_{edge.Size}",
                angleDeg,
                radius,
                baseY,
                width,
                height,
                rockMat,
                darkMat);

            // Điểm xuất hiện khi từ khoang đích bước sang khoang này
            var exitPoint = new GameObject("ExitSpawnPoint").transform;
            exitPoint.SetParent(mouth, false);
            exitPoint.localPosition = new Vector3(0f, 0.6f, -2.2f);
            exitPoint.localRotation = Quaternion.Euler(0f, 180f, 0f);

            // Cổng hoán đổi ẩn đặt sâu trong đoạn hầm khuất (z = +5.8m)
            var portalGo = new GameObject("TunnelPortal");
            portalGo.transform.SetParent(mouth, false);
            portalGo.transform.localPosition = new Vector3(0f, height * 0.5f, 5.8f);
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

        static Material LoadOrMakeMat(string fileName, Color color)
        {
            string path = Root + "/Materials/" + fileName;
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Smoothness", 0.12f);
            AssetDatabase.CreateAsset(m, path);
            return m;
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
