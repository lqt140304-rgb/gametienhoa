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

        [Tooltip("Slow breathing of the light: 0 = steady, 0.3 = about a third brighter and dimmer")]
        public float Pulse;
        public float PulseSpeed = 0.7f;

        Light lamp;
        float baseIntensity;
        static MonochromeVision sharedVision;   // one lookup for all lights, retried at most once a second
        static float nextVisionLookup;
        Transform cam;
        float phase;
        float nextCamLookup;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { sharedVision = null; nextVisionLookup = 0f; }

        void Awake()
        {
            lamp = GetComponent<Light>();
            baseIntensity = lamp.intensity;
            phase = (GetInstanceID() & 255) * 0.1f;   // lights breathe out of step
        }

        void Update()
        {
            if (sharedVision == null && Time.unscaledTime >= nextVisionLookup)
            {
                nextVisionLookup = Time.unscaledTime + 1f;
                sharedVision = FindAnyObjectByType<MonochromeVision>();
            }
            if (cam == null && Time.unscaledTime >= nextCamLookup)
            {
                nextCamLookup = Time.unscaledTime + 1f;
                if (Camera.main != null) cam = Camera.main.transform;
            }
            float sight = sharedVision != null ? Mathf.Clamp01(sharedVision.Sight) : 1f;
            bool near = AlwaysOnWhenSighted || cam == null || (cam.position - transform.position).sqrMagnitude < FarDistance * FarDistance;
            lamp.enabled = near && sight > 0.01f;
            float breathe = Pulse > 0f ? 1f + Pulse * Mathf.Sin(Time.time * PulseSpeed * 6.2832f + phase) : 1f;
            lamp.intensity = baseIntensity * sight * breathe;
        }
    }
}
