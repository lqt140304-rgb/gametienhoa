using Evolve.Organs;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Evolve.World
{
    /// <summary>
    /// "No eyes" vision: desaturates the URP volume and uses dense fog for short
    /// view range. Follows the player's OrganHost: growing eyes gradually restores colour and range.
    /// Put on a GameObject with a global Volume whose profile has Color Adjustments.
    /// </summary>
    [RequireComponent(typeof(Volume))]
    public class MonochromeVision : MonoBehaviour
    {
        [Tooltip("0 = blind (grey, short range), 1 = full colour vision")]
        [Range(0, 1)] [SerializeField] float sight;
        [Tooltip("Player whose organs decide the vision")]
        [SerializeField] OrganHost host;
        [SerializeField] float blindFogEnd = 6f;
        [SerializeField] float eyesFogEnd = 120f;
        [SerializeField] Color fogColor = new Color(0.08f, 0.08f, 0.08f);

        ColorAdjustments color;

        // Temporary view-range boost from the vibration scan.
        float revealStart = -1f, revealRadius, revealExpand, revealHold, revealFade;

        void Awake()
        {
            var volume = GetComponent<Volume>();
            if (!volume.profile.TryGet(out color))
                color = volume.profile.Add<ColorAdjustments>(true);
            color.saturation.overrideState = true;
            Apply();
            if (host != null) host.Changed += OnOrgansChanged;
        }

        void OnDestroy()
        {
            if (host != null) host.Changed -= OnOrgansChanged;
        }

        void OnOrgansChanged() => SetSight(host.Sight);

        public float Sight => sight;

        void Update()
        {
            if (Evolve.Survival.KeyPressed.V()) SetSight(sight >= 1f ? 0f : 1f);
            if (revealStart >= 0f) Apply();
        }

        /// <summary>
        /// Lift the fog out to <paramref name="radius"/> metres: the edge of the view spreads out over
        /// <paramref name="expand"/> seconds, stays for <paramref name="hold"/>, then closes in over <paramref name="fade"/>.
        /// </summary>
        public void Reveal(float radius, float expand, float hold, float fade)
        {
            revealStart = Time.time;
            revealRadius = radius;
            revealExpand = Mathf.Max(0.01f, expand);
            revealHold = hold;
            revealFade = Mathf.Max(0.01f, fade);
            Apply();
        }

        /// <summary>Current extra view distance from the scan (0 when none).</summary>
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

        /// <summary>0 = blind, 1 = full colour vision; above 1 (levelled eyes) only extends view range.</summary>
        public void SetSight(float value)
        {
            value = Mathf.Max(0f, value);
            if (Mathf.Approximately(value, sight)) return;
            sight = value;
            Apply();
        }

        void Apply()
        {
            float c = Mathf.Clamp01(sight);
            color.saturation.value = Mathf.Lerp(-100f, 0f, c);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogStartDistance = 0f;
            float baseEnd = Mathf.Lerp(blindFogEnd, eyesFogEnd, c * c) * (1f + Mathf.Max(0f, sight - 1f));
            RenderSettings.fogEndDistance = Mathf.Max(baseEnd, RevealDistance());
        }

        void OnValidate() { if (color != null) Apply(); }
    }
}
