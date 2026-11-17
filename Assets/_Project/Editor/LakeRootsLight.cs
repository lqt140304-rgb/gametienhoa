using System.Collections.Generic;
using Evolve.Visuals;
using Evolve.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Evolve.EditorTools
{
    /// <summary>
    /// Old roots and light. Menu: Evolve -> Dress Lake: Roots and Light.
    /// Roots: knobby arches and snaking roots over the ground (mostly around the ancient roots in the north), thick
    /// roots hanging from the ceiling with branches. Light: soft shafts of light slanting down from the ceiling, a
    /// spot light inside each, and slow glowing spores drifting through the air.
    /// Visual only (no colliders), a few combined meshes under one object, safe to run again.
    /// Run "Dress Lake: Water" and "Dress Lake: Ground" first.
    /// </summary>
    public static class LakeRootsLight
    {
        const string Root = "Assets/_Project";
        const string DressedName = "Lake Roots and Light (Dressed)";

        [MenuItem("Evolve/Dress Lake: Roots and Light")]
        public static void Dress()
        {
            PrototypeSceneBuilder.EnsureFolder(Root + "/Materials");
            PrototypeSceneBuilder.EnsureFolder(Root + "/Models");
            WaterSurface.Refresh();
            Physics.SyncTransforms();

            var old = GameObject.Find(DressedName);
            if (old != null) Object.DestroyImmediate(old);
            var root = new GameObject(DressedName).transform;
            root.gameObject.AddComponent<DetailCulling>();   // only what is near the camera is drawn and lit
            var rnd = new System.Random(4242);
            var ponds = new List<WaterSurface>(WaterSurface.All);
            int seed = 1000;

            var bark = new List<CombineInstance>();
            var moss = new List<CombineInstance>();
            var blob = FloraMeshes.Blob();

            void AddRoot(Vector3[] path, float baseR, float tipR, float knobs, bool mossOnTop)
            {
                bark.Add(new CombineInstance { mesh = RootMeshes.RootTube(path, baseR, tipR, ++seed, knobs), transform = Matrix4x4.identity });
                if (!mossOnTop) return;
                for (int i = 2; i < path.Length - 1; i += 2)
                {
                    float r = Mathf.Lerp(baseR, tipR, i / (float)(path.Length - 1));
                    moss.Add(new CombineInstance
                    {
                        mesh = blob,
                        transform = Matrix4x4.TRS(path[i] + Vector3.up * r * 0.75f, Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f), new Vector3(r * 1.8f, r * 0.6f, r * 1.8f))
                    });
                }
            }

            // ---------------------------------------------------------------- roots over the ground
            // The ancient roots area (north): arches and snaking roots, but never across a shelter entrance.
            Vector2 centre = new Vector2(0f, 50f);
            var ancient = GameObject.Find("Chan Re Co Thu (Ancient Roots)");
            if (ancient != null)
            {
                Bounds b = default; bool any = false;
                foreach (var r in ancient.GetComponentsInChildren<Renderer>())
                { if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds); }
                if (any) centre = new Vector2(b.center.x, b.center.z);
            }
            var shelters = new List<Vector2>();
            foreach (var s in Object.FindObjectsByType<ShelterZone>()) shelters.Add(new Vector2(s.transform.position.x, s.transform.position.z));
            bool NearShelter(Vector2 p) { foreach (var s in shelters) if (Vector2.Distance(s, p) < 5.5f) return true; return false; }
            Vector3 Ground(Vector2 p) => new Vector3(p.x, LakeGround.GroundY(p.x, p.y), p.y);

            void Arches(Vector2 at0, float rangeX, float rangeZ, int count)
            {
                for (int i = 0; i < count; i++)
                {
                    Vector2 a = at0 + new Vector2(Mathf.Lerp(-rangeX, rangeX, (float)rnd.NextDouble()), Mathf.Lerp(-rangeZ, rangeZ, (float)rnd.NextDouble()));
                    float angle = (float)rnd.NextDouble() * Mathf.PI * 2f, span = 5f + (float)rnd.NextDouble() * 6f;
                    Vector2 b2 = a + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * span;
                    Vector2 mid = (a + b2) * 0.5f;
                    if (NearShelter(a) || NearShelter(b2) || NearShelter(mid) || LakeGround.Wet(ponds, a) || LakeGround.Wet(ponds, b2)) continue;
                    float height = 2f + (float)rnd.NextDouble() * 3.2f, radius = 0.4f + (float)rnd.NextDouble() * 0.45f;
                    var path = RootMeshes.ArchPath(Ground(a), Ground(b2), height, ++seed, 0.9f, 16);
                    AddRoot(path, radius * 1.25f, radius * 1.0f, 0.12f, true);
                    // two side roots reaching down to the ground
                    for (int k = 0; k < 2; k++)
                    {
                        int at = 4 + rnd.Next(7);
                        Vector3 dir = new Vector3((float)rnd.NextDouble() - 0.5f, 0f, (float)rnd.NextDouble() - 0.5f).normalized;
                        var branch = RootMeshes.BranchPath(path[at], dir, 2f + (float)rnd.NextDouble() * 2.5f, Mathf.Max(0.5f, path[at].y - Ground(new Vector2(path[at].x, path[at].z)).y + 0.2f), ++seed, 9);
                        AddRoot(branch, radius * 0.45f, radius * 0.1f, 0.1f, false);
                    }
                }
            }

            void Snake(Vector2 start)
            {
                if (LakeGround.Wet(ponds, start) || NearShelter(start)) return;
                float angle = (float)rnd.NextDouble() * Mathf.PI * 2f, length = 4f + (float)rnd.NextDouble() * 5f;
                var path = new Vector3[10];
                float r = 0.14f + (float)rnd.NextDouble() * 0.2f;
                Vector2 p = start;
                for (int j = 0; j < path.Length; j++)
                {
                    angle += ((float)rnd.NextDouble() - 0.5f) * 0.7f;
                    p += new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (length / path.Length);
                    path[j] = new Vector3(p.x, LakeGround.GroundY(p.x, p.y) + r * 0.2f, p.y);
                }
                AddRoot(path, r * 1.2f, r * 0.35f, 0.18f, false);
            }

            // The ancient roots (the root-foot pad in the layout): the biggest cluster of arches.
            Arches(centre, 26f, 9f, 11);
            // Every other biome pad of the layout gets some roots too: a few arches and snaking roots.
            var otherPads = LakeLayout.Areas();
            otherPads.RemoveAt(0);                                            // the core has its own ponds' roots below
            foreach (var pad in otherPads)
            {
                if (Vector2.Distance(pad.Center, centre) < 30f) continue;     // that one is the ancient roots above
                Arches(pad.Center, pad.Radius * 0.6f, pad.Radius * 0.6f, 5);
                for (int i = 0; i < 9; i++)
                    Snake(pad.Center + new Vector2(Mathf.Lerp(-pad.Radius, pad.Radius, (float)rnd.NextDouble()), Mathf.Lerp(-pad.Radius, pad.Radius, (float)rnd.NextDouble())) * 0.7f);
            }

            // Low roots snaking over the ground near the ponds, half buried.
            foreach (var pond in ponds)
            {
                var outline = pond.WorldOutline;
                for (int k = 0; k < outline.Length; k += 13)
                {
                    if (rnd.NextDouble() > 0.6) continue;
                    Vector2 start = outline[k] + LakeGround.Outward(outline, k) * (1.5f + (float)rnd.NextDouble() * 3f);
                    if (LakeGround.Wet(ponds, start) || NearShelter(start)) continue;
                    float angle = (float)rnd.NextDouble() * Mathf.PI * 2f, length = 4f + (float)rnd.NextDouble() * 5f;
                    var path = new Vector3[10];
                    float r = 0.14f + (float)rnd.NextDouble() * 0.2f;
                    Vector2 p = start;
                    for (int j = 0; j < path.Length; j++)
                    {
                        angle += ((float)rnd.NextDouble() - 0.5f) * 0.7f;
                        p += new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (length / path.Length);
                        path[j] = new Vector3(p.x, LakeGround.GroundY(p.x, p.y) + r * 0.2f, p.y);
                    }
                    AddRoot(path, r * 1.2f, r * 0.35f, 0.18f, false);
                }
            }

            // ---------------------------------------------------------------- roots hanging from the ceiling
            // Thick roots hang from the roof only in a small cave; a big cave (dome with its own "Hanging Roots") has its own.
            if (!LakeLayout.HasHangingRoots())
            {
                var tops = LakeLayout.HangingPoints(rnd);
                foreach (var top in tops)
                {
                    var start = top + new Vector3(((float)rnd.NextDouble() - 0.5f) * 3f, 1f, ((float)rnd.NextDouble() - 0.5f) * 3f);
                    float bottom = 5f + (float)rnd.NextDouble() * 6f;
                    var path = FloraMeshes.VinePath(start, Mathf.Max(4f, start.y - bottom), ++seed, 0.9f, 14);
                    AddRoot(path, 0.95f, 0.1f, 0.14f, false);
                    for (int k = 0; k < 3; k++)
                    {
                        int at = 3 + rnd.Next(6);
                        float a = (float)rnd.NextDouble() * Mathf.PI * 2f;
                        var branch = RootMeshes.BranchPath(path[at], new Vector3(Mathf.Cos(a), -0.2f, Mathf.Sin(a)), 3f + (float)rnd.NextDouble() * 3f, 1.5f + (float)rnd.NextDouble() * 2f, ++seed, 8);
                        AddRoot(branch, 0.3f, 0.05f, 0.1f, false);
                    }
                }
            }

            LakeGround.AddCombined(root, "Roots", bark, LakeGround.OpaqueMaterial("Lake_RootBark", new Color(0.30f, 0.21f, 0.15f), 0.15f, false));
            LakeGround.AddCombined(root, "Root Moss", moss, LakeGround.OpaqueMaterial("Lake_RockMoss", new Color(0.30f, 0.50f, 0.22f), 0.1f, false));

            // ---------------------------------------------------------------- light shafts
            var shaftMaterial = ShaftMaterial();
            var targets = new List<Vector2>();
            foreach (var pond in ponds)
            {
                var o = pond.WorldOutline; Vector2 c = Vector2.zero;
                foreach (var p in o) c += p;
                targets.Add(c / o.Length);
            }
            for (int i = 0; i < 4; i++) targets.Add(new Vector2(Mathf.Lerp(-45f, 45f, (float)rnd.NextDouble()), Mathf.Lerp(-45f, 45f, (float)rnd.NextDouble())));
            foreach (var area in LakeLayout.Areas())      // two shafts in every biome pad of the layout
                if (area.Name != "Core")
                    for (int i = 0; i < 2; i++)
                        targets.Add(area.Center + new Vector2(Mathf.Lerp(-area.Radius, area.Radius, (float)rnd.NextDouble()), Mathf.Lerp(-area.Radius, area.Radius, (float)rnd.NextDouble())) * 0.5f);
            int shafts = 0;
            var slant = new Vector3(5f, 0f, 3f);
            foreach (var target in targets)
            {
                if (shafts >= 8) break;
                var bottomPoint = new Vector3(target.x + (float)(rnd.NextDouble() - 0.5) * 8f, 0f, target.y + (float)(rnd.NextDouble() - 0.5) * 8f);
                bottomPoint.y = LakeGround.GroundY(bottomPoint.x, bottomPoint.z);
                float roofY = LakeLayout.CeilingY(bottomPoint.x - slant.x, bottomPoint.z - slant.z);   // the real roof above the spot
                Vector3 topPoint = bottomPoint - slant + Vector3.up * (roofY + 2f);
                Vector3 dir = (bottomPoint - topPoint).normalized;
                float height = Vector3.Distance(topPoint, bottomPoint);
                float topR = 1.2f + (float)rnd.NextDouble() * 1.2f, bottomR = 3.8f + (float)rnd.NextDouble() * 2.2f;

                var go = new GameObject("Light Shaft");
                go.transform.SetParent(root, false);
                go.transform.position = topPoint;
                go.transform.rotation = Quaternion.FromToRotation(Vector3.down, dir);
                go.AddComponent<MeshFilter>().sharedMesh = RootMeshes.LightShaft(topR, bottomR, height, 18, 8, ++seed);
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = shaftMaterial;
                mr.shadowCastingMode = ShadowCastingMode.Off;
                mr.receiveShadows = false;

                var lightGo = new GameObject("Shaft Light");
                lightGo.transform.SetParent(go.transform, false);
                var l = lightGo.AddComponent<Light>();
                l.type = LightType.Spot;
                l.range = height * 1.05f;
                l.spotAngle = Mathf.Clamp(2f * Mathf.Atan((bottomR + 1.5f) / height) * Mathf.Rad2Deg, 15f, 90f);
                l.color = new Color(1f, 0.96f, 0.82f);
                l.intensity = Mathf.Clamp(0.45f * (height / 20f) * (height / 20f), 0.45f, 4f); // same brightness on the ground whatever the roof height (a gentle level: glossy water glares)
                l.shadows = LightShadows.None;
                lightGo.transform.localRotation = Quaternion.FromToRotation(Vector3.forward, Vector3.down);
                shafts++;
            }

            // ---------------------------------------------------------------- spores in the air
            foreach (var area in LakeLayout.Areas()) MakeSpores(root, area.Center, area.Radius);

            EditorSceneManager.MarkAllScenesDirty();
            AssetDatabase.SaveAssets();
            Debug.Log($"Đã dựng rễ ({bark.Count} rễ), {shafts} tia sáng và bụi bào tử. Bấm Ctrl+S để lưu scene. Chạy lại menu này sẽ làm lại từ đầu.");
        }

        /// <summary>Soft light: warm, strongest at the top, fading toward the ground and streaky, seen from both sides.</summary>
        static Material ShaftMaterial()
        {
            var tex = LakeWater.PaintedTexture(Root + "/Models/LightShaft.png", 64, 64, (u, v) =>
            {
                float vertical = Mathf.Pow(1f - Mathf.Clamp01(v), 1.3f);                       // v: 0 = top, 1 = bottom
                float streaks = 0.55f + 0.45f * LakeWater.TileableNoise(u * 9f, v * 2f, 9, 4);  // vertical streaks
                float soft = Mathf.Clamp01(v * 8f);                                            // no hard edge at the very top
                return new Color(1f, 1f, 1f, vertical * streaks * soft);
            });
            var mat = LakeWater.TranslucentMaterial("Lake_LightShaft", tex, new Color(1f, 0.97f, 0.82f, 0.075f), new Color(0.10f, 0.095f, 0.07f), 0f, (int)RenderQueue.Transparent + 20);
            mat.SetFloat("_Cull", 0f); // visible from inside the beam too
            mat.SetFloat("_ReceiveShadows", 0f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static void MakeSpores(Transform parent, Vector2 centre, float radius)
        {
            var go = new GameObject("Spore Dust");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(centre.x, 3f, centre.y);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.prewarm = true;
            main.startLifetime = 9f;
            main.startSpeed = 0.12f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.09f);
            main.startColor = new Color(0.7f, 1f, 0.88f, 0.45f);
            main.maxParticles = 120;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = ps.emission;
            emission.rateOverTime = 12f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(radius * 1.6f, 6f, radius * 1.6f);
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.35f;
            noise.frequency = 0.25f;
            var colour = ps.colorOverLifetime;
            colour.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                             new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(1f, 0.8f), new GradientAlphaKey(0f, 1f) });
            colour.color = gradient;

            var tex = LakeWater.PaintedTexture(Root + "/Models/SporeDot.png", 32, 32, (u, v) =>
            {
                float d = new Vector2(u - 0.5f, v - 0.5f).magnitude * 2f;
                float a = Mathf.Pow(Mathf.Clamp01(1f - d), 2f);
                return new Color(a, a, a, a);
            });
            string path = Root + "/Materials/Lake_Spore.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")) { name = "Lake_Spore" };
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetTexture("_BaseMap", tex);
            mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 2f); // additive: spores glow
            mat.SetFloat("_SrcBlend", (float)BlendMode.One);
            mat.SetFloat("_DstBlend", (float)BlendMode.One);
            mat.SetFloat("_ZWrite", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = (int)RenderQueue.Transparent + 30;
            EditorUtility.SetDirty(mat);
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = mat;
        }
    }
}
