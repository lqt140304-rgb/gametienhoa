using Evolve.Abilities;
using Evolve.Creatures;
using Evolve.Organs;
using Evolve.Player;
using Evolve.Survival;
using Evolve.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Evolve.EditorTools
{
    /// <summary>
    /// Menu "Evolve → Build Prototype Scene": builds Prototype_01 in one click
    /// (ground, slime, camera, nutrients, monochrome vision, vibration sense, HUD, ability).
    /// Safe to run again: existing assets are reused, the scene is rebuilt.
    /// </summary>
    public static class PrototypeSceneBuilder
    {
        const string Root = "Assets/_Project";
        const string ScenePath = Root + "/Scenes/Prototype_01.unity";
        const int NutrientCount = 20;
        const float SpawnRadius = 20f;

        [MenuItem("Evolve/Build Prototype Scene")]
        public static void Build()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null &&
                !EditorUtility.DisplayDialog("Prototype_01",
                    "Scene Prototype_01 đã tồn tại. Dựng lại từ đầu? (thay đổi thủ công trong scene sẽ mất)",
                    "Dựng lại", "Huỷ"))
                return;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            foreach (var f in new[] { "Scenes", "Prefabs", "ScriptableObjects", "Materials", "Settings" })
                EnsureFolder(Root + "/" + f);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var shared = CreateSharedAssets();
            var preyPrefab = shared.PreyPrefab;
            var predatorPrefab = shared.PredatorPrefab;
            var nutrientPrefab = shared.NutrientPrefab;

            // Ground
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(10, 1, 10);
            ground.GetComponent<Renderer>().sharedMaterial = shared.GroundMat;

            var slime = CreatePlayer(shared, new Vector3(0, 1, 0));

            // Food
            var foodRoot = new GameObject("Nutrients").transform;
            var rng = new System.Random(140304);
            for (int i = 0; i < NutrientCount; i++)
            {
                var n = (GameObject)PrefabUtility.InstantiatePrefab(nutrientPrefab, foodRoot);
                float angle = (float)(rng.NextDouble() * Mathf.PI * 2);
                float dist = Mathf.Lerp(3f, SpawnRadius, (float)rng.NextDouble());
                n.transform.position = new Vector3(Mathf.Cos(angle) * dist, 0.25f, Mathf.Sin(angle) * dist);
            }

            // Prey
            var spawner = new GameObject("Prey Spawner").AddComponent<PreySpawner>();
            Set(spawner, "prefab", preyPrefab.GetComponent<Prey>());

            // Desert: hot strip on the east edge with richer food and more predators
            var desert = GameObject.CreatePrimitive(PrimitiveType.Plane);
            desert.name = "Desert";
            Object.DestroyImmediate(desert.GetComponent<Collider>());
            desert.transform.position = new Vector3(37.5f, 0.01f, 0f);
            desert.transform.localScale = new Vector3(2.5f, 1f, 5f);
            desert.GetComponent<Renderer>().sharedMaterial = Mat("Sand", new Color(0.85f, 0.72f, 0.45f), "Universal Render Pipeline/Lit");

            var zoneGo = new GameObject("Desert Zone (50°C)");
            zoneGo.transform.position = new Vector3(37.5f, 3f, 0f);
            var zoneCol = zoneGo.AddComponent<BoxCollider>();
            zoneCol.isTrigger = true;
            zoneCol.size = new Vector3(25f, 6f, 50f);
            var zone = zoneGo.AddComponent<TemperatureZone>();
            zone.Ambient = 50f;
            zone.DisplayName = "Hoang mạc";

            var desertFood = new GameObject("Nutrients (Desert)").transform;
            for (int i = 0; i < 12; i++)
            {
                var n = (GameObject)PrefabUtility.InstantiatePrefab(nutrientPrefab, desertFood);
                n.transform.position = new Vector3(Mathf.Lerp(28f, 48f, (float)rng.NextDouble()), 0.25f, Mathf.Lerp(-22f, 22f, (float)rng.NextDouble()));
            }

            // Predators: two in the desert, one roaming the far side of the plains
            PredatorSpawnerAt("Predator Spawner (Desert)", new Vector3(38f, 0f, 0f), 2, 10f, predatorPrefab);
            PredatorSpawnerAt("Predator Spawner (Plains)", new Vector3(-25f, 0f, 25f), 1, 10f, predatorPrefab);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = slime;
            Debug.Log("Đã dựng " + ScenePath + ". Bấm Play: WASD trườn, Space nảy, Q Dị Năng, giữ chuột phải xoay camera. Hoang mạc ở phía đông (x > 25).");
        }

        /// <summary>Assets shared by scene builders (materials, SOs, creature prefabs). Created once, reused after.</summary>
        internal sealed class SharedAssets
        {
            public Material GroundMat, SlimeMat;
            public SurvivalConfig Config;
            public BurstDashAbility Dash;
            public GameObject PreyPrefab, PredatorPrefab, NutrientPrefab, PulsePrefab;
        }

        internal static SharedAssets CreateSharedAssets()
        {
            // Assets
            var groundMat = Mat("Ground", new Color(0.35f, 0.45f, 0.3f), "Universal Render Pipeline/Lit");
            var slimeMat = Mat("Slime", new Color(0.45f, 0.85f, 0.55f), "Universal Render Pipeline/Lit");
            var foodMat = Mat("Nutrient", new Color(0.95f, 0.8f, 0.25f), "Universal Render Pipeline/Lit");
            var pulseMat = Mat("Pulse", new Color(0.5f, 0.9f, 1f), "Universal Render Pipeline/Particles/Unlit");

            var config = Asset<SurvivalConfig>(Root + "/ScriptableObjects/SurvivalConfigSO.asset");
            var dash = Asset<BurstDashAbility>(Root + "/ScriptableObjects/BurstDashSO.asset", a =>
            {
                a.DisplayName = "Bộc Phá Màng";
                a.Description = "Co bóp màng cơ thể dữ dội để lao về phía trước.";
                a.Tier = AbilityTier.C;
            });
            var eyeMat = Mat("Eye", new Color(0.95f, 0.95f, 0.95f), "Universal Render Pipeline/Lit");
            var pupilMat = Mat("Pupil", new Color(0.05f, 0.05f, 0.05f), "Universal Render Pipeline/Lit");
            var legMat = Mat("Pseudopod", new Color(0.3f, 0.65f, 0.4f), "Universal Render Pipeline/Lit");
            var preyMat = Mat("Prey", new Color(0.9f, 0.45f, 0.55f), "Universal Render Pipeline/Lit");

            var eyes = Asset<OrganSO>(Root + "/ScriptableObjects/Organ_Eyes.asset", o =>
            {
                o.DisplayName = "Mắt sơ khai";
                o.Description = "Cảm nhận được ánh sáng và màu sắc, nhìn xa hơn.";
                o.Slot = OrganSlot.Eyes;
                o.GrantsSight = true;
                o.VisualPrefab = EyesPrefab(eyeMat, pupilMat);
            });
            var legs = Asset<OrganSO>(Root + "/ScriptableObjects/Organ_Pseudopods.asset", o =>
            {
                o.DisplayName = "Chân giả";
                o.Description = "Những xúc tu ngắn giúp trườn nhanh hơn.";
                o.Slot = OrganSlot.Legs;
                o.SpeedMultiplier = 1.4f;
                o.VisualPrefab = LegsPrefab(legMat, "Organ_Pseudopods", 1f);
            });
            var legsB = Asset<OrganSO>(Root + "/ScriptableObjects/Organ_MuscularPseudopods.asset", o =>
            {
                o.DisplayName = "Chân giả cơ bắp";
                o.Description = "Xúc tu dày, co bóp mạnh: trườn nhanh hơn hẳn.";
                o.Slot = OrganSlot.Legs;
                o.Tier = OrganTier.B;
                o.SpeedMultiplier = 1.7f;
                o.GrowthExpRequired = 45f;
                o.VisualPrefab = LegsPrefab(Mat("Pseudopod_B", new Color(0.3f, 0.8f, 0.35f), "Universal Render Pipeline/Lit"), "Organ_MuscularPseudopods", 1.3f);
            });
            var legsA = Asset<OrganSO>(Root + "/ScriptableObjects/Organ_RunnerLegs.asset", o =>
            {
                o.DisplayName = "Chân chạy";
                o.Description = "Chân có khớp đầu tiên: tốc độ gấp đôi.";
                o.Slot = OrganSlot.Legs;
                o.Tier = OrganTier.A;
                o.SpeedMultiplier = 2f;
                o.GrowthExpRequired = 70f;
                o.VisualPrefab = LegsPrefab(Mat("Pseudopod_A", new Color(0.3f, 0.5f, 0.9f), "Universal Render Pipeline/Lit"), "Organ_RunnerLegs", 1.6f);
            });
            var preyDrops = Asset<DropTableSO>(Root + "/ScriptableObjects/DropTable_Prey.asset", t =>
            {
                // Chân chạy (A) không rơi từ con mồi nhỏ (đã chốt trong Bestiary).
                t.Organs.AddRange(new[] { eyes, legs, legsB });
            });
            var membrane = Asset<OrganSO>(Root + "/ScriptableObjects/Organ_HeatMembrane.asset", o =>
            {
                o.DisplayName = "Màng chịu nhiệt";
                o.Description = "Lớp màng dày phản xạ nhiệt: chịu nóng thêm 20°C.";
                o.Slot = OrganSlot.Skin;
                o.Tier = OrganTier.C;
                o.HeatResistance = 20f;
                o.VisualPrefab = MembranePrefab(Mat("HeatMembrane", new Color(0.95f, 0.55f, 0.2f), "Universal Render Pipeline/Lit"));
            });
            var spikes = Asset<OrganSO>(Root + "/ScriptableObjects/Organ_ToxicSpikes.asset", o =>
            {
                o.DisplayName = "Gai độc";
                o.Description = "Gai nhọn trên lưng: kẻ nào cắn bạn sẽ bị đâm lại 15 sát thương.";
                o.Slot = OrganSlot.Back;
                o.Tier = OrganTier.B;
                o.ContactDamage = 15f;
                o.GrowthExpRequired = 45f;
                o.VisualPrefab = SpikesPrefab(Mat("Spike", new Color(0.45f, 0.2f, 0.55f), "Universal Render Pipeline/Lit"));
            });
            EnsureOrgans(preyDrops, membrane, spikes);

            // Skills granted by tier B organs (Bestiary: tier C has none). Proposed numbers; tune in the assets.
            var charge = Asset<ChargeAbility>(Root + "/ScriptableObjects/ChargeSO.asset", a =>
            {
                a.DisplayName = "Xung Phong Húc";
                a.Description = "Dồn sức cơ bắp lao thẳng tới, đủ nhanh để húc văng kẻ săn mồi.";
                a.Tier = AbilityTier.B;
                a.EnergyCost = 20f;
                a.Cooldown = 6f;
                a.Force = 9f;
            });
            var spikeBurst = Asset<SpikeBurstAbility>(Root + "/ScriptableObjects/SpikeBurstSO.asset", a =>
            {
                a.DisplayName = "Xả Gai";
                a.Description = "Bắn một chùm gai về phía hồng tâm, gây sát thương kẻ săn mồi trúng đòn.";
                a.Tier = AbilityTier.B;
                a.EnergyCost = 20f;
                a.Cooldown = 8f;
            });
            GrantAbility(legsB, charge);
            GrantAbility(spikes, spikeBurst);
            var predatorDrops = Asset<DropTableSO>(Root + "/ScriptableObjects/DropTable_Predator.asset", t =>
            {
                t.DropChance = 1f;
                // Theo Bestiary mục 3.2, quái bậc B: 45 / 48 / 7 / 0.
                t.WeightC = 45f; t.WeightB = 48f; t.WeightA = 7f; t.WeightS = 0f;
            });
            EnsureOrgans(predatorDrops, eyes, legs, legsB, legsA, membrane, spikes);

            var preyPrefab = PreyPrefab(preyMat, preyDrops);
            var predatorPrefab = PredatorPrefab(
                Mat("Predator", new Color(0.35f, 0.15f, 0.3f), "Universal Render Pipeline/Lit"),
                Mat("PredatorEye", new Color(1f, 0.15f, 0.1f), "Universal Render Pipeline/Lit"),
                predatorDrops);
            var nutrientPrefab = NutrientPrefab(foodMat);
            var pulsePrefab = PulsePrefab(pulseMat);

            GelVisuals.RebuildOrganPrefabs();

            return new SharedAssets
            {
                GroundMat = groundMat, SlimeMat = slimeMat, Config = config, Dash = dash,
                PreyPrefab = preyPrefab, PredatorPrefab = predatorPrefab,
                NutrientPrefab = nutrientPrefab, PulsePrefab = pulsePrefab,
            };
        }

        /// <summary>Slime collider radius at BodyScale 1 (base diameter = 2 × this). Shelter gaps are sized from it.</summary>
        internal const float SlimeRadius = 0.5f;

        /// <summary>Slime player + follow camera (Camera.main) + vision volume + HUD. Returns the slime.</summary>
        internal static GameObject CreatePlayer(SharedAssets shared, Vector3 position)
        {
            // Slime
            var slime = new GameObject("Slime");
            slime.transform.position = position;
            slime.AddComponent<SphereCollider>().radius = SlimeRadius;
            var rb = slime.AddComponent<Rigidbody>();
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "Visual";
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            visual.transform.SetParent(slime.transform, false);
            visual.GetComponent<Renderer>().sharedMaterial = shared.SlimeMat;
            GelVisuals.StyleBody(visual);
            foreach (OrganSlot slot in System.Enum.GetValues(typeof(OrganSlot)))
                Socket(visual.transform, slot, Evolve.Visuals.SlimeSockets.Position(slot));

            var host = slime.AddComponent<OrganHost>();
            slime.AddComponent<OrganInteractor>();
            var stats = slime.AddComponent<SurvivalStats>();
            Set(stats, "config", shared.Config);
            slime.AddComponent<ClimateSensor>();
            slime.AddComponent<WaterSensor>();   // thirst only switches on in scenes with a WaterZone
            slime.AddComponent<ShelterSensor>();
            slime.AddComponent<SporeSensor>();      // spore clouds only exist in some scenes
            slime.AddComponent<ContagionSensor>();  // Thối Gel spreads between players
            slime.AddComponent<SkillBar>();
            var controller = slime.AddComponent<SlimeController>();
            slime.AddComponent<BodyFeedback>();
            Set(controller, "visual", visual.transform);
            var caster = slime.AddComponent<AbilityCaster>();
            SetArray(caster, "randomPool", new Object[] { shared.Dash });
            var sense = slime.AddComponent<VibrationSense>();
            Set(sense, "pulsePrefab", shared.PulsePrefab.GetComponent<ParticleSystem>());

            // Camera
            var cam = Camera.main;
            cam.transform.SetPositionAndRotation(position + new Vector3(0, 3, -6), Quaternion.Euler(25, 0, 0));
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.08f, 0.08f);
            cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            var follow = cam.gameObject.AddComponent<ThirdPersonCamera>();
            Set(follow, "target", slime.transform);
            Set(controller, "cameraTransform", cam.transform);

            // Vision (global volume + fog)
            var volumeGo = new GameObject("Vision Volume");
            var volume = volumeGo.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = VisionProfile();
            var vision = volumeGo.AddComponent<MonochromeVision>();
            Set(vision, "host", host);

            // HUD
            var hud = new GameObject("HUD").AddComponent<SurvivalHUD>();
            Set(hud, "stats", stats);

            return slime;
        }

        static readonly Vector3 BackSocket = new Vector3(0f, 0.3f, -0.25f);

        internal static void PredatorSpawnerAt(string name, Vector3 pos, int count, float radius, GameObject prefab)
        {
            var go = new GameObject(name);
            go.transform.position = pos;
            var sp = go.AddComponent<PredatorSpawner>();
            Set(sp, "prefab", prefab.GetComponent<Predator>());
            var so = new SerializedObject(sp);
            so.FindProperty("count").intValue = count;
            so.FindProperty("radius").floatValue = radius;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Add organs missing from a drop table (keeps the user's weights and existing entries).</summary>
        /// <summary>Give an organ its skill if it has none (keeps skills the project owner already set).</summary>
        static void GrantAbility(OrganSO organ, AbilitySO ability)
        {
            if (organ == null || ability == null || organ.GrantedAbility != null) return;
            organ.GrantedAbility = ability;
            EditorUtility.SetDirty(organ);
        }

        static void EnsureOrgans(DropTableSO table, params OrganSO[] organs)
        {
            bool changed = false;
            foreach (var o in organs)
                if (o != null && !table.Organs.Contains(o)) { table.Organs.Add(o); changed = true; }
            if (changed) EditorUtility.SetDirty(table);
        }

        static GameObject MembranePrefab(Material mat)
        {
            const string path = Root + "/Prefabs/Organ_HeatMembrane.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var root = new GameObject("Organ_HeatMembrane");
            Part(PrimitiveType.Cylinder, "Band", root.transform, Vector3.zero, new Vector3(1.06f, 0.04f, 1.06f), mat);
            Part(PrimitiveType.Cylinder, "BandTop", root.transform, new Vector3(0f, 0.22f, 0f), new Vector3(0.92f, 0.03f, 0.92f), mat);
            Part(PrimitiveType.Cylinder, "BandLow", root.transform, new Vector3(0f, -0.22f, 0f), new Vector3(0.92f, 0.03f, 0.92f), mat);
            return SavePrefab(root, path);
        }

        static GameObject SpikesPrefab(Material mat)
        {
            const string path = Root + "/Prefabs/Organ_ToxicSpikes.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var root = new GameObject("Organ_ToxicSpikes");
            var offsets = new[]
            {
                new Vector3(0f, 0.08f, 0.05f), new Vector3(0.15f, 0f, 0.02f), new Vector3(-0.15f, 0f, 0.02f),
                new Vector3(0.1f, -0.06f, -0.12f), new Vector3(-0.1f, -0.06f, -0.12f), new Vector3(0f, 0.02f, -0.1f)
            };
            foreach (var off in offsets)
            {
                var spike = Part(PrimitiveType.Capsule, "Spike", root.transform, off, new Vector3(0.05f, 0.12f, 0.05f), mat);
                Vector3 outward = (BackSocket + off).normalized; // point away from body centre
                spike.transform.localRotation = Quaternion.FromToRotation(Vector3.up, outward);
            }
            return SavePrefab(root, path);
        }

        static GameObject PredatorPrefab(Material body, Material eye, DropTableSO drops)
        {
            const string path = Root + "/Prefabs/Predator.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var root = new GameObject("Predator");
            root.transform.position = new Vector3(0f, 0.35f, 0f);
            var col = root.AddComponent<CapsuleCollider>();
            col.direction = 2;
            col.radius = 0.35f;
            col.height = 1.3f;
            col.center = new Vector3(0f, 0f, 0.1f);
            root.AddComponent<Rigidbody>().isKinematic = true;
            var predator = root.AddComponent<Predator>();
            Set(predator, "drops", drops);

            Part(PrimitiveType.Sphere, "Body", root.transform, Vector3.zero, new Vector3(0.7f, 0.55f, 1.1f), body);
            var head = Part(PrimitiveType.Sphere, "Head", root.transform, new Vector3(0f, 0.1f, 0.6f), Vector3.one * 0.45f, body);
            Part(PrimitiveType.Sphere, "Jaw", root.transform, new Vector3(0f, -0.05f, 0.8f), new Vector3(0.3f, 0.12f, 0.25f), body);
            foreach (float x in new[] { -0.12f, 0.12f })
                Part(PrimitiveType.Sphere, "Eye", head.transform, new Vector3(x / 0.45f, 0.35f, 0.35f), Vector3.one * 0.22f, eye);
            return SavePrefab(root, path);
        }

        static void Socket(Transform parent, OrganSlot slot, Vector3 localPos)
        {
            var go = new GameObject("Socket_" + slot);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.AddComponent<OrganSocket>().Slot = slot;
        }

        internal static GameObject Part(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        static GameObject SavePrefab(GameObject go, string path)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        static GameObject EyesPrefab(Material white, Material black)
        {
            const string path = Root + "/Prefabs/Organ_Eyes.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var root = new GameObject("Organ_Eyes");
            foreach (float x in new[] { -0.13f, 0.13f })
            {
                var eye = Part(PrimitiveType.Sphere, "Eye", root.transform, new Vector3(x, 0f, 0f), Vector3.one * 0.18f, white);
                Part(PrimitiveType.Sphere, "Pupil", eye.transform, new Vector3(0f, 0f, 0.4f), Vector3.one * 0.5f, black);
            }
            return SavePrefab(root, path);
        }

        static GameObject LegsPrefab(Material mat, string name, float size)
        {
            string path = $"{Root}/Prefabs/{name}.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var root = new GameObject(name);
            foreach (var p in new[] { new Vector3(-0.25f, 0f, 0.2f), new Vector3(0.25f, 0f, 0.2f), new Vector3(-0.25f, 0f, -0.2f), new Vector3(0.25f, 0f, -0.2f) })
            {
                var leg = Part(PrimitiveType.Capsule, "Pseudopod", root.transform, p, new Vector3(0.12f, 0.1f, 0.12f) * size, mat);
                leg.transform.localRotation = Quaternion.Euler(p.z * 60f, 0f, -p.x * 60f);
            }
            return SavePrefab(root, path);
        }

        static GameObject PreyPrefab(Material mat, DropTableSO drops)
        {
            const string path = Root + "/Prefabs/Prey.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
            {
                // Upgrade prefabs made before drop tables existed (keeps tuned values).
                var old = existing.GetComponent<Prey>();
                if (old != null && new SerializedObject(old).FindProperty("drops").objectReferenceValue == null)
                {
                    Set(old, "drops", drops);
                    EditorUtility.SetDirty(existing);
                }
                return existing;
            }

            var root = new GameObject("Prey");
            root.transform.position = new Vector3(0f, 0.2f, 0f);
            var col = root.AddComponent<CapsuleCollider>();
            col.direction = 2; // along Z
            col.radius = 0.18f;
            col.height = 0.55f;
            root.AddComponent<Rigidbody>().isKinematic = true;
            var prey = root.AddComponent<Prey>();
            Set(prey, "drops", drops);

            Part(PrimitiveType.Sphere, "Body", root.transform, Vector3.zero, new Vector3(0.35f, 0.3f, 0.55f), mat);
            Part(PrimitiveType.Sphere, "Head", root.transform, new Vector3(0f, 0.05f, 0.28f), Vector3.one * 0.22f, mat);
            return SavePrefab(root, path);
        }

        static GameObject NutrientPrefab(Material mat)
        {
            const string path = Root + "/Prefabs/Nutrient.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Nutrient";
            go.transform.localScale = Vector3.one * 0.5f;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            go.GetComponent<Collider>().isTrigger = true;
            go.AddComponent<Nutrient>();
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        static GameObject PulsePrefab(Material mat)
        {
            const string path = Root + "/Prefabs/VibrationPulse.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var go = new GameObject("VibrationPulse");
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.loop = false;
            main.duration = 0.5f;
            main.startLifetime = 0.8f;
            main.startSpeed = 2f;
            main.startSize = 0.15f;
            main.startColor = new Color(0.5f, 0.9f, 1f);
            main.playOnAwake = true;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 20) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.3f;

            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = mat;
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        static VolumeProfile VisionProfile()
        {
            const string path = Root + "/Settings/VisionProfile.asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, path);
            }
            if (!profile.Has<ColorAdjustments>())
            {
                var ca = profile.Add<ColorAdjustments>(true);
                ca.name = nameof(ColorAdjustments);
                AssetDatabase.AddObjectToAsset(ca, profile);
                EditorUtility.SetDirty(profile);
            }
            return profile;
        }

        internal static Material Mat(string name, Color color, string shaderName)
        {
            string path = $"{Root}/Materials/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null) return mat;

            var shader = Shader.Find(shaderName) ?? Shader.Find("Universal Render Pipeline/Lit");
            mat = new Material(shader) { name = name };
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            else mat.color = color;
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        internal static T Asset<T>(string path, System.Action<T> init = null) where T : ScriptableObject
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a != null) return a;
            a = ScriptableObject.CreateInstance<T>();
            init?.Invoke(a);
            AssetDatabase.CreateAsset(a, path);
            return a;
        }

        internal static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null) { Debug.LogError($"{target.GetType().Name}.{field} không tồn tại"); return; }
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void SetArray(Object target, string field, Object[] values)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null) { Debug.LogError($"{target.GetType().Name}.{field} không tồn tại"); return; }
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            EnsureFolder(path.Substring(0, slash));
            AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
        }

        internal static void AddToBuildSettings(string path)
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(s => s.path == path);
            scenes.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
