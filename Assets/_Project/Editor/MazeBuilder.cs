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

        [InitializeOnLoadMethod]
        static void AutoRebuildIfOutdated()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                var mgr = Object.FindAnyObjectByType<MazeWorldManager>();
                if (mgr == null) return;

                bool hasLuma = false, hasCrystal = false;
                foreach (var tr in mgr.GetComponentsInChildren<Transform>(true))
                {
                    if (tr.name == "Khoang_RừngNấmPhátSáng_3D") hasLuma = true;
                    else if (tr.name == "Khoang_VựcThẳmTinhThể_3D") hasCrystal = true;
                }

                if (mgr.BuildVersion < MazeWorldManager.CurrentBuildVersion || !hasLuma || !hasCrystal)
                {
                    Debug.Log("[MazeBuilder] Phát hiện Maze World bản cũ hoặc thiếu khoang 3D mới — tự động dựng lại scene...");
                    Build();
                }
            };
        }

        [MenuItem("Evolve/Build Maze World (Step 3)")]
        public static void Build()
        {
            PropInstaller.EnsureRocks();
            PropInstaller.EnsureLandmarks();
            CaveCreaturesWave1Builder.EnsureAll();

            // Dọn cả Cavern Maze Wall cũ (nếu có) để không bị 2 bộ miệng hang trùng nhau
            var oldWall = GameObject.Find("Cavern Maze Wall");
            if (oldWall != null)
            {
                if (Application.isPlaying) Object.Destroy(oldWall);
                else Object.DestroyImmediate(oldWall);
            }

            var old = GameObject.Find(WorldRootName);
            if (old != null)
            {
                if (Application.isPlaying)
                {
                    old.name = "_MazeWorld_Old";
                    old.SetActive(false);
                    Object.Destroy(old);
                }
                else
                {
                    Object.DestroyImmediate(old);
                }
            }

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
                    float angleDeg;
                    if (isBirth)
                    {
                        angleDeg = (315f + e * 115f) % 360f;
                    }
                    else if (node.Theme == ChamberTheme.LumaMushroomGrove && edges.Count <= 3)
                    {
                        float[] lumaAngles = { 20f, 156f, 264f };
                        angleDeg = lumaAngles[e % lumaAngles.Length];
                    }
                    else if (node.Theme == ChamberTheme.CrystalVein && edges.Count <= 3)
                    {
                        float[] crystalAngles = { 25f, 148f, 268f };
                        angleDeg = crystalAngles[e % crystalAngles.Length];
                    }
                    else
                    {
                        angleDeg = (e * (360f / Mathf.Max(1, edges.Count)) + 30f);
                    }
                    tunnelAngles.Add(angleDeg);
                }

                var arrival = new GameObject("ArrivalPoint").transform;
                arrival.SetParent(chamberGo.transform, false);
                if (isBirth)
                {
                    arrival.position = CavernLayout.SpawnPoint();
                }
                else if (node.Theme == ChamberTheme.LumaMushroomGrove)
                {
                    arrival.localPosition = new Vector3(0f, 0.8f, 0f);
                }
                else if (node.Theme == ChamberTheme.CrystalVein)
                {
                    arrival.localPosition = new Vector3(0f, 1.8f, -24f);
                }
                else
                {
                    float arrivalY = MazeTerrainMath.GroundHeightAt(0f, 0f, terrainSeed, ChamberRadius, tunnelAngles, node.HasHighLedge) + 0.8f;
                    arrival.localPosition = new Vector3(0f, arrivalY, 0f);
                }

                var inst = chamberGo.AddComponent<ChamberInstance>();
                inst.Configure(node.Id, node.Name, node.Depth, node.Kind, alwaysActive: isBirth, arrival: arrival);
                inst.ConfigureTerrain(terrainSeed, ChamberRadius, node.HasHighLedge, tunnelAngles);

                MeshCollider floor3d = null;
                if (isBirth)
                {
                    CaveWave1ScenePopulator.PopulateBirthCavern(chamberGo.transform, origin);
                }
                else
                {
                    MazeChamberDecorator.Decorate(chamberGo.transform, node, ChamberRadius, tunnelAngles, mossMat);
                    floor3d = FindHandcraftedFloorCollider(chamberGo);
                    if (floor3d != null)
                    {
                        Physics.SyncTransforms();
                        var arrRay = new Ray(new Vector3(origin.x + arrival.localPosition.x, origin.y + 30f, origin.z + arrival.localPosition.z), Vector3.down);
                        if (floor3d.Raycast(arrRay, out var arrHit, 60f))
                        {
                            arrival.localPosition = new Vector3(arrival.localPosition.x, arrHit.point.y - origin.y + 0.8f, arrival.localPosition.z);
                        }
                    }
                }

                var profile = ChamberThemeCatalog.Get(node.Theme);
                Color wallTint = Color.Lerp(new Color(profile.WallColor.R, profile.WallColor.G, profile.WallColor.B), Color.white, 0.35f);
                var mouthMat = isBirth
                    ? rockMat
                    : MazeTerrainMesh.TexturedSurfaceMaterial($"Maze_Wall_{node.Theme}", wallTint, profile.WallSmoothness, true, "Cavern_Rock_v3.png");

                // Đặt miệng hang ở vùng đất phẳng trước chân vách
                float baseRadius = isBirth ? 25.5f : ((node.Theme == ChamberTheme.LumaMushroomGrove || node.Theme == ChamberTheme.CrystalVein) ? 72.0f : 56.0f);
                for (int e = 0; e < edges.Count; e++)
                {
                    var edge = edges[e];
                    int otherId = edge.OtherEnd(node.Id);
                    var destProfile = ChamberThemeCatalog.Get(graph.GetChamber(otherId).Theme);
                    Color destGlow = new Color(
                        destProfile.AccentLightColor.R,
                        destProfile.AccentLightColor.G,
                        destProfile.AccentLightColor.B);
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
                        darkMat,
                        destGlow,
                        floor3d);
                }

                instances.Add(inst);
            }

            // Gắn MazeWorldManager (chỉ bật khoang hiện tại và các khoang kề)
            var manager = root.AddComponent<MazeWorldManager>();
            manager.Configure(DefaultSeed, DefaultChambers, instances);
            manager.SetBuildVersion(MazeWorldManager.CurrentBuildVersion);
            manager.ApplyActiveChambers();

            if (!Application.isPlaying)
            {
                EditorSceneManager.MarkSceneDirty(root.scene);
                Selection.activeGameObject = root;
            }
            Debug.Log($"Maze World: đã dựng {graph.Chambers.Count} khoang (bán kính {ChamberRadius}m = đường kính {ChamberRadius * 2f}m, bỏ nước lũ & chỗ trú), chỉ bật Hang Khởi Sinh và {graph.GetNeighborIds(0).Count} khoang kề.");
        }

        static MeshCollider FindHandcraftedFloorCollider(GameObject chamberGo)
        {
            foreach (var mc in chamberGo.GetComponentsInChildren<MeshCollider>(true))
            {
                if (mc == null) continue;
                string n = mc.gameObject.name;
                string pn = mc.transform.parent != null ? mc.transform.parent.name : "";
                if (n.EndsWith("_3D") || pn.EndsWith("_3D")) return mc;
            }
            return null;
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
            Material darkMat,
            Color deepGlowColor,
            MeshCollider floor3d = null)
        {
            float rad = angleDeg * Mathf.Deg2Rad;
            float gx = Mathf.Cos(rad) * radius;
            float gz = Mathf.Sin(rad) * radius;
            bool isBirth = node.Kind == ChamberKind.BirthCavern;
            float baseY = isBirth
                ? CavernLayout.GroundHeight(gx, gz) - 0.1f
                : MazeTerrainMath.GroundHeightAt(gx, gz, terrainSeed, ChamberRadius, tunnelAngles, node.HasHighLedge) - 0.1f;

            if (!isBirth && floor3d != null)
            {
                var ray = new Ray(new Vector3(parent.position.x + gx, parent.position.y + 30f, parent.position.z + gz), Vector3.down);
                if (floor3d.Raycast(ray, out var hit, 60f))
                {
                    baseY = hit.point.y - parent.position.y - 0.1f;
                }
            }

            // Miệng hang Blender rộng rãi (scale 1.4x): thông thoáng, dấu hiệu hang đích sáng rõ dần vào trong
            (float width, float height) = edge.Size switch
            {
                TunnelSize.Small => (4.0f, 3.3f),
                TunnelSize.Medium => (6.2f, 5.0f),
                _ => (9.0f, 7.0f)
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
                seed: edge.Id * 31 + node.Id,
                sizeHint: edge.Size,
                deepGlowColor: deepGlowColor);

            // Điểm xuất hiện khi từ khoang đích bước ra (đứng ngoài hẳn phần miệng loe -3.6m)
            var exitPoint = new GameObject("ExitSpawnPoint").transform;
            exitPoint.SetParent(mouth, false);
            exitPoint.localPosition = new Vector3(0f, 0.8f, isBirth ? -5.2f : -5.8f);
            exitPoint.localRotation = Quaternion.Euler(0f, 180f, 0f);

            // Cổng hoán đổi đặt sâu trong đoạn khuất giữa hầm (sau miệng hang, trước vách đáy kín)
            var portalGo = new GameObject("TunnelPortal");
            portalGo.transform.SetParent(mouth, false);
            portalGo.transform.localPosition = new Vector3(0f, height * 0.45f, isBirth ? 2.8f : 4.0f);
            var portalCol = portalGo.AddComponent<BoxCollider>();
            portalCol.isTrigger = true;
            portalCol.size = new Vector3(width * 0.9f, height * 0.85f, 3.0f);

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
