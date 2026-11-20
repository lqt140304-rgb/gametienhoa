using System.Collections.Generic;
using Evolve.Civilization;
using Evolve.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Evolve.EditorTools
{
    /// <summary>
    /// Evolve → Dress Lake: Class Houses. The treefolk are tree people who live in hollow trunks, root hollows and carved
    /// stone like dwarves, so every class house is a great carved trunk (ridged bark, root buttresses, a leafy crown, a
    /// dwarf-style stone doorway with a carved lintel, amber-lit windows, a band of glowing runes) shaped and furnished for
    /// its class, standing alone on its own plot (CityLayout keeps the plots well apart):
    ///  Đấu Thể   a wide squat bastion-trunk behind a stockade of carved shield-posts, with warrior statues and a drill yard.
    ///  Liệp Thể  a tall thin dead tree with an antlered crown, a nest in its branches, trophies and a camouflage net.
    ///  Năng Thể  a twisted spire-tree veined with glowing crystal, ringed by rune obelisks, stones floating round it.
    ///  Quần Thể  a mother-tree with burrow doors round its foot and a corral of great root arches.
    ///  Dưỡng Thể a flowering tree under a canopy of huge petals, a healing spring at its roots and herb beds.
    ///  Xạ Thể    a very tall trunk with a stair carved round it up to a nest platform, and an archery range.
    ///  Cấu Thể   a trunk with big amber-resin windows, stone stills joined by root pipes, gel vats and jar shelves.
    ///  Dã Thể    a huge hollow fallen log, open at one end, with a camp in front of it.
    /// Run again to rebuild.
    /// </summary>
    public static class LakeClassHouses
    {
        const string DressedName = "Treefolk Class Houses (Dressed)";

        class Kit
        {
            public Material Bark, Stone, Wood, Amber, Moss, DarkWood, Cloth, Trunk;
            public Mesh Blob;
            public Transform Parent;
            public readonly List<CombineInstance> BarkParts = new List<CombineInstance>();
            public readonly List<CombineInstance> WoodParts = new List<CombineInstance>();
            public readonly List<CombineInstance> StoneParts = new List<CombineInstance>();
            public readonly List<CombineInstance> GlowParts = new List<CombineInstance>();
            public readonly List<CombineInstance> MossParts = new List<CombineInstance>();
            public readonly List<CombineInstance> DarkParts = new List<CombineInstance>();
            public readonly List<CombineInstance> ClothParts = new List<CombineInstance>();
            public int Seed = 20000;
        }

        /// <summary>A house's frame: where it stands, which way it faces, and a way to find points on its trunk.</summary>
        class Frame
        {
            public Vector3 At, Fwd, Side;
            public float Yaw;
            public System.Func<float, float, Vector3> Local;
            public System.Func<float, float> Radius;   // trunk radius at height y (standing trunks)

            /// <summary>A world point on the trunk at <paramref name="degrees"/> round it (0 = the front) and height <paramref name="y"/>, pushed out by <paramref name="push"/>.</summary>
            public Vector3 Surface(float degrees, float y, float push)
            {
                float a = degrees * Mathf.Deg2Rad, r = Radius(y) + push;
                return At + Quaternion.Euler(0f, Yaw, 0f) * new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r);
            }
        }

        [MenuItem("Evolve/Bản đồ cũ (Underroot)/Dress Lake: Class Houses")]
        public static void Dress()
        {
            Physics.SyncTransforms();
            var old = GameObject.Find(DressedName);
            if (old != null) Object.DestroyImmediate(old);
            var root = new GameObject(DressedName).transform;
            root.gameObject.AddComponent<DetailCulling>().ShowDistance = 5000f;   // only fades the glows with the slime's sight

            var kit = new Kit
            {
                Bark = LakeGround.OpaqueMaterial("Lake_CanopyBark", new Color(0.24f, 0.17f, 0.11f), 0.12f, false),
                Trunk = LakeGround.OpaqueMaterial("Lake_TrunkBark", new Color(0.30f, 0.21f, 0.13f), 0.1f, true),
                Stone = LakeGround.OpaqueMaterial("Lake_Limestone", new Color(0.74f, 0.72f, 0.64f), 0.2f, false),
                Wood = LakeGround.OpaqueMaterial("Lake_PetrifiedWood", new Color(0.46f, 0.36f, 0.26f), 0.35f, false),
                DarkWood = LakeGround.OpaqueMaterial("Lake_DarkRoot", new Color(0.16f, 0.12f, 0.09f), 0.1f, false),
                Amber = LakeGround.OpaqueMaterial("Lake_AmberLampSpot", new Color(0.95f, 0.62f, 0.18f), 0.6f, false, new Color(1.2f, 0.62f, 0.14f)),
                Moss = LakeGround.MossMaterial(),
                Cloth = LakeGround.OpaqueMaterial("Lake_Cloth", new Color(0.55f, 0.22f, 0.14f), 0.1f, true),
                Blob = FloraMeshes.Blob(),
            };
            var houses = CityLayout.ClassHouses();
            foreach (var h in houses)
            {
                var parent = new GameObject(TreefolkClasses.Name(h.Class)).transform;
                parent.SetParent(root, false);
                kit.Parent = parent;
                Build(kit, h);
                Flush(parent, kit);
            }
            EditorSceneManager.MarkAllScenesDirty();
            AssetDatabase.SaveAssets();
            Debug.Log($"Đã dựng {houses.Length} nhà của các class người cây (thân cây khoét rỗng, cửa đá kiểu người lùn, rune phát sáng). Bấm Ctrl+S để lưu scene.");
        }

        static void Flush(Transform parent, Kit kit)
        {
            LakeGround.AddCombined(parent, "Bark", kit.BarkParts, kit.Bark); kit.BarkParts.Clear();
            LakeGround.AddCombined(parent, "Wood", kit.WoodParts, kit.Wood); kit.WoodParts.Clear();
            LakeGround.AddCombined(parent, "Dark Wood", kit.DarkParts, kit.DarkWood); kit.DarkParts.Clear();
            LakeGround.AddCombined(parent, "Stone", kit.StoneParts, kit.Stone); kit.StoneParts.Clear();
            LakeGround.AddCombined(parent, "Glow", kit.GlowParts, kit.Amber); kit.GlowParts.Clear();
            LakeGround.AddCombined(parent, "Moss", kit.MossParts, kit.Moss); kit.MossParts.Clear();
            LakeGround.AddCombined(parent, "Cloth", kit.ClothParts, kit.Cloth); kit.ClothParts.Clear();
        }

        // ---------------------------------------------------------------- helpers

        static Vector3 OnGround(float x, float z) => new Vector3(x, LakeGround.GroundY(x, z), z);

        static void Tube(List<CombineInstance> list, Kit kit, Vector3[] path, float r0, float r1, int sides = 7, float knobs = 0.18f) =>
            list.Add(new CombineInstance { mesh = RootMeshes.RootTube(path, r0, r1, ++kit.Seed, knobs, sides), transform = Matrix4x4.identity });

        static void Ball(List<CombineInstance> list, Kit kit, Vector3 at, Vector3 size, float yaw = 0f) =>
            list.Add(new CombineInstance { mesh = kit.Blob, transform = Matrix4x4.TRS(at, Quaternion.Euler(0f, yaw, 0f), size) });

        static GameObject Box(Kit kit, string name, Vector3 centre, Vector3 size, float yaw, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(kit.Parent, false);
            go.transform.position = centre;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            go.isStatic = true;
            return go;
        }

        static GameObject Drum(Kit kit, string name, Vector3 baseAt, float radius, float height, Material mat)
        {
            var mesh = CaveMeshes.Disc(radius, height, 28);
            var go = new GameObject(name);
            go.transform.SetParent(kit.Parent, false);
            go.transform.position = baseAt;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            go.AddComponent<MeshCollider>().sharedMesh = mesh;
            go.isStatic = true;
            return go;
        }

        static GameObject Solid(Kit kit, string name, Mesh mesh, Vector3 at, Quaternion rot, Material mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(kit.Parent, false);
            go.transform.position = at;
            go.transform.rotation = rot;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.TwoSided;
            go.AddComponent<MeshCollider>().sharedMesh = mesh;
            go.isStatic = true;
            return go;
        }

        static void Capsule(Kit kit, Vector3 centre, float radius, float height)
        {
            var col = new GameObject("Collider").AddComponent<CapsuleCollider>();
            col.transform.SetParent(kit.Parent, false);
            col.transform.position = centre; col.radius = radius; col.height = height;
        }

        static void PointLight(Kit kit, Vector3 at, Color color, float range, float intensity)
        {
            var go = new GameObject("Light");
            go.transform.SetParent(kit.Parent, false);
            go.transform.position = at;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point; l.range = range; l.intensity = intensity; l.color = color; l.shadows = LightShadows.None;
        }

        static Mesh Quad(float w, float h)
        {
            var m = new Mesh { name = "Cloth" };
            m.vertices = new[] { new Vector3(0f, 0f, -w * 0.5f), new Vector3(0f, 0f, w * 0.5f), new Vector3(0f, -h, -w * 0.5f), new Vector3(0f, -h, w * 0.5f) };
            m.triangles = new[] { 0, 1, 2, 1, 3, 2 };
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        // ---------------------------------------------------------------- the carved trunk house

        /// <summary>
        /// A hollow standing trunk: a flared foot, a tapering ridged shaft, a doorway on the front (+x) framed in carved
        /// dwarf-style stonework with a lintel plaque and glowing runes, round amber-lit windows, root buttresses splaying at
        /// the foot and (optionally) a crown of moss and branches. The inside is open (walkable) through the door.
        /// </summary>
        static Frame Trunk(Kit kit, CityLayout.ClassHouse h, float baseR, float topR, float height, float doorW, float doorH, float[] windowDegrees, float windowY, bool crown = true, int buttresses = 5)
        {
            Quaternion rot = Quaternion.Euler(0f, h.Yaw, 0f);
            Vector3 at = OnGround(h.Position.x, h.Position.y);
            var f = new Frame { At = at, Yaw = h.Yaw, Fwd = rot * Vector3.right, Side = rot * Vector3.forward };
            f.Local = (x, z) => { var p = at + f.Fwd * x + f.Side * z; return OnGround(p.x, p.z); };
            const float flare = 0.45f;
            f.Radius = y => Mathf.Lerp(baseR, topR, Mathf.Pow(Mathf.Clamp01(y / height), 0.85f)) * (1f + flare * Mathf.Exp(-Mathf.Max(0f, y) * 1.1f));

            // the shaft: a lathe from below the ground up to the rim
            var profile = new List<Vector2>();
            foreach (float py in new[] { -0.4f, 0.2f, 0.7f, 1.5f, height * 0.25f, height * 0.5f, height * 0.75f, height })
                profile.Add(new Vector2(f.Radius(Mathf.Max(0f, py)), py));
            var shaft = FloraMeshes.Lathe(profile, 48, height * 0.5f, ++kit.Seed, 0.025f);
            float doorHalf = doorW * 0.5f, straight = doorH * 0.62f, cap = doorH - straight;
            WaterMeshes.RemoveTriangles(shaft, v =>
            {
                float ang = Mathf.Atan2(v.z, v.x) * Mathf.Rad2Deg;
                float rr = Mathf.Sqrt(v.x * v.x + v.z * v.z);
                // the doorway: straight sides, a round top
                if (Mathf.Abs(ang) < 40f && v.y > -0.2f)
                {
                    float across = Mathf.Sin(ang * Mathf.Deg2Rad) * rr;
                    if (v.y <= straight) { if (Mathf.Abs(across) < doorHalf) return true; }
                    else { float e = (v.y - straight) / cap, fz = across / doorHalf; if (e * e + fz * fz < 1f) return true; }
                }
                // round windows
                if (windowDegrees != null)
                    foreach (float wd in windowDegrees)
                    {
                        float da = Mathf.DeltaAngle(ang, wd) * Mathf.Deg2Rad * rr, dy = v.y - windowY;
                        if (da * da + dy * dy < 0.36f * 0.36f) return true;
                    }
                return false;
            });
            Solid(kit, "Trunk", shaft, at, rot, kit.Trunk);

            // bark ridges: roots running up the shaft, merging into it (never across the door or a window)
            for (int i = 0; i < 12; i++)
            {
                float deg = i * 30f + 11f;
                if (Mathf.Abs(Mathf.DeltaAngle(deg, 0f)) < 38f) continue;
                bool nearWindow = false;
                if (windowDegrees != null) foreach (float wd in windowDegrees) if (Mathf.Abs(Mathf.DeltaAngle(deg, wd)) < 12f) nearWindow = true;
                if (nearWindow) continue;
                var path = new Vector3[8];
                for (int k = 0; k < path.Length; k++)
                {
                    float y = Mathf.Lerp(-0.2f, height * 0.97f, k / 7f);
                    path[k] = f.Surface(deg + 4f * Mathf.Sin(k * 1.3f + i), y, -0.1f);
                }
                Tube(kit.BarkParts, kit, path, 0.3f, 0.18f, 7, 0.3f);
            }
            // buttress roots splaying out over the ground, thick where they leave the trunk
            for (int i = 0; i < buttresses; i++)
            {
                float deg = 40f + i * (280f / Mathf.Max(1, buttresses - 1)) + 8f * Mathf.Sin(i * 2.3f);
                var path = new Vector3[8];
                Vector3 root0 = f.Surface(deg, 0f, 0f);
                Vector3 outward = root0 - at; outward.y = 0f; outward.Normalize();
                for (int k = 0; k < path.Length; k++)
                {
                    float t = k / 7f;
                    Vector3 p = root0 + outward * (baseR * Mathf.Lerp(0f, 1.7f, t));
                    p.y = LakeGround.GroundY(p.x, p.z) + Mathf.Lerp(1.8f, -0.25f, Mathf.Sqrt(t));
                    path[k] = p;
                }
                Tube(kit.BarkParts, kit, path, 0.75f, 0.2f, 8, 0.25f);
            }

            // the carved stone doorway: two jambs, a lintel with a carved plaque, a threshold slab, lanterns and runes
            Vector3 doorBase = f.Surface(0f, 0f, 0.05f);
            foreach (float s in new[] { -1f, 1f })
                Box(kit, "Jamb", doorBase + f.Side * s * (doorHalf + 0.2f) + Vector3.up * (straight * 0.5f) + f.Fwd * 0.1f, new Vector3(0.7f, straight + 0.2f, 0.4f), h.Yaw, kit.Stone);
            Box(kit, "Lintel", doorBase + Vector3.up * (doorH + 0.15f) + f.Fwd * 0.1f, new Vector3(0.7f, 0.5f, doorW + 1.1f), h.Yaw, kit.Stone);
            Box(kit, "Plaque", doorBase + Vector3.up * (doorH + 0.9f) + f.Fwd * 0.18f, new Vector3(0.3f, 0.9f, doorW * 0.8f + 0.4f), h.Yaw, kit.Stone);
            Box(kit, "Threshold", doorBase + f.Fwd * 0.6f + Vector3.up * 0.05f, new Vector3(1.4f, 0.2f, doorW + 1.4f), h.Yaw, kit.Stone);
            for (int i = 0; i < 5; i++)   // carved runes on the plaque
                Ball(kit.GlowParts, kit, doorBase + Vector3.up * (doorH + 0.9f) + f.Fwd * 0.36f + f.Side * ((i - 2) * (doorW * 0.8f + 0.2f) / 5f), new Vector3(0.05f, 0.4f, 0.1f), h.Yaw);
            Ball(kit.GlowParts, kit, doorBase + f.Side * (doorHalf + 0.85f) + Vector3.up * 1.8f + f.Fwd * 0.2f, new Vector3(0.22f, 0.3f, 0.22f));
            Ball(kit.GlowParts, kit, doorBase - f.Side * (doorHalf + 0.85f) + Vector3.up * 1.8f + f.Fwd * 0.2f, new Vector3(0.22f, 0.3f, 0.22f));
            // a band of runes carved all round the shaft
            for (int i = 0; i < 20; i++)
            {
                float deg = i * 18f + 9f;
                if (Mathf.Abs(Mathf.DeltaAngle(deg, 0f)) < 36f) continue;
                Ball(kit.GlowParts, kit, f.Surface(deg, height * 0.42f, 0.02f), new Vector3(0.05f, 0.34f, 0.1f), h.Yaw - deg);
            }
            // amber glass behind each window
            if (windowDegrees != null)
                foreach (float wd in windowDegrees) Ball(kit.GlowParts, kit, f.Surface(wd, windowY, -0.45f), new Vector3(0.4f, 0.55f, 0.4f));

            if (crown)
            {
                // a crown: a mossy mound and branches curving out and up, with leaf clumps at their tips
                Vector3 top = at + Vector3.up * height;
                Ball(kit.MossParts, kit, top + Vector3.up * 0.2f, new Vector3(topR * 2.1f, 0.9f, topR * 2.1f));
                for (int i = 0; i < 7; i++)
                {
                    float a = i * Mathf.PI * 2f / 7f + 0.4f;
                    Vector3 dir = rot * new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                    var path = new[] { top, top + dir * topR * 1.3f + Vector3.up * 0.9f, top + dir * topR * 2.9f + Vector3.up * 1.2f, top + dir * topR * 4.0f + Vector3.up * 2.2f };
                    Tube(kit.BarkParts, kit, path, topR * 0.4f, 0.1f, 7, 0.3f);
                    Ball(kit.MossParts, kit, path[3] + Vector3.up * 0.3f, new Vector3(1.5f, 0.9f, 1.5f));
                }
            }
            return f;
        }

        // ---------------------------------------------------------------- one house per class

        static void Build(Kit kit, CityLayout.ClassHouse h)
        {
            switch (h.Class)
            {
                case TreefolkClass.DauThe: Bastion(kit, h); break;
                case TreefolkClass.LiepThe: DeadTree(kit, h); break;
                case TreefolkClass.NangThe: SpireTree(kit, h); break;
                case TreefolkClass.QuanThe: MotherTree(kit, h); break;
                case TreefolkClass.DuongThe: BloomTree(kit, h); break;
                case TreefolkClass.XaThe: WatchTree(kit, h); break;
                case TreefolkClass.CauThe: ResinTree(kit, h); break;
                default: HollowLog(kit, h); break;
            }
        }

        /// <summary>A carved statue of a treefolk (plinth, trunk body, head, an arm, an optional lamp), facing the front.</summary>
        static void Statue(Kit kit, Vector3 at, float yaw, float scale, bool lamp, bool shield)
        {
            Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
            Vector3 side = rot * Vector3.forward, fwd = rot * Vector3.right;
            Drum(kit, "Plinth", at - Vector3.up * 0.2f, 0.9f * scale, 0.6f * scale, kit.Stone);
            Tube(kit.StoneParts, kit, new[] { at + Vector3.up * 0.3f * scale, at + Vector3.up * 1.4f * scale, at + Vector3.up * 2.4f * scale }, 0.5f * scale, 0.32f * scale, 10, 0.1f);
            Ball(kit.StoneParts, kit, at + Vector3.up * 2.8f * scale, new Vector3(0.6f, 0.65f, 0.6f) * scale);
            Tube(kit.StoneParts, kit, new[] { at + Vector3.up * 2.1f * scale + side * 0.25f * scale, at + Vector3.up * 2.6f * scale + side * 0.7f * scale, at + Vector3.up * 3.2f * scale + side * 0.8f * scale }, 0.13f * scale, 0.09f * scale, 6, 0.1f);
            if (lamp) Ball(kit.GlowParts, kit, at + Vector3.up * 3.45f * scale + side * 0.8f * scale, new Vector3(0.3f, 0.4f, 0.3f) * scale);
            if (shield) Ball(kit.StoneParts, kit, at + Vector3.up * 1.7f * scale + fwd * 0.45f * scale - side * 0.3f * scale, new Vector3(0.12f, 0.9f, 0.7f) * scale, yaw);
            Capsule(kit, at + Vector3.up * 1.3f * scale, 0.6f * scale, 2.6f * scale);
        }

        // ---- Đấu Thể --------------------------------------------------------------------------------

        static void Bastion(Kit kit, CityLayout.ClassHouse h)
        {
            var f = Trunk(kit, h, 4.2f, 3.6f, 4.6f, 2.0f, 2.6f, new[] { 55f, -55f, 125f, -125f }, 2.6f, false, 6);
            // the crown is a ring of notched stumps (a battlement of wood) with a banner on each side
            Vector3 top = f.At + Vector3.up * 4.6f;
            for (int i = 0; i < 12; i++)
            {
                float a = i * Mathf.PI * 2f / 12f;
                Vector3 p = top + f.Fwd * Mathf.Cos(a) * 3.45f + f.Side * Mathf.Sin(a) * 3.45f;
                Tube(kit.BarkParts, kit, new[] { p - Vector3.up * 0.3f, p + Vector3.up * 0.8f, p + Vector3.up * 1.1f }, 0.34f, 0.26f, 7, 0.2f);
            }
            foreach (float s in new[] { -1f, 1f })
            {
                Vector3 b = f.Surface(s * 38f, 0f, 0.4f);
                Tube(kit.WoodParts, kit, new[] { b - Vector3.up * 0.2f, b + Vector3.up * 4.6f, b + Vector3.up * 6.6f }, 0.16f, 0.07f, 6, 0.1f);
                kit.ClothParts.Add(new CombineInstance { mesh = Quad(1.5f, 2.4f), transform = Matrix4x4.TRS(b + Vector3.up * 6.4f + f.Side * 0.8f * s, Quaternion.Euler(0f, f.Yaw, 0f), Vector3.one) });
            }
            // a stockade of carved posts (each with a shield) in an arc round the front, open at the door
            for (int i = 0; i < 17; i++)
            {
                float a = Mathf.Lerp(-95f, 95f, i / 16f);
                if (Mathf.Abs(a) < 20f) continue;
                Vector3 b = f.Local(Mathf.Cos(a * Mathf.Deg2Rad) * 8.2f, Mathf.Sin(a * Mathf.Deg2Rad) * 8.2f);
                Vector3 outward = f.Fwd * Mathf.Cos(a * Mathf.Deg2Rad) + f.Side * Mathf.Sin(a * Mathf.Deg2Rad);
                Tube(kit.WoodParts, kit, new[] { b - Vector3.up * 0.3f, b + outward * 0.1f + Vector3.up * 1.6f, b + outward * 0.25f + Vector3.up * 2.9f }, 0.26f, 0.08f, 7, 0.15f);
                if (i % 2 == 0) Ball(kit.StoneParts, kit, b + Vector3.up * 1.7f + outward * 0.28f, new Vector3(0.1f, 0.8f, 0.6f), f.Yaw - a);
            }
            Statue(kit, f.Local(6f, -2.8f), f.Yaw + 180f, 1.6f, false, true);
            Statue(kit, f.Local(6f, 2.8f), f.Yaw + 180f, 1.6f, false, true);
            for (int i = 0; i < 3; i++)   // drill dummies and the weapon rack in the yard
            {
                Vector3 p = f.Local(11.2f, -4.5f + i * 4.5f);
                Tube(kit.WoodParts, kit, new[] { p - Vector3.up * 0.2f, p + Vector3.up * 1.0f, p + Vector3.up * 1.8f }, 0.15f, 0.11f, 6);
                Tube(kit.WoodParts, kit, new[] { p + Vector3.up * 1.3f - f.Side * 0.6f, p + Vector3.up * 1.3f + f.Side * 0.6f }, 0.07f, 0.07f, 5);
                Ball(kit.MossParts, kit, p + Vector3.up * 1.95f, new Vector3(0.32f, 0.32f, 0.32f));
            }
            Vector3 rack = f.Local(9.5f, 7.5f);
            Tube(kit.WoodParts, kit, new[] { rack, rack + Vector3.up * 1.6f }, 0.11f, 0.1f, 6);
            Tube(kit.WoodParts, kit, new[] { rack + f.Side * 1.8f, rack + f.Side * 1.8f + Vector3.up * 1.6f }, 0.11f, 0.1f, 6);
            for (int i = 0; i < 6; i++)
            {
                Vector3 b = rack + f.Side * (0.2f + i * 0.28f);
                Tube(kit.WoodParts, kit, new[] { b + Vector3.up * 0.1f, b + Vector3.up * 1.0f + f.Fwd * 0.15f, b + Vector3.up * 2.2f + f.Fwd * 0.25f }, 0.04f, 0.02f, 4, 0f);
            }
            PointLight(kit, f.Local(5f, 0f) + Vector3.up * 3.2f, new Color(1f, 0.7f, 0.4f), 14f, 1.2f);
        }

        // ---- Liệp Thể -------------------------------------------------------------------------------

        static void DeadTree(Kit kit, CityLayout.ClassHouse h)
        {
            var f = Trunk(kit, h, 1.8f, 1.2f, 9.5f, 1.2f, 1.8f, new[] { 90f, -90f }, 3.4f, false, 6);
            Vector3 top = f.At + Vector3.up * 9.5f;
            Quaternion rot = Quaternion.Euler(0f, f.Yaw, 0f);
            // an antlered crown: dead branches forking upward, a nest platform wedged in the trunk below
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI * 2f / 6f + 0.3f;
                Vector3 dir = rot * new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                var path = new[] { top - Vector3.up * 0.4f, top + dir * 1.0f + Vector3.up * 1.4f, top + dir * 2.0f + Vector3.up * 3.2f, top + dir * 1.6f + Vector3.up * 4.8f };
                Tube(kit.DarkParts, kit, path, 0.34f, 0.07f, 6, 0.3f);
                Tube(kit.DarkParts, kit, new[] { path[2], path[2] + dir * 0.9f + Vector3.up * 1.3f, path[2] + dir * 1.4f + Vector3.up * 2.4f }, 0.14f, 0.04f, 5, 0.3f);
            }
            var nest = CaveMeshes.Disc(2.3f, 0.3f, 20);
            var ng = new GameObject("Nest");
            ng.transform.SetParent(kit.Parent, false);
            ng.transform.position = f.At + Vector3.up * 7.0f;
            ng.AddComponent<MeshFilter>().sharedMesh = nest;
            ng.AddComponent<MeshRenderer>().sharedMaterial = kit.DarkWood;
            ng.AddComponent<MeshCollider>().sharedMesh = nest;
            for (int i = 0; i < 14; i++)
            {
                float a = i / 14f * Mathf.PI * 2f;
                Vector3 b = f.At + Vector3.up * 7.3f + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 2.1f;
                Tube(kit.DarkParts, kit, new[] { b, b + Vector3.up * 0.9f }, 0.07f, 0.04f, 4, 0.2f);
            }
            // a camouflage net and trophies: teeth and claws on strings from a branch by the door
            var rnd = new System.Random((int)(f.At.x * 7f + f.At.z * 13f));
            for (int i = 0; i < 12; i++)
            {
                float a = i / 12f * Mathf.PI * 2f + (float)rnd.NextDouble() * 0.3f;
                if (Mathf.Abs(Mathf.DeltaAngle(a * Mathf.Rad2Deg, 0f)) < 35f) continue;
                Vector3 dir = rot * new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                Vector3 start = f.At + Vector3.up * 8.6f + dir * 1.0f, end = OnGround(f.At.x + dir.x * 5.8f, f.At.z + dir.z * 5.8f);
                Tube(kit.DarkParts, kit, new[] { start, Vector3.Lerp(start, end, 0.5f) + Vector3.up * 1.4f, end }, 0.06f, 0.03f, 5, 0.3f);
            }
            Vector3 post = f.Local(3.6f, -1.8f);
            Tube(kit.DarkParts, kit, new[] { post - Vector3.up * 0.2f, post + Vector3.up * 2.0f, post + Vector3.up * 2.5f + f.Side * 1.8f }, 0.13f, 0.08f, 6);
            for (int i = 0; i < 8; i++)
            {
                Vector3 hang = post + Vector3.up * 2.45f + f.Side * (0.4f + i * 0.2f);
                Tube(kit.WoodParts, kit, new[] { hang, hang - Vector3.up * (0.5f + 0.15f * (i % 3)) }, 0.012f, 0.012f, 4, 0f);
                Ball(kit.StoneParts, kit, hang - Vector3.up * (0.55f + 0.15f * (i % 3)), new Vector3(0.05f, 0.14f, 0.05f));
            }
            Vector3 boulder = f.Local(2.5f, 4.4f);   // a boulder scarred with claw marks
            Ball(kit.StoneParts, kit, boulder + Vector3.up * 0.55f, new Vector3(1.6f, 1.1f, 1.3f), 15f);
            for (int i = 0; i < 3; i++) Ball(kit.WoodParts, kit, boulder + Vector3.up * 0.95f + f.Side * (i * 0.22f - 0.22f) - f.Fwd * 0.6f, new Vector3(0.03f, 0.45f, 0.03f), f.Yaw);
        }

        // ---- Năng Thể -------------------------------------------------------------------------------

        static void SpireTree(Kit kit, CityLayout.ClassHouse h)
        {
            var f = Trunk(kit, h, 2.6f, 1.5f, 8.0f, 1.4f, 2.0f, new[] { 70f, -70f, 150f, -150f }, 4.6f, false, 5);
            Vector3 top = f.At + Vector3.up * 8.0f;
            var crystalMat = LakeGround.OpaqueMaterial("Lake_CrystalSpot", new Color(0.35f, 0.85f, 0.9f), 0.8f, false, new Color(0.25f, 1.0f, 1.1f));
            // a vein of glowing crystal spiralling up the trunk
            var vein = new Vector3[18];
            for (int k = 0; k < vein.Length; k++)
            {
                float t = k / 17f, deg = 40f + t * 760f;
                vein[k] = f.Surface(deg, 0.6f + t * 7.0f, 0.12f);
            }
            var veinParts = new List<CombineInstance> { new CombineInstance { mesh = RootMeshes.RootTube(vein, 0.18f, 0.12f, ++kit.Seed, 0.15f, 6), transform = Matrix4x4.identity } };
            var crystals = new List<CombineInstance>();
            var rnd = new System.Random(77);
            for (int i = 0; i < 9; i++)
            {
                float a = i * Mathf.PI * 2f / 9f;
                float len = i == 0 ? 5.4f : 2.0f + (float)rnd.NextDouble() * 1.4f;
                Vector3 b = i == 0 ? top : top + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 0.9f;
                Quaternion lean = i == 0 ? Quaternion.identity : Quaternion.Euler(Mathf.Sin(a) * 22f, 0f, -Mathf.Cos(a) * 22f);
                crystals.Add(new CombineInstance { mesh = kit.Blob, transform = Matrix4x4.TRS(b + lean * Vector3.up * len * 0.5f, lean, new Vector3(0.5f, len, 0.5f)) });
            }
            LakeGround.AddCombined(kit.Parent, "Crystals", crystals, crystalMat);
            LakeGround.AddCombined(kit.Parent, "Crystal Vein", veinParts, crystalMat);
            for (int i = 0; i < 9; i++)
            {
                float a = i * Mathf.PI * 2f / 9f + 0.3f;
                Ball(kit.StoneParts, kit, top + new Vector3(Mathf.Cos(a) * 3.2f, 0.6f + 0.9f * Mathf.Sin(i * 1.9f) + 1.4f, Mathf.Sin(a) * 3.2f), Vector3.one * (0.24f + 0.1f * (i % 3)));
            }
            // a ring of rune obelisks round the tree, each topped with a glowing cap, and a carved rune circle on the ground
            for (int i = 0; i < 7; i++)
            {
                float a = i / 7f * Mathf.PI * 2f + 0.2f;
                if (Mathf.Abs(Mathf.DeltaAngle(a * Mathf.Rad2Deg, 0f)) < 24f) continue;
                Vector3 p = f.Local(Mathf.Cos(a) * 8.5f, Mathf.Sin(a) * 8.5f);
                float oy = f.Yaw - a * Mathf.Rad2Deg;
                Box(kit, "Obelisk", p + Vector3.up * 1.6f, new Vector3(0.7f, 3.4f, 0.7f), oy, kit.Stone);
                Ball(kit.GlowParts, kit, p + Vector3.up * 3.55f, new Vector3(0.5f, 0.6f, 0.5f));
                for (int k = 0; k < 4; k++) Ball(kit.GlowParts, kit, p + Vector3.up * (0.7f + k * 0.6f) + (Quaternion.Euler(0f, oy, 0f) * Vector3.right) * 0.38f, new Vector3(0.04f, 0.25f, 0.2f), oy);
            }
            for (int i = 0; i < 36; i++)
            {
                float a = i / 36f * Mathf.PI * 2f;
                if (Mathf.Abs(Mathf.DeltaAngle(a * Mathf.Rad2Deg, 0f)) < 14f) continue;
                Vector3 p = f.Local(Mathf.Cos(a) * 6.0f, Mathf.Sin(a) * 6.0f);
                Ball(kit.GlowParts, kit, p + Vector3.up * 0.05f, new Vector3(0.16f, 0.03f, 0.05f), f.Yaw - a * Mathf.Rad2Deg + 90f);
            }
            PointLight(kit, top + Vector3.up * 3.5f, new Color(0.45f, 0.95f, 1f), 18f, 1.7f);
        }

        // ---- Quần Thể -------------------------------------------------------------------------------

        static void MotherTree(Kit kit, CityLayout.ClassHouse h)
        {
            var f = Trunk(kit, h, 3.4f, 2.4f, 5.5f, 1.5f, 2.0f, new[] { 80f, -80f }, 3.6f, true, 7);
            // three burrow doors round the foot of the trunk, small arches with a stone lintel, each with a hay bed outside
            foreach (float deg in new[] { 105f, 180f, -105f })
            {
                Vector3 b = f.Surface(deg, 0f, 0.15f);
                Vector3 dir = b - f.At; dir.y = 0f; dir.Normalize();
                Vector3 sd = Vector3.Cross(Vector3.up, dir);
                float by = Mathf.Atan2(-dir.z, dir.x) * Mathf.Rad2Deg;
                Box(kit, "Burrow Lintel", b + Vector3.up * 1.2f + dir * 0.05f, new Vector3(0.5f, 0.3f, 1.5f), by, kit.Stone);
                foreach (float s in new[] { -1f, 1f }) Box(kit, "Burrow Jamb", b + sd * s * 0.65f + Vector3.up * 0.55f + dir * 0.05f, new Vector3(0.45f, 1.1f, 0.35f), by, kit.Stone);
                Ball(kit.GlowParts, kit, b + Vector3.up * 0.5f - dir * 0.2f, new Vector3(0.22f, 0.18f, 0.22f));
                Ball(kit.MossParts, kit, b + dir * 1.6f + Vector3.up * 0.15f, new Vector3(1.6f, 0.4f, 1.1f));
            }
            // a corral of great root arches enclosing the yard in front
            for (int i = 0; i < 7; i++)
            {
                float a = Mathf.Lerp(-110f, 110f, i / 6f);
                if (Mathf.Abs(a) < 22f) continue;
                Vector3 c = f.Local(Mathf.Cos(a * Mathf.Deg2Rad) * 11f + 3f, Mathf.Sin(a * Mathf.Deg2Rad) * 10f);
                Vector3 tang = (Quaternion.Euler(0f, f.Yaw, 0f) * new Vector3(-Mathf.Sin(a * Mathf.Deg2Rad), 0f, Mathf.Cos(a * Mathf.Deg2Rad))).normalized;
                Vector3 p0 = c - tang * 1.5f, p1 = c + tang * 1.5f;
                p0.y = LakeGround.GroundY(p0.x, p0.z); p1.y = LakeGround.GroundY(p1.x, p1.z);
                Tube(kit.BarkParts, kit, RootMeshes.ArchPath(p0, p1, 2.6f, ++kit.Seed, 0.5f, 10, 0.5f), 0.4f, 0.28f, 8, 0.25f);
                Ball(kit.MossParts, kit, (p0 + p1) * 0.5f + Vector3.up * 2.4f, new Vector3(1.4f, 0.5f, 0.8f));
            }
            for (int i = 0; i < 3; i++)
            {
                float a = i * 2.1f + 0.4f;
                Vector3 m = f.Local(8f + Mathf.Cos(a) * 2.6f, Mathf.Sin(a) * 2.6f);
                Ball(kit.MossParts, kit, m, new Vector3(1.7f, 0.7f, 1.4f), i * 40f);
                Ball(kit.GlowParts, kit, m + Vector3.up * 0.2f - f.Fwd * 0.7f, new Vector3(0.2f, 0.16f, 0.2f));
            }
            Vector3 bell = f.Local(5.5f, -5f);
            Tube(kit.WoodParts, kit, new[] { bell - Vector3.up * 0.2f, bell + Vector3.up * 3.0f, bell + Vector3.up * 3.6f + f.Side * 0.8f }, 0.17f, 0.09f, 6);
            Ball(kit.StoneParts, kit, bell + Vector3.up * 3.15f + f.Side * 0.8f, new Vector3(0.5f, 0.6f, 0.5f));
            Tube(kit.WoodParts, kit, new[] { f.Local(5.2f, 5.2f) + Vector3.up * 0.35f, f.Local(6.9f, 5.2f) + Vector3.up * 0.35f }, 0.38f, 0.34f, 8, 0.05f);
        }

        // ---- Dưỡng Thể ------------------------------------------------------------------------------

        static void BloomTree(Kit kit, CityLayout.ClassHouse h)
        {
            var f = Trunk(kit, h, 3.0f, 2.0f, 5.2f, 1.4f, 1.9f, new[] { 75f, -75f, 160f, -160f }, 3.2f, false, 6);
            Vector3 top = f.At + Vector3.up * 5.2f;
            var petals = LakeGround.OpaqueMaterial("Lake_PetalSpot", new Color(0.78f, 0.95f, 0.78f), 0.3f, true, new Color(0.08f, 0.25f, 0.12f));
            var petalParts = new List<CombineInstance>();
            // a canopy of huge petals opening from the top, with a glowing bud, and vines hanging from their tips
            for (int i = 0; i < 9; i++)
            {
                float a = i / 9f * Mathf.PI * 2f;
                Vector3 dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                petalParts.Add(new CombineInstance { mesh = kit.Blob, transform = Matrix4x4.TRS(top + dir * 2.6f + Vector3.up * 1.4f, Quaternion.LookRotation(dir, Vector3.up) * Quaternion.Euler(-32f, 0f, 0f), new Vector3(2.1f, 0.14f, 5.2f)) });
                var vine = FloraMeshes.VinePath(top + dir * 5.0f + Vector3.up * 0.4f, 2.4f + 0.5f * (i % 3), 22000 + i, 0.2f, 7);
                Tube(kit.MossParts, kit, vine, 0.05f, 0.025f, 4, 0f);
            }
            petalParts.Add(new CombineInstance { mesh = kit.Blob, transform = Matrix4x4.TRS(top + Vector3.up * 2.4f, Quaternion.identity, new Vector3(1.6f, 2.2f, 1.6f)) });
            LakeGround.AddCombined(kit.Parent, "Petals", petalParts, petals);
            Ball(kit.GlowParts, kit, top + Vector3.up * 2.5f, new Vector3(0.9f, 1.2f, 0.9f));
            // the healing spring among the roots in front, in a ring of stones, and glowing herb beds
            Vector3 spring = f.Local(6.0f, -4.6f);
            var water = LakeWater.TranslucentMaterial("Lake_HealingWater", null, new Color(0.45f, 1f, 0.85f, 0.6f), new Color(0.05f, 0.25f, 0.2f), 0.9f, (int)RenderQueue.Transparent + 2);
            var disc = new GameObject("Healing Spring");
            disc.transform.SetParent(kit.Parent, false);
            disc.transform.position = spring + Vector3.up * 0.12f;
            disc.AddComponent<MeshFilter>().sharedMesh = WaterMeshes.Disc(1.8f, 0.4f);
            disc.AddComponent<MeshRenderer>().sharedMaterial = water;
            for (int i = 0; i < 12; i++)
            {
                float a = i * Mathf.PI * 2f / 12f;
                Ball(kit.StoneParts, kit, spring + new Vector3(Mathf.Cos(a) * 2.0f, 0.15f, Mathf.Sin(a) * 2.0f), new Vector3(0.5f, 0.34f, 0.42f), i * 30f);
            }
            var herbs = LakeGround.OpaqueMaterial("Lake_HerbSpot", new Color(0.45f, 0.95f, 0.65f), 0.4f, false, new Color(0.18f, 0.7f, 0.4f));
            var stalks = new List<CombineInstance>(); var capsList = new List<CombineInstance>();
            var rnd = new System.Random(88);
            for (int i = 0; i < 26; i++)
            {
                float a = (float)rnd.NextDouble() * Mathf.PI * 2f, d = 5.2f + (float)rnd.NextDouble() * 4.5f;
                Vector3 p = f.At + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * d; p.y = LakeGround.GroundY(p.x, p.z);
                FloraMeshes.Mushroom(21000 + i, 0.4f + (float)rnd.NextDouble() * 0.7f, out var st, out var cp, out _);
                stalks.Add(new CombineInstance { mesh = st, transform = Matrix4x4.Translate(p) });
                capsList.Add(new CombineInstance { mesh = cp, transform = Matrix4x4.Translate(p) });
            }
            LakeGround.AddCombined(kit.Parent, "Herb Stalks", stalks, LakeGround.OpaqueMaterial("Lake_MushroomStalk", new Color(0.93f, 0.89f, 0.78f), 0.3f, false));
            LakeGround.AddCombined(kit.Parent, "Herb Caps", capsList, herbs);
            Statue(kit, f.Local(7.5f, 3.4f), f.Yaw + 180f, 1.2f, true, false);   // a carved healer holding a lamp
            PointLight(kit, top + Vector3.up * 2f, new Color(0.7f, 1f, 0.8f), 14f, 1.3f);
        }

        // ---- Xạ Thể ---------------------------------------------------------------------------------

        static void WatchTree(Kit kit, CityLayout.ClassHouse h)
        {
            const float height = 10.5f;
            var f = Trunk(kit, h, 2.5f, 1.5f, height, 1.3f, 1.9f, new[] { 80f, -80f, 150f }, 4.0f, false, 6);
            Vector3 top = f.At + Vector3.up * height;
            // a stair carved round the outside of the trunk, step after step, up to a nest platform in the crown
            const int steps = 34;
            for (int i = 0; i < steps; i++)
            {
                float t = i / (float)steps, deg = 20f + t * 720f;
                float y = 0.35f + t * (height - 0.9f);
                Vector3 p = f.Surface(deg, y, 0.9f);
                var mesh = CaveMeshes.Disc(1.05f, 0.24f, 10);
                var go = new GameObject("Stair Step");
                go.transform.SetParent(kit.Parent, false);
                go.transform.position = p - Vector3.up * 0.22f;
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                go.AddComponent<MeshRenderer>().sharedMaterial = kit.Wood;
                go.AddComponent<MeshCollider>().sharedMesh = mesh;
                go.isStatic = true;
            }
            var plat = CaveMeshes.Disc(3.6f, 0.35f, 28);
            var pg = new GameObject("Lookout Platform");
            pg.transform.SetParent(kit.Parent, false);
            pg.transform.position = top - Vector3.up * 0.1f;
            pg.AddComponent<MeshFilter>().sharedMesh = plat;
            pg.AddComponent<MeshRenderer>().sharedMaterial = kit.Wood;
            pg.AddComponent<MeshCollider>().sharedMesh = plat;
            for (int i = 0; i < 18; i++)
            {
                float a = i / 18f * Mathf.PI * 2f;
                Vector3 b = top + Vector3.up * 0.25f + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 3.4f;
                Tube(kit.WoodParts, kit, new[] { b, b + Vector3.up * 1.2f }, 0.07f, 0.06f, 4, 0f);
            }
            for (int i = 0; i < 5; i++)   // branches curving over the platform like a roof, with leaf clumps
            {
                float a = i * Mathf.PI * 2f / 5f;
                Vector3 dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                var path = new[] { top + Vector3.up * 0.3f, top + dir * 1.2f + Vector3.up * 2.4f, top + dir * 3.4f + Vector3.up * 3.6f, top + dir * 5.2f + Vector3.up * 3.0f };
                Tube(kit.BarkParts, kit, path, 0.34f, 0.1f, 7, 0.3f);
                Ball(kit.MossParts, kit, path[2] + Vector3.up * 0.4f, new Vector3(2.4f, 0.8f, 2.4f));
            }
            Ball(kit.GlowParts, kit, top + Vector3.up * 2.8f, new Vector3(0.3f, 0.4f, 0.3f));
            // the archery range in front: targets 10 to 20 m out on carved posts, and a rack of bows
            for (int i = 0; i < 4; i++)
            {
                Vector3 p = f.Local(10f + i * 3.4f, -4f + i * 2.6f);
                Tube(kit.WoodParts, kit, new[] { p - Vector3.up * 0.1f, p + Vector3.up * 1.2f }, 0.07f, 0.06f, 5, 0f);
                Ball(kit.StoneParts, kit, p + Vector3.up * 1.25f, new Vector3(0.12f, 0.95f, 0.95f), f.Yaw + 180f);
                Ball(kit.GlowParts, kit, p + Vector3.up * 1.25f - f.Fwd * 0.08f, new Vector3(0.05f, 0.28f, 0.28f), f.Yaw + 180f);
            }
            Vector3 rack = f.Local(5.5f, 4.2f);
            Tube(kit.WoodParts, kit, new[] { rack, rack + Vector3.up * 1.4f }, 0.09f, 0.08f, 5);
            Tube(kit.WoodParts, kit, new[] { rack + f.Side * 2.0f, rack + f.Side * 2.0f + Vector3.up * 1.4f }, 0.09f, 0.08f, 5);
            for (int i = 0; i < 4; i++)
            {
                var bow = new Vector3[5];
                for (int k = 0; k < 5; k++) { float t = k / 4f; bow[k] = rack + f.Side * (0.3f + i * 0.45f) + Vector3.up * (0.1f + t * 1.2f) + f.Fwd * Mathf.Sin(t * Mathf.PI) * 0.22f; }
                Tube(kit.WoodParts, kit, bow, 0.028f, 0.028f, 4, 0f);
            }
        }

        // ---- Cấu Thể --------------------------------------------------------------------------------

        static void ResinTree(Kit kit, CityLayout.ClassHouse h)
        {
            var f = Trunk(kit, h, 3.2f, 2.3f, 5.6f, 1.4f, 1.9f, null, 0f, true, 5);
            // big arched windows glazed with amber resin (translucent), a row round the trunk
            var resin = LakeWater.TranslucentMaterial("Lake_AmberResin", null, new Color(0.95f, 0.65f, 0.2f, 0.55f), new Color(0.35f, 0.18f, 0.02f), 0.9f, (int)RenderQueue.Transparent + 3);
            var panes = new List<CombineInstance>();
            foreach (float deg in new[] { 70f, -70f, 140f, -140f, 180f })
            {
                Vector3 c = f.Surface(deg, 2.9f, 0.06f);
                Vector3 outward = c - f.At; outward.y = 0f; outward.Normalize();
                Quaternion look = Quaternion.LookRotation(-outward, Vector3.up);
                panes.Add(new CombineInstance { mesh = kit.Blob, transform = Matrix4x4.TRS(c, look, new Vector3(1.0f, 1.5f, 0.12f)) });
                var arch = new Vector3[7];
                Vector3 across = Vector3.Cross(Vector3.up, outward);
                for (int k = 0; k < arch.Length; k++) { float t = k / 6f * Mathf.PI; arch[k] = c + across * Mathf.Cos(t) * 0.62f + Vector3.up * (Mathf.Sin(t) * 0.9f - 0.2f) + outward * 0.1f; }
                Tube(kit.StoneParts, kit, arch, 0.1f, 0.1f, 6, 0.05f);
            }
            LakeGround.AddCombined(kit.Parent, "Resin Panes", panes, resin);
            // a stone chimney, and tall stone stills joined by root pipes
            Vector3 chim = f.Surface(-150f, 0f, 0.5f);
            Drum(kit, "Chimney", chim - Vector3.up * 0.3f, 0.7f, 7.4f, kit.Stone);
            Ball(kit.GlowParts, kit, chim + Vector3.up * 7.2f, new Vector3(0.5f, 0.3f, 0.5f));
            var gel = LakeWater.TranslucentMaterial("Lake_GelDome", null, new Color(0.72f, 0.86f, 0.50f, 0.5f), Color.black, 0.92f, (int)RenderQueue.Transparent + 1);
            var cores = LakeGround.OpaqueMaterial("Lake_GelCoreSpot", new Color(0.95f, 0.62f, 0.18f), 0.4f, false, new Color(0.9f, 0.5f, 0.12f));
            var vats = new List<CombineInstance>(); var coreParts = new List<CombineInstance>();
            Vector3 prev = default;
            for (int i = 0; i < 5; i++)
            {
                Vector3 p = f.Local(5.8f + (i % 2) * 2.4f, -5f + i * 2.5f);
                float rad = 0.75f + 0.12f * (i % 3);
                Drum(kit, "Plinth", p - Vector3.up * 0.2f, rad * 0.95f, 0.8f, kit.Stone);
                var profile = new List<Vector2> { new Vector2(0f, rad * 1.7f), new Vector2(0.55f * rad, rad * 1.6f), new Vector2(0.95f * rad, rad * 0.95f), new Vector2(rad, rad * 0.25f), new Vector2(rad * 0.95f, 0f) };
                vats.Add(new CombineInstance { mesh = FloraMeshes.Lathe(profile, 14, rad * 0.5f, 23000 + i, 0.04f), transform = Matrix4x4.Translate(p + Vector3.up * 0.6f) });
                coreParts.Add(new CombineInstance { mesh = kit.Blob, transform = Matrix4x4.TRS(p + Vector3.up * (0.6f + rad * 0.75f), Quaternion.identity, Vector3.one * rad * 0.75f) });
                if (i > 0) Tube(kit.BarkParts, kit, new[] { prev + Vector3.up * 1.7f, Vector3.Lerp(prev, p, 0.5f) + Vector3.up * 2.4f, p + Vector3.up * 1.7f }, 0.07f, 0.07f, 5, 0.1f);
                prev = p;
            }
            LakeGround.AddCombined(kit.Parent, "Gel Vats", vats, gel);
            LakeGround.AddCombined(kit.Parent, "Vat Cores", coreParts, cores);
            Vector3 shelf = f.Local(3.8f, 5.6f);   // a shelf of jars
            Tube(kit.WoodParts, kit, new[] { shelf + Vector3.up * 1.0f - f.Side * 1.4f, shelf + Vector3.up * 1.0f + f.Side * 1.4f }, 0.22f, 0.22f, 6, 0f);
            Tube(kit.WoodParts, kit, new[] { shelf - f.Side * 1.4f, shelf - f.Side * 1.4f + Vector3.up * 1.0f }, 0.1f, 0.1f, 5);
            Tube(kit.WoodParts, kit, new[] { shelf + f.Side * 1.4f, shelf + f.Side * 1.4f + Vector3.up * 1.0f }, 0.1f, 0.1f, 5);
            for (int i = 0; i < 7; i++) Ball(kit.GlowParts, kit, shelf + Vector3.up * 1.4f + f.Side * (-1.2f + i * 0.4f), new Vector3(0.17f, 0.28f, 0.17f));
            PointLight(kit, f.At + Vector3.up * 3f + f.Fwd * 3f, new Color(1f, 0.7f, 0.3f), 14f, 1.3f);
        }

        // ---- Dã Thể ---------------------------------------------------------------------------------

        static void HollowLog(Kit kit, CityLayout.ClassHouse h)
        {
            // a huge hollow log lying along the front axis: open at the front end, sealed at the back; carved door-posts and runes
            Quaternion rot = Quaternion.Euler(0f, h.Yaw, 0f);
            Vector3 at = OnGround(h.Position.x, h.Position.y);
            Vector3 fwd = rot * Vector3.right, side = rot * Vector3.forward;
            System.Func<float, float, Vector3> L = (x, z) => { var p = at + fwd * x + side * z; return OnGround(p.x, p.z); };
            const float r = 2.5f, len = 10f;
            var profile = new List<Vector2>();
            for (int i = 0; i <= 8; i++) profile.Add(new Vector2(r * (1f + 0.08f * Mathf.Sin(i * 1.1f)), i / 8f * len));
            var tube = FloraMeshes.Lathe(profile, 28, len * 0.5f, ++kit.Seed, 0.035f);
            // laid on its side: the lathe's top end (y = len) becomes the front (+x), its axis r above the ground
            Quaternion lie = rot * Quaternion.Euler(0f, 0f, -90f);
            Vector3 start = L(-len * 0.35f, 0f) + Vector3.up * (r * 0.92f);
            Solid(kit, "Hollow Log", tube, start, lie, kit.Trunk);
            Vector3 mouth = start + fwd * len;
            // the sealed back, runes ringed round the open mouth, bark ridges along the log, a mossy top, broken branch stubs
            Ball(kit.WoodParts, kit, start + fwd * 0.2f, new Vector3(0.7f, r * 2.0f, r * 2.0f), h.Yaw);
            for (int i = 0; i < 14; i++)
            {
                float a = i / 14f * Mathf.PI * 2f;
                if (Mathf.Sin(a) < -0.7f) continue;
                Vector3 c = mouth + side * Mathf.Cos(a) * (r + 0.1f) + Vector3.up * Mathf.Sin(a) * (r + 0.1f);
                Ball(kit.GlowParts, kit, c - fwd * 0.2f, new Vector3(0.1f, 0.36f, 0.08f), h.Yaw);
            }
            for (int i = 0; i < 9; i++)
            {
                float a = (i / 9f - 0.5f) * Mathf.PI * 1.5f;
                var path = new Vector3[6];
                for (int k = 0; k < path.Length; k++) path[k] = start + fwd * (k / 5f * len) + side * Mathf.Sin(a) * r + Vector3.up * Mathf.Cos(a) * r;
                Tube(kit.BarkParts, kit, path, 0.28f, 0.2f, 7, 0.25f);
            }
            Ball(kit.MossParts, kit, start + fwd * len * 0.5f + Vector3.up * (r * 0.95f), new Vector3(len * 0.8f, 0.55f, r * 1.1f), h.Yaw + 90f);
            for (int i = 0; i < 4; i++)
            {
                float sgn = i % 2 == 0 ? 1f : -1f;
                Vector3 b = start + fwd * (2f + i * 2.3f) + Vector3.up * r * 0.9f + side * 0.9f * sgn;
                Tube(kit.BarkParts, kit, new[] { b, b + Vector3.up * 1.2f + side * 0.6f * sgn, b + Vector3.up * 2.0f + side * 0.9f * sgn }, 0.22f, 0.07f, 6, 0.3f);
            }
            foreach (float s in new[] { -1f, 1f })   // carved door-posts at the mouth
            {
                Vector3 p = mouth + side * s * (r + 0.7f);
                p.y = LakeGround.GroundY(p.x, p.z);
                Statue(kit, p + fwd * 0.8f, h.Yaw + 180f, 1.0f, true, false);
            }
            // the camp in front: fire pit with a warm light, lean-to, map post, gear pile, hammock
            float cx = len * 0.65f;
            Vector3 fire = L(cx + 3.5f, -0.5f);
            for (int i = 0; i < 9; i++)
            {
                float a = i * Mathf.PI * 2f / 9f;
                Ball(kit.StoneParts, kit, fire + new Vector3(Mathf.Cos(a) * 0.8f, 0.12f, Mathf.Sin(a) * 0.8f), new Vector3(0.36f, 0.26f, 0.3f), i * 40f);
            }
            Ball(kit.GlowParts, kit, fire + Vector3.up * 0.2f, new Vector3(0.7f, 0.3f, 0.7f));
            for (int i = 0; i < 3; i++) Tube(kit.WoodParts, kit, new[] { fire + new Vector3(Mathf.Cos(i * 2.1f), 0.15f, Mathf.Sin(i * 2.1f)) * 0.7f, fire + Vector3.up * 0.5f }, 0.07f, 0.05f, 4, 0f);
            PointLight(kit, fire + Vector3.up * 1.0f, new Color(1f, 0.6f, 0.25f), 12f, 1.4f);
            Vector3 a0 = L(cx + 2.5f, -4.8f), a1 = L(cx + 2.5f, -7.8f);
            Tube(kit.WoodParts, kit, new[] { a0 - Vector3.up * 0.2f, a0 + Vector3.up * 2.2f }, 0.12f, 0.1f, 5);
            Tube(kit.WoodParts, kit, new[] { a1 - Vector3.up * 0.2f, a1 + Vector3.up * 2.2f }, 0.12f, 0.1f, 5);
            for (int i = 0; i < 5; i++)
            {
                Vector3 e = Vector3.Lerp(a0, a1, i / 4f);
                Tube(kit.BarkParts, kit, new[] { e + Vector3.up * 2.2f, e + fwd * 2.6f + Vector3.up * 0.3f }, 0.09f, 0.06f, 5, 0.1f);
            }
            Ball(kit.MossParts, kit, L(cx + 3.8f, -6.3f) + Vector3.up * 1.3f, new Vector3(2.6f, 0.28f, 3.8f), h.Yaw);
            Vector3 map = L(cx + 2.0f, 4.4f);
            Tube(kit.WoodParts, kit, new[] { map - Vector3.up * 0.2f, map + Vector3.up * 2.4f }, 0.1f, 0.09f, 5);
            Ball(kit.StoneParts, kit, map + Vector3.up * 1.9f - fwd * 0.13f, new Vector3(0.07f, 0.8f, 1.0f), h.Yaw + 180f);
            Vector3 pile = L(cx + 0.5f, 6.4f);
            for (int i = 0; i < 6; i++) Ball(kit.WoodParts, kit, pile + new Vector3(Mathf.Sin(i * 2.2f) * 0.55f, 0.2f + 0.2f * (i % 3), Mathf.Cos(i * 1.7f) * 0.55f), new Vector3(0.55f, 0.32f, 0.44f), i * 55f);
            Vector3 h0 = L(cx + 6.5f, 3.5f), h1 = L(cx + 6.5f, 7.2f);
            Tube(kit.WoodParts, kit, new[] { h0 - Vector3.up * 0.2f, h0 + Vector3.up * 2.0f }, 0.12f, 0.1f, 5);
            Tube(kit.WoodParts, kit, new[] { h1 - Vector3.up * 0.2f, h1 + Vector3.up * 2.0f }, 0.12f, 0.1f, 5);
            var sling = new Vector3[7];
            for (int i = 0; i < sling.Length; i++) { float t = i / 6f; sling[i] = Vector3.Lerp(h0 + Vector3.up * 1.8f, h1 + Vector3.up * 1.8f, t) - Vector3.up * 0.8f * Mathf.Sin(t * Mathf.PI); }
            Tube(kit.MossParts, kit, sling, 0.22f, 0.22f, 6, 0.05f);
        }
    }
}
