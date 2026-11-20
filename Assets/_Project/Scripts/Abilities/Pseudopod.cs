using Evolve.Swallow;
using UnityEngine;
using UnityEngine.Rendering;

namespace Evolve.Abilities
{
    /// <summary>
    /// A limb the slime forms out of its own gel (tongue, stinger, foot): a soft tube drawn with the slime's own body material, so it has the
    /// same colour, shine and see-through as the slime. It is rooted inside the body, thicker where it leaves the body, and is drawn back in
    /// (thinning to nothing) instead of fading. Rebuilt every frame from where it starts and where it reaches.
    /// </summary>
    sealed class Pseudopod
    {
        const int Points = 16, Sides = 10;
        readonly GameObject go;
        readonly Mesh mesh;
        readonly Material mat;
        readonly Vector3[] verts = new Vector3[Points * Sides];

        public Pseudopod(Material material)
        {
            mat = material;
            mesh = new Mesh { name = "Pseudopod", hideFlags = HideFlags.HideAndDontSave };
            mesh.MarkDynamic();
            var tris = new int[(Points - 1) * Sides * 6];
            int k = 0;
            for (int i = 0; i < Points - 1; i++)
                for (int a = 0; a < Sides; a++)
                {
                    int a2 = (a + 1) % Sides;
                    int p00 = i * Sides + a, p01 = i * Sides + a2, p10 = (i + 1) * Sides + a, p11 = (i + 1) * Sides + a2;
                    tris[k++] = p00; tris[k++] = p10; tris[k++] = p01;
                    tris[k++] = p01; tris[k++] = p10; tris[k++] = p11;
                }
            mesh.vertices = verts;
            mesh.triangles = tris;
            go = new GameObject("Pseudopod") { layer = 2 };   // Ignore Raycast: aiming rays pass through
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = material;
            r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
            go.SetActive(false);
        }

        /// <summary>Stretch the limb from <paramref name="from"/> toward <paramref name="to"/>; reach 0..1 is how much of the way it has gone.</summary>
        public void Set(Vector3 from, Vector3 to, float reach, float time, float baseR, float tipR, float flare, float bulb, float cap, float wobble)
        {
            Vector3 d = to - from;
            float len = d.magnitude * reach;
            if (len < 0.02f || reach <= 0.001f) { go.SetActive(false); return; }
            go.SetActive(true);
            Vector3 dir = d.normalized;
            Vector3 right = Vector3.Cross(Vector3.up, dir);
            if (right.sqrMagnitude < 1e-4f) right = Vector3.Cross(Vector3.forward, dir);
            right.Normalize();
            Vector3 up = Vector3.Cross(dir, right);
            float thin = Mathf.Min(1f, reach * 3f);   // drawing back in: it thins to nothing
            for (int i = 0; i < Points; i++)
            {
                float f = i / (Points - 1f);
                float sway = Mathf.Sin(f * Mathf.PI) * Mathf.Sin(time * 18f + f * 7f) * wobble;
                Vector3 c = from + dir * (len * f) + right * sway;
                float r = SkillFxMath.PseudopodRadius(f, baseR, tipR, flare, bulb, cap) * thin;
                for (int a = 0; a < Sides; a++)
                {
                    float ang = a / (float)Sides * Mathf.PI * 2f;
                    verts[i * Sides + a] = c + (right * Mathf.Cos(ang) + up * Mathf.Sin(ang)) * r;
                }
            }
            mesh.vertices = verts;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }

        public void Dispose()
        {
            if (go != null) Object.Destroy(go);
            if (mesh != null) Object.Destroy(mesh);
            if (mat != null) Object.Destroy(mat);
        }
    }
}
