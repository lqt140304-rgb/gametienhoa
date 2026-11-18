using UnityEngine;

namespace Evolve.World
{
    /// <summary>
    /// A bit of life hanging from the vault, only while the camera is within <see cref="NearDistance"/> metres: water dripping from a stalactite
    /// tip, or glowing spores drifting up from a pod. Tiny, built at runtime (a few particles each), so they cost nothing when far away.
    /// </summary>
    public class VaultLifeFx : MonoBehaviour
    {
        public enum Kind { Drip, Spores }

        public const float NearDistance = 45f;

        public Kind Effect = Kind.Drip;
        public Color Tint = new Color(0.6f, 0.85f, 1f);

        ParticleSystem ps;
        Transform cam;
        float nextCheck;
        static Material material;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { material = null; }

        void Awake()
        {
            if (material == null)
            {
                var tex = new Texture2D(16, 16, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
                for (int y = 0; y < 16; y++)
                    for (int x = 0; x < 16; x++)
                    {
                        float d = Mathf.Clamp01(1f - Vector2.Distance(new Vector2(x, y), new Vector2(7.5f, 7.5f)) / 7.5f);
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, d * d));
                    }
                tex.Apply();
                var shader = Shader.Find("Sprites/Default");
                if (shader == null) shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                material = new Material(shader) { mainTexture = tex, hideFlags = HideFlags.HideAndDontSave };
            }
            var go = new GameObject(Effect == Kind.Drip ? "Drip" : "Spores");
            go.transform.SetParent(transform, false);
            ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.loop = true;
            main.maxParticles = 12;
            var emission = ps.emission;
            var shape = ps.shape;
            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = material;
            r.renderMode = ParticleSystemRenderMode.Billboard;
            if (Effect == Kind.Drip)
            {
                main.startLifetime = 4f;
                main.startSpeed = 0f;
                main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.12f);
                main.startColor = new Color(Tint.r, Tint.g, Tint.b, 0.8f);
                main.gravityModifier = 1f;
                emission.rateOverTime = 0.35f;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = 0.05f;
            }
            else
            {
                main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 8f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
                main.startColor = new Color(Tint.r, Tint.g, Tint.b, 0.85f);
                main.gravityModifier = -0.02f;   // drifts slowly up
                emission.rateOverTime = 1.4f;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = 0.5f;
                var fade = ps.colorOverLifetime;
                fade.enabled = true;
                var g = new Gradient();
                g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                          new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(0f, 1f) });
                fade.color = g;
            }
        }

        void Update()
        {
            if (Time.unscaledTime < nextCheck) return;
            nextCheck = Time.unscaledTime + 0.5f;
            if (cam == null && Camera.main != null) cam = Camera.main.transform;
            bool near = cam != null && (cam.position - transform.position).sqrMagnitude < NearDistance * NearDistance;
            if (near && !ps.isPlaying) ps.Play();
            else if (!near && ps.isPlaying) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
