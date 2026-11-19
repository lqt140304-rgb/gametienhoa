using System.Collections.Generic;
using Evolve.Survival;
using Evolve.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Evolve.EditorTools
{
    /// <summary>
    /// Evolve → Dress Cavern: Forage. Puts the things a slime can absorb into the Slime Cavern (spec 2026-11-19-slime-cavern-design): eight plants and mushrooms
    /// and four minerals, each its own <see cref="FoodKind"/> with its own nutrient recipe, each in its own habitat (CavernForageData). Most plants of the cavern
    /// (the grass carpet, plain rocks, the teal mushrooms) are only scenery and cannot be eaten. They are ordinary Nutrient objects (trigger sphere and model),
    /// so absorbing, regrowth and co-op sync work as for any food. Rerunnable: the old "Cavern Forage" is replaced. Part of Build Slime Cavern.
    /// </summary>
    public static class CavernForage
    {
        const string RootName = "Cavern Forage";

        [MenuItem("Evolve/Dress Cavern: Forage")]
        public static void Dress()
        {
            var old = GameObject.Find(RootName);
            if (old != null) Object.DestroyImmediate(old);
            var root = new GameObject(RootName).transform;

            var rnd = new System.Random(5151);
            var meshes = new Dictionary<string, (Mesh mesh, Material[] mats)[]>();
            foreach (var spec in CavernForageData.Specs) meshes[spec.Id] = Variants(spec, rnd);

            var all = CavernForageData.PlaceAll();
            var perKind = new Dictionary<string, int>();
            foreach (var (pos, spec) in all)
            {
                var variant = meshes[spec.Id][rnd.Next(meshes[spec.Id].Length)];
                var go = new GameObject(spec.Name);
                go.transform.SetParent(root, false);
                go.transform.position = pos;
                go.transform.rotation = Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f);
                go.transform.localScale = Vector3.one * Mathf.Lerp(0.85f, 1.25f, (float)rnd.NextDouble());
                var sc = go.AddComponent<SphereCollider>();
                sc.isTrigger = true;
                sc.radius = spec.Mineral ? 0.9f : 0.7f;
                sc.center = new Vector3(0f, 0.35f, 0f);
                go.AddComponent<Nutrient>();
                var so = new SerializedObject(go.GetComponent<Nutrient>());
                so.FindProperty("foodKind").enumValueIndex = (int)spec.Food;
                so.FindProperty("totalNutrition").floatValue = spec.Nutrition;
                so.FindProperty("absorbRate").floatValue = spec.AbsorbRate;
                so.FindProperty("respawnSeconds").floatValue = spec.Respawn;
                so.ApplyModifiedPropertiesWithoutUndo();

                var visual = new GameObject("Visual");
                visual.transform.SetParent(go.transform, false);
                if (spec.Mineral) visual.transform.localPosition = new Vector3(0f, 0.35f, 0f);   // crystals stand on top of the rock under them
                visual.AddComponent<MeshFilter>().sharedMesh = variant.mesh;
                var mr = visual.AddComponent<MeshRenderer>();
                mr.sharedMaterials = variant.mats;
                mr.shadowCastingMode = ShadowCastingMode.Off;
                perKind[spec.Id] = perKind.TryGetValue(spec.Id, out int c) ? c + 1 : 1;
            }
            var summary = new List<string>();
            foreach (var kv in perKind) summary.Add($"{kv.Key} {kv.Value}");
            EditorSceneManager.MarkAllScenesDirty();
            AssetDatabase.SaveAssets();
            Debug.Log($"Thức ăn trong hang: {all.Count} cái ({string.Join(", ", summary)}). Ctrl+S để lưu scene.");
        }

        // ---------------------------------------------------------------- the looks of each kind

        static Material M(string name, Color color, Color emission = default, float smooth = 0.1f, bool twoSided = true) =>
            LakeGround.OpaqueMaterial("Cavern_" + name, color, smooth, twoSided, emission);

        static (Mesh, Material[])[] Variants(ForageSpec spec, System.Random rnd)
        {
            var list = new List<(Mesh, Material[])>();
            for (int v = 0; v < 3; v++) list.Add(Build(spec.Id, 100 * v + 7, rnd));
            return list.ToArray();
        }

        static (Mesh, Material[]) Build(string id, int seed, System.Random rnd)
        {
            var one = new List<Vector3> { Vector3.zero };
            switch (id)
            {
                case "reed":   // tall tan-green blades with brown seed heads
                {
                    var blades = GroundMeshes.Tufts(one, seed, 1.3f);
                    var heads = new List<CombineInstance>();
                    for (int i = 0; i < 4; i++)
                    {
                        float a = i * 1.57f + (float)rnd.NextDouble();
                        heads.Add(Part(FloraMeshes.Blob(), new Vector3(Mathf.Cos(a) * 0.12f, 1.15f + (float)rnd.NextDouble() * 0.2f, Mathf.Sin(a) * 0.12f), new Vector3(0.07f, 0.2f, 0.07f)));
                    }
                    return (Pair(blades, Merge(heads)), new[] { M("Reed", new Color(0.46f, 0.52f, 0.22f)), M("ReedHead", new Color(0.32f, 0.19f, 0.1f), default, 0.1f, false) });
                }
                case "lily": return (Lily(rnd), new[] { M("LilyPad", new Color(0.16f, 0.42f, 0.2f), default, 0.3f), M("LilyFlower", new Color(0.95f, 0.78f, 0.85f), new Color(0.25f, 0.12f, 0.16f), 0.3f) });
                case "moss":   // low glowing clumps
                {
                    var parts = new List<CombineInstance>();
                    for (int i = 0; i < 5; i++)
                        parts.Add(Part(FloraMeshes.Blob(), new Vector3(((float)rnd.NextDouble() - 0.5f) * 0.55f, 0.04f, ((float)rnd.NextDouble() - 0.5f) * 0.55f), new Vector3(0.2f + 0.12f * (float)rnd.NextDouble(), 0.09f, 0.2f + 0.1f * (float)rnd.NextDouble())));
                    return (Merge(parts), new[] { M("GlowMoss", new Color(0.3f, 0.7f, 0.32f), new Color(0.22f, 0.8f, 0.3f), 0.05f, false) });
                }
                case "sweet": return (GroundMeshes.Tufts(one, seed, 0.75f), new[] { M("SweetGrass", new Color(0.62f, 0.66f, 0.24f), new Color(0.07f, 0.07f, 0.0f)) });
                case "fern": return (GroundMeshes.Tufts(one, seed + 3, 0.95f), new[] { M("BitterFern", new Color(0.1f, 0.34f, 0.28f)) });
                case "berry":  // a green bush with glowing orange berries
                {
                    var leaves = new List<CombineInstance>();
                    var berries = new List<CombineInstance>();
                    for (int i = 0; i < 5; i++)
                    {
                        float a = i * 1.26f + (float)rnd.NextDouble();
                        leaves.Add(Part(FloraMeshes.Blob(), new Vector3(Mathf.Cos(a) * 0.22f, 0.3f, Mathf.Sin(a) * 0.22f), new Vector3(0.3f, 0.26f, 0.3f)));
                        berries.Add(Part(FloraMeshes.Blob(), new Vector3(Mathf.Cos(a + 0.5f) * 0.36f, 0.34f + 0.1f * (float)rnd.NextDouble(), Mathf.Sin(a + 0.5f) * 0.36f), new Vector3(0.07f, 0.07f, 0.07f)));
                    }
                    return (Pair(Merge(leaves), Merge(berries)), new[] { M("EmberBush", new Color(0.16f, 0.3f, 0.12f), default, 0.05f, false), M("EmberBerry", new Color(1f, 0.45f, 0.12f), new Color(0.9f, 0.35f, 0.05f), 0.4f, false) });
                }
                case "fatcap":
                {
                    FloraMeshes.Mushroom(seed, 0.6f, out var stalk, out var cap, out _);
                    var st = new List<CombineInstance> { Part(stalk, Vector3.zero, Vector3.one * 1.5f) };
                    var cp = new List<CombineInstance> { Part(cap, Vector3.zero, Vector3.one * 1.5f) };
                    return (Pair(Merge(st), Merge(cp)), new[] { M("FatStalk", new Color(0.9f, 0.86f, 0.74f), default, 0.1f, false), M("FatCap", new Color(0.78f, 0.6f, 0.42f), default, 0.15f, false) });
                }
                case "pod":    // teal glowing pods
                {
                    var parts = new List<CombineInstance>();
                    for (int i = 0; i < 3; i++)
                        parts.Add(Part(FloraMeshes.Blob(), new Vector3(((float)rnd.NextDouble() - 0.5f) * 0.4f, 0.25f, ((float)rnd.NextDouble() - 0.5f) * 0.4f), new Vector3(0.16f, 0.3f + 0.15f * (float)rnd.NextDouble(), 0.16f)));
                    return (Merge(parts), new[] { M("SporePod", new Color(0.2f, 0.65f, 0.7f), new Color(0.1f, 0.5f, 0.55f), 0.3f, false) });
                }
                case "quartz": return (CavernBuilder.OreCluster(rnd, 1f, 1f, 6), new[] { M("Ore", new Color(0.25f, 0.75f, 0.95f), new Color(0.25f, 1.1f, 1.4f), 0.55f) });
                case "lime": return (CavernBuilder.OreCluster(rnd, 0.55f, 1.7f, 5), new[] { M("LimeCrystal", new Color(0.92f, 0.9f, 0.76f), default, 0.35f) });
                case "salt": return (CavernBuilder.OreCluster(rnd, 0.4f, 1.3f, 9), new[] { M("SaltCrust", new Color(0.93f, 0.76f, 0.76f), new Color(0.05f, 0.02f, 0.02f), 0.3f) });
                default:       // iron: a rusty nodule
                {
                    var rock = GroundMeshes.Rock(seed, out _);
                    return (Merge(new List<CombineInstance> { Part(rock, new Vector3(0f, 0.05f, 0f), new Vector3(0.7f, 0.55f, 0.7f)) }), new[] { M("IronNodule", new Color(0.5f, 0.24f, 0.1f), default, 0.2f, false) });
                }
            }
        }

        static CombineInstance Part(Mesh mesh, Vector3 pos, Vector3 scale) =>
            new CombineInstance { mesh = mesh, transform = Matrix4x4.TRS(pos, Quaternion.identity, scale) };

        static Mesh Merge(List<CombineInstance> parts)
        {
            var mesh = new Mesh();
            mesh.CombineMeshes(parts.ToArray(), true, true);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Two meshes as two sub-meshes of one (so two materials).</summary>
        static Mesh Pair(Mesh a, Mesh b)
        {
            var mesh = new Mesh();
            mesh.CombineMeshes(new[] { new CombineInstance { mesh = a, transform = Matrix4x4.identity }, new CombineInstance { mesh = b, transform = Matrix4x4.identity } }, false, false);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>A floating pad (fan of 14 sides) with a six-petal flower in the middle: two sub-meshes, pad and flower.</summary>
        static Mesh Lily(System.Random rnd)
        {
            const int sides = 14;
            float r = 0.34f + 0.1f * (float)rnd.NextDouble();
            var pv = new List<Vector3> { new Vector3(0f, 0.02f, 0f) };
            for (int i = 0; i < sides; i++) { float a = i / (float)sides * Mathf.PI * 2f; pv.Add(new Vector3(Mathf.Cos(a) * r, 0.02f, Mathf.Sin(a) * r)); }
            var pt = new List<int>();
            for (int i = 0; i < sides; i++) { pt.Add(0); pt.Add(1 + (i + 1) % sides); pt.Add(1 + i); }
            var pad = new Mesh();
            pad.SetVertices(pv); pad.SetTriangles(pt, 0); pad.RecalculateNormals();

            var fv = new List<Vector3>();
            var ft = new List<int>();
            for (int i = 0; i < 6; i++)
            {
                float a0 = i / 6f * Mathf.PI * 2f, a1 = (i + 1) / 6f * Mathf.PI * 2f, am = (a0 + a1) * 0.5f;
                int b = fv.Count;
                fv.Add(new Vector3(Mathf.Cos(a0) * 0.05f, 0.04f, Mathf.Sin(a0) * 0.05f));
                fv.Add(new Vector3(Mathf.Cos(a1) * 0.05f, 0.04f, Mathf.Sin(a1) * 0.05f));
                fv.Add(new Vector3(Mathf.Cos(am) * 0.12f, 0.2f, Mathf.Sin(am) * 0.12f));
                ft.Add(b); ft.Add(b + 2); ft.Add(b + 1);
            }
            var flower = new Mesh();
            flower.SetVertices(fv); flower.SetTriangles(ft, 0); flower.RecalculateNormals();
            return Pair(pad, flower);
        }
    }
}
