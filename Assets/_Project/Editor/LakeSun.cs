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
    /// Sun through the root roof: the main light shines almost straight down with real shadows, the roof (Lake Canopy ->
    /// Roof) casts them, so light only falls through the gaps in the roots; everything else is dark. Menu: Evolve ->
    /// Dress Lake: Dappled Sun. "Restore Lake Sun (undo)" puts everything back. Only for the Underroot_Lake scene.
    /// </summary>
    public static class LakeSun
    {
        const string Root = "Assets/_Project";
        const string ScenePath = Root + "/Scenes/Underroot_Lake.unity";
        const string BackupPath = Root + "/Settings/LakeSunBackup.json";
        const string CanopyName = "Lake Canopy (Dressed)";

        [System.Serializable]
        class Backup
        {
            public bool saved;
            public Vector3 sunEuler;
            public Color sunColor;
            public float sunIntensity;
            public int sunShadows;
            public float sunShadowStrength;
            public int ambientMode;
            public Color ambientLight;
            public float reflectionIntensity;
            public List<string> urpPaths = new List<string>();
            public List<float> urpDistance = new List<float>();
            public List<int> urpCascades = new List<int>();
            public List<int> urpResolution = new List<int>();
            public List<bool> urpSoft = new List<bool>();
            public List<string> roofPaths = new List<string>();
            public List<int> roofModes = new List<int>();
        }

        [MenuItem("Evolve/Dress Lake: Dappled Sun")]
        public static void Dress()
        {
            if (EditorSceneManager.GetActiveScene().path != ScenePath)
            {
                Debug.LogWarning("Mở scene Underroot_Lake trước (menu này chỉ đổi cảnh hồ, không đổi Prototype_01).");
                return;
            }
            var sun = FindSun();
            var canopy = GameObject.Find(CanopyName);
            if (sun == null) { Debug.LogWarning("Không có đèn Directional trong scene: bỏ qua."); return; }
            if (canopy == null) { Debug.LogWarning("Chưa có " + CanopyName + " (chạy Dress Lake: Root Canopy trước): không có mái để đổ bóng, bỏ qua."); return; }
            var roofRenderers = RoofRenderers(canopy);
            if (roofRenderers.Count == 0) { Debug.LogWarning("Không thấy renderer của mái (Roof) dưới " + CanopyName + ": bỏ qua."); return; }

            var backup = Load();
            if (!backup.saved) { backup = Capture(sun, canopy.transform, roofRenderers); Save(backup); }

            // the sun: almost straight down, warm, bright, soft real shadows
            sun.transform.rotation = Quaternion.Euler(DappledSunMath.Elevation, DappledSunMath.Yaw, 0f);
            sun.color = DappledSunMath.SunColor;
            sun.intensity = DappledSunMath.Intensity;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 1f;

            // the roof blocks it
            foreach (var r in roofRenderers) r.shadowCastingMode = ShadowCastingMode.On;

            // everything the roof covers is dark
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = DappledSunMath.AmbientColor;
            RenderSettings.reflectionIntensity = DappledSunMath.ReflectionIntensity;

            foreach (var urp in AllUrpAssets())
            {
                urp.shadowDistance = DappledSunMath.ShadowDistance;
                urp.shadowCascadeCount = DappledSunMath.ShadowCascades;
                urp.mainLightShadowmapResolution = DappledSunMath.ShadowResolution;
                urp.supportsSoftShadows = true;
                EditorUtility.SetDirty(urp);
            }

            if (!sun.TryGetComponent<DappledSun>(out _)) sun.gameObject.AddComponent<DappledSun>();
            if (!sun.TryGetComponent<ShadowToggle>(out var toggle)) toggle = sun.gameObject.AddComponent<ShadowToggle>();
            toggle.SetRoof(roofRenderers.ToArray());
            EditorUtility.SetDirty(toggle);

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"Mặt trời lọt kẽ rễ: {roofRenderers.Count} mảnh mái đổ bóng, tầm bóng {DappledSunMath.ShadowDistance:0} m. F9 bật tắt bóng của mái; Evolve → Restore Lake Sun (undo) để trả lại. Bấm Ctrl+S để lưu scene.");
        }

        [MenuItem("Evolve/Restore Lake Sun (undo)")]
        public static void Restore()
        {
            var b = Load();
            var sun = FindSun();
            if (b.saved)
            {
                if (sun != null)
                {
                    sun.transform.eulerAngles = b.sunEuler;
                    sun.color = b.sunColor;
                    sun.intensity = b.sunIntensity;
                    sun.shadows = (LightShadows)b.sunShadows;
                    sun.shadowStrength = b.sunShadowStrength;
                }
                RenderSettings.ambientMode = (AmbientMode)b.ambientMode;
                RenderSettings.ambientLight = b.ambientLight;
                RenderSettings.reflectionIntensity = b.reflectionIntensity;
                for (int i = 0; i < b.urpPaths.Count; i++)
                {
                    var urp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(b.urpPaths[i]);
                    if (urp == null) continue;
                    urp.shadowDistance = b.urpDistance[i];
                    urp.shadowCascadeCount = b.urpCascades[i];
                    urp.mainLightShadowmapResolution = b.urpResolution[i];
                    urp.supportsSoftShadows = b.urpSoft[i];
                    EditorUtility.SetDirty(urp);
                }
                var canopy = GameObject.Find(CanopyName);
                if (canopy != null)
                    foreach (var r in RoofRenderers(canopy))
                    {
                        int k = b.roofPaths.IndexOf(PathOf(r.transform, canopy.transform));
                        if (k >= 0) r.shadowCastingMode = (ShadowCastingMode)b.roofModes[k];
                    }
            }
            if (sun != null)
            {
                if (sun.TryGetComponent<DappledSun>(out var d)) Object.DestroyImmediate(d);
                if (sun.TryGetComponent<ShadowToggle>(out var t)) Object.DestroyImmediate(t);
            }
            AssetDatabase.DeleteAsset(BackupPath);
            AssetDatabase.SaveAssets();
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.IsValid()) EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("Đã trả mặt trời, ambient, bóng đổ như trước. Bấm Ctrl+S để lưu scene.");
        }

        static Light FindSun()
        {
            foreach (var l in Object.FindObjectsByType<Light>())
                if (l.type == LightType.Directional) return l;
            return null;
        }

        static List<Renderer> RoofRenderers(GameObject canopy)
        {
            var list = new List<Renderer>();
            var roof = canopy.transform.Find("Roof");
            if (roof == null) return list;
            foreach (var r in roof.GetComponentsInChildren<Renderer>(true))
                if (r is MeshRenderer) list.Add(r);
            return list;
        }

        static IEnumerable<UniversalRenderPipelineAsset> AllUrpAssets()
        {
            var seen = new HashSet<UniversalRenderPipelineAsset>();
            if (GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset d && seen.Add(d)) yield return d;
            for (int i = 0; i < QualitySettings.names.Length; i++)
                if (QualitySettings.GetRenderPipelineAssetAt(i) is UniversalRenderPipelineAsset q && seen.Add(q)) yield return q;
        }

        static string PathOf(Transform t, Transform root)
        {
            string p = t.name;
            for (var c = t.parent; c != null && c != root; c = c.parent) p = c.name + "/" + p;
            return p;
        }

        static Backup Capture(Light sun, Transform canopy, List<Renderer> roof)
        {
            var b = new Backup
            {
                saved = true,
                sunEuler = sun.transform.eulerAngles,
                sunColor = sun.color,
                sunIntensity = sun.intensity,
                sunShadows = (int)sun.shadows,
                sunShadowStrength = sun.shadowStrength,
                ambientMode = (int)RenderSettings.ambientMode,
                ambientLight = RenderSettings.ambientLight,
                reflectionIntensity = RenderSettings.reflectionIntensity
            };
            foreach (var urp in AllUrpAssets())
            {
                b.urpPaths.Add(AssetDatabase.GetAssetPath(urp));
                b.urpDistance.Add(urp.shadowDistance);
                b.urpCascades.Add(urp.shadowCascadeCount);
                b.urpResolution.Add(urp.mainLightShadowmapResolution);
                b.urpSoft.Add(urp.supportsSoftShadows);
            }
            foreach (var r in roof)
            {
                b.roofPaths.Add(PathOf(r.transform, canopy));
                b.roofModes.Add((int)r.shadowCastingMode);
            }
            return b;
        }

        static Backup Load()
        {
            if (!File.Exists(BackupPath)) return new Backup();
            var b = JsonUtility.FromJson<Backup>(File.ReadAllText(BackupPath));
            return b ?? new Backup();
        }

        static void Save(Backup b)
        {
            PrototypeSceneBuilder.EnsureFolder(Root + "/Settings");
            File.WriteAllText(BackupPath, JsonUtility.ToJson(b, true));
            AssetDatabase.ImportAsset(BackupPath);
        }
    }
}
