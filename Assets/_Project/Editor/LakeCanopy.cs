using System.Collections.Generic;
using Evolve.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Evolve.EditorTools
{
    /// <summary>
    /// Evolve → Dress Lake: Root Canopy. The cave's roof becomes a woven ceiling of roots (spec 2026-11-17-lake-map-500m
    /// design §7): thick roots sagging between knots with a mat of thin roots between them, about 25 m up (lower where the
    /// dome comes down), with gaps where the light shafts fall (ZoneLayout.LightShafts). All round the rim, three rows of
    /// great roots climb the cliff from the ground to the roof. Built per 64 m square; the roof has two levels of detail
    /// and is never hidden (it is the view). Run again to rebuild.
    /// </summary>
    public static class LakeCanopy
    {
        const string Root = "Assets/_Project";
        const string DressedName = "Lake Canopy (Dressed)";
        const float Chunk = 64f;
        const float RoofHeight = 25f;

        [MenuItem("Evolve/Dress Lake: Root Canopy")]
        public static void Dress()
        {
            PrototypeSceneBuilder.EnsureFolder(Root + "/Materials");
            Physics.SyncTransforms();
            var old = GameObject.Find(DressedName);
            if (old != null) Object.DestroyImmediate(old);
            var root = new GameObject(DressedName).transform;

            var bark = LakeGround.OpaqueMaterial("Lake_CanopyBark", new Color(0.24f, 0.17f, 0.11f), 0.12f, false);
            var barkFar = LakeGround.OpaqueMaterial("Lake_CanopyBarkFar", new Color(0.20f, 0.15f, 0.10f), 0.05f, false);
            float half = WorldScale.ZoneSize * 0.5f;
            int chunks = 0;

            // ---- the roof ----
            var roof = new GameObject("Roof").transform;
            roof.SetParent(root, false);
            for (float x = -half; x < half; x += Chunk)
                for (float z = -half; z < half; z += Chunk)
                {
                    var area = new Rect(x, z, Chunk, Chunk);
                    float h = Mathf.Min(RoofHeight, LakeLayout.CeilingY(area.center.x, area.center.y, RoofHeight + 3f) - 3f);
                    if (h < 8f) continue;   // the corners, where the dome meets the cliffs
                    var near = RootMeshes.Lattice(area, h, 41, ZoneLayout.LightShafts, 8f, true);
                    var far = RootMeshes.Lattice(area, h, 41, ZoneLayout.LightShafts, 8f, false);
                    var go = new GameObject($"Roof {x:0},{z:0}");
                    go.transform.SetParent(roof, false);
                    var r0 = Part(go.transform, "LOD0", near, bark);
                    var r1 = Part(go.transform, "LOD1", far, barkFar);
                    var group = go.AddComponent<LODGroup>();
                    group.fadeMode = LODFadeMode.CrossFade;
                    group.animateCrossFading = true;
                    group.SetLODs(new[] { new LOD(0.35f, new Renderer[] { r0 }), new LOD(0.0001f, new Renderer[] { r1 }) });   // never culled
                    group.RecalculateBounds();
                    chunks++;
                }

            // ---- great roots climbing the rim ----
            var rim = new List<CombineInstance>();
            var rnd = new System.Random(77);
            float[] rows = { 206f, 220f, 236f };
            float[] spacing = { 11f, 15f, 21f };
            for (int row = 0; row < rows.Length; row++)
            {
                float d = rows[row];
                for (float s = -d; s < d; s += spacing[row])
                    foreach (var side in new[] { 0, 1, 2, 3 })
                    {
                        float along = s + (float)(rnd.NextDouble() - 0.5) * spacing[row] * 0.6f;
                        Vector2 p = side == 0 ? new Vector2(along, d) : side == 1 ? new Vector2(along, -d) : side == 2 ? new Vector2(d, along) : new Vector2(-d, along);
                        if (side == 0 && Mathf.Abs(p.x - ZoneLayout.NorthEntry.x) < 16f) continue;   // keep the hunter's way in open
                        Vector2 inward = -new Vector2(Mathf.Abs(p.x) >= Mathf.Abs(p.y) ? Mathf.Sign(p.x) : 0f, Mathf.Abs(p.y) > Mathf.Abs(p.x) ? Mathf.Sign(p.y) : 0f);
                        float ground = TerrainShape.HeightAt(p.x, p.y, 1337) - 0.5f;
                        float top = Mathf.Min(RoofHeight, LakeLayout.CeilingY(p.x, p.y, RoofHeight + 3f) - 1f);
                        if (top - ground < 3f) continue;
                        var path = new Vector3[9];
                        float lean = 2f + (float)rnd.NextDouble() * 5f, twist = (float)(rnd.NextDouble() - 0.5) * 6f;
                        for (int k = 0; k < path.Length; k++)
                        {
                            float t = k / (float)(path.Length - 1);
                            Vector2 q = p + inward * (lean * Mathf.Sin(t * Mathf.PI * 0.9f) + 3f * t) + new Vector2(-inward.y, inward.x) * twist * t * t;
                            path[k] = new Vector3(q.x, Mathf.Lerp(ground, top, t), q.y);
                        }
                        float baseR = (row == 0 ? 1.6f : row == 1 ? 1.9f : 2.3f) * (0.75f + (float)rnd.NextDouble() * 0.5f);
                        rim.Add(new CombineInstance { mesh = RootMeshes.RootTube(path, baseR, baseR * 0.55f, 9000 + rim.Count, 0.25f, 8), transform = Matrix4x4.identity });
                    }
            }
            LakeGround.AddCombined(root, "Rim Roots", rim, bark);   // big and few: always drawn

            EditorSceneManager.MarkAllScenesDirty();
            AssetDatabase.SaveAssets();
            Debug.Log($"Đã dựng mái lưới rễ ({chunks} ô) và {rim.Count} rễ lớn quanh vách. Bấm Ctrl+S để lưu scene.");
        }

        static Renderer Part(Transform parent, string name, Mesh mesh, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = material;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            go.isStatic = true;
            return mr;
        }
    }
}
