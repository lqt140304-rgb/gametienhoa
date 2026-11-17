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
        [Tooltip("Brightness of the wide light thrown by the scan")]
        [SerializeField] float scanLightIntensity = 150f;
        [Tooltip("How high above the character the scan light hangs")]
        [SerializeField] float scanLightHeight = 8f;

        [Header("With eyes")]
        [SerializeField] float eyesFogEnd = 120f;
        [SerializeField] Color fogColor = new Color(0.08f, 0.08f, 0.08f);

        static readonly Color LightColor = new Color(0.85f, 0.95f, 1f);

        ColorAdjustments color;
        Light sun, sightLight, scanLight;
        Camera cam;
        float sunBase, origReflection;
        Color origBackground, origAmbient;
        AmbientMode origAmbientMode;
        bool restoredSky = true;

        // Temporary wide reveal from the vibration scan.
        float revealStart = -1f, revealRadius, revealExpand, revealHold, revealFade;

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

        void OnDestroy()
        {
            if (host != null) host.Changed -= OnOrgansChanged;
            // Put the scene lighting back the way it was.
            if (sun != null) sun.intensity = sunBase;
            RenderSettings.ambientMode = origAmbientMode;
            RenderSettings.ambientLight = origAmbient;
            RenderSettings.reflectionIntensity = origReflection;
            if (cam != null) cam.backgroundColor = origBackground;
            if (sightLight != null) Destroy(sightLight.gameObject);
            if (scanLight != null) Destroy(scanLight.gameObject);
        }

        void OnOrgansChanged() => SetSight(host.Sight);

        public float Sight => sight;

        void Update()
        {
            if (Evolve.Survival.KeyPressed.V()) SetSight(sight >= 1f ? 0f : 1f);
            UpdateScan();
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
            revealStart = Time.time;
            revealRadius = radius;
            revealExpand = Mathf.Max(0.01f, expand);
            revealHold = hold;
            revealFade = Mathf.Max(0.01f, fade);
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
            if (scanLight == null) return;
            float c = Mathf.Clamp01(sight);
            float r = RevealDistance();
            if (r <= 0.01f || c >= 0.999f)
            {
                if (scanLight.enabled) scanLight.enabled = false;
                return;
            }
            float k = revealRadius > 0f ? r / revealRadius : 0f;
            float lit = r * 1.15f; // the light fades out toward its range, so reach a bit further
            scanLight.enabled = true;
            scanLight.range = Mathf.Sqrt(lit * lit + scanLightHeight * scanLightHeight);
            scanLight.intensity = scanLightIntensity * Mathf.Min(1f, k * 3f) * (1f - c);
        }

        void Apply()
        {
            float c = Mathf.Clamp01(sight);
            if (color != null) color.saturation.value = Mathf.Lerp(-100f, 0f, c);

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
                RenderSettings.ambientLight = Color.Lerp(Color.black, new Color(0.45f, 0.45f, 0.45f), c);
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
                sightLight.intensity = blindLightIntensity * (1f - c);
                sightLight.enabled = sightLight.intensity > 0.01f;
            }
        }

        void OnValidate() { if (Application.isPlaying && color != null) Apply(); }
    }
}
