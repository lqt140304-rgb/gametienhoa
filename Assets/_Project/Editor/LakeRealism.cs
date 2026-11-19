using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Evolve.EditorTools
{
    /// <summary>
    /// Takes the plastic look off the lake (Evolve -> Dress Lake: Realism, undo: Restore Lake Realism):
    /// (1) film-like post-processing on the existing look volume: ACES tonemapping, a little contrast, fine film grain;
    /// (2) screen-space ambient occlusion on every URP renderer (dark creases and contact shadows);
    /// (3) the lit materials lose their gloss: low smoothness, no specular highlights, no environment reflections
    ///     (water, crystals, lamps and anything that glows are left alone). The rock/bark shader got its own grain in the shader file.
    /// </summary>
    public static class LakeRealism
    {
        const string Root = "Assets/_Project";
        const string ProfilePath = Root + "/Settings/LakeLook.asset";
        const string BackupPath = Root + "/Settings/LakeRealismBackup.json";
        const string FeatureName = "Evolve SSAO";
        static readonly string[] Skip = { "Water", "Crystal", "Amber", "Gel", "Eye", "Glass", "Shaft", "Vision", "Glow", "Lamp", "Fire", "Spore" };

        [System.Serializable] class MatState { public string path; public float smoothness, specular, reflections; }
        [System.Serializable] class Backup { public List<MatState> materials = new List<MatState>(); }

        [MenuItem("Evolve/Bản đồ cũ (Underroot)/Dress Lake: Realism")]
        public static void Dress()
        {
            int post = Post();
            int ssao = Ssao(true);
            int mats = Materials();
            AssetDatabase.SaveAssets();
            Debug.Log($"Thực tế hơn: {post} hiệu ứng hậu xử lý, SSAO trên {ssao} renderer, {mats} vật liệu bớt bóng. Hoàn tác bằng Evolve → Restore Lake Realism.");
        }

        [MenuItem("Evolve/Bản đồ cũ (Underroot)/Restore Lake Realism (undo)")]
        public static void Restore()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            if (profile != null)
            {
                RemoveComponent<Tonemapping>(profile);
                RemoveComponent<FilmGrain>(profile);
                RemoveComponent<ColorAdjustments>(profile);
                EditorUtility.SetDirty(profile);
            }
            Ssao(false);
            if (File.Exists(BackupPath))
            {
                var b = JsonUtility.FromJson<Backup>(File.ReadAllText(BackupPath));
                foreach (var m in b.materials)
                {
                    var mat = AssetDatabase.LoadAssetAtPath<Material>(m.path);
                    if (mat == null) continue;
                    SetGloss(mat, m.smoothness, m.specular, m.reflections);
                    EditorUtility.SetDirty(mat);
                }
                AssetDatabase.DeleteAsset(BackupPath);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("Đã hoàn tác Dress Lake: Realism.");
        }

        // ------------------------------------------------------------------ post-processing

        static int Post()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            if (profile == null) { Debug.LogWarning("Chưa có " + ProfilePath + " (chạy Dress Lake: Light and Water Look trước): bỏ qua hậu xử lý."); return 0; }
            int n = 0;
            if (!profile.Has<Tonemapping>())
            {
                var t = profile.Add<Tonemapping>(true);
                t.mode.Override(TonemappingMode.ACES);
                AssetDatabase.AddObjectToAsset(t, profile);
                n++;
            }
            if (!profile.Has<FilmGrain>())
            {
                var g = profile.Add<FilmGrain>(true);
                g.type.Override(FilmGrainLookup.Thin1);
                g.intensity.Override(0.22f);
                g.response.Override(0.8f);
                AssetDatabase.AddObjectToAsset(g, profile);
                n++;
            }
            if (!profile.Has<ColorAdjustments>())
            {
                var c = profile.Add<ColorAdjustments>(true);   // the vision volume above owns saturation; this one only adds contrast and a touch of exposure
                c.contrast.Override(12f);
                c.postExposure.Override(0.2f);
                AssetDatabase.AddObjectToAsset(c, profile);
                n++;
            }
            EditorUtility.SetDirty(profile);
            return n;
        }

        static void RemoveComponent<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (!profile.TryGet<T>(out var c)) return;
            profile.Remove<T>();
            Object.DestroyImmediate(c, true);
        }

        // ------------------------------------------------------------------ ambient occlusion

        static int Ssao(bool add)
        {
            int n = 0;
            var seen = new HashSet<ScriptableRendererData>();
            var assets = new List<UniversalRenderPipelineAsset>();
            if (GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset d) assets.Add(d);
            for (int i = 0; i < QualitySettings.names.Length; i++)
                if (QualitySettings.GetRenderPipelineAssetAt(i) is UniversalRenderPipelineAsset q && !assets.Contains(q)) assets.Add(q);
            foreach (var urp in assets)
            {
                var list = new SerializedObject(urp).FindProperty("m_RendererDataList");
                if (list == null) continue;
                for (int i = 0; i < list.arraySize; i++)
                {
                    var rd = list.GetArrayElementAtIndex(i).objectReferenceValue as ScriptableRendererData;
                    if (rd == null || !seen.Add(rd)) continue;
                    try { if (add ? AddSsao(rd) : RemoveSsao(rd)) n++; }
                    catch (System.Exception e) { Debug.LogWarning("SSAO: không chỉnh được " + rd.name + ": " + e.Message); }
                }
            }
            return n;
        }

        static bool AddSsao(ScriptableRendererData rd)
        {
            foreach (var f in rd.rendererFeatures) if (f is ScreenSpaceAmbientOcclusion) return true;   // already there (ours or the user's)
            var feature = ScriptableObject.CreateInstance<ScreenSpaceAmbientOcclusion>();
            feature.name = FeatureName;
            AssetDatabase.AddObjectToAsset(feature, rd);
            var so = new SerializedObject(rd);
            var feats = so.FindProperty("m_RendererFeatures");
            var map = so.FindProperty("m_RendererFeatureMap");
            feats.arraySize++;
            feats.GetArrayElementAtIndex(feats.arraySize - 1).objectReferenceValue = feature;
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out _, out long id);
            map.arraySize++;
            map.GetArrayElementAtIndex(map.arraySize - 1).longValue = id;
            so.ApplyModifiedPropertiesWithoutUndo();
            var fso = new SerializedObject(feature);
            var intensity = fso.FindProperty("m_Settings.Intensity");
            if (intensity != null) { intensity.floatValue = 1.4f; fso.ApplyModifiedPropertiesWithoutUndo(); }
            EditorUtility.SetDirty(rd);
            return true;
        }

        static bool RemoveSsao(ScriptableRendererData rd)
        {
            var so = new SerializedObject(rd);
            var feats = so.FindProperty("m_RendererFeatures");
            var map = so.FindProperty("m_RendererFeatureMap");
            bool removed = false;
            for (int i = feats.arraySize - 1; i >= 0; i--)
            {
                var f = feats.GetArrayElementAtIndex(i).objectReferenceValue;
                if (f == null || f.name != FeatureName) continue;
                feats.GetArrayElementAtIndex(i).objectReferenceValue = null;
                feats.DeleteArrayElementAtIndex(i);
                if (i < map.arraySize) map.DeleteArrayElementAtIndex(i);
                Object.DestroyImmediate(f, true);
                removed = true;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(rd);
            return removed;
        }

        // ------------------------------------------------------------------ materials

        static int Materials()
        {
            var backup = File.Exists(BackupPath) ? JsonUtility.FromJson<Backup>(File.ReadAllText(BackupPath)) : new Backup();
            var known = new HashSet<string>();
            foreach (var m in backup.materials) known.Add(m.path);
            int n = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { Root + "/Materials" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null || mat.shader == null || mat.shader.name != "Universal Render Pipeline/Lit") continue;
                bool skip = false;
                foreach (var s in Skip) if (mat.name.Contains(s)) { skip = true; break; }
                if (skip) continue;
                if (mat.IsKeywordEnabled("_EMISSION") && mat.HasProperty("_EmissionColor") && mat.GetColor("_EmissionColor").maxColorComponent > 0.05f) continue;
                if (known.Add(path))
                    backup.materials.Add(new MatState
                    {
                        path = path,
                        smoothness = mat.HasProperty("_Smoothness") ? mat.GetFloat("_Smoothness") : 0.5f,
                        specular = mat.HasProperty("_SpecularHighlights") ? mat.GetFloat("_SpecularHighlights") : 1f,
                        reflections = mat.HasProperty("_EnvironmentReflections") ? mat.GetFloat("_EnvironmentReflections") : 1f
                    });
                SetGloss(mat, Mathf.Min(mat.HasProperty("_Smoothness") ? mat.GetFloat("_Smoothness") : 0.1f, 0.1f), 0f, 0f);
                EditorUtility.SetDirty(mat);
                n++;
            }
            PrototypeSceneBuilder.EnsureFolder(Root + "/Settings");
            File.WriteAllText(BackupPath, JsonUtility.ToJson(backup, true));
            AssetDatabase.ImportAsset(BackupPath);
            return n;
        }

        static void SetGloss(Material mat, float smoothness, float specular, float reflections)
        {
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);
            if (mat.HasProperty("_SpecularHighlights")) mat.SetFloat("_SpecularHighlights", specular);
            if (mat.HasProperty("_EnvironmentReflections")) mat.SetFloat("_EnvironmentReflections", reflections);
            if (specular < 0.5f) mat.EnableKeyword("_SPECULARHIGHLIGHTS_OFF"); else mat.DisableKeyword("_SPECULARHIGHLIGHTS_OFF");
            if (reflections < 0.5f) mat.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF"); else mat.DisableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
        }
    }
}
