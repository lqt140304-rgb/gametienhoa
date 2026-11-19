using System.Collections.Generic;
using Evolve.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using P = Evolve.EditorTools.PrototypeSceneBuilder;

namespace Evolve.EditorTools
{
    /// <summary>
    /// Evolve → Build Slime Cavern (new map): the clean new map (spec 2026-11-19-slime-cavern-design). One small cavern, one lake (8 m deep) where the slime is
    /// born, a ring of small plants and glowing ore round it, a roof with a hole of sky above the lake and the sun falling through it. Everything comes from
    /// <see cref="CavernLayout"/>; nothing of the old Underroot Lake map is used. Makes the scene Assets/_Project/Scenes/Slime_Cavern.unity.
    /// </summary>
    public static class CavernBuilder
    {
        const string Root = "Assets/_Project";
        const string ScenePath = Root + "/Scenes/Slime_Cavern.unity";
        const string TerrainFolder = Root + "/Terrain";
        const string ShallowTag = "ShallowWater";
        const float BaseOffset = 9f;          // the lake bed is 8 m under the water: the terrain starts 9 m below the water level
        const float TerrainHeightY = 44f;     // 9 + the 30 m wall + headroom
        const int HeightRes = 257;            // 0.5 m cells over 128 m
        const int AlphaRes = 128;

        [MenuItem("Evolve/Build Slime Cavern (new map)")]
        public static void Build()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null &&
                !EditorUtility.DisplayDialog("Slime_Cavern", "Scene Slime_Cavern đã tồn tại. Dựng lại từ đầu? (thay đổi thủ công trong scene sẽ mất)", "Dựng lại", "Huỷ"))
                return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            foreach (var f in new[] { "Scenes", "Prefabs", "ScriptableObjects", "Materials", "Settings", "Terrain" })
                P.EnsureFolder(Root + "/" + f);
            EnsureTag(ShallowTag);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var shared = P.CreateSharedAssets();

            BuildTerrain();
            BuildWater();
            BuildLook();
            BuildCeiling();
            BuildSkyAndSun();
            BuildBeam();
            BuildPlants();
            BuildOreAndRocks();
            CavernForage.Dress();   // what can be eaten: plants, mushrooms and four minerals

            var spawn = CavernLayout.SpawnPoint();
            var slime = P.CreatePlayer(shared, spawn);
            if (Camera.main != null)
            {
                Camera.main.farClipPlane = 300f;
                Camera.main.clearFlags = CameraClearFlags.Skybox;   // the hole in the roof shows the sky
            }

            EditorSceneManager.SaveScene(scene, ScenePath);
            P.AddToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = slime;
            Debug.Log("Đã dựng " + ScenePath + ": hang nhỏ có một hồ sâu 8 m, thảm cây nhỏ và quặng phát sáng quanh hồ, lỗ trời trên hồ. Slime xuất hiện bên bờ hồ.");
        }

        // ---------------------------------------------------------------- terrain

        static TerrainLayer Layer(string name, Color a, Color b, float tile)
        {
            string texPath = $"{TerrainFolder}/Cavern_{name}_v2.png", layerPath = $"{TerrainFolder}/Cavern_{name}.terrainlayer";
            var tex = LakeWater.PaintedTexture(texPath, 256, 256, (u, v) =>
            {
                // four scales of noise (blotches, patches, grain, specks) so the ground is not a flat colour; a pebble speckle on top
                float n = LakeWater.TileableNoise(u * 6f, v * 6f, 6, 6) * 0.4f + LakeWater.TileableNoise(u * 16f, v * 16f, 16, 16) * 0.3f
                        + LakeWater.TileableNoise(u * 48f, v * 48f, 48, 48) * 0.2f + LakeWater.TileableNoise(u * 128f, v * 128f, 128, 128) * 0.1f;
                var c = Color.Lerp(a, b, n);
                if (LakeWater.TileableNoise(u * 90f + 5f, v * 90f + 9f, 90, 90) > 0.86f) c *= 0.6f;   // dark pebbles and pits
                c.a = 1f;
                return c;
            });
            var layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(layerPath);
            if (layer == null)
            {
                layer = new TerrainLayer();
                AssetDatabase.CreateAsset(layer, layerPath);
            }
            layer.diffuseTexture = tex;
            layer.tileSize = new Vector2(tile, tile);
            EditorUtility.SetDirty(layer);
            return layer;
        }

        static void BuildTerrain()
        {
            var layers = new[]
            {
                Layer("Mud", new Color(0.13f, 0.11f, 0.08f), new Color(0.22f, 0.19f, 0.13f), 5f),
                Layer("Moss", new Color(0.12f, 0.2f, 0.09f), new Color(0.2f, 0.32f, 0.13f), 4f),
                Layer("Rock", new Color(0.18f, 0.17f, 0.16f), new Color(0.32f, 0.3f, 0.28f), 8f),
            };

            var data = new TerrainData { heightmapResolution = HeightRes };
            data.size = new Vector3(CavernLayout.Size, TerrainHeightY, CavernLayout.Size);
            var heights = new float[HeightRes, HeightRes];
            float cell = CavernLayout.Size / (HeightRes - 1), half = CavernLayout.Size * 0.5f;
            for (int zi = 0; zi < HeightRes; zi++)
                for (int xi = 0; xi < HeightRes; xi++)
                    heights[zi, xi] = Mathf.Clamp01((BaseOffset + CavernLayout.GroundHeight(-half + xi * cell, -half + zi * cell)) / TerrainHeightY);
            data.SetHeights(0, 0, heights);

            data.alphamapResolution = AlphaRes;
            data.terrainLayers = layers;
            var alpha = new float[AlphaRes, AlphaRes, 3];
            float a = CavernLayout.Size / AlphaRes;
            for (int zi = 0; zi < AlphaRes; zi++)
                for (int xi = 0; xi < AlphaRes; xi++)
                {
                    float x = -half + (xi + 0.5f) * a, z = -half + (zi + 0.5f) * a;
                    float h = CavernLayout.GroundHeight(x, z);
                    float slope = Mathf.Abs(CavernLayout.GroundHeight(x + 1f, z) - CavernLayout.GroundHeight(x - 1f, z)) * 0.5f
                                + Mathf.Abs(CavernLayout.GroundHeight(x, z + 1f) - CavernLayout.GroundHeight(x, z - 1f)) * 0.5f;
                    float wet = CavernLayout.DepthAt(x, z) > 0f ? 1f : Mathf.Clamp01(1f - (Mathf.Sqrt(x * x + z * z) - CavernLayout.LakeRadius(Mathf.Atan2(z, x))) / 3f);
                    float rock = Mathf.Clamp01((slope - 0.5f) / 0.7f);
                    float w0 = wet, w2 = (1f - wet) * rock, w1 = Mathf.Max(0f, 1f - w0 - w2);
                    float sum = w0 + w1 + w2;
                    alpha[zi, xi, 0] = w0 / sum; alpha[zi, xi, 1] = w1 / sum; alpha[zi, xi, 2] = w2 / sum;
                }
            data.SetAlphamaps(0, 0, alpha);

            string path = TerrainFolder + "/Cavern_Terrain.asset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(data, path);
            var go = Terrain.CreateTerrainGameObject(data);
            go.name = "Cavern Terrain";
            go.transform.position = new Vector3(-half, -BaseOffset, -half);
            var terrain = go.GetComponent<Terrain>();
            terrain.heightmapPixelError = 3f;
            terrain.basemapDistance = 150f;
            terrain.drawInstanced = true;
        }

        // ---------------------------------------------------------------- the lake

        static void BuildWater()
        {
            if (BuildRealisticWater()) { MakeWaterZones(); return; }   // the project's own water shader: depth tint, ripples, foam, refraction
            var water = LakeWater.TranslucentMaterial("Cavern_Water", Texture2D.whiteTexture, new Color(0.10f, 0.40f, 0.46f, 0.78f), new Color(0.01f, 0.06f, 0.07f), 0.92f, (int)RenderQueue.Transparent);
            water.SetFloat("_Cull", 0f);
            EditorUtility.SetDirty(water);

            // a fan over the lake, a little past the shore so its edge hides under the bank
            const int sides = 96;
            var verts = new List<Vector3> { new Vector3(0f, CavernLayout.WaterLevel, 0f) };
            for (int i = 0; i < sides; i++)
            {
                float ang = i / (float)sides * Mathf.PI * 2f, r = CavernLayout.LakeRadius(ang) + 0.35f;
                verts.Add(new Vector3(Mathf.Cos(ang) * r, CavernLayout.WaterLevel, Mathf.Sin(ang) * r));
            }
            var tris = new List<int>();
            for (int i = 0; i < sides; i++) { tris.Add(0); tris.Add(1 + (i + 1) % sides); tris.Add(1 + i); }   // faces up
            var mesh = new Mesh { name = "Cavern_Lake" };
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            var go = new GameObject("Cavern Lake Surface");
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = water;
            mr.shadowCastingMode = ShadowCastingMode.Off;

            MakeWaterZones();
        }

        /// <summary>Swimming reads CavernWater (WaterField); the trigger box makes the lake drinkable.</summary>
        static void MakeWaterZones()
        {
            var zones = new GameObject("Water Zones");   // the slime looks for this name to add its swim ripple, under-water tint and zone air
            zones.AddComponent<CavernWater>();
            var lake = new GameObject("Hồ Khởi Sinh");
            lake.transform.SetParent(zones.transform, false);
            lake.transform.position = new Vector3(0f, -2f, 0f);
            lake.tag = ShallowTag;
            var box = lake.AddComponent<BoxCollider>();
            box.isTrigger = true;
            float side = CavernLayout.LakeMeanRadius * 2.4f + 4f;
            box.size = new Vector3(side, 12f, side);
            var wz = lake.AddComponent<WaterZone>();
            wz.Clean = true;
            wz.DisplayName = "Hồ Khởi Sinh";
        }

        /// <summary>The lake as a WaterSurface (animated shore, foam, bank) with the Evolve/LakeWater shader. False if the shader is missing: the plain surface is used.</summary>
        static bool BuildRealisticWater()
        {
            var shader = Shader.Find("Evolve/LakeWater");
            if (shader == null) return false;
            string path = Root + "/Materials/Cavern_WaterLake.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { mat = new Material(shader) { name = "Cavern_WaterLake" }; AssetDatabase.CreateAsset(mat, path); }
            mat.shader = shader;
            mat.SetColor("_ShallowColor", new Color(0.2f, 0.62f, 0.62f, 0.5f));
            mat.SetColor("_DeepColor", new Color(0.015f, 0.17f, 0.22f, 0.88f));
            mat.SetFloat("_DeepDistance", 4.5f);
            mat.SetTexture("_BumpMap", LakeWater.RippleNormalMap());
            mat.renderQueue = (int)RenderQueue.Transparent - 2;   // just before the slime's gel, so a swimming slime is never washed out
            EditorUtility.SetDirty(mat);

            var go = new GameObject("Cavern Lake Surface");
            go.transform.position = new Vector3(0f, CavernLayout.WaterLevel, 0f);
            var surface = go.AddComponent<WaterSurface>();
            surface.Shape = WaterShape.Outline;
            surface.WaveHeight = 0.02f;
            surface.FoamMaterial = LakeWater.FoamMaterial();
            surface.BankMaterial = LakeWater.BankMaterial();
            surface.Seed = 3;
            const int points = 160;
            var outline = new Vector2[points];
            for (int i = 0; i < points; i++)
            {
                float a = i / (float)points * Mathf.PI * 2f, r = CavernLayout.LakeRadius(a) + 0.3f;
                outline[i] = new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r);
            }
            surface.OutlinePoints = outline;
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            WaterSurface.Refresh();   // the shape was set after the component woke up

            foreach (var urp in new[] { GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset })
                if (urp != null) { urp.supportsCameraDepthTexture = true; EditorUtility.SetDirty(urp); }   // the water shader reads the scene depth
            return true;
        }

        // ---------------------------------------------------------------- the film look

        /// <summary>Post-processing like a photograph (ACES tone mapping, a little bloom, vignette and fine grain, more contrast) and ambient occlusion in the creases.</summary>
        static void BuildLook()
        {
            const string profilePath = Root + "/Settings/CavernLook.asset";
            AssetDatabase.DeleteAsset(profilePath);
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, profilePath);

            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(0.9f);
            bloom.intensity.Override(0.7f);
            bloom.scatter.Override(0.65f);
            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.3f);
            vignette.smoothness.Override(0.5f);
            var tone = profile.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.ACES);
            var grain = profile.Add<FilmGrain>(true);
            grain.type.Override(FilmGrainLookup.Thin1);
            grain.intensity.Override(0.2f);
            grain.response.Override(0.8f);
            var color = profile.Add<ColorAdjustments>(true);   // the vision volume owns saturation; this adds contrast and a touch of exposure
            color.contrast.Override(12f);
            color.postExposure.Override(0.15f);
            foreach (var c in profile.components) AssetDatabase.AddObjectToAsset(c, profile);
            EditorUtility.SetDirty(profile);

            var go = new GameObject("Cavern Look");
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = -10f;   // below the vision volume: MonochromeVision owns saturation, fog, ambient and daylight
            volume.sharedProfile = profile;

            LakeRealism.Ssao(true);   // dark creases and contact shadows (adds the feature to the URP renderer if it is not there)
        }

        // ---------------------------------------------------------------- roof, sky, sun

        static void BuildCeiling()
        {
            var mat = LakeGround.OpaqueMaterial("Cavern_Ceiling", new Color(0.10f, 0.085f, 0.07f), 0.05f, true);
            int n = (int)CavernLayout.Size;           // 1 m cells
            float half = CavernLayout.Size * 0.5f;
            var verts = new List<Vector3>((n + 1) * (n + 1));
            for (int zi = 0; zi <= n; zi++)
                for (int xi = 0; xi <= n; xi++)
                {
                    float x = -half + xi, z = -half + zi;
                    float lump = (ZoneLayout.Noise(x / 7f + 5f, z / 7f + 2f) - 0.5f) * 2.4f;   // a lumpy rock roof
                    verts.Add(new Vector3(x, CavernLayout.CeilingY(x, z) + lump, z));
                }
            var tris = new List<int>();
            for (int zi = 0; zi < n; zi++)
                for (int xi = 0; xi < n; xi++)
                {
                    if (CavernLayout.InHole(-half + xi + 0.5f, -half + zi + 0.5f)) continue;   // the hole of sky
                    int a = zi * (n + 1) + xi, b = a + 1, c = a + (n + 1), d = c + 1;
                    tris.Add(a); tris.Add(b); tris.Add(c);   // faces down
                    tris.Add(b); tris.Add(d); tris.Add(c);
                }
            var mesh = new Mesh { name = "Cavern_Ceiling" };
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            var go = new GameObject("Cavern Ceiling");
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.TwoSided;   // the roof throws the shadow that leaves only the beam of light
        }

        static void BuildSkyAndSun()
        {
            var shader = Shader.Find("Skybox/Procedural");
            var sky = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/CavernSky.mat");
            if (sky == null && shader != null)
            {
                sky = new Material(shader);
                AssetDatabase.CreateAsset(sky, Root + "/Materials/CavernSky.mat");
            }
            if (sky != null)
            {
                sky.SetFloat("_SunSize", 0.05f);
                sky.SetFloat("_AtmosphereThickness", 1f);
                sky.SetColor("_SkyTint", new Color(0.5f, 0.55f, 0.62f));
                sky.SetFloat("_Exposure", 1.2f);
                EditorUtility.SetDirty(sky);
                RenderSettings.skybox = sky;
            }
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.05f, 0.065f, 0.08f);   // dark: only the beam and the glowing ore light the cave
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.03f, 0.045f, 0.055f);
            RenderSettings.fogStartDistance = 10f;
            RenderSettings.fogEndDistance = 120f;

            Light sun = null;
            foreach (var l in Object.FindObjectsByType<Light>()) if (l.type == LightType.Directional) { sun = l; break; }
            if (sun != null)
            {
                sun.transform.rotation = Quaternion.Euler(CavernLayout.SunElevation, CavernLayout.SunAzimuth, 0f);
                sun.color = new Color(1f, 0.95f, 0.85f);
                sun.intensity = 1.3f;
                sun.shadows = LightShadows.Soft;
                sun.shadowStrength = 1f;
                RenderSettings.sun = sun;
            }
            var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urp != null)
            {
                urp.shadowDistance = Mathf.Max(urp.shadowDistance, 120f);
                EditorUtility.SetDirty(urp);
            }
        }

        static void BuildBeam()
        {
            var hole = CavernLayout.HoleCenter();
            var dir = CavernLayout.SunDirection;
            float top = CavernLayout.CeilingY(hole.x, hole.y);
            float height = top / -dir.y;
            var topPoint = new Vector3(hole.x, top, hole.y);

            var mat = LakeWater.TranslucentMaterial("Cavern_Beam", Texture2D.whiteTexture, new Color(1f, 0.95f, 0.8f, 0.10f), new Color(0.30f, 0.28f, 0.2f), 0f, (int)RenderQueue.Transparent + 20);
            mat.SetFloat("_Cull", 0f);
            EditorUtility.SetDirty(mat);

            var go = new GameObject("Light Beam");
            go.transform.position = topPoint;
            go.transform.rotation = Quaternion.FromToRotation(Vector3.down, dir);
            go.AddComponent<MeshFilter>().sharedMesh = RootMeshes.LightShaft(CavernLayout.HoleRadius, CavernLayout.HoleRadius + 1.8f, height, 18, 8, 3);
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;

            var lightGo = new GameObject("Beam Light");
            lightGo.transform.SetParent(go.transform, false);
            var l = lightGo.AddComponent<Light>();
            l.type = LightType.Spot;
            l.range = height * 1.05f;
            l.spotAngle = Mathf.Clamp(2f * Mathf.Atan((CavernLayout.HoleRadius + 3f) / height) * Mathf.Rad2Deg, 15f, 90f);
            l.color = new Color(1f, 0.96f, 0.82f);
            l.intensity = Mathf.Clamp(0.45f * (height / 20f) * (height / 20f), 0.45f, 4f);
            l.shadows = LightShadows.None;
            lightGo.transform.localRotation = Quaternion.FromToRotation(Vector3.forward, Vector3.down);
        }

        // ---------------------------------------------------------------- plants

        static void BuildPlants()
        {
            var root = new GameObject("Plants").transform;

            // the carpet: real 3D grass blades (terrain details, swaying and bending away from the slime)
            CavernGrass.Dress(Object.FindAnyObjectByType<Terrain>());

            // decorative mushrooms in clumps at the water's edge and at the foot of the wall: the detailed Blender clusters (CaveShroom_0..3);
            // the code mushrooms are only the fallback
            PropInstaller.EnsureLandmarks();
            var prefabs = new GameObject[4];
            bool haveModels = true;
            for (int i = 0; i < prefabs.Length; i++)
            {
                prefabs[i] = AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Models/Underroot/CaveShroom_{i}.fbx");
                if (prefabs[i] == null) haveModels = false;
            }
            var rnd = new System.Random(23);
            var points = CavernLayout.Scatter(23, 4000, CavernLayout.MushroomDensity);
            if (haveModels)
            {
                var tints = new Dictionary<int, Color> { { 2, new Color(0.3f, 0.95f, 0.9f) }, { 3, new Color(1f, 0.72f, 0.3f) } };   // the glowing caps: teal and amber
                int placed = 0;
                foreach (var p in points)
                {
                    if (placed >= 60) break;                       // each cluster is already several mushrooms
                    double pick = rnd.NextDouble();
                    int v = pick < 0.34 ? 0 : pick < 0.60 ? 1 : pick < 0.85 ? 2 : 3;
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[v], root);
                    go.transform.position = p;
                    go.transform.rotation = Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f);
                    go.transform.localScale = Vector3.one * Mathf.Lerp(0.8f, 1.4f, (float)rnd.NextDouble());
                    if (tints.TryGetValue(v, out var tint))
                    {
                        var glow = CavernForage.GlowMat("shroom" + v, tint);
                        if (glow != null) foreach (var r in go.GetComponentsInChildren<Renderer>(true)) if (r.name.Contains("Glow")) r.sharedMaterial = glow;
                    }
                    placed++;
                }
                return;
            }
            var stalkParts = new List<CombineInstance>();
            var capParts = new List<CombineInstance>();
            int count = 0;
            foreach (var p in points)
            {
                if (count >= 160) break;
                FloraMeshes.Mushroom(1000 + count, 0.25f + (float)rnd.NextDouble() * 0.45f, out var stalk, out var cap, out _);
                var m = Matrix4x4.TRS(p, Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f), Vector3.one);
                stalkParts.Add(new CombineInstance { mesh = stalk, transform = m });
                capParts.Add(new CombineInstance { mesh = cap, transform = m });
                count++;
            }
            LakeGround.AddCombined(root, "Mushroom Stalks", stalkParts, LakeGround.OpaqueMaterial("Cavern_MushroomStalk", new Color(0.85f, 0.82f, 0.7f), 0.1f, false));
            LakeGround.AddCombined(root, "Mushroom Caps", capParts, LakeGround.OpaqueMaterial("Cavern_MushroomCap", new Color(0.5f, 0.85f, 0.8f), 0.2f, false, new Color(0.08f, 0.5f, 0.45f)));
        }

        // ---------------------------------------------------------------- ore and rocks

        static void BuildOreAndRocks()
        {
            var root = new GameObject("Ore and Rocks").transform;
            var rockMat = LakeGround.OpaqueMaterial("Cavern_Rock", new Color(0.30f, 0.29f, 0.28f), 0.08f, false);
            var mossMat = LakeGround.OpaqueMaterial("Cavern_RockMoss", new Color(0.20f, 0.34f, 0.13f), 0.05f, false);

            // a few kinds of rock, used again and again
            var rocks = new Mesh[6];
            var mosses = new Mesh[6];
            for (int i = 0; i < rocks.Length; i++) rocks[i] = GroundMeshes.Rock(40 + i, out mosses[i]);

            var rnd = new System.Random(25);
            var rockParts = new List<CombineInstance>();
            var mossParts = new List<CombineInstance>();
            var blender = BlenderRocks();   // the rocks modelled in Blender (tools/blender/build_rocks.py); the code rocks are only the fallback
            var scatter = CavernLayout.Scatter(25, 1500, CavernLayout.RockDensity);
            if (blender != null) PlaceBlenderRocks(scatter, rnd, blender, root, 0, 3, 0.6f, 1.3f);
            else AddRocks(scatter, rnd, rocks, mosses, rockParts, mossParts, 0.4f, 1.4f);

            // the ore sites: a big rock under each (the crystals themselves are food, see CavernForage), a few glowing ones lighting the cave
            var sites = CavernForageData.PlaceOre();
            int lights = 0;
            for (int i = 0; i < sites.Count; i++)
            {
                var p = sites[i].pos;
                if (blender != null) PlaceBlenderRocks(new List<Vector3> { p }, rnd, blender, root, 3, 5, 0.9f, 1.2f);
                else AddRocks(new List<Vector3> { p }, rnd, rocks, mosses, rockParts, mossParts, 1.2f, 1.9f);
                if (sites[i].spec.Id == "quartz" && lights < 8)
                {
                    lights++;
                    var lg = new GameObject("Ore Light " + i);
                    lg.transform.SetParent(root, false);
                    lg.transform.position = p + Vector3.up * 1.4f;
                    var l = lg.AddComponent<Light>();
                    l.type = LightType.Point;
                    l.color = new Color(0.35f, 0.85f, 1f);
                    l.range = 11f;
                    l.intensity = 2.2f;
                    l.shadows = LightShadows.None;
                }
            }
            LakeGround.AddCombined(root, "Rocks", rockParts, rockMat);
            LakeGround.AddCombined(root, "Rock Moss", mossParts, mossMat);
        }

        /// <summary>The six Blender rocks (Rock_0 .. Rock_5, from small to big), installed from tools/blender/out if missing; null if they are not there.</summary>
        static GameObject[] BlenderRocks()
        {
            PropInstaller.EnsureRocks();
            var list = new GameObject[6];
            for (int i = 0; i < list.Length; i++)
            {
                list[i] = AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Models/Underroot/Rock_{i}.fbx");
                if (list[i] == null) return null;
            }
            return list;
        }

        static void PlaceBlenderRocks(IList<Vector3> positions, System.Random rnd, GameObject[] prefabs, Transform parent, int firstSize, int lastSize, float minScale, float maxScale)
        {
            foreach (var p in positions)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[rnd.Next(firstSize, lastSize + 1)], parent);
                go.transform.position = p + Vector3.down * 0.05f;
                go.transform.rotation = Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f);
                go.transform.localScale = Vector3.one * Mathf.Lerp(minScale, maxScale, (float)rnd.NextDouble());
            }
        }

        static void AddRocks(IList<Vector3> positions, System.Random rnd, Mesh[] rocks, Mesh[] mosses, List<CombineInstance> rockParts, List<CombineInstance> mossParts, float minScale, float maxScale)
        {
            foreach (var p in positions)
            {
                int k = rnd.Next(rocks.Length);
                float s = Mathf.Lerp(minScale, maxScale, (float)rnd.NextDouble());
                var m = Matrix4x4.TRS(p + Vector3.down * 0.1f * s, Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f), new Vector3(s, s * (0.6f + 0.4f * (float)rnd.NextDouble()), s));
                rockParts.Add(new CombineInstance { mesh = rocks[k], transform = m });
                mossParts.Add(new CombineInstance { mesh = mosses[k], transform = m });
            }
        }

        /// <summary>A cluster of 6–9 flat-shaded hexagonal crystals leaning out of one spot, each ending in a point.</summary>
        internal static Mesh OreCluster(System.Random rnd, float lengthScale = 1f, float radiusScale = 1f, int minCrystals = 6)
        {
            var verts = new List<Vector3>();
            var tris = new List<int>();
            int crystals = minCrystals + rnd.Next(4);
            for (int c = 0; c < crystals; c++)
            {
                float lean = Mathf.Lerp(0f, 38f, (float)rnd.NextDouble()) * Mathf.Deg2Rad, around = (float)rnd.NextDouble() * Mathf.PI * 2f;
                var dir = new Vector3(Mathf.Sin(lean) * Mathf.Cos(around), Mathf.Cos(lean), Mathf.Sin(lean) * Mathf.Sin(around)).normalized;
                var right = Vector3.Cross(dir, Vector3.forward).sqrMagnitude < 0.01f ? Vector3.right : Vector3.Cross(dir, Vector3.forward).normalized;
                var fwd = Vector3.Cross(right, dir).normalized;
                float off = c == 0 ? 0f : 0.25f + (float)rnd.NextDouble() * 0.6f, offAng = (float)rnd.NextDouble() * Mathf.PI * 2f;
                var basePos = new Vector3(Mathf.Cos(offAng) * off, 0f, Mathf.Sin(offAng) * off);
                float radius = (0.10f + (float)rnd.NextDouble() * 0.16f) * radiusScale, length = ((c == 0 ? 1.8f : 0.7f) + (float)rnd.NextDouble() * 1.4f) * lengthScale;
                const int sides = 6;
                var ring0 = new Vector3[sides];
                var ring1 = new Vector3[sides];
                for (int s = 0; s < sides; s++)
                {
                    float a = s / (float)sides * Mathf.PI * 2f;
                    var o = right * Mathf.Cos(a) * radius + fwd * Mathf.Sin(a) * radius;
                    ring0[s] = basePos + o;
                    ring1[s] = basePos + dir * length * 0.72f + o * 0.9f;
                }
                var apex = basePos + dir * length;
                for (int s = 0; s < sides; s++)
                {
                    int t = (s + 1) % sides;
                    Quad(verts, tris, ring0[s], ring0[t], ring1[t], ring1[s]);
                    Tri(verts, tris, ring1[s], ring1[t], apex);
                }
            }
            var mesh = new Mesh { name = "Cavern_Ore" };
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        static void Tri(List<Vector3> verts, List<int> tris, Vector3 a, Vector3 b, Vector3 c)
        {
            int i = verts.Count;
            verts.Add(a); verts.Add(b); verts.Add(c);
            tris.Add(i); tris.Add(i + 2); tris.Add(i + 1);   // flat-shaded: every face has its own vertices
        }

        static void Quad(List<Vector3> verts, List<int> tris, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            Tri(verts, tris, a, b, c);
            Tri(verts, tris, a, c, d);
        }

        // ---------------------------------------------------------------- helpers

        static void EnsureTag(string tag)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0) return;
            var so = new SerializedObject(assets[0]);
            var tags = so.FindProperty("tags");
            for (int i = 0; i < tags.arraySize; i++)
                if (tags.GetArrayElementAtIndex(i).stringValue == tag) return;
            tags.InsertArrayElementAtIndex(tags.arraySize);
            tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
