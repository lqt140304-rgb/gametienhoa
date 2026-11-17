using System.Collections.Generic;
using UnityEngine;

namespace Evolve.World
{
    /// <summary>
    /// A small waterfall: a thin sheet of water falling from a lip, curving out a little as it starts to fall, with a
    /// ripple pattern that runs down it. Built in code when the object wakes up (never saved into the scene). The
    /// object's forward direction is the way the water falls toward; its origin is the middle of the top edge.
    /// Visual only: no collider.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class Waterfall : MonoBehaviour
    {
        public float Width = 3.2f;
        public float Height = 3f;
        [Tooltip("Metres of pattern per repeat")]
        public float MetersPerRepeat = 1.4f;
        [Tooltip("Repeats per second running down")]
        public float Speed = 1.6f;

        Mesh mesh;
        MaterialPropertyBlock block;
        float offset;

        void OnEnable() => Build();
        void OnDisable() => Release();
        void OnDestroy() => Release();

#if UNITY_EDITOR
        void OnValidate()
        {
            UnityEditor.EditorApplication.delayCall += () => { if (this != null && isActiveAndEnabled) Build(); };
        }
#endif

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
            const int cols = 8, rows = 12;
            var verts = new List<Vector3>((cols + 1) * (rows + 1));
            var uvs = new List<Vector2>(verts.Capacity);
            var rnd = new System.Random(Mathf.RoundToInt(Mathf.Abs(transform.position.x * 31f + transform.position.z * 17f)));
            var edgeJitter = new float[rows + 1];
            for (int r = 0; r <= rows; r++) edgeJitter[r] = ((float)rnd.NextDouble() - 0.5f) * 0.18f;

            for (int r = 0; r <= rows; r++)
            {
                float t = r / (float)rows;
                float y = -t * Height;
                float z = 0.45f * Mathf.Sin(Mathf.Min(1f, t * 2.5f) * Mathf.PI * 0.5f);      // arcs out at the lip, then falls straight
                float width = Width * (0.88f + 0.22f * t) + edgeJitter[r] * Width;           // a little wider as it falls
                for (int c = 0; c <= cols; c++)
                {
                    float u = c / (float)cols;
                    verts.Add(new Vector3((u - 0.5f) * width, y, z));
                    uvs.Add(new Vector2(u, t));
                }
            }
            var tris = new List<int>();
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    int a = r * (cols + 1) + c, b = a + 1, d = a + cols + 1, e = d + 1;
                    tris.Add(a); tris.Add(b); tris.Add(d);
                    tris.Add(b); tris.Add(e); tris.Add(d);
                }
            mesh = new Mesh { name = "Waterfall", hideFlags = HideFlags.HideAndDontSave };
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = GetComponent<MeshRenderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            Apply();
        }

        void Apply()
        {
            var renderer = GetComponent<MeshRenderer>();
            if (renderer == null) return;
            if (block == null) block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            float repeat = Mathf.Max(0.3f, MetersPerRepeat);
            block.SetVector("_BaseMap_ST", new Vector4(Width / repeat, Height / repeat, 0f, offset));
            renderer.SetPropertyBlock(block);
        }

        void Update()
        {
            if (!Application.isPlaying) return;
            offset = (offset - Speed * Time.deltaTime) % 1f;
            Apply();
        }
    }
}
