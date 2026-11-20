using Evolve.Organs;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;

namespace Evolve.World
{
    /// <summary>
    /// "No eyes" vision. Without eyes the world is dark and grey: only a small area around the
    /// character is lit (a point light carried by the player), so the visible range is centred on the
    /// character and does not depend on where the camera is. The vibration scan (Reveal) lights up
    /// a wide area for a moment. Growing eyes gradually bring back colour, daylight and view distance.
    /// Follows the player's OrganHost. Put on a GameObject with a global Volume.
    /// </summary>
    [RequireComponent(typeof(Volume))]
    public class MonochromeVision : MonoBehaviour
    {
        [Tooltip("0 = blind (grey, dark, short range), 1 = full colour vision")]
        [Range(0, 1)] [SerializeField] float sight;
        [Tooltip("Player whose organs decide the vision")]
        [SerializeField] OrganHost host;

        [Header("Blind")]
        [FormerlySerializedAs("blindFogEnd")]
        [Tooltip("Radius around the character that is lit while blind, in metres")]
        [SerializeField] float blindSightRadius = 6f;
        [Tooltip("Brightness of the light around the character while blind")]
        [SerializeField] float blindLightIntensity = 8f;

        [Header("Scan (vibration wave)")]
        [Tooltip("How high above the character the scan light hangs")]
        [SerializeField] float scanLightHeight = 8f;

        [Header("With eyes")]
        [SerializeField] float eyesFogEnd = 120f;
        [SerializeField] Color fogColor = new Color(0.08f, 0.08f, 0.08f);

        // Read by the Evolve/LakeWater shader: without eyes the water is only visible near the character (and in a scan).
        static readonly int BlindId = Shader.PropertyToID("_EvolveBlind");
        static readonly int SightCenterId = Shader.PropertyToID("_EvolveSightCenter");
        static readonly int WaveId = Shader.PropertyToID("_EvolveWave");
        static readonly int WaveFadeId = Shader.PropertyToID("_EvolveWaveFade");

        static readonly Color LightColor = new Color(0.85f, 0.95f, 1f);

        ColorAdjustments color;
        Light sun, sightLight, scanLight;
        Transform sonarShell;          // the sound wave: a thin expanding shell that lights nothing, it only shows where it cuts the ground and the things on it
        Material sonarMat;
        Vector3 waveOrigin;
        Camera cam;
        float sunBase, origReflection;
        Color origBackground, origAmbient;
        AmbientMode origAmbientMode;
        bool restoredSky = true;

        // Temporary wide reveal from the vibration scan.
        float revealStart = -1f, revealRadius, revealExpand, revealHold, revealFade;

        /// <summary>Phát ra mỗi khi Sóng Dò (phím C) được kích hoạt: (vị trí nguồn, bán kính tối đa).</summary>
        public static event System.Action<Vector3, float> SonarPulsed;

        void Awake()
        {
            var volume = GetComponent<Volume>();
            if (!volume.profile.TryGet(out color))
                color = volume.profile.Add<ColorAdjustments>(true);
            color.saturation.overrideState = true;

            cam = Camera.main;
            if (cam != null) origBackground = cam.backgroundColor;
            foreach (var l in FindObjectsByType<Light>())
                if (l.type == LightType.Directional) { sun = l; sunBase = l.intensity; break; }
            origAmbientMode = RenderSettings.ambientMode;
            origAmbient = RenderSettings.ambientLight;
            origReflection = RenderSettings.reflectionIntensity;

            Transform carrier = host != null ? host.transform : transform;
            sightLight = MakeLight("Sight Light", carrier, new Vector3(0f, 1.6f, 0f));
            scanLight = MakeLight("Scan Light", carrier, new Vector3(0f, scanLightHeight, 0f));
            scanLight.enabled = false;
            CreateSonar();

            Apply();
            if (host != null) host.Changed += OnOrgansChanged;
        }

        static Light MakeLight(string name, Transform parent, Vector3 localPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.shadows = LightShadows.None;
            l.color = LightColor;
            return l;
        }

        void CreateSonar()
        {
            var shader = Shader.Find("Evolve/SonarShell");
            if (shader == null) return;
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Sonar Wave";
            Destroy(go.GetComponent<Collider>());
            sonarMat = new Material(shader) { name = "SonarShell (runtime)" };
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = sonarMat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            go.SetActive(false);
            sonarShell = go.transform;
        }

        void OnDestroy()
        {
            if (sonarShell != null) Destroy(sonarShell.gameObject);
            Shader.SetGlobalVector(WaveId, Vector4.zero);
            Shader.SetGlobalFloat(WaveFadeId, 0f);
            if (host != null) host.Changed -= OnOrgansChanged;
            // Put the scene lighting back the way it was.
            if (sun != null) sun.intensity = sunBase;
            RenderSettings.ambientMode = origAmbientMode;
            RenderSettings.ambientLight = origAmbient;
            RenderSettings.reflectionIntensity = origReflection;
            if (cam != null) cam.backgroundColor = origBackground;
            Shader.SetGlobalFloat(BlindId, 0f); // the water shader shows normally again
            if (sightLight != null) Destroy(sightLight.gameObject);
            if (scanLight != null) Destroy(scanLight.gameObject);
        }

        void OnOrgansChanged() => SetSight(host.Sight);

        public float Sight => sight;

        void Update()
        {
            if (Evolve.Survival.KeyPressed.V()) SetSight(sight >= 1f ? 0f : 1f);
            UpdateScan();
            Transform carrier = host != null ? host.transform : transform;
            Vector3 p = carrier.position;
            // the blind slime feels only its small surroundings; the wave is a separate thing (a ring spreading out, not a lit disc)
            Shader.SetGlobalVector(SightCenterId, new Vector4(p.x, p.y, p.z, blindSightRadius));
            UpdateWave();
        }

        /// <summary>0 = blind, 1 = full colour vision; above 1 (levelled eyes) only extends view range.</summary>
        public void SetSight(float value)
        {
            value = Mathf.Max(0f, value);
            if (Mathf.Approximately(value, sight)) return;
            sight = value;
            Apply();
        }

        /// <summary>
        /// Throw light out to <paramref name="radius"/> metres around the character: the lit area spreads out over
        /// <paramref name="expand"/> seconds, stays for <paramref name="hold"/>, then fades over <paramref name="fade"/>.
        /// </summary>
        public void Reveal(float radius, float expand, float hold, float fade)
        {
            waveOrigin = (host != null ? host.transform : transform).position;
            revealStart = Time.time;
            revealRadius = radius;
            revealExpand = Mathf.Max(0.01f, expand);
            revealHold = hold;
            revealFade = Mathf.Max(0.01f, fade);
            SonarPulsed?.Invoke(waveOrigin, revealRadius);
        }

        /// <summary>Current radius of the scan light (0 when none).</summary>
        float RevealDistance()
        {
            if (revealStart < 0f) return 0f;
            float t = Time.time - revealStart;
            if (t < revealExpand) return Mathf.Lerp(0f, revealRadius, t / revealExpand);
            t -= revealExpand;
            if (t < revealHold) return revealRadius;
            t -= revealHold;
            if (t < revealFade) return Mathf.Lerp(revealRadius, 0f, t / revealFade);
            revealStart = -1f;
            return 0f;
        }

        void UpdateScan()
        {
            // no light is thrown any more: the scan is a sound wave (UpdateWave), the old wide scan light stays off
            if (scanLight != null && scanLight.enabled) scanLight.enabled = false;
        }

        /// <summary>The sound wave: its front radius and fade go to the shaders (things light up only while the ring passes) and to the shell that draws it.</summary>
        void UpdateWave()
        {
            bool on = revealStart >= 0f;          // with eyes too: the scan still works and still shows the wave
            float t = Time.time - revealStart;
            float r = on ? SonarWaveMath.Radius(t, revealExpand, revealRadius) : 0f;
            float fade = on ? SonarWaveMath.Fade(t, revealExpand, revealHold, revealFade) : 0f;
            if (on && fade <= 0f) { revealStart = -1f; on = false; r = 0f; }
            Shader.SetGlobalVector(WaveId, new Vector4(waveOrigin.x, waveOrigin.y, waveOrigin.z, r));
            Shader.SetGlobalFloat(WaveFadeId, fade);
            if (sonarShell == null) return;
            bool show = on && r > 0.05f;
            if (sonarShell.gameObject.activeSelf != show) sonarShell.gameObject.SetActive(show);
            if (!show) return;
            sonarShell.position = waveOrigin;
            sonarShell.localScale = Vector3.one * (r * 2f);
            if (sonarMat != null) sonarMat.SetFloat("_Fade", fade);
        }

        void Apply()
        {
            float c = Mathf.Clamp01(sight);
            if (color != null) color.saturation.value = Mathf.Lerp(-100f, 0f, c);
            Shader.SetGlobalFloat(BlindId, 1f - c);

            // Daylight and ambient fade out as sight goes to 0; the player's own light fades in.
            if (sun != null) sun.intensity = Mathf.Lerp(0f, sunBase, c);
            if (c >= 0.999f)
            {
                if (!restoredSky)
                {
                    RenderSettings.ambientMode = origAmbientMode;
                    RenderSettings.ambientLight = origAmbient;
                    DynamicGI.UpdateEnvironment();
                    restoredSky = true;
                }
            }
            else
            {
                restoredSky = false;
                RenderSettings.ambientMode = AmbientMode.Flat;
                // never brighter than the scene's own ambient: with a dark cave (Dappled Sun) a growing eye must not light it up and then snap dark
                var cap = new Color(Mathf.Min(0.45f, origAmbient.r), Mathf.Min(0.45f, origAmbient.g), Mathf.Min(0.45f, origAmbient.b));
                RenderSettings.ambientLight = Color.Lerp(Color.black, cap, c);
            }
            RenderSettings.reflectionIntensity = Mathf.Lerp(0f, origReflection, c);
            if (cam != null) cam.backgroundColor = Color.Lerp(Color.black, origBackground, c);

            // Fog only softens the far distance now; it no longer limits what the blind character sees.
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = Color.Lerp(Color.black, fogColor, c);
            RenderSettings.fogStartDistance = 0f;
            RenderSettings.fogEndDistance = Mathf.Lerp(80f, eyesFogEnd, c * c) * (1f + Mathf.Max(0f, sight - 1f));

            if (sightLight != null)
            {
                sightLight.range = blindSightRadius;
                sightLight.intensity = Mathf.Min(blindLightIntensity, SonarWaveMath.MaxBlindLight) * (1f - c);   // never blown out: the scene's own serialized value may be much higher
                sightLight.enabled = sightLight.intensity > 0.01f;
            }
        }

        void OnValidate() { if (Application.isPlaying && color != null) Apply(); }
    }
}
