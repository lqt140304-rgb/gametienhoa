using System.Collections.Generic;
using Evolve.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Evolve.EditorTools
{
    /// <summary>
    /// Evolve -> Dress Lake: Grass. A thick carpet of real 3D grass round the water (World/GrassMath.Density: full within about 30 m of the water, thinning to
    /// nothing by 80 m, patchy with clearings, off the trails). Four clump variants (0.55 to 1.15 m tall, 5 to 8 tapering, curved blades each) are
    /// terrain "details": GPU-instanced, culled and faded by Unity itself. The material uses Evolve/GrassBlade, which sways in a faint breeze and
    /// bends the blades away from every walker (World/GrassPushers feeds it the nearest ones). Run after Dress Lake: Ground Clutter (which resets
    /// the terrain's detail prototypes); rerunnable.
    /// </summary>
    public static class LakeGrass
    {
        const string Root = "Assets/_Project";
        const string ShaderName = "Evolve/GrassBlade";
        const string Prefix = "Lake_Grass_";
        const int Variants = 4;
        static readonly float[] Heights = { 0.55f, 0.75f, 0.95f, 1.15f };
        static readonly int[] Blades = { 8, 7, 6, 5 };
        static readonly Color[] Tips = { new Color(0.30f, 0.54f, 0.15f), new Color(0.34f, 0.58f, 0.17f), new Color(0.40f, 0.58f, 0.18f), new Color(0.27f, 0.50f, 0.20f) };

        [MenuItem("Evolve/Bản đồ cũ (Underroot)/Dress Lake: Grass")]
        public static void Dress()
        {
            var terrain = Object.FindAnyObjectByType<Terrain>();
            if (terrain == null) { Debug.LogWarning("Không có Terrain trong scene: bỏ qua thảm cỏ."); return; }
            var shader = Shader.Find(ShaderName);
            if (shader == null) { Debug.LogWarning("Không tìm thấy shader " + ShaderName + ": bỏ qua thảm cỏ."); return; }
            PrototypeSceneBuilder.EnsureFolder(Root + "/Models");
            PrototypeSceneBuilder.EnsureFolder(Root + "/Models/Grass");
            PrototypeSceneBuilder.EnsureFolder(Root + "/Prefabs");

            var prefabs = new GameObject[Variants];
            for (int v = 0; v < Variants; v++) prefabs[v] = MakePrefab(v, shader);

            var data = terrain.terrainData;
            int res = data.detailResolution;
            // keep every other detail layer (clutter) exactly as it is; only the grass prototypes are replaced
            var keep = new List<DetailPrototype>();
            var keepLayers = new List<int[,]>();
            var all = data.detailPrototypes;
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].prototype != null && all[i].prototype.name.StartsWith(Prefix)) continue;
                keep.Add(all[i]);
                keepLayers.Add(data.GetDetailLayer(0, 0, res, res, i));
            }
            var protos = new List<DetailPrototype>(keep);
            int first = protos.Count;
            for (int v = 0; v < Variants; v++)
                protos.Add(new DetailPrototype
                {
                    prototype = prefabs[v],
                    usePrototypeMesh = true,
                    useInstancing = true,
                    renderMode = DetailRenderMode.VertexLit,
                    minWidth = 0.9f, maxWidth = 1.35f, minHeight = 0.8f, maxHeight = 1.4f,
                    alignToGround = 0.1f,
                    positionJitter = 0.9f,
                    holeEdgePadding = 0f,
                    noiseSpread = 7f,
                    useDensityScaling = false
                });
            data.detailPrototypes = protos.ToArray();
            for (int i = 0; i < keepLayers.Count; i++) data.SetDetailLayer(0, 0, i, keepLayers[i]);

            Vector3 origin = terrain.transform.position, size = data.size;
            var maps = new int[Variants][,];
            for (int v = 0; v < Variants; v++) maps[v] = new int[res, res];
            long clumps = 0;
            for (int z = 0; z < res; z++)
            {
                if (z % 32 == 0 && EditorUtility.DisplayCancelableProgressBar("Trồng thảm cỏ", $"{z}/{res}", z / (float)res)) { EditorUtility.ClearProgressBar(); return; }
                for (int x = 0; x < res; x++)
                {
                    float wx = origin.x + (x + 0.5f) / res * size.x, wz = origin.z + (z + 0.5f) / res * size.z;
                    float dens = GrassMath.Density(wx, wz);
                    if (dens <= 0.03f) continue;
                    float amount = dens * 6f;     // clumps per cell (about 1 m x 1 m), split over the variants
                    for (int v = 0; v < Variants; v++)
                    {
                        int n = Mathf.Clamp(Mathf.FloorToInt(amount / Variants + Hash(x, z, v)), 0, 8);
                        maps[v][z, x] = n;
                        clumps += n;
                    }
                }
            }
            EditorUtility.ClearProgressBar();
            for (int v = 0; v < Variants; v++) data.SetDetailLayer(0, 0, first + v, maps[v]);
            terrain.drawInstanced = true;
            EditorUtility.SetDirty(data);
            EditorSceneManager.MarkAllScenesDirty();
            AssetDatabase.SaveAssets();
            Debug.Log($"Đã trồng thảm cỏ: khoảng {clumps} bụi quanh vùng nước, rẽ sang hai bên khi đi qua (cần GrassPushers chạy khi Play). Bấm Ctrl+S để lưu scene.");
        }

        static float Hash(int x, int y, int s)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + (s + 5) * 1274126177;
                h = (h ^ (h >> 13)) * 1274126177;
                h ^= h >> 16;
                return (h & 0xffff) / 65536f;
            }
        }

        static GameObject MakePrefab(int variant, Shader shader)
        {
            string name = Prefix + variant;
            string meshPath = $"{Root}/Models/Grass/{name}.asset", matPath = $"{Root}/Materials/{name}.mat", prefabPath = $"{Root}/Prefabs/{name}.prefab";
            var mesh = BuildClump(variant);
            AssetDatabase.DeleteAsset(meshPath);
            AssetDatabase.CreateAsset(mesh, meshPath);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null) { mat = new Material(shader) { name = name }; AssetDatabase.CreateAsset(mat, matPath); }
            mat.shader = shader;
            mat.SetColor("_Root", new Color(0.04f, 0.10f, 0.03f));
            mat.SetColor("_Tip", Tips[variant]);
            mat.SetFloat("_Height", Heights[variant]);
            mat.SetFloat("_Wind", 0.05f + 0.01f * variant);
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            var go = new GameObject(name);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = true;
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);
            return prefab;
        }

        /// <summary>One clump: a handful of blades, each a tapering ribbon of five rows that curves over; uv.y is the height from root (0) to tip (1).</summary>
        static Mesh BuildClump(int variant)
        {
            var rnd = new System.Random(31 + variant * 17);
            var verts = new List<Vector3>();
            var norms = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            const int rows = 5;
            float baseHeight = Heights[variant];
            for (int b = 0; b < Blades[variant]; b++)
            {
                float yaw = (float)rnd.NextDouble() * Mathf.PI * 2f;
                float radius = 0.16f * Mathf.Sqrt((float)rnd.NextDouble());
                Vector3 root = new Vector3(Mathf.Cos(yaw + 1.3f) * radius, 0f, Mathf.Sin(yaw + 1.3f) * radius);
                float height = baseHeight * (0.65f + 0.35f * (float)rnd.NextDouble());
                float lean = 0.10f + 0.34f * (float)rnd.NextDouble();             // how far the tip leans, as a fraction of the height
                float width = 0.05f + 0.025f * (float)rnd.NextDouble();
                Vector3 dir = new Vector3(Mathf.Cos(yaw), 0f, Mathf.Sin(yaw));
                Vector3 side = new Vector3(-dir.z, 0f, dir.x);
                int start = verts.Count;
                for (int r = 0; r < rows; r++)
                {
                    float t = r / (float)(rows - 1);
                    Vector3 c = root + dir * (lean * height * t * t) + Vector3.up * (height * (t - 0.12f * t * t * lean * 2f));
                    float w = width * (1f - t * t * 0.96f) * 0.5f;                // tapers to a point
                    Vector3 slope = (dir * (2f * lean * t) + Vector3.up).normalized;
                    Vector3 n = Vector3.Cross(side, slope).normalized;
                    if (r == rows - 1)
                    {
                        verts.Add(c); norms.Add(n); uvs.Add(new Vector2(0.5f, 1f));
                    }
                    else
                    {
                        verts.Add(c - side * w); norms.Add(n); uvs.Add(new Vector2(0f, t));
                        verts.Add(c + side * w); norms.Add(n); uvs.Add(new Vector2(1f, t));
                    }
                }
                for (int r = 0; r < rows - 2; r++)
                {
                    int a = start + r * 2;
                    tris.Add(a); tris.Add(a + 1); tris.Add(a + 2);
                    tris.Add(a + 1); tris.Add(a + 3); tris.Add(a + 2);
                }
                int last = start + (rows - 2) * 2;     // the tip triangle
                tris.Add(last); tris.Add(last + 1); tris.Add(last + 2);
            }
            var mesh = new Mesh { name = Prefix + variant };
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
