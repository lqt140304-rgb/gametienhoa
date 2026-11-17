using System.Collections.Generic;
using Evolve.Visuals;
using Evolve.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Evolve.EditorTools
{
    /// <summary>
    /// Dresses the lake with plants. Menu: Evolve -> Dress Lake: Mushrooms and Vines.
    /// Giant mushrooms (lilac, coral and teal caps with glowing spots, a few of them lit) in clusters in the spore
    /// garden, along the shores and scattered; and vines hanging from the ceiling roots with leaves along them.
    /// Visual only (no colliders), combined into a few meshes under one object, safe to run again.
    /// Run "Dress Lake: Water" first (the shores are read from the ponds).
    /// </summary>
    public static class LakeFlora
    {
        const string DressedName = "Lake Flora (Dressed)";

        [MenuItem("Evolve/Dress Lake: Mushrooms and Vines")]
        public static void Dress()
        {
            PrototypeSceneBuilder.EnsureFolder("Assets/_Project/Materials");
            WaterSurface.Refresh();
            Physics.SyncTransforms();

            var old = GameObject.Find(DressedName);
            if (old != null) Object.DestroyImmediate(old);
            var root = new GameObject(DressedName).transform;
            var rnd = new System.Random(77);
            var ponds = new List<WaterSurface>(WaterSurface.All);

            // ---------------------------------------------------------------- mushrooms
            var stalks = new List<CombineInstance>();
            var caps = new[] { new List<CombineInstance>(), new List<CombineInstance>(), new List<CombineInstance>() };
            var spots = new List<CombineInstance>();
            var blob = FloraMeshes.Blob();
            int seed = 300, lights = 0;

            void Mushroom(Vector2 p, float height, int colour)
            {
                if (LakeGround.OnWater(ponds, p)) return;
                FloraMeshes.Mushroom(++seed, height, out var stalk, out var cap, out float lean);
                var rot = Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f);
                var pos = new Vector3(p.x, LakeGround.GroundY(p.x, p.y) - 0.04f, p.y);
                var m = Matrix4x4.TRS(pos, rot, Vector3.one);
                stalks.Add(new CombineInstance { mesh = stalk, transform = m });
                caps[colour].Add(new CombineInstance { mesh = cap, transform = m });

                // glowing spots on top of the cap
                float capR = cap.bounds.extents.x, baseY = height * 0.76f, capH = cap.bounds.max.y - baseY;
                int count = height >= 0.9f ? 5 + rnd.Next(5) : 0;
                for (int i = 0; i < count; i++)
                {
                    float a = (float)rnd.NextDouble() * Mathf.PI * 2f, r = capR * (0.12f + (float)rnd.NextDouble() * 0.6f);
                    float y = FloraMeshes.CapTopHeight(height, capR, capH, r) + 0.01f * height;
                    var local = new Vector3(Mathf.Cos(a) * r + FloraMeshes.LeanShift(lean, y, height), y, Mathf.Sin(a) * r);
                    float size = capR * (0.1f + (float)rnd.NextDouble() * 0.1f);
                    spots.Add(new CombineInstance { mesh = blob, transform = Matrix4x4.TRS(pos + rot * local, Quaternion.identity, new Vector3(size, size * 0.45f, size)) });
                }

                // a few of the tall ones give light, so the cave is not pitch dark around them
                if (height >= 3f && lights < 6)
                {
                    lights++;
                    var go = new GameObject("Mushroom Glow");
                    go.transform.SetParent(root, false);
                    go.transform.position = pos + rot * new Vector3(FloraMeshes.LeanShift(lean, baseY, height), baseY + capH * 0.5f, 0f);
                    var l = go.AddComponent<Light>();
                    l.type = LightType.Point;
                    l.range = 12f;
                    l.intensity = 1.3f;
                    l.shadows = LightShadows.None;
                    l.color = colour == 0 ? new Color(0.75f, 0.55f, 1f) : colour == 1 ? new Color(1f, 0.6f, 0.5f) : new Color(0.4f, 1f, 0.9f);
                }
            }

            // 1) the spore garden: a big cluster, lilac mostly
            Vector2 garden = new Vector2(-40f, 0f);
            var gardenRoot = GameObject.Find("Vuon Bao Tu (Spore Garden)");
            if (gardenRoot != null)
            {
                Bounds b = default; bool any = false;
                foreach (var r in gardenRoot.GetComponentsInChildren<Renderer>())
                { if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds); }
                if (any) garden = new Vector2(b.center.x, b.center.z);
            }
            for (int i = 0; i < 16; i++)
            {
                float a = (float)rnd.NextDouble() * Mathf.PI * 2f, d = 3f + (float)rnd.NextDouble() * 12f;
                Mushroom(garden + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * d, i < 3 ? 3.5f + (float)rnd.NextDouble() * 2f : 0.6f + (float)rnd.NextDouble() * 1.9f,
                    rnd.NextDouble() < 0.7 ? 0 : rnd.Next(3));
            }

            // 2) along the shores
            foreach (var pond in ponds)
            {
                var outline = pond.WorldOutline;
                for (int k = 0; k < outline.Length; k += 7)
                {
                    if (rnd.NextDouble() > 0.65) continue;
                    Mushroom(outline[k] + LakeGround.Outward(outline, k) * (1.3f + (float)rnd.NextDouble() * 4.5f),
                        0.5f + (float)rnd.NextDouble() * 1.4f, rnd.Next(3));
                }
            }

            // 3) scattered over the flat ground
            foreach (var p in LakeLayout.Scatter(18, rnd))   // over the core and every biome pad of the layout
                Mushroom(p, 0.5f + (float)rnd.NextDouble() * 1.8f, rnd.Next(3));

            var capColours = new[] { new Color(0.62f, 0.42f, 0.82f), new Color(0.92f, 0.46f, 0.44f), new Color(0.28f, 0.72f, 0.68f) };
            LakeGround.AddCombined(root, "Mushroom Stalks", stalks, LakeGround.OpaqueMaterial("Lake_MushroomStalk", new Color(0.88f, 0.85f, 0.75f), 0.3f, false));
            for (int c = 0; c < 3; c++)
                LakeGround.AddCombined(root, "Mushroom Caps " + c, caps[c], LakeGround.OpaqueMaterial("Lake_MushroomCap" + c, capColours[c], 0.45f, false, capColours[c] * 0.10f));
            LakeGround.AddCombined(root, "Mushroom Spots", spots, LakeGround.OpaqueMaterial("Lake_MushroomSpot", new Color(0.95f, 0.98f, 0.9f), 0.2f, false, new Color(0.7f, 0.85f, 0.7f)));

            // ---------------------------------------------------------------- hanging vines
            // Vines hang from the lower end of the hanging roots of the scene (or the old ceiling roots).
            var tops = LakeLayout.HangingPoints(rnd);

            var vines = new List<CombineInstance>();
            var leaves = new List<CombineInstance>();
            var leafA = FloraMeshes.Leaf(0.45f, 0.22f);
            var leafB = FloraMeshes.Leaf(0.3f, 0.16f);
            foreach (var top in tops)
            {
                int count = 3 + rnd.Next(3);
                for (int v = 0; v < count; v++)
                {
                    var start = top + new Vector3(((float)rnd.NextDouble() - 0.5f) * 5f, -0.5f, ((float)rnd.NextDouble() - 0.5f) * 5f);
                    float bottom = 1.5f + (float)rnd.NextDouble() * 6f;
                    float length = Mathf.Clamp(start.y - bottom, 3f, 24f); // long vines stop short of the ground in a tall cave
                    var path = FloraMeshes.VinePath(start, length, ++seed, 1.1f, 14);
                    var radii = new float[path.Length];
                    for (int i = 0; i < radii.Length; i++) radii[i] = Mathf.Lerp(0.07f, 0.025f, i / (float)(radii.Length - 1));
                    vines.Add(new CombineInstance { mesh = SlimeMeshBuilder.Tube(path, radii, 6, "Vine"), transform = Matrix4x4.identity });

                    // leaves hang along the vine
                    for (int i = 2; i < path.Length; i++)
                    {
                        if (rnd.NextDouble() < 0.35) continue;
                        var rot = Quaternion.Euler(140f + (float)rnd.NextDouble() * 40f, (float)rnd.NextDouble() * 360f, 0f);
                        leaves.Add(new CombineInstance { mesh = rnd.NextDouble() < 0.5 ? leafA : leafB, transform = Matrix4x4.TRS(path[i], rot, Vector3.one) });
                    }
                }
            }
            LakeGround.AddCombined(root, "Vines", vines, LakeGround.OpaqueMaterial("Lake_Vine", new Color(0.20f, 0.36f, 0.17f), 0.15f, false));
            LakeGround.AddCombined(root, "Vine Leaves", leaves, LakeGround.OpaqueMaterial("Lake_VineLeaf", new Color(0.36f, 0.62f, 0.24f), 0.15f, true));

            EditorSceneManager.MarkAllScenesDirty();
            AssetDatabase.SaveAssets();
            Debug.Log($"Đã trồng nấm khổng lồ ({stalks.Count} cây, {lights} cây có đèn) và dây leo ({vines.Count} sợi). Bấm Ctrl+S để lưu scene. Chạy lại menu này sẽ làm lại từ đầu.");
        }
    }
}
