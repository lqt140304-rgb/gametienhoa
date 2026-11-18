using System.Collections.Generic;
using Evolve.Creatures;
using Evolve.Ecology;
using Evolve.Organs;
using Evolve.Survival;
using Evolve.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using P = Evolve.EditorTools.PrototypeSceneBuilder;

namespace Evolve.EditorTools
{
    /// <summary>
    /// Menu "Evolve → Build Underroot Lake": builds Underroot_Lake (Hồ Khởi Sinh) — the first biome with
    /// an abstract Region population, dens, a tier-B incursion and the shared slime/HUD/camera setup.
    /// Origin = lake centre, y = 0 water surface. Safe to run again: assets are reused, the scene is rebuilt.
    /// </summary>
    public static class UnderrootLakeBuilder
    {
        const string Root = "Assets/_Project";
        const string ScenePath = Root + "/Scenes/Underroot_Lake.unity";
        const string SO = Root + "/ScriptableObjects/";
        const string ShallowTag = "ShallowWater";
        const string Lit = "Universal Render Pipeline/Lit";
        const string TerrainFolder = Root + "/Terrain";
        const int TerrainSeed = 1337;
        const int HeightmapResolution = 1025; // about 0.5 m per point over 500 m
        /// <summary>Dome wide enough to cover the zone corners (radial distance up to ~340 m).</summary>
        static readonly float DomeRadius = WorldScale.ZoneSize * 0.75f;
        const float DomeHeight = 30f;  // the root roof (the canopy hangs at about 25 m)

        /// <summary>Largest body scale that fits a Root Shelter (BodySize.ScaleFor(5) = 1.3; 6 organs = 1.36 does not fit).</summary>
        const float ShelterMaxScale = 1.3f;
        /// <summary>Shelter entrance/interior width: real slime base diameter × max scale + 5 cm (0.6 × 1.3 + 0.05 = 0.83 m).</summary>
        static readonly float ShelterGap = P.SlimeRadius * P.SlimeScale * 2f * ShelterMaxScale + 0.05f;

        [MenuItem("Evolve/Build Underroot Lake")]
        public static void Build()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null &&
                !EditorUtility.DisplayDialog("Underroot_Lake",
                    "Scene Underroot_Lake đã tồn tại. Dựng lại từ đầu? (thay đổi thủ công trong scene sẽ mất)",
                    "Dựng lại", "Huỷ"))
                return;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            foreach (var f in new[] { "Scenes", "Prefabs", "ScriptableObjects", "Materials", "Settings", "Terrain" })
                P.EnsureFolder(Root + "/" + f);
            EnsureTag(ShallowTag);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // ---- Assets (shared with the prototype; prefabs upgraded in place, tuned values kept) ----
            var shared = P.CreateSharedAssets();
            var preyPrefab = EnsureEmitter(shared.PreyPrefab, 0.4f, false);
            var predatorPrefab = EnsureEmitter(shared.PredatorPrefab, 1f, true);
            var preyCPrefab = PreyCPrefab(preyPrefab);

            var d = P.Asset<SpeciesSO>(SO + "Species_D_Mudmite.asset", s =>
            {
                s.Id = "D_Mudmite"; s.Prefab = preyPrefab; s.Tier = OrganTier.D;
                s.StartCount = 18; s.Capacity = 32; s.BirthsPerMinute = 0.6f;
                s.FoodPerHeadPerMinute = 0.2f; s.Prey = null; s.KillsPerHeadPerMinute = 0f;
            });
            var c = P.Asset<SpeciesSO>(SO + "Species_C_Rootcrawler.asset", s =>
            {
                s.Id = "C_Rootcrawler"; s.Prefab = preyCPrefab; s.Tier = OrganTier.C;
                s.StartCount = 10; s.Capacity = 16; s.BirthsPerMinute = 0.35f;
                s.Prey = d; s.KillsPerHeadPerMinute = 0.05f;
            });
            var b = P.Asset<SpeciesSO>(SO + "Species_B_Hunter.asset", s =>
            {
                s.Id = "B_Hunter"; s.Prefab = predatorPrefab; s.Tier = OrganTier.B;
                s.StartCount = 2; s.Capacity = 3; s.BirthsPerMinute = 0.02f;
                s.Prey = c; s.KillsPerHeadPerMinute = 0.15f;
            });
            FillMissing(d, preyPrefab, null);
            FillMissing(c, preyCPrefab, d);
            FillMissing(b, predatorPrefab, c);

            var mud = P.Mat("Lake_Mud", new Color(0.24f, 0.2f, 0.15f), Lit);
            var water = P.Mat("Lake_Water", new Color(0.2f, 0.38f, 0.45f), Lit);
            var shallow = P.Mat("Lake_Shallow", new Color(0.3f, 0.48f, 0.5f), Lit);
            var dirtyWater = P.Mat("Lake_DirtyWater", new Color(0.2f, 0.2f, 0.11f), Lit);
            var dark = P.Mat("Lake_DeepPocket", new Color(0.02f, 0.02f, 0.03f), Lit);
            var root = P.Mat("Lake_Root", new Color(0.32f, 0.22f, 0.14f), Lit);
            var humus = P.Mat("Lake_Humus", new Color(0.18f, 0.12f, 0.08f), Lit);
            var rock = P.Mat("Lake_Rock", new Color(0.35f, 0.35f, 0.33f), Lit);
            var fungus = P.Mat("Lake_Fungus", new Color(0.7f, 0.6f, 0.85f), Lit);
            var denMat = P.Mat("Lake_Den", new Color(0.45f, 0.32f, 0.2f), Lit);

            // ---- Zone (480 m): terrain, dome roof, hanging roots, landmarks, lights, boundary ----
            foreach (var sun in Object.FindObjectsByType<Light>())
                if (sun.type == LightType.Directional) sun.intensity = 0.35f;

            var domeMat = P.Mat("Lake_Dome", new Color(0.05f, 0.05f, 0.06f), Lit);
            BuildTerrain();
            BuildDome(domeMat);
            BuildRoots(root);
            BuildLandmarks();
            BuildLights();
            BuildBoundary();

            // Layout random for the nutrients and mushrooms below. The old ceiling loop used to draw 28 values
            // from this seed first; skipping them keeps every nutrient and mushroom exactly where it was.
            var rng = new System.Random(50501);
            for (int i = 0; i < 28; i++) rng.NextDouble();

            // ---- Regions: one per populated biome (population numbers live per region) ----
            var humusRegion = MakeRegion("Region (Bai Mun Re)", ZoneLayout.Humus, 90f, d);
            var sporeRegion = MakeRegion("Region (Vuon Bao Tu)", ZoneLayout.Spore, 90f, d, c);
            var edgeRegion = MakeRegion("Region (Ria San Moi)", ZoneLayout.HuntEdge, 140f, d, c, b); // wide deactivate radius so the hunter does not vanish while chasing

            // ---- Vũng Sinh Mệnh (life pool) ----
            var pool = new GameObject("Vung Sinh Menh (Life Pool)").transform;
            Vector3 firstSpawn = Vector3.zero;
            // The slimes appear on the dry south shore of the Life Shallows bay.
            var spawnXZ = ZoneLayout.LifeShallows.Center;
            while (ZoneLayout.DepthAt(spawnXZ.x, spawnXZ.y) > 0f && spawnXZ.y > -200f) spawnXZ.y -= 1f;
            var spawnShore = new Vector3(spawnXZ.x, 0f, spawnXZ.y - 9f);
            for (int i = 0; i < 8; i++)
            {
                float ang = i * Mathf.PI * 2f / 8f; // chord 2·5·sin(22.5°) ≈ 3.8 m
                var sp = new GameObject("Spawn Point " + i);
                sp.transform.SetParent(pool, false);
                sp.transform.position = G(spawnShore + new Vector3(Mathf.Cos(ang) * 5f, 0.6f, Mathf.Sin(ang) * 5f));
                if (i == 0) firstSpawn = sp.transform.position;
            }

            // ---- Water: drinkable trigger boxes over every wet cell of the layout (lake, Life Shallows bay, stream, pools) ----
            // Swimming and wading read ZoneLayout.DepthAt directly; these boxes only say "you can drink here".
            BuildWaterZones();

            // ---- Chân Rễ Cổ Thụ (north): 3 U-shaped root shelters (gap 1.35 m, ShelterZone inside), cool 18°C ----
            var roots = new GameObject("Chan Re Co Thu (Ancient Roots)").transform;
            Zone(roots, new Vector3(ZoneLayout.RootFoot.Center.x, 3f, ZoneLayout.RootFoot.Center.y), new Vector3(50f, 6f, 16f), 18f, "Chân Rễ Cổ Thụ");
            // the three small root shelters stand just north of the Great Hall (the hall itself fills the middle of the area)
            foreach (float dx in new[] { -22f, 17f, 29f })
                Shelter(roots, new Vector3(ZoneLayout.RootFoot.Center.x + dx, 0f, ZoneLayout.RootFoot.Center.y + ZoneLayout.CaveRadii.y + 7f), root);

            // ---- Bãi Mùn Rễ (east): warm 26°C, 6 nutrients, 2 D dens ----
            var humusRoot = new GameObject("Bai Mun Re (Humus Bed)").transform;
            var H = new Vector3(ZoneLayout.Humus.Center.x, 0f, ZoneLayout.Humus.Center.y);
            Zone(humusRoot, H + new Vector3(0f, 3f, 0f), new Vector3(20f, 6f, 30f), 26f, "Bãi Mùn Rễ");
            for (int i = 0; i < 6; i++)
            {
                var n = (GameObject)PrefabUtility.InstantiatePrefab(shared.NutrientPrefab, humusRoot);
                n.transform.position = G(H + new Vector3(Mathf.Lerp(-7f, 7f, (float)rng.NextDouble()), 0.25f, Mathf.Lerp(-12f, 12f, (float)rng.NextDouble())));
                var nso = new SerializedObject(n.GetComponent<Nutrient>());
                nso.FindProperty("foodKind").enumValueIndex = (int)FoodKind.AmberBud; // the humus bed's own food
                nso.ApplyModifiedPropertiesWithoutUndo();
            }
            MakeDen("Den D (Mudmite) 1", humusRoot, H + new Vector3(-3f, 0f, 8f), d, humusRegion, denMat);
            MakeDen("Den D (Mudmite) 2", humusRoot, H + new Vector3(4f, 0f, -8f), d, humusRegion, denMat);
            // Stagnant puddle (dirty): quenches thirst, disease risk comes in 0.5d. x 164..172 keeps clear of den 2.
            Water(ShallowBox("Vung Nuoc Dong (Dirty Puddle)", humusRoot, G(new Vector3(ZoneLayout.PuddleCenter.x, 0f, ZoneLayout.PuddleCenter.y)), new Vector2(9f, 9f), dirtyWater), false, "Vũng nước đọng (Bãi Mùn Rễ)");

            // ---- Vườn Bào Tử (west): mushroom clusters, 2 C dens ----
            var garden = new GameObject("Vuon Bao Tu (Spore Garden)").transform;
            var S = new Vector3(ZoneLayout.Spore.Center.x, 0f, ZoneLayout.Spore.Center.y);
            for (int i = 0; i < 9; i++)
            {
                var pos = G(S + new Vector3(Mathf.Lerp(-8f, 8f, (float)rng.NextDouble()), 0f, Mathf.Lerp(-15f, 15f, (float)rng.NextDouble())));
                float h = Mathf.Lerp(0.8f, 2.2f, (float)rng.NextDouble());
                Visual(PrimitiveType.Cylinder, "Stalk", garden, pos + new Vector3(0f, h * 0.5f, 0f), new Vector3(0.25f, h * 0.5f, 0.25f), root);
                Visual(PrimitiveType.Sphere, "Cap", garden, pos + new Vector3(0f, h, 0f), new Vector3(h, h * 0.4f, h), fungus);
            }
            MakeDen("Den C (Rootcrawler) 1", garden, S + new Vector3(2f, 0f, 9f), c, sporeRegion, denMat);
            MakeDen("Den C (Rootcrawler) 2", garden, S + new Vector3(-3f, 0f, -9f), c, sporeRegion, denMat);
            MakeDen("Den D (Mudmite) 3", garden, S + new Vector3(12f, 0f, -20f), d, sporeRegion, denMat); // prey for the C of this region
            MakeDen("Den D (Mudmite) 5", garden, S + new Vector3(-6f, 0f, 22f), d, sporeRegion, denMat);
            SporeCloud("Spore Cloud (mushroom cluster)", garden, S + new Vector3(0f, 3f, 0f), new Vector3(20f, 6f, 30f)); // breathing it can give Mờ Bào Tử

            // ---- Rìa Săn Mồi (south-east): B den at the edge + 2 incursion entries ----
            var hunt = new GameObject("Ria San Moi (Hunting Edge)").transform;
            var E = new Vector3(ZoneLayout.HuntEdge.Center.x, 0f, ZoneLayout.HuntEdge.Center.y);
            MakeDen("Den B (Hunter)", hunt, E + new Vector3(0f, 0f, 28f), b, edgeRegion, denMat);
            MakeDen("Den C (Rootcrawler) 3", hunt, E + new Vector3(12f, 0f, 8f), c, edgeRegion, denMat); // prey for the hunter
            MakeDen("Den D (Mudmite) 4", hunt, E + new Vector3(-12f, 0f, -8f), d, edgeRegion, denMat); // so the C of this region can breed
            var e1 = new GameObject("Incursion Entry North").transform;
            e1.SetParent(hunt, false); e1.position = G(new Vector3(ZoneLayout.NorthEntry.x, 0.35f, ZoneLayout.NorthEntry.y));
            var e2 = new GameObject("Incursion Entry North 2").transform;
            e2.SetParent(hunt, false); e2.position = G(new Vector3(ZoneLayout.NorthEntry.x + 4f, 0.35f, ZoneLayout.NorthEntry.y - 8f));
            var incursion = new GameObject("Predator Incursion").AddComponent<PredatorIncursion>();
            incursion.transform.SetParent(hunt, false);
            var bHome = new GameObject("B Hunting Ground").transform;
            bHome.SetParent(hunt, false); bHome.position = G(new Vector3(ZoneLayout.HuntEdge.Center.x, 0f, 80f)); // the hunter patrols the north shore of the lake
            P.Set(incursion, "home", bHome);
            P.Set(incursion, "region", edgeRegion);
            var iso = new SerializedObject(incursion);
            iso.FindProperty("homeRadius").floatValue = 90f;
            iso.ApplyModifiedPropertiesWithoutUndo();
            P.Set(incursion, "predator", b);
            P.SetArray(incursion, "entries", new Object[] { e1, e2 });

            // ---- Glowing edible plants along the trails and in the biomes ----
            BuildVegetation(shared.NutrientPrefab);

            // ---- Co-op templates: WorldSync (client) reads .Prefab from these; count 0 → spawn nothing ----
            var preyTemplate = new GameObject("Net Template (Prey)").AddComponent<PreySpawner>();
            P.Set(preyTemplate, "prefab", preyPrefab.GetComponent<Prey>());
            var pso = new SerializedObject(preyTemplate);
            pso.FindProperty("count").intValue = 0;
            pso.ApplyModifiedPropertiesWithoutUndo();
            P.PredatorSpawnerAt("Net Template (Predator)", Vector3.zero, 0, 10f, predatorPrefab);

            // ---- Player, camera, vision, HUD ----
            var slime = P.CreatePlayer(shared, firstSpawn);
            if (Camera.main != null) Camera.main.farClipPlane = 700f; // the zone is 500 m across

            EditorSceneManager.SaveScene(scene, ScenePath);
            P.AddToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = slime;
            Debug.Log("Đã dựng " + ScenePath + " (300 m): hồ ở giữa, Vũng Sinh Mệnh phía nam, Chân Rễ phía tây, Bãi Mùn Rễ tây bắc, Rìa Săn Mồi phía bắc (lối B vào), Vườn Bào Tử đông bắc, Khe Thủy Mạch phía đông.");
        }

        const int VegetationSeed = 4242;
        const int SyncedFoodLimit = 400; // WorldSync sends at most this many food blobs

        static readonly string[] VegetationNames = { "Glow Moss", "Amber Bud", "Spore Pod", "Root Dew", "Ember Berry" };
        static readonly Vector2[] VegetationScale =
        {
            new Vector2(0.7f, 1.0f), new Vector2(0.9f, 1.3f), new Vector2(0.8f, 1.2f), new Vector2(0.6f, 0.9f), new Vector2(0.8f, 1.1f)
        };

        /// <summary>One emissive material per plant kind (a property block would not be saved in the scene).</summary>
        static Material GlowMat(string name, Color baseColor, Color glow, float intensity)
        {
            var m = P.Mat(name, baseColor, Lit);
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", glow * intensity);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            EditorUtility.SetDirty(m);
            return m;
        }

        static FoodKind FoodKindFor(VegetationKind kind) => kind switch
        {
            VegetationKind.AmberBud => FoodKind.AmberBud,
            VegetationKind.SporePod => FoodKind.SporePod,
            VegetationKind.RootDew => FoodKind.RootDew,
            VegetationKind.EmberBerry => FoodKind.EmberBerry,
            _ => FoodKind.GlowMoss
        };

        static void AddBlocker(List<Bounds> list, Collider c, float margin)
        {
            if (c == null) return;
            var b = c.bounds;
            b.Expand(margin * 2f); // Expand grows the whole box: margin on each side
            list.Add(b);
        }

        /// <summary>Scatter the glowing plants (VegetationScatter) as Nutrient objects on the ground, away from water, dens and shelters.</summary>
        static void BuildVegetation(GameObject nutrientPrefab)
        {
            Physics.SyncTransforms(); // collider bounds must reflect the objects just created
            var blockers = new List<Bounds>();
            foreach (var w in Object.FindObjectsByType<WaterZone>()) AddBlocker(blockers, w.GetComponent<Collider>(), 2f);
            foreach (var sh in Object.FindObjectsByType<ShelterZone>()) AddBlocker(blockers, sh.GetComponent<Collider>(), 2f);
            foreach (var den in Object.FindObjectsByType<Den>()) blockers.Add(new Bounds(den.transform.position, new Vector3(10f, 10f, 10f)));
            foreach (var n in Object.FindObjectsByType<Nutrient>()) blockers.Add(new Bounds(n.transform.position, new Vector3(6f, 6f, 6f)));
            foreach (var t in Object.FindObjectsByType<Transform>())
            {
                if (t.name.StartsWith("Deep Pocket")) blockers.Add(new Bounds(t.position, new Vector3(16f, 16f, 16f)));
                if (t.name.StartsWith("Landmark ")) blockers.Add(new Bounds(t.position, new Vector3(11f, 1f, 11f))); // solid 7 m pillars (not the "Landmarks" parent)
            }

            bool Blocked(float x, float z)
            {
                if (ZoneLayout.DepthAt(x, z) > 0f) return true; // no plants in the water
                for (int i = 0; i < blockers.Count; i++)
                {
                    var b = blockers[i];
                    if (x >= b.min.x && x <= b.max.x && z >= b.min.z && z <= b.max.z) return true;
                }
                return false;
            }

            var mats = new[]
            {
                GlowMat("Veg_GlowMoss", new Color(0.1f, 0.35f, 0.3f), new Color(0.2f, 0.9f, 0.75f), 1.6f),
                GlowMat("Veg_AmberBud", new Color(0.5f, 0.3f, 0.05f), new Color(1f, 0.6f, 0.15f), 2f),
                GlowMat("Veg_SporePod", new Color(0.35f, 0.2f, 0.5f), new Color(0.7f, 0.4f, 1f), 2.2f),
                GlowMat("Veg_RootDew", new Color(0.5f, 0.75f, 0.8f), new Color(0.6f, 0.9f, 1f), 1.8f),
                GlowMat("Veg_EmberBerry", new Color(0.5f, 0.12f, 0.05f), new Color(1f, 0.35f, 0.1f), 2f),
            };
            var parents = new Transform[VegetationNames.Length];
            for (int i = 0; i < parents.Length; i++) parents[i] = new GameObject("Vegetation (" + VegetationNames[i] + ")").transform;
            // Their glow fades out with the slime's sight: without eyes they show only where its light or a scan reaches.
            foreach (var parent in parents) parent.gameObject.AddComponent<DetailCulling>().ShowDistance = 5000f;

            var points = VegetationScatter.Points(VegetationSeed, Blocked);
            var rng = new System.Random(VegetationSeed + 1);
            foreach (var pt in points)
            {
                int k = (int)pt.Kind;
                var go = (GameObject)PrefabUtility.InstantiatePrefab(nutrientPrefab, parents[k]);
                float size = Mathf.Lerp(VegetationScale[k].x, VegetationScale[k].y, (float)rng.NextDouble());
                float y = TerrainShape.HeightAt(pt.Position.x, pt.Position.y, TerrainSeed) + 0.25f * size; // sphere radius 0.25 x size: rests on the ground
                go.transform.position = new Vector3(pt.Position.x, y, pt.Position.y);
                go.transform.localScale = nutrientPrefab.transform.localScale * size;
                go.GetComponentInChildren<Renderer>().sharedMaterial = mats[k];
                var so = new SerializedObject(go.GetComponent<Nutrient>());
                so.FindProperty("totalNutrition").floatValue = 12f;   // a plant is a snack, not a meal
                so.FindProperty("respawnSeconds").floatValue = 180f;  // and grows back slowly
                so.FindProperty("foodKind").enumValueIndex = (int)FoodKindFor(pt.Kind); // which nutrients eating it gives
                so.ApplyModifiedPropertiesWithoutUndo();
                if (pt.Kind == VegetationKind.SporePod) go.AddComponent<GlowPulse>();
            }

            int total = Object.FindObjectsByType<Nutrient>().Length;
            if (total > SyncedFoodLimit)
                Debug.LogWarning($"Underroot_Lake có {total} cục thức ăn, vượt giới hạn đồng bộ co-op {SyncedFoodLimit}: người vào phòng sẽ thấy thức ăn lệch.");
            Debug.Log($"Thực vật phát quang: {points.Count} cây, tổng {total} cục thức ăn trong scene.");
        }

        static void SporeCloud(string name, Transform parent, Vector3 center, Vector3 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = G(center);
            var col = go.AddComponent<BoxCollider>();
            col.size = size;
            col.isTrigger = true;
            go.AddComponent<SporeZone>();
        }

        static Region MakeRegion(string name, Pad pad, float deactivateRadius, params SpeciesSO[] species)
        {
            var go = new GameObject(name);
            go.transform.position = new Vector3(pad.Center.x, 0f, pad.Center.y);
            var region = go.AddComponent<Region>();
            var so = new SerializedObject(region);
            so.FindProperty("size").vector3Value = new Vector3(pad.Radius * 2f, 40f, pad.Radius * 2f);
            so.FindProperty("activateRadius").floatValue = 70f;
            so.FindProperty("deactivateRadius").floatValue = deactivateRadius;
            so.ApplyModifiedPropertiesWithoutUndo();
            P.SetArray(region, "species", species);
            return region;
        }

        /// <summary>Roof height (m above the core surface) at a horizontal distance from the zone centre.</summary>
        static float DomeY(float distance)
        {
            float t = distance / DomeRadius;
            return DomeHeight * Mathf.Sqrt(Mathf.Max(0f, 1f - t * t));
        }

        static void BuildTerrain()
        {
            string texPath = TerrainFolder + "/Underroot_Mud.asset";
            string layerPath = TerrainFolder + "/Underroot_Zone1_Layer.terrainlayer";
            string dataPath = TerrainFolder + "/Underroot_Zone1.asset";

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            if (tex == null)
            {
                tex = new Texture2D(4, 4, TextureFormat.RGBA32, true);
                var px = new Color[16];
                for (int i = 0; i < px.Length; i++)
                {
                    float v = 0.9f + 0.1f * ((i * 7) % 5) / 4f;
                    px[i] = new Color(0.24f * v, 0.2f * v, 0.15f * v, 1f);
                }
                tex.SetPixels(px);
                tex.Apply();
                AssetDatabase.CreateAsset(tex, texPath);
            }
            var layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(layerPath);
            if (layer == null)
            {
                layer = new TerrainLayer { diffuseTexture = tex, tileSize = new Vector2(8f, 8f) };
                AssetDatabase.CreateAsset(layer, layerPath);
            }

            // Second layer: packed earth along the trails (lighter than the mud)
            string trailTexPath = TerrainFolder + "/Underroot_Trail.asset";
            string trailLayerPath = TerrainFolder + "/Underroot_Zone1_TrailLayer.terrainlayer";
            var trailTex = AssetDatabase.LoadAssetAtPath<Texture2D>(trailTexPath);
            if (trailTex == null)
            {
                trailTex = new Texture2D(4, 4, TextureFormat.RGBA32, true);
                var tp = new Color[16];
                for (int i = 0; i < tp.Length; i++)
                {
                    float v = 0.9f + 0.1f * ((i * 5) % 7) / 6f;
                    tp[i] = new Color(0.52f * v, 0.45f * v, 0.34f * v, 1f);
                }
                trailTex.SetPixels(tp);
                trailTex.Apply();
                AssetDatabase.CreateAsset(trailTex, trailTexPath);
            }
            var trailLayer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(trailLayerPath);
            if (trailLayer == null)
            {
                trailLayer = new TerrainLayer { diffuseTexture = trailTex, tileSize = new Vector2(8f, 8f) };
                AssetDatabase.CreateAsset(trailLayer, trailLayerPath);
            }

            AssetDatabase.DeleteAsset(dataPath); // heights are deterministic: always rebuild
            var data = new TerrainData { heightmapResolution = HeightmapResolution };
            data.size = new Vector3(WorldScale.ZoneSize, TerrainShape.TerrainHeight, WorldScale.ZoneSize);
            data.SetHeights(0, 0, TerrainShape.Heights(HeightmapResolution, TerrainSeed));
            data.terrainLayers = new[] { layer, trailLayer };
            const int alphaRes = 1024;
            data.alphamapResolution = alphaRes;
            var alpha = new float[alphaRes, alphaRes, 2];
            float cell = WorldScale.ZoneSize / alphaRes;
            float halfWidth = ZoneLayout.PathWidth * 0.5f;
            for (int zi = 0; zi < alphaRes; zi++)
                for (int xi = 0; xi < alphaRes; xi++)
                {
                    float wx = -WorldScale.ZoneSize * 0.5f + (xi + 0.5f) * cell;
                    float wz = -WorldScale.ZoneSize * 0.5f + (zi + 0.5f) * cell;
                    float trail = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(halfWidth, halfWidth + 2f, ZoneLayout.DistanceToPath(wx, wz)));
                    alpha[zi, xi, 0] = 1f - trail;  // mud
                    alpha[zi, xi, 1] = trail;       // trail
                }
            data.SetAlphamaps(0, 0, alpha);
            AssetDatabase.CreateAsset(data, dataPath); // must be an asset, or the scene loses its hills when reopened

            var go = Terrain.CreateTerrainGameObject(data);
            go.name = "Zone Terrain";
            float half = WorldScale.ZoneSize * 0.5f;
            go.transform.position = new Vector3(-half, -TerrainShape.BaseOffset, -half);
        }

        static void BuildDome(Material mat)
        {
            string meshPath = TerrainFolder + "/Underroot_Dome.asset";
            AssetDatabase.DeleteAsset(meshPath);
            var mesh = DomeMesh.Build(DomeRadius, DomeHeight, 48, 16);
            AssetDatabase.CreateAsset(mesh, meshPath);
            var go = new GameObject("Dome");
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var dome = go.AddComponent<MeshRenderer>();
            dome.sharedMaterial = mat;
            dome.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; // keep the sun lighting the lake as before
            go.isStatic = true; // visual only: no collider
        }

        static void BuildRoots(Material root)
        {
            var parent = new GameObject("Hanging Roots").transform;
            var rng = new System.Random(50501);
            for (int i = 0; i < 28; i++)
            {
                float ang = (float)(rng.NextDouble() * Mathf.PI * 2.0);
                float dist = Mathf.Lerp(30f, 230f, (float)rng.NextDouble());
                float len = Mathf.Lerp(8f, 18f, (float)rng.NextDouble());
                float width = Mathf.Lerp(2f, 4f, (float)rng.NextDouble());
                float topY = DomeY(dist);
                var r = Visual(PrimitiveType.Cylinder, "Hanging Root " + i, parent,
                    new Vector3(Mathf.Cos(ang) * dist, topY - len * 0.5f, Mathf.Sin(ang) * dist),
                    new Vector3(width, len * 0.5f, width), root); // Unity's cylinder is 2 m tall
                r.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.transform.rotation = Quaternion.Euler(Mathf.Lerp(-12f, 12f, (float)rng.NextDouble()), ang * Mathf.Rad2Deg,
                    Mathf.Lerp(-12f, 12f, (float)rng.NextDouble()));
            }
        }

        /// <summary>Huge root pillars from the ground to the roof to find your way by: one beside each biome and one at the stream end.</summary>
        static void BuildLandmarks()
        {
            var parent = new GameObject("Landmarks").transform;
            // (no pillar for the Root Foot: the Great Hall is its landmark)
            AddLandmark(parent, "Humus", Outward(ZoneLayout.Humus), new Color(0.3f, 0.2f, 0.08f));
            AddLandmark(parent, "Spore", Outward(ZoneLayout.Spore), new Color(0.45f, 0.3f, 0.55f));
            AddLandmark(parent, "HuntEdge", Outward(ZoneLayout.HuntEdge), new Color(0.5f, 0.15f, 0.12f));
        }

        /// <summary>A point 12 m outward (away from the zone centre) from a pad's centre.</summary>
        static Vector2 Outward(Pad pad) => pad.Center + pad.Center.normalized * 12f;

        static void AddLandmark(Transform parent, string name, Vector2 xz, Color color)
        {
            float ground = TerrainShape.HeightAt(xz.x, xz.y, TerrainSeed);
            const float sink = 3f; // bury the foot so the downhill side of the 7 m pillar doesn't float
            float len = DomeY(xz.magnitude) - ground + sink;
            var mat = P.Mat("Lake_Landmark_" + name, color, Lit);
            Solid(PrimitiveType.Cylinder, "Landmark " + name, parent, new Vector3(xz.x, ground - sink + len * 0.5f, xz.y),
                new Vector3(7f, len * 0.5f, 7f), mat);
        }

        static void BuildLights()
        {
            var parent = new GameObject("Root Glow Lights").transform;
            for (int ix = -2; ix <= 2; ix++)
                for (int iz = -2; iz <= 2; iz++)
                {
                    float x = ix * 95f, z = iz * 95f;
                    float y = Mathf.Min(60f, Mathf.Max(15f, DomeY(Mathf.Sqrt(x * x + z * z)) - 6f));
                    var lightGo = new GameObject("Root Glow");
                    lightGo.transform.SetParent(parent, false);
                    lightGo.transform.position = new Vector3(x, y, z);
                    var l = lightGo.AddComponent<Light>();
                    l.type = LightType.Point;
                    l.range = 130f;
                    l.intensity = 0.5f;
                    l.color = new Color(0.65f, 0.8f, 0.7f);
                }
        }

        /// <summary>Invisible walls at the zone rim, so nothing can leave the zone.</summary>
        static void BuildBoundary()
        {
            var parent = new GameObject("Zone Boundary").transform;
            float edge = WorldScale.ZoneSize * 0.5f - 1f;
            foreach (float side in new[] { 1f, -1f })
            {
                AddWall("Boundary Z", parent, new Vector3(0f, 60f, side * edge), new Vector3(WorldScale.ZoneSize, 120f, 2f));
                AddWall("Boundary X", parent, new Vector3(side * edge, 60f, 0f), new Vector3(2f, 120f, WorldScale.ZoneSize));
            }
        }

        static void AddWall(string name, Transform parent, Vector3 center, Vector3 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            go.AddComponent<BoxCollider>().size = size;
            go.isStatic = true;
        }

        /// <summary>Add a VibrationEmitter to a prefab asset if it has none (keeps tuned values on existing ones).</summary>
        static GameObject EnsureEmitter(GameObject prefab, float strength, bool footsteps)
        {
            if (prefab == null) return prefab;
            var existing = prefab.GetComponent<VibrationEmitter>();
            if (existing != null && (footsteps || !existing.Footsteps)) return prefab;
            string path = AssetDatabase.GetAssetPath(prefab);
            var contents = PrefabUtility.LoadPrefabContents(path);
            var em = contents.GetComponent<VibrationEmitter>();
            if (em == null) { em = contents.AddComponent<VibrationEmitter>(); em.strength = strength; }
            em.Footsteps = footsteps;
            PrefabUtility.SaveAsPrefabAsset(contents, path);
            PrefabUtility.UnloadPrefabContents(contents);
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        /// <summary>Tier-C crawler: a 1.6× variant of Prey.prefab (inherits Prey + VibrationEmitter).</summary>
        static GameObject PreyCPrefab(GameObject prey)
        {
            const string path = Root + "/Prefabs/Prey_C.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prey);
            inst.name = "Prey_C";
            inst.transform.localScale = Vector3.one * 1.6f;
            var ip = inst.transform.position; inst.transform.position = new Vector3(ip.x, 0.32f, ip.z);
            var variant = PrefabUtility.SaveAsPrefabAsset(inst, path);
            Object.DestroyImmediate(inst);
            return variant;
        }

        /// <summary>Repair references on species assets that exist but lost them (never overwrites set values).</summary>
        static void FillMissing(SpeciesSO s, GameObject prefab, SpeciesSO prey)
        {
            bool changed = false;
            if (s.Prefab == null) { s.Prefab = prefab; changed = true; }
            if (s.Prey == null && prey != null) { s.Prey = prey; changed = true; }
            if (changed) EditorUtility.SetDirty(s);
        }

        static void EnsureTag(string tag)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0) return;
            var so = new SerializedObject(assets[0]);
            var tags = so.FindProperty("tags");
            for (int i = 0; i < tags.arraySize; i++)
                if (tags.GetArrayElementAtIndex(i).stringValue == tag) return;
            tags.InsertArrayElementAtIndex(tags.arraySize);
            tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static GameObject Visual(PrimitiveType type, string name, Transform parent, Vector3 worldPos, Vector3 scale, Material mat)
        {
            var go = P.Part(type, name, parent, Vector3.zero, scale, mat);
            go.transform.position = worldPos;
            return go;
        }

        static GameObject Solid(PrimitiveType type, string name, Transform parent, Vector3 worldPos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = worldPos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            go.isStatic = true;
            return go;
        }

        static GameObject ShallowBox(string name, Transform parent, Vector3 center, Vector2 size, Material mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = center + new Vector3(0f, 0.5f, 0f);
            go.tag = ShallowTag;
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(size.x, 1f, size.y);
            var surface = P.Part(PrimitiveType.Cube, "Surface", go.transform, new Vector3(0f, -0.48f, 0f), new Vector3(size.x, 0.02f, size.y), mat);
            surface.isStatic = true;
            return go;
        }

        /// <summary>
        /// Drinkable trigger boxes over the layout's water: the zone is cut into 8 m rows, and each run of wet cells in a row
        /// becomes one box (about 40 boxes), from the deepest bed (-2 m) to 1 m above the surface.
        /// </summary>
        static void BuildWaterZones()
        {
            var parent = new GameObject("Water Zones").transform;
            const float cell = 8f;
            float half = WorldScale.ZoneSize * 0.5f;
            int boxes = 0;
            for (float z = -half + cell * 0.5f; z < half; z += cell)
            {
                float runStart = float.NaN;
                for (float x = -half + cell * 0.5f; x < half + cell; x += cell)
                {
                    bool wet = x < half && WetCell(x, z, cell);
                    if (wet && float.IsNaN(runStart)) runStart = x;
                    if (!wet && !float.IsNaN(runStart))
                    {
                        float x0 = runStart - cell * 0.5f, x1 = x - cell * 0.5f;
                        var go = new GameObject("Water " + boxes);
                        go.transform.SetParent(parent, false);
                        go.transform.position = new Vector3((x0 + x1) * 0.5f, -0.5f, z);
                        go.tag = ShallowTag;
                        var box = go.AddComponent<BoxCollider>();
                        box.isTrigger = true;
                        box.size = new Vector3(x1 - x0, 3f, cell);
                        var name = ZoneLayout.WaterVein.DistanceOutside(new Vector2((x0 + x1) * 0.5f, z)) <= 0f ? "Khe Thủy Mạch" : "Hồ Khởi Sinh";
                        Water(go, true, name);
                        runStart = float.NaN;
                        boxes++;
                    }
                }
            }
            Debug.Log($"Water zones: {boxes} boxes.");
        }

        /// <summary>A cell counts as water when its centre or any corner is under water (the boxes reach the shore).</summary>
        static bool WetCell(float x, float z, float cell)
        {
            float h = cell * 0.5f;
            return ZoneLayout.CleanDepthAt(x, z) > 0f || ZoneLayout.CleanDepthAt(x - h, z - h) > 0f || ZoneLayout.CleanDepthAt(x + h, z - h) > 0f
                || ZoneLayout.CleanDepthAt(x - h, z + h) > 0f || ZoneLayout.CleanDepthAt(x + h, z + h) > 0f;
        }

        /// <summary>Make a shallow-water trigger drinkable.</summary>
        static void Water(GameObject go, bool clean, string displayName)
        {
            var w = go.AddComponent<WaterZone>();
            w.Clean = clean;
            w.DisplayName = displayName;
        }

        /// <summary>The same point lifted (or lowered) onto the terrain; on open water it sits at the water level.</summary>
        static Vector3 G(Vector3 p)
        {
            float h = TerrainShape.HeightAt(p.x, p.z, TerrainSeed);
            if (ZoneLayout.CleanDepthAt(p.x, p.z) > 0f) h = Mathf.Max(h, 0f);   // on the water, not on the lake bed
            return new Vector3(p.x, p.y + h, p.z);
        }

        static void DeepPocket(string name, Vector3 center, Material mat)
        {
            var go = new GameObject(name);
            go.transform.position = center;
            Visual(PrimitiveType.Cylinder, "Darkness", go.transform, center + new Vector3(0f, 0.03f, 0f), new Vector3(10f, 0.01f, 10f), mat);
            // Solid blocker: cylinder mesh (r 0.5, h 2) scaled to r 5, h 1.5 → slimes bump into or stand on it.
            var blocker = new GameObject("Blocker");
            blocker.transform.SetParent(go.transform, false);
            blocker.transform.localScale = new Vector3(10f, 0.75f, 10f);
            var tmp = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            blocker.AddComponent<MeshCollider>().sharedMesh = tmp.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(tmp);
            blocker.isStatic = true;
        }

        static void Zone(Transform parent, Vector3 center, Vector3 size, float ambient, string displayName)
        {
            var go = new GameObject($"Temperature Zone ({ambient:0}°C)");
            go.transform.SetParent(parent, false);
            go.transform.position = G(center);
            var col = go.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = size;
            var z = go.AddComponent<TemperatureZone>();
            z.Ambient = ambient;
            z.DisplayName = displayName;
        }

        /// <summary>
        /// U-shaped root block opening south (towards the lake). Inner width = ShelterGap (1.35 m): a slime up to
        /// BodyScale 1.3 fits, a bigger one is stopped by the walls. A ShelterZone trigger fills the covered interior.
        /// </summary>
        static void Shelter(Transform parent, Vector3 center, Material mat)
        {
            center = G(center);
            var go = new GameObject("Root Shelter").transform;
            go.SetParent(parent, false);
            go.position = center;
            float gap = ShelterGap;
            const float thick = 1f, depth = 3f, h = 2f;
            float side = gap * 0.5f + thick * 0.5f;
            Solid(PrimitiveType.Cube, "Root L", go, center + new Vector3(-side, h * 0.5f, 0f), new Vector3(thick, h, depth), mat);
            Solid(PrimitiveType.Cube, "Root R", go, center + new Vector3(side, h * 0.5f, 0f), new Vector3(thick, h, depth), mat);
            Solid(PrimitiveType.Cube, "Root Back", go, center + new Vector3(0f, h * 0.5f, depth * 0.5f + thick * 0.5f), new Vector3(gap + 2f * thick, h, thick), mat);
            Solid(PrimitiveType.Cube, "Root Top", go, center + new Vector3(0f, h + 0.25f, 0.5f), new Vector3(gap + 2f * thick, 0.5f, depth + thick), mat);

            // Interior trigger: starts `inset` behind the mouth so a too-big slime pressing its sphere into the
            // opening (it bulges in ≤ its radius, max 0.75 m at scale 1.5) never touches it. Height h ≥ 2 m.
            const float inset = 0.8f;
            float zoneDepth = depth - inset;
            var zoneGo = new GameObject("Shelter Zone");
            zoneGo.transform.SetParent(go, false);
            zoneGo.transform.position = center + new Vector3(0f, h * 0.5f, depth * 0.5f - zoneDepth * 0.5f);
            var col = zoneGo.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(gap, h, zoneDepth);
            zoneGo.AddComponent<ShelterZone>().MaxBodyScale = ShelterMaxScale;
        }

        static Den MakeDen(string name, Transform parent, Vector3 pos, SpeciesSO species, Region region, Material mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = G(pos);
            P.Part(PrimitiveType.Sphere, "Mound", go.transform, new Vector3(0f, 0.2f, 0f), new Vector3(2.2f, 0.8f, 2.2f), mat);
            var den = go.AddComponent<Den>();
            den.Species = species;
            den.Region = region;
            return den;
        }
    }
}
