using System.Collections.Generic;
using Evolve.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Evolve.EditorTools
{
    /// <summary>
    /// Dresses the ground around the lake. Menu: Evolve -> Dress Lake: Ground.
    /// Moss patches and grass along the shores and scattered over the flat ground, rocks with a moss cap, and winding
    /// mud paths between the pool and the other areas. Everything is visual only (no colliders), made of a few combined
    /// meshes under one object, and it is safe to run again (it replaces its own work). It reads the ponds from the scene
    /// (WaterSurface) and the ground height by ray, so it keeps working when the terrain changes.
    /// Run "Dress Lake: Water" first.
    /// </summary>
    public static class LakeGround
    {
        const string Root = "Assets/_Project";
        const string DressedName = "Lake Ground (Dressed)";

        [MenuItem("Evolve/Dress Lake: Ground")]
        public static void Dress()
        {
            PrototypeSceneBuilder.EnsureFolder(Root + "/Materials");
            PrototypeSceneBuilder.EnsureFolder(Root + "/Models");
            WaterSurface.Refresh();                       // so the ponds' shores are current
            Physics.SyncTransforms();

            var old = GameObject.Find(DressedName);
            if (old != null) Object.DestroyImmediate(old);
            var root = new GameObject(DressedName).transform;
            root.gameObject.AddComponent<DetailCulling>();   // only what is near the camera is drawn

            var rnd = new System.Random(2024);
            var ponds = new List<WaterSurface>(WaterSurface.All);

            // ---- moss patches ----
            var mossInstances = new List<CombineInstance>();
            int seed = 100;
            foreach (var pond in ponds)
            {
                var outline = pond.WorldOutline;
                for (int k = 0; k < outline.Length; k += 10)
                {
                    Vector2 p = outline[k] + Outward(outline, k) * (1.5f + (float)rnd.NextDouble() * 3f);
                    if (Wet(ponds, p)) continue;
                    AddMoss(mossInstances, p, 0.9f + (float)rnd.NextDouble() * 1.2f, ++seed, ponds);
                }
            }
            foreach (var p in LakeLayout.Scatter(9, rnd))    // over the core and every biome pad of the layout
            {
                if (Wet(ponds, p)) continue;
                AddMoss(mossInstances, p, 1.6f + (float)rnd.NextDouble() * 1.5f, ++seed, ponds);
            }
            AddCombined(root, "Moss", mossInstances, MossMaterial());

            // The humus bed's flat dark rectangle becomes a soft, uneven patch of dark mud (no straight edges).
            var humusBed = GameObject.Find("Humus Ground");
            if (humusBed != null)
            {
                var flat = humusBed.GetComponent<Renderer>();
                if (flat != null) flat.enabled = false;
                Vector3 c = humusBed.transform.position, extent = humusBed.transform.lossyScale;
                var outline = WaterOutline.Make(extent.x, extent.z, false, 5150, 2.2f, 64);
                var patch = WaterMeshes.Organic(outline, 2f);
                var parts = new List<CombineInstance>
                {
                    new CombineInstance { mesh = patch, transform = Matrix4x4.TRS(new Vector3(c.x, GroundY(c.x, c.z) + 0.025f, c.z), Quaternion.identity, new Vector3(1.25f, 1f, 1.25f)) }
                };
                AddCombined(root, "Humus Mud", parts, MudPatchMaterial());
            }

            // ---- rocks (with moss caps) ----
            var rocks = new List<Mesh>();
            var caps = new List<Mesh>();
            for (int v = 0; v < 6; v++) { rocks.Add(GroundMeshes.Rock(500 + v, out var cap)); caps.Add(cap); }
            var rockInstances = new List<CombineInstance>();
            var capInstances = new List<CombineInstance>();
            void PlaceRock(Vector2 p, float size)
            {
                if (Wet(ponds, p)) return;
                int variant = rnd.Next(rocks.Count);
                var rot = Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f);
                var scale = new Vector3(size * (0.8f + (float)rnd.NextDouble() * 0.5f), size * (0.7f + (float)rnd.NextDouble() * 0.6f), size * (0.8f + (float)rnd.NextDouble() * 0.5f));
                var m = Matrix4x4.TRS(new Vector3(p.x, GroundY(p.x, p.y), p.y), rot, scale);
                rockInstances.Add(new CombineInstance { mesh = rocks[variant], transform = m });
                capInstances.Add(new CombineInstance { mesh = caps[variant], transform = m });
            }
            foreach (var pond in ponds)
            {
                var outline = pond.WorldOutline;
                for (int k = 0; k < outline.Length; k += 5)
                {
                    if (rnd.NextDouble() > 0.5) continue;
                    Vector2 p = outline[k] + Outward(outline, k) * (0.6f + (float)rnd.NextDouble() * 2.4f);
                    PlaceRock(p, 0.35f + (float)rnd.NextDouble() * 0.75f);
                    if (rnd.NextDouble() < 0.4) PlaceRock(p + new Vector2((float)rnd.NextDouble() - 0.5f, (float)rnd.NextDouble() - 0.5f) * 1.6f, 0.2f + (float)rnd.NextDouble() * 0.35f);
                }
            }
            foreach (var p in LakeLayout.Scatter(26, rnd))
                PlaceRock(p, 0.4f + (float)rnd.NextDouble() * 1.1f);
            AddCombined(root, "Rocks", rockInstances, OpaqueMaterial("Lake_Rock", new Color(0.40f, 0.41f, 0.39f), 0.15f, false));
            AddCombined(root, "Rock Moss", capInstances, OpaqueMaterial("Lake_RockMoss", new Color(0.30f, 0.50f, 0.22f), 0.1f, false));

            // ---- grass tufts (three greens) ----
            var tufts = new List<Vector3>[] { new List<Vector3>(), new List<Vector3>(), new List<Vector3>() };
            void PlaceTuft(Vector2 p)
            {
                if (Wet(ponds, p)) return;
                tufts[rnd.Next(3)].Add(new Vector3(p.x, GroundY(p.x, p.y) - 0.02f, p.y));
            }
            foreach (var pond in ponds)
            {
                var outline = pond.WorldOutline;
                for (int k = 0; k < outline.Length; k++)
                    for (int j = 0; j < 2; j++)
                        PlaceTuft(outline[k] + Outward(outline, k) * (0.9f + (float)rnd.NextDouble() * 4.5f) + RandomOffset(rnd, 0.8f));
            }
            foreach (var p in LakeLayout.Scatter(260, rnd))
                PlaceTuft(p);
            var grassColors = new[] { new Color(0.32f, 0.58f, 0.22f), new Color(0.42f, 0.68f, 0.26f), new Color(0.26f, 0.48f, 0.24f) };
            for (int g = 0; g < 3; g++)
            {
                var buckets = new Dictionary<(int, int), List<Vector3>>();
                foreach (var t in tufts[g])
                {
                    var key = Cell(t);
                    if (!buckets.TryGetValue(key, out var list)) buckets[key] = list = new List<Vector3>();
                    list.Add(t);
                }
                var holder = new GameObject("Grass " + g).transform;
                holder.SetParent(root, false);
                var grassMaterial = OpaqueMaterial("Lake_Grass" + g, grassColors[g], 0.1f, true);
                foreach (var bucket in buckets)
                    AddMesh(holder, $"Grass {g} {bucket.Key.Item1},{bucket.Key.Item2}", GroundMeshes.Tufts(bucket.Value, 900 + g, 0.45f), grassMaterial);
            }

            // ---- mud paths from the pool to the other areas ----
            var ribbons = new List<CombineInstance>();
            int pathSeed = 7;
            foreach (var trail in TrailLines())
            {
                Vector2 a = trail[0], b = trail[trail.Length - 1];
                var line = GroundMeshes.WindingLine(a, b, ++pathSeed, Mathf.Min(3.5f, Vector2.Distance(a, b) * 0.06f), 1.5f);
                var run = new List<Vector2>();
                void Flush()
                {
                    if (run.Count >= 3)
                        ribbons.Add(new CombineInstance { mesh = GroundMeshes.Ribbon(run, 0.9f, ++pathSeed, GroundY, 0.015f), transform = Matrix4x4.identity });
                    run = new List<Vector2>();
                }
                foreach (var p in line)
                {
                    if (Wet(ponds, p)) Flush(); else run.Add(p);
                }
                Flush();
            }
            AddCombined(root, "Mud Paths", ribbons, PathMaterial());

            EditorSceneManager.MarkAllScenesDirty();
            AssetDatabase.SaveAssets();
            Debug.Log("Đã trang trí mặt đất (rêu, đá, cỏ, đường mòn). Bấm Ctrl+S để lưu scene. Chạy lại menu này sẽ làm lại từ đầu.");
        }

        /// <summary>
        /// The trails to draw, as (start, end) points on the ground. They follow the zone layout when the project has one
        /// (World/ZoneLayout.Paths, read by name so this tool compiles with or without it). Without it, trails run from the
        /// pool to the biomes found in the scene by name (by their objects' position), or to the old default spots.
        /// </summary>
        static List<Vector2[]> TrailLines()
        {
            var lines = new List<Vector2[]>();
            var layout = System.Type.GetType("Evolve.World.ZoneLayout, Evolve");
            var field = layout != null ? layout.GetField("Paths") : null;
            if (field != null && field.GetValue(null) is Vector2[][] paths && paths.Length > 0)
            {
                foreach (var path in paths) if (path != null && path.Length >= 2) lines.Add(path);
                if (lines.Count > 0) return lines;
            }

            // Fallback: from the edge of the pool to each biome found by its object's name.
            var anchors = new[]
            {
                Anchor("Bai Mun Re (Humus Bed)", new Vector2(40f, 0f)),
                Anchor("Chan Re Co Thu (Ancient Roots)", new Vector2(0f, 50f)),
                Anchor("Vuon Bao Tu (Spore Garden)", new Vector2(-40f, 0f)),
            };
            foreach (var target in anchors)
            {
                Vector2 dir = target.normalized;
                lines.Add(new[] { dir * 17f, target - dir * 8f });
            }
            return lines;
        }

        // The middle of a named object's renderers; a default if the object is not in the scene.
        static Vector2 Anchor(string objectName, Vector2 fallback)
        {
            var go = GameObject.Find(objectName);
            if (go == null) return fallback;
            Bounds b = default; bool any = false;
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            { if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds); }
            return any ? new Vector2(b.center.x, b.center.z) : fallback;
        }

        // ---------------------------------------------------------------- placing helpers

        /// <summary>Under water: on a pond, or in a hollow of the ground lower than the water level. Nothing is placed there.</summary>
        internal static bool Wet(List<WaterSurface> ponds, Vector2 p) => OnWater(ponds, p) || GroundY(p.x, p.y) < -0.04f;

        internal static bool OnWater(List<WaterSurface> ponds, Vector2 p)
        {
            foreach (var pond in ponds) if (pond != null && pond.ContainsWorld(p)) return true;
            return false;
        }

        static Vector2 RandomOffset(System.Random rnd, float size) =>
            new Vector2((float)rnd.NextDouble() - 0.5f, (float)rnd.NextDouble() - 0.5f) * size * 2f;

        // The direction pointing away from the pond at outline point k.
        internal static Vector2 Outward(Vector2[] outline, int k)
        {
            int n = outline.Length;
            Vector2 t = (outline[(k + 1) % n] - outline[(k - 1 + n) % n]).normalized;
            Vector2 normal = new Vector2(t.y, -t.x);
            Vector2 centre = Vector2.zero;
            foreach (var p in outline) centre += p;
            centre /= n;
            if (Vector2.Dot(normal, outline[k] - centre) < 0f) normal = -normal;
            return normal;
        }

        /// <summary>Height of the ground at (x, z): the terrain or the ground plane, found by ray (0 if none).</summary>
        internal static float GroundY(float x, float z)
        {
            foreach (var hit in Physics.RaycastAll(new Vector3(x, 200f, z), Vector3.down, 400f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                if (hit.collider is TerrainCollider || hit.collider.gameObject.name.StartsWith("Ground"))
                    return hit.point.y;
            return 0f;
        }

        static void AddMoss(List<CombineInstance> list, Vector2 p, float radius, int seed, List<WaterSurface> ponds)
        {
            var outline = WaterOutline.Make(radius * 2f, radius * 2f, true, seed, radius * 0.35f, 28);
            var mesh = WaterMeshes.Organic(outline, 1.2f);
            // Moss never grows over water: drop the part of the patch that lies on a pond.
            WaterMeshes.RemoveTriangles(mesh, c => OnWater(ponds, new Vector2(p.x + c.x, p.y + c.z)));
            list.Add(new CombineInstance { mesh = mesh, transform = Matrix4x4.TRS(new Vector3(p.x, GroundY(p.x, p.y) + 0.03f, p.y), Quaternion.identity, Vector3.one) });
        }

        /// <summary>Size of one decoration chunk (m). Each chunk is its own mesh, so the camera can skip what it does not see.</summary>
        internal const float ChunkSize = 64f;

        internal static (int, int) Cell(Vector3 p) => (Mathf.FloorToInt(p.x / ChunkSize), Mathf.FloorToInt(p.z / ChunkSize));

        /// <summary>Combines the parts into one mesh PER CHUNK (a square of ChunkSize metres), under a holder named `name`.</summary>
        internal static void AddCombined(Transform parent, string name, List<CombineInstance> parts, Material material)
        {
            if (parts.Count == 0) return;
            var groups = new Dictionary<(int, int), List<CombineInstance>>();
            foreach (var part in parts)
            {
                var key = Cell(part.transform.MultiplyPoint3x4(part.mesh.bounds.center));
                if (!groups.TryGetValue(key, out var list)) groups[key] = list = new List<CombineInstance>();
                list.Add(part);
            }
            var holder = new GameObject(name).transform;
            holder.SetParent(parent, false);
            foreach (var group in groups)
            {
                var mesh = new Mesh { name = name };
                int total = 0;
                foreach (var part in group.Value) total += part.mesh.vertexCount;
                if (total > 65000) mesh.indexFormat = IndexFormat.UInt32;
                mesh.CombineMeshes(group.Value.ToArray(), true, true);
                mesh.RecalculateBounds();
                AddMesh(holder, $"{name} {group.Key.Item1},{group.Key.Item2}", mesh, material);
            }
        }

        internal static void AddMesh(Transform parent, string name, Mesh mesh, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = material;
            mr.shadowCastingMode = ShadowCastingMode.Off; // decoration casts no shadows: cheaper, and the cave is dark anyway
            go.isStatic = true;
        }

        // ---------------------------------------------------------------- materials

        static Material MossMaterial()
        {
            var tex = LakeWater.PaintedTexture(Root + "/Models/MossPatch.png", 64, 64, (u, v) =>
            {
                float n = LakeWater.TileableNoise(u * 8f, v * 8f, 8, 8);
                var colour = Color.Lerp(new Color(0.16f, 0.34f, 0.12f), new Color(0.46f, 0.72f, 0.26f), n);
                float d = new Vector2(u - 0.5f, v - 0.5f).magnitude * 2f;      // 0 centre .. 1 edge
                colour.a = Mathf.SmoothStep(1f, 0.45f, d) * (0.75f + 0.25f * n);
                return colour;
            });
            return LakeWater.TranslucentMaterial("Lake_Moss", tex, Color.white, Color.black, 0.05f, (int)RenderQueue.Transparent - 20);
        }

        static Material MudPatchMaterial()
        {
            var tex = LakeWater.PaintedTexture(Root + "/Models/MudPatch.png", 64, 64, (u, v) =>
            {
                float n = LakeWater.TileableNoise(u * 7f, v * 7f, 7, 7);
                var colour = Color.Lerp(new Color(0.20f, 0.14f, 0.10f), new Color(0.38f, 0.28f, 0.20f), n);
                float d = new Vector2(u - 0.5f, v - 0.5f).magnitude * 2f;
                colour.a = Mathf.SmoothStep(1f, 0.55f, d) * (0.8f + 0.2f * n);
                return colour;
            });
            return LakeWater.TranslucentMaterial("Lake_MudPatch", tex, Color.white, Color.black, 0.05f, (int)RenderQueue.Transparent - 19);
        }

        static Material PathMaterial()
        {
            var tex = LakeWater.PaintedTexture(Root + "/Models/MudPath.png", 128, 32, (u, v) =>
            {
                float across = Mathf.Pow(Mathf.Sin(Mathf.Clamp01(v) * Mathf.PI), 0.7f);   // soft on both edges
                float mottled = 0.7f + 0.3f * LakeWater.TileableNoise(u * 9f, v * 3f, 9, 3);
                return new Color(1f, 1f, 1f, across * mottled);
            });
            return LakeWater.TranslucentMaterial("Lake_MudPath", tex, new Color(0.24f, 0.17f, 0.12f, 0.95f), Color.black, 0.05f, (int)RenderQueue.Transparent - 18);
        }

        internal static Material OpaqueMaterial(string name, Color color, float smoothness, bool doubleSided, Color emission = default)
        {
            string path = $"{Root}/Materials/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_Cull", doubleSided ? 0f : 2f);
            if (emission.maxColorComponent > 0.001f) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", emission); }
            else mat.DisableKeyword("_EMISSION");
            EditorUtility.SetDirty(mat);
            return mat;
        }
    }
}
