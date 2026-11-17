using System.Collections.Generic;
using Evolve.Ecology;
using Evolve.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Evolve.EditorTools
{
    /// <summary>
    /// Evolve → Dress Lake: Plant Communities. Covers the zone with each area's own plants (World/PlantCommunities): wet moss
    /// and little cream mushrooms by the Life Shallows, thick moss and clover at the Root Foot, leaf drifts, rotting logs and
    /// ear fungus in the Humus Bed, crowds of cream mushrooms in the Spore Garden, pale limestone in the Water Vein, bare
    /// trampled mud and dark roots at the Hunting Edge; blended where areas meet, in patches with gaps.
    /// Level of detail: every 64 m square is one LODGroup with three levels: LOD0 (near) everything; LOD1 (to ~180 m) the
    /// mid-sized things, a third of the small patches; LOD2 (to ~450 m) only the big things (big mushrooms, logs, roots,
    /// boulders). So the whole map can be seen from above without drawing every leaf. Run again to rebuild.
    /// </summary>
    public static class LakeCommunities
    {
        const string Root = "Assets/_Project";
        const string DressedName = "Lake Plants (Dressed)";
        const float Spacing = 2.5f;     // one candidate spot per 2.5 x 2.5 m
        const float Reach = 225f;

        internal struct Item { public Mesh Mesh; public Material Material; public Matrix4x4 Matrix; public int MaxLod; public Vector3? Center; }

        [MenuItem("Evolve/Dress Lake: Plant Communities")]
        public static void Dress()
        {
            PrototypeSceneBuilder.EnsureFolder(Root + "/Materials");
            PrototypeSceneBuilder.EnsureFolder(Root + "/Models");
            Physics.SyncTransforms();
            var old = GameObject.Find(DressedName);
            if (old != null) Object.DestroyImmediate(old);
            var root = new GameObject(DressedName).transform;
            var culling = root.gameObject.AddComponent<DetailCulling>();   // only for the sight fade of glowing caps:
            culling.ShowDistance = 5000f;                                   // distance is handled by the LODGroups

            var kit = new Kit();
            var keepOut = KeepOutPoints();
            var rnd = new System.Random(7070);
            var items = new List<Item>();
            int placed = 0;
            for (float x = -Reach; x <= Reach; x += Spacing)
                for (float z = -Reach; z <= Reach; z += Spacing)
                {
                    float px = x + (float)(rnd.NextDouble() - 0.5) * Spacing, pz = z + (float)(rnd.NextDouble() - 0.5) * Spacing;
                    double roll = rnd.NextDouble();
                    float yaw = (float)rnd.NextDouble() * 360f, size = (float)rnd.NextDouble();
                    var plant = PlantCommunities.Pick(px, pz, roll);
                    if (plant == null) continue;
                    if (TerrainShape.HeightAt(px, pz, 1337) > 15f) continue;   // up the rim cliffs
                    bool flat = plant == Plant.WetMoss || plant == Plant.ThickMoss || plant == Plant.Clover || plant == Plant.LeafLitter
                                || plant == Plant.BareMud || plant == Plant.TrackMark;
                    float trail = ZoneLayout.DistanceToPath(px, pz);
                    if (!flat && trail < ZoneLayout.PathWidth) continue;                    // trails stay clear
                    if (!flat && Near(keepOut, px, pz, 6f)) continue;                       // dens, shelters, the spawn
                    float y = LakeGround.GroundY(px, pz);
                    if (kit.Add(items, plant.Value, new Vector3(px, y, pz), yaw, size)) placed++;
                }

            Build(root, items);
            EditorSceneManager.MarkAllScenesDirty();
            AssetDatabase.SaveAssets();
            Debug.Log($"Đã trồng {placed} cụm thực vật theo quần xã (có LOD theo ô 64 m). Bấm Ctrl+S để lưu scene.");
        }

        static List<Vector2> KeepOutPoints()
        {
            var points = new List<Vector2>();
            foreach (var den in Object.FindObjectsByType<Den>()) points.Add(new Vector2(den.transform.position.x, den.transform.position.z));
            foreach (var s in Object.FindObjectsByType<ShelterZone>()) points.Add(new Vector2(s.transform.position.x, s.transform.position.z));
            foreach (var t in Object.FindObjectsByType<Transform>())
                if (t.name.StartsWith("Spawn Point")) points.Add(new Vector2(t.position.x, t.position.z));
            return points;
        }

        static bool Near(List<Vector2> points, float x, float z, float radius)
        {
            foreach (var p in points) if ((p.x - x) * (p.x - x) + (p.y - z) * (p.y - z) < radius * radius) return true;
            return false;
        }

        // ---------------------------------------------------------------- level of detail

        /// <summary>One LODGroup per 64 m square; each level combines its items per material.</summary>
        static void Build(Transform root, List<Item> items)
        {
            var chunks = new Dictionary<(int, int), List<Item>>();
            foreach (var item in items)
            {
                var key = LakeGround.Cell(item.Center ?? (Vector3)item.Matrix.GetColumn(3));   // draped items are already in world space
                if (!chunks.TryGetValue(key, out var list)) chunks[key] = list = new List<Item>();
                list.Add(item);
            }
            float[] heights = { 0.9f, 0.31f, 0.12f };   // ~60 m, ~180 m, ~450 m for a 64 m square (60° view)
            foreach (var chunk in chunks)
            {
                var go = new GameObject($"Plants {chunk.Key.Item1},{chunk.Key.Item2}");
                go.transform.SetParent(root, false);
                var lods = new List<LOD>();
                for (int level = 0; level < 3; level++)
                {
                    var byMaterial = new Dictionary<Material, List<CombineInstance>>();
                    foreach (var item in chunk.Value)
                    {
                        if (item.MaxLod < level) continue;
                        if (!byMaterial.TryGetValue(item.Material, out var parts)) byMaterial[item.Material] = parts = new List<CombineInstance>();
                        parts.Add(new CombineInstance { mesh = item.Mesh, transform = item.Matrix });
                    }
                    var renderers = new List<Renderer>();
                    var holder = new GameObject("LOD" + level).transform;
                    holder.SetParent(go.transform, false);
                    foreach (var pair in byMaterial)
                    {
                        var mesh = new Mesh { name = "Plants", indexFormat = IndexFormat.UInt32 };
                        mesh.CombineMeshes(pair.Value.ToArray(), true, true);
                        mesh.RecalculateBounds();
                        var part = new GameObject(pair.Key.name);
                        part.transform.SetParent(holder, false);
                        part.AddComponent<MeshFilter>().sharedMesh = mesh;
                        var mr = part.AddComponent<MeshRenderer>();
                        mr.sharedMaterial = pair.Key;
                        mr.shadowCastingMode = ShadowCastingMode.Off;
                        part.isStatic = true;
                        renderers.Add(mr);
                    }
                    lods.Add(new LOD(heights[level], renderers.ToArray()));
                }
                var group = go.AddComponent<LODGroup>();
                group.fadeMode = LODFadeMode.CrossFade;
                group.animateCrossFading = true;
                group.SetLODs(lods.ToArray());
                group.RecalculateBounds();
            }
        }

        // ---------------------------------------------------------------- what each plant looks like

        /// <summary>The meshes and materials of every plant, made once per run.</summary>
        class Kit
        {
            readonly List<Mesh> patches = new List<Mesh>(), tufts = new List<Mesh>(), leaves = new List<Mesh>(), fungus = new List<Mesh>(), tracks = new List<Mesh>();
            readonly List<(Mesh stalk, Mesh cap)> smallMush = new List<(Mesh, Mesh)>(), bigMush = new List<(Mesh, Mesh)>();
            readonly List<(Mesh rock, Mesh moss)> rocks = new List<(Mesh, Mesh)>();
            readonly List<Mesh> logs = new List<Mesh>(), roots = new List<Mesh>();
            readonly Material moss, mud, clover, stalk, fungusMat, logMat, rootMat, darkRootMat, stone, stoneMoss, track;
            readonly Material[] caps, leafMats;
            int smallIndex;   // every third small thing also shows in the middle distance

            public Kit()
            {
                for (int i = 0; i < 4; i++)
                {
                    patches.Add(WaterMeshes.Organic(WaterOutline.Make(2f, 2f, true, 300 + i, 0.35f, 28), 0.5f));
                    tufts.Add(GroundMeshes.Tufts(new[] { Vector3.zero, new Vector3(0.12f, 0f, 0.05f), new Vector3(-0.08f, 0f, 0.1f) }, 310 + i, 0.14f));
                    var spots = new List<Vector3>();
                    var r = new System.Random(320 + i);
                    for (int k = 0; k < 7; k++) spots.Add(new Vector3((float)(r.NextDouble() - 0.5) * 2.2f, 0.03f, (float)(r.NextDouble() - 0.5) * 2.2f));
                    leaves.Add(GroundMeshes.FallenLeaves(spots, 330 + i, 0.3f));
                }
                foreach (float h in new[] { 0.2f, 0.28f, 0.36f }) { FloraMeshes.Mushroom(340 + smallMush.Count, h, out var s, out var c, out _); smallMush.Add((s, c)); }
                foreach (float h in new[] { 0.55f, 0.8f, 1.2f, 1.8f, 2.6f }) { FloraMeshes.Mushroom(360 + bigMush.Count, h, out var s, out var c, out _); bigMush.Add((s, c)); }
                for (int i = 0; i < 4; i++) rocks.Add((GroundMeshes.Rock(380 + i, out var m, 0.82f), m));
                var blob = FloraMeshes.Blob();
                for (int i = 0; i < 3; i++)
                {
                    var r = new System.Random(390 + i);
                    var parts = new List<CombineInstance>();
                    for (int k = 0; k < 4; k++)
                        parts.Add(new CombineInstance { mesh = blob, transform = Matrix4x4.TRS(new Vector3((float)(r.NextDouble() - 0.5) * 0.5f, 0.05f + k * 0.07f, (float)(r.NextDouble() - 0.5) * 0.5f),
                            Quaternion.Euler(0f, (float)r.NextDouble() * 360f, 0f), new Vector3(0.32f, 0.06f, 0.22f)) });
                    fungus.Add(Combine(parts));
                    var claws = new List<CombineInstance>();
                    for (int k = 0; k < 3; k++)
                        claws.Add(new CombineInstance { mesh = blob, transform = Matrix4x4.TRS(new Vector3(k * 0.14f - 0.14f, 0.005f, 0f), Quaternion.Euler(0f, (k - 1) * 8f, 0f), new Vector3(0.07f, 0.012f, 0.42f)) });
                    tracks.Add(Combine(claws));
                }
                foreach (float len in new[] { 4f, 5.5f, 7f })
                {
                    var path = new Vector3[6];
                    for (int k = 0; k < path.Length; k++) path[k] = new Vector3((k / 5f - 0.5f) * len, 0f, Mathf.Sin(k * 0.9f) * 0.15f);
                    logs.Add(RootMeshes.RootTube(path, 0.75f, 0.6f, 400 + logs.Count, 0.2f, 10));
                    roots.Add(RootMeshes.RootTube(RootMeshes.ArchPath(new Vector3(-len, 0f, 0f), new Vector3(len, 0f, 0f), 0.7f, 410 + roots.Count, 0.9f, 10), 0.45f, 0.18f, 420 + roots.Count, 0.25f, 7));
                }

                moss = LakeGround.MossMaterial();
                mud = LakeGround.MudPatchMaterial();
                clover = LakeGround.OpaqueMaterial("Lake_Clover", new Color(0.30f, 0.50f, 0.20f), 0.1f, true);
                stalk = LakeGround.OpaqueMaterial("Lake_MushroomStalk", new Color(0.93f, 0.89f, 0.78f), 0.3f, false);
                var capColours = new[] { new Color(0.94f, 0.89f, 0.74f), new Color(0.88f, 0.70f, 0.42f), new Color(0.80f, 0.72f, 0.58f) };
                caps = new Material[3];
                for (int i = 0; i < 3; i++) caps[i] = LakeGround.OpaqueMaterial("Lake_MushroomCap" + i, capColours[i], 0.45f, false, capColours[i] * 0.10f);
                var leafColours = new[] { new Color(0.52f, 0.30f, 0.12f), new Color(0.64f, 0.42f, 0.17f), new Color(0.40f, 0.24f, 0.10f) };
                leafMats = new Material[3];
                for (int i = 0; i < 3; i++) leafMats[i] = LakeGround.OpaqueMaterial("Lake_Leaf" + i, leafColours[i], 0.1f, true);
                fungusMat = LakeGround.OpaqueMaterial("Lake_EarFungus", new Color(0.78f, 0.60f, 0.38f), 0.25f, false);
                logMat = LakeGround.OpaqueMaterial("Lake_LogBark", new Color(0.30f, 0.20f, 0.13f), 0.1f, false);
                rootMat = LakeGround.OpaqueMaterial("Lake_SurfaceRoot", new Color(0.33f, 0.24f, 0.16f), 0.12f, false);
                darkRootMat = LakeGround.OpaqueMaterial("Lake_DarkRoot", new Color(0.16f, 0.12f, 0.09f), 0.1f, false);
                stone = LakeGround.OpaqueMaterial("Lake_Limestone", new Color(0.74f, 0.72f, 0.64f), 0.2f, false);
                stoneMoss = LakeGround.OpaqueMaterial("Lake_RockMoss", new Color(0.30f, 0.50f, 0.22f), 0.1f, false);
                track = LakeGround.OpaqueMaterial("Lake_TrackMark", new Color(0.12f, 0.09f, 0.07f), 0.3f, false);
            }

            static Mesh Combine(List<CombineInstance> parts)
            {
                var mesh = new Mesh();
                mesh.CombineMeshes(parts.ToArray(), true, true);
                mesh.RecalculateBounds();
                return mesh;
            }

            /// <summary>Adds what <paramref name="plant"/> looks like at <paramref name="at"/>; size is a random 0..1.</summary>
            public bool Add(List<Item> items, Plant plant, Vector3 at, float yaw, float size)
            {
                var rot = Quaternion.Euler(0f, yaw, 0f);
                int v = (int)(size * 1000f);
                // small things: near view only, and a third of them also in the middle distance
                int Mid() => (smallIndex++ % 3 == 0) ? 1 : 0;
                void Put(Mesh m, Material mat, Vector3 pos, Vector3 scale, int maxLod) =>
                    items.Add(new Item { Mesh = m, Material = mat, Matrix = Matrix4x4.TRS(pos, rot, scale), MaxLod = maxLod });
                // flat things (moss, mud, leaves, tracks) are laid onto the ground's every bump, and never onto water
                void Lay(Mesh m, Material mat, Vector3 pos, Vector3 scale, float lift, int maxLod)
                {
                    var draped = LakeGround.Drape(m, Matrix4x4.TRS(new Vector3(pos.x, 0f, pos.z), rot, scale), lift);
                    if (draped != null) items.Add(new Item { Mesh = draped, Material = mat, Matrix = Matrix4x4.identity, MaxLod = maxLod, Center = draped.bounds.center });
                }

                switch (plant)
                {
                    case Plant.WetMoss:
                        Lay(patches[v % 4], moss, at, Vector3.one * (0.7f + size * 0.8f), 0.03f, Mid());
                        break;
                    case Plant.ThickMoss:
                    {
                        float r = 1.4f + size * 1.8f;
                        Lay(patches[v % 4], moss, at, new Vector3(r, 1f, r), 0.035f, r > 2.6f ? 2 : 1);
                        break;
                    }
                    case Plant.BareMud:
                        Lay(patches[v % 4], mud, at, new Vector3(1.2f + size * 1.4f, 1f, 1.2f + size * 1.4f), 0.025f, Mid());
                        break;
                    case Plant.Clover:
                        Put(tufts[v % 4], clover, at, Vector3.one * (0.8f + size * 0.6f), 0);
                        break;
                    case Plant.LeafLitter:
                        Lay(leaves[v % 4], leafMats[v % 3], at, Vector3.one * (0.8f + size * 0.5f), 0.03f, 0);
                        break;
                    case Plant.TrackMark:
                        Lay(tracks[v % 3], track, at, Vector3.one, 0.015f, 0);
                        break;
                    case Plant.EarFungus:
                        Put(fungus[v % 3], fungusMat, at, Vector3.one * (0.8f + size * 0.6f), 0);
                        break;
                    case Plant.SmallCream:
                    {
                        var m = smallMush[v % smallMush.Count];
                        for (int k = 0; k < 3; k++)
                        {
                            var off = Quaternion.Euler(0f, k * 120f + yaw, 0f) * new Vector3(0.18f + k * 0.07f, 0f, 0f);
                            float s = 0.8f + 0.25f * k;
                            Put(m.stalk, stalk, at + off, Vector3.one * s, 0);
                            Put(m.cap, caps[0], at + off, Vector3.one * s, 0);
                        }
                        break;
                    }
                    case Plant.CreamMushroom:
                    {
                        bool big = size > 0.8f;
                        var m = big ? bigMush[2 + v % 3] : bigMush[v % 2];
                        var capMat = caps[v % 3];
                        Put(m.stalk, stalk, at, Vector3.one * (0.85f + size * 0.3f), big ? 2 : Mid());
                        Put(m.cap, capMat, at, Vector3.one * (0.85f + size * 0.3f), big ? 2 : Mid());
                        if (!big)   // a little family round it
                        {
                            var s2 = smallMush[v % smallMush.Count];
                            var off = rot * new Vector3(0.45f, 0f, 0.2f);
                            Put(s2.stalk, stalk, at + off, Vector3.one, 0);
                            Put(s2.cap, capMat, at + off, Vector3.one, 0);
                        }
                        break;
                    }
                    case Plant.RottingLog:
                        Put(logs[v % 3], logMat, at + Vector3.up * 0.45f, Vector3.one * (0.9f + size * 0.3f), 2);
                        Put(fungus[v % 3], fungusMat, at + rot * new Vector3(0.6f, 0.7f, 0.55f), Vector3.one, 0);
                        break;
                    case Plant.SurfaceRoot:
                        Put(roots[v % 3], rootMat, at, Vector3.one * (0.8f + size * 0.6f), size > 0.5f ? 2 : 1);
                        break;
                    case Plant.DarkRoot:
                        Put(roots[v % 3], darkRootMat, at, Vector3.one * (1f + size * 0.8f), 2);
                        break;
                    case Plant.LimestoneRock:
                    {
                        var r = rocks[v % 4];
                        var scale = new Vector3(0.7f + size * 2f, 0.5f + size * 1.4f, 0.7f + size * 1.8f);
                        int lod = size > 0.45f ? 2 : 1;
                        Put(r.rock, stone, at - Vector3.up * 0.1f, scale, lod);
                        Put(r.moss, stoneMoss, at - Vector3.up * 0.1f, scale, lod);
                        break;
                    }
                    default:
                        return false;
                }
                return true;
            }
        }
    }
}
