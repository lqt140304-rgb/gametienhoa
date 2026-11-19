using System.Collections.Generic;
using System.IO;
using Evolve.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Evolve.EditorTools
{
    /// <summary>
    /// The open-sky pit (spec 2026-11-19-open-sky-pit-design). Menu: Evolve → Dress Lake: Open Sky. The roof of roots, the earth sheet, the
    /// dome, the hanging roots, the vault pieces and the slanting light shafts are hidden (not deleted); the whole sky and a real sun take their
    /// place; and the surface world (hills and a treeline) is built beyond the rim so looking up from the bottom shows the world above.
    /// The rim wall itself (55 m, steep) comes from TerrainShape: run Dress Lake: Terrain (or the whole rebuild) for it.
    /// "Restore Lake Roof (undo)" puts everything back. Only for the Underroot_Lake scene. Run it after the other lake steps (it is the last in the rebuild).
    /// </summary>
    public static class LakeOpenSky
    {
        const string Root = "Assets/_Project";
        const string ScenePath = Root + "/Scenes/Underroot_Lake.unity";
        const string BackupPath = Root + "/Settings/LakeOpenSkyBackup.json";
        const string SkyPath = Root + "/Materials/LakeSky.mat";
        const string CanopyName = "Lake Canopy (Dressed)";
        const string SurfaceName = "Surface World";
        const int Seed = 1337;

        [System.Serializable]
        class Backup
        {
            public bool saved;
            public Vector3 sunEuler;
            public Color sunColor;
            public float sunIntensity;
            public int sunShadows;
            public float sunShadowStrength;
            public string skyboxPath;
            public int ambientMode;
            public Color ambientLight;
            public float ambientIntensity;
            public bool fog;
            public int fogMode;
            public Color fogColor;
            public float fogStart, fogEnd, fogDensity;
            public string urpPath;
            public float urpShadowDistance;
            public List<string> hiddenPaths = new List<string>();   // "rootIndex/child/child" of what was hidden
            public List<string> hiddenNames = new List<string>();   // their names, to check a path still points at the same thing
        }

        // ---------------------------------------------------------------- Dress

        [MenuItem("Evolve/Bản đồ cũ (Underroot)/Dress Lake: Open Sky")]
        public static void Dress()
        {
            if (EditorSceneManager.GetActiveScene().path != ScenePath)
            {
                Debug.LogWarning("Mở scene Underroot_Lake trước (menu này chỉ đổi cảnh hồ, không đổi Prototype_01).");
                return;
            }
            var scene = SceneManager.GetActiveScene();
            var sun = FindSun();
            if (sun == null) { Debug.LogWarning("Không có đèn Directional trong scene: bỏ qua."); return; }

            var backup = Load();
            if (!backup.saved) { backup = Capture(sun, scene); Save(backup); }

            // ---- 1. the roof goes away (hidden, never deleted) ----
            var toHide = FindToHide(scene);
            int hidden = 0;
            foreach (var go in toHide) if (go.activeSelf) { go.SetActive(false); hidden++; }

            // ---- 2. the sky ----
            PrototypeSceneBuilder.EnsureFolder(Root + "/Materials");
            var sky = AssetDatabase.LoadAssetAtPath<Material>(SkyPath);
            if (sky == null)
            {
                var shader = Shader.Find("Skybox/Procedural");
                if (shader == null) { Debug.LogError("Không tìm thấy shader Skybox/Procedural."); return; }
                sky = new Material(shader);
                AssetDatabase.CreateAsset(sky, SkyPath);
            }
            sky.SetFloat("_SunSize", 0.045f);
            sky.SetFloat("_SunSizeConvergence", 5f);
            sky.SetFloat("_AtmosphereThickness", 1.0f);
            sky.SetColor("_SkyTint", new Color(0.5f, 0.55f, 0.62f));
            sky.SetColor("_GroundColor", new Color(0.35f, 0.4f, 0.42f));
            sky.SetFloat("_Exposure", 1.25f);
            EditorUtility.SetDirty(sky);
            RenderSettings.skybox = sky;
            RenderSettings.ambientMode = AmbientMode.Skybox;
            RenderSettings.ambientIntensity = 1f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.62f, 0.72f, 0.82f);
            RenderSettings.fogStartDistance = 60f;
            RenderSettings.fogEndDistance = 600f;

            // ---- 3. the sun: bright, warm, high enough to light the floor, low enough that the wall casts shadow ----
            sun.transform.rotation = Quaternion.Euler(50f, 140f, 0f);
            sun.color = new Color(1f, 0.96f, 0.88f);
            sun.intensity = 1.25f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.9f;
            RenderSettings.sun = sun;
            var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urp != null)
            {
                urp.shadowDistance = Mathf.Max(urp.shadowDistance, 180f);   // the shadow of the 55 m wall reaches far across the floor
                EditorUtility.SetDirty(urp);
            }
            DynamicGI.UpdateEnvironment();

            // ---- 4. the world above the rim ----
            BuildSurface(scene);

            EditorSceneManager.MarkAllScenesDirty();
            AssetDatabase.SaveAssets();
            Debug.Log($"Đã mở trần hố: ẩn {hidden} vật thể của mái và vòm, đặt bầu trời và nắng, dựng thế giới phía trên miệng hố. Bấm Ctrl+S để lưu scene. Địa hình (vách cao 55 m) cần chạy Dress Lake: Terrain.");
        }

        // ---------------------------------------------------------------- Restore

        [MenuItem("Evolve/Bản đồ cũ (Underroot)/Restore Lake Roof (undo)")]
        public static void Restore()
        {
            if (EditorSceneManager.GetActiveScene().path != ScenePath)
            {
                Debug.LogWarning("Mở scene Underroot_Lake trước.");
                return;
            }
            var b = Load();
            if (!b.saved) { Debug.LogWarning("Chưa có bản sao lưu: chưa chạy Dress Lake: Open Sky."); return; }
            var scene = SceneManager.GetActiveScene();

            int shown = 0;
            for (int i = 0; i < b.hiddenPaths.Count; i++)
            {
                var go = ByPath(scene, b.hiddenPaths[i]);
                if (go != null && go.name == b.hiddenNames[i]) { go.SetActive(true); shown++; }
            }
            var surface = GameObject.Find(SurfaceName);
            if (surface != null) Object.DestroyImmediate(surface);

            var sun = FindSun();
            if (sun != null)
            {
                sun.transform.rotation = Quaternion.Euler(b.sunEuler);
                sun.color = b.sunColor;
                sun.intensity = b.sunIntensity;
                sun.shadows = (LightShadows)b.sunShadows;
                sun.shadowStrength = b.sunShadowStrength;
            }
            RenderSettings.skybox = string.IsNullOrEmpty(b.skyboxPath) ? null : AssetDatabase.LoadAssetAtPath<Material>(b.skyboxPath);
            RenderSettings.ambientMode = (AmbientMode)b.ambientMode;
            RenderSettings.ambientLight = b.ambientLight;
            RenderSettings.ambientIntensity = b.ambientIntensity;
            RenderSettings.fog = b.fog;
            RenderSettings.fogMode = (FogMode)b.fogMode;
            RenderSettings.fogColor = b.fogColor;
            RenderSettings.fogStartDistance = b.fogStart;
            RenderSettings.fogEndDistance = b.fogEnd;
            RenderSettings.fogDensity = b.fogDensity;
            var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urp != null && AssetDatabase.GetAssetPath(urp) == b.urpPath) { urp.shadowDistance = b.urpShadowDistance; EditorUtility.SetDirty(urp); }
            DynamicGI.UpdateEnvironment();
            b.saved = false;
            Save(b);
            EditorSceneManager.MarkAllScenesDirty();
            AssetDatabase.SaveAssets();
            Debug.Log($"Đã trả lại mái và ánh sáng hang ({shown} vật thể hiện lại). Bấm Ctrl+S để lưu scene.");
        }

        // ---------------------------------------------------------------- what is hidden

        static readonly HashSet<string> HideAnywhere = new HashSet<string>
        {
            "Hanging Roots", "Root Ceiling", "Roof (old lattice)", "Vault Ribs", "Vault Pillars", "Vault Hanging Roots", "Light Shaft"
        };

        static bool ShouldHide(Transform t)
        {
            string n = t.name;
            if (HideAnywhere.Contains(n)) return true;
            if (t.parent == null && n == "Dome") return true;   // the inward dome of the cave (top-level only: a hut's dome stays)
            if (t.parent != null && t.parent.name == CanopyName && (n == "Roof" || n == "Ceiling Earth")) return true;   // not a hut's roof
            return false;
        }

        static List<GameObject> FindToHide(Scene scene)
        {
            var list = new List<GameObject>();
            void Walk(Transform t)
            {
                if (ShouldHide(t)) { list.Add(t.gameObject); return; }
                for (int i = 0; i < t.childCount; i++) Walk(t.GetChild(i));
            }
            foreach (var root in scene.GetRootGameObjects()) Walk(root.transform);
            return list;
        }

        static string PathOf(Scene scene, Transform t)
        {
            var parts = new List<int>();
            for (var c = t; c != null; c = c.parent) parts.Add(c.parent != null ? c.GetSiblingIndex() : System.Array.IndexOf(scene.GetRootGameObjects(), c.gameObject));
            parts.Reverse();
            return string.Join("/", parts);
        }

        static GameObject ByPath(Scene scene, string path)
        {
            var parts = path.Split('/');
            var roots = scene.GetRootGameObjects();
            if (!int.TryParse(parts[0], out int r) || r < 0 || r >= roots.Length) return null;
            var t = roots[r].transform;
            for (int i = 1; i < parts.Length; i++)
            {
                if (!int.TryParse(parts[i], out int k) || k < 0 || k >= t.childCount) return null;
                t = t.GetChild(k);
            }
            return t.gameObject;
        }

        // ---------------------------------------------------------------- backup

        static Backup Capture(Light sun, Scene scene)
        {
            var b = new Backup
            {
                saved = true,
                sunEuler = sun.transform.rotation.eulerAngles,
                sunColor = sun.color,
                sunIntensity = sun.intensity,
                sunShadows = (int)sun.shadows,
                sunShadowStrength = sun.shadowStrength,
                skyboxPath = RenderSettings.skybox != null ? AssetDatabase.GetAssetPath(RenderSettings.skybox) : "",
                ambientMode = (int)RenderSettings.ambientMode,
                ambientLight = RenderSettings.ambientLight,
                ambientIntensity = RenderSettings.ambientIntensity,
                fog = RenderSettings.fog,
                fogMode = (int)RenderSettings.fogMode,
                fogColor = RenderSettings.fogColor,
                fogStart = RenderSettings.fogStartDistance,
                fogEnd = RenderSettings.fogEndDistance,
                fogDensity = RenderSettings.fogDensity,
            };
            var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urp != null) { b.urpPath = AssetDatabase.GetAssetPath(urp); b.urpShadowDistance = urp.shadowDistance; }
            foreach (var go in FindToHide(scene))
                if (go.activeSelf) { b.hiddenPaths.Add(PathOf(scene, go.transform)); b.hiddenNames.Add(go.name); }
            return b;
        }

        static Backup Load()
        {
            if (!File.Exists(BackupPath)) return new Backup();
            try { return JsonUtility.FromJson<Backup>(File.ReadAllText(BackupPath)) ?? new Backup(); }
            catch { return new Backup(); }
        }

        static void Save(Backup b)
        {
            PrototypeSceneBuilder.EnsureFolder(Root + "/Settings");
            File.WriteAllText(BackupPath, JsonUtility.ToJson(b, true));
            AssetDatabase.ImportAsset(BackupPath);
        }

        static Light FindSun()
        {
            if (RenderSettings.sun != null) return RenderSettings.sun;
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional) return l;
            return null;
        }

        // ---------------------------------------------------------------- the surface world

        static void BuildSurface(Scene scene)
        {
            var old = GameObject.Find(SurfaceName);
            if (old != null) Object.DestroyImmediate(old);
            var root = new GameObject(SurfaceName);
            SceneManager.MoveGameObjectToScene(root, scene);

            var ground = LakeGround.OpaqueMaterial("Lake_SurfaceGround", new Color(0.17f, 0.27f, 0.13f), 0.05f, false);
            var trees = LakeGround.OpaqueMaterial("Lake_SurfaceTrees", new Color(0.07f, 0.16f, 0.09f), 0.05f, false);

            var g = new GameObject("Ground");
            g.transform.SetParent(root.transform, false);
            g.AddComponent<MeshFilter>().sharedMesh = SurfaceMesh();
            var gr = g.AddComponent<MeshRenderer>();
            gr.sharedMaterial = ground;
            gr.shadowCastingMode = ShadowCastingMode.Off;
            g.isStatic = true;

            var t = new GameObject("Treeline");
            t.transform.SetParent(root.transform, false);
            t.AddComponent<MeshFilter>().sharedMesh = TreelineMesh();
            var tr = t.AddComponent<MeshRenderer>();
            tr.sharedMaterial = trees;
            tr.shadowCastingMode = ShadowCastingMode.Off;
            t.isStatic = true;
        }

        /// <summary>The ground beyond the rim: square rings from the rim (250 m) out to 650 m, spaced wider and wider.</summary>
        static Mesh SurfaceMesh()
        {
            const int rings = 24, around = 192;
            float half = WorldScale.ZoneSize * 0.5f;
            var verts = new List<Vector3>((rings + 1) * around);
            for (int r = 0; r <= rings; r++)
            {
                float d = half + Mathf.Pow(r / (float)rings, 1.6f) * 400f;
                for (int k = 0; k < around; k++)
                {
                    Vector2 p = OnSquare(d, k / (float)around * 4f);
                    verts.Add(new Vector3(p.x, SurfaceWorld.HeightAt(p.x, p.y, Seed), p.y));
                }
            }
            var tris = new List<int>();
            for (int r = 0; r < rings; r++)
                for (int k = 0; k < around; k++)
                {
                    int a = r * around + k, b = r * around + (k + 1) % around, c = (r + 1) * around + k, d = (r + 1) * around + (k + 1) % around;
                    tris.Add(a); tris.Add(c); tris.Add(b);   // faces up
                    tris.Add(b); tris.Add(c); tris.Add(d);
                }
            var mesh = new Mesh { name = "Lake_SurfaceGround", indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>A point on the square of half-size d; s runs 0..4 once round it, north side first.</summary>
        static Vector2 OnSquare(float d, float s)
        {
            int side = Mathf.Clamp((int)s, 0, 3);
            float t = s - side;
            switch (side)
            {
                case 0: return new Vector2(Mathf.Lerp(-d, d, t), d);
                case 1: return new Vector2(d, Mathf.Lerp(d, -d, t));
                case 2: return new Vector2(Mathf.Lerp(d, -d, t), -d);
                default: return new Vector2(-d, Mathf.Lerp(-d, d, t));
            }
        }

        /// <summary>About 700 dark conifer cones along and beyond the rim, so the horizon has a ragged treeline against the sky.</summary>
        static Mesh TreelineMesh()
        {
            var cone = Cone(6);
            var parts = new List<CombineInstance>();
            var rnd = new System.Random(77);
            float half = WorldScale.ZoneSize * 0.5f;
            for (int i = 0; i < 700; i++)
            {
                float d = half + 6f + (float)rnd.NextDouble() * (float)rnd.NextDouble() * 330f;   // thicker near the rim
                Vector2 p = OnSquare(d, (float)rnd.NextDouble() * 4f);
                if (p.y > 0f && Mathf.Abs(p.x - ZoneLayout.NorthEntry.x) < ZoneLayout.NorthEntryHalfWidth + 24f) continue;   // the canyon of the north entry stays clear
                float h = 9f + (float)rnd.NextDouble() * 12f, w = 2.6f + (float)rnd.NextDouble() * 2.6f;
                float y = SurfaceWorld.HeightAt(p.x, p.y, Seed);
                parts.Add(new CombineInstance { mesh = cone, transform = Matrix4x4.TRS(new Vector3(p.x, y - 0.5f, p.y), Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f), new Vector3(w, h, w)) });
            }
            var mesh = new Mesh { name = "Lake_SurfaceTrees", indexFormat = IndexFormat.UInt32 };
            mesh.CombineMeshes(parts.ToArray(), true, true);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>A unit cone: radius 1, height 1, flat base, n sides.</summary>
        static Mesh Cone(int sides)
        {
            var verts = new List<Vector3>();
            var tris = new List<int>();
            for (int i = 0; i < sides; i++)
            {
                float a0 = i / (float)sides * Mathf.PI * 2f, a1 = (i + 1) / (float)sides * Mathf.PI * 2f;
                int b = verts.Count;
                verts.Add(new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0)));
                verts.Add(new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1)));
                verts.Add(new Vector3(0f, 1f, 0f));
                tris.Add(b); tris.Add(b + 2); tris.Add(b + 1);
            }
            var m = new Mesh { name = "Cone" };
            m.SetVertices(verts);
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            return m;
        }
    }
}
