using Evolve.Survival;
using UnityEngine;

namespace Evolve.Visuals
{
    /// <summary>
    /// The living core inside the gel: an amber glowing ball with a few thin green veins. Built when the game
    /// starts (so no temporary materials are ever saved into scenes) and it pulses; faster and dimmer when the
    /// owner's health is low. Put it on the slime's "Visual" object.
    /// </summary>
    public class SlimeGelCore : MonoBehaviour
    {
        static readonly Color Amber = new Color(1f, 0.62f, 0.15f);
        static readonly Color Vein = new Color(0.45f, 0.95f, 0.4f);

        Transform core;
        Renderer coreRenderer;
        MaterialPropertyBlock block;
        SurvivalStats stats;
        float phase;

        void Awake()
        {
            stats = GetComponentInParent<SurvivalStats>();
            block = new MaterialPropertyBlock();
            var amber = EmissiveMaterial(Amber, 3f);
            var green = EmissiveMaterial(Vein, 0.9f);

            core = MakePart(PrimitiveType.Sphere, "Core", transform, new Vector3(0f, -0.12f, 0f), Vector3.one * 0.3f, amber).transform;
            coreRenderer = core.GetComponent<Renderer>();

            // A few small bubbles drifting inside the gel.
            var bubbleMat = EmissiveMaterial(new Color(0.85f, 1f, 0.8f), 0.25f);
            var rnd = new System.Random(11);
            for (int i = 0; i < 7; i++)
            {
                float a = (float)rnd.NextDouble() * Mathf.PI * 2f, r = 0.12f + (float)rnd.NextDouble() * 0.22f;
                MakePart(PrimitiveType.Sphere, "Bubble", transform,
                    new Vector3(Mathf.Cos(a) * r, -0.2f + (float)rnd.NextDouble() * 0.38f, Mathf.Sin(a) * r),
                    Vector3.one * (0.025f + (float)rnd.NextDouble() * 0.03f), bubbleMat);
            }

            const int veins = 8;
            for (int i = 0; i < veins; i++)
            {
                float a = i / (float)veins * Mathf.PI * 2f;
                var dir = new Vector3(Mathf.Cos(a), 0.45f, Mathf.Sin(a)).normalized;
                var vein = MakePart(PrimitiveType.Capsule, "Vein", transform, new Vector3(0f, -0.12f, 0f) + dir * 0.24f,
                    new Vector3(0.022f, 0.24f, 0.022f), green);
                vein.transform.localRotation = Quaternion.FromToRotation(Vector3.up, dir);
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
            core.localScale = Vector3.one * (0.3f * (1f + 0.12f * beat));

            float glow = Mathf.Lerp(0.5f, 2.4f, health) * (0.75f + 0.25f * beat);
            coreRenderer.GetPropertyBlock(block);
            block.SetColor("_EmissionColor", Amber * glow);
            coreRenderer.SetPropertyBlock(block);
        }

        static GameObject MakePart(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            var collider = go.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        static Material EmissiveMaterial(Color color, float intensity)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader) { name = "GelCoreRuntime" };
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color); else mat.color = color;
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            mat.SetColor("_EmissionColor", color * intensity);
            return mat;
        }
    }
}
