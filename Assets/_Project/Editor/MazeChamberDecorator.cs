using System.Collections.Generic;
using Evolve.Creatures;
using Evolve.Maze;
using Evolve.Survival;
using Evolve.World;
using UnityEditor;
using UnityEngine;

namespace Evolve.EditorTools
{
    /// <summary>
    /// Trang trí từng khoang rộng (>= 45m) với địa hình gò đồi tự nhiên, vòm trần kín 360 độ,
    /// thảm thực vật bám đúng mặt đất/trần, thức ăn, sinh vật và Kỳ Ngộ / Bẫy riêng của khoang.
    /// </summary>
    public static class MazeChamberDecorator
    {
        const string Root = "Assets/_Project";
        const string ModelFolder = Root + "/Models/Underroot/";
        const string PrefabFolder = Root + "/Prefabs/";

        public static void Decorate(
            Transform parent,
            ChamberNode node,
            float chamberRadius,
            IReadOnlyList<float> tunnelAnglesDeg,
            Material mossMat)
        {
            var profile = ChamberThemeCatalog.Get(node.Theme);
            int seed = 1000 + node.Id * 73;
            var rnd = new System.Random(10007 + node.Id * 919);

            // Nhân màu lên nhẹ để kết hợp đẹp với texture đá/đất thật (tránh bị tối đen khi không có ánh nắng trời)
            Color floorColor = Color.Lerp(ToColor(profile.FloorColor), Color.white, 0.38f);
            Color wallColor = Color.Lerp(ToColor(profile.WallColor), Color.white, 0.35f);
            Color mainLightColor = ToColor(profile.MainLightColor);
            Color accentLightColor = ToColor(profile.AccentLightColor);

            string floorTex = node.Depth == ChamberDepth.Nong ? "Cavern_Mud_v3.png" : "Cavern_Rock_v3.png";
            var floorMat = MazeTerrainMesh.TexturedSurfaceMaterial($"Maze_Floor_{node.Theme}", floorColor, profile.WallSmoothness * 0.5f, false, floorTex);
            var wallMat = MazeTerrainMesh.TexturedSurfaceMaterial($"Maze_Wall_{node.Theme}", wallColor, profile.WallSmoothness, true, "Cavern_Rock_v3.png");
            var glowMat = LakeGround.OpaqueMaterial($"Maze_Glow_{node.Theme}", accentLightColor, 0.25f, false, accentLightColor * 1.15f);

            // 1. Địa hình gò đồi + đồi trú lũ + vách bao mở khe hầm + vòm trần kín 360 độ
            MazeTerrainMesh.BuildFloorAndWalls(parent, node, chamberRadius, tunnelAnglesDeg, floorMat);
            MazeTerrainMesh.BuildClosedCeiling(parent, node.Id, chamberRadius, wallMat);

            // 2. Chi tiết kiến trúc đặc trưng (Hang Trú trên đỉnh đồi tự nhiên / Bộ xương cổ Long ở Khoang Phong Ấn)
            BuildSpecialLandmarks(parent, node, seed, chamberRadius, tunnelAnglesDeg, wallMat, mossMat, accentLightColor);

            // 3. Thảm thực vật, cỏ mềm, đá tảng và nhũ đá/rễ bám chặt đúng cao độ đất/trần
            PlacePropsAndFlora(parent, node, seed, chamberRadius, tunnelAnglesDeg, profile, floorColor, wallMat, glowMat, rnd);

            // 4. Thức ăn (Nutrient) & Kỳ Ngộ / Bẫy riêng của khoang
            PlaceForageNutrients(parent, node, seed, chamberRadius, tunnelAnglesDeg, profile, glowMat, rnd);
            PlaceEncounterOrTrap(parent, node, seed, chamberRadius, tunnelAnglesDeg, wallMat, glowMat, rnd);

            // 5. Sinh vật & Ánh sáng đa tầng
            PlaceCreatures(parent, node, chamberRadius, profile, rnd);
            BuildChamberLighting(parent, profile, mainLightColor, accentLightColor);
        }

        static void BuildSpecialLandmarks(
            Transform parent,
            ChamberNode node,
            int seed,
            float chamberRadius,
            IReadOnlyList<float> tunnelAnglesDeg,
            Material wallMat,
            Material mossMat,
            Color accentColor)
        {
            if (node.Kind == ChamberKind.Shelter)
            {
                var (hx, hz) = MazeTerrainMath.GetHighLedgeCenter(chamberRadius, tunnelAnglesDeg, seed);
                float hy = MazeTerrainMath.GroundHeightAt(hx, hz, seed, chamberRadius, tunnelAnglesDeg, node.HasHighLedge);
                Vector3 hillPos = new Vector3(hx, hy - 0.15f, hz);
                Vector3 toCenter = new Vector3(-hx, 0f, -hz).normalized;

                MazeShelterBuilder.BuildRealShelter(
                    parent,
                    "Hang Trú Trong Khoang",
                    hillPos,
                    -toCenter,
                    wallMat,
                    mossMat,
                    ledgeHeight: 0f,
                    placeOnNaturalHill: true);
            }
            else if (node.Kind == ChamberKind.SealedCavern)
            {
                float ySkull = MazeTerrainMath.GroundHeightAt(0f, 6f, seed, chamberRadius, tunnelAnglesDeg, node.HasHighLedge);
                float yRibs = MazeTerrainMath.GroundHeightAt(0f, -2f, seed, chamberRadius, tunnelAnglesDeg, node.HasHighLedge);
                var skull = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFolder + "DragonSkull_0.fbx");
                var ribs = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFolder + "DragonRibcage_0.fbx");
                if (skull != null && ribs != null)
                {
                    var s = (GameObject)PrefabUtility.InstantiatePrefab(skull, parent);
                    s.transform.localPosition = new Vector3(0f, ySkull + 0.15f, 6f);
                    s.transform.localScale = Vector3.one * 1.65f;

                    var r = (GameObject)PrefabUtility.InstantiatePrefab(ribs, parent);
                    r.transform.localPosition = new Vector3(0f, yRibs + 0.05f, -2f);
                    r.transform.localScale = Vector3.one * 1.65f;
                }
            }
        }

        static void PlacePropsAndFlora(
            Transform parent,
            ChamberNode node,
            int seed,
            float chamberRadius,
            IReadOnlyList<float> tunnelAnglesDeg,
            ChamberThemeProfile profile,
            Color floorColor,
            Material wallMat,
            Material glowMat,
            System.Random rnd)
        {
            var floraGroup = new GameObject("Thảm Thực Vật & Đá").transform;
            floraGroup.SetParent(parent, false);
            float usableRadius = chamberRadius * 0.78f;

            // 1. Thảm thực vật / tinh thể / xương đặc trưng
            if (profile.FloorFloraModels.Count > 0)
            {
                int propCount = node.Depth == ChamberDepth.Nong ? 48 : (node.Depth == ChamberDepth.Giua ? 34 : 24);
                var tuftPoints = new List<Vector3>(60);

                for (int i = 0; i < propCount; i++)
                {
                    string modelName = profile.FloorFloraModels[rnd.Next(profile.FloorFloraModels.Count)];
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFolder + modelName + ".fbx");
                    if (!TryPickFloorPoint(usableRadius, 4.5f, tunnelAnglesDeg, rnd, out float lx, out float lz)) continue;

                    float ly = MazeTerrainMath.GroundHeightAt(lx, lz, seed, chamberRadius, tunnelAnglesDeg, node.HasHighLedge);
                    if (prefab != null)
                    {
                        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, floraGroup);
                        go.transform.localPosition = new Vector3(lx, ly - 0.06f, lz);
                        go.transform.localRotation = Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f);
                        go.transform.localScale = Vector3.one * Mathf.Lerp(0.9f, 1.65f, (float)rnd.NextDouble());
                        ApplyGlowTint(go, glowMat);
                    }
                    tuftPoints.Add(new Vector3(lx, ly - 0.02f, lz));
                }

                // Thêm bụi cỏ mềm tự nhiên ở tầng Nông & Giữa
                if (node.Depth != ChamberDepth.Sau && tuftPoints.Count > 0)
                {
                    var grassMat = LakeGround.OpaqueMaterial($"Maze_Grass_{node.Theme}", Color.Lerp(floorColor, new Color(0.20f, 0.45f, 0.18f), 0.65f), 0.05f, true);
                    var tuftsGo = new GameObject("Thảm Cỏ Hang");
                    tuftsGo.transform.SetParent(floraGroup, false);
                    tuftsGo.AddComponent<MeshFilter>().sharedMesh = GroundMeshes.Tufts(tuftPoints, seed + 19, 0.48f);
                    tuftsGo.AddComponent<MeshRenderer>().sharedMaterial = grassMat;
                }
            }

            // 2. Đá tảng tự nhiên rải trên mặt đất và chân vách tạo điểm nấp
            for (int rIdx = 0; rIdx < 14; rIdx++)
            {
                if (!TryPickFloorPoint(usableRadius, 6f, tunnelAnglesDeg, rnd, out float rx, out float rz)) continue;
                float ry = MazeTerrainMath.GroundHeightAt(rx, rz, seed, chamberRadius, tunnelAnglesDeg, node.HasHighLedge);
                float rockScale = Mathf.Lerp(0.8f, 2.2f, (float)rnd.NextDouble());
                MazeTerrainMesh.PlaceFramingBoulder(floraGroup, new Vector3(rx, ry - 0.25f, rz), rockScale, seed + rIdx * 13, wallMat);
            }

            // 3. Nhũ đá, rễ cây, dây leo bám CHẶT vào mặt trần (không bay lơ lửng)
            if (profile.CeilingOrWallProps.Count > 0)
            {
                int ceilCount = 20;
                for (int i = 0; i < ceilCount; i++)
                {
                    string modelName = profile.CeilingOrWallProps[rnd.Next(profile.CeilingOrWallProps.Count)];
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFolder + modelName + ".fbx");
                    if (prefab == null) continue;

                    float angle = (float)rnd.NextDouble() * 360f;
                    float rad = angle * Mathf.Deg2Rad;
                    bool isFloorEdgeProp = modelName.StartsWith("Cliff") || modelName.StartsWith("RootStub");
                    float dist = isFloorEdgeProp
                        ? Mathf.Lerp(usableRadius * 0.85f, usableRadius * 1.02f, (float)rnd.NextDouble())
                        : Mathf.Lerp(6f, usableRadius * 0.88f, (float)rnd.NextDouble());

                    if (isFloorEdgeProp && IsNearAnyAngle(angle, tunnelAnglesDeg, 22f)) continue;

                    float px = Mathf.Cos(rad) * dist;
                    float pz = Mathf.Sin(rad) * dist;
                    float py = isFloorEdgeProp
                        ? MazeTerrainMath.GroundHeightAt(px, pz, seed, chamberRadius, tunnelAnglesDeg, node.HasHighLedge) - 0.2f
                        : MazeTerrainMath.CeilingHeightAt(px, pz, seed, chamberRadius) + 0.6f; // Cắm gốc sâu vào trần 0.6m

                    var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, floraGroup);
                    go.transform.localPosition = new Vector3(px, py, pz);
                    go.transform.localRotation = Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f);
                    go.transform.localScale = Vector3.one * Mathf.Lerp(1.0f, 1.55f, (float)rnd.NextDouble());
                    ApplyGlowTint(go, glowMat);
                }
            }
        }

        static void PlaceForageNutrients(
            Transform parent,
            ChamberNode node,
            int seed,
            float chamberRadius,
            IReadOnlyList<float> tunnelAnglesDeg,
            ChamberThemeProfile profile,
            Material glowMat,
            System.Random rnd)
        {
            if (profile.ForageItems.Count == 0 || profile.ForageCount <= 0) return;

            var forageGroup = new GameObject("Thức Ăn Trong Khoang").transform;
            forageGroup.SetParent(parent, false);
            float usableRadius = chamberRadius * 0.74f;

            for (int i = 0; i < profile.ForageCount; i++)
            {
                var item = profile.ForageItems[i % profile.ForageItems.Count];
                if (!TryPickFloorPoint(usableRadius, 4.0f, tunnelAnglesDeg, rnd, out float lx, out float lz)) continue;
                float ly = MazeTerrainMath.GroundHeightAt(lx, lz, seed, chamberRadius, tunnelAnglesDeg, node.HasHighLedge);
                CreateNutrientObject(forageGroup, item, new Vector3(lx, ly, lz), glowMat, rnd);
            }
        }

        static void CreateNutrientObject(Transform parent, ThemeForageItem item, Vector3 localPos, Material glowMat, System.Random rnd)
        {
            var go = new GameObject(item.DisplayName);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f);
            go.transform.localScale = Vector3.one * Mathf.Lerp(0.9f, 1.3f, (float)rnd.NextDouble());

            var sc = go.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = item.IsMineral ? 0.95f : 0.75f;
            sc.center = new Vector3(0f, 0.35f, 0f);

            var nut = go.AddComponent<Nutrient>();
            var so = new SerializedObject(nut);
            so.FindProperty("foodKind").enumValueIndex = (int)item.Food;
            so.FindProperty("totalNutrition").floatValue = item.IsMineral ? 24f : 14f;
            so.FindProperty("absorbRate").floatValue = item.IsMineral ? 2.5f : 5f;
            so.FindProperty("respawnSeconds").floatValue = item.IsMineral ? 480f : 150f;
            so.ApplyModifiedPropertiesWithoutUndo();

            var modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFolder + item.FbxName + ".fbx");
            if (modelPrefab != null)
            {
                var visual = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab, go.transform);
                visual.transform.localPosition = item.IsMineral ? new Vector3(0f, 0.2f, 0f) : Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
                foreach (var t in visual.GetComponentsInChildren<Transform>(true))
                    GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
                ApplyGlowTint(visual, glowMat);
            }
        }

        static void PlaceEncounterOrTrap(
            Transform parent,
            ChamberNode node,
            int seed,
            float chamberRadius,
            IReadOnlyList<float> tunnelAnglesDeg,
            Material wallMat,
            Material glowMat,
            System.Random rnd)
        {
            if (node.Encounter == ChamberEncounterKind.None) return;

            // Đặt Kỳ Ngộ / Bẫy chính ở phía đối diện đồi trú lũ (vùng dễ gặp khi khám phá khoang)
            float ledgeAngle = MazeTerrainMath.PickHighLedgeAngleDeg(tunnelAnglesDeg, seed);
            float encAngleRad = (ledgeAngle + 150f) * Mathf.Deg2Rad;
            float dist = node.Encounter == ChamberEncounterKind.UndergroundGeyser ? 10.5f : 12.0f;
            float ex = Mathf.Cos(encAngleRad) * dist;
            float ez = Mathf.Sin(encAngleRad) * dist;
            if (node.Kind == ChamberKind.Shelter)
            {
                ex = 0f;
                ez = 0f;
            }
            float ey = MazeTerrainMath.GroundHeightAt(ex, ez, seed, chamberRadius, tunnelAnglesDeg, node.HasHighLedge);

            SpawnEncounterNode(parent, node.Encounter, new Vector3(ex, ey, ez), wallMat, glowMat, seed);

            // Nếu là bẫy môi trường (Bào Tử / Khe Nhiệt), thêm 1 bẫy phụ + thưởng quặng quý ở gần để kích thích mạo hiểm
            if (node.Encounter == ChamberEncounterKind.SporeGeyserTrap || node.Encounter == ChamberEncounterKind.ThermalVentTrap)
            {
                float subAngle = encAngleRad + 1.4f;
                float sx = Mathf.Cos(subAngle) * 14f;
                float sz = Mathf.Sin(subAngle) * 14f;
                float sy = MazeTerrainMath.GroundHeightAt(sx, sz, seed, chamberRadius, tunnelAnglesDeg, node.HasHighLedge);
                SpawnEncounterNode(parent, node.Encounter, new Vector3(sx, sy, sz), wallMat, glowMat, seed + 50);

                var rewardItem = new ThemeForageItem("Forage_Quartz", "Thạch Anh Hiếm", FoodKind.GlowQuartz, isMineral: true);
                CreateNutrientObject(parent, rewardItem, new Vector3(ex + 2.4f, MazeTerrainMath.GroundHeightAt(ex + 2.4f, ez, seed, chamberRadius, tunnelAnglesDeg, node.HasHighLedge), ez), glowMat, rnd);
            }
        }

        static void SpawnEncounterNode(
            Transform parent,
            ChamberEncounterKind kind,
            Vector3 localPos,
            Material wallMat,
            Material glowMat,
            int seed)
        {
            var go = new GameObject($"KỳNgộ_{kind}");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;

            var col = go.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = kind == ChamberEncounterKind.ThermalVentTrap ? 3.2f : 2.5f;
            col.center = new Vector3(0f, 0.8f, 0f);

            // Bệ đá tự nhiên
            var ring = new GameObject("Bệ Đá Tự Nhiên");
            ring.transform.SetParent(go.transform, false);
            ring.transform.localPosition = new Vector3(0f, -0.15f, 0f);
            ring.AddComponent<MeshFilter>().sharedMesh = CaveMeshes.Disc(2.2f, 0.28f, 14);
            ring.AddComponent<MeshRenderer>().sharedMaterial = wallMat;

            (string fbxName, Color auraColor, float intensity) = kind switch
            {
                ChamberEncounterKind.AncientFont => ("Forage_WaterLily", new Color(0.25f, 1.0f, 0.78f), 3.2f),
                ChamberEncounterKind.ResonanceCrystal => ("CrystalCluster_0", new Color(0.75f, 0.38f, 1.0f), 3.5f),
                ChamberEncounterKind.AmberShrine => ("Forage_Quartz", new Color(1.0f, 0.72f, 0.18f), 3.2f),
                ChamberEncounterKind.UndergroundGeyser => ("BulbPlant_0", new Color(0.30f, 0.80f, 1.0f), 3.0f),
                ChamberEncounterKind.SporeGeyserTrap => ("VaultPod_0", new Color(0.85f, 0.25f, 0.85f), 2.8f),
                _ => ("Cliff_3", new Color(1.0f, 0.35f, 0.12f), 3.6f)
            };

            var visualHolder = new GameObject("Biểu Tượng Kỳ Ngộ").transform;
            visualHolder.SetParent(go.transform, false);
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFolder + fbxName + ".fbx");
            if (fbx != null)
            {
                var vis = (GameObject)PrefabUtility.InstantiatePrefab(fbx, visualHolder);
                vis.transform.localScale = Vector3.one * 1.45f;
                ApplyGlowTint(vis, LakeGround.OpaqueMaterial($"Maze_Enc_{kind}", auraColor, 0.3f, false, auraColor * 1.6f));
            }
            else
            {
                MazeTerrainMesh.PlaceFramingBoulder(visualHolder, Vector3.zero, 1.4f, seed, wallMat);
            }

            var light = new GameObject("Hào Quang Kỳ Ngộ").AddComponent<Light>();
            light.transform.SetParent(go.transform, false);
            light.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            light.type = LightType.Point;
            light.range = 13f;
            light.color = auraColor;
            light.intensity = intensity;
            light.shadows = LightShadows.None;

            var zone = go.AddComponent<ChamberEncounterZone>();
            zone.Configure(kind, light, visualHolder);
        }

        static void PlaceCreatures(
            Transform parent,
            ChamberNode node,
            float chamberRadius,
            ChamberThemeProfile profile,
            System.Random rnd)
        {
            if (profile.PreyCount > 0 && profile.PreyPrefabs.Count > 0)
            {
                string preyName = profile.PreyPrefabs[rnd.Next(profile.PreyPrefabs.Count)];
                var preyPrefab = LoadCreatureComponent<Prey>(preyName, "Prey");
                if (preyPrefab != null)
                {
                    var spawnerGo = new GameObject($"Ổ Con Mồi ({preyName})");
                    spawnerGo.transform.SetParent(parent, false);
                    spawnerGo.transform.localPosition = new Vector3(-8f, 0f, -6f);
                    var so = new SerializedObject(spawnerGo.AddComponent<PreySpawner>());
                    so.FindProperty("prefab").objectReferenceValue = preyPrefab;
                    so.FindProperty("count").intValue = profile.PreyCount;
                    so.FindProperty("radius").floatValue = chamberRadius * 0.55f;
                    so.FindProperty("minDistanceFromCenter").floatValue = 2.0f;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            if (node.Kind != ChamberKind.Shelter && profile.PredatorCount > 0 && profile.PredatorPrefabs.Count > 0)
            {
                string predName = profile.PredatorPrefabs[rnd.Next(profile.PredatorPrefabs.Count)];
                var predPrefab = LoadCreatureComponent<Predator>(predName, "Predator");
                if (predPrefab != null)
                {
                    var spawnerGo = new GameObject($"Lãnh Địa Quái Săn ({predName})");
                    spawnerGo.transform.SetParent(parent, false);
                    spawnerGo.transform.localPosition = new Vector3(6f, 0f, 9f);
                    var so = new SerializedObject(spawnerGo.AddComponent<PredatorSpawner>());
                    so.FindProperty("prefab").objectReferenceValue = predPrefab;
                    so.FindProperty("count").intValue = profile.PredatorCount;
                    so.FindProperty("radius").floatValue = chamberRadius * 0.50f;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }

        static void BuildChamberLighting(
            Transform parent,
            ChamberThemeProfile profile,
            Color mainColor,
            Color accentColor)
        {
            var mainLight = new GameObject("Ánh Sáng Chính").AddComponent<Light>();
            mainLight.transform.SetParent(parent, false);
            mainLight.transform.localPosition = new Vector3(0f, 17.5f, 0f);
            mainLight.type = LightType.Point;
            mainLight.range = 68f;
            mainLight.color = mainColor;
            mainLight.intensity = profile.MainLightIntensity * 1.4f;
            mainLight.shadows = LightShadows.None;

            Vector3[] accentOffsets = { new Vector3(-18f, 6.5f, 16f), new Vector3(18f, 7.0f, -16f), new Vector3(0f, 7.5f, 20f) };
            for (int i = 0; i < accentOffsets.Length; i++)
            {
                var accent = new GameObject($"Ánh Sáng Điểm Nhấn_{i}").AddComponent<Light>();
                accent.transform.SetParent(parent, false);
                accent.transform.localPosition = accentOffsets[i];
                accent.type = LightType.Point;
                accent.range = 28f;
                accent.color = accentColor;
                accent.intensity = profile.AccentLightIntensity * 1.2f;
                accent.shadows = LightShadows.None;
            }
        }

        static T LoadCreatureComponent<T>(string preferredPrefabName, string fallbackPrefabName) where T : Component
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + preferredPrefabName + ".prefab");
            var comp = go != null ? go.GetComponent<T>() : null;
            if (comp != null) return comp;

            var fallback = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + fallbackPrefabName + ".prefab");
            return fallback != null ? fallback.GetComponent<T>() : null;
        }

        static void ApplyGlowTint(GameObject go, Material glowMat)
        {
            if (glowMat == null) return;
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                if (r.name.Contains("Glow")) r.sharedMaterial = glowMat;
            }
        }

        static bool TryPickFloorPoint(
            float maxRadius,
            float minRadius,
            IReadOnlyList<float> tunnelAnglesDeg,
            System.Random rnd,
            out float localX,
            out float localZ)
        {
            for (int attempt = 0; attempt < 20; attempt++)
            {
                float angleDeg = (float)rnd.NextDouble() * 360f;
                float r = Mathf.Lerp(minRadius, maxRadius, Mathf.Sqrt((float)rnd.NextDouble()));
                if (r > maxRadius - 6f && IsNearAnyAngle(angleDeg, tunnelAnglesDeg, 20f)) continue;
                localX = Mathf.Cos(angleDeg * Mathf.Deg2Rad) * r;
                localZ = Mathf.Sin(angleDeg * Mathf.Deg2Rad) * r;
                return true;
            }
            localX = 0f;
            localZ = 0f;
            return false;
        }

        static bool IsNearAnyAngle(float angleDeg, IReadOnlyList<float> targets, float toleranceDeg)
        {
            if (targets == null) return false;
            for (int i = 0; i < targets.Count; i++)
            {
                if (Mathf.Abs(Mathf.DeltaAngle(angleDeg, targets[i])) < toleranceDeg)
                    return true;
            }
            return false;
        }

        static Color ToColor(MazeColor c) => new Color(c.R, c.G, c.B);
    }
}
