using Evolve.Survival;
using UnityEngine;

namespace Evolve.Visuals
{
    /// <summary>
    /// The living core inside the gel: a soft amber glow (a bright centre inside two faint halos) with a few bubbles.
    /// Built when the game starts (so no temporary materials are ever saved into scenes). It pulses; faster and
    /// dimmer when the owner's health is low. Put it on the slime's "Visual" object.
    /// </summary>
    public class SlimeGelCore : MonoBehaviour
    {
        static readonly Color Amber = new Color(1f, 0.62f, 0.15f);

        Transform core, halo1, halo2;
        Renderer coreRenderer;
        MaterialPropertyBlock block;
        SurvivalStats stats;
        float phase;

        /// <summary>Extra glow (0..1) other scripts can add for a moment, e.g. while absorbing.</summary>
        public float Boost;

        void Awake()
        {
            stats = GetComponentInParent<SurvivalStats>();
            block = new MaterialPropertyBlock();
            var amber = MakeMaterial(Amber, 3f, 1f);
            var haloA = MakeMaterial(Amber, 1.2f, 0.28f);
            var haloB = MakeMaterial(new Color(1f, 0.8f, 0.4f), 0.8f, 0.16f);
            var bubbleMat = MakeMaterial(new Color(0.9f, 1f, 0.85f), 0.2f, 0.45f);

            var centre = new Vector3(0.03f, -0.1f, -0.02f);
            core = MakePart("Core", centre, 0.24f, amber).transform;
            coreRenderer = core.GetComponent<Renderer>();
            halo1 = MakePart("CoreHalo", centre, 0.38f, haloA).transform;
            halo2 = MakePart("CoreHalo", centre, 0.52f, haloB).transform;

            var rnd = new System.Random(11);
            for (int i = 0; i < 6; i++)
            {
                float a = (float)rnd.NextDouble() * Mathf.PI * 2f, r = 0.15f + (float)rnd.NextDouble() * 0.2f;
                MakePart("Bubble", new Vector3(Mathf.Cos(a) * r, -0.2f + (float)rnd.NextDouble() * 0.34f, Mathf.Sin(a) * r),
                    0.03f + (float)rnd.NextDouble() * 0.035f, bubbleMat);
            }
        }

        void Update()
        {
            if (core == null) return;
            if (stats == null) stats = GetComponentInParent<SurvivalStats>();
            float health = 1f;
            if (stats != null && stats.State.MaxHealth > 0f) health = Mathf.Clamp01(stats.State.Health / stats.State.MaxHealth);
            float rate = Mathf.Lerp(7f, 2.2f, health);
            phase += Time.deltaTime * rate;
            float beat = 0.5f + 0.5f * Mathf.Sin(phase);
            float pulse = 1f + 0.1f * beat + 0.25f * Boost;
            core.localScale = Vector3.one * (0.24f * pulse);
            halo1.localScale = Vector3.one * (0.38f * pulse);
            halo2.localScale = Vector3.one * (0.52f * pulse);

            float glow = (Mathf.Lerp(0.5f, 2.6f, health) + 2f * Boost) * (0.75f + 0.25f * beat);
            coreRenderer.GetPropertyBlock(block);
            block.SetColor("_EmissionColor", Amber * glow);
            coreRenderer.SetPropertyBlock(block);
        }

        GameObject MakePart(string name, Vector3 pos, float size, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            var collider = go.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = pos;
            go.transform.localScale = Vector3.one * size;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        /// <summary>Runtime emissive material; alpha below 1 makes it translucent.</summary>
        static Material MakeMaterial(Color color, float intensity, float alpha)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader) { name = "GelCoreRuntime" };
            color.a = alpha;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color); else mat.color = color;
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            mat.SetColor("_EmissionColor", color * intensity);
            if (alpha < 1f)
            {
                mat.SetFloat("_Surface", 1f);
                mat.SetFloat("_Blend", 0f);
                mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetFloat("_ZWrite", 0f);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + 5;
            }
            return mat;
        }
    }
}
