using UnityEngine;

namespace Evolve.World
{
    /// <summary>
    /// A light of the living world (glowing moss, mushrooms, pods, the daylight at the way out): it shines with the player's
    /// sight, so a slime without eyes still sees only the small circle around itself, and it switches off beyond
    /// <see cref="FarDistance"/> metres from the camera to keep the number of active lights small.
    /// </summary>
    [RequireComponent(typeof(Light))]
    public class LightByVision : MonoBehaviour
    {
        public const float FarDistance = 60f;

        [Tooltip("Ignore the distance cut (for the daylight at the way out, which is seen from far away)")]
        public bool AlwaysOnWhenSighted;

        Light lamp;
        float baseIntensity;
        MonochromeVision vision;
        Transform cam;
        float nextCamLookup;

        void Awake()
        {
            lamp = GetComponent<Light>();
            baseIntensity = lamp.intensity;
        }

        void Update()
        {
            if (vision == null) vision = FindAnyObjectByType<MonochromeVision>();
            if (cam == null && Time.unscaledTime >= nextCamLookup)
            {
                nextCamLookup = Time.unscaledTime + 1f;
                if (Camera.main != null) cam = Camera.main.transform;
            }
            float sight = vision != null ? Mathf.Clamp01(vision.Sight) : 1f;
            bool near = AlwaysOnWhenSighted || cam == null || (cam.position - transform.position).sqrMagnitude < FarDistance * FarDistance;
            lamp.enabled = near && sight > 0.01f;
            lamp.intensity = baseIntensity * sight;
        }
    }
}
