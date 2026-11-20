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
    /// Trang trí từng khoang quy mô lớn (150m đường kính) theo chủ đề riêng:
    /// địa hình gò đồi thung lũng đa tầng, vòng vách đá đại cảnh, quần thể kiến trúc trung tâm đặc trưng,
    /// thảm thực vật bám chặt mặt đất (0% lơ lửng), thức ăn, sinh vật và Tình Huống Môi Trường Động.
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

            Color floorColor = Color.Lerp(ToColor(profile.FloorColor), Color.white, 0.36f);
            Color wallColor = Color.Lerp(ToColor(profile.WallColor), Color.white, 0.34f);
            Color mainLightColor = ToColor(profile.MainLightColor);
            Color accentLightColor = ToColor(profile.AccentLightColor);

            string floorTex = node.Depth == ChamberDepth.Nong ? "Cavern_Mud_v3.png" : "Cavern_Rock_v3.png";
            var floorMat = MazeTerrainMesh.TexturedSurfaceMaterial($"Maze_Floor_{node.Theme}", floorColor, profile.WallSmoothness * 0.5f, false, floorTex);
            var wallMat = MazeTerrainMesh.TexturedSurfaceMaterial($"Maze_Wall_{node.Theme}", wallColor, profile.WallSmoothness, true, "Cavern_Rock_v3.png");
            var glowMat = LakeGround.OpaqueMaterial($"Maze_Glow_{node.Theme}", accentLightColor, 0.25f, false, accentLightColor * 0.55f);

            // Khoang mẫu Rừng Nấm Phát Sáng (Step 2) sử dụng model 3D Blender hoàn chỉnh (230m x 175m)
            if (node.Theme == ChamberTheme.LumaMushroomGrove)
            {
                var lumaFbx = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFolder + "MazeChamber_Luma.fbx");
                if (lumaFbx != null)
                {
                    BuildHandcraftedLumaChamber(parent, node, lumaFbx, profile, mainLightColor, accentLightColor, glowMat, wallMat, rnd);
                    return;
                }
            }

            // Khoang mẫu Vực Thẳm Tinh Thể (Step 3) sử dụng model 3D Blender hoàn chỉnh (235m x 180m, hẻm vực -15m)
            if (node.Theme == ChamberTheme.CrystalVein)
            {
                var crystalFbx = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFolder + "MazeChamber_Crystal.fbx");
                if (crystalFbx != null)
                {
                    BuildHandcraftedCrystalChamber(parent, node, crystalFbx, profile, mainLightColor, accentLightColor, glowMat, wallMat, rnd);
                    return;
                }
            }

            // 1. Địa hình đồi thung lũng 150m + vách bao mở khe hầm + vòm trần kín 360 độ
            MazeTerrainMesh.BuildFloorAndWalls(parent, node, chamberRadius, tunnelAnglesDeg, floorMat);
            MazeTerrainMesh.BuildClosedCeiling(parent, node.Id, chamberRadius, wallMat);

            // 2. Vòng vách đá đại cảnh + Quần thể kiến trúc khổng lồ đặc trưng cho từng chủ đề
            BuildGrandPerimeterCliffs(parent, node, seed, chamberRadius, tunnelAnglesDeg, wallMat, rnd);
            BuildGrandThemeLandmarks(parent, node, seed, chamberRadius, tunnelAnglesDeg, wallMat, mossMat, glowMat, accentLightColor);

            // 3. Thảm thực vật, cỏ mềm, đá tảng cắm chặt chân xuống mặt đất
            PlacePropsAndFlora(parent, node, seed, chamberRadius, tunnelAnglesDeg, profile, floorColor, wallMat, glowMat, rnd);

            // 4. Thức ăn (Nutrient) & Sinh vật & Ánh sáng đa tầng
            PlaceForageNutrients(parent, node, seed, chamberRadius, tunnelAnglesDeg, profile, glowMat, rnd);
            if (node.Theme == ChamberTheme.BreathingMoss)
                CaveWave1ScenePopulator.PopulateBreathingMossChamber(parent, parent.position);
            else if (node.Theme == ChamberTheme.AbandonedNest)
                CaveWave1ScenePopulator.PopulateAbandonedNestChamber(parent, parent.position);
            else if (node.Theme == ChamberTheme.GlowMushroomCavern)
                CaveWave1ScenePopulator.PopulateGlowMushroomChamber(parent, parent.position);
            else if (node.Theme == ChamberTheme.HotSpringCavern)
                CaveWave1ScenePopulator.PopulateHotSpringChamber(parent, parent.position);
            else if (node.Theme == ChamberTheme.HowlingWindGorge)
                CaveWave1ScenePopulator.PopulateHowlingWindChamber(parent, parent.position);
            else
                PlaceCreatures(parent, node, chamberRadius, profile, rnd);
            var lights = BuildChamberLighting(parent, profile, mainLightColor, accentLightColor);

            // 5. Tình Huống Môi Trường Động gắn với chủ đề
            PlaceEncounterSituation(parent, node, seed, chamberRadius, tunnelAnglesDeg, wallMat, lights);
        }

        static void BuildHandcraftedLumaChamber(
            Transform parent,
            ChamberNode node,
            GameObject lumaFbx,
            ChamberThemeProfile profile,
            Color mainLightColor,
            Color accentLightColor,
            Material glowMat,
            Material wallMat,
            System.Random rnd)
        {
            // 1. Dựng model 3D khoang mẫu hoàn chỉnh từ Blender (xoay 180° quanh Y để khớp hệ tọa độ Unity 1:1)
            var lumaGo = (GameObject)PrefabUtility.InstantiatePrefab(lumaFbx, parent);
            lumaGo.name = "Khoang_RừngNấmPhátSáng_3D";
            lumaGo.transform.localPosition = Vector3.zero;
            lumaGo.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            lumaGo.transform.localScale = Vector3.one;

            EnsureLumaMaterials(lumaGo);
            var chamberCollider = EnsureChamberCollider(lumaGo);

            // 2. Thảm thực vật nhỏ có vật lý rẽ ngang sống động và animation rung động nhẹ
            PlaceInteractiveLumaFlora(parent, chamberCollider, rnd);

            // 3. Thức ăn (Nutrient) phân bố tự nhiên
            PlaceHandcraftedLumaForage(parent, chamberCollider, profile, glowMat, rnd);

            // 4. Sinh vật sinh thái Đợt 1 + Tổ (Den) + LightShafts + Kỳ ngộ Cầu Vồng Trong Thân
            CaveWave1ScenePopulator.PopulateLumaChamber(parent, parent.position, chamberCollider);

            // 5. Hệ thống chiếu sáng đa tầng
            var lights = BuildHandcraftedLumaLighting(parent, mainLightColor, accentLightColor);

            // 6. Tình huống môi trường động (nếu có)
            if (node.Encounter != ChamberEncounterKind.None)
            {
                PlaceEncounterSituation(parent, node, 1000 + node.Id * 73, 65f, new float[] { 20f, 156f, 264f }, wallMat, lights);
            }
        }

        static MeshCollider EnsureChamberCollider(GameObject root)
        {
            if (root == null) return null;
            MeshCollider colMc = null;
            bool hasColChild = false;
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null) continue;
                if (mf.gameObject.name.EndsWith("_COL"))
                {
                    hasColChild = true;
                    if (!mf.gameObject.TryGetComponent<MeshCollider>(out colMc) || colMc == null)
                        colMc = mf.gameObject.AddComponent<MeshCollider>();
                    colMc.sharedMesh = mf.sharedMesh;
                    colMc.convex = false;
                    var mr = mf.GetComponent<MeshRenderer>();
                    if (mr != null) mr.enabled = false;
                }
            }

            if (hasColChild)
            {
                var rootMc = root.GetComponent<MeshCollider>();
                if (rootMc != null) Object.DestroyImmediate(rootMc);
                Physics.SyncTransforms();
                return colMc;
            }

            var mc = root.GetComponent<MeshCollider>() ?? root.GetComponentInChildren<MeshCollider>(true);
            if (mc == null)
            {
                MeshFilter colMf = null;
                foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (mf.sharedMesh == null) continue;
                    if (mf.gameObject.name.Contains("Shell")) { colMf = mf; break; }
                }
                if (colMf != null)
                {
                    mc = colMf.gameObject.AddComponent<MeshCollider>();
                    mc.sharedMesh = colMf.sharedMesh;
                    mc.convex = false;
                }
            }
            Physics.SyncTransforms();
            return mc;
        }

        static Vector3 SnapToChamberSurface(Transform chamberParent, MeshCollider chamberCollider, Vector3 localPos, float offsetY = 0f)
        {
            if (chamberCollider != null && chamberCollider.enabled && chamberCollider.gameObject.activeInHierarchy)
            {
                float startLocalY = Mathf.Clamp(localPos.y + 18f, 22f, 36f);
                Vector3 worldTop = chamberParent.TransformPoint(new Vector3(localPos.x, startLocalY, localPos.z));
                var ray = new Ray(worldTop, Vector3.down);
                if (chamberCollider.Raycast(ray, out var hit, 58f))
                {
                    return new Vector3(localPos.x, hit.point.y - chamberParent.position.y + offsetY, localPos.z);
                }
            }
            return new Vector3(localPos.x, localPos.y + offsetY, localPos.z);
        }

        static Material LoadOrCreateVertexMaterial(string name, Color tint, float fill, float glow)
        {
            string path = $"{Root}/Materials/{name}.mat";
            var shader = Shader.Find("Evolve/RockVertexColor");
            var baseMat = AssetDatabase.LoadAssetAtPath<Material>($"{Root}/Materials/Rock_VC.mat");
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = baseMat != null ? new Material(baseMat) { name = name } : new Material(shader) { name = name };
                AssetDatabase.CreateAsset(mat, path);
            }
            if (shader != null) mat.shader = shader;
            mat.SetColor("_Tint", new Color(tint.r, tint.g, tint.b, 1f));
            mat.SetFloat("_Fill", fill);
            mat.SetFloat("_Glow", glow);
            mat.enableInstancing = true;
            if (!Application.isPlaying) EditorUtility.SetDirty(mat);
            return mat;
        }

        static (Material rock, Material largeGlow, Material smallGlow) GetLumaMaterials() => (
            LoadOrCreateVertexMaterial("Maze_Luma_RockVC", Color.white, fill: 0.28f, glow: 0f),
            LoadOrCreateVertexMaterial("Maze_Luma_LargeGlow", new Color(0.92f, 0.96f, 0.94f), fill: 0.14f, glow: 0.42f),
            LoadOrCreateVertexMaterial("Maze_Luma_SmallGlow", new Color(0.95f, 0.98f, 0.96f), fill: 0.16f, glow: 0.52f)
        );

        static (Material rock, Material largeGlow, Material smallGlow) GetCrystalMaterials() => (
            LoadOrCreateVertexMaterial("Maze_Crystal_RockVC", new Color(1.15f, 1.10f, 1.22f), fill: 0.34f, glow: 0f),
            LoadOrCreateVertexMaterial("Maze_Crystal_LargeGlow", new Color(0.90f, 0.86f, 0.95f), fill: 0.14f, glow: 0.42f),
            LoadOrCreateVertexMaterial("Maze_Crystal_SmallGlow", new Color(0.94f, 0.90f, 0.98f), fill: 0.16f, glow: 0.50f)
        );

        static void EnsureLumaMaterials(GameObject root)
        {
            var (lumaRockMat, lumaLargeGlowMat, lumaSmallGlowMat) = GetLumaMaterials();
            var waterMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Cavern_WaterLake.mat")
                         ?? AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Cavern_Water.mat");

            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                string n = r.gameObject.name;
                if (n.EndsWith("_COL"))
                {
                    r.enabled = false;
                    continue;
                }
                if (n.Contains("Water") && waterMat != null)
                {
                    r.sharedMaterial = waterMat;
                }
                else if (n.Contains("Undergrowth_Glow"))
                {
                    r.sharedMaterial = lumaSmallGlowMat;
                }
                else if (n.Contains("Glow"))
                {
                    r.sharedMaterial = lumaLargeGlowMat;
                }
                else
                {
                    r.sharedMaterial = lumaRockMat;
                }
            }
            EnsureChamberCollider(root);
        }

        static void PlaceInteractiveLumaFlora(Transform parent, MeshCollider chamberCollider, System.Random rnd)
        {
            var floraGroup = new GameObject("Thảm Thực Vật Tương Tác (Rẽ Ngang)").transform;
            floraGroup.SetParent(parent, false);

            string[] floraPrefabs = {
                "MazeFlora_MutantReed",
                "MazeFlora_TwinFern",
                "MazeFlora_StackedSprout",
                "MazeFlora_GrassClump"
            };

            var fbxList = new List<GameObject>();
            for (int i = 0; i < floraPrefabs.Length; i++)
            {
                var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFolder + floraPrefabs[i] + ".fbx");
                if (fbx != null) fbxList.Add(fbx);
            }
            if (fbxList.Count == 0) return;

            var keyWaypoints = new List<Vector3>
            {
                // Lòng thung lũng & lối vào
                new Vector3(-4f, 0f, -8f),
                new Vector3(6f, 0f, -12f),
                new Vector3(-10f, 0f, 2f),
                new Vector3(12f, 0f, 4f),
                new Vector3(2f, 0f, 10f),
                new Vector3(-6f, 0f, 16f),
                new Vector3(8f, 0f, -2f),
                new Vector3(-14f, 0f, -16f),

                // Ven hồ hạ lưu & chân thác nước
                new Vector3(-16f, -0.8f, 4f),
                new Vector3(-8f, -0.8f, 8f),
                new Vector3(-18f, -0.5f, 12f),
                new Vector3(-10f, -0.6f, 18f),
                new Vector3(-4f, -0.7f, 22f),

                // Lối dẫn lên Cầu Đá Vòm
                new Vector3(32f, 1.0f, -22f),
                new Vector3(26f, 2.2f, -16f),
                new Vector3(20f, 3.8f, -10f),
                new Vector3(14f, 5.2f, -4f),
                new Vector3(8f, 6.4f, 2f),
                new Vector3(2f, 7.1f, 8f),
                new Vector3(-4f, 7.5f, 14f),
                new Vector3(-10f, 7.8f, 22f),
                new Vector3(-16f, 7.9f, 28f),

                // Thềm Ban Công Cao & bờ Hồ Thượng
                new Vector3(-20f, 8.0f, 34f),
                new Vector3(-24f, 8.0f, 40f),
                new Vector3(-18f, 8.0f, 46f),
                new Vector3(-10f, 8.0f, 50f),
                new Vector3(-6f, 7.6f, 44f),
                new Vector3(-28f, 8.2f, 32f),
                new Vector3(-32f, 8.4f, 26f),

                // Ngách hang & cửa hầm
                new Vector3(18f, 0.4f, -28f),
                new Vector3(24f, 0.6f, -32f),
                new Vector3(-24f, 0.5f, -6f),
                new Vector3(-30f, 0.6f, 2f),
            };

            var (lumaRockMat, _, lumaSmallGlowMat) = GetLumaMaterials();
            for (int i = 0; i < keyWaypoints.Count; i++)
            {
                var wp = keyWaypoints[i];
                int countAtPoint = rnd.Next(1, 3);
                for (int c = 0; c < countAtPoint; c++)
                {
                    float ox = (float)(rnd.NextDouble() - 0.5) * 2.8f;
                    float oz = (float)(rnd.NextDouble() - 0.5) * 2.8f;
                    var pos = SnapToChamberSurface(parent, chamberCollider, wp + new Vector3(ox, 0f, oz));

                    var fbx = fbxList[rnd.Next(fbxList.Count)];
                    var plantGo = (GameObject)PrefabUtility.InstantiatePrefab(fbx, floraGroup);
                    plantGo.name = $"{fbx.name}_{i:00}_{c}";
                    plantGo.transform.localPosition = pos;
                    plantGo.transform.localRotation = Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f);
                    plantGo.transform.localScale = Vector3.one * Mathf.Lerp(0.95f, 1.45f, (float)rnd.NextDouble());

                    bool isGrass = fbx.name.Contains("Grass");
                    foreach (var r in plantGo.GetComponentsInChildren<Renderer>(true))
                        r.sharedMaterial = isGrass ? lumaRockMat : lumaSmallGlowMat;

                    var iflora = plantGo.AddComponent<InteractiveFlora>();
                    var sc = plantGo.GetComponent<SphereCollider>();
                    if (sc == null) sc = plantGo.AddComponent<SphereCollider>();
                    sc.isTrigger = true;
                    sc.radius = 1.6f;
                    sc.center = new Vector3(0f, 0.5f, 0f);
                }
            }
        }

        static void PlaceHandcraftedLumaForage(Transform parent, MeshCollider chamberCollider, ChamberThemeProfile profile, Material glowMat, System.Random rnd)
        {
            if (profile.ForageItems.Count == 0) return;
            var forageGroup = new GameObject("Thức Ăn Khoang Luma").transform;
            forageGroup.SetParent(parent, false);

            var foragePoints = new Vector3[]
            {
                new Vector3(-2f, 0.1f, -4f),
                new Vector3(4f, 0.1f, 6f),
                new Vector3(-8f, 0.1f, 12f),
                new Vector3(10f, 0.1f, -8f),
                new Vector3(-14f, -0.6f, 6f),
                new Vector3(-6f, -0.6f, 20f),
                new Vector3(12f, 5.4f, -2f),
                new Vector3(-22f, 8.1f, 38f),
                new Vector3(-12f, 8.1f, 48f),
                new Vector3(-28f, 8.3f, 28f),
            };

            for (int i = 0; i < foragePoints.Length; i++)
            {
                var item = profile.ForageItems[i % profile.ForageItems.Count];
                var snappedPos = SnapToChamberSurface(parent, chamberCollider, foragePoints[i], 0.05f);
                CreateNutrientObject(forageGroup, item, snappedPos, glowMat, rnd);
            }
        }

        static List<Light> BuildHandcraftedLumaLighting(Transform parent, Color mainColor, Color accentColor)
        {
            var lightGroup = new GameObject("Ánh Sáng Khoang Luma").transform;
            lightGroup.SetParent(parent, false);
            var lights = new List<Light>();

            // 1. 2 Đèn Vòm Khuếch Tán Tổng Thể ở độ cao y = 26m (bù suy hao 1/d² trong URP)
            Color domeCool = Color.Lerp(mainColor, Color.white, 0.35f);
            lights.Add(CreatePointLight(lightGroup, "Đèn_Vòm_TâyNam", new Vector3(-12f, 26f, -6f), domeCool, 65f, 110f));
            lights.Add(CreatePointLight(lightGroup, "Đèn_Vòm_ĐôngBắc", new Vector3(16f, 26f, 22f), domeCool, 65f, 110f));

            // 2. 5 Đèn Vùng Trung Tầng ở độ cao 11m..16m (tạo khối sáng tối hài hòa, không chói)
            lights.Add(CreatePointLight(lightGroup, "Đèn_ThungLũng_TrungTâm", new Vector3(0f, 12f, -4f), mainColor, 30f, 60f));
            lights.Add(CreatePointLight(lightGroup, "Đèn_ThácNước_HồHạ", new Vector3(-14f, 11f, 12f), accentColor, 32f, 56f));
            lights.Add(CreatePointLight(lightGroup, "Đèn_CầuĐáVòm", new Vector3(10f, 14f, 4f), Color.Lerp(mainColor, new Color(1.0f, 0.78f, 0.45f), 0.45f), 28f, 58f));
            lights.Add(CreatePointLight(lightGroup, "Đèn_ThềmCao_HồThượng", new Vector3(-18f, 16f, 42f), accentColor, 30f, 58f));
            lights.Add(CreatePointLight(lightGroup, "Đèn_ĐồiĐông", new Vector3(32f, 11f, -16f), mainColor, 26f, 54f));

            return lights;
        }

        static void BuildHandcraftedCrystalChamber(
            Transform parent,
            ChamberNode node,
            GameObject crystalFbx,
            ChamberThemeProfile profile,
            Color mainLightColor,
            Color accentLightColor,
            Material glowMat,
            Material wallMat,
            System.Random rnd)
        {
            // 1. Dựng model 3D khoang mẫu hoàn chỉnh từ Blender (xoay 180° quanh Y để khớp hệ tọa độ Unity 1:1)
            var crystalGo = (GameObject)PrefabUtility.InstantiatePrefab(crystalFbx, parent);
            crystalGo.name = "Khoang_VựcThẳmTinhThể_3D";
            crystalGo.transform.localPosition = Vector3.zero;
            crystalGo.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            crystalGo.transform.localScale = Vector3.one;

            EnsureCrystalMaterials(crystalGo);
            var chamberCollider = EnsureChamberCollider(crystalGo);

            // 2. Thảm khoáng biến dị & quặng gai nhọn rải theo các gờ đá (0% cây cỏ, đàn hồi khoáng thạch giòn cứng)
            PlaceInteractiveCrystalMinerals(parent, chamberCollider, rnd);

            // 3. Thức ăn (Nutrient/Ore) phân bố tự nhiên trên các mỏm đá & hốc quặng
            PlaceHandcraftedCrystalForage(parent, chamberCollider, profile, glowMat, rnd);

            // 4. Sinh vật sinh thái Đợt 1 + Tổ (Den) + LightShafts + Kỳ ngộ Hợp Âm Tinh Thể
            CaveWave1ScenePopulator.PopulateCrystalChamber(parent, parent.position, chamberCollider);

            // 5. Hệ thống chiếu sáng đa tầng (khuếch tán vòm + hắt ngược đáy vực -5.5m + vùng đi bộ)
            var lights = BuildHandcraftedCrystalLighting(parent, mainLightColor, accentLightColor);

            // 6. Tình huống môi trường động (nếu có)
            if (node.Encounter != ChamberEncounterKind.None)
            {
                PlaceEncounterSituation(parent, node, 2000 + node.Id * 79, 68f, new float[] { 25f, 148f, 268f }, wallMat, lights);
            }
        }

        static void EnsureCrystalMaterials(GameObject root)
        {
            var (crystalRockMat, crystalLargeGlowMat, crystalSmallGlowMat) = GetCrystalMaterials();
            var waterMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Cavern_WaterLake.mat")
                         ?? AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Cavern_Water.mat");

            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                string n = r.gameObject.name;
                if (n.EndsWith("_COL"))
                {
                    r.enabled = false;
                    continue;
                }
                if ((n.Contains("Water") || n.Contains("Fluid")) && waterMat != null)
                {
                    r.sharedMaterial = waterMat;
                }
                else if (n.Contains("SmallOres") || n.Contains("Ore"))
                {
                    r.sharedMaterial = crystalSmallGlowMat;
                }
                else if (n.Contains("Glow"))
                {
                    r.sharedMaterial = crystalLargeGlowMat;
                }
                else
                {
                    r.sharedMaterial = crystalRockMat;
                }
            }
            EnsureChamberCollider(root);
        }

        static void PlaceInteractiveCrystalMinerals(Transform parent, MeshCollider chamberCollider, System.Random rnd)
        {
            var mineralGroup = new GameObject("Cụm Khoáng Biến Dị Tương Tác").transform;
            mineralGroup.SetParent(parent, false);

            string[] mineralPrefabs = {
                "MazeMineral_VoidSpine",
                "MazeMineral_BarbedTendril",
                "MazeMineral_ShardCrust",
                "MazeMineral_RawChunk"
            };

            var fbxList = new List<GameObject>();
            for (int i = 0; i < mineralPrefabs.Length; i++)
            {
                var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFolder + mineralPrefabs[i] + ".fbx");
                if (fbx != null) fbxList.Add(fbx);
            }
            if (fbxList.Count == 0) return;

            // Tọa độ trên các gờ đá lồi lõm, thềm đá đi bộ, và 4 dải đá ngầm tự nhiên vượt qua vực
            var mineralWaypoints = new List<Vector3>
            {
                // Thềm đá phía Nam (khu vực xuất hiện & cửa hầm Nam 268 độ)
                new Vector3(0f, 1.8f, -24f),
                new Vector3(-8f, 1.6f, -22f),
                new Vector3(10f, 1.5f, -25f),
                new Vector3(-18f, 1.2f, -26f),
                new Vector3(20f, 1.4f, -20f),
                new Vector3(-28f, 1.5f, -20f),
                new Vector3(32f, 1.1f, -18f),

                // 4 Dải đá ngầm / Cổ đất tự nhiên băng qua hẻm vực (-15m)
                // Cổ đất Tây (-36m)
                new Vector3(-38f, 0.2f, -10f),
                new Vector3(-35f, -0.4f, -4f),
                new Vector3(-36f, 0.3f, 4f),
                // Cổ đất Trung-Tây (-12m)
                new Vector3(-14f, 0.1f, -12f),
                new Vector3(-11f, -0.5f, -3f),
                new Vector3(-12f, 0.4f, 6f),
                // Cổ đất Trung-Đông (14m)
                new Vector3(12f, 0.2f, -10f),
                new Vector3(15f, -0.3f, -2f),
                new Vector3(13f, 0.5f, 7f),
                // Cổ đất Đông (40m)
                new Vector3(40f, 0.4f, -8f),
                new Vector3(42f, -0.2f, 0f),
                new Vector3(38f, 0.6f, 8f),

                // Thềm đá dốc gồ ghề phía Bắc (dẫn lên 2 hố dịch độc)
                new Vector3(-18f, 1.6f, 14f),
                new Vector3(16f, 1.8f, 16f),
                new Vector3(-4f, 2.2f, 18f),
                new Vector3(6f, 2.5f, 22f),
                new Vector3(-12f, 4.2f, 28f),
                new Vector3(8f, 4.6f, 30f),
                new Vector3(0f, 5.2f, 36f),
                new Vector3(-6f, 7.8f, 44f),
                new Vector3(10f, 8.2f, 48f),

                // Cửa hầm Đông-Bắc (25 độ) & Tây-Bắc (148 độ)
                new Vector3(48f, 1.5f, 22f),
                new Vector3(56f, 1.2f, 28f),
                new Vector3(-48f, 1.6f, 24f),
                new Vector3(-58f, 1.3f, 32f),
            };

            var (crystalRockMat, _, crystalSmallGlowMat) = GetCrystalMaterials();
            for (int i = 0; i < mineralWaypoints.Count; i++)
            {
                var wp = mineralWaypoints[i];
                int countAtPoint = rnd.Next(1, 3);
                for (int c = 0; c < countAtPoint; c++)
                {
                    float ox = (float)(rnd.NextDouble() - 0.5) * 2.5f;
                    float oz = (float)(rnd.NextDouble() - 0.5) * 2.5f;
                    var pos = SnapToChamberSurface(parent, chamberCollider, wp + new Vector3(ox, 0f, oz));

                    var fbx = fbxList[rnd.Next(fbxList.Count)];
                    var minGo = (GameObject)PrefabUtility.InstantiatePrefab(fbx, mineralGroup);
                    minGo.name = $"{fbx.name}_{i:00}_{c}";
                    minGo.transform.localPosition = pos;
                    minGo.transform.localRotation = Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f);
                    minGo.transform.localScale = Vector3.one * Mathf.Lerp(0.85f, 1.35f, (float)rnd.NextDouble());

                    foreach (var r in minGo.GetComponentsInChildren<Renderer>(true))
                        r.sharedMaterial = r.gameObject.name.Contains("Glow") ? crystalSmallGlowMat : crystalRockMat;

                    // Vật lý tương tác rẽ ngang: khoáng thạch giòn cứng
                    var iflora = minGo.AddComponent<InteractiveFlora>();
                    var so = new SerializedObject(iflora);
                    so.FindProperty("maxBendAngle").floatValue = 14f;
                    so.FindProperty("recoverySmoothTime").floatValue = 0.14f;
                    so.FindProperty("idleAmplitude").floatValue = 0.5f;
                    so.FindProperty("stiffness").floatValue = 0.72f;
                    so.ApplyModifiedPropertiesWithoutUndo();

                    var sc = minGo.GetComponent<SphereCollider>();
                    if (sc == null) sc = minGo.AddComponent<SphereCollider>();
                    sc.isTrigger = true;
                    sc.radius = 1.4f;
                    sc.center = new Vector3(0f, 0.45f, 0f);
                }
            }
        }

        static void PlaceHandcraftedCrystalForage(Transform parent, MeshCollider chamberCollider, ChamberThemeProfile profile, Material glowMat, System.Random rnd)
        {
            if (profile.ForageItems.Count == 0) return;
            var forageGroup = new GameObject("Thức Ăn Khoang Tinh Thể").transform;
            forageGroup.SetParent(parent, false);

            var foragePoints = new Vector3[]
            {
                new Vector3(-4f, 1.8f, -22f),
                new Vector3(8f, 1.6f, -23f),
                new Vector3(-20f, 1.3f, -24f),
                new Vector3(-12f, 0.1f, -6f),
                new Vector3(14f, 0.2f, -4f),
                new Vector3(-35f, 0.3f, 2f),
                new Vector3(12f, 1.8f, 14f),
                new Vector3(-10f, 4.5f, 28f),
                new Vector3(2f, 5.6f, 38f),
                new Vector3(-4f, 8.9f, 46f),
            };

            for (int i = 0; i < foragePoints.Length; i++)
            {
                var item = profile.ForageItems[i % profile.ForageItems.Count];
                var snappedPos = SnapToChamberSurface(parent, chamberCollider, foragePoints[i], 0.05f);
                CreateNutrientObject(forageGroup, item, snappedPos, glowMat, rnd);
            }
        }

        static List<Light> BuildHandcraftedCrystalLighting(Transform parent, Color mainColor, Color accentColor)
        {
            var lightGroup = new GameObject("Ánh Sáng Vực Thẳm Tinh Thể").transform;
            lightGroup.SetParent(parent, false);
            var lights = new List<Light>();

            // 1. 2 Đèn Vòm Khuếch Tán Vực Thẳm ở y = 26m (bù suy hao 1/d² để nhìn rõ toàn bộ địa hình gãy khúc & mỏm đá)
            Color domeMist = new Color(0.72f, 0.60f, 0.92f);
            lights.Add(CreatePointLight(lightGroup, "Đèn_Vòm_Nam", new Vector3(-14f, 26f, -8f), domeMist, 68f, 115f));
            lights.Add(CreatePointLight(lightGroup, "Đèn_Vòm_Bắc", new Vector3(14f, 26f, 18f), domeMist, 68f, 115f));

            // 2. 3 Đèn Hắt Ngược Đáy Vực lơ lửng giữa khe vực ở y = -5.5m (không dí sát vào tinh thể gây chói)
            lights.Add(CreatePointLight(lightGroup, "Đèn_ĐáyVực_Tây", new Vector3(-22f, -5.5f, -3f), new Color(0.88f, 0.22f, 0.48f), 24f, 52f));
            lights.Add(CreatePointLight(lightGroup, "Đèn_ĐáyVực_TrungTâm", new Vector3(0f, -5.5f, -3f), new Color(0.72f, 0.28f, 0.96f), 26f, 55f));
            lights.Add(CreatePointLight(lightGroup, "Đèn_ĐáyVực_Đông", new Vector3(20f, -5.5f, -3f), new Color(0.86f, 0.22f, 0.52f), 24f, 52f));

            // 3. 4 Đèn Vùng Đi Bộ ở độ cao 10m..17m (thềm xuất phát, cổ đất Tây-Đông, hố dịch độc Bắc)
            lights.Add(CreatePointLight(lightGroup, "Đèn_ThềmĐá_Nam", new Vector3(0f, 11f, -24f), new Color(0.76f, 0.52f, 0.96f), 30f, 60f));
            lights.Add(CreatePointLight(lightGroup, "Đèn_CổĐất_Tây", new Vector3(-34f, 10f, -2f), new Color(0.72f, 0.48f, 0.92f), 26f, 55f));
            lights.Add(CreatePointLight(lightGroup, "Đèn_CổĐất_Đông", new Vector3(36f, 10f, 2f), new Color(0.72f, 0.48f, 0.92f), 26f, 55f));
            lights.Add(CreatePointLight(lightGroup, "Đèn_HốDịchĐộc_Bắc", new Vector3(2f, 17f, 42f), new Color(0.70f, 0.38f, 0.96f), 28f, 60f));

            return lights;
        }

        static Light CreatePointLight(Transform parent, string name, Vector3 localPos, Color color, float intensity, float range)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.intensity = intensity;
            l.range = range;
            l.shadows = LightShadows.None;
            return l;
        }

        static void BuildGrandPerimeterCliffs(
            Transform parent,
            ChamberNode node,
            int seed,
            float chamberRadius,
            IReadOnlyList<float> tunnelAnglesDeg,
            Material wallMat,
            System.Random rnd)
        {
            var cliffGroup = new GameObject("Vách Đá Đại Cảnh").transform;
            cliffGroup.SetParent(parent, false);

            const int count = 16;
            for (int i = 0; i < count; i++)
            {
                float angleDeg = i * (360f / count) + (float)rnd.NextDouble() * 10f;
                if (IsNearAnyAngle(angleDeg, tunnelAnglesDeg, 22f)) continue;

                float rad = angleDeg * Mathf.Deg2Rad;
                float dist = chamberRadius * 0.80f; // ~60m, nằm ngay chân vách bao
                float cx = Mathf.Cos(rad) * dist;
                float cz = Mathf.Sin(rad) * dist;
                float cy = MazeTerrainMath.GroundHeightAt(cx, cz, seed, chamberRadius, tunnelAnglesDeg, node.HasHighLedge);

                var cliffFbx = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFolder + $"Cliff_{i % 4}.fbx");
                if (cliffFbx != null)
                {
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(cliffFbx, cliffGroup);
                    go.name = $"VáchĐá_{i:00}";
                    go.transform.localPosition = new Vector3(cx, cy - 2.6f, cz); // Cắm sâu chân -2.6m vào sườn vách
                    go.transform.localRotation = Quaternion.LookRotation(-new Vector3(cx, 0f, cz), Vector3.up) * Quaternion.Euler(0f, (float)(rnd.NextDouble() - 0.5) * 35f, 0f);
                    go.transform.localScale = new Vector3(1.5f, Mathf.Lerp(1.3f, 2.1f, (float)rnd.NextDouble()), 1.5f);
                }
                else
                {
                    MazeTerrainMesh.PlaceFramingBoulder(cliffGroup, new Vector3(cx, cy - 1.5f, cz), 5.5f, seed + i, wallMat);
                }
            }
        }

        static void BuildGrandThemeLandmarks(
            Transform parent,
            ChamberNode node,
            int seed,
            float chamberRadius,
            IReadOnlyList<float> tunnelAnglesDeg,
            Material wallMat,
            Material mossMat,
            Material glowMat,
            Color accentColor)
        {
            var landmarkGroup = new GameObject("Đại Cảnh Chủ Đề").transform;
            landmarkGroup.SetParent(parent, false);

            switch (node.Theme)
            {
                case ChamberTheme.LumaMushroomGrove:
                case ChamberTheme.SporeCurtain:
                    // Rừng đại thụ nấm khổng lồ vây quanh thung lũng giữa hang
                    PlaceGroundedModel(landmarkGroup, "Mushroom_Giant_0", -6f, 4f, 3.6f, 25f, 0.5f, seed, chamberRadius, tunnelAnglesDeg, node.HasHighLedge, glowMat);
                    PlaceGroundedModel(landmarkGroup, "Mushroom_Giant_1", 9f, -5f, 3.1f, 110f, 0.5f, seed, chamberRadius, tunnelAnglesDeg, node.HasHighLedge, glowMat);
                    PlaceGroundedModel(landmarkGroup, "Mushroom_Giant_2", -12f, -10f, 2.8f, 210f, 0.5f, seed, chamberRadius, tunnelAnglesDeg, node.HasHighLedge, glowMat);
                    PlaceGroundedModel(landmarkGroup, "LumaShroom_0", 2f, 12f, 2.6f, 45f, 0.3f, seed, chamberRadius, tunnelAnglesDeg, node.HasHighLedge, glowMat);
                    break;

                case ChamberTheme.RootCanopy:
                case ChamberTheme.DeepRootTangle:
                    // Cung rễ đại thụ khổng lồ & thân gỗ mục làm đường hầm tự nhiên
                    PlaceGroundedModel(landmarkGroup, "RootArch", 0f, 0f, 2.4f, 35f, 0.8f, seed, chamberRadius, tunnelAnglesDeg, node.HasHighLedge, glowMat);
                    PlaceGroundedModel(landmarkGroup, "Log_Hollow", 12f, -9f, 1.9f, -40f, 0.6f, seed, chamberRadius, tunnelAnglesDeg, node.HasHighLedge, glowMat);
                    PlaceGroundedModel(landmarkGroup, "RootArch", -16f, 11f, 2.0f, 125f, 0.8f, seed, chamberRadius, tunnelAnglesDeg, node.HasHighLedge, glowMat);
                    break;

                case ChamberTheme.DampReedHollow:
                case ChamberTheme.MossSanctuary:
                    // Hồ nước rêu cạn tự nhiên ở vùng trũng giữa khoang + thảm lau/nấm
                    float poolY = MazeTerrainMath.GroundHeightAt(0f, 0f, seed, chamberRadius, tunnelAnglesDeg, node.HasHighLedge);
                    var poolGo = new GameObject("Hồ Nước Rêu Cạn");
                    poolGo.transform.SetParent(landmarkGroup, false);
                    poolGo.transform.localPosition = new Vector3(0f, poolY + 0.08f, 0f);
                    poolGo.AddComponent<MeshFilter>().sharedMesh = CaveMeshes.Disc(13.5f, 0.06f, 24);
                    var poolMat = LakeGround.OpaqueMaterial("Maze_ShallowPool", new Color(0.14f, 0.42f, 0.48f), 0.78f, false, accentColor * 0.25f);
                    poolGo.AddComponent<MeshRenderer>().sharedMaterial = poolMat;
                    PlaceGroundedModel(landmarkGroup, "Mushroom_Giant_0", -14f, 6f, 2.2f, 40f, 0.4f, seed, chamberRadius, tunnelAnglesDeg, node.HasHighLedge, glowMat);
                    break;

                case ChamberTheme.CrystalVein:
                case ChamberTheme.SaltAbyss:
                    // Rừng trụ tinh thể khổng lồ mọc lên từ lòng đất
                    PlaceGroundedModel(landmarkGroup, "CrystalCluster_0", -8f, 7f, 4.2f, 20f, 0.7f, seed, chamberRadius, tunnelAnglesDeg, node.HasHighLedge, glowMat);
                    PlaceGroundedModel(landmarkGroup, "CrystalCluster_1", 10f, -6f, 3.8f, 130f, 0.7f, seed, chamberRadius, tunnelAnglesDeg, node.HasHighLedge, glowMat);
                    PlaceGroundedModel(landmarkGroup, "CrystalCluster_2", 3f, 15f, 3.4f, 250f, 0.7f, seed, chamberRadius, tunnelAnglesDeg, node.HasHighLedge, glowMat);
                    break;

                case ChamberTheme.LimestoneStalactite:
                    // Măng đá và cụm thạch nhũ lớn vươn lên giữa hẻm đá
                    PlaceGroundedModel(landmarkGroup, "Cliff_2", -12f, 8f, 1.3f, 65f, 2.2f, seed, chamberRadius, tunnelAnglesDeg, node.HasHighLedge, glowMat);
                    PlaceGroundedModel(landmarkGroup, "Cliff_3", 14f, -10f, 1.2f, 210f, 2.2f, seed, chamberRadius, tunnelAnglesDeg, node.HasHighLedge, glowMat);
                    PlaceGroundedModel(landmarkGroup, "CrystalCluster_0", 0f, -14f, 2.8f, 90f, 0.6f, seed, chamberRadius, tunnelAnglesDeg, node.HasHighLedge, glowMat);
                    break;

                case ChamberTheme.EmberBasalt:
                    // Vòng đá bazan cháy đỏ và cụm cầu nhiệt ở vùng trũng
                    PlaceGroundedModel(landmarkGroup, "Cliff_3", -11f, -8f, 1.35f, 30f, 2.2f, seed, chamberRadius, tunnelAnglesDeg, node.HasHighLedge, glowMat);
                    PlaceGroundedModel(landmarkGroup, "Cliff_1", 11f, 9f, 1.35f, 195f, 2.2f, seed, chamberRadius, tunnelAnglesDeg, node.HasHighLedge, glowMat);
                    PlaceGroundedModel(landmarkGroup, "BulbPlant_1", 0f, 5f, 2.6f, 0f, 0.3f, seed, chamberRadius, tunnelAnglesDeg, node.HasHighLedge, glowMat);
                    break;

                case ChamberTheme.FossilBoneyard:
                case ChamberTheme.SealedDragonTomb:
                    // Đại sảnh xương Cổ Long khổng lồ
                    PlaceGroundedModel(landmarkGroup, "DragonSkull_0", 0f, 10f, 2.6f, 180f, 0.5f, seed, chamberRadius, tunnelAnglesDeg, node.HasHighLedge, glowMat);
                    PlaceGroundedModel(landmarkGroup, "DragonRibcage_0", 0f, -2f, 2.7f, 0f, 0.4f, seed, chamberRadius, tunnelAnglesDeg, node.HasHighLedge, glowMat);
                    PlaceGroundedModel(landmarkGroup, "Rib_Bend_0", -14f, 4f, 2.4f, 45f, 0.8f, seed, chamberRadius, tunnelAnglesDeg, node.HasHighLedge, glowMat);
                    PlaceGroundedModel(landmarkGroup, "Rib_Fork_0", 14f, -4f, 2.4f, -135f, 0.8f, seed, chamberRadius, tunnelAnglesDeg, node.HasHighLedge, glowMat);
                    break;

                default:
                    PlaceGroundedModel(landmarkGroup, "Cliff_0", -10f, 8f, 1.15f, 50f, 2.0f, seed, chamberRadius, tunnelAnglesDeg, node.HasHighLedge, glowMat);
                    PlaceGroundedModel(landmarkGroup, "CrystalCluster_1", 10f, -8f, 2.6f, 120f, 0.6f, seed, chamberRadius, tunnelAnglesDeg, node.HasHighLedge, glowMat);
                    break;
            }
        }

        static GameObject PlaceGroundedModel(
            Transform parent,
            string modelName,
            float lx,
            float lz,
            float scale,
            float yawDeg,
            float sinkMeters,
            int seed,
            float chamberRadius,
            IReadOnlyList<float> tunnelAnglesDeg,
            bool hasHighLedge,
            Material glowMat)
        {
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFolder + modelName + ".fbx");
            if (fbx == null) return null;

            float ly = MazeTerrainMath.GroundHeightAt(lx, lz, seed, chamberRadius, tunnelAnglesDeg, hasHighLedge);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(fbx, parent);
            go.transform.localPosition = new Vector3(lx, ly - sinkMeters, lz);
            go.transform.localRotation = Quaternion.Euler(0f, yawDeg, 0f);
            go.transform.localScale = Vector3.one * scale;
            ApplyGlowTint(go, glowMat);
            return go;
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
            float usableRadius = chamberRadius * 0.74f; // ~55.5m

            // 1. Thảm thực vật mặt đất (nhấn chân nhẹ -0.18m để không bao giờ lơ lửng trên dốc nghiêng)
            if (profile.FloorFloraModels.Count > 0)
            {
                int propCount = node.Depth == ChamberDepth.Nong ? 75 : (node.Depth == ChamberDepth.Giua ? 55 : 40);
                var tuftPoints = new List<Vector3>(80);

                for (int i = 0; i < propCount; i++)
                {
                    string modelName = profile.FloorFloraModels[rnd.Next(profile.FloorFloraModels.Count)];
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFolder + modelName + ".fbx");
                    if (!TryPickFloorPoint(usableRadius, 5.5f, tunnelAnglesDeg, rnd, out float lx, out float lz)) continue;

                    float ly = MazeTerrainMath.GroundHeightAt(lx, lz, seed, chamberRadius, tunnelAnglesDeg, node.HasHighLedge);
                    if (prefab != null)
                    {
                        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, floraGroup);
                        go.transform.localPosition = new Vector3(lx, ly - 0.18f, lz);
                        go.transform.localRotation = Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f);
                        go.transform.localScale = Vector3.one * Mathf.Lerp(1.0f, 1.85f, (float)rnd.NextDouble());
                        ApplyGlowTint(go, glowMat);
                    }
                    tuftPoints.Add(new Vector3(lx, ly - 0.04f, lz));
                }

                if (node.Depth != ChamberDepth.Sau && tuftPoints.Count > 0)
                {
                    var grassMat = LakeGround.OpaqueMaterial($"Maze_Grass_{node.Theme}", Color.Lerp(floorColor, new Color(0.20f, 0.45f, 0.18f), 0.65f), 0.05f, true);
                    var tuftsGo = new GameObject("Thảm Cỏ Hang");
                    tuftsGo.transform.SetParent(floraGroup, false);
                    tuftsGo.AddComponent<MeshFilter>().sharedMesh = GroundMeshes.Tufts(tuftPoints, seed + 19, 0.55f);
                    tuftsGo.AddComponent<MeshRenderer>().sharedMaterial = grassMat;
                }
            }

            // 2. Đá tảng lớn cắm sâu gốc -0.65m vào sườn đồi (tuyệt đối không lơ lửng)
            for (int rIdx = 0; rIdx < 24; rIdx++)
            {
                if (!TryPickFloorPoint(usableRadius, 7f, tunnelAnglesDeg, rnd, out float rx, out float rz)) continue;
                float ry = MazeTerrainMath.GroundHeightAt(rx, rz, seed, chamberRadius, tunnelAnglesDeg, node.HasHighLedge);
                float rockScale = Mathf.Lerp(1.1f, 2.8f, (float)rnd.NextDouble());
                MazeTerrainMesh.PlaceFramingBoulder(floraGroup, new Vector3(rx, ry - 0.65f, rz), rockScale, seed + rIdx * 13, wallMat);
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
            float usableRadius = chamberRadius * 0.70f;

            for (int i = 0; i < profile.ForageCount; i++)
            {
                var item = profile.ForageItems[i % profile.ForageItems.Count];
                if (!TryPickFloorPoint(usableRadius, 5.0f, tunnelAnglesDeg, rnd, out float lx, out float lz)) continue;
                float ly = MazeTerrainMath.GroundHeightAt(lx, lz, seed, chamberRadius, tunnelAnglesDeg, node.HasHighLedge);
                CreateNutrientObject(forageGroup, item, new Vector3(lx, ly - 0.05f, lz), glowMat, rnd);
            }
        }

        static void CreateNutrientObject(Transform parent, ThemeForageItem item, Vector3 localPos, Material glowMat, System.Random rnd)
        {
            var go = new GameObject(item.DisplayName);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f);
            go.transform.localScale = Vector3.one * Mathf.Lerp(0.95f, 1.35f, (float)rnd.NextDouble());

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
                visual.transform.localPosition = item.IsMineral ? new Vector3(0f, 0.1f, 0f) : Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
                foreach (var t in visual.GetComponentsInChildren<Transform>(true))
                    GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
                ApplyGlowTint(visual, glowMat);
            }
        }

        static void PlaceEncounterSituation(
            Transform parent,
            ChamberNode node,
            int seed,
            float chamberRadius,
            IReadOnlyList<float> tunnelAnglesDeg,
            Material wallMat,
            List<Light> chamberLights)
        {
            if (node.Encounter == ChamberEncounterKind.None) return;

            var situationGo = new GameObject($"TìnhHuốngMôiTrường_{node.Encounter}");
            situationGo.transform.SetParent(parent, false);
            var covers = new List<Transform>();
            var geysers = new List<Transform>();

            float[] anchorAngles = { 45f, 165f, 285f };
            for (int i = 0; i < anchorAngles.Length; i++)
            {
                float rad = anchorAngles[i] * Mathf.Deg2Rad;
                float dist = node.Encounter == ChamberEncounterKind.ThermalGeyserSurge ? 16f : 22f;
                float px = Mathf.Cos(rad) * dist;
                float pz = Mathf.Sin(rad) * dist;
                if (node.Encounter == ChamberEncounterKind.AncientDragonBreath)
                {
                    if (i > 0) continue;
                    px = 0f;
                    pz = -2f;
                }
                float py = MazeTerrainMath.GroundHeightAt(px, pz, seed, chamberRadius, tunnelAnglesDeg, node.HasHighLedge);
                Vector3 localPos = new Vector3(px, py - 0.15f, pz);

                switch (node.Encounter)
                {
                    case ChamberEncounterKind.AcidSporeRain:
                        covers.Add(CreateFeatureMarker(situationGo.transform, $"Tán Nấm Trú Mưa_{i}", localPos, $"Mushroom_Giant_{i % 3}", 2.6f, new Color(0.35f, 0.95f, 0.65f), wallMat));
                        break;

                    case ChamberEncounterKind.CrystalPulseStorm:
                        covers.Add(CreateFeatureMarker(situationGo.transform, $"Tinh Thể Chắn Sóng_{i}", localPos, $"CrystalCluster_{i % 3}", 2.5f, new Color(0.35f, 0.85f, 1.0f), wallMat));
                        break;

                    case ChamberEncounterKind.ThermalGeyserSurge:
                        if (i < 2)
                            geysers.Add(CreateFeatureMarker(situationGo.transform, $"Mạch Phun Ngầm_{i}", localPos, "BulbPlant_0", 1.8f, new Color(0.35f, 0.80f, 1.0f), wallMat));
                        break;

                    case ChamberEncounterKind.AwakeningRoots:
                        if (i < 2)
                            geysers.Add(CreateFeatureMarker(situationGo.transform, $"Gốc Rễ Thức Tỉnh_{i}", localPos, i == 0 ? "RootArch" : "Log_Hollow", 1.6f, new Color(0.55f, 1.0f, 0.45f), wallMat));
                        break;

                    case ChamberEncounterKind.AncientDragonBreath:
                        var dragonCover = new GameObject("Vùng Trú Lồng Xương Rồng").transform;
                        dragonCover.SetParent(situationGo.transform, false);
                        dragonCover.localPosition = localPos;
                        covers.Add(dragonCover);
                        break;
                }
            }

            var zone = situationGo.AddComponent<ChamberEncounterZone>();
            zone.Configure(node.Encounter, offsetSeconds: node.Id * 7f, covers, geysers, chamberLights);
        }

        static Transform CreateFeatureMarker(
            Transform parent,
            string name,
            Vector3 localPos,
            string fbxName,
            float scale,
            Color lightColor,
            Material wallMat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;

            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFolder + fbxName + ".fbx");
            if (fbx != null)
            {
                var vis = (GameObject)PrefabUtility.InstantiatePrefab(fbx, go.transform);
                vis.transform.localScale = Vector3.one * scale;
            }
            else
            {
                MazeTerrainMesh.PlaceFramingBoulder(go.transform, Vector3.zero, scale, name.GetHashCode(), wallMat);
            }

            var l = new GameObject("Ánh Sáng Dấu Hiệu").AddComponent<Light>();
            l.transform.SetParent(go.transform, false);
            l.transform.localPosition = new Vector3(0f, 2.2f, 0f);
            l.type = LightType.Point;
            l.range = 14f;
            l.color = lightColor;
            l.intensity = 2.4f;
            l.shadows = LightShadows.None;
            return go.transform;
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
                    spawnerGo.transform.localPosition = new Vector3(-12f, 0f, -10f);
                    var so = new SerializedObject(spawnerGo.AddComponent<PreySpawner>());
                    so.FindProperty("prefab").objectReferenceValue = preyPrefab;
                    so.FindProperty("count").intValue = profile.PreyCount;
                    so.FindProperty("radius").floatValue = chamberRadius * 0.50f;
                    so.FindProperty("minDistanceFromCenter").floatValue = 3.0f;
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
                    spawnerGo.transform.localPosition = new Vector3(12f, 0f, 14f);
                    var so = new SerializedObject(spawnerGo.AddComponent<PredatorSpawner>());
                    so.FindProperty("prefab").objectReferenceValue = predPrefab;
                    so.FindProperty("count").intValue = profile.PredatorCount;
                    so.FindProperty("radius").floatValue = chamberRadius * 0.45f;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }

        static List<Light> BuildChamberLighting(
            Transform parent,
            ChamberThemeProfile profile,
            Color mainColor,
            Color accentColor)
        {
            var list = new List<Light>(5);
            var mainLight = new GameObject("Ánh Sáng Chính").AddComponent<Light>();
            mainLight.transform.SetParent(parent, false);
            mainLight.transform.localPosition = new Vector3(0f, 24f, 0f);
            mainLight.type = LightType.Point;
            mainLight.range = 110f;
            mainLight.color = mainColor;
            mainLight.intensity = 55f;
            mainLight.shadows = LightShadows.None;
            list.Add(mainLight);

            Vector3[] accentOffsets =
            {
                new Vector3(-28f, 11f, 24f),
                new Vector3(28f, 11f, -24f),
                new Vector3(-24f, 11f, -28f),
                new Vector3(24f, 11f, 28f)
            };
            for (int i = 0; i < accentOffsets.Length; i++)
            {
                var accent = new GameObject($"Ánh Sáng Điểm Nhấn_{i}").AddComponent<Light>();
                accent.transform.SetParent(parent, false);
                accent.transform.localPosition = accentOffsets[i];
                accent.type = LightType.Point;
                accent.range = 52f;
                accent.color = accentColor;
                accent.intensity = 22f;
                accent.shadows = LightShadows.None;
                list.Add(accent);
            }
            return list;
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
                if (r > maxRadius - 10f && IsNearAnyAngle(angleDeg, tunnelAnglesDeg, 22f)) continue;
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
