using Evolve.Survival;
using UnityEngine;

namespace Evolve.World
{
    /// <summary>
    /// The air of each zone: fog colour and sight distance, plus a few floating particles (at most
    /// <see cref="ZoneAtmosphereMath.MaxParticles"/>), blended smoothly by where the camera is.
    /// Sits on the camera (added at run time like UnderwaterTint). Runs after MonochromeVision (it owns the fog from
    /// here: black without eyes, the zone's colour with them) and steps aside while the camera is under water.
    /// F8 turns the particles off and on.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class ZoneAtmosphere : MonoBehaviour
    {
        const float BlendPerSecond = 0.4f;   // how fast the zone weights follow the camera (a teleport settles in about 2.5 s)
        const float BoxX = 30f, BoxY = 14f, BoxZ = 30f;
        const float LifetimeMin = 4f, LifetimeMax = 6f;
        const int ParticlesPerSystem = 50;   // 3 systems x 50 = 150 = ZoneAtmosphereMath.MaxParticles

        /// <summary>Influence of each zone (01..06) at a point.</summary>
        public static float[] WeightsAt(float x, float z)
        {
            var w = new float[ZoneLayout.Zones.Length];
            for (int i = 0; i < w.Length; i++) w[i] = ZoneLayout.Influence(i, x, z);
            return w;
        }

        public bool ParticlesEnabled { get; set; } = true;
        public AtmosphereProfile Current { get; private set; }

        enum Group { Falling, Rising, Drifting }

        static Group GroupOf(ParticleKind kind) => kind switch
        {
            ParticleKind.Spores => Group.Rising,
            ParticleKind.Droplets => Group.Drifting,
            ParticleKind.Mist => Group.Drifting,
            _ => Group.Falling // Dust, Leaves, Ash
        };

        float[] weights;
        MonochromeVision vision;
        UnderwaterTint underwater;
        ParticleSystem[] systems;
        Texture2D dot;
        Material material;

        void Awake()
        {
            systems = new ParticleSystem[3];
            for (int i = 0; i < 3; i++) systems[i] = MakeSystem(((Group)i).ToString());
        }

        void OnDestroy()
        {
            if (dot != null) Destroy(dot);
            if (material != null) Destroy(material);
        }

        ParticleSystem MakeSystem(string name)
        {
            if (material == null)
            {
                dot = new Texture2D(32, 32, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
                for (int y = 0; y < 32; y++)
                    for (int x = 0; x < 32; x++)
                    {
                        float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(16f, 16f)) / 16f;
                        float a = Mathf.Clamp01(1f - d);
                        dot.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
                    }
                dot.Apply();
                var shader = Shader.Find("Sprites/Default");
                if (shader == null) shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                material = new Material(shader) { mainTexture = dot };
            }
            var go = new GameObject("Atmosphere " + name);
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.maxParticles = ParticlesPerSystem;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(LifetimeMin, LifetimeMax); // short: the box refills quickly when the camera moves
            main.startSpeed = 0f;
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(BoxX, BoxY, BoxZ);
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(1f, 0.8f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.sharedMaterial = material;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            ps.Play();
            return ps;
        }

        void LateUpdate()
        {
            if (KeyPressed.F8())
            {
                ParticlesEnabled = !ParticlesEnabled;
                Debug.Log("Hạt không khí: " + (ParticlesEnabled ? "bật" : "tắt"));
            }
            if (vision == null) vision = FindAnyObjectByType<MonochromeVision>();
            if (underwater == null) underwater = GetComponent<UnderwaterTint>();
            float sight = vision != null ? vision.Sight : 1f;
            bool under = underwater != null && underwater.IsUnder;

            Vector3 p = transform.position;
            var target = WeightsAt(p.x, p.z);
            if (weights == null) weights = (float[])target.Clone(); // first frame: no easing
            else for (int i = 0; i < weights.Length; i++) weights[i] = Mathf.MoveTowards(weights[i], target[i], BlendPerSecond * Time.unscaledDeltaTime);
            Current = ZoneAtmosphereMath.Blend(weights);

            if (!under)
            {
                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.Linear;
                RenderSettings.fogStartDistance = 0f;
                RenderSettings.fogEndDistance = ZoneAtmosphereMath.FogEndFor(Current, sight);
                RenderSettings.fogColor = ZoneAtmosphereMath.FogColorFor(Current, sight);
            }
            UpdateParticles(sight, under);
        }

        bool wasOn;
        Group lastActive = Group.Falling;

        void UpdateParticles(float sight, bool under)
        {
            bool on = ParticlesEnabled && !under && sight >= 0.05f;
            if (!on)
            {
                if (wasOn) foreach (var ps in systems) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); // really off, nothing lingers
                wasOn = false;
                return;
            }
            if (!wasOn) foreach (var ps in systems) ps.Play();
            wasOn = true;

            float target = Mathf.Min(ZoneAtmosphereMath.ParticleBudget(sight, Current.Density), ParticlesPerSystem);
            float rate = target / ((LifetimeMin + LifetimeMax) * 0.5f);   // steady state: rate x lifetime = target live particles
            var active = GroupOf(Current.Particles);
            for (int i = 0; i < systems.Length; i++)
            {
                var ps = systems[i];
                bool mine = (Group)i == active;
                ps.transform.rotation = Quaternion.identity; // the box stays level whatever the camera's pitch and yaw
                var emission = ps.emission;
                emission.rateOverTime = mine ? rate : 0f;     // the other groups just fade out within a few seconds
                if (!mine) continue;
                var main = ps.main;
                main.startSize = Current.Size * Random.Range(0.8f, 1.2f);
                main.startColor = Current.ParticleColor;
                var vel = ps.velocityOverLifetime;
                float s = Current.Speed;
                switch (active)
                {
                    case Group.Falling: vel.x = 0f; vel.y = -s; vel.z = 0f; break;
                    case Group.Rising: vel.x = 0f; vel.y = s; vel.z = 0f; break;
                    default: vel.x = s * 0.6f; vel.y = 0f; vel.z = s * 0.4f; break;
                }
            }
            lastActive = active;
        }
    }
}
