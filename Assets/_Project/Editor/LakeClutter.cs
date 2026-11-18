using System.Collections.Generic;
using Evolve.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Evolve.EditorTools
{
    /// <summary>
    /// Evolve → Dress Lake: Ground Clutter. Covers the ground with thousands of little real 3D things so it is no longer
    /// a flat layer: grass tufts, tall reeds by the water, ferns, moss clumps, pebbles, twigs, fallen leaves, clusters of tiny
    /// mushrooms, small bushes and root nubs. They are Unity terrain "details" (GPU-instanced meshes that Unity draws only
    /// near the camera and culls by itself), so they cost very little however many there are. What grows where follows
    /// World/PlantCommunities (each area its own mix), the water (reeds and pebbles on the banks) and keeps off the trails,
    /// the hall, the houses and the water. Run after Dress Lake: Terrain. Run again to repaint.
    /// </summary>
    public static class LakeClutter
    {
        const string Root = "Assets/_Project";
        const int Res = 512;   // detail map: about 1 m per cell over 500 m

        enum Kind { Grass, Reeds, Fern, MossClump, Pebbles, Twigs, Leaves, MiniMushrooms, Bush, RootNub }

        [MenuItem("Evolve/Dress Lake: Ground Clutter")]
        public static void Dress()
        {
            var terrain = Object.FindAnyObjectByType<Terrain>();
            if (terrain == null) { Debug.LogWarning("Không có Terrain trong scene. Bỏ qua bước phủ chi tiết mặt đất."); return; }
            PrototypeSceneBuilder.EnsureFolder(Root + "/Models");
            PrototypeSceneBuilder.EnsureFolder(Root + "/Prefabs");

            // ---- the prototypes ----
            var protos = new List<DetailPrototype>();
            foreach (Kind k in System.Enum.GetValues(typeof(Kind))) protos.Add(MakePrototype(k));
            var data = terrain.terrainData;
            data.SetDetailResolution(Res, 16);
            data.detailPrototypes = protos.ToArray();

            // ---- the density maps ----
            Vector3 origin = terrain.transform.position, size = data.size;
            var maps = new int[protos.Count][,];
            for (int i = 0; i < maps.Length; i++) maps[i] = new int[Res, Res];
            var houses = CityLayout.Houses();
            var classHouses = CityLayout.ClassHouses();
            var paths = CityLayout.HousePaths();
            Vector2 hall = CityLayout.HallCenter;
            for (int z = 0; z < Res; z++)
            {
                if (z % 32 == 0 && EditorUtility.DisplayCancelableProgressBar("Phủ chi tiết mặt đất", $"{z}/{Res}", z / (float)Res)) { EditorUtility.ClearProgressBar(); return; }
                for (int x = 0; x < Res; x++)
                {
                    float wx = origin.x + (x + 0.5f) / Res * size.x, wz = origin.z + (z + 0.5f) / Res * size.z;
                    if (Mathf.Max(Mathf.Abs(wx), Mathf.Abs(wz)) > 205f) continue;                       // not up the rim cliffs
                    float depth = ZoneLayout.CleanDepthAt(wx, wz);
                    if (depth > 0f) continue;                                                           // nothing under water
                    if (Blocked(wx, wz, hall, houses, classHouses, paths)) continue;
                    float dw = ZoneLayout.DistanceToWater(wx, wz), dp = ZoneLayout.DistanceToPath(wx, wz);
                    var d = PlantCommunities.DensityAll(wx, wz, 0.45f);
                    float onTrail = Mathf.Clamp01((dp - 1.4f) / 2.2f);                                  // clear in the trail's middle
                    float macro = ZoneLayout.Noise(wx / 17f + 5f, wz / 17f + 9f);
                    float macro2 = ZoneLayout.Noise(wx / 6f + 31f, wz / 6f + 12f);
                    float moss = d[(int)Plant.WetMoss] + d[(int)Plant.ThickMoss] + d[(int)Plant.Clover] + 0.5f * d[(int)Plant.SmallCream];
                    float leaf = d[(int)Plant.LeafLitter] + 0.5f * d[(int)Plant.EarFungus];
                    float roots = d[(int)Plant.SurfaceRoot] + d[(int)Plant.DarkRoot];
                    float bank = Mathf.Clamp01(1f - Mathf.Abs(dw - 3.2f) / 3.2f);                       // the water's margin
                    float wild = 0.25f + 0.6f * macro;

                    Put(maps, Kind.Grass, x, z, onTrail * (wild * (1f - 0.8f * leaf) + 0.9f * moss) * 7f, wx, wz, 1);
                    Put(maps, Kind.Reeds, x, z, onTrail * bank * bank * (0.4f + macro2) * 6f, wx, wz, 2);
                    Put(maps, Kind.Fern, x, z, onTrail * (0.8f * moss + 0.9f * roots + 0.15f * macro) * 3.5f, wx, wz, 3);
                    Put(maps, Kind.MossClump, x, z, onTrail * (1.2f * moss + 0.2f * macro2) * 2.5f, wx, wz, 4);
                    Put(maps, Kind.Pebbles, x, z, (0.35f * Mathf.Clamp01((macro2 - 0.5f) * 3f) + 1.1f * bank * macro2 + 0.5f * d[(int)Plant.LimestoneRock]) * 3.5f, wx, wz, 5);
                    Put(maps, Kind.Twigs, x, z, onTrail * (0.3f * wild + 0.8f * leaf + 0.6f * roots) * 2.5f, wx, wz, 6);
                    Put(maps, Kind.Leaves, x, z, (1.6f * leaf + 0.15f * macro) * 4f, wx, wz, 7);
                    Put(maps, Kind.MiniMushrooms, x, z, onTrail * (1.4f * (d[(int)Plant.SmallCream] + d[(int)Plant.CreamMushroom]) + 0.5f * d[(int)Plant.EarFungus]) * 2.2f, wx, wz, 8);
                    Put(maps, Kind.Bush, x, z, onTrail * (0.6f * moss + 0.3f * wild * macro) * 0.55f, wx, wz, 9);
                    Put(maps, Kind.RootNub, x, z, onTrail * (1.3f * roots + 0.12f * wild) * 1.6f, wx, wz, 10);
                }
            }
            EditorUtility.ClearProgressBar();
            for (int i = 0; i < maps.Length; i++) data.SetDetailLayer(0, 0, i, maps[i]);

            terrain.detailObjectDistance = 85f;      // drawn only this close to the camera
            terrain.detailObjectDensity = 0.9f;
            terrain.drawInstanced = true;
            EditorUtility.SetDirty(data);
            EditorSceneManager.MarkAllScenesDirty();
            AssetDatabase.SaveAssets();
            Debug.Log("Đã phủ chi tiết 3D lên mặt đất: cỏ, lau sậy, dương xỉ, cụm rêu, cuội, cành khô, lá rụng, nấm nhỏ, bụi cây, mấu rễ (chỉ vẽ trong ~85 m quanh camera). Bấm Ctrl+S để lưu scene.");
        }

        /// <summary>Count for a cell: the fractional part is decided by a hash of the cell, so there is no banding.</summary>
        static void Put(int[][,] maps, Kind kind, int x, int z, float amount, float wx, float wz, int salt)
        {
            if (amount <= 0.02f) return;
            float r = Hash(x, z, salt);
            maps[(int)kind][z, x] = Mathf.Clamp(Mathf.FloorToInt(amount + r), 0, 12);
        }

        static float Hash(int x, int y, int s)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + s * 1274126177;
                h = (h ^ (h >> 13)) * 1274126177;
                h ^= h >> 16;
                return (h & 0xffff) / 65536f;
            }
        }

        /// <summary>Where nothing is scattered: inside the hall, over the tunnel, at the houses and their paving, beside the class houses.</summary>
        static bool Blocked(float x, float z, Vector2 hall, CityLayout.House[] houses, CityLayout.ClassHouse[] classHouses, List<Vector2[]> paths)
        {
            var p = new Vector2(x, z);
            if (ZoneLayout.InsideCave(x, z, 1.5f) || ZoneLayout.DistanceToLine(p, ZoneLayout.TunnelLine) < 3.5f) return true;
            if ((p - hall).sqrMagnitude > 90f * 90f) return false;   // the village is within 90 m of the hall
            foreach (var h in houses) if (Vector2.Distance(p, h.Position) < h.Radius + 1.6f) return true;
            foreach (var c in classHouses) if (Vector2.Distance(p, c.Position) < 5f) return true;
            foreach (var path in paths) if (ZoneLayout.DistanceToLine(p, path) < 1.4f) return true;
            // the paved ring round the hall
            float nx = (x - hall.x) / (ZoneLayout.CaveRadii.x + CityLayout.RingOffset), nz = (z - hall.y) / (ZoneLayout.CaveRadii.y + CityLayout.RingOffset);
            if (Mathf.Abs(Mathf.Sqrt(nx * nx + nz * nz) - 1f) * 26f < 1.8f) return true;
            return false;
        }

        // ---------------------------------------------------------------- the prototypes

        static DetailPrototype MakePrototype(Kind kind)
        {
            string name = "Clutter_" + kind;
            string meshPath = $"{Root}/Models/{name}.asset", prefabPath = $"{Root}/Prefabs/{name}.prefab";
            Material[] mats;
            Mesh mesh = Build(kind, out mats);
            AssetDatabase.DeleteAsset(meshPath);
            AssetDatabase.CreateAsset(mesh, meshPath);
            var go = new GameObject(name);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterials = mats;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);

            var proto = new DetailPrototype
            {
                prototype = prefab,
                usePrototypeMesh = true,
                useInstancing = true,
                renderMode = DetailRenderMode.VertexLit,
                alignToGround = kind == Kind.Leaves || kind == Kind.Pebbles ? 0.8f : 0.2f,
                positionJitter = 0.9f,
                holeEdgePadding = 0f,
                noiseSpread = 6f,
                useDensityScaling = false,
            };
            // size ranges (a multiple of the mesh, which is built at a natural size)
            switch (kind)
            {
                case Kind.Grass: proto.minWidth = 0.7f; proto.maxWidth = 1.5f; proto.minHeight = 0.6f; proto.maxHeight = 1.5f; break;
                case Kind.Reeds: proto.minWidth = 0.8f; proto.maxWidth = 1.3f; proto.minHeight = 0.7f; proto.maxHeight = 1.4f; break;
                case Kind.Fern: proto.minWidth = 0.7f; proto.maxWidth = 1.5f; proto.minHeight = 0.7f; proto.maxHeight = 1.4f; break;
                case Kind.Bush: proto.minWidth = 0.7f; proto.maxWidth = 1.6f; proto.minHeight = 0.7f; proto.maxHeight = 1.6f; break;
                default: proto.minWidth = 0.6f; proto.maxWidth = 1.5f; proto.minHeight = 0.6f; proto.maxHeight = 1.5f; break;
            }
            return proto;
        }

        static Material Mat(string name, Color color, Color emission = default)
        {
            var m = LakeGround.OpaqueMaterial("Clutter_" + name, color, 0.08f, true, emission);
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            return m;
        }

        static Mesh Build(Kind kind, out Material[] mats)
        {
            var rnd = new System.Random((int)kind * 7919 + 13);
            Mesh mesh;
            switch (kind)
            {
                case Kind.Grass:
                {
                    var spots = new List<Vector3>();
                    for (int i = 0; i < 4; i++) spots.Add(new Vector3(((float)rnd.NextDouble() - 0.5f) * 0.4f, 0f, ((float)rnd.NextDouble() - 0.5f) * 0.4f));
                    mesh = GroundMeshes.Tufts(spots, 100, 0.42f);
                    mats = new[] { Mat("Grass", new Color(0.38f, 0.5f, 0.2f)) };
                    break;
                }
                case Kind.Reeds:
                {
                    var spots = new List<Vector3>();
                    for (int i = 0; i < 5; i++) spots.Add(new Vector3(((float)rnd.NextDouble() - 0.5f) * 0.35f, 0f, ((float)rnd.NextDouble() - 0.5f) * 0.35f));
                    mesh = GroundMeshes.Tufts(spots, 200, 1.1f);
                    mats = new[] { Mat("Reed", new Color(0.5f, 0.55f, 0.26f)) };
                    break;
                }
                case Kind.Fern:
                    mesh = Fronds(9, 0.7f, 0.16f, 0.5f);
                    mats = new[] { Mat("Fern", new Color(0.24f, 0.42f, 0.16f)) };
                    break;
                case Kind.MossClump:
                {
                    var parts = new List<CombineInstance>();
                    var blob = FloraMeshes.Blob();
                    for (int i = 0; i < 4; i++)
                        parts.Add(new CombineInstance { mesh = blob, transform = Matrix4x4.TRS(new Vector3(((float)rnd.NextDouble() - 0.5f) * 0.3f, 0.02f, ((float)rnd.NextDouble() - 0.5f) * 0.3f), Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f), new Vector3(0.28f + 0.1f * (float)rnd.NextDouble(), 0.12f, 0.24f)) });
                    mesh = Combine(parts);
                    mats = new[] { Mat("MossClump", new Color(0.26f, 0.4f, 0.13f)) };
                    break;
                }
                case Kind.Pebbles:
                {
                    var parts = new List<CombineInstance>();
                    var blob = FloraMeshes.Blob();
                    for (int i = 0; i < 6; i++)
                    {
                        float s = 0.05f + 0.07f * (float)rnd.NextDouble();
                        parts.Add(new CombineInstance { mesh = blob, transform = Matrix4x4.TRS(new Vector3(((float)rnd.NextDouble() - 0.5f) * 0.5f, s * 0.2f, ((float)rnd.NextDouble() - 0.5f) * 0.5f), Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f), new Vector3(s * 1.6f, s * 0.9f, s * 1.3f)) });
                    }
                    mesh = Combine(parts);
                    mats = new[] { Mat("Pebbles", new Color(0.52f, 0.49f, 0.44f)) };
                    break;
                }
                case Kind.Twigs:
                {
                    var parts = new List<CombineInstance>();
                    for (int i = 0; i < 3; i++)
                    {
                        float a = (float)rnd.NextDouble() * Mathf.PI, len = 0.45f + 0.4f * (float)rnd.NextDouble();
                        Vector3 dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                        Vector3 c = new Vector3(((float)rnd.NextDouble() - 0.5f) * 0.3f, 0.03f + 0.02f * i, ((float)rnd.NextDouble() - 0.5f) * 0.3f);
                        var path = new[] { c - dir * len * 0.5f, c + new Vector3(0f, 0.04f, 0f), c + dir * len * 0.5f + Vector3.up * 0.01f };
                        parts.Add(new CombineInstance { mesh = RootMeshes.RootTube(path, 0.022f, 0.01f, 300 + i, 0.2f, 5), transform = Matrix4x4.identity });
                        var side = new[] { c, c + dir * len * 0.15f + Vector3.Cross(Vector3.up, dir) * 0.18f + Vector3.up * 0.05f };
                        parts.Add(new CombineInstance { mesh = RootMeshes.RootTube(side, 0.012f, 0.006f, 310 + i, 0.1f, 4), transform = Matrix4x4.identity });
                    }
                    mesh = Combine(parts);
                    mats = new[] { Mat("Twigs", new Color(0.32f, 0.22f, 0.14f)) };
                    break;
                }
                case Kind.Leaves:
                {
                    var spots = new List<Vector3>();
                    for (int i = 0; i < 9; i++) spots.Add(new Vector3(((float)rnd.NextDouble() - 0.5f) * 0.7f, 0.02f + 0.012f * (i % 3), ((float)rnd.NextDouble() - 0.5f) * 0.7f));
                    mesh = GroundMeshes.FallenLeaves(spots, 400, 0.17f);
                    mats = new[] { Mat("Leaves", new Color(0.58f, 0.34f, 0.13f)) };
                    break;
                }
                case Kind.MiniMushrooms:
                {
                    var stalks = new List<CombineInstance>();
                    var caps = new List<CombineInstance>();
                    for (int i = 0; i < 4; i++)
                    {
                        FloraMeshes.Mushroom(500 + i, 0.14f + 0.12f * (float)rnd.NextDouble(), out var st, out var cp, out _);
                        var m = Matrix4x4.Translate(new Vector3(((float)rnd.NextDouble() - 0.5f) * 0.3f, 0f, ((float)rnd.NextDouble() - 0.5f) * 0.3f));
                        stalks.Add(new CombineInstance { mesh = st, transform = m });
                        caps.Add(new CombineInstance { mesh = cp, transform = m });
                    }
                    var two = new List<CombineInstance> { new CombineInstance { mesh = Combine(stalks), transform = Matrix4x4.identity }, new CombineInstance { mesh = Combine(caps), transform = Matrix4x4.identity } };
                    mesh = new Mesh { name = "MiniMushrooms" };
                    mesh.CombineMeshes(two.ToArray(), false, false);   // two sub-meshes: stalks, caps
                    mesh.RecalculateBounds();
                    mats = new[] { Mat("MushroomStalk", new Color(0.93f, 0.89f, 0.78f)), Mat("MushroomCap", new Color(0.9f, 0.84f, 0.66f), new Color(0.07f, 0.06f, 0.04f)) };
                    break;
                }
                case Kind.Bush:
                {
                    var parts = new List<CombineInstance>();
                    var blob = FloraMeshes.Blob();
                    for (int i = 0; i < 6; i++)
                    {
                        float a = i / 6f * Mathf.PI * 2f + (float)rnd.NextDouble();
                        float r = 0.25f + 0.2f * (float)rnd.NextDouble(), s = 0.28f + 0.14f * (float)rnd.NextDouble();
                        parts.Add(new CombineInstance { mesh = blob, transform = Matrix4x4.TRS(new Vector3(Mathf.Cos(a) * r, 0.28f + 0.25f * (float)rnd.NextDouble(), Mathf.Sin(a) * r), Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f), new Vector3(s * 1.3f, s, s * 1.2f)) });
                    }
                    parts.Add(new CombineInstance { mesh = blob, transform = Matrix4x4.TRS(new Vector3(0f, 0.55f, 0f), Quaternion.identity, new Vector3(0.5f, 0.38f, 0.5f)) });
                    mesh = Combine(parts);
                    mats = new[] { Mat("Bush", new Color(0.28f, 0.42f, 0.16f)) };
                    break;
                }
                default:   // RootNub: a short arch of root coming out of the ground
                {
                    var path = RootMeshes.ArchPath(new Vector3(-0.35f, 0f, 0f), new Vector3(0.35f, 0f, 0.05f), 0.28f, 600, 0.4f, 8, 0.1f);
                    mesh = RootMeshes.RootTube(path, 0.07f, 0.045f, 601, 0.3f, 6);
                    mats = new[] { Mat("RootNub", new Color(0.3f, 0.21f, 0.13f)) };
                    break;
                }
            }
            mesh.name = "Clutter_" + kind;
            return mesh;
        }

        static Mesh Combine(List<CombineInstance> parts)
        {
            var mesh = new Mesh();
            mesh.CombineMeshes(parts.ToArray(), true, true);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>A fan of arching fronds (double-sided strips tapering to a point), for ferns.</summary>
        static Mesh Fronds(int count, float length, float width, float droop)
        {
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            const int seg = 6;
            for (int f = 0; f < count; f++)
            {
                float a = f / (float)count * Mathf.PI * 2f + 0.3f * (f % 2);
                Vector3 dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)), side = new Vector3(-dir.z, 0f, dir.x);
                float len = length * (0.8f + 0.35f * ((f * 37) % 10) / 10f);
                int b = verts.Count;
                for (int s = 0; s <= seg; s++)
                {
                    float t = s / (float)seg;
                    Vector3 c = dir * len * t * 0.8f + Vector3.up * (len * (0.55f * Mathf.Sin(t * Mathf.PI * 0.6f)) - droop * len * t * t * 0.5f) + Vector3.up * 0.02f;
                    float w = width * Mathf.Sin(Mathf.Clamp01(t * 1.15f) * Mathf.PI) * (1f - t * 0.4f) + 0.004f;
                    verts.Add(c + side * w); verts.Add(c - side * w);
                    uvs.Add(new Vector2(0f, t)); uvs.Add(new Vector2(1f, t));
                }
                for (int s = 0; s < seg; s++)
                {
                    int i = b + s * 2;
                    tris.Add(i); tris.Add(i + 2); tris.Add(i + 1);
                    tris.Add(i + 1); tris.Add(i + 2); tris.Add(i + 3);
                }
            }
            var mesh = new Mesh { name = "Fronds" };
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
