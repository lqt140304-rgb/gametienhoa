using System.IO;
using UnityEditor;
using UnityEngine;

namespace Evolve.EditorTools
{
    /// <summary>
    /// Models in Models/Wildlife/Meshy come with two loose textures (name_albedo/_mask.png) instead of embedded ones:
    /// this builds a URP/Lit material from them and remaps the model's material to it. Runs when such a model is imported,
    /// or from the menu Evolve → Fix Meshy Materials.
    /// </summary>
    public class MeshyMaterialSetup : AssetPostprocessor
    {
        const string Folder = "Assets/_Project/Models/Wildlife/Meshy";

        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            foreach (var path in imported)
                if (path.StartsWith(Folder) && path.EndsWith(".fbx"))
                {
                    string p = path;
                    EditorApplication.delayCall += () => Fix(p, false);
                }
        }

        [MenuItem("Evolve/Fix Meshy Materials")]
        static void FixAll()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { Folder }))
                Fix(AssetDatabase.GUIDToAssetPath(guid), true);
        }

        static void Fix(string fbxPath, bool force)
        {
            var importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (importer == null) return;
            if (!force && importer.GetExternalObjectMap().Count > 0) return;   // already done

            string stem = fbxPath.Substring(0, fbxPath.Length - 4);
            string albedoPath = stem + "_albedo.png", maskPath = stem + "_mask.png";
            if (!File.Exists(albedoPath)) { Debug.LogWarning("[Meshy] no albedo texture next to " + fbxPath); return; }
            SetupTexture(maskPath, TextureImporterType.Default, false);
            SetupTexture(albedoPath, TextureImporterType.Default, true);

            string matPath = stem + "_Mat.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, matPath);
            }
            mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(albedoPath));
            mat.SetColor("_BaseColor", Color.white);
            mat.SetTexture("_BumpMap", null); mat.DisableKeyword("_NORMALMAP");   // no normal map: it showed seam cracks
            var mask = AssetDatabase.LoadAssetAtPath<Texture2D>(maskPath);
            if (mask != null)
            {
                mat.SetTexture("_MetallicGlossMap", mask);
                mat.EnableKeyword("_METALLICSPECGLOSSMAP");
                mat.SetFloat("_Metallic", 1f); mat.SetFloat("_Smoothness", 1f);   // the map scales both
            }
            EditorUtility.SetDirty(mat);

            foreach (var o in AssetDatabase.LoadAllAssetRepresentationsAtPath(fbxPath))
                if (o is Material m)
                    importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), m.name), mat);
            AssetDatabase.SaveAssets();
            importer.SaveAndReimport();
            Debug.Log("[Meshy] material set up for " + fbxPath);
        }

        static void SetupTexture(string path, TextureImporterType type, bool srgb)
        {
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null || (ti.textureType == type && ti.sRGBTexture == srgb && !ti.mipmapEnabled)) return;
            ti.textureType = type;
            ti.sRGBTexture = srgb;
            ti.mipmapEnabled = false;   // mips blur colours across the atlas's many small islands, which shows as seams
            ti.SaveAndReimport();
        }
    }
}
