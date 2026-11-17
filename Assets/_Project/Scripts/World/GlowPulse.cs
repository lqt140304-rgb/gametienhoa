using UnityEngine;

namespace Evolve.World
{
    /// <summary>
    /// Makes a glowing plant breathe: its emission rises and falls slowly. Visual only. It touches the renderer
    /// only while the camera is near (a property block takes a renderer out of SRP batching, so far plants stay static).
    /// </summary>
    public class GlowPulse : MonoBehaviour
    {
        [SerializeField] float speed = 1.6f;
        [Range(0f, 0.9f)] [SerializeField] float amount = 0.45f;
        [SerializeField] float activeDistance = 80f;

        const float UpdateInterval = 0.05f, CameraCheckInterval = 0.5f;
        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        Renderer rend;
        MaterialPropertyBlock block;
        Color baseEmission;
        float phase, nextUpdate, nextCameraCheck;
        bool ready, near, wasNear;
        Camera cam;

        /// <summary>1 ± amount, a slow sine; the phase keeps neighbouring plants out of step.</summary>
        public static float Intensity(float time, float speed, float amount, float phase)
            => 1f + amount * Mathf.Sin(time * speed + phase);

        void Start()
        {
            rend = GetComponentInChildren<Renderer>();
            if (rend == null) return;
            var mat = rend.sharedMaterial;
            if (mat == null || !mat.HasProperty(EmissionId)) return;
            baseEmission = mat.GetColor(EmissionId);
            block = new MaterialPropertyBlock();
            Vector3 p = transform.position;
            phase = p.x * 12.9898f + p.z * 78.233f;
            ready = true;
        }

        void Update()
        {
            if (!ready) return;
            float now = Time.time;
            if (now >= nextCameraCheck)
            {
                nextCameraCheck = now + CameraCheckInterval;
                if (cam == null) cam = Camera.main;
                near = cam != null && (cam.transform.position - transform.position).sqrMagnitude <= activeDistance * activeDistance;
            }
            if (!near)
            {
                if (wasNear) { rend.SetPropertyBlock(null); wasNear = false; } // back to the shared material
                return;
            }
            wasNear = true;
            if (now < nextUpdate) return;
            nextUpdate = now + UpdateInterval;
            rend.GetPropertyBlock(block);
            block.SetColor(EmissionId, baseEmission * Intensity(now, speed, amount, phase));
            rend.SetPropertyBlock(block);
        }

        void OnDisable()
        {
            if (rend != null && wasNear) { rend.SetPropertyBlock(null); wasNear = false; }
        }
    }
}
