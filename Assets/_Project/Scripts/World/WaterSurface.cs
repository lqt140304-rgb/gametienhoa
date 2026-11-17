using System.Collections.Generic;
using UnityEngine;

namespace Evolve.World
{
    public enum WaterShape { Rect, Disc, Stream }

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
        [Header("Stream (Shape = Stream)")]
        [Tooltip("Centre line of the stream, in this object's own space (x, z). Size = (length, width) is used for the ripple scale.")]
        public Vector2[] StreamPath;
        public float StreamHalfWidth = 3f;
        [Tooltip("How much the stream's width wanders (0 = constant)")]
        public float StreamWobble = 0.25f;
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

        // Every pond in the scene knows the others, so where two ponds meet they become ONE sheet of water:
        // no shore band is drawn across the water of a neighbour, and overlapping water is drawn only once.
        static readonly List<WaterSurface> all = new List<WaterSurface>();
        public static IReadOnlyList<WaterSurface> All => all;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetAll() => all.Clear();

        Vector2[] worldOutline;
        bool dirty;

        /// <summary>The shoreline in world coordinates (x, z).</summary>
        public Vector2[] WorldOutline
        {
            get
            {
                if (worldOutline == null) ComputeOutline();
                return worldOutline;
            }
        }

        /// <summary>True if this world point lies on this pond's water.</summary>
        public bool ContainsWorld(Vector2 p) => PointInPolygon(WorldOutline, p);

        static bool PointInPolygon(Vector2[] poly, Vector2 p)
        {
            bool inside = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
            {
                if ((poly[i].y > p.y) != (poly[j].y > p.y) &&
                    p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
                    inside = !inside;
            }
            return inside;
        }

        Vector2[] localOutline;

        Vector2[] streamLeft, streamRight;

        void ComputeOutline()
        {
            if (Shape == WaterShape.Stream && StreamPath != null && StreamPath.Length >= 2)
            {
                WaterStream.Edges(StreamPath, StreamHalfWidth, Seed, StreamWobble, out streamLeft, out streamRight);
                localOutline = WaterStream.Polygon(streamLeft, streamRight);
                worldOutline = new Vector2[localOutline.Length];
                var o = new Vector2(transform.position.x, transform.position.z);
                for (int i = 0; i < localOutline.Length; i++) worldOutline[i] = localOutline[i] + o;
                return;
            }
            bool disc = Shape == WaterShape.Disc;
            float width = disc ? Size.x * 2f : Size.x, depth = disc ? Size.x * 2f : Size.y;
            int samples = Mathf.Clamp(Mathf.RoundToInt(WaterOutline.PerimeterEstimate(width, depth, disc) / Mathf.Max(0.3f, Cell)), 24, 220);
            localOutline = WaterOutline.Make(width, depth, disc, Seed, Wobble, samples);
            worldOutline = new Vector2[localOutline.Length];
            var origin = new Vector2(transform.position.x, transform.position.z);
            for (int i = 0; i < localOutline.Length; i++) worldOutline[i] = localOutline[i] + origin;
        }

        // Is this point (in my mesh space) on the water of another pond? onlyLowerPriority: only ponds drawn "before" me.
        bool OnOtherWater(Vector3 local, bool onlyLowerPriority)
        {
            var p = new Vector2(transform.position.x + local.x, transform.position.z + local.z);
            foreach (var other in all)
            {
                if (other == null || other == this || !other.isActiveAndEnabled) continue;
                if (onlyLowerPriority && other.Seed >= Seed) continue;
                if (other.ContainsWorld(p)) return true;
            }
            return false;
        }

        static void MarkAllDirty()
        {
            foreach (var s in all) if (s != null) s.dirty = true;
#if UNITY_EDITOR
            if (!Application.isPlaying) UnityEditor.EditorApplication.delayCall += FlushAll;
#endif
        }

        /// <summary>Rebuild every pond now (other tools that read pond shapes call this first).</summary>
        public static void Refresh()
        {
            foreach (var s in all) if (s != null) { s.worldOutline = null; s.dirty = true; }
            FlushAll();
        }

        static void FlushAll()
        {
            var copy = new List<WaterSurface>(all);
            // Outlines are cached from each pond's settings; refresh them first so ponds see each other's final shape.
            foreach (var s in copy) if (s != null && s.dirty) s.worldOutline = null;
            foreach (var s in copy) if (s != null && s.dirty && s.isActiveAndEnabled) { s.dirty = false; s.Build(); }
        }

        Mesh mesh;
        readonly List<Mesh> extraMeshes = new List<Mesh>();
        Vector3[] basePos, work;
        MaterialPropertyBlock block;
        Vector2 offset;
        float clock, nextUpdate, nextCullCheck;
        bool culled;

        void OnEnable()
        {
            all.Add(this);
            worldOutline = null;
            MarkAllDirty();
        }

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

        void OnDisable()
        {
            all.Remove(this);
            Release();
            MarkAllDirty();
        }

        void OnDestroy()
        {
            all.Remove(this);
            Release();
        }

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
            ComputeOutline();
            var outline = localOutline;

            bool stream = Shape == WaterShape.Stream && streamLeft != null;
            mesh = stream ? WaterMeshes.Strip(streamLeft, streamRight, Cell) : WaterMeshes.Organic(outline, Cell);
            // Where another pond already covers the water, draw nothing twice.
            WaterMeshes.RemoveTriangles(mesh, c => OnOtherWater(c, true));
            mesh.hideFlags = HideFlags.HideAndDontSave; // never saved into the scene: it is rebuilt every time
            mesh.MarkDynamic();
            GetComponent<MeshFilter>().sharedMesh = mesh;
            // Water neither casts nor receives shadows: shadows of the roots and pillars would show up as black patches on it.
            var surfaceRenderer = GetComponent<MeshRenderer>();
            surfaceRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            surfaceRenderer.receiveShadows = false;
            basePos = mesh.vertices;
            work = new Vector3[basePos.Length];
            var b = mesh.bounds;
            b.Expand(new Vector3(0f, 0.5f, 0f));
            mesh.bounds = b;
            ApplyRipples();

            // Wet bank (dark, fading outward) and foam (white, broken up), both following the same shore.
            // The parts that would lie on a neighbour's water are removed: ponds that touch simply flow together.
            var bank = stream
                ? WaterMeshes.Combine(WaterMeshes.EdgeBand(streamLeft, BankOutside, BankInside, 1f, Seed * 31 + 1), WaterMeshes.EdgeBand(streamRight, BankOutside, BankInside, -1f, Seed * 31 + 3))
                : WaterMeshes.Band(outline, BankOutside, BankInside, Seed * 31 + 1);
            WaterMeshes.RemoveTriangles(bank, c => OnOtherWater(c, false));
            Band("Bank", BankMaterial, bank, 0.025f);
            var foam = stream
                ? WaterMeshes.Combine(WaterMeshes.EdgeBand(streamLeft, FoamOutside, FoamInside, 1f, Seed * 31 + 2), WaterMeshes.EdgeBand(streamRight, FoamOutside, FoamInside, -1f, Seed * 31 + 4))
                : WaterMeshes.Band(outline, FoamOutside, FoamInside, Seed * 31 + 2);
            WaterMeshes.RemoveTriangles(foam, c => OnOtherWater(c, false));
            Band("Foam", FoamMaterial, foam, 0.05f);
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
                mr.receiveShadows = false;
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
            bool box = Shape != WaterShape.Disc;
            float w = box ? Size.x : Size.x * 2f;
            float d = box ? Size.y : Size.x * 2f;
            float repeat = Mathf.Max(0.5f, MetersPerRepeat);
            renderer.GetPropertyBlock(block);
            block.SetVector("_BaseMap_ST", new Vector4(w / repeat, d / repeat, offset.x, offset.y));
            renderer.SetPropertyBlock(block);
        }

        void Update()
        {
            if (!Application.isPlaying) return;
            if (dirty) FlushAll();
            if (mesh == null) return;

            // Far from the camera: do not animate and do not draw (the whole zone is much bigger than what is in view).
            if (Time.time >= nextCullCheck)
            {
                nextCullCheck = Time.time + 0.3f;
                var cam = Camera.main;
                bool far = cam != null && Vector3.Distance(cam.transform.position, transform.position + mesh.bounds.center) - mesh.bounds.extents.magnitude > 170f;
                if (far != culled)
                {
                    culled = far;
                    foreach (var r in GetComponentsInChildren<Renderer>(true)) r.enabled = !culled;
                }
            }
            if (culled) return;
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
