using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Evolve.World
{
    /// <summary>
    /// "No eyes" vision: desaturates the URP volume and uses dense fog for short
    /// view range. Call SetHasEyes(true) when the player evolves eyes.
    /// Put on a GameObject with a global Volume whose profile has Color Adjustments.
    /// </summary>
    [RequireComponent(typeof(Volume))]
    public class MonochromeVision : MonoBehaviour
    {
        [SerializeField] bool hasEyes;
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
        }

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
