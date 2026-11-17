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

            // ---- the earth above the roots: a lumpy ceiling of soil and root mat, so no bare grey shows through the gaps ----
            var earth = CeilingMaterial();
            var ceiling = new GameObject("Ceiling Earth").transform;
            ceiling.SetParent(root, false);
            for (float x = -half; x < half; x += Chunk)
                for (float z = -half; z < half; z += Chunk)
                {
                    var area = new Rect(x, z, Chunk, Chunk);
                    float h = Mathf.Min(RoofHeight, LakeLayout.CeilingY(area.center.x, area.center.y, RoofHeight + 3f) - 3f);
                    if (h < 8f) continue;
                    var mesh = CeilingMesh(area, h + 3.5f);
                    if (mesh.vertexCount > 0) Part(ceiling, $"Earth {x:0},{z:0}", mesh, earth);
                }
            // the dome behind it all: dark earth, not grey
            var dome = GameObject.Find("Dome");
            var domeRenderer = dome != null ? dome.GetComponent<Renderer>() : null;
            if (domeRenderer != null && domeRenderer.sharedMaterial != null)
            {
                domeRenderer.sharedMaterial.SetColor("_BaseColor", new Color(0.06f, 0.045f, 0.03f));
                domeRenderer.sharedMaterial.SetFloat("_Smoothness", 0f);
                EditorUtility.SetDirty(domeRenderer.sharedMaterial);
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

        /// <summary>
        /// A lumpy sheet of earth over <paramref name="area"/> at about <paramref name="height"/> m, sagging into knobs (root
        /// clumps), with holes where the light shafts come through. World-space vertices, facing down, UVs tile every 8 m.
        /// </summary>
        static Mesh CeilingMesh(Rect area, float height)
        {
            const float cell = 4f;
            int n = Mathf.RoundToInt(area.width / cell);
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            for (int j = 0; j <= n; j++)
                for (int i = 0; i <= n; i++)
                {
                    float x = area.xMin + i * cell, z = area.yMin + j * cell;
                    float lumps = 3f * (ZoneLayout.Noise(x / 14f + 900f, z / 14f + 900f) - 0.5f);
                    float knobs = 2.5f * Mathf.Max(0f, ZoneLayout.Noise(x / 5f + 950f, z / 5f + 950f) - 0.6f) / 0.4f;   // root clumps hang down
                    verts.Add(new Vector3(x, height + lumps - knobs, z));
                    uvs.Add(new Vector2(x / 8f, z / 8f));
                }
            for (int j = 0; j < n; j++)
                for (int i = 0; i < n; i++)
                {
                    Vector2 c = new Vector2(area.xMin + (i + 0.5f) * cell, area.yMin + (j + 0.5f) * cell);
                    bool hole = false;
                    foreach (var shaft in ZoneLayout.LightShafts) if (Vector2.Distance(c, shaft) < 7f) { hole = true; break; }
                    if (hole) continue;
                    int a = j * (n + 1) + i, b = a + 1, d = a + n + 1, e = d + 1;
                    tris.Add(a); tris.Add(b); tris.Add(d);   // clockwise seen from below: faces down
                    tris.Add(b); tris.Add(e); tris.Add(d);
                }
            var mesh = new Mesh { name = "CeilingEarth" };
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        static Material CeilingMaterial()
        {
            const string path = Root + "/Models/LakeCeilingEarth.png";
            var tex = LakeWater.PaintedTexture(path, 128, 128, (u, v) =>
            {
                float big = LakeWater.TileableNoise(u * 4f, v * 4f, 4, 4);
                float fine = LakeWater.TileableNoise(u * 32f, v * 32f, 32, 32);
                var c = new Color(0.20f, 0.14f, 0.09f) * (0.7f + 0.35f * big + 0.15f * fine);
                // thin root strands woven through the earth
                float strands = Mathf.Abs(Mathf.Sin((u * 6f + 0.6f * LakeWater.TileableNoise(u * 5f, v * 5f, 5, 5)) * Mathf.PI * 2f));
                if (strands > 0.93f) c = Color.Lerp(c, new Color(0.34f, 0.24f, 0.15f), 0.7f);
                c.a = 1f;
                return c;
            });
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer != null && importer.wrapModeV != TextureWrapMode.Repeat)
            {
                importer.wrapModeU = TextureWrapMode.Repeat;
                importer.wrapModeV = TextureWrapMode.Repeat;
                importer.SaveAndReimport();
                tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }
            var mat = LakeGround.OpaqueMaterial("Lake_CeilingEarth", new Color(0.85f, 0.85f, 0.85f), 0.03f, true);
            mat.SetTexture("_BaseMap", tex);
            EditorUtility.SetDirty(mat);
            return mat;
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
