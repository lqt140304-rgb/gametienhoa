using System.Collections.Generic;
using Evolve.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Evolve.EditorTools
{
    /// <summary>
    /// Gives each stream a proper source. Menu: Evolve -> Dress Lake: Spring.
    /// At the upstream end of every stream (where "Dress Lake: Water" made a round spring pool) a crescent of big mossy
    /// rocks rises behind the pool, a small waterfall pours from the tallest one into it, with foam where it lands and a
    /// light mist drifting up. Visual only; safe to run again. Run after Water and Ground.
    /// </summary>
    public static class LakeSpring
    {
        const string Root = "Assets/_Project";
        const string DressedName = "Lake Springs (Dressed)";

        [MenuItem("Evolve/Dress Lake: Spring")]
        public static void Dress()
        {
            PrototypeSceneBuilder.EnsureFolder(Root + "/Materials");
            PrototypeSceneBuilder.EnsureFolder(Root + "/Models");
            WaterSurface.Refresh();
            Physics.SyncTransforms();

            var old = GameObject.Find(DressedName);
            if (old != null) Object.DestroyImmediate(old);
            var root = new GameObject(DressedName).transform;
            root.gameObject.AddComponent<DetailCulling>();

            var rocks = new List<CombineInstance>();
            var caps = new List<CombineInstance>();
            var variants = new List<Mesh>();
            var variantCaps = new List<Mesh>();
            for (int v = 0; v < 4; v++) { variants.Add(GroundMeshes.Rock(700 + v, out var cap)); variantCaps.Add(cap); }
            var rnd = new System.Random(808);

            var waterfallMaterial = WaterfallMaterial();
            var foamMaterial = Foam();
            int springs = 0;

            foreach (var stream in new List<WaterSurface>(WaterSurface.All))
            {
                if (stream == null || stream.Shape != WaterShape.Stream || stream.StreamPath == null || stream.StreamPath.Length < 2) continue;
                var origin = new Vector2(stream.transform.position.x, stream.transform.position.z);
                Vector2 a = stream.StreamPath[0] + origin, b = stream.StreamPath[stream.StreamPath.Length - 1] + origin;
                LakeLayout.StreamEnds(a, b, out Vector2 source, out Vector2 flow);
                float poolRadius = Mathf.Max(4.5f, stream.StreamHalfWidth * 1.9f);
                Vector2 side = new Vector2(-flow.y, flow.x);
                Vector2 cliffCentre = source - flow * (poolRadius + 2.6f);

                // A crescent of big rocks behind the pool: the middle one is the tallest (the water falls from it).
                float topY = 0f; Vector2 fallAt = cliffCentre;
                for (int k = -2; k <= 2; k++)
                {
                    float size = k == 0 ? 5.4f : 3.0f + (float)rnd.NextDouble() * 1.6f;
                    Vector2 p = cliffCentre + side * (k * 3.4f) - flow * (Mathf.Abs(k) * 1.1f) + new Vector2((float)rnd.NextDouble() - 0.5f, (float)rnd.NextDouble() - 0.5f);
                    float ground = LakeGround.GroundY(p.x, p.y);
                    int variant = rnd.Next(variants.Count);
                    var m = Matrix4x4.TRS(new Vector3(p.x, ground, p.y), Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f), new Vector3(size, size * 1.8f, size));
                    rocks.Add(new CombineInstance { mesh = variants[variant], transform = m });
                    caps.Add(new CombineInstance { mesh = variantCaps[variant], transform = m });
                    if (k == 0) { topY = ground + 0.33f * size * 1.8f; fallAt = p; }
                }
                // a few smaller rocks round the pool's edge
                for (int i = 0; i < 7; i++)
                {
                    float angle = ((float)rnd.NextDouble() * 1.6f - 0.8f) * Mathf.PI + Mathf.Atan2(-flow.y, -flow.x) ;
                    Vector2 p = source + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (poolRadius + 0.8f + (float)rnd.NextDouble() * 1.4f);
                    if (LakeGround.Wet(new List<WaterSurface>(WaterSurface.All), p)) continue;
                    float size = 0.5f + (float)rnd.NextDouble() * 0.9f;
                    int variant = rnd.Next(variants.Count);
                    var m = Matrix4x4.TRS(new Vector3(p.x, LakeGround.GroundY(p.x, p.y), p.y), Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f), new Vector3(size, size * 1.2f, size));
                    rocks.Add(new CombineInstance { mesh = variants[variant], transform = m });
                    caps.Add(new CombineInstance { mesh = variantCaps[variant], transform = m });
                }

                // The waterfall: from near the top of the tallest rock, falling toward the pool.
                float poolY = 0.06f;
                float lip = topY * 0.82f;
                Vector2 lipAt = fallAt + flow * 1.9f;                  // on the pool-side slope of the rock
                var fall = new GameObject("Waterfall");
                fall.transform.SetParent(root, false);
                fall.transform.position = new Vector3(lipAt.x, lip, lipAt.y);
                fall.transform.rotation = Quaternion.LookRotation(new Vector3(flow.x, 0f, flow.y), Vector3.up);
                var wf = fall.AddComponent<Waterfall>();
                wf.Width = 2.6f;
                wf.Height = Mathf.Max(1.5f, lip - poolY);
                fall.GetComponent<MeshRenderer>().sharedMaterial = waterfallMaterial;

                // Foam where it lands, and a soft mist rising from there.
                Vector3 landing = new Vector3(lipAt.x + flow.x * 0.55f, poolY, lipAt.y + flow.y * 0.55f);
                var splash = new GameObject("Splash Foam");
                splash.transform.SetParent(root, false);
                splash.transform.position = landing + Vector3.up * 0.04f;
                splash.AddComponent<MeshFilter>().sharedMesh = WaterMeshes.Disc(1.7f, 0.5f);
                var splashRenderer = splash.AddComponent<MeshRenderer>();
                splashRenderer.sharedMaterial = foamMaterial;
                splashRenderer.shadowCastingMode = ShadowCastingMode.Off;
                splashRenderer.receiveShadows = false;
                MakeMist(root, landing);
                springs++;
            }

            LakeGround.AddCombined(root, "Spring Rocks", rocks, LakeGround.OpaqueMaterial("Lake_Rock", new Color(0.40f, 0.41f, 0.39f), 0.15f, false));
            LakeGround.AddCombined(root, "Spring Rock Moss", caps, LakeGround.OpaqueMaterial("Lake_RockMoss", new Color(0.30f, 0.50f, 0.22f), 0.1f, false));

            EditorSceneManager.MarkAllScenesDirty();
            AssetDatabase.SaveAssets();
            Debug.Log($"Đã dựng {springs} suối nguồn (vách đá, thác nước, bọt, hơi nước). Bấm Ctrl+S để lưu scene.");
        }

        static Material Foam()
        {
            var foam = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/Lake_Foam.mat");
            return foam != null ? foam : LakeGround.OpaqueMaterial("Lake_SplashFallback", Color.white, 0.1f, true);
        }

        /// <summary>Translucent white-cyan water with the ripple pattern (the pattern runs down the fall, see Waterfall).</summary>
        static Material WaterfallMaterial()
        {
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Models/WaterRipples_v2.png");
            string path = Root + "/Materials/Lake_Waterfall.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "Lake_Waterfall" };
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", new Color(0.78f, 0.96f, 1f, 0.5f));
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.SetFloat("_Cull", 0f);              // seen from both sides
            mat.SetFloat("_Smoothness", 0.8f);
            if (normal != null) { mat.SetTexture("_BumpMap", normal); mat.SetFloat("_BumpScale", 0.9f); mat.EnableKeyword("_NORMALMAP"); }
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", new Color(0.10f, 0.16f, 0.17f));
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = (int)RenderQueue.Transparent + 3;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static void MakeMist(Transform parent, Vector3 at)
        {
            var go = new GameObject("Spring Mist");
            go.transform.SetParent(parent, false);
            go.transform.position = at + Vector3.up * 0.4f;
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.prewarm = true;
            main.startLifetime = 3.5f;
            main.startSpeed = 0.25f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.8f, 1.6f);
            main.startColor = new Color(0.85f, 0.97f, 1f, 0.12f);
            main.maxParticles = 40;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = -0.02f;
            var emission = ps.emission;
            emission.rateOverTime = 9f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 1.3f;
            var colour = ps.colorOverLifetime;
            colour.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                             new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(0f, 1f) });
            colour.color = gradient;
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.6f), new Keyframe(1f, 1.5f)));

            var tex = LakeWater.PaintedTexture(Root + "/Models/SporeDot.png", 32, 32, (u, v) =>
            {
                float d = new Vector2(u - 0.5f, v - 0.5f).magnitude * 2f;
                float a = Mathf.Pow(Mathf.Clamp01(1f - d), 2f);
                return new Color(a, a, a, a);
            });
            string path = Root + "/Materials/Lake_Mist.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")) { name = "Lake_Mist" };
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetTexture("_BaseMap", tex);
            mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);                         // ordinary alpha: mist, not light
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = (int)RenderQueue.Transparent + 25;
            EditorUtility.SetDirty(mat);
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = mat;
        }
    }
}
