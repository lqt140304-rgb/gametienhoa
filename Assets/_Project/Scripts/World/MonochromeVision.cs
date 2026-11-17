using Evolve.Organs;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Evolve.World
{
    /// <summary>
    /// "No eyes" vision: desaturates the URP volume and uses dense fog for short
    /// view range. Follows the player's OrganHost: evolving eyes restores colour.
    /// Put on a GameObject with a global Volume whose profile has Color Adjustments.
    /// </summary>
    [RequireComponent(typeof(Volume))]
    public class MonochromeVision : MonoBehaviour
    {
        [SerializeField] bool hasEyes;
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

        void OnOrgansChanged() => SetHasEyes(host.HasSight);

        public bool HasEyes => hasEyes;

        void Update()
        {
            if (Evolve.Survival.KeyPressed.V()) SetHasEyes(!hasEyes);
        }

        public void SetHasEyes(bool value) { hasEyes = value; Apply(); }

        void Apply()
        {
            color.saturation.value = hasEyes ? 0f : -100f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogStartDistance = 0f;
            RenderSettings.fogEndDistance = hasEyes ? eyesFogEnd : blindFogEnd;
        }

        void OnValidate() { if (color != null) Apply(); }
    }
}
