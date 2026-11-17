using UnityEngine;

namespace Evolve.World
{
    public enum WaterShape { Rect, Disc }

    /// <summary>
    /// A calm, gently moving water surface: a flat mesh built in code whose vertices rise and fall in slow waves,
    /// while the ripple (normal) texture drifts slowly across it. Visual only; it has no collider, so it never
    /// changes how the slime moves (the invisible WaterZone triggers still do that).
    /// The mesh is built when the object wakes up (also in the editor, so the water shows without playing).
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
        [Tooltip("Size of one ripple pattern repeat in metres (the pattern keeps the same size on big and small ponds)")]
        public float MetersPerRepeat = 5f;
        [Tooltip("How fast the ripple texture drifts (texture repeats per second)")]
        public Vector2 Drift = new Vector2(0.02f, 0.012f);

        Mesh mesh;
        Vector3[] basePos, work;
        MaterialPropertyBlock block;
        Vector2 offset;
        float clock, nextUpdate;

        void OnEnable() => Build();

        void OnValidate()
        {
            // Rebuild after the inspector changed a value (not while the object is being created).
            if (isActiveAndEnabled) Build();
        }

        void OnDisable() => Release();

        void OnDestroy() => Release();

        void Release()
        {
            if (mesh == null) return;
            var filter = GetComponent<MeshFilter>();
            if (filter != null && filter.sharedMesh == mesh) filter.sharedMesh = null;
            if (Application.isPlaying) Destroy(mesh); else DestroyImmediate(mesh);
            mesh = null;
        }

        void Build()
        {
            Release();
            mesh = Shape == WaterShape.Rect ? WaterMeshes.Rect(Size.x, Size.y, Cell) : WaterMeshes.Disc(Size.x, Cell);
            mesh.hideFlags = HideFlags.HideAndDontSave; // never saved into the scene: it is rebuilt every time
            mesh.MarkDynamic();
            GetComponent<MeshFilter>().sharedMesh = mesh;
            basePos = mesh.vertices;
            work = new Vector3[basePos.Length];
            var b = mesh.bounds;
            b.Expand(new Vector3(0f, 0.5f, 0f));
            mesh.bounds = b;
            ApplyRipples();
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
