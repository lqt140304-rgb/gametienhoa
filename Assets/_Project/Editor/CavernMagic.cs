using System.Collections.Generic;
using Evolve.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Evolve.EditorTools
{
    /// <summary>
    /// Evolve -> Dress Cavern: Magic Light. Makes the cavern feel enchanted instead of lit by one plain beam:
    /// (1) the beam becomes a volume of light in dusty air (Evolve/LightBeam: soft edges, drifting streaks, twinkling specks) with a few thinner rays
    ///     leaking through the hole's rough edge; (2) the water gets a pool of light with slow rings and fine caustics under the beam, twinkling
    ///     sparkles, a faintly glowing shore and a hue that drifts from teal toward violet (Evolve/LakeWater); (3) soft glowing particles
    ///     (Evolve/GlowParticle): dust motes in the beam, fireflies wandering round the shore, big blurred spore lights, glints on the water;
    ///     (4) a cooler, bluer mood: ambient, fog and bloom. Rerunnable; run by Build Slime Cavern as well.
    /// </summary>
    public static class CavernMagic
    {
        const string Root = "Assets/_Project";
        const string RootName = "Cavern Magic";

        [MenuItem("Evolve/Dress Cavern: Magic Light")]
        public static void Dress()
        {
            var old = GameObject.Find(RootName);
            if (old != null) Object.DestroyImmediate(old);
            var root = new GameObject(RootName).transform;

            var glow = GlowMaterial("Cavern_Glow", 1.6f, 0f);
            var glowBlink = GlowMaterial("Cavern_GlowBlink", 2.2f, 1f);
            var bokeh = GlowMaterial("Cavern_GlowBokeh", 1.2f, 0f, 1.6f);

            Beams(root);
            Bounce(root);
            Particles(root, glow, glowBlink, bokeh);
            Water();
            Mood();
            LightsFollowSight();
            AssetDatabase.SaveAssets();
            Debug.Log("Ánh sáng huyền ảo: chùm sáng có bụi và vệt trôi, hồ có vũng sáng, lấp lánh và bờ phát quang, đom đóm và bụi sáng bay quanh bờ. Ctrl+S để lưu scene.");
        }

        // ------------------------------------------------------------------ the beam

        static void Beams(Transform root)
        {
            var mat = LoadOrCreate("Cavern_BeamMagic", "Evolve/LightBeam");
            if (mat == null) return;
            mat.SetColor("_Color", new Color(1f, 0.9f, 0.68f));
            mat.SetFloat("_Intensity", 0.42f);
            mat.SetFloat("_Streaks", 12f);
            mat.SetFloat("_Speed", 0.06f);
            mat.SetFloat("_Dust", 1f);
            EditorUtility.SetDirty(mat);

            var main = GameObject.Find("Light Beam");
            if (main != null)
            {
                main.GetComponent<MeshRenderer>().sharedMaterial = mat;
                // the spot light's cone must not leave a circle with a hard edge on the floor and walls: all penumbra, no flat middle
                foreach (var l in main.GetComponentsInChildren<Light>())
                {
                    // the main source spreads wide and softly, lighting the shore, the plants and the near walls, steady and gentle
                    l.innerSpotAngle = 0f;
                    l.spotAngle = 85f;
                    l.range = CavernMagicMath.BeamHeight * 1.6f;
                    l.intensity = 1.5f;
                    l.color = new Color(1f, 0.93f, 0.78f);
                    l.shadows = LightShadows.None;
                }
            }

            // a few thinner rays through the rough edge of the hole, slightly different in angle, so the light is not one clean tube
            var dir = CavernLayout.SunDirection;
            var top = CavernMagicMath.BeamTop;
            float height = CavernMagicMath.BeamHeight;
            var rays = new GameObject("Rays").transform;
            rays.SetParent(root, false);
            var rnd = new System.Random(19);
            var rayMat = new Material(mat) { name = "Cavern_BeamRay" };
            rayMat.SetFloat("_Intensity", 0.16f);
            rayMat.SetFloat("_Dust", 0.5f);
            for (int i = 0; i < 4; i++)
            {
                float a = i * Mathf.PI * 0.5f + (float)rnd.NextDouble() * 0.9f;
                float off = CavernLayout.HoleRadius * (0.45f + 0.4f * (float)rnd.NextDouble());
                var start = top + new Vector3(Mathf.Cos(a) * off, 0f, Mathf.Sin(a) * off);
                var rd = (dir + new Vector3((float)(rnd.NextDouble() - 0.5) * 0.06f, 0f, (float)(rnd.NextDouble() - 0.5) * 0.06f)).normalized;
                var go = new GameObject("Ray " + i);
                go.transform.SetParent(rays, false);
                go.transform.position = start;
                go.transform.rotation = Quaternion.FromToRotation(Vector3.down, rd);
                float r0 = 0.7f + 0.9f * (float)rnd.NextDouble();
                go.AddComponent<MeshFilter>().sharedMesh = RootMeshes.LightShaft(r0, r0 + 1.4f, height * 1.02f, 14, 8, 40 + i);
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = rayMat;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
            }
            AssetDatabase.DeleteAsset(Root + "/Materials/Cavern_BeamRay.mat");
            AssetDatabase.CreateAsset(rayMat, Root + "/Materials/Cavern_BeamRay.mat");
        }

        /// <summary>The beam lands on the water and the light bounces off it: one wide, soft, steady light over the pool that gently lifts the shore and everything
        /// round it (warm from the beam, a little teal from the water). Low intensity and no flicker.</summary>
        static void Bounce(Transform root)
        {
            var hit = CavernMagicMath.BeamHit;
            var go = new GameObject("Beam Bounce Light");
            go.transform.SetParent(root, false);
            go.transform.position = new Vector3(hit.x, CavernLayout.WaterLevel + 2.5f, hit.z);
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(0.78f, 0.92f, 0.88f);
            l.range = 42f;
            l.intensity = 1.1f;
            l.shadows = LightShadows.None;
            go.AddComponent<LightByVision>().AlwaysOnWhenSighted = true;   // fades with sight like every light, never culled by distance (the cave is small)
        }

        // ------------------------------------------------------------------ particles

        static void Particles(Transform root, Material glow, Material glowBlink, Material bokeh)
        {
            var top = CavernMagicMath.BeamTop;
            var dir = CavernLayout.SunDirection;

            // dust motes drifting in the beam (a cone from the hole down to the water, filled)
            var dust = NewSystem(root, "Beam Dust", glowBlink, 300, 11f, 17f, 0.03f, 0.075f);
            dust.transform.position = top;
            dust.transform.rotation = Quaternion.LookRotation(dir);
            var sh = dust.shape;
            sh.shapeType = ParticleSystemShapeType.Cone;
            sh.radius = CavernMagicMath.BeamTopRadius * 0.95f;
            sh.angle = Mathf.Atan(CavernMagicMath.BeamBottomExtra / CavernMagicMath.BeamHeight) * Mathf.Rad2Deg;
            sh.length = CavernMagicMath.BeamHeight;
            sh.radiusThickness = 1f;
            sh.rotation = Vector3.zero;
            var m = dust.main;
            m.startSpeed = 0f;
            m.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.95f, 0.75f, 0.9f), new Color(1f, 0.85f, 0.6f, 0.6f));
            m.gravityModifier = 0.004f;
            var em = dust.emission; em.rateOverTime = 20f;
            Wander(dust, 0.35f, 0.28f);
            Fade(dust);

            // fireflies: small, bright, blinking, wandering above the floor round the shore
            var fire = NewSystem(root, "Fireflies", glowBlink, 110, 16f, 26f, 0.13f, 0.22f);
            Ring(fire);
            var fm = fire.main;
            fm.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.35f);
            fm.startColor = new ParticleSystem.MinMaxGradient(new Color(0.78f, 1f, 0.35f, 1f), new Color(0.35f, 1f, 0.85f, 1f));
            var fe = fire.emission; fe.rateOverTime = 5.5f;
            Wander(fire, 1.1f, 0.22f);
            Fade(fire);

            // big soft blurred lights drifting up (bokeh): teal, violet and amber
            var spores = NewSystem(root, "Spore Lights", bokeh, 90, 14f, 22f, 0.35f, 0.8f);
            Ring(spores);
            var sm = spores.main;
            sm.startSpeed = 0f;
            sm.startColor = new ParticleSystem.MinMaxGradient(new Color(0.3f, 0.9f, 0.9f, 0.16f), new Color(0.75f, 0.45f, 1f, 0.16f));
            var se = spores.emission; se.rateOverTime = 5f;
            var sv = spores.velocityOverLifetime;
            sv.enabled = true; sv.space = ParticleSystemSimulationSpace.World; sv.x = 0f; sv.y = 0.18f; sv.z = 0f;
            Wander(spores, 0.4f, 0.15f);
            Fade(spores);

            // glints twinkling on the water
            var glints = NewSystem(root, "Water Glints", glow, 170, 1.2f, 2.8f, 0.05f, 0.13f);
            glints.transform.position = new Vector3(0f, CavernLayout.WaterLevel + 0.08f, 0f);
            glints.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);   // the circle lies on the water
            var gs = glints.shape;
            gs.shapeType = ParticleSystemShapeType.Circle;
            gs.radius = CavernMagicMath.GlintRadius;
            gs.radiusThickness = 1f;
            var gm = glints.main;
            gm.startSpeed = 0f;
            gm.startColor = new ParticleSystem.MinMaxGradient(new Color(0.75f, 1f, 1f, 0.9f), new Color(1f, 0.95f, 0.8f, 0.8f));
            var ge = glints.emission; ge.rateOverTime = 55f;
            var size = glints.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.5f, 1f), new Keyframe(1f, 0f)));
        }

        static ParticleSystem NewSystem(Transform parent, string name, Material mat, int max, float lifeLo, float lifeHi, float sizeLo, float sizeHi)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var m = ps.main;
            m.loop = true;
            m.prewarm = true;
            m.playOnAwake = true;
            m.simulationSpace = ParticleSystemSimulationSpace.World;
            m.maxParticles = max;
            m.startLifetime = new ParticleSystem.MinMaxCurve(lifeLo, lifeHi);
            m.startSize = new ParticleSystem.MinMaxCurve(sizeLo, sizeHi);
            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat;
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return ps;
        }

        /// <summary>A flattened ring round the lake (a donut lying on the ground).</summary>
        static void Ring(ParticleSystem ps)
        {
            ps.transform.position = new Vector3(0f, (CavernMagicMath.RingHeightLow + CavernMagicMath.RingHeightHigh) * 0.5f, 0f);
            ps.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Donut;
            sh.radius = CavernMagicMath.RingCentreRadius;
            sh.donutRadius = CavernMagicMath.RingTubeRadius;
            float flat = (CavernMagicMath.RingHeightHigh - CavernMagicMath.RingHeightLow) * 0.5f / CavernMagicMath.RingTubeRadius;
            sh.scale = new Vector3(1f, 1f, flat);
        }

        static void Wander(ParticleSystem ps, float strength, float frequency)
        {
            var n = ps.noise;
            n.enabled = true;
            n.strength = strength;
            n.frequency = frequency;
            n.scrollSpeed = 0.12f;
            n.damping = true;
            n.quality = ParticleSystemNoiseQuality.Low;
        }

        /// <summary>Fade in and out so particles never pop.</summary>
        static void Fade(ParticleSystem ps)
        {
            var c = ps.colorOverLifetime;
            c.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(1f, 0.8f), new GradientAlphaKey(0f, 1f) });
            c.color = g;
        }

        // ------------------------------------------------------------------ the water and the mood

        static void Water()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/Cavern_WaterLake.mat");
            var shader = Shader.Find("Evolve/LakeWater");
            if (mat != null && shader != null && mat.shader != shader) mat.shader = shader;   // an older material still pointing at another shader
            if (mat == null || !mat.HasProperty("_SparkleStrength"))
            {
                Debug.LogWarning("Scene hang này có mặt nước cũ (chưa có vật liệu Cavern_WaterLake dùng shader LakeWater): bỏ qua phần nước. Chạy Evolve → Build Slime Cavern (new map) để dựng lại hang với nước mới.");
                return;
            }
            mat.SetFloat("_SparkleStrength", 1.4f);
            mat.SetFloat("_SparkleScale", 2.4f);
            mat.SetColor("_ShoreGlow", new Color(0.06f, 0.55f, 0.6f, 1f));
            var hit = CavernMagicMath.BeamHit;
            mat.SetVector("_BeamPool", new Vector4(hit.x, CavernLayout.WaterLevel, hit.z, CavernMagicMath.PoolRadius));
            mat.SetColor("_BeamColor", new Color(1f, 0.88f, 0.62f));
            mat.SetFloat("_Shimmer", 0.6f);
            EditorUtility.SetDirty(mat);
        }

        /// <summary>The cavern's own lights (the beam's spot light, the glowing ore) fade with the slime's sight like every light of the living world:
        /// without eyes they no longer light up the whole cave.</summary>
        static void LightsFollowSight()
        {
            int n = 0;
            foreach (var l in Object.FindObjectsByType<Light>())
            {
                if (!(l.name.StartsWith("Beam Light") || l.name.StartsWith("Ore Light")) || l.GetComponent<LightByVision>() != null) continue;
                var lv = l.gameObject.AddComponent<LightByVision>();
                lv.AlwaysOnWhenSighted = l.name.StartsWith("Beam Light");   // the beam is seen from across the cave
                n++;
            }
            if (n > 0) Debug.Log($"{n} đèn của hang giờ mờ theo thị lực của slime.");
        }

        static void Mood()
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.07f, 0.085f, 0.13f);          // a little more, and bluer: shapes stay readable
            RenderSettings.fogColor = new Color(0.035f, 0.05f, 0.09f);
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(Root + "/Settings/CavernLook.asset");
            if (profile != null && profile.TryGet(out Bloom bloom))
            {
                bloom.intensity.Override(1.0f);       // the glows bloom into soft halos
                bloom.threshold.Override(0.8f);
                bloom.scatter.Override(0.75f);
                bloom.tint.Override(new Color(0.85f, 0.95f, 1f));
                EditorUtility.SetDirty(profile);
            }
        }

        // ------------------------------------------------------------------ materials

        static Material LoadOrCreate(string name, string shaderName)
        {
            var shader = Shader.Find(shaderName);
            if (shader == null) { Debug.LogWarning("Không tìm thấy shader " + shaderName + ": bỏ qua."); return null; }
            string path = $"{Root}/Materials/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { mat = new Material(shader) { name = name }; AssetDatabase.CreateAsset(mat, path); }
            mat.shader = shader;
            return mat;
        }

        static Material GlowMaterial(string name, float intensity, float blink, float halo = 1f)
        {
            var mat = LoadOrCreate(name, "Evolve/GlowParticle");
            if (mat == null) return new Material(Shader.Find("Sprites/Default"));   // never null: the particle systems need something
            mat.SetFloat("_Intensity", intensity);
            mat.SetFloat("_Blink", blink);
            mat.SetFloat("_Halo", halo);
            EditorUtility.SetDirty(mat);
            return mat;
        }
    }
}
