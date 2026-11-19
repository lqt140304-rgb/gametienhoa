using System.Collections.Generic;
using Evolve.Creatures;
using Evolve.Maze;
using Evolve.Survival;
using Evolve.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Evolve.EditorTools
{
    /// <summary>
    /// Trang trí từng khoang theo chủ đề riêng (vòm đá tự nhiên, vách/sàn theo chủ đề,
    /// thảm thực vật, điểm thức ăn Nutrient, loài quái/con mồi đặc trưng và ánh sáng đa tầng).
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
            var rnd = new System.Random(10007 + node.Id * 919);

            Color floorColor = ToColor(profile.FloorColor);
            Color wallColor = ToColor(profile.WallColor);
            Color mainLightColor = ToColor(profile.MainLightColor);
            Color accentLightColor = ToColor(profile.AccentLightColor);

            var floorMat = LakeGround.OpaqueMaterial($"Maze_Floor_{node.Theme}", floorColor, profile.WallSmoothness * 0.6f, false);
            var wallMat = LakeGround.OpaqueMaterial($"Maze_Wall_{node.Theme}", wallColor, profile.WallSmoothness, true);
            var glowMat = LakeGround.OpaqueMaterial($"Maze_Glow_{node.Theme}", accentLightColor, 0.25f, false, accentLightColor * 1.1f);

            BuildShellAndBounds(parent, node, chamberRadius, profile, tunnelAnglesDeg, floorMat, wallMat);
            BuildLedgeAndSpecialArchitecture(parent, node, wallMat, mossMat, accentLightColor);
            PlacePropsAndFlora(parent, node, chamberRadius, tunnelAnglesDeg, profile, glowMat, rnd);
            PlaceForageNutrients(parent, chamberRadius, tunnelAnglesDeg, profile, glowMat, rnd);
            PlaceCreatures(parent, node, chamberRadius, profile, rnd);
            BuildChamberLighting(parent, profile, mainLightColor, accentLightColor);
        }

        static void BuildShellAndBounds(
            Transform parent,
            ChamberNode node,
            float chamberRadius,
            ChamberThemeProfile profile,
            IReadOnlyList<float> tunnelAnglesDeg,
            Material floorMat,
            Material wallMat)
        {
            // 1. Sàn thấp (ngập khi lũ dâng 4.5m; mặt trên tại y = 0)
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            floor.name = "Sàn Thấp (Ngập Lũ)";
            floor.transform.SetParent(parent, false);
            floor.transform.localPosition = new Vector3(0f, -0.5f, 0f);
            floor.transform.localScale = new Vector3(chamberRadius * 2.15f, 0.5f, chamberRadius * 2.15f);
            if (floorMat != null) floor.GetComponent<Renderer>().sharedMaterial = floorMat;

            // 2. Vòm hang đá 3D tự nhiên bằng CaveMeshes.Shell (hai mặt, không collider để không bít cửa hầm)
            var shellGo = new GameObject("Vòm Hang Đá");
            shellGo.transform.SetParent(parent, false);
            shellGo.transform.localPosition = new Vector3(0f, -0.4f, 0f);
            var shellMesh = CaveMeshes.Shell(chamberRadius + 2.8f, chamberRadius + 2.8f, 15.5f, 28, 10, 100 + node.Id * 37, profile.DomeWobble);
            shellGo.AddComponent<MeshFilter>().sharedMesh = shellMesh;
            var shellMr = shellGo.AddComponent<MeshRenderer>();
            shellMr.sharedMaterial = wallMat;
            shellMr.shadowCastingMode = ShadowCastingMode.Off;

            // 3. Vách collider bao quanh (chừa khe mở tại đúng góc đặt các miệng hầm)
            const int wallSegments = 24;
            float step = 360f / wallSegments;
            for (int i = 0; i < wallSegments; i++)
            {
                float angleDeg = i * step;
                if (IsNearAnyAngle(angleDeg, tunnelAnglesDeg, 16f)) continue;

                float rad = angleDeg * Mathf.Deg2Rad;
                Vector3 pos = new Vector3(Mathf.Cos(rad) * (chamberRadius + 1.2f), 6.0f, Mathf.Sin(rad) * (chamberRadius + 1.2f));
                var wallCol = new GameObject($"Vách_Chắn_{i:00}");
                wallCol.transform.SetParent(parent, false);
                wallCol.transform.localPosition = pos;
                wallCol.transform.localRotation = Quaternion.LookRotation(-new Vector3(pos.x, 0f, pos.z), Vector3.up);
                var box = wallCol.AddComponent<BoxCollider>();
                box.size = new Vector3(6.2f, 13f, 2.0f);
            }
        }

        static void BuildLedgeAndSpecialArchitecture(
            Transform parent,
            ChamberNode node,
            Material wallMat,
            Material mossMat,
            Color accentColor)
        {
            // Gờ cao an toàn (y = 5.3..5.6m) + dốc leo
            if (node.HasHighLedge && node.Kind != ChamberKind.DeadEnd)
            {
                var ledge = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ledge.name = "Gờ Cao An Toàn";
                ledge.transform.SetParent(parent, false);
                ledge.transform.localPosition = new Vector3(9.5f, 5.3f, 0f);
                ledge.transform.localScale = new Vector3(7.2f, 0.6f, 7.2f);
                if (wallMat != null) ledge.GetComponent<Renderer>().sharedMaterial = wallMat;

                var ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ramp.name = "Dốc Lên Gờ Cao";
                ramp.transform.SetParent(parent, false);
                ramp.transform.localPosition = new Vector3(4.0f, 2.65f, 0f);
                ramp.transform.localRotation = Quaternion.Euler(0f, 0f, 38f);
                ramp.transform.localScale = new Vector3(8.5f, 0.5f, 3.2f);
                if (wallMat != null) ramp.GetComponent<Renderer>().sharedMaterial = wallMat;
            }

            if (node.Kind == ChamberKind.Shelter)
            {
                MazeShelterBuilder.BuildRealShelter(
                    parent,
                    "Hang Trú Trong Khoang",
                    new Vector3(-7f, 0f, 7f),
                    new Vector3(-1f, 0f, 1f),
                    wallMat,
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
                if (wallMat != null) slope.GetComponent<Renderer>().sharedMaterial = wallMat;
            }
            else if (node.Kind == ChamberKind.SealedCavern)
            {
                // Bộ xương rồng cổ đại (ưu tiên FBX Blender, fallback về khối xương nếu thiếu)
                var skull = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFolder + "DragonSkull_0.fbx");
                var ribs = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFolder + "DragonRibcage_0.fbx");
                if (skull != null && ribs != null)
                {
                    var s = (GameObject)PrefabUtility.InstantiatePrefab(skull, parent);
                    s.transform.localPosition = new Vector3(0f, 0.2f, 4.5f);
                    s.transform.localScale = Vector3.one * 1.4f;

                    var r = (GameObject)PrefabUtility.InstantiatePrefab(ribs, parent);
                    r.transform.localPosition = new Vector3(0f, 0.1f, -1.5f);
                    r.transform.localScale = Vector3.one * 1.4f;
                }
                else
                {
                    var boneMat = LakeGround.OpaqueMaterial("Maze_DragonBone", new Color(0.76f, 0.72f, 0.62f), 0.15f, false, accentColor * 0.15f);
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
            }
        }

        static void PlacePropsAndFlora(
            Transform parent,
            ChamberNode node,
            float chamberRadius,
            IReadOnlyList<float> tunnelAnglesDeg,
            ChamberThemeProfile profile,
            Material glowMat,
            System.Random rnd)
        {
            var floraGroup = new GameObject("Thảm Thực Vật & Đá").transform;
            floraGroup.SetParent(parent, false);

            // Rải thảm thực vật / xương / tinh thể đặc trưng của chủ đề
            if (profile.FloorFloraModels.Count > 0)
            {
                int propCount = node.Depth == ChamberDepth.Nong ? 28 : (node.Depth == ChamberDepth.Giua ? 20 : 14);
                for (int i = 0; i < propCount; i++)
                {
                    string modelName = profile.FloorFloraModels[rnd.Next(profile.FloorFloraModels.Count)];
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFolder + modelName + ".fbx");
                    if (prefab == null) continue;

                    if (!TryPickFloorPoint(chamberRadius - 2.5f, 3.8f, tunnelAnglesDeg, rnd, out Vector3 localPos)) continue;
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, floraGroup);
                    go.transform.localPosition = localPos;
                    go.transform.localRotation = Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f);
                    float scale = Mathf.Lerp(0.85f, 1.55f, (float)rnd.NextDouble());
                    go.transform.localScale = Vector3.one * scale;
                    ApplyGlowTint(go, glowMat);
                }
            }

            // Rải nhũ đá / rễ buông / dây leo trên trần & chân vách
            if (profile.CeilingOrWallProps.Count > 0)
            {
                int ceilCount = 12;
                for (int i = 0; i < ceilCount; i++)
                {
                    string modelName = profile.CeilingOrWallProps[rnd.Next(profile.CeilingOrWallProps.Count)];
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFolder + modelName + ".fbx");
                    if (prefab == null) continue;

                    float angle = (float)rnd.NextDouble() * 360f;
                    float rad = angle * Mathf.Deg2Rad;
                    bool isCliffOrStub = modelName.StartsWith("Cliff") || modelName.StartsWith("RootStub");
                    float r = isCliffOrStub ? chamberRadius - 1.8f : Mathf.Lerp(4f, chamberRadius - 4f, (float)rnd.NextDouble());
                    float y = isCliffOrStub ? 0f : Mathf.Lerp(10.5f, 13.8f, (float)rnd.NextDouble());

                    if (isCliffOrStub && IsNearAnyAngle(angle, tunnelAnglesDeg, 18f)) continue;

                    var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, floraGroup);
                    go.transform.localPosition = new Vector3(Mathf.Cos(rad) * r, y, Mathf.Sin(rad) * r);
                    go.transform.localRotation = Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f);
                    go.transform.localScale = Vector3.one * Mathf.Lerp(0.9f, 1.4f, (float)rnd.NextDouble());
                    ApplyGlowTint(go, glowMat);
                }
            }
        }

        static void PlaceForageNutrients(
            Transform parent,
            float chamberRadius,
            IReadOnlyList<float> tunnelAnglesDeg,
            ChamberThemeProfile profile,
            Material glowMat,
            System.Random rnd)
        {
            if (profile.ForageItems.Count == 0 || profile.ForageCount <= 0) return;

            var forageGroup = new GameObject("Thức Ăn Trong Khoang").transform;
            forageGroup.SetParent(parent, false);

            for (int i = 0; i < profile.ForageCount; i++)
            {
                var item = profile.ForageItems[i % profile.ForageItems.Count];
                if (!TryPickFloorPoint(chamberRadius - 3.5f, 3.2f, tunnelAnglesDeg, rnd, out Vector3 localPos)) continue;

                var go = new GameObject(item.DisplayName);
                go.transform.SetParent(forageGroup, false);
                go.transform.localPosition = localPos;
                go.transform.localRotation = Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f);
                go.transform.localScale = Vector3.one * Mathf.Lerp(0.85f, 1.25f, (float)rnd.NextDouble());

                var sc = go.AddComponent<SphereCollider>();
                sc.isTrigger = true;
                sc.radius = item.IsMineral ? 0.9f : 0.7f;
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
                    spawnerGo.transform.localPosition = new Vector3(-3f, 0f, -3f);
                    var so = new SerializedObject(spawnerGo.AddComponent<PreySpawner>());
                    so.FindProperty("prefab").objectReferenceValue = preyPrefab;
                    so.FindProperty("count").intValue = profile.PreyCount;
                    so.FindProperty("radius").floatValue = chamberRadius * 0.55f;
                    so.FindProperty("minDistanceFromCenter").floatValue = 1.5f;
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
                    spawnerGo.transform.localPosition = new Vector3(3.5f, 0f, 4.5f);
                    var so = new SerializedObject(spawnerGo.AddComponent<PredatorSpawner>());
                    so.FindProperty("prefab").objectReferenceValue = predPrefab;
                    so.FindProperty("count").intValue = profile.PredatorCount;
                    so.FindProperty("radius").floatValue = chamberRadius * 0.5f;
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
            mainLight.transform.localPosition = new Vector3(0f, 11.2f, 0f);
            mainLight.type = LightType.Point;
            mainLight.range = 36f;
            mainLight.color = mainColor;
            mainLight.intensity = profile.MainLightIntensity;
            mainLight.shadows = LightShadows.None;

            Vector3[] accentOffsets = { new Vector3(-9f, 3.2f, 8f), new Vector3(8f, 3.5f, -8f) };
            for (int i = 0; i < accentOffsets.Length; i++)
            {
                var accent = new GameObject($"Ánh Sáng Điểm Nhấn_{i}").AddComponent<Light>();
                accent.transform.SetParent(parent, false);
                accent.transform.localPosition = accentOffsets[i];
                accent.type = LightType.Point;
                accent.range = 18f;
                accent.color = accentColor;
                accent.intensity = profile.AccentLightIntensity;
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
            out Vector3 localPos)
        {
            for (int attempt = 0; attempt < 20; attempt++)
            {
                float angleDeg = (float)rnd.NextDouble() * 360f;
                float r = Mathf.Lerp(minRadius, maxRadius, Mathf.Sqrt((float)rnd.NextDouble()));
                if (r > maxRadius - 4.5f && IsNearAnyAngle(angleDeg, tunnelAnglesDeg, 20f)) continue;
                // Tránh đè lên chân dốc hoặc bệ gờ cao ở phía (+9.5, 0)
                float x = Mathf.Cos(angleDeg * Mathf.Deg2Rad) * r;
                float z = Mathf.Sin(angleDeg * Mathf.Deg2Rad) * r;
                if (x > 2.5f && Mathf.Abs(z) < 3.8f) continue;
                localPos = new Vector3(x, 0f, z);
                return true;
            }
            localPos = Vector3.zero;
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
