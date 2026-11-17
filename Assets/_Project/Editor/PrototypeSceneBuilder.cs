using Evolve.Abilities;
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
            var nutrientPrefab = NutrientPrefab(foodMat);
            var pulsePrefab = PulsePrefab(pulseMat);

            // Ground
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(10, 1, 10);
            ground.GetComponent<Renderer>().sharedMaterial = groundMat;

            // Slime
            var slime = new GameObject("Slime");
            slime.transform.position = new Vector3(0, 1, 0);
            slime.AddComponent<SphereCollider>().radius = 0.5f;
            var rb = slime.AddComponent<Rigidbody>();
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "Visual";
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            visual.transform.SetParent(slime.transform, false);
            visual.GetComponent<Renderer>().sharedMaterial = slimeMat;

            var stats = slime.AddComponent<SurvivalStats>();
            Set(stats, "config", config);
            var controller = slime.AddComponent<SlimeController>();
            Set(controller, "visual", visual.transform);
            var caster = slime.AddComponent<AbilityCaster>();
            SetArray(caster, "randomPool", new Object[] { dash });
            var sense = slime.AddComponent<VibrationSense>();
            Set(sense, "pulsePrefab", pulsePrefab.GetComponent<ParticleSystem>());

            // Camera
            var cam = Camera.main;
            cam.transform.SetPositionAndRotation(new Vector3(0, 4, -6), Quaternion.Euler(25, 0, 0));
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
            volumeGo.AddComponent<MonochromeVision>();

            // HUD
            var hud = new GameObject("HUD").AddComponent<SurvivalHUD>();
            Set(hud, "stats", stats);

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

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = slime;
            Debug.Log("Đã dựng " + ScenePath + ". Bấm Play: WASD trườn, Space nảy, Q Dị Năng, giữ chuột phải xoay camera.");
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

        static Material Mat(string name, Color color, string shaderName)
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

        static T Asset<T>(string path, System.Action<T> init = null) where T : ScriptableObject
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a != null) return a;
            a = ScriptableObject.CreateInstance<T>();
            init?.Invoke(a);
            AssetDatabase.CreateAsset(a, path);
            return a;
        }

        static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null) { Debug.LogError($"{target.GetType().Name}.{field} không tồn tại"); return; }
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetArray(Object target, string field, Object[] values)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null) { Debug.LogError($"{target.GetType().Name}.{field} không tồn tại"); return; }
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            EnsureFolder(path.Substring(0, slash));
            AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
        }

        static void AddToBuildSettings(string path)
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(s => s.path == path);
            scenes.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
