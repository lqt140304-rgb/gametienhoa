using System.Collections.Generic;
using Evolve.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Evolve.EditorTools
{
    /// <summary>
    /// Makes the lake look like a cavern: dark teal ambient light and fog, a post-processing volume (bloom, contrast,
    /// vignette, tonemapping), glow lights for the giant mushrooms, and the water shader with depth-based colour,
    /// soft edges and foam. Menu: Evolve -> Dress Lake: Light and Water Look. Rerunnable; "Restore Lake Look" undoes it.
    /// </summary>
    public static class LakeLook
    {
        const string Root = "Assets/_Project";
        const string VolumeName = "Lake Post Process (Dressed)";
        const string LightsName = "Lake Look Lights (Dressed)";
        const string ProfilePath = Root + "/Settings/LakeLook.asset";
        const string BackupKey = "Evolve.LakeLook.Backup";
        const string WaterShaderName = "Evolve/LakeWater";
        const string LitShaderName = "Universal Render Pipeline/Lit";

        [System.Serializable]
        class Backup
        {
            public bool depthTexture;
            public bool fog;
            public int fogMode;
            public float fogDensity;
            public Color fogColor, ambientSky, ambientEquator, ambientGround;
            public int ambientMode;
            public bool saved;
        }

        [MenuItem("Evolve/Dress Lake: Light and Water Look")]
        public static void Dress()
        {
            SaveBackupOnce();
            SetupUrp();
            SetupAtmosphere();
            SetupPostProcess();
            SetupWater();
            SetupGlowLights();
            AssetDatabase.SaveAssets();
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.IsValid()) EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("Đã đặt ánh sáng và nước (sương, ambient, hậu xử lý, đèn nấm, shader nước). Nếu có gì lạ: Evolve → Restore Lake Look (undo).");
        }

        [MenuItem("Evolve/Restore Lake Look (undo)")]
        public static void Restore()
        {
            var json = EditorPrefs.GetString(BackupKey, "");
            if (!string.IsNullOrEmpty(json))
            {
                var b = JsonUtility.FromJson<Backup>(json);
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
                    if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp)
                    {
                        urp.supportsCameraDepthTexture = b.depthTexture;
                        EditorUtility.SetDirty(urp);
                    }
                }
            }
            DestroyByName(VolumeName);
            DestroyByName(LightsName);
            var lit = Shader.Find(LitShaderName);
            foreach (var n in new[] { "Lake_WaterClean", "Lake_WaterDirty" })
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>($"{Root}/Materials/{n}.mat");
                if (mat != null && lit != null) { mat.shader = lit; EditorUtility.SetDirty(mat); }
            }
            EditorPrefs.DeleteKey(BackupKey);
            AssetDatabase.SaveAssets();
            Debug.Log("Đã trả ánh sáng và nước về như trước (chạy lại Dress Lake: Water nếu nước còn lạ).");
        }

        static void SaveBackupOnce()
        {
            if (!string.IsNullOrEmpty(EditorPrefs.GetString(BackupKey, ""))) return; // keep the very first state
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
                depthTexture = GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset u && u.supportsCameraDepthTexture
            };
            EditorPrefs.SetString(BackupKey, JsonUtility.ToJson(b));
        }

        static void SetupUrp()
        {
            if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp)
            {
                urp.supportsCameraDepthTexture = true; // the water shader reads the scene depth
                EditorUtility.SetDirty(urp);
            }
            else Debug.LogWarning("Không tìm thấy URP asset: nước sẽ không mờ dần theo độ sâu.");
        }

        static void SetupAtmosphere()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = WaterLook.AmbientSky;
            RenderSettings.ambientEquatorColor = WaterLook.AmbientEquator;
            RenderSettings.ambientGroundColor = WaterLook.AmbientGround;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = WaterLook.FogColor;
            RenderSettings.fogDensity = WaterLook.FogDensityFor(220f);
        }

        static void SetupPostProcess()
        {
            DestroyByName(VolumeName);
            PrototypeSceneBuilder.EnsureFolder(Root + "/Settings");
            AssetDatabase.DeleteAsset(ProfilePath);
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, ProfilePath);

            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(0.9f);
            bloom.intensity.Override(0.5f);
            bloom.scatter.Override(0.6f);
            var colour = profile.Add<ColorAdjustments>(true);
            colour.contrast.Override(8f);
            colour.saturation.Override(10f);
            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.25f);
            vignette.smoothness.Override(0.4f);
            var tone = profile.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.ACES);
            foreach (var c in profile.components) AssetDatabase.AddObjectToAsset(c, profile);
            EditorUtility.SetDirty(profile);

            var go = new GameObject(VolumeName);
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10f;
            volume.sharedProfile = profile;

            var cam = Camera.main;
            if (cam != null) cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            else Debug.LogWarning("Không có camera chính: bật 'Post Processing' trên camera bằng tay.");
        }

        static void SetupWater()
        {
            var shader = Shader.Find(WaterShaderName);
            if (shader == null) { Debug.LogWarning("Không tìm thấy shader " + WaterShaderName + ": giữ nước như cũ."); return; }
            Retint("Lake_WaterClean", shader, new Color(0.02f, 0.27f, 0.31f, 0.92f), 3.0f);
            Retint("Lake_WaterDirty", shader, new Color(0.22f, 0.26f, 0.07f, 0.95f), 1.5f);
        }

        static void Retint(string name, Shader shader, Color deep, float deepDistance)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>($"{Root}/Materials/{name}.mat");
            if (mat == null) { Debug.LogWarning("Chưa có material " + name + " (chạy Dress Lake: Water trước)."); return; }
            Color shallow = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : new Color(0.25f, 0.84f, 0.82f, 0.5f);
            var bump = mat.HasProperty("_BumpMap") ? mat.GetTexture("_BumpMap") : null;
            if (mat.shader != shader) mat.shader = shader;
            mat.SetColor("_ShallowColor", shallow);
            mat.SetColor("_DeepColor", deep);
            mat.SetFloat("_DeepDistance", deepDistance);
            if (bump != null) mat.SetTexture("_BumpMap", bump);
            mat.renderQueue = (int)RenderQueue.Transparent + 20;
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
                if (mat != null) c = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : mat.color;
                for (int i = 0; i < 8; i++)
                {
                    var go = new GameObject("Glow");
                    go.transform.SetParent(root, false);
                    go.transform.position = new Vector3(
                        Mathf.Lerp(b.min.x, b.max.x, (float)rnd.NextDouble()),
                        b.max.y * 0.9f,
                        Mathf.Lerp(b.min.z, b.max.z, (float)rnd.NextDouble()));
                    var l = go.AddComponent<Light>();
                    l.type = LightType.Point;
                    l.color = Color.Lerp(c, Color.white, 0.25f);
                    l.range = 6f + 3f * (float)rnd.NextDouble();
                    l.intensity = 1.2f + 0.8f * (float)rnd.NextDouble();
                    l.shadows = LightShadows.None;
                    made++;
                }
            }
            if (made == 0) Debug.Log("Không thấy nấm khổng lồ (Giant Caps) nên không đặt đèn nấm.");
        }

        static void DestroyByName(string name)
        {
            var go = GameObject.Find(name);
            if (go != null) Object.DestroyImmediate(go);
        }
    }
}
