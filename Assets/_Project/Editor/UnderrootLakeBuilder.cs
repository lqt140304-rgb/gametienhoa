using Evolve.Creatures;
using Evolve.Ecology;
using Evolve.Organs;
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
        const int HeightmapResolution = 513;
        /// <summary>Dome wide enough to cover the zone corners (radial distance up to ~340 m).</summary>
        static readonly float DomeRadius = WorldScale.ZoneSize * 0.75f;
        const float DomeHeight = 85f;

        /// <summary>Largest body scale that fits a Root Shelter (BodySize.ScaleFor(5) = 1.3; 6 organs = 1.36 does not fit).</summary>
        const float ShelterMaxScale = 1.3f;
        /// <summary>Shelter entrance/interior width: real slime base diameter × max scale + 5 cm = 1.0 × 1.3 + 0.05 = 1.35 m.</summary>
        static readonly float ShelterGap = P.SlimeRadius * 2f * ShelterMaxScale + 0.05f;

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
                s.StartCount = 14; s.Capacity = 24; s.BirthsPerMinute = 0.6f;
                s.FoodPerHeadPerMinute = 0.2f; s.Prey = null; s.KillsPerHeadPerMinute = 0f;
            });
            var c = P.Asset<SpeciesSO>(SO + "Species_C_Rootcrawler.asset", s =>
            {
                s.Id = "C_Rootcrawler"; s.Prefab = preyCPrefab; s.Tier = OrganTier.C;
                s.StartCount = 6; s.Capacity = 10; s.BirthsPerMinute = 0.2f;
                s.Prey = d; s.KillsPerHeadPerMinute = 0.3f;
            });
            var b = P.Asset<SpeciesSO>(SO + "Species_B_Hunter.asset", s =>
            {
                s.Id = "B_Hunter"; s.Prefab = predatorPrefab; s.Tier = OrganTier.B;
                s.StartCount = 2; s.Capacity = 3; s.BirthsPerMinute = 0.02f;
                s.Prey = c; s.KillsPerHeadPerMinute = 0.5f;
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
            BuildLandmarks(root);
            BuildLights();
            BuildBoundary();

            // Layout random for the nutrients and mushrooms below. The old ceiling loop used to draw 28 values
            // from this seed first; skipping them keeps every nutrient and mushroom exactly where it was.
            var rng = new System.Random(50501);
            for (int i = 0; i < 28; i++) rng.NextDouble();

            // ---- Region (whole area) ----
            var regionGo = new GameObject("Region (Underroot Lake)");
            var region = regionGo.AddComponent<Region>();
            P.SetArray(region, "species", new Object[] { d, c, b });

            // ---- Vũng Sinh Mệnh (life pool) ----
            var pool = new GameObject("Vung Sinh Menh (Life Pool)").transform;
            Visual(PrimitiveType.Cylinder, "Pool Water", pool, new Vector3(0f, 0.02f, 0f), new Vector3(30f, 0.01f, 30f), water);
            Vector3 firstSpawn = Vector3.zero;
            for (int i = 0; i < 8; i++)
            {
                float ang = i * Mathf.PI * 2f / 8f; // chord 2·5·sin(22.5°) ≈ 3.8 m
                var sp = new GameObject("Spawn Point " + i);
                sp.transform.SetParent(pool, false);
                sp.transform.position = new Vector3(Mathf.Cos(ang) * 5f, 0.6f, Mathf.Sin(ang) * 5f);
                if (i == 0) firstSpawn = sp.transform.position;
            }

            // ---- Shallow water: trigger boxes y 0..1 (overlap the slime body, r 0.5 resting at y 0.5) + WaterZone ----
            // SlimeController has no speed hook yet → the ShallowWater tag is only a marker.
            var shallows = new GameObject("Shallow Water").transform;
            Water(ShallowBox("Shallows North", shallows, new Vector3(0f, 0f, 20f), new Vector2(24f, 8f), shallow), true, "Vũng Sinh Mệnh");
            Water(ShallowBox("Shallows West", shallows, new Vector3(-20f, 0f, 0f), new Vector2(8f, 24f), shallow), true, "Vũng Sinh Mệnh");
            Water(ShallowBox("Khe Thuy Mach (South Channel)", shallows, new Vector3(0f, 0f, -37.5f), new Vector2(6f, 45f), shallow), true, "Khe Thủy Mạch");

            // ---- Deep pockets: black disc + solid blocker so slimes cannot fall in ----
            DeepPocket("Deep Pocket West", new Vector3(-30f, 0f, 25f), dark);
            DeepPocket("Deep Pocket South", new Vector3(25f, 0f, -35f), dark);

            // ---- Chân Rễ Cổ Thụ (north): 3 U-shaped root shelters (gap 1.35 m, ShelterZone inside), cool 18°C ----
            var roots = new GameObject("Chan Re Co Thu (Ancient Roots)").transform;
            Zone(roots, new Vector3(0f, 3f, 50f), new Vector3(50f, 6f, 16f), 18f, "Chân Rễ Cổ Thụ");
            for (int i = 0; i < 3; i++) Shelter(roots, new Vector3(-16f + i * 16f, 0f, 50f), root);

            // ---- Bãi Mùn Rễ (east): warm 26°C, 6 nutrients, 2 D dens ----
            var humusRoot = new GameObject("Bai Mun Re (Humus Bed)").transform;
            Visual(PrimitiveType.Cube, "Humus Ground", humusRoot, new Vector3(40f, 0.01f, 0f), new Vector3(20f, 0.02f, 30f), humus);
            Zone(humusRoot, new Vector3(40f, 3f, 0f), new Vector3(20f, 6f, 30f), 26f, "Bãi Mùn Rễ");
            for (int i = 0; i < 6; i++)
            {
                var n = (GameObject)PrefabUtility.InstantiatePrefab(shared.NutrientPrefab, humusRoot);
                n.transform.position = new Vector3(Mathf.Lerp(33f, 47f, (float)rng.NextDouble()), 0.25f, Mathf.Lerp(-12f, 12f, (float)rng.NextDouble()));
            }
            MakeDen("Den D (Mudmite) 1", humusRoot, new Vector3(37f, 0f, 8f), d, region, denMat);
            MakeDen("Den D (Mudmite) 2", humusRoot, new Vector3(44f, 0f, -8f), d, region, denMat);
            // Stagnant puddle (dirty): quenches thirst, disease risk comes in 0.5d. x 34..42 keeps clear of den 2.
            Water(ShallowBox("Vung Nuoc Dong (Dirty Puddle)", humusRoot, new Vector3(38f, 0f, -8f), new Vector2(8f, 8f), dirtyWater), false, "Vũng nước đọng (Bãi Mùn Rễ)");

            // ---- Vườn Bào Tử (west): mushroom clusters, 2 C dens ----
            var garden = new GameObject("Vuon Bao Tu (Spore Garden)").transform;
            for (int i = 0; i < 9; i++)
            {
                var pos = new Vector3(Mathf.Lerp(-48f, -32f, (float)rng.NextDouble()), 0f, Mathf.Lerp(-15f, 15f, (float)rng.NextDouble()));
                float h = Mathf.Lerp(0.8f, 2.2f, (float)rng.NextDouble());
                Visual(PrimitiveType.Cylinder, "Stalk", garden, pos + new Vector3(0f, h * 0.5f, 0f), new Vector3(0.25f, h * 0.5f, 0.25f), root);
                Visual(PrimitiveType.Sphere, "Cap", garden, pos + new Vector3(0f, h, 0f), new Vector3(h, h * 0.4f, h), fungus);
            }
            MakeDen("Den C (Rootcrawler) 1", garden, new Vector3(-38f, 0f, 9f), c, region, denMat);
            MakeDen("Den C (Rootcrawler) 2", garden, new Vector3(-43f, 0f, -9f), c, region, denMat);

            // ---- Rìa Săn Mồi (south-east): B den at the edge + 2 incursion entries ----
            var hunt = new GameObject("Ria San Moi (Hunting Edge)").transform;
            MakeDen("Den B (Hunter)", hunt, new Vector3(55f, 0f, -55f), b, region, denMat);
            var e1 = new GameObject("Incursion Entry East").transform;
            e1.SetParent(hunt, false); e1.position = new Vector3(56f, 0.35f, -30f);
            var e2 = new GameObject("Incursion Entry South").transform;
            e2.SetParent(hunt, false); e2.position = new Vector3(30f, 0.35f, -56f);
            var incursion = new GameObject("Predator Incursion").AddComponent<PredatorIncursion>();
            incursion.transform.SetParent(hunt, false);
            var bHome = new GameObject("B Hunting Ground").transform;
            bHome.SetParent(hunt, false); bHome.position = new Vector3(-40f, 0f, 0f);
            P.Set(incursion, "home", bHome);
            P.Set(incursion, "region", region);
            P.Set(incursion, "predator", b);
            P.SetArray(incursion, "entries", new Object[] { e1, e2 });

            // ---- Co-op templates: WorldSync (client) reads .Prefab from these; count 0 → spawn nothing ----
            var preyTemplate = new GameObject("Net Template (Prey)").AddComponent<PreySpawner>();
            P.Set(preyTemplate, "prefab", preyPrefab.GetComponent<Prey>());
            var pso = new SerializedObject(preyTemplate);
            pso.FindProperty("count").intValue = 0;
            pso.ApplyModifiedPropertiesWithoutUndo();
            P.PredatorSpawnerAt("Net Template (Predator)", Vector3.zero, 0, 10f, predatorPrefab);

            // ---- Player, camera, vision, HUD ----
            var slime = P.CreatePlayer(shared, firstSpawn);
            if (Camera.main != null) Camera.main.farClipPlane = 800f; // the zone is 480 m across

            EditorSceneManager.SaveScene(scene, ScenePath);
            P.AddToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = slime;
            Debug.Log("Đã dựng " + ScenePath + ". Bãi Mùn Rễ (tổ D) ở phía đông, Vườn Bào Tử (tổ C) phía tây, Rìa Săn Mồi (B) đông nam.");
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

            AssetDatabase.DeleteAsset(dataPath); // heights are deterministic: always rebuild
            var data = new TerrainData { heightmapResolution = HeightmapResolution };
            data.size = new Vector3(WorldScale.ZoneSize, TerrainShape.TerrainHeight, WorldScale.ZoneSize);
            data.SetHeights(0, 0, TerrainShape.Heights(HeightmapResolution, TerrainSeed));
            data.terrainLayers = new[] { layer };
            data.alphamapResolution = 64;
            var alpha = new float[64, 64, 1];
            for (int z = 0; z < 64; z++)
                for (int x = 0; x < 64; x++) alpha[z, x, 0] = 1f; // the single layer covers everything
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
                float dist = Mathf.Lerp(30f, 220f, (float)rng.NextDouble());
                float len = Mathf.Lerp(25f, 45f, (float)rng.NextDouble());
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

        /// <summary>Four huge root pillars from the ground to the roof, one per compass direction, to find your way by.</summary>
        static void BuildLandmarks(Material root)
        {
            var parent = new GameObject("Landmarks").transform;
            const float distance = 190f;
            foreach (var (name, dx, dz) in new[] { ("North", 0f, 1f), ("East", 1f, 0f), ("South", 0f, -1f), ("West", -1f, 0f) })
            {
                float x = dx * distance, z = dz * distance;
                float ground = TerrainShape.HeightAt(x, z, TerrainSeed);
                const float sink = 3f; // bury the foot so the downhill side of the 7 m pillar doesn't float
                float len = DomeY(distance) - ground + sink;
                Solid(PrimitiveType.Cylinder, "Landmark " + name, parent, new Vector3(x, ground - sink + len * 0.5f, z),
                    new Vector3(7f, len * 0.5f, 7f), root);
            }
        }

        static void BuildLights()
        {
            var parent = new GameObject("Root Glow Lights").transform;
            for (int ix = -2; ix <= 2; ix++)
                for (int iz = -2; iz <= 2; iz++)
                {
                    float x = ix * 100f, z = iz * 100f;
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

        /// <summary>Make a shallow-water trigger drinkable.</summary>
        static void Water(GameObject go, bool clean, string displayName)
        {
            var w = go.AddComponent<WaterZone>();
            w.Clean = clean;
            w.DisplayName = displayName;
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
            go.transform.position = center;
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
            go.transform.position = pos;
            P.Part(PrimitiveType.Sphere, "Mound", go.transform, new Vector3(0f, 0.2f, 0f), new Vector3(2.2f, 0.8f, 2.2f), mat);
            var den = go.AddComponent<Den>();
            den.Species = species;
            den.Region = region;
            return den;
        }
    }
}
