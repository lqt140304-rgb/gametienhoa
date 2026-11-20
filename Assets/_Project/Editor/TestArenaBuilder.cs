using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Evolve.EditorTools
{
    /// <summary>
    /// Menu "Evolve → Build Test Arena": a clean, bright, flat 60 m arena with simple props for testing movement and combat:
    /// small rocks to crawl over, a wall too high to climb, a ramp, two ledges for the monkey to leap to, a bridge to walk under,
    /// a low wall to stun a charging beetle, and pillars for cover. No fog, no darkness, no spawned creatures (make them from the F1 god menu).
    /// Builds Assets/_Project/Scenes/TestArena.unity and does not touch the build settings.
    /// </summary>
    public static class TestArenaBuilder
    {
        const string Root = "Assets/_Project";
        const string ScenePath = Root + "/Scenes/TestArena.unity";
        const float Half = 30f;

        [MenuItem("Evolve/Build Test Arena")]
        public static void Build()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null &&
                !EditorUtility.DisplayDialog("TestArena", "Scene TestArena đã tồn tại. Dựng lại từ đầu?", "Dựng lại", "Huỷ")) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            foreach (var f in new[] { "Scenes", "Prefabs", "ScriptableObjects", "Materials", "Settings" }) PrototypeSceneBuilder.EnsureFolder(Root + "/" + f);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var shared = PrototypeSceneBuilder.CreateSharedAssets();
            var slime = PrototypeSceneBuilder.CreatePlayer(shared, new Vector3(0f, 1f, 0f));
            var dark = GameObject.Find("Vision Volume");
            if (dark != null) Object.DestroyImmediate(dark);   // the grey dark-vision filter of the cave scenes: not wanted here

            BuildLight();
            BuildFloorAndWalls();
            BuildProps();
            BuildFood(shared);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = slime;
            Debug.Log("Đã dựng " + ScenePath + ". Mở scene này, bấm Play. F1 mở menu thần để tạo quái thử (đóng F1 trước khi bấm chuột trái để Vồ).");
        }

        static void BuildLight()
        {
            var sun = Object.FindFirstObjectByType<Light>();   // the default directional light of a new scene
            sun.name = "Mặt Trời Thử";
            sun.type = LightType.Directional;
            sun.transform.rotation = Quaternion.Euler(52f, -35f, 0f);
            sun.color = new Color(1f, 0.97f, 0.9f);
            sun.intensity = 1.25f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.7f;

            RenderSettings.fog = false;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.72f, 0.74f, 0.78f);   // bright fill so nothing is ever dark, even in the shadow of a wall
            RenderSettings.skybox = null;
            var cam = Camera.main;
            if (cam != null) { cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.68f, 0.8f, 0.92f); }
        }

        static Material Flat(string name, Color c) => PrototypeSceneBuilder.Mat("Arena_" + name, c, "Universal Render Pipeline/Lit");

        static GameObject Block(string name, Vector3 center, Vector3 size, Material mat, Transform parent, Vector3? euler = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            go.transform.localScale = size;
            if (euler.HasValue) go.transform.localRotation = Quaternion.Euler(euler.Value);
            go.GetComponent<Renderer>().sharedMaterial = mat;
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.ContributeGI | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
            return go;
        }

        static void BuildFloorAndWalls()
        {
            var root = new GameObject("Sàn Và Tường").transform;
            // a faint grid, one line every 3 m, so distances and speed can be judged by eye
            const string gridPath = Root + "/Materials/Arena_Grid.asset";
            var grid = AssetDatabase.LoadAssetAtPath<Texture2D>(gridPath);
            if (grid == null)
            {
                grid = new Texture2D(64, 64, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear, name = "Arena_Grid" };
                for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
                    grid.SetPixel(x, y, x == 0 || y == 0 ? new Color(0.58f, 0.6f, 0.64f) : new Color(0.86f, 0.87f, 0.89f));
                grid.Apply(true);
                AssetDatabase.CreateAsset(grid, gridPath);
            }
            var floorMat = Flat("Floor", Color.white);
            floorMat.SetTexture("_BaseMap", grid);
            floorMat.SetTextureScale("_BaseMap", new Vector2(Half * 2f / 3f, Half * 2f / 3f));
            EditorUtility.SetDirty(floorMat);

            Block("Sàn", new Vector3(0f, -0.25f, 0f), new Vector3(Half * 2f, 0.5f, Half * 2f), floorMat, root);
            var wallMat = Flat("Wall", new Color(0.82f, 0.84f, 0.88f));
            float h = 5f, t = 1f, span = Half * 2f + t * 2f;
            Block("Tường Bắc", new Vector3(0f, h / 2f, Half + t / 2f), new Vector3(span, h, t), wallMat, root);
            Block("Tường Nam", new Vector3(0f, h / 2f, -Half - t / 2f), new Vector3(span, h, t), wallMat, root);
            Block("Tường Đông", new Vector3(Half + t / 2f, h / 2f, 0f), new Vector3(t, h, span), wallMat, root);
            Block("Tường Tây", new Vector3(-Half - t / 2f, h / 2f, 0f), new Vector3(t, h, span), wallMat, root);
        }

        static void BuildProps()
        {
            var root = new GameObject("Vật Thử").transform;
            var rock = Flat("Rock", new Color(0.55f, 0.5f, 0.45f));
            var stone = Flat("Stone", new Color(0.46f, 0.48f, 0.55f));
            var wood = Flat("Wood", new Color(0.55f, 0.38f, 0.22f));
            var warn = Flat("Warn", new Color(0.85f, 0.35f, 0.28f));

            // small rocks of rising height: the slime should crawl over the low ones and not over the high ones
            float[] heights = { 0.2f, 0.3f, 0.45f, 0.6f, 0.9f };
            for (int i = 0; i < heights.Length; i++)
                Block($"Đá Nhỏ {heights[i]:0.0#} m", new Vector3(5f + i * 2.5f, heights[i] / 2f, 4f), new Vector3(1f, heights[i], 1f), rock, root, new Vector3(0f, 17f * i, 0f));

            Block("Tường Cao 2 m (không leo được)", new Vector3(-8f, 1f, 6f), new Vector3(7f, 2f, 0.6f), stone, root);
            Block("Dốc 12 độ", new Vector3(0f, 0.85f, 15f), new Vector3(4f, 0.3f, 8.5f), wood, root, new Vector3(-12f, 0f, 0f));

            // ledges 1.5 m and 2.5 m high: a monkey leaps up to these
            Block("Bục Cao 1,5 m", new Vector3(14f, 0.75f, -8f), new Vector3(4f, 1.5f, 4f), stone, root);
            Block("Bục Cao 2,5 m", new Vector3(-14f, 1.25f, -8f), new Vector3(4f, 2.5f, 4f), stone, root);

            // a bridge 3 m up with open space under it: creatures below must not pop up onto it
            var bridge = new GameObject("Cầu").transform;
            bridge.SetParent(root, false);
            Block("Mặt Cầu", new Vector3(-10f, 3f, -20f), new Vector3(12f, 0.4f, 3f), wood, bridge);
            foreach (float px in new[] { -15.5f, -4.5f }) foreach (float pz in new[] { -21.2f, -18.8f })
                Block("Trụ Cầu", new Vector3(px, 1.5f, pz), new Vector3(0.5f, 3f, 0.5f), wood, bridge);

            // a low wall: a charging beetle that hits it is stunned
            Block("Tường Thấp (thử Húc đâm vách)", new Vector3(12f, 1f, -20f), new Vector3(9f, 2f, 0.6f), warn, root);

            // pillars all round: cover to hide behind, and a place for the weasel to lurk
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI * 2f / 8f;
                Block($"Cột {i + 1}", new Vector3(Mathf.Cos(a) * 20f, 1.5f, Mathf.Sin(a) * 20f), new Vector3(0.9f, 3f, 0.9f), stone, root);
            }
        }

        static void BuildFood(PrototypeSceneBuilder.SharedAssets shared)
        {
            var root = new GameObject("Thức Ăn").transform;
            for (int i = 0; i < 8; i++)
            {
                var n = (GameObject)PrefabUtility.InstantiatePrefab(shared.NutrientPrefab, root);
                float a = i * Mathf.PI * 2f / 8f;
                n.transform.position = new Vector3(Mathf.Cos(a) * 5f, 0.25f, Mathf.Sin(a) * 5f);
            }
        }
    }
}
