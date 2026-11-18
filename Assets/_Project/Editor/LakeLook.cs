using System.Collections.Generic;
using System.IO;
using Evolve.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Evolve.EditorTools
{
    /// <summary>
    /// Makes the lake look like a cavern: a post-processing volume (bloom, light vignette; saturation, fog, ambient and
    /// daylight stay with MonochromeVision), glow lights for the giant mushrooms, and the water shader with depth-based colour,
    /// soft edges and foam. Menu: Evolve -> Dress Lake: Light and Water Look. Rerunnable; "Restore Lake Look" undoes it.
    /// </summary>
    public static class LakeLook
    {
        const string Root = "Assets/_Project";
        const string VolumeName = "Lake Post Process (Dressed)";
        const string LightsName = "Lake Look Lights (Dressed)";
        const string ProfilePath = Root + "/Settings/LakeLook.asset";
        const string BackupPath = Root + "/Settings/LakeLookBackup.json";
        const string LakeScenePath = Root + "/Scenes/Underroot_Lake.unity";
        const int MaxGlowLights = 24;
        const int MaxZoneLights = 6;
        const string WaterShaderName = "Evolve/LakeWater";
        const string LitShaderName = "Universal Render Pipeline/Lit";

        [System.Serializable]
        class Backup
        {
            public bool saved;
            public bool fog;
            public int fogMode;
            public float fogDensity;
            public Color fogColor, ambientSky, ambientEquator, ambientGround;
            public int ambientMode;
            public bool postProcessing;
            public string[] urpAssetPaths = new string[0];
            public bool[] urpDepthTexture = new bool[0];
        }

        [MenuItem("Evolve/Dress Lake: Light and Water Look")]
        public static void Dress()
        {
            if (EditorSceneManager.GetActiveScene().path != LakeScenePath)
            {
                Debug.LogWarning("Mở scene Underroot_Lake trước (menu này chỉ đổi cảnh hồ, không đổi Prototype_01).");
                return;
            }
            var backup = LoadBackup();
            if (!backup.saved) SaveBackup();
            SetupUrp();
            SetupPostProcess();
            SetupWater();
            SetupGlowLights();
            AssetDatabase.SaveAssets();
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.IsValid()) EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("Đã đặt ánh sáng và nước (hậu xử lý nhẹ, đèn nấm, shader nước; sương, ambient và màu theo mắt do MonochromeVision lo). Nếu có gì lạ: Evolve → Restore Lake Look (undo).");
        }

        [MenuItem("Evolve/Restore Lake Look (undo)")]
        public static void Restore()
        {
            var b = LoadBackup();
            if (b.saved)
            {
                RenderSettings.fog = b.fog;
                RenderSettings.fogMode = (FogMode)b.fogMode;
                RenderSettings.fogDensity = b.fogDensity;
                RenderSettings.fogColor = b.fogColor;
                RenderSettings.ambientMode = (AmbientMode)b.ambientMode;
                RenderSettings.ambientSkyColor = b.ambientSky;
                RenderSettings.ambientEquatorColor = b.ambientEquator;
                RenderSettings.ambientGroundColor = b.ambientGround;
                for (int i = 0; i < b.urpAssetPaths.Length; i++)
                {
                    var urp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(b.urpAssetPaths[i]);
                    if (urp == null) continue;
                    urp.supportsCameraDepthTexture = b.urpDepthTexture[i];
                    EditorUtility.SetDirty(urp);
                }
                var cam = Camera.main;
                if (cam != null) cam.GetUniversalAdditionalCameraData().renderPostProcessing = b.postProcessing;
            }
            DestroyByName(VolumeName);
            DestroyByName(LightsName);
            AssetDatabase.DeleteAsset(ProfilePath);
            var lit = Shader.Find(LitShaderName);
            foreach (var n in new[] { "Lake_WaterClean", "Lake_WaterDirty" })
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>($"{Root}/Materials/{n}.mat");
                if (mat == null || lit == null) continue;
                Color shallow = mat.HasProperty("_ShallowColor") ? mat.GetColor("_ShallowColor") : Color.white;
                var bump = mat.HasProperty("_BumpMap") ? mat.GetTexture("_BumpMap") : null;
                mat.shader = lit;
                mat.SetColor("_BaseColor", shallow);
                if (bump != null) mat.SetTexture("_BumpMap", bump);
                mat.renderQueue = (int)RenderQueue.Transparent + 20;
                EditorUtility.SetDirty(mat);
            }
            AssetDatabase.DeleteAsset(BackupPath);
            AssetDatabase.SaveAssets();
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.IsValid()) EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("Đã trả ánh sáng và nước về như trước (chạy lại Dress Lake: Water nếu nước còn lạ). Bấm Ctrl+S để lưu scene.");
        }

        static Backup LoadBackup()
        {
            if (!File.Exists(BackupPath)) return new Backup();
            var b = JsonUtility.FromJson<Backup>(File.ReadAllText(BackupPath));
            return b ?? new Backup();
        }

        static IEnumerable<UniversalRenderPipelineAsset> AllUrpAssets()
        {
            var seen = new HashSet<UniversalRenderPipelineAsset>();
            if (GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset d && seen.Add(d)) yield return d;
            for (int i = 0; i < QualitySettings.names.Length; i++)
                if (QualitySettings.GetRenderPipelineAssetAt(i) is UniversalRenderPipelineAsset q && seen.Add(q)) yield return q;
        }

        static void SaveBackup()
        {
            var paths = new List<string>();
            var flags = new List<bool>();
            foreach (var urp in AllUrpAssets())
            {
                paths.Add(AssetDatabase.GetAssetPath(urp));
                flags.Add(urp.supportsCameraDepthTexture);
            }
            var cam = Camera.main;
            var b = new Backup
            {
                saved = true,
                fog = RenderSettings.fog,
                fogMode = (int)RenderSettings.fogMode,
                fogDensity = RenderSettings.fogDensity,
                fogColor = RenderSettings.fogColor,
                ambientMode = (int)RenderSettings.ambientMode,
                ambientSky = RenderSettings.ambientSkyColor,
                ambientEquator = RenderSettings.ambientEquatorColor,
                ambientGround = RenderSettings.ambientGroundColor,
                postProcessing = cam != null && cam.GetUniversalAdditionalCameraData().renderPostProcessing,
                urpAssetPaths = paths.ToArray(),
                urpDepthTexture = flags.ToArray()
            };
            PrototypeSceneBuilder.EnsureFolder(Root + "/Settings");
            File.WriteAllText(BackupPath, JsonUtility.ToJson(b, true));
            AssetDatabase.ImportAsset(BackupPath);
        }

        static void SetupUrp()
        {
            int n = 0;
            foreach (var urp in AllUrpAssets())
            {
                urp.supportsCameraDepthTexture = true; // the water shader reads the scene depth
                EditorUtility.SetDirty(urp);
                n++;
            }
            if (n == 0) Debug.LogWarning("Không tìm thấy URP asset: nước sẽ không mờ dần theo độ sâu.");
        }

        static void SetupPostProcess()
        {
            DestroyByName(VolumeName);
            PrototypeSceneBuilder.EnsureFolder(Root + "/Settings");
            AssetDatabase.DeleteAsset(ProfilePath);
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, ProfilePath);

            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(WaterLook.BloomThreshold);
            bloom.intensity.Override(WaterLook.BloomIntensity);
            bloom.scatter.Override(WaterLook.BloomScatter);
            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(WaterLook.VignetteIntensity);
            vignette.smoothness.Override(WaterLook.VignetteSmoothness);
            foreach (var c in profile.components) AssetDatabase.AddObjectToAsset(c, profile);
            EditorUtility.SetDirty(profile);

            var go = new GameObject(VolumeName);
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = -10f; // below the vision volume: MonochromeVision owns saturation, fog, ambient and daylight
            volume.sharedProfile = profile;

            var cam = Camera.main;
            if (cam != null) cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            else Debug.LogWarning("Không có camera chính: bật 'Post Processing' trên camera bằng tay.");
        }

        static void SetupWater()
        {
            var shader = Shader.Find(WaterShaderName);
            if (shader == null) { Debug.LogWarning("Không tìm thấy shader " + WaterShaderName + ": giữ nước như cũ."); return; }
            Retint("Lake_WaterClean", shader, new Color(0.02f, 0.27f, 0.31f, 0.70f), 3.0f);
            Retint("Lake_WaterDirty", shader, new Color(0.22f, 0.26f, 0.07f, 0.80f), 1.5f);
        }

        static void Retint(string name, Shader shader, Color deep, float deepDistance)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>($"{Root}/Materials/{name}.mat");
            if (mat == null) { Debug.LogWarning("Chưa có material " + name + " (chạy Dress Lake: Water trước)."); return; }
            Color shallow = mat.shader == shader && mat.HasProperty("_ShallowColor") ? mat.GetColor("_ShallowColor")
                : mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : new Color(0.25f, 0.84f, 0.82f, 0.5f);
            var bump = mat.HasProperty("_BumpMap") ? mat.GetTexture("_BumpMap") : null;
            if (mat.shader != shader) mat.shader = shader;
            mat.SetColor("_ShallowColor", shallow);
            mat.SetColor("_DeepColor", deep);
            mat.SetFloat("_DeepDistance", deepDistance);
            if (bump != null) mat.SetTexture("_BumpMap", bump);
            // drawn just BEFORE the slime's gel (queue Transparent): a swimming slime is never washed out by the water over it
            mat.renderQueue = (int)RenderQueue.Transparent - 2;
            EditorUtility.SetDirty(mat);
        }

        static void SetupGlowLights()
        {
            DestroyByName(LightsName);
            var root = new GameObject(LightsName).transform;
            var rnd = new System.Random(4401);
            int made = 0;
            foreach (var r in Object.FindObjectsByType<MeshRenderer>())
            {
                if (!r.name.StartsWith("Giant Caps")) continue;
                var b = r.bounds;
                Color c = Color.white;
                var mat = r.sharedMaterial;
                if (mat != null && mat.HasProperty("_BaseColor")) c = mat.GetColor("_BaseColor");
                else if (mat != null && mat.HasProperty("_Color")) c = mat.color;
                else c = new Color(0.95f, 0.85f, 0.6f); // vertex-colour props (Blender mushrooms) have no colour property: warm cap glow
                for (int i = 0; i < 4 && made < MaxGlowLights; i++)
                {
                    var go = new GameObject("Glow");
                    go.transform.SetParent(root, false);
                    go.transform.position = new Vector3(
                        Mathf.Lerp(b.min.x, b.max.x, (float)rnd.NextDouble()),
                        Mathf.Lerp(b.min.y, b.max.y, 0.85f),
                        Mathf.Lerp(b.min.z, b.max.z, (float)rnd.NextDouble()));
                    var l = go.AddComponent<Light>();
                    l.type = LightType.Point;
                    l.color = Color.Lerp(c, Color.white, 0.25f);
                    l.range = 6f + 3f * (float)rnd.NextDouble();
                    l.intensity = 1.2f + 0.8f * (float)rnd.NextDouble();
                    l.shadows = LightShadows.None;
                    go.AddComponent<LightByVision>(); // dark without eyes, far ones off
                    made++;
                }
            }
            if (made == 0) Debug.Log("Không thấy nấm khổng lồ (Giant Caps) nên không đặt đèn nấm.");

            // the living glow of each zone: a few soft coloured lights next to its glowing plants
            var zoneColours = new[]
            {
                new Color(0.30f, 0.90f, 0.85f),   // 01 teal pods
                new Color(1.00f, 0.80f, 0.50f),   // 02 old warm dust (rare)
                new Color(1.00f, 0.65f, 0.25f),   // 03 amber
                new Color(0.70f, 0.40f, 1.00f),   // 04 violet spores
                new Color(0.80f, 0.95f, 1.00f),   // 05 cold white
                new Color(1.00f, 0.45f, 0.30f)    // 06 weak red embers
            };
            var perZone = new int[ZoneLayout.Zones.Length];
            var plants = Object.FindObjectsByType<Nutrient>();
            int step = 0;
            foreach (var plant in plants)
            {
                step++;
                if (step % 3 != 0) continue; // every third plant: a few, spread out
                Vector3 pos = plant.transform.position;
                int zone = -1;
                float best = 0.5f;
                for (int i = 0; i < ZoneLayout.Zones.Length; i++)
                {
                    float inf = ZoneLayout.Influence(i, pos.x, pos.z);
                    if (inf > best) { best = inf; zone = i; }
                }
                if (zone < 0 || perZone[zone] >= MaxZoneLights) continue;
                perZone[zone]++;
                var go = new GameObject("Zone Glow " + (zone + 1));
                go.transform.SetParent(root, false);
                go.transform.position = pos + Vector3.up * 0.7f;
                var lamp = go.AddComponent<Light>();
                lamp.type = LightType.Point;
                lamp.color = zoneColours[zone];
                lamp.range = 6f + 3f * (float)rnd.NextDouble();
                lamp.intensity = 0.8f + 0.6f * (float)rnd.NextDouble();
                lamp.shadows = LightShadows.None;
                go.AddComponent<LightByVision>();
            }

            // the daylight at the hunter's way in (north): warm, seen from far, brightest on the side of area 06
            var exit = new GameObject("Daylight at the way out");
            exit.transform.SetParent(root, false);
            Vector2 gate = ZoneLayout.NorthEntry;
            exit.transform.position = new Vector3(gate.x, LakeGround.GroundY(gate.x, gate.y) + 6f, gate.y - 6f);
            var day = exit.AddComponent<Light>();
            day.type = LightType.Point;
            day.color = new Color(1f, 0.85f, 0.6f);
            day.range = 40f;
            day.intensity = 2f;
            day.shadows = LightShadows.None;
            exit.AddComponent<LightByVision>().AlwaysOnWhenSighted = true;
        }

        static void DestroyByName(string name)
        {
            var go = GameObject.Find(name);
            if (go != null) Object.DestroyImmediate(go);
        }
    }
}
