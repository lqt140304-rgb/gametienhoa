using System.Collections.Generic;
using Evolve.World;
using UnityEditor;
using UnityEngine;

namespace Evolve.EditorTools
{
    /// <summary>
    /// FBX props from Blender (Assets/_Project/Models/Underroot): the child named "&lt;name&gt;_COL" becomes a MeshCollider on the
    /// root (its renderer is removed), children "&lt;name&gt;_LOD1/_LOD2" become LODs, and everything is static.
    /// Scale is kept at 1 (the files are in metres).
    /// </summary>
    public class PropImportPostprocessor : AssetPostprocessor
    {
        const string Folder = "Assets/_Project/Models/Underroot/";

        void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(Folder)) return;
            var importer = (ModelImporter)assetImporter;
            importer.globalScale = 1f;
            importer.useFileScale = false;
            importer.importCameras = false;
            importer.importLights = false;
        }

        const string RockMaterial = "Assets/_Project/Materials/Rock_VC.mat";

        // Props come with painted vertex colours (stone and moss); URP Lit ignores them, so use the vertex-colour shader.
        Material OnAssignMaterialModel(Material material, Renderer renderer)
        {
            if (!assetPath.StartsWith(Folder)) return null;
            return AssetDatabase.LoadAssetAtPath<Material>(RockMaterial);
        }

        void OnPostprocessModel(GameObject root)
        {
            if (!assetPath.StartsWith(Folder)) return;
            var colliders = new List<Transform>();
            var lods = new SortedDictionary<int, List<Renderer>>();
            Renderer main = null;
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                string n = r.gameObject.name;
                if (PropNaming.IsCollider(n)) { colliders.Add(r.transform); continue; }
                if (PropNaming.IsLod(n, out int level))
                {
                    if (!lods.ContainsKey(level)) lods[level] = new List<Renderer>();
                    lods[level].Add(r);
                    continue;
                }
                if (main == null) main = r;
            }
            foreach (var c in colliders)
            {
                var mesh = c.GetComponent<MeshFilter>()?.sharedMesh;
                if (mesh != null)
                {
                    var mc = root.AddComponent<MeshCollider>();
                    mc.sharedMesh = mesh;
                    mc.convex = false;
                }
                Object.DestroyImmediate(c.GetComponent<Renderer>());
            }
            if (main != null && lods.Count > 0)
            {
                var group = root.AddComponent<LODGroup>();
                // ribs are long thin pieces seen from far away: the detailed copy only up close, the plain one down to a speck
                bool rib = root.name.StartsWith("Rib_");
                var levels = new List<LOD> { new LOD(rib ? 0.10f : 0.5f, new[] { main }) };
                float h = rib ? 0.006f : 0.25f;
                foreach (var kv in lods) { levels.Add(new LOD(h, kv.Value.ToArray())); h *= 0.4f; }
                group.SetLODs(levels.ToArray());
            }
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(t.gameObject, (StaticEditorFlags)(-1));
        }
    }
}
