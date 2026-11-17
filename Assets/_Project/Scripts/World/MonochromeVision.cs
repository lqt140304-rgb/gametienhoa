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
        [SerializeField] float blindFogEnd = 12f;
        [SerializeField] float eyesFogEnd = 120f;
        [SerializeField] Color fogColor = new Color(0.08f, 0.08f, 0.08f);

        ColorAdjustments color;

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
        }

        public void SetSight(float value)
        {
            value = Mathf.Clamp01(value);
            if (Mathf.Approximately(value, sight)) return;
            sight = value;
            Apply();
        }

        void Apply()
        {
            color.saturation.value = Mathf.Lerp(-100f, 0f, sight);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogStartDistance = 0f;
            RenderSettings.fogEndDistance = Mathf.Lerp(blindFogEnd, eyesFogEnd, sight * sight);
        }

        void OnValidate() { if (color != null) Apply(); }
    }
}
