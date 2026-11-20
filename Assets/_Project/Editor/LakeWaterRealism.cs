using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Evolve.EditorTools
{
    /// <summary>
    /// Evolve -> Dress Lake: Water Realism. Makes the lake read as real water: turns on URP's "Opaque Texture" (the water shader refracts the bed through it),
    /// and sets the new realism parameters on every lake water material (light absorbed per metre, refraction, reflection tint, caustics, glints).
    /// Clean water is clear and blue-green; the dirty puddle is murky and brown-green. Run after Dress Lake: Light and Water Look; rerunnable.
    /// </summary>
    public static class LakeWaterRealism
    {
        const string Root = "Assets/_Project";
        const string ShaderName = "Evolve/LakeWater";

        [MenuItem("Evolve/Bản đồ cũ (Underroot)/Dress Lake: Water Realism")]
        public static void Dress()
        {
            int urp = EnableOpaqueTexture();
            var shader = Shader.Find(ShaderName);
            if (shader == null) { Debug.LogWarning("Không tìm thấy shader " + ShaderName + ": bỏ qua nước thật hơn."); return; }
            int n = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { Root + "/Materials" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null || mat.shader != shader) continue;
                bool dirty = mat.name.Contains("Dirty");
                mat.SetColor("_Absorb", dirty ? new Color(0.30f, 0.34f, 0.60f) : new Color(0.60f, 0.16f, 0.10f));
                mat.SetFloat("_Refract", dirty ? 0.025f : 0.07f);
                mat.SetColor("_ReflectTint", dirty ? new Color(0.09f, 0.11f, 0.05f) : new Color(0.10f, 0.17f, 0.16f));
                mat.SetFloat("_CausticStrength", dirty ? 0.15f : 0.55f);
                mat.SetFloat("_GlintStrength", 1f);
                EditorUtility.SetDirty(mat);
                n++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"Nước thật hơn: bật Opaque Texture trên {urp} URP asset, {n} vật liệu nước có khúc xạ, hấp thụ ánh sáng, phản chiếu Fresnel, vân sáng đáy nước, ánh lấp lánh theo đèn. Nếu nước đen thì kiểm tra Opaque Texture trong URP asset.");
        }

        static int EnableOpaqueTexture()
        {
            var seen = new HashSet<UniversalRenderPipelineAsset>();
            var all = new List<UniversalRenderPipelineAsset>();
            if (GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset d && seen.Add(d)) all.Add(d);
            for (int i = 0; i < QualitySettings.names.Length; i++)
                if (QualitySettings.GetRenderPipelineAssetAt(i) is UniversalRenderPipelineAsset q && seen.Add(q)) all.Add(q);
            int n = 0;
            foreach (var urp in all)
            {
                var so = new SerializedObject(urp);
                var opaque = so.FindProperty("m_RequireOpaqueTexture");
                var depth = so.FindProperty("m_RequireDepthTexture");
                if (opaque == null) { Debug.LogWarning("Không tìm thấy m_RequireOpaqueTexture trên " + urp.name + ": bật Opaque Texture bằng tay."); continue; }
                opaque.boolValue = true;
                if (depth != null) depth.boolValue = true;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(urp);
                n++;
            }
            return n;
        }
    }
}
