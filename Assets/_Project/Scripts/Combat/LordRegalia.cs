using Evolve.Abilities;
using Evolve.Creatures;
using UnityEngine;

namespace Evolve.Combat
{
    /// <summary>
    /// Clothes and ornaments for a cave lord (Cáo Gió), built from code so the boss looks like one before it has hand-made costume art:
    /// a gold crown with a pale sky-blue gem, a sky-blue scarf, a fluttering wind cloak and a glowing chest charm with a turning ring.
    /// Everything is placed from the model's bounding box (forward is local +Z), so the fractions below can be tuned live from the god menu.
    /// </summary>
    public class LordRegalia : MonoBehaviour
    {
        /// <summary>Where on the box the parts sit (fractions); adjustable in the god menu while the fox is made after the change.</summary>
        public static float HeadHeight = 0.92f, HeadForward = 0.82f, NeckForward = 0.62f, Size = 1f;

        static readonly Color Sky = new Color(0.58f, 0.8f, 1f), Gold = new Color(1f, 0.82f, 0.3f);

        public static bool Has(string speciesIdOrName) => SpeciesNames.Key(speciesIdOrName) == "caogio" && !FoxRigFx.Has(speciesIdOrName);   // the Meshy fox has its own trinkets

        /// <summary>Puts the costume on a creature once; a second call does nothing.</summary>
        public static LordRegalia Attach(GameObject creature) =>
            creature == null || creature.GetComponent<LordRegalia>() != null ? null : creature.AddComponent<LordRegalia>();

        Mesh capeMesh;
        Vector3[] capeVerts;
        int capeCols, capeRows;
        float capeSway;
        Material skyMat, goldMat;
        Transform ring;

        void Start()
        {
            var model = transform.Find("Model");
            if (model == null) foreach (var r in GetComponentsInChildren<Renderer>()) { model = r.transform; break; }
            if (model == null || !TryBounds(model, out Bounds box)) return;   // nothing to dress

            skyMat = SkillFx.Gel(Sky, 1f, 0.8f, 2.4f);
            goldMat = SkillFx.Gel(Gold, 1f, 0.3f, 1.4f);
            Vector3 min = box.min, size = box.size, c = box.center;
            float u = size.y * Size;   // one unit: the model's height

            // crown on the head, with a gem in the middle of the brow
            Vector3 head = new Vector3(c.x, min.y + size.y * HeadHeight, min.z + size.z * HeadForward);
            // the rig's own Head bone is the exact spot (same name CreatureLocomotion looks for); the sliders are only the fallback
            bool onBone = false;
            foreach (var t in GetComponentsInChildren<Transform>())
                if (t.name.Equals("Head", System.StringComparison.OrdinalIgnoreCase))
                { head = model.InverseTransformPoint(t.position) + new Vector3(0f, 0.08f * u, 0f); onBone = true; break; }
            Part("Vương Miện", RegaliaMeshes.Crown(0.17f * u, 0.2f * u, 5), goldMat, model, head, Quaternion.identity, false);
            Sphere("Ngọc Trán", skyMat, model, head + new Vector3(0f, 0.1f * u, 0.17f * u), 0.07f * u);

            // scarf round the neck
            Vector3 neck = new Vector3(c.x, min.y + size.y * 0.62f, min.z + size.z * NeckForward);
            if (onBone) neck = new Vector3(head.x, head.y - 0.12f * u, Mathf.Lerp(neck.z, head.z, 0.5f));
            Part("Khăn Gió", RegaliaMeshes.Torus(0.2f * u, 0.04f * u, 20, 8), skyMat, model, neck, Quaternion.Euler(65f, 0f, 0f), false);

            // cloak from the shoulders down the back, fluttering
            capeCols = 6; capeRows = 8; capeSway = 0.12f * size.z;
            var cape = RegaliaMeshes.CapeGrid(Mathf.Max(0.3f * size.x, 0.4f * u), 0.5f * size.z, capeCols, capeRows);
            Vector3 shoulders = new Vector3(c.x, min.y + size.y * 0.82f, min.z + size.z * 0.58f);
            capeMesh = Part("Áo Choàng Gió", cape, skyMat, model, shoulders, Quaternion.Euler(-20f, 0f, 0f), true);
            capeVerts = capeMesh.vertices;
            Sphere("Ghim Áo", goldMat, model, shoulders + new Vector3(0f, 0.03f * u, 0.02f * u), 0.05f * u);

            // glowing charm on the chest with a ring turning round it
            Vector3 chest = new Vector3(c.x, min.y + size.y * 0.5f, min.z + size.z * 0.88f);
            Sphere("Bùa Ngực", skyMat, model, chest, 0.1f * u);
            ring = Part("Vòng Bùa", RegaliaMeshes.Torus(0.11f * u, 0.012f * u, 20, 6), goldMat, model, chest, Quaternion.identity, false) != null ? model.Find("Vòng Bùa") : null;
        }

        void Update()
        {
            if (ring != null) ring.Rotate(0f, 0f, 140f * Time.deltaTime, Space.Self);
            if (capeMesh == null) return;
            float t = Time.time;
            for (int r = 0; r <= capeRows; r++)
                for (int col = 0; col <= capeCols; col++)
                    capeVerts[r * (capeCols + 1) + col].y = RegaliaMeshes.CapeWave(col / (float)capeCols, r / (float)capeRows, t) * capeSway;   // the same array every frame: no garbage
            capeMesh.vertices = capeVerts;
            capeMesh.RecalculateBounds();
        }

        void OnDestroy()
        {
            if (capeMesh != null) Destroy(capeMesh);
            if (skyMat != null) Destroy(skyMat);
            if (goldMat != null) Destroy(goldMat);
        }

        /// <summary>Bounding box of the model's renderers in the model's own space (all eight corners of each renderer box), or false when there are none.</summary>
        static bool TryBounds(Transform model, out Bounds box)
        {
            box = default; bool any = false;
            foreach (var r in model.GetComponentsInChildren<Renderer>())
            {
                var w = r.bounds;
                if (w.size == Vector3.zero) continue;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 corner = w.center + Vector3.Scale(w.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    Vector3 local = model.InverseTransformPoint(corner);
                    if (!any) { box = new Bounds(local, Vector3.zero); any = true; } else box.Encapsulate(local);
                }
            }
            return any;
        }

        internal static Mesh ToMesh(MeshData d, bool doubleSided)
        {
            var verts = new Vector3[d.VertexCount];
            for (int i = 0; i < verts.Length; i++) verts[i] = new Vector3(d.Vertices[i * 3], d.Vertices[i * 3 + 1], d.Vertices[i * 3 + 2]);
            int n = d.Triangles.Length;
            var tris = new int[doubleSided ? n * 2 : n];
            for (int i = 0; i < n; i++) tris[i] = d.Triangles[i];
            if (doubleSided) for (int i = 0; i < n; i += 3) { tris[n + i] = d.Triangles[i]; tris[n + i + 1] = d.Triangles[i + 2]; tris[n + i + 2] = d.Triangles[i + 1]; }   // the back faces
            var mesh = new Mesh { name = "Regalia" };
            mesh.vertices = verts; mesh.triangles = tris;
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            mesh.MarkDynamic();
            return mesh;
        }

        static Mesh Part(string name, MeshData data, Material mat, Transform parent, Vector3 localPos, Quaternion localRot, bool doubleSided)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos; go.transform.localRotation = localRot;
            var mesh = ToMesh(data, doubleSided);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return mesh;
        }

        static void Sphere(string name, Material mat, Transform parent, Vector3 localPos, float radius)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(go.GetComponent<Collider>());
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos; go.transform.localScale = Vector3.one * radius * 2f;
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = mat; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }
}
