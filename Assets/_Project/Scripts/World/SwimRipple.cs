using Evolve.Player;
using UnityEngine;

namespace Evolve.World
{
    /// <summary>
    /// A soft ring of ripples on the water round a swimming or wading slime: it lies on the surface, follows the body,
    /// and slowly widens and fades in a loop. Built in code (no assets); hidden on land.
    /// </summary>
    [RequireComponent(typeof(SlimeController))]
    public class SwimRipple : MonoBehaviour
    {
        const int Segments = 40;
        SlimeController controller;
        Transform ring;
        Material material;
        MeshRenderer ringRenderer;

        void Awake()
        {
            controller = GetComponent<SlimeController>();
            var go = new GameObject("Swim Ripple") { hideFlags = HideFlags.DontSave };
            ring = go.transform;
            go.AddComponent<MeshFilter>().sharedMesh = RingMesh(0.8f, 1f);
            ringRenderer = go.AddComponent<MeshRenderer>();
            ringRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ringRenderer.receiveShadows = false;
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default");
            material = new Material(shader) { name = "SwimRipple" };
            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_Blend", 0f);
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_ZWrite", 0f);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + 25;   // on top of the water
            ringRenderer.sharedMaterial = material;
            go.SetActive(false);
        }

        void OnDestroy()
        {
            if (ring != null) Destroy(ring.gameObject);
            if (material != null) Destroy(material);
        }

        void LateUpdate()
        {
            bool show = controller.IsSwimming || controller.IsWading;
            if (ring.gameObject.activeSelf != show) ring.gameObject.SetActive(show);
            if (!show) return;
            float cycle = Mathf.Repeat(Time.time * 0.6f, 1f);
            float size = (0.55f + 0.5f * cycle) * Mathf.Max(0.5f, controller.BaseDiameter * controller.BodyScale);
            ring.position = new Vector3(transform.position.x, WaterField.Level + 0.01f, transform.position.z);
            ring.localScale = new Vector3(size, 1f, size);
            var c = new Color(0.9f, 1f, 1f, 0.45f * (1f - cycle));
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", c); else material.color = c;
        }

        /// <summary>A flat ring between radius <paramref name="inner"/> and <paramref name="outer"/>, facing up.</summary>
        static Mesh RingMesh(float inner, float outer)
        {
            var verts = new Vector3[Segments * 2];
            var tris = new int[Segments * 6];
            for (int i = 0; i < Segments; i++)
            {
                float a = i * Mathf.PI * 2f / Segments;
                var d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                verts[i * 2] = d * inner;
                verts[i * 2 + 1] = d * outer;
                int n = (i + 1) % Segments;
                int t = i * 6;
                tris[t] = i * 2; tris[t + 1] = n * 2; tris[t + 2] = i * 2 + 1;
                tris[t + 3] = i * 2 + 1; tris[t + 4] = n * 2; tris[t + 5] = n * 2 + 1;
            }
            var mesh = new Mesh { name = "SwimRippleRing", vertices = verts, triangles = tris };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
