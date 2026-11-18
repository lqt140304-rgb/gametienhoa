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
    /// Evolve → Dress Lake: Class Houses. One building for each of the eight treefolk classes, on the sites chosen by
    /// CityLayout.ClassHouses, each with its own architecture (none is a round village hut) and its own surroundings:
    ///  Đấu Thể   a square stone fort: crenellated walls, a gate, two round corner towers, a keep, banners, a drill yard.
    ///  Liệp Thể  a hunter's cabin on four tall stilts, reached by a long ramp, under a camouflage net, with trophies.
    ///  Năng Thể  a three-tier stone temple ringed by pillars, a crystal spire on top and stones floating round it.
    ///  Quần Thể  a long vaulted barn open at the front, its pen, a bell post and a feeding trough.
    ///  Dưỡng Thể an open pavilion on a stone floor under a roof of great petals, a healing spring in the middle, herb beds.
    ///  Xạ Thể    a tall root tower with a spiral stair round it, a lookout platform and roof, and an archery range.
    ///  Cấu Thể   a glasshouse of translucent gel on a stone ring, ribbed with roots, full of vats and jars.
    ///  Dã Thể    a conical hide tent, a fire pit, a lean-to, a map post, a hammock and a pile of gear.
    /// Run again to rebuild.
    /// </summary>
    public static class LakeClassHouses
    {
        const string DressedName = "Treefolk Class Houses (Dressed)";

        class Kit
        {
            public Material Bark, Stone, Wood, Amber, Moss, DarkWood, Cloth;
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

        [MenuItem("Evolve/Dress Lake: Class Houses")]
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
                Stone = LakeGround.OpaqueMaterial("Lake_Limestone", new Color(0.74f, 0.72f, 0.64f), 0.2f, false),
                Wood = LakeGround.OpaqueMaterial("Lake_PetrifiedWood", new Color(0.46f, 0.36f, 0.26f), 0.35f, false),
                DarkWood = LakeGround.OpaqueMaterial("Lake_DarkRoot", new Color(0.16f, 0.12f, 0.09f), 0.1f, false),
                Amber = LakeGround.OpaqueMaterial("Lake_AmberLampSpot", new Color(0.95f, 0.62f, 0.18f), 0.6f, false, new Color(1.2f, 0.62f, 0.14f)),
                Moss = LakeGround.MossMaterial(),
                Cloth = LakeGround.OpaqueMaterial("Lake_Cloth", new Color(0.62f, 0.2f, 0.16f), 0.1f, true),
                Blob = FloraMeshes.Blob(),
            };
            var houses = CityLayout.ClassHouses();
            foreach (var h in houses)
            {
                var parent = new GameObject(TreefolkClasses.Name(h.Class)).transform;
                parent.SetParent(root, false);
                kit.Parent = parent;
                Build(parent, kit, h);
                // each house's loose pieces are combined per house, so a house is one handful of meshes
                Flush(parent, kit);
            }

            EditorSceneManager.MarkAllScenesDirty();
            AssetDatabase.SaveAssets();
            Debug.Log($"Đã dựng {houses.Length} nhà của các class người cây, mỗi nhà một kiến trúc riêng. Bấm Ctrl+S để lưu scene.");
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

        // ---------------------------------------------------------------- small helpers

        static Vector3 OnGround(float x, float z) => new Vector3(x, LakeGround.GroundY(x, z), z);

        static void Tube(List<CombineInstance> list, Kit kit, Vector3[] path, float r0, float r1, int sides = 7, float knobs = 0.18f) =>
            list.Add(new CombineInstance { mesh = RootMeshes.RootTube(path, r0, r1, ++kit.Seed, knobs, sides), transform = Matrix4x4.identity });

        static void Ball(List<CombineInstance> list, Kit kit, Vector3 at, Vector3 size, float yaw = 0f) =>
            list.Add(new CombineInstance { mesh = kit.Blob, transform = Matrix4x4.TRS(at, Quaternion.Euler(0f, yaw, 0f), size) });

        /// <summary>A solid box (with a collider) centred at <paramref name="centre"/>, turned by <paramref name="yaw"/>.</summary>
        static GameObject Box(Kit kit, string name, Vector3 centre, Vector3 size, float yaw, Material mat, float pitch = 0f)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(kit.Parent, false);
            go.transform.position = centre;
            go.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            go.isStatic = true;
            return go;
        }

        /// <summary>A solid upright cylinder (closed, with a mesh collider): a pillar, a tower, a tier.</summary>
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

        static GameObject Solid(Kit kit, string name, Mesh mesh, Vector3 at, float yaw, Material mat, bool collide = true)
        {
            var go = new GameObject(name);
            go.transform.SetParent(kit.Parent, false);
            go.transform.position = at;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.TwoSided;
            if (collide) go.AddComponent<MeshCollider>().sharedMesh = mesh;
            go.isStatic = true;
            return go;
        }

        static void PointLight(Kit kit, Vector3 at, Color color, float range, float intensity)
        {
            var go = new GameObject("Light");
            go.transform.SetParent(kit.Parent, false);
            go.transform.position = at;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point; l.range = range; l.intensity = intensity; l.color = color; l.shadows = LightShadows.None;
        }

        // ---------------------------------------------------------------- one building per class

        static void Build(Transform parent, Kit kit, CityLayout.ClassHouse h)
        {
            Vector2 pos = h.Position;
            float yaw = h.Yaw;
            var sign = parent.gameObject.AddComponent<ClassHouseSign>();
            sign.Class = h.Class;
            Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
            Vector3 fwd = rot * Vector3.right, side = rot * Vector3.forward;   // the front (+x local) and the sides (+z local)
            Vector3 at = OnGround(pos.x, pos.y);
            sign.transform.position = at;
            Vector3 Local(float x, float z) { var p = at + fwd * x + side * z; return OnGround(p.x, p.z); }
            switch (h.Class)
            {
                case TreefolkClass.DauThe: Fort(kit, at, yaw, fwd, side, Local); sign.Height = 8f; break;
                case TreefolkClass.LiepThe: Stilts(kit, at, yaw, fwd, side, Local); sign.Height = 7f; break;
                case TreefolkClass.NangThe: Temple(kit, at, yaw, fwd, side, Local); sign.Height = 9f; break;
                case TreefolkClass.QuanThe: Barn(kit, at, yaw, fwd, side, Local); sign.Height = 6.5f; break;
                case TreefolkClass.DuongThe: Pavilion(kit, at, yaw, fwd, side, Local); sign.Height = 6f; break;
                case TreefolkClass.XaThe: Tower(kit, at, yaw, fwd, side, Local); sign.Height = 12f; break;
                case TreefolkClass.CauThe: Glasshouse(kit, at, yaw, fwd, side, Local); sign.Height = 6f; break;
                default: Tent(kit, at, yaw, fwd, side, Local); sign.Height = 6f; break;
            }
        }

        // ---- Đấu Thể: the fort -----------------------------------------------------------------------

        static void Fort(Kit kit, Vector3 at, float yaw, Vector3 fwd, Vector3 side, System.Func<float, float, Vector3> L)
        {
            const float half = 4.4f, wallH = 3.4f, thick = 0.9f;
            // four walls of stone blocks: the front (+x) has a gate 1.8 m wide
            Box(kit, "Wall Back", L(-half, 0f) + Vector3.up * wallH * 0.5f, new Vector3(thick, wallH, half * 2f), yaw, kit.Stone);
            Box(kit, "Wall Left", L(0f, -half) + Vector3.up * wallH * 0.5f, new Vector3(half * 2f, wallH, thick), yaw, kit.Stone);
            Box(kit, "Wall Right", L(0f, half) + Vector3.up * wallH * 0.5f, new Vector3(half * 2f, wallH, thick), yaw, kit.Stone);
            float gate = 0.9f, seg = half - gate;
            Box(kit, "Wall Front L", L(half, -(gate + seg * 0.5f)) + Vector3.up * wallH * 0.5f, new Vector3(thick, wallH, seg), yaw, kit.Stone);
            Box(kit, "Wall Front R", L(half, gate + seg * 0.5f) + Vector3.up * wallH * 0.5f, new Vector3(thick, wallH, seg), yaw, kit.Stone);
            Box(kit, "Gate Lintel", L(half, 0f) + Vector3.up * (wallH - 0.55f), new Vector3(thick + 0.2f, 1.1f, gate * 2f + 0.6f), yaw, kit.Stone);
            // crenellations along every wall top
            for (float t = -half; t <= half; t += 1.5f)
            {
                foreach (var (x, z) in new[] { (-half, t), (t, -half), (t, half) })
                    Box(kit, "Merlon", L(x, z) + Vector3.up * (wallH + 0.35f), new Vector3(0.7f, 0.7f, 0.7f), yaw, kit.Stone);
                if (Mathf.Abs(t) > gate + 0.4f) Box(kit, "Merlon", L(half, t) + Vector3.up * (wallH + 0.35f), new Vector3(0.7f, 0.7f, 0.7f), yaw, kit.Stone);
            }
            // two round corner towers at the front, taller, with conical roofs and a banner each
            foreach (float z in new[] { -half, half })
            {
                Vector3 b = L(half, z);
                Drum(kit, "Tower", b - Vector3.up * 0.3f, 1.4f, 5.6f, kit.Stone);
                Ball(kit.MossParts, kit, b + Vector3.up * 6.3f, new Vector3(2.0f, 1.5f, 2.0f));
                Tube(kit.WoodParts, kit, new[] { b + Vector3.up * 6.8f, b + Vector3.up * 8.6f }, 0.06f, 0.05f, 4, 0f);
                kit.ClothParts.Add(new CombineInstance { mesh = Quad(1.2f, 0.9f), transform = Matrix4x4.TRS(b + Vector3.up * 8.2f + side * 0.6f * Mathf.Sign(z), Quaternion.Euler(0f, yaw, 0f), Vector3.one) });
            }
            // the keep inside: a raised block with a little stair and amber windows
            Box(kit, "Keep", L(-1.6f, 0f) + Vector3.up * 2.4f, new Vector3(3.0f, 4.8f, 3.4f), yaw, kit.Wood);
            for (int i = 0; i < 3; i++) Box(kit, "Keep Step", L(0.2f + i * 0f, 0f) + Vector3.up * (0.12f + 0.24f * (2 - i)) - fwd * (0.0f - 0.35f * i), new Vector3(0.6f, 0.24f + 0.24f * (2 - i), 1.2f), yaw, kit.Stone);
            for (int i = 0; i < 3; i++) Ball(kit.GlowParts, kit, L(-0.1f, -1f + i) + Vector3.up * 3.4f, new Vector3(0.12f, 0.45f, 0.3f), yaw);
            // outside: a short palisade of stakes either side of the gate, drill dummies and a weapon rack
            for (int i = 0; i < 8; i++)
            {
                float z = (i < 4 ? -1f : 1f) * (2.4f + (i % 4) * 0.55f);
                Vector3 b = L(half + 2.4f, z);
                Tube(kit.WoodParts, kit, new[] { b - Vector3.up * 0.2f, b + fwd * 0.15f + Vector3.up * 1.3f, b + fwd * 0.3f + Vector3.up * 2.3f }, 0.17f, 0.04f, 6, 0.1f);
            }
            for (int i = 0; i < 3; i++)
            {
                Vector3 p = L(half + 5.5f, -4f + i * 4f);
                Tube(kit.WoodParts, kit, new[] { p - Vector3.up * 0.2f, p + Vector3.up * 1.0f, p + Vector3.up * 1.7f }, 0.13f, 0.1f, 6);
                Tube(kit.WoodParts, kit, new[] { p + Vector3.up * 1.25f - side * 0.55f, p + Vector3.up * 1.25f + side * 0.55f }, 0.07f, 0.07f, 5);
                Ball(kit.MossParts, kit, p + Vector3.up * 1.85f, new Vector3(0.3f, 0.3f, 0.3f));
            }
            Vector3 rack = L(half + 3.5f, 5.4f);
            Tube(kit.WoodParts, kit, new[] { rack, rack + Vector3.up * 1.5f }, 0.1f, 0.09f, 6);
            Tube(kit.WoodParts, kit, new[] { rack + side * 1.6f, rack + side * 1.6f + Vector3.up * 1.5f }, 0.1f, 0.09f, 6);
            for (int i = 0; i < 6; i++)
            {
                Vector3 b = rack + side * (0.15f + i * 0.26f);
                Tube(kit.WoodParts, kit, new[] { b + Vector3.up * 0.1f, b + Vector3.up * 1.0f + fwd * 0.15f, b + Vector3.up * 2.1f + fwd * 0.25f }, 0.04f, 0.02f, 4, 0f);
            }
            PointLight(kit, L(half + 1f, 0f) + Vector3.up * 3.2f, new Color(1f, 0.7f, 0.4f), 14f, 1.2f);
        }

        /// <summary>A flat double-sided cloth quad facing sideways (for banners).</summary>
        static Mesh Quad(float w, float h)
        {
            var m = new Mesh { name = "Cloth" };
            m.vertices = new[] { new Vector3(0f, 0f, -w * 0.5f), new Vector3(0f, 0f, w * 0.5f), new Vector3(0f, -h, -w * 0.5f), new Vector3(0f, -h, w * 0.5f) };
            m.triangles = new[] { 0, 1, 2, 1, 3, 2 };
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        // ---- Liệp Thể: the stilt cabin ------------------------------------------------------------------

        static void Stilts(Kit kit, Vector3 at, float yaw, Vector3 fwd, Vector3 side, System.Func<float, float, Vector3> L)
        {
            const float floor = 3.2f;
            // four tall stilts, braced, carrying a floor
            foreach (var (x, z) in new[] { (-1.8f, -1.8f), (-1.8f, 1.8f), (1.8f, -1.8f), (1.8f, 1.8f) })
            {
                Vector3 b = L(x, z);
                Tube(kit.DarkParts, kit, new[] { b - Vector3.up * 0.5f, b + Vector3.up * floor * 0.5f + fwd * 0.08f, b + Vector3.up * (floor + 0.2f) }, 0.28f, 0.2f, 8, 0.2f);
                var col = new GameObject("Stilt").AddComponent<CapsuleCollider>();
                col.transform.SetParent(kit.Parent, false);
                col.transform.position = b + Vector3.up * floor * 0.5f; col.radius = 0.3f; col.height = floor;
            }
            Tube(kit.DarkParts, kit, new[] { L(-1.8f, -1.8f) + Vector3.up * 1.0f, L(1.8f, 1.8f) + Vector3.up * 2.0f }, 0.07f, 0.07f, 5, 0.1f);
            Tube(kit.DarkParts, kit, new[] { L(1.8f, -1.8f) + Vector3.up * 1.0f, L(-1.8f, 1.8f) + Vector3.up * 2.0f }, 0.07f, 0.07f, 5, 0.1f);
            Vector3 centre = at + Vector3.up * floor;
            Box(kit, "Floor", centre + Vector3.up * 0.1f, new Vector3(4.6f, 0.25f, 4.6f), yaw, kit.DarkWood);
            // cabin walls (a gap for the door at the front), an A-frame roof, a veranda
            Box(kit, "Wall Back", L(-1.9f, 0f) + Vector3.up * (floor + 1.3f), new Vector3(0.25f, 2.4f, 4.0f), yaw, kit.DarkWood);
            Box(kit, "Wall Left", L(0f, -1.9f) + Vector3.up * (floor + 1.3f), new Vector3(3.8f, 2.4f, 0.25f), yaw, kit.DarkWood);
            Box(kit, "Wall Right", L(0f, 1.9f) + Vector3.up * (floor + 1.3f), new Vector3(3.8f, 2.4f, 0.25f), yaw, kit.DarkWood);
            Box(kit, "Wall Front L", L(1.9f, -1.35f) + Vector3.up * (floor + 1.3f), new Vector3(0.25f, 2.4f, 1.3f), yaw, kit.DarkWood);
            Box(kit, "Wall Front R", L(1.9f, 1.35f) + Vector3.up * (floor + 1.3f), new Vector3(0.25f, 2.4f, 1.3f), yaw, kit.DarkWood);
            Box(kit, "Roof Left", L(0f, -1.15f) + Vector3.up * (floor + 3.0f), new Vector3(4.6f, 0.2f, 2.6f), yaw, kit.DarkWood, 0f).transform.rotation = Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(-32f, 0f, 0f);
            Box(kit, "Roof Right", L(0f, 1.15f) + Vector3.up * (floor + 3.0f), new Vector3(4.6f, 0.2f, 2.6f), yaw, kit.DarkWood, 0f).transform.rotation = Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(32f, 0f, 0f);
            Box(kit, "Veranda", L(3.1f, 0f) + Vector3.up * (floor + 0.1f), new Vector3(1.8f, 0.2f, 3.2f), yaw, kit.DarkWood);
            // a long ramp up from the ground to the veranda
            Vector3 rampLow = L(10.6f, 0f), rampHigh = L(3.9f, 0f) + Vector3.up * (floor + 0.1f);
            Vector3 mid = (rampLow + rampHigh) * 0.5f;
            Vector3 rampDir = (rampHigh - rampLow).normalized;
            var ramp = Box(kit, "Ramp", mid, new Vector3(1.6f, 0.2f, Vector3.Distance(rampLow, rampHigh)), yaw, kit.DarkWood);
            ramp.transform.rotation = Quaternion.LookRotation(rampDir, Vector3.up);   // its long axis runs up the slope
            // a camouflage net of thin roots draped from the roof to the ground, and trophies on the veranda rail
            var rnd = new System.Random((int)(at.x * 7f + at.z * 13f));
            for (int i = 0; i < 12; i++)
            {
                float a = i / 12f * Mathf.PI * 2f + (float)rnd.NextDouble() * 0.3f;
                Vector3 top = L(Mathf.Cos(a) * 1.2f, Mathf.Sin(a) * 1.2f) + Vector3.up * (floor + 3.5f);
                Vector3 b = L(Mathf.Cos(a) * 4.6f, Mathf.Sin(a) * 4.6f);
                if (Vector3.Dot((b - at).normalized, fwd) > 0.75f) continue;   // keep the ramp clear
                Tube(kit.DarkParts, kit, new[] { top, Vector3.Lerp(top, b, 0.5f) + Vector3.up * 0.6f, b }, 0.06f, 0.03f, 5, 0.3f);
            }
            for (int i = 0; i < 7; i++)
            {
                Vector3 hang = L(3.9f, -1.4f + i * 0.45f) + Vector3.up * (floor + 1.4f);
                Tube(kit.WoodParts, kit, new[] { hang, hang - Vector3.up * (0.5f + 0.15f * (i % 3)) }, 0.012f, 0.012f, 4, 0f);
                Ball(kit.StoneParts, kit, hang - Vector3.up * (0.55f + 0.15f * (i % 3)), new Vector3(0.05f, 0.14f, 0.05f));
            }
            Ball(kit.GlowParts, kit, L(2.0f, 1.9f) + Vector3.up * (floor + 2.4f), new Vector3(0.14f, 0.2f, 0.14f));   // one small dim lantern
        }

        // ---- Năng Thể: the temple ----------------------------------------------------------------------

        static void Temple(Kit kit, Vector3 at, float yaw, Vector3 fwd, Vector3 side, System.Func<float, float, Vector3> L)
        {
            var radii = new[] { 4.4f, 3.2f, 2.1f };
            float y = 0f;
            for (int i = 0; i < radii.Length; i++)
            {
                Drum(kit, "Tier " + i, at + Vector3.up * (y - 0.4f), radii[i], 1.4f, kit.Stone);
                y += 1.0f;
            }
            // steps up the front, tier by tier: four steps of 0.25 m each (a slime can climb them)
            for (int tier = 0; tier < 3; tier++)
                for (int st = 0; st < 4; st++)
                {
                    float baseY = tier * 1.0f, topY = baseY + 0.25f * (st + 1);
                    float x = radii[tier] + 0.35f * (3 - st) - 0.1f;
                    Box(kit, "Step", L(x, 0f) + Vector3.up * ((baseY - 0.3f + topY) * 0.5f), new Vector3(0.36f, topY - baseY + 0.3f, 1.6f), yaw, kit.Stone);
                }
            // six pillars ringed round the base tier, with amber caps
            for (int i = 0; i < 6; i++)
            {
                float a = i / 6f * Mathf.PI * 2f + 0.5f;
                if (Mathf.Abs(Mathf.DeltaAngle(a * Mathf.Rad2Deg, 0f)) < 28f) continue;
                Vector3 p = L(Mathf.Cos(a) * 6.2f, Mathf.Sin(a) * 6.2f);
                Drum(kit, "Pillar", p - Vector3.up * 0.3f, 0.38f, 4.2f, kit.Stone);
                Ball(kit.GlowParts, kit, p + Vector3.up * 4.1f, new Vector3(0.5f, 0.5f, 0.5f));
            }
            // the crystal spire on the top tier, and stones floating round it
            var crystalMat = LakeGround.OpaqueMaterial("Lake_CrystalSpot", new Color(0.35f, 0.85f, 0.9f), 0.8f, false, new Color(0.25f, 1.0f, 1.1f));
            var crystals = new List<CombineInstance>();
            var rnd = new System.Random(77);
            Vector3 top = at + Vector3.up * 3.0f;
            for (int i = 0; i < 7; i++)
            {
                float a = i * Mathf.PI * 2f / 7f;
                float len = i == 0 ? 4.6f : 1.8f + (float)rnd.NextDouble() * 1.2f;
                Vector3 b = i == 0 ? top : top + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 0.8f;
                Quaternion lean = i == 0 ? Quaternion.identity : Quaternion.Euler(Mathf.Sin(a) * 20f, 0f, -Mathf.Cos(a) * 20f);
                crystals.Add(new CombineInstance { mesh = kit.Blob, transform = Matrix4x4.TRS(b + lean * Vector3.up * len * 0.5f, lean, new Vector3(0.45f, len, 0.45f)) });
            }
            LakeGround.AddCombined(kit.Parent, "Crystals", crystals, crystalMat);
            for (int i = 0; i < 9; i++)
            {
                float a = i * Mathf.PI * 2f / 9f + 0.3f;
                Ball(kit.StoneParts, kit, top + new Vector3(Mathf.Cos(a) * 2.8f, 1.2f + 0.9f * Mathf.Sin(i * 1.9f) + 1.2f, Mathf.Sin(a) * 2.8f), Vector3.one * (0.22f + 0.1f * (i % 3)));
            }
            PointLight(kit, top + Vector3.up * 3.5f, new Color(0.45f, 0.95f, 1f), 16f, 1.6f);
        }

        // ---- Quần Thể: the barn ------------------------------------------------------------------------

        static void Barn(Kit kit, Vector3 at, float yaw, Vector3 fwd, Vector3 side, System.Func<float, float, Vector3> L)
        {
            // a long vault (a tunnel of earth and wood) lying along the front axis, open at the front end
            var path = new List<Vector3>();
            for (int i = 0; i <= 8; i++) path.Add(L(-4f + i, 0f));
            var vault = CaveMeshes.ArchTunnel(path, 5.2f, 3.6f, 14);
            var mat = LakeGround.OpaqueMaterial("Lake_BarnWood", new Color(0.4f, 0.3f, 0.2f), 0.1f, true);
            Solid(kit, "Barn Vault", vault, Vector3.zero, 0f, mat);
            Box(kit, "Back Wall", L(-4.3f, 0f) + Vector3.up * 1.6f, new Vector3(0.4f, 3.4f, 5.2f), yaw, kit.Wood);
            // ribs of wood along the vault, a ridge beam with crossed horns at the front
            for (int i = 0; i <= 8; i += 2)
            {
                var rib = new Vector3[9];
                for (int k = 0; k < rib.Length; k++) { float t = k / 8f * Mathf.PI; rib[k] = L(-4f + i, 0f) + side * Mathf.Cos(t) * 2.7f + Vector3.up * (Mathf.Sin(t) * 3.7f - (k == 0 || k == 8 ? 0.5f : 0f)); }
                Tube(kit.WoodParts, kit, rib, 0.16f, 0.14f, 6, 0.08f);
            }
            Tube(kit.WoodParts, kit, new[] { L(-4f, 0f) + Vector3.up * 3.75f, L(4.6f, 0f) + Vector3.up * 3.75f }, 0.14f, 0.14f, 6, 0.05f);
            Tube(kit.WoodParts, kit, new[] { L(4.6f, 0f) + Vector3.up * 3.6f, L(5.5f, 0f) + Vector3.up * 5.0f + side * 0.7f }, 0.09f, 0.04f, 5);
            Tube(kit.WoodParts, kit, new[] { L(4.6f, 0f) + Vector3.up * 3.6f, L(5.5f, 0f) + Vector3.up * 5.0f - side * 0.7f }, 0.09f, 0.04f, 5);
            // the pen in front: a round fence of posts and rails, with three dens inside
            for (int i = 0; i < 18; i++)
            {
                float a = i / 18f * Mathf.PI * 2f, an = (i + 1) / 18f * Mathf.PI * 2f;
                Vector3 b = L(10.5f + Mathf.Cos(a) * 4.8f, Mathf.Sin(a) * 4.8f), nb = L(10.5f + Mathf.Cos(an) * 4.8f, Mathf.Sin(an) * 4.8f);
                if (Mathf.Abs(Mathf.DeltaAngle(a * Mathf.Rad2Deg, 180f)) < 22f) continue;   // the pen's gate faces the barn
                Tube(kit.WoodParts, kit, new[] { b - Vector3.up * 0.2f, b + Vector3.up * 1.2f }, 0.1f, 0.08f, 5);
                Tube(kit.WoodParts, kit, new[] { b + Vector3.up * 0.5f, nb + Vector3.up * 0.5f }, 0.045f, 0.045f, 4, 0f);
                Tube(kit.WoodParts, kit, new[] { b + Vector3.up * 0.95f, nb + Vector3.up * 0.95f }, 0.045f, 0.045f, 4, 0f);
            }
            for (int i = 0; i < 3; i++)
            {
                float a = i * 2.1f + 0.4f;
                Vector3 m = L(10.5f + Mathf.Cos(a) * 2.2f, Mathf.Sin(a) * 2.2f);
                Ball(kit.MossParts, kit, m, new Vector3(1.5f, 0.6f, 1.3f), i * 40f);
                Ball(kit.GlowParts, kit, m + Vector3.up * 0.15f - fwd * 0.6f, new Vector3(0.2f, 0.16f, 0.2f));
            }
            Vector3 bell = L(6.5f, -4.2f);
            Tube(kit.WoodParts, kit, new[] { bell - Vector3.up * 0.2f, bell + Vector3.up * 2.6f, bell + Vector3.up * 3.1f + side * 0.7f }, 0.14f, 0.08f, 6);
            Ball(kit.StoneParts, kit, bell + Vector3.up * 2.75f + side * 0.7f, new Vector3(0.4f, 0.5f, 0.4f));
            Tube(kit.WoodParts, kit, new[] { L(6.2f, 4.0f) + Vector3.up * 0.3f, L(7.6f, 4.0f) + Vector3.up * 0.3f }, 0.32f, 0.3f, 8, 0.05f);
            for (int i = 0; i < 4; i++) Ball(kit.MossParts, kit, L(-2.5f + i * 1.6f, 1.6f) + Vector3.up * 0.3f, new Vector3(1.2f, 0.5f, 0.9f), i * 30f);   // hay inside
        }

        // ---- Dưỡng Thể: the petal pavilion ------------------------------------------------------------

        static void Pavilion(Kit kit, Vector3 at, float yaw, Vector3 fwd, Vector3 side, System.Func<float, float, Vector3> L)
        {
            Drum(kit, "Floor", at - Vector3.up * 0.35f, 4.4f, 0.65f, kit.Stone);
            var petals = LakeGround.OpaqueMaterial("Lake_PetalSpot", new Color(0.78f, 0.95f, 0.78f), 0.3f, true, new Color(0.08f, 0.25f, 0.12f));
            var petalParts = new List<CombineInstance>();
            // six slender pillars and a roof of eight great petals meeting in a bud
            for (int i = 0; i < 6; i++)
            {
                float a = i / 6f * Mathf.PI * 2f;
                Vector3 b = at + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 3.9f;
                Tube(kit.BarkParts, kit, new[] { b - Vector3.up * 0.2f, b + Vector3.up * 2.0f + new Vector3(-Mathf.Cos(a), 0f, -Mathf.Sin(a)) * 0.2f, b + Vector3.up * 4.0f + new Vector3(-Mathf.Cos(a), 0f, -Mathf.Sin(a)) * 0.9f }, 0.24f, 0.16f, 8, 0.2f);
            }
            for (int i = 0; i < 8; i++)
            {
                float a = i / 8f * Mathf.PI * 2f;
                Vector3 dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                Quaternion tilt = Quaternion.LookRotation(dir, Vector3.up) * Quaternion.Euler(-28f, 0f, 0f);
                petalParts.Add(new CombineInstance { mesh = kit.Blob, transform = Matrix4x4.TRS(at + Vector3.up * 4.9f + dir * 2.1f, Quaternion.LookRotation(dir, Vector3.up) * Quaternion.Euler(-24f, 0f, 0f), new Vector3(1.9f, 0.14f, 4.4f)) });
            }
            petalParts.Add(new CombineInstance { mesh = kit.Blob, transform = Matrix4x4.TRS(at + Vector3.up * 5.9f, Quaternion.identity, new Vector3(1.4f, 1.8f, 1.4f)) });
            LakeGround.AddCombined(kit.Parent, "Petals", petalParts, petals);
            // the healing spring in the middle
            var water = LakeWater.TranslucentMaterial("Lake_HealingWater", null, new Color(0.45f, 1f, 0.85f, 0.6f), new Color(0.05f, 0.25f, 0.2f), 0.9f, (int)RenderQueue.Transparent + 2);
            var disc = new GameObject("Healing Spring");
            disc.transform.SetParent(kit.Parent, false);
            disc.transform.position = at + Vector3.up * 0.42f;
            disc.AddComponent<MeshFilter>().sharedMesh = WaterMeshes.Disc(1.5f, 0.4f);
            disc.AddComponent<MeshRenderer>().sharedMaterial = water;
            for (int i = 0; i < 10; i++)
            {
                float a = i * Mathf.PI * 2f / 10f;
                Ball(kit.StoneParts, kit, at + new Vector3(Mathf.Cos(a) * 1.65f, 0.4f, Mathf.Sin(a) * 1.65f), new Vector3(0.42f, 0.3f, 0.36f), i * 36f);
            }
            // glowing herb beds all round the outside, and lanterns hanging from the petals
            var herbs = LakeGround.OpaqueMaterial("Lake_HerbSpot", new Color(0.45f, 0.95f, 0.65f), 0.4f, false, new Color(0.18f, 0.7f, 0.4f));
            var stalks = new List<CombineInstance>(); var capsList = new List<CombineInstance>();
            var rnd = new System.Random(88);
            for (int i = 0; i < 24; i++)
            {
                float a = (float)rnd.NextDouble() * Mathf.PI * 2f, d = 5.6f + (float)rnd.NextDouble() * 3.2f;
                Vector3 p = at + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * d; p.y = LakeGround.GroundY(p.x, p.z);
                FloraMeshes.Mushroom(21000 + i, 0.4f + (float)rnd.NextDouble() * 0.6f, out var st, out var cp, out _);
                stalks.Add(new CombineInstance { mesh = st, transform = Matrix4x4.Translate(p) });
                capsList.Add(new CombineInstance { mesh = cp, transform = Matrix4x4.Translate(p) });
            }
            LakeGround.AddCombined(kit.Parent, "Herb Stalks", stalks, LakeGround.OpaqueMaterial("Lake_MushroomStalk", new Color(0.93f, 0.89f, 0.78f), 0.3f, false));
            LakeGround.AddCombined(kit.Parent, "Herb Caps", capsList, herbs);
            for (int i = 0; i < 8; i++)
            {
                float a = i / 8f * Mathf.PI * 2f + 0.2f;
                Vector3 top = at + Vector3.up * 4.6f + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 3.2f;
                Tube(kit.WoodParts, kit, new[] { top, top - Vector3.up * 1.0f }, 0.012f, 0.012f, 4, 0f);
                Ball(kit.GlowParts, kit, top - Vector3.up * 1.1f, new Vector3(0.2f, 0.28f, 0.2f));
            }
            PointLight(kit, at + Vector3.up * 3f, new Color(0.7f, 1f, 0.8f), 12f, 1.2f);
        }

        // ---- Xạ Thể: the tower ---------------------------------------------------------------------------

        static void Tower(Kit kit, Vector3 at, float yaw, Vector3 fwd, Vector3 side, System.Func<float, float, Vector3> L)
        {
            const float platform = 8.0f;
            // a great trunk, wide at the foot, with a spiral stair winding round it
            var trunk = new Vector3[8];
            for (int k = 0; k < trunk.Length; k++) trunk[k] = at + Vector3.up * Mathf.Lerp(-0.5f, platform, k / 7f) + new Vector3(Mathf.Sin(k * 0.8f) * 0.12f, 0f, Mathf.Cos(k * 0.8f) * 0.12f);
            Tube(kit.BarkParts, kit, trunk, 1.7f, 1.0f, 12, 0.2f);
            var col = new GameObject("Trunk").AddComponent<CapsuleCollider>();
            col.transform.SetParent(kit.Parent, false);
            col.transform.position = at + Vector3.up * platform * 0.5f; col.radius = 1.4f; col.height = platform + 1f;
            const int steps = 30;
            for (int i = 0; i < steps; i++)
            {
                float t = i / (float)steps, a = t * Mathf.PI * 2f * 2.2f;
                Vector3 p = at + new Vector3(Mathf.Cos(a) * 2.5f, 0.3f + t * (platform - 0.3f), Mathf.Sin(a) * 2.5f);
                p.y += Mathf.Max(0f, LakeGround.GroundY(at.x, at.z) - at.y);
                var mesh = CaveMeshes.Disc(0.95f, 0.22f, 10);
                var go = new GameObject("Stair Step");
                go.transform.SetParent(kit.Parent, false);
                go.transform.position = p - Vector3.up * 0.2f;
                go.transform.rotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                go.AddComponent<MeshRenderer>().sharedMaterial = kit.Wood;
                go.AddComponent<MeshCollider>().sharedMesh = mesh;
                go.isStatic = true;
            }
            var plat = CaveMeshes.Disc(3.4f, 0.35f, 28);
            var pg = new GameObject("Lookout Platform");
            pg.transform.SetParent(kit.Parent, false);
            pg.transform.position = at + Vector3.up * (platform - 0.1f);
            pg.AddComponent<MeshFilter>().sharedMesh = plat;
            pg.AddComponent<MeshRenderer>().sharedMaterial = kit.Wood;
            pg.AddComponent<MeshCollider>().sharedMesh = plat;
            for (int i = 0; i < 16; i++)
            {
                float a = i / 16f * Mathf.PI * 2f;
                Vector3 b = at + Vector3.up * (platform + 0.25f) + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 3.2f;
                Tube(kit.WoodParts, kit, new[] { b, b + Vector3.up * 1.1f }, 0.06f, 0.05f, 4, 0f);
            }
            for (int i = 0; i < 4; i++)   // four roof posts and a mossy conical roof, a lantern under it
            {
                float a = i * Mathf.PI * 0.5f + 0.4f;
                Vector3 b = at + Vector3.up * (platform + 0.25f) + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 2.7f;
                Tube(kit.WoodParts, kit, new[] { b, b + Vector3.up * 3.0f }, 0.1f, 0.08f, 5);
            }
            Ball(kit.MossParts, kit, at + Vector3.up * (platform + 3.7f), new Vector3(6.4f, 1.3f, 6.4f));
            Tube(kit.WoodParts, kit, new[] { at + Vector3.up * (platform + 4.2f), at + Vector3.up * (platform + 6.2f) }, 0.07f, 0.05f, 4, 0f);
            kit.ClothParts.Add(new CombineInstance { mesh = Quad(1.4f, 1.0f), transform = Matrix4x4.TRS(at + Vector3.up * (platform + 5.9f) + side * 0.7f, Quaternion.Euler(0f, yaw, 0f), Vector3.one) });
            Ball(kit.GlowParts, kit, at + Vector3.up * (platform + 2.6f), new Vector3(0.3f, 0.4f, 0.3f));
            // the archery range in front: a row of targets 10 to 20 m out, and a rack of bows
            for (int i = 0; i < 4; i++)
            {
                Vector3 p = L(10f + i * 3.5f, -4f + i * 2.6f);
                Tube(kit.WoodParts, kit, new[] { p - Vector3.up * 0.1f, p + Vector3.up * 1.1f }, 0.05f, 0.05f, 4, 0f);
                Ball(kit.StoneParts, kit, p + Vector3.up * 1.15f, new Vector3(0.1f, 0.85f, 0.85f), yaw + 180f);
                Ball(kit.GlowParts, kit, p + Vector3.up * 1.15f - fwd * 0.07f, new Vector3(0.05f, 0.25f, 0.25f), yaw + 180f);
            }
            Vector3 rack = L(5f, 3.8f);
            Tube(kit.WoodParts, kit, new[] { rack, rack + Vector3.up * 1.3f }, 0.08f, 0.07f, 5);
            Tube(kit.WoodParts, kit, new[] { rack + side * 1.8f, rack + side * 1.8f + Vector3.up * 1.3f }, 0.08f, 0.07f, 5);
            for (int i = 0; i < 4; i++)
            {
                var bow = new Vector3[5];
                for (int k = 0; k < 5; k++) { float t = k / 4f; bow[k] = rack + side * (0.25f + i * 0.4f) + Vector3.up * (0.1f + t * 1.1f) + fwd * Mathf.Sin(t * Mathf.PI) * 0.2f; }
                Tube(kit.WoodParts, kit, bow, 0.025f, 0.025f, 4, 0f);
            }
        }

        // ---- Cấu Thể: the glasshouse --------------------------------------------------------------------

        static void Glasshouse(Kit kit, Vector3 at, float yaw, Vector3 fwd, Vector3 side, System.Func<float, float, Vector3> L)
        {
            const float r = 3.9f, H = 3.9f;
            var glass = LakeWater.TranslucentMaterial("Lake_GlasshouseGel", null, new Color(0.7f, 0.95f, 0.9f, 0.32f), new Color(0.02f, 0.08f, 0.07f), 0.95f, (int)RenderQueue.Transparent + 3);
            // a translucent dome with a doorway at the front, on a ring of stones, ribbed with roots
            var shell = CaveMeshes.Shell(r, r * 0.9f, H, 40, 14, 31000, 0f);
            WaterMeshes.RemoveTriangles(shell, v => v.x > 0f && Mathf.Abs(v.z) < 0.75f && v.y < 1.9f);
            var go = Solid(kit, "Glass Dome", shell, at - Vector3.up * 0.15f, yaw, glass);
            go.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            for (int i = 0; i < 28; i++)
            {
                float a = i / 28f * Mathf.PI * 2f;
                if (Mathf.Abs(Mathf.DeltaAngle(a * Mathf.Rad2Deg, 0f)) < 14f) continue;
                Ball(kit.StoneParts, kit, L(Mathf.Cos(a) * (r + 0.1f), Mathf.Sin(a) * r * 0.9f) + Vector3.up * 0.2f, new Vector3(0.55f, 0.4f, 0.5f), i * 25f);
            }
            for (int m = 0; m < 8; m++)
            {
                float a0 = m / 8f * Mathf.PI * 2f;
                if (Mathf.Abs(Mathf.DeltaAngle(a0 * Mathf.Rad2Deg, 0f)) < 24f) continue;
                var rib = new Vector3[8];
                for (int k = 0; k < rib.Length; k++)
                {
                    float t = k / 7f * Mathf.PI * 0.5f;
                    rib[k] = L(Mathf.Cos(a0) * r * Mathf.Cos(t) * 1.01f, Mathf.Sin(a0) * r * 0.9f * Mathf.Cos(t) * 1.01f) + Vector3.up * (H * Mathf.Sin(t) * 1.01f - 0.1f);
                }
                Tube(kit.BarkParts, kit, rib, 0.16f, 0.1f, 6, 0.1f);
            }
            // inside: gel vats on stone plinths with glowing cores, root tubes up into the dome, a bench of jars
            var gel = LakeWater.TranslucentMaterial("Lake_GelDome", null, new Color(0.72f, 0.86f, 0.50f, 0.5f), Color.black, 0.92f, (int)RenderQueue.Transparent + 1);
            var cores = LakeGround.OpaqueMaterial("Lake_GelCoreSpot", new Color(0.95f, 0.62f, 0.18f), 0.4f, false, new Color(0.9f, 0.5f, 0.12f));
            var vats = new List<CombineInstance>(); var coreParts = new List<CombineInstance>();
            for (int i = 0; i < 5; i++)
            {
                float a = i / 5f * Mathf.PI * 2f + 1.2f;
                Vector3 p = L(Mathf.Cos(a) * 1.8f - 0.6f, Mathf.Sin(a) * 1.7f);
                float rad = 0.6f + 0.12f * (i % 3);
                Tube(kit.StoneParts, kit, new[] { p - Vector3.up * 0.2f, p + Vector3.up * 0.55f }, rad * 0.9f, rad * 0.85f, 10, 0f);
                var profile = new List<Vector2> { new Vector2(0f, rad * 1.6f), new Vector2(0.55f * rad, rad * 1.5f), new Vector2(0.95f * rad, rad * 0.9f), new Vector2(rad, rad * 0.25f), new Vector2(rad * 0.95f, 0f) };
                vats.Add(new CombineInstance { mesh = FloraMeshes.Lathe(profile, 14, rad * 0.5f, 23000 + i, 0.04f), transform = Matrix4x4.Translate(p + Vector3.up * 0.55f) });
                coreParts.Add(new CombineInstance { mesh = kit.Blob, transform = Matrix4x4.TRS(p + Vector3.up * (0.55f + rad * 0.7f), Quaternion.identity, Vector3.one * rad * 0.7f) });
                Tube(kit.BarkParts, kit, new[] { p + Vector3.up * (0.55f + rad * 1.5f), p + Vector3.up * 2.4f, at + Vector3.up * (H - 0.3f) }, 0.06f, 0.04f, 5, 0.3f);
            }
            LakeGround.AddCombined(kit.Parent, "Gel Vats", vats, gel);
            LakeGround.AddCombined(kit.Parent, "Vat Cores", coreParts, cores);
            // outside: planters of glowing mushrooms along the front, a rain barrel
            for (int i = 0; i < 6; i++)
            {
                Vector3 p = L(r + 1.4f + (i % 2) * 0.6f, -2.5f + i * 1.0f);
                FloraMeshes.Mushroom(24000 + i, 0.4f + 0.1f * (i % 3), out var st, out var cp, out _);
                kit.WoodParts.Add(new CombineInstance { mesh = st, transform = Matrix4x4.Translate(p) });
                kit.GlowParts.Add(new CombineInstance { mesh = cp, transform = Matrix4x4.Translate(p) });
            }
            Drum(kit, "Barrel", L(2f, r + 1.5f) - Vector3.up * 0.1f, 0.5f, 1.0f, kit.Wood);
            PointLight(kit, at + Vector3.up * 2f, new Color(0.7f, 1f, 0.85f), 12f, 1.3f);
        }

        // ---- Dã Thể: the hide tent and its camp -----------------------------------------------------------

        static void Tent(Kit kit, Vector3 at, float yaw, Vector3 fwd, Vector3 side, System.Func<float, float, Vector3> L)
        {
            var canvas = LakeGround.OpaqueMaterial("Lake_TentCanvas", new Color(0.82f, 0.72f, 0.5f), 0.05f, true);
            // a cone of stretched hide with a door flap, poles crossing out of the top
            var profile = new List<Vector2> { new Vector2(3.4f, -0.1f), new Vector2(3.3f, 0.4f), new Vector2(2.6f, 1.4f), new Vector2(1.6f, 2.6f), new Vector2(0.7f, 3.5f), new Vector2(0.1f, 4.2f) };
            var cone = FloraMeshes.Lathe(profile, 18, 1.5f, 32000, 0.03f);
            WaterMeshes.RemoveTriangles(cone, v => v.x > 0f && Mathf.Abs(v.z) < 0.7f && v.y < 2.0f);
            Solid(kit, "Tent", cone, at, yaw, canvas);
            Tube(kit.WoodParts, kit, new[] { at + Vector3.up * 2.4f + side * -1.0f, at + Vector3.up * 5.4f + side * 0.3f }, 0.08f, 0.05f, 5);
            Tube(kit.WoodParts, kit, new[] { at + Vector3.up * 2.4f + side * 1.0f, at + Vector3.up * 5.4f + side * -0.3f }, 0.08f, 0.05f, 5);
            for (int i = 0; i < 8; i++)   // stakes and ropes round the skirt
            {
                float a = i / 8f * Mathf.PI * 2f;
                if (Mathf.Abs(Mathf.DeltaAngle(a * Mathf.Rad2Deg, 0f)) < 30f) continue;
                Vector3 s = L(Mathf.Cos(a) * 4.6f, Mathf.Sin(a) * 4.6f);
                Tube(kit.WoodParts, kit, new[] { s - Vector3.up * 0.1f, s + Vector3.up * 0.5f }, 0.05f, 0.04f, 4, 0f);
                Tube(kit.WoodParts, kit, new[] { s + Vector3.up * 0.45f, L(Mathf.Cos(a) * 3.0f, Mathf.Sin(a) * 3.0f) + Vector3.up * 1.4f }, 0.015f, 0.015f, 4, 0f);
            }
            // the camp in front: a fire pit, a lean-to, a map post, a pile of gear and a hammock
            Vector3 fire = L(7.5f, -0.5f);
            for (int i = 0; i < 9; i++)
            {
                float a = i * Mathf.PI * 2f / 9f;
                Ball(kit.StoneParts, kit, fire + new Vector3(Mathf.Cos(a) * 0.8f, 0.12f, Mathf.Sin(a) * 0.8f), new Vector3(0.36f, 0.26f, 0.3f), i * 40f);
            }
            Ball(kit.GlowParts, kit, fire + Vector3.up * 0.2f, new Vector3(0.7f, 0.3f, 0.7f));
            for (int i = 0; i < 3; i++) Tube(kit.WoodParts, kit, new[] { fire + new Vector3(Mathf.Cos(i * 2.1f), 0.15f, Mathf.Sin(i * 2.1f)) * 0.7f, fire + Vector3.up * 0.5f }, 0.07f, 0.05f, 4, 0f);
            PointLight(kit, fire + Vector3.up * 1.0f, new Color(1f, 0.6f, 0.25f), 12f, 1.4f);
            Vector3 a0 = L(5.5f, -4.5f), a1 = L(5.5f, -7.5f);
            Tube(kit.WoodParts, kit, new[] { a0 - Vector3.up * 0.2f, a0 + Vector3.up * 2.0f }, 0.1f, 0.09f, 5);
            Tube(kit.WoodParts, kit, new[] { a1 - Vector3.up * 0.2f, a1 + Vector3.up * 2.0f }, 0.1f, 0.09f, 5);
            for (int i = 0; i < 5; i++)
            {
                Vector3 e = Vector3.Lerp(a0, a1, i / 4f);
                Tube(kit.BarkParts, kit, new[] { e + Vector3.up * 2.0f, e + fwd * 2.4f + Vector3.up * 0.3f }, 0.08f, 0.06f, 5, 0.1f);
            }
            Ball(kit.MossParts, kit, L(6.8f, -6.0f) + Vector3.up * 1.2f, new Vector3(2.4f, 0.25f, 3.6f), yaw);
            Vector3 map = L(5.0f, 4.2f);
            Tube(kit.WoodParts, kit, new[] { map - Vector3.up * 0.2f, map + Vector3.up * 2.2f }, 0.09f, 0.08f, 5);
            Ball(kit.StoneParts, kit, map + Vector3.up * 1.8f - fwd * 0.12f, new Vector3(0.06f, 0.7f, 0.9f), yaw + 180f);
            Vector3 pile = L(2.5f, 6.0f);
            for (int i = 0; i < 5; i++) Ball(kit.WoodParts, kit, pile + new Vector3(Mathf.Sin(i * 2.2f) * 0.5f, 0.2f + 0.18f * (i % 3), Mathf.Cos(i * 1.7f) * 0.5f), new Vector3(0.5f, 0.3f, 0.4f), i * 55f);
            Vector3 h0 = L(9.5f, 3.5f), h1 = L(9.5f, 7.0f);
            Tube(kit.WoodParts, kit, new[] { h0 - Vector3.up * 0.2f, h0 + Vector3.up * 1.8f }, 0.11f, 0.09f, 5);
            Tube(kit.WoodParts, kit, new[] { h1 - Vector3.up * 0.2f, h1 + Vector3.up * 1.8f }, 0.11f, 0.09f, 5);
            var sling = new Vector3[7];
            for (int i = 0; i < sling.Length; i++) { float t = i / 6f; sling[i] = Vector3.Lerp(h0 + Vector3.up * 1.6f, h1 + Vector3.up * 1.6f, t) - Vector3.up * 0.7f * Mathf.Sin(t * Mathf.PI); }
            Tube(kit.MossParts, kit, sling, 0.2f, 0.2f, 6, 0.05f);
        }
    }
}
