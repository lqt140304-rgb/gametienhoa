using System.IO;
using Evolve.World;
using UnityEditor;
using UnityEngine;

namespace Evolve.EditorTools
{
    /// <summary>
    /// Turns a downloaded scan (Assets/_Project/ThirdParty/Scanned/{id}: the model and its textures/) into a ready prefab {id}.prefab with one URP Lit
    /// material in the semi-real look: the colour map graded by <see cref="SemiRealGrade"/> (and, for foliage, the alpha mask packed into it), the
    /// normal map, matte smoothness, AO when present; foliage is alpha-clipped and two-sided.
    /// </summary>
    public static class ScannedMaterials
    {
        const string Lit = "Universal Render Pipeline/Lit";

        public static GameObject Build(FloraAsset a)
        {
            string folder = FloraDownload.FolderFor(a);
            string model = FindFile(folder, ".fbx") ?? FindFile(folder, ".gltf") ?? FindFile(folder, ".glb");
            if (model == null) { Debug.LogWarning($"[Scanned] {a.id}: chưa có model trong {folder}."); return null; }
            string tex = folder + "/textures";
            string diff = FindFile(tex, "_diff_"), nor = FindFile(tex, "_nor_gl_"), alpha = FindFile(tex, "_alpha_"), ao = FindFile(tex, "_ao_"), arm = FindFile(tex, "_arm_");

            if (nor != null && AssetImporter.GetAtPath(nor) is TextureImporter ni && ni.textureType != TextureImporterType.NormalMap)
            {
                ni.textureType = TextureImporterType.NormalMap;
                ni.SaveAndReimport();
            }
            string graded = diff != null ? GradedBaseMap(diff, alpha, folder + "/" + a.id + "_base_semireal.png") : null;

            bool foliage = a.IsFoliage || alpha != null;
            string matPath = folder + "/" + a.id + "_semireal.mat";
            var shader = foliage ? Shader.Find("Evolve/ScannedFoliage") : Shader.Find(Lit);
            if (shader == null) shader = Shader.Find(Lit);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null) { mat = new Material(shader) { name = a.id + "_semireal" }; AssetDatabase.CreateAsset(mat, matPath); }
            mat.shader = shader;
            if (graded != null) mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(graded));
            if (nor != null) { mat.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(nor)); mat.EnableKeyword("_NORMALMAP"); }
            if (foliage)
            {
                // our two-sided leaf shader: back faces lit like the front (URP Lit with Cull Off would leave them black)
                if (arm != null) mat.SetTexture("_ArmMap", AssetDatabase.LoadAssetAtPath<Texture2D>(arm));
                else if (ao != null) mat.SetTexture("_ArmMap", AssetDatabase.LoadAssetAtPath<Texture2D>(ao));
                mat.SetFloat("_Cutoff", 0.45f);
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
            }
            else
            {
                // solid scans (rock, wood): URP Lit, matte; AO only from a plain AO map (URP reads it from G, the packed ARM keeps AO in R)
                if (ao != null) { mat.SetTexture("_OcclusionMap", AssetDatabase.LoadAssetAtPath<Texture2D>(ao)); mat.EnableKeyword("_OCCLUSIONMAP"); }
                mat.SetFloat("_Smoothness", 1f - SemiRealGrade.Roughness(0.75f));
                mat.SetFloat("_Metallic", 0f);
                mat.SetFloat("_SpecularHighlights", 0f); mat.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
                mat.SetFloat("_AlphaClip", 0f); mat.DisableKeyword("_ALPHATEST_ON");
                mat.SetFloat("_Cull", 2f);
                mat.renderQueue = -1;
            }
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);

            var src = AssetDatabase.LoadAssetAtPath<GameObject>(model);
            if (src == null) { Debug.LogWarning($"[Scanned] {a.id}: không nạp được {model}."); return null; }
            var go = (GameObject)Object.Instantiate(src);
            go.name = a.id;
            var renderers = go.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                var mats = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++) mats[i] = mat;
                r.sharedMaterials = mats;
                r.shadowCastingMode = foliage ? UnityEngine.Rendering.ShadowCastingMode.Off : UnityEngine.Rendering.ShadowCastingMode.On;
            }
            if (go.GetComponent<LODGroup>() == null && renderers.Length > 0)
            {
                var lod = go.AddComponent<LODGroup>();
                lod.SetLODs(new[] { new LOD(foliage ? 0.03f : 0.01f, renderers) });   // culled when tiny on screen
                lod.RecalculateBounds();
            }
            // occlusion and reflection only: static batching would undo GPU instancing and inflate memory with hundreds of copies
            foreach (var t in go.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.OccludeeStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.ReflectionProbeStatic);
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, folder + "/" + a.id + ".prefab");
            Object.DestroyImmediate(go);
            return prefab;
        }

        /// <summary>The colour map graded to the semi-real look, with the foliage alpha mask in its alpha channel; written once as a PNG next to the model.</summary>
        static string GradedBaseMap(string diffPath, string alphaPath, string outPath)
        {
            if (File.Exists(Abs(outPath))) return outPath;
            var diff = Load(diffPath);
            if (diff == null) return null;
            var mask = alphaPath != null ? Load(alphaPath) : null;
            var px = diff.GetPixels();
            Color[] mp = null;
            if (mask != null)
            {
                if (mask.width != diff.width || mask.height != diff.height) mask = Resized(mask, diff.width, diff.height);
                mp = mask.GetPixels();
            }
            for (int i = 0; i < px.Length; i++)
            {
                var c = SemiRealGrade.Grade(px[i], SemiRealGrade.CaveTint);
                c.a = mp != null ? mp[i].r : 1f;
                px[i] = c;
            }
            var outTex = new Texture2D(diff.width, diff.height, TextureFormat.RGBA32, true);
            outTex.SetPixels(px);
            outTex.Apply();
            File.WriteAllBytes(Abs(outPath), outTex.EncodeToPNG());
            Object.DestroyImmediate(outTex); Object.DestroyImmediate(diff); if (mask != null) Object.DestroyImmediate(mask);
            AssetDatabase.ImportAsset(outPath);
            if (AssetImporter.GetAtPath(outPath) is TextureImporter imp)
            {
                imp.alphaIsTransparency = mp != null;
                imp.mipMapsPreserveCoverage = mp != null;   // leaves keep their shape in the distance
                imp.alphaTestReferenceValue = 0.45f;
                imp.SaveAndReimport();
            }
            return outPath;
        }

        static Texture2D Load(string path)
        {
            var t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            return t.LoadImage(File.ReadAllBytes(Abs(path))) ? t : null;   // jpg and png only (the colour and alpha maps are)
        }

        static Texture2D Resized(Texture2D src, int w, int h)
        {
            var rt = RenderTexture.GetTemporary(w, h);
            Graphics.Blit(src, rt);
            var prev = RenderTexture.active; RenderTexture.active = rt;
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.ReadPixels(new Rect(0, 0, w, h), 0, 0); t.Apply();
            RenderTexture.active = prev; RenderTexture.ReleaseTemporary(rt);
            Object.DestroyImmediate(src);
            return t;
        }

        static string FindFile(string folder, string contains)
        {
            if (!Directory.Exists(Abs(folder))) return null;
            foreach (var f in Directory.GetFiles(Abs(folder)))
            {
                string n = Path.GetFileName(f);
                if (n.EndsWith(".meta")) continue;
                if (n.Contains(contains)) return folder + "/" + n;
            }
            return null;
        }

        static string Abs(string assetPath) => Path.Combine(Path.GetDirectoryName(Application.dataPath), assetPath);
    }
}
