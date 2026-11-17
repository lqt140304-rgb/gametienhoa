using System.Collections.Generic;
using UnityEngine;

namespace Evolve.World
{
    public enum WaterShape { Rect, Disc }

    /// <summary>
    /// A calm, gently moving pond: a mesh built in code that fills a winding shoreline (WaterOutline), whose vertices
    /// rise and fall in slow waves while the ripple (normal) pattern drifts across it. Around the shore there is a
    /// band of wet bank and a band of foam, so the edge reads as a real bank and not a cut. Visual only: no collider,
    /// it never changes how the slime moves (the invisible WaterZone triggers still do that).
    /// Everything is built when the object wakes up (also in the editor, so the water shows without playing); the
    /// meshes are never saved into the scene.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class WaterSurface : MonoBehaviour
    {
        public WaterShape Shape = WaterShape.Rect;
        [Tooltip("Rect: width (x) and depth (y) in metres. Disc: x = radius.")]
        public Vector2 Size = new Vector2(10f, 10f);
        [Tooltip("Spacing of the mesh vertices in metres")]
        public float Cell = 1.25f;
        public float WaveHeight = 0.03f;
        public float WaveSpeed = 1f;

        [Header("Shore")]
        [Tooltip("Different values give different winding shores")]
        public int Seed = 1;
        [Tooltip("How far the shore wanders in and out, in metres")]
        public float Wobble = 0.8f;
        public Material BankMaterial;
        public Material FoamMaterial;
        [Tooltip("Metres of wet bank outside the water's edge / inside it")]
        public float BankOutside = 2.4f, BankInside = 0.3f;
        [Tooltip("Metres of foam outside the water's edge / inside it")]
        public float FoamOutside = 1.0f, FoamInside = 0.45f;

        [Header("Ripples")]
        [Tooltip("Size of one ripple pattern repeat in metres (the pattern keeps the same size on big and small ponds)")]
        public float MetersPerRepeat = 9f;
        [Tooltip("How fast the ripple texture drifts (texture repeats per second)")]
        public Vector2 Drift = new Vector2(0.012f, 0.007f);

        Mesh mesh;
        readonly List<Mesh> extraMeshes = new List<Mesh>();
        Vector3[] basePos, work;
        MaterialPropertyBlock block;
        Vector2 offset;
        float clock, nextUpdate;

        void OnEnable() => Build();

#if UNITY_EDITOR
        void OnValidate()
        {
            // Unity forbids destroying meshes inside OnValidate, so the rebuild after an inspector change is
            // postponed to the next editor tick.
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null && isActiveAndEnabled) Build();
            };
        }
#endif

        void OnDisable() => Release();

        void OnDestroy() => Release();

        void Release()
        {
            foreach (var m in extraMeshes) Kill(m);
            extraMeshes.Clear();
            foreach (var name in new[] { "Bank", "Foam" })
            {
                var child = transform.Find(name);
                if (child == null) continue;
                var f = child.GetComponent<MeshFilter>();
                if (f != null) f.sharedMesh = null;
            }
            if (mesh == null) return;
            var filter = GetComponent<MeshFilter>();
            if (filter != null && filter.sharedMesh == mesh) filter.sharedMesh = null;
            Kill(mesh);
            mesh = null;
        }

        static void Kill(Mesh m)
        {
            if (m == null) return;
            if (Application.isPlaying) Destroy(m); else DestroyImmediate(m);
        }

        void Build()
        {
            Release();
            bool disc = Shape == WaterShape.Disc;
            float width = disc ? Size.x * 2f : Size.x, depth = disc ? Size.x * 2f : Size.y;
            int samples = Mathf.Clamp(Mathf.RoundToInt(WaterOutline.PerimeterEstimate(width, depth, disc) / Mathf.Max(0.3f, Cell)), 24, 220);
            var outline = WaterOutline.Make(width, depth, disc, Seed, Wobble, samples);

            mesh = WaterMeshes.Organic(outline, Cell);
            mesh.hideFlags = HideFlags.HideAndDontSave; // never saved into the scene: it is rebuilt every time
            mesh.MarkDynamic();
            GetComponent<MeshFilter>().sharedMesh = mesh;
            basePos = mesh.vertices;
            work = new Vector3[basePos.Length];
            var b = mesh.bounds;
            b.Expand(new Vector3(0f, 0.5f, 0f));
            mesh.bounds = b;
            ApplyRipples();

            // Wet bank (dark, fading outward) and foam (white, broken up), both following the same shore.
            Band("Bank", BankMaterial, WaterMeshes.Band(outline, BankOutside, BankInside, Seed * 31 + 1), 0.025f);
            Band("Foam", FoamMaterial, WaterMeshes.Band(outline, FoamOutside, FoamInside, Seed * 31 + 2), 0.05f);
        }

        void Band(string name, Material material, Mesh bandMesh, float height)
        {
            var child = transform.Find(name);
            if (material == null)
            {
                if (child != null) child.gameObject.SetActive(false);
                Kill(bandMesh);
                return;
            }
            if (child == null)
            {
                var go = new GameObject(name);
                go.transform.SetParent(transform, false);
                go.AddComponent<MeshFilter>();
                var mr = go.AddComponent<MeshRenderer>();
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                child = go.transform;
            }
            child.gameObject.SetActive(true);
            child.localPosition = new Vector3(0f, height, 0f);
            child.GetComponent<MeshRenderer>().sharedMaterial = material;
            bandMesh.hideFlags = HideFlags.HideAndDontSave;
            child.GetComponent<MeshFilter>().sharedMesh = bandMesh;
            extraMeshes.Add(bandMesh);
        }

        // Tiling and drift of the ripple pattern go through a property block: no material copies, works in the editor too.
        void ApplyRipples()
        {
            var renderer = GetComponent<MeshRenderer>();
            if (renderer == null) return;
            if (block == null) block = new MaterialPropertyBlock();
            float w = Shape == WaterShape.Rect ? Size.x : Size.x * 2f;
            float d = Shape == WaterShape.Rect ? Size.y : Size.x * 2f;
            float repeat = Mathf.Max(0.5f, MetersPerRepeat);
            renderer.GetPropertyBlock(block);
            block.SetVector("_BaseMap_ST", new Vector4(w / repeat, d / repeat, offset.x, offset.y));
            renderer.SetPropertyBlock(block);
        }

        void Update()
        {
            if (!Application.isPlaying || mesh == null) return;
            float dt = Time.deltaTime;
            clock += dt * WaveSpeed;

            // The ripple pattern drifts slowly.
            offset += Drift * dt;
            offset.x %= 1f; offset.y %= 1f;
            ApplyRipples();

            // The surface itself moves at 20 Hz: plenty for slow waves, and cheap.
            if (Time.time < nextUpdate) return;
            nextUpdate = Time.time + 0.05f;
            float t = clock;
            for (int i = 0; i < basePos.Length; i++)
            {
                Vector3 p = basePos[i];
                float y = Mathf.Sin(p.x * 0.9f + t * 1.3f) + 0.6f * Mathf.Sin(p.z * 1.3f - t * 1.1f + 1.7f) + 0.4f * Mathf.Sin((p.x + p.z) * 0.6f + t * 0.7f);
                p.y = y * WaveHeight;
                work[i] = p;
            }
            mesh.SetVertices(work);
        }
    }
}
