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
    /// Evolve → Dress Lake: Class Houses. One special building for each of the eight treefolk classes, on the sites chosen
    /// by CityLayout.ClassHouses (scattered round the village, never in a ring), each in the style of its class:
    ///  Đấu Thể (melee, defence): a squat stone-grey fort with a palisade of stakes, training dummies and a weapon rack.
    ///  Liệp Thể (assassin, hunter): a small dark hut half hidden under a camouflage of roots, trophies hanging by the door.
    ///  Năng Thể (energy, elements): a hut crowned by a spire of glowing crystals, with stones floating round it.
    ///  Quần Thể (summoner): a hut with a creature pen, a bell post and a feeding trough.
    ///  Dưỡng Thể (healer): a hut in a glowing herb garden with a little healing spring and hanging vines.
    ///  Xạ Thể (archer): a tower of roots on stilts with a lookout platform, and a row of archery targets.
    ///  Cấu Thể (researcher): a hut full of gel vats and jars joined by root tubes.
    ///  Dã Thể (explorer, survivor): a camp: lean-to, fire pit, gear pile, a map post and a hammock.
    /// Run again to rebuild.
    /// </summary>
    public static class LakeClassHouses
    {
        const string DressedName = "Treefolk Class Houses (Dressed)";
        const float HutSink = 0.3f;

        class Kit
        {
            public Material Earth, Bark, Stone, Wood, Amber, Moss;
            public Mesh Blob;
            public readonly List<CombineInstance> BarkParts = new List<CombineInstance>();
            public readonly List<CombineInstance> WoodParts = new List<CombineInstance>();
            public readonly List<CombineInstance> StoneParts = new List<CombineInstance>();
            public readonly List<CombineInstance> GlowParts = new List<CombineInstance>();
            public readonly List<CombineInstance> MossParts = new List<CombineInstance>();
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
                Earth = LakeGround.OpaqueMaterial("Lake_HutEarth", new Color(0.33f, 0.24f, 0.16f), 0.05f, true),
                Bark = LakeGround.OpaqueMaterial("Lake_CanopyBark", new Color(0.24f, 0.17f, 0.11f), 0.12f, false),
                Stone = LakeGround.OpaqueMaterial("Lake_Limestone", new Color(0.74f, 0.72f, 0.64f), 0.2f, false),
                Wood = LakeGround.OpaqueMaterial("Lake_PetrifiedWood", new Color(0.46f, 0.36f, 0.26f), 0.35f, false),
                Amber = LakeGround.OpaqueMaterial("Lake_AmberLampSpot", new Color(0.95f, 0.62f, 0.18f), 0.6f, false, new Color(1.2f, 0.62f, 0.14f)),
                Moss = LakeGround.MossMaterial(),
                Blob = FloraMeshes.Blob(),
            };
            var houses = CityLayout.ClassHouses();
            foreach (var h in houses)
            {
                var parent = new GameObject(TreefolkClasses.Name(h.Class)).transform;
                parent.SetParent(root, false);
                Build(parent, kit, h);
            }
            LakeGround.AddCombined(root, "Class Bark", kit.BarkParts, kit.Bark);
            LakeGround.AddCombined(root, "Class Wood", kit.WoodParts, kit.Wood);
            LakeGround.AddCombined(root, "Class Stone", kit.StoneParts, kit.Stone);
            LakeGround.AddCombined(root, "Class Glow", kit.GlowParts, kit.Amber);
            LakeGround.AddCombined(root, "Class Moss", kit.MossParts, kit.Moss);

            EditorSceneManager.MarkAllScenesDirty();
            AssetDatabase.SaveAssets();
            Debug.Log($"Đã dựng {houses.Length} nhà của các class người cây. Bấm Ctrl+S để lưu scene.");
        }

        // ---------------------------------------------------------------- shared pieces

        static Vector3 OnGround(float x, float z) => new Vector3(x, LakeGround.GroundY(x, z), z);

        static void Tube(List<CombineInstance> list, Kit kit, Vector3[] path, float r0, float r1, int sides = 7, float knobs = 0.18f) =>
            list.Add(new CombineInstance { mesh = RootMeshes.RootTube(path, r0, r1, ++kit.Seed, knobs, sides), transform = Matrix4x4.identity });

        static void Ball(List<CombineInstance> list, Kit kit, Vector3 at, Vector3 size, float yaw = 0f) =>
            list.Add(new CombineInstance { mesh = kit.Blob, transform = Matrix4x4.TRS(at, Quaternion.Euler(0f, yaw, 0f), size) });

        static Material Tinted(string name, Color tint)
        {
            var mat = LakeGround.OpaqueMaterial(name, tint, 0.08f, true);
            mat.SetTexture("_BaseMap", LakeTreefolkCity.ShingleMaterial().GetTexture("_BaseMap"));
            EditorUtility.SetDirty(mat);
            return mat;
        }

        /// <summary>
        /// A hut like the village huts (smooth dome, an arched doorway, bark plates over it, a frame hugging the doorway),
        /// bigger and tinted for its class. Its local +x is the front. Returns the hut's object; <paramref name="surface"/>
        /// gives world points on the dome.
        /// </summary>
        static GameObject Hut(Transform parent, Kit kit, Vector2 pos, float yaw, float r, float tall, Color tint, out System.Func<float, float, float, Vector3> surface)
        {
            float ground = LakeGround.GroundY(pos.x, pos.y);
            float rz = r * 0.92f, H = r * tall + HutSink;
            var shell = CaveMeshes.Shell(r, rz, H, 56, 18, ++kit.Seed, 0f);
            WaterMeshes.RemoveTriangles(shell, v =>
            {
                if (v.x <= 0f) return false;
                float y = v.y - HutSink;
                if (y < -0.25f) return false;
                if (y <= 0.9f) return Mathf.Abs(v.z) < 0.58f;
                float e = (y - 0.9f) / 0.55f, f = v.z / 0.58f;
                return e * e + f * f < 1f;
            });
            var go = new GameObject("Hut");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(pos.x, ground - HutSink, pos.y);
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            go.AddComponent<MeshFilter>().sharedMesh = shell;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = kit.Earth;
            mr.shadowCastingMode = ShadowCastingMode.TwoSided;
            go.AddComponent<MeshCollider>().sharedMesh = shell;
            go.isStatic = true;

            var plates = CaveMeshes.Shingles(r, rz, H, 0.4f, 0.34f, 0.09f, ++kit.Seed, c => c.y < HutSink - 0.05f || (c.x > 0f && Mathf.Abs(c.z) < 0.88f && c.y - HutSink < 2.0f));
            var plateGo = new GameObject("Bark Plates");
            plateGo.transform.SetParent(go.transform, false);
            plateGo.AddComponent<MeshFilter>().sharedMesh = plates;
            var pr = plateGo.AddComponent<MeshRenderer>();
            pr.sharedMaterial = Tinted("Lake_BarkPlates_" + ColorUtility.ToHtmlStringRGB(tint), tint);
            pr.shadowCastingMode = ShadowCastingMode.TwoSided;
            plateGo.isStatic = true;

            var t = go.transform;
            float hh = H;
            surface = (y, z, push) =>
            {
                float q = 1f - (y / hh) * (y / hh) - (z / rz) * (z / rz);
                return t.TransformPoint(new Vector3(r * Mathf.Sqrt(Mathf.Max(0.001f, q)) + push, y, z));
            };
            var surf = surface;
            // the frame round the doorway, lying on the dome, feet in the ground
            var frame = new List<Vector3> { surf(0.05f, -0.64f, 0.02f) - Vector3.up * 0.25f, surf(0.3f, -0.64f, 0.02f) };
            for (int k = 0; k <= 8; k++)
            {
                float a = k / 8f * Mathf.PI;
                frame.Add(surf(HutSink + 0.9f + 0.62f * Mathf.Sin(a), -0.64f * Mathf.Cos(a), 0.03f));
            }
            frame.Add(surf(0.3f, 0.64f, 0.02f));
            frame.Add(surf(0.05f, 0.64f, 0.02f) - Vector3.up * 0.25f);
            Tube(kit.BarkParts, kit, frame.ToArray(), 0.15f, 0.15f, 7, 0.08f);
            // roots hugging the dome, never across the door
            for (int k = 0; k < 6; k++)
            {
                float a0 = (0.5f + k) * Mathf.PI * 2f / 6f;
                if (Mathf.Abs(Mathf.DeltaAngle(a0 * Mathf.Rad2Deg, 0f)) < 45f) continue;
                var path = new Vector3[9];
                for (int s = 0; s < path.Length; s++)
                {
                    float tt = s / (float)(path.Length - 1), y = Mathf.Lerp(HutSink - 0.35f, H * 0.93f, tt);
                    float q = Mathf.Sqrt(Mathf.Max(0.001f, 1f - (y / H) * (y / H))), aa = a0 + 0.1f * Mathf.Sin(tt * 5f + k);
                    path[s] = t.TransformPoint(new Vector3(Mathf.Cos(aa) * r * q * 0.985f, y, Mathf.Sin(aa) * rz * q * 0.985f));
                }
                Tube(kit.BarkParts, kit, path, 0.28f, 0.12f, 7, 0.2f);
            }
            Ball(kit.GlowParts, kit, surf(HutSink + 1.0f, 0.95f, 0.18f), new Vector3(0.22f, 0.3f, 0.22f));
            return go;
        }

        // ---------------------------------------------------------------- one house per class

        static void Build(Transform parent, Kit kit, CityLayout.ClassHouse h)
        {
            Vector2 pos = h.Position;
            float yaw = h.Yaw;
            var sign = parent.gameObject.AddComponent<ClassHouseSign>();
            sign.Class = h.Class;
            Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
            Vector3 fwd = rot * Vector3.right, side = rot * Vector3.forward;   // front (+x local) and left/right (+z local)
            Vector3 at = OnGround(pos.x, pos.y);
            sign.transform.position = at;
            Vector3 Local(float x, float z) { var p = at + fwd * x + side * z; return OnGround(p.x, p.z); }
            System.Func<float, float, float, Vector3> surf;

            switch (h.Class)
            {
                case TreefolkClass.DauThe:   // the fort
                {
                    Hut(parent, kit, pos, yaw, 3.6f, 0.95f, new Color(0.72f, 0.72f, 0.76f), out surf);
                    // a palisade of leaning stakes in an arc round the front, with a gap for the door
                    for (int i = 0; i < 15; i++)
                    {
                        float a = Mathf.Lerp(-100f, 100f, i / 14f) * Mathf.Deg2Rad;
                        if (Mathf.Abs(a) < 0.3f) continue;
                        Vector3 b = Local(Mathf.Cos(a) * 5.6f, Mathf.Sin(a) * 5.6f);
                        Vector3 outward = (fwd * Mathf.Cos(a) + side * Mathf.Sin(a));
                        Tube(kit.WoodParts, kit, new[] { b - Vector3.up * 0.2f, b + outward * 0.15f + Vector3.up * 1.4f, b + outward * 0.35f + Vector3.up * 2.4f }, 0.2f, 0.05f, 6, 0.1f);
                    }
                    // training dummies: post, crossbar, straw head
                    for (int i = 0; i < 3; i++)
                    {
                        Vector3 p = Local(8.5f + i * 0.6f, -4f + i * 3.4f);
                        Tube(kit.WoodParts, kit, new[] { p - Vector3.up * 0.2f, p + Vector3.up * 1.0f, p + Vector3.up * 1.7f }, 0.13f, 0.1f, 6);
                        Tube(kit.WoodParts, kit, new[] { p + Vector3.up * 1.25f - side * 0.55f, p + Vector3.up * 1.25f + side * 0.55f }, 0.07f, 0.07f, 5);
                        Ball(kit.MossParts, kit, p + Vector3.up * 1.85f, new Vector3(0.3f, 0.3f, 0.3f));
                    }
                    // a weapon rack with six spears
                    Vector3 rack = Local(6.5f, 4.4f);
                    Tube(kit.WoodParts, kit, new[] { rack, rack + Vector3.up * 1.5f }, 0.1f, 0.09f, 6);
                    Tube(kit.WoodParts, kit, new[] { rack + side * 1.6f, rack + side * 1.6f + Vector3.up * 1.5f }, 0.1f, 0.09f, 6);
                    for (int i = 0; i < 6; i++)
                    {
                        Vector3 b = rack + side * (0.15f + i * 0.26f);
                        Tube(kit.WoodParts, kit, new[] { b + Vector3.up * 0.1f, b + Vector3.up * 1.0f + fwd * 0.15f, b + Vector3.up * 2.1f + fwd * 0.25f }, 0.04f, 0.02f, 4, 0f);
                    }
                    // a shield emblem above the door: a flat stone disc
                    Ball(kit.StoneParts, kit, surf(HutSink + 2.3f, 0f, 0.15f), new Vector3(0.18f, 0.9f, 0.9f), yaw);
                    break;
                }
                case TreefolkClass.LiepThe:   // the hidden hut
                {
                    Hut(parent, kit, pos, yaw, 2.3f, 1.15f, new Color(0.45f, 0.45f, 0.5f), out surf);
                    // a net of thin roots draped over the roof and down to the ground
                    var rnd = new System.Random((int)(pos.x * 7f + pos.y * 13f));
                    for (int i = 0; i < 9; i++)
                    {
                        float a = (i / 9f) * Mathf.PI * 2f + (float)rnd.NextDouble() * 0.3f;
                        if (Mathf.Abs(Mathf.DeltaAngle(a * Mathf.Rad2Deg, 0f)) < 40f) continue;
                        Vector3 top = surf(2.6f, 0f, 0.1f) + Vector3.up * 0.1f;
                        Vector3 b = Local(Mathf.Cos(a) * 3.4f, Mathf.Sin(a) * 3.1f);
                        Tube(kit.BarkParts, kit, new[] { top, Vector3.Lerp(top, b, 0.5f) + Vector3.up * 0.8f, b }, 0.07f, 0.04f, 5, 0.3f);
                    }
                    // trophies hanging from a branch by the door: teeth and claws
                    Vector3 post = Local(3.2f, -1.4f);
                    Tube(kit.BarkParts, kit, new[] { post - Vector3.up * 0.2f, post + Vector3.up * 1.6f, post + Vector3.up * 2.1f + side * 1.4f }, 0.1f, 0.07f, 6);
                    for (int i = 0; i < 6; i++)
                    {
                        Vector3 hang = post + Vector3.up * 2.05f + side * (0.3f + i * 0.2f);
                        Tube(kit.WoodParts, kit, new[] { hang, hang - Vector3.up * (0.5f + 0.15f * (i % 3)) }, 0.012f, 0.012f, 4, 0f);
                        Ball(kit.StoneParts, kit, hang - Vector3.up * (0.55f + 0.15f * (i % 3)), new Vector3(0.05f, 0.14f, 0.05f));
                    }
                    // three claw scratches on a boulder beside the hut
                    Vector3 boulder = Local(1.5f, 3.6f);
                    Ball(kit.StoneParts, kit, boulder + Vector3.up * 0.45f, new Vector3(1.3f, 0.9f, 1.1f), 15f);
                    for (int i = 0; i < 3; i++) Ball(kit.WoodParts, kit, boulder + Vector3.up * 0.8f + side * (i * 0.2f - 0.2f) - fwd * 0.5f, new Vector3(0.03f, 0.4f, 0.03f), yaw);
                    break;
                }
                case TreefolkClass.NangThe:   // the crystal spire
                {
                    Hut(parent, kit, pos, yaw, 2.9f, 1.25f, new Color(0.78f, 0.9f, 1f), out surf);
                    var crystalMat = LakeGround.OpaqueMaterial("Lake_CrystalSpot", new Color(0.35f, 0.85f, 0.9f), 0.8f, false, new Color(0.25f, 1.0f, 1.1f));
                    var crystals = new List<CombineInstance>();
                    var rnd = new System.Random(77);
                    Vector3 top = surf(4.0f, 0f, 0f);
                    for (int i = 0; i < 7; i++)
                    {
                        float a = i * Mathf.PI * 2f / 7f;
                        float len = i == 0 ? 3.4f : 1.5f + (float)rnd.NextDouble() * 1.1f;
                        Vector3 b = i == 0 ? top : top + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 0.7f;
                        Quaternion lean = i == 0 ? Quaternion.identity : Quaternion.Euler(Mathf.Sin(a) * 22f, 0f, -Mathf.Cos(a) * 22f);
                        crystals.Add(new CombineInstance { mesh = kit.Blob, transform = Matrix4x4.TRS(b + lean * Vector3.up * len * 0.5f, lean, new Vector3(0.38f, len, 0.38f)) });
                    }
                    LakeGround.AddCombined(parent, "Crystals", crystals, crystalMat);
                    // stones floating in a ring round the spire
                    for (int i = 0; i < 8; i++)
                    {
                        float a = i * Mathf.PI * 2f / 8f + 0.3f;
                        Ball(kit.StoneParts, kit, top + new Vector3(Mathf.Cos(a) * 2.2f, 0.5f + 0.5f * Mathf.Sin(i * 1.9f) + 1.2f, Mathf.Sin(a) * 2.2f), Vector3.one * (0.22f + 0.1f * (i % 3)));
                    }
                    var lightGo = new GameObject("Crystal Light");
                    lightGo.transform.SetParent(parent, false);
                    lightGo.transform.position = top + Vector3.up * 2.5f;
                    var l = lightGo.AddComponent<Light>();
                    l.type = LightType.Point; l.range = 14f; l.intensity = 1.5f; l.color = new Color(0.45f, 0.95f, 1f); l.shadows = LightShadows.None;
                    break;
                }
                case TreefolkClass.QuanThe:   // the creature pen
                {
                    Hut(parent, kit, pos, yaw, 3.0f, 1.1f, new Color(0.8f, 0.82f, 0.68f), out surf);
                    // a round pen of posts and two rails, with three mounds (dens) inside
                    for (int i = 0; i < 18; i++)
                    {
                        float a = i / 18f * Mathf.PI * 2f;
                        Vector3 b = Local(8.5f + Mathf.Cos(a) * 4.6f, Mathf.Sin(a) * 4.6f);
                        Vector3 nb = Local(8.5f + Mathf.Cos((i + 1) / 18f * Mathf.PI * 2f) * 4.6f, Mathf.Sin((i + 1) / 18f * Mathf.PI * 2f) * 4.6f);
                        Tube(kit.WoodParts, kit, new[] { b - Vector3.up * 0.2f, b + Vector3.up * 1.2f }, 0.1f, 0.08f, 5);
                        Tube(kit.WoodParts, kit, new[] { b + Vector3.up * 0.5f, nb + Vector3.up * 0.5f }, 0.045f, 0.045f, 4, 0f);
                        Tube(kit.WoodParts, kit, new[] { b + Vector3.up * 0.95f, nb + Vector3.up * 0.95f }, 0.045f, 0.045f, 4, 0f);
                    }
                    for (int i = 0; i < 3; i++)
                    {
                        float a = i * 2.1f + 0.4f;
                        Vector3 m = Local(8.5f + Mathf.Cos(a) * 2.2f, Mathf.Sin(a) * 2.2f);
                        Ball(kit.MossParts, kit, m, new Vector3(1.5f, 0.6f, 1.3f), i * 40f);
                        Ball(kit.GlowParts, kit, m + Vector3.up * 0.15f + fwd * -0.6f, new Vector3(0.2f, 0.16f, 0.2f));    // eyes shine in the den's mouth
                    }
                    // a bell post and a trough of half a log
                    Vector3 bell = Local(4.5f, -3.6f);
                    Tube(kit.WoodParts, kit, new[] { bell - Vector3.up * 0.2f, bell + Vector3.up * 2.6f, bell + Vector3.up * 3.1f + side * 0.7f }, 0.14f, 0.08f, 6);
                    Ball(kit.StoneParts, kit, bell + Vector3.up * 2.75f + side * 0.7f, new Vector3(0.4f, 0.5f, 0.4f));
                    Tube(kit.WoodParts, kit, new[] { Local(4.2f, 3.6f) + Vector3.up * 0.3f, Local(5.6f, 3.6f) + Vector3.up * 0.3f }, 0.32f, 0.3f, 8, 0.05f);
                    break;
                }
                case TreefolkClass.DuongThe:   // the herb garden and the spring
                {
                    Hut(parent, kit, pos, yaw, 3.0f, 1.2f, new Color(0.82f, 1.0f, 0.82f), out surf);
                    var herbs = LakeGround.OpaqueMaterial("Lake_HerbSpot", new Color(0.45f, 0.95f, 0.65f), 0.4f, false, new Color(0.18f, 0.7f, 0.4f));
                    var stalks = new List<CombineInstance>(); var capsList = new List<CombineInstance>();
                    var rnd = new System.Random(88);
                    for (int i = 0; i < 14; i++)
                    {
                        Vector3 p = Local(4.5f + (float)rnd.NextDouble() * 4.5f, ((float)rnd.NextDouble() - 0.5f) * 8f);
                        FloraMeshes.Mushroom(21000 + i, 0.4f + (float)rnd.NextDouble() * 0.5f, out var st, out var cp, out _);
                        var m = Matrix4x4.Translate(p);
                        stalks.Add(new CombineInstance { mesh = st, transform = m });
                        capsList.Add(new CombineInstance { mesh = cp, transform = m });
                    }
                    LakeGround.AddCombined(parent, "Herb Stalks", stalks, LakeGround.OpaqueMaterial("Lake_MushroomStalk", new Color(0.93f, 0.89f, 0.78f), 0.3f, false));
                    LakeGround.AddCombined(parent, "Herb Caps", capsList, herbs);
                    // a small spring: a ring of stones round a disc of clear water
                    Vector3 spring = Local(5.2f, -5.4f);
                    var water = LakeWater.TranslucentMaterial("Lake_HealingWater", null, new Color(0.45f, 1f, 0.85f, 0.6f), new Color(0.05f, 0.25f, 0.2f), 0.9f, (int)RenderQueue.Transparent + 2);
                    var disc = new GameObject("Healing Spring");
                    disc.transform.SetParent(parent, false);
                    disc.transform.position = spring + Vector3.up * 0.12f;
                    disc.AddComponent<MeshFilter>().sharedMesh = WaterMeshes.Disc(1.5f, 0.4f);
                    disc.AddComponent<MeshRenderer>().sharedMaterial = water;
                    for (int i = 0; i < 10; i++)
                    {
                        float a = i * Mathf.PI * 2f / 10f;
                        Ball(kit.StoneParts, kit, spring + new Vector3(Mathf.Cos(a) * 1.65f, 0.12f, Mathf.Sin(a) * 1.65f), new Vector3(0.42f, 0.3f, 0.36f), i * 36f);
                    }
                    // vines hanging from the roof edge
                    for (int i = 0; i < 7; i++)
                    {
                        float a = (0.6f + i * 0.7f);
                        Vector3 top = surf(2.2f, Mathf.Sin(a) * 2.2f, 0.05f);
                        var vine = FloraMeshes.VinePath(top, 1.4f + 0.3f * (i % 3), 22000 + i, 0.2f, 7);
                        Tube(kit.MossParts, kit, vine, 0.04f, 0.02f, 4, 0f);
                    }
                    break;
                }
                case TreefolkClass.XaThe:   // the watchtower
                {
                    Hut(parent, kit, pos, yaw, 2.2f, 1.1f, new Color(0.95f, 0.85f, 0.7f), out surf);
                    Vector3 c = Local(-3.5f, 0f);   // the tower stands behind the hut
                    const float platform = 6.5f;
                    for (int i = 0; i < 4; i++)
                    {
                        float a = i * Mathf.PI * 0.5f + Mathf.PI * 0.25f;
                        Vector3 foot = c + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 2.4f;
                        foot.y = LakeGround.GroundY(foot.x, foot.z);
                        Tube(kit.BarkParts, kit, new[] { foot - Vector3.up * 0.5f, Vector3.Lerp(foot, c + Vector3.up * platform, 0.6f) + Vector3.up * 0.3f, new Vector3(c.x + Mathf.Cos(a) * 1.5f, c.y + platform, c.z + Mathf.Sin(a) * 1.5f) }, 0.34f, 0.24f, 8, 0.2f);
                        var col = new GameObject("Tower Leg").AddComponent<CapsuleCollider>();
                        col.transform.SetParent(parent, false);
                        col.transform.position = (foot + c) * 0.5f + Vector3.up * platform * 0.5f;
                        col.radius = 0.4f; col.height = platform;
                    }
                    var plat = CaveMeshes.Disc(2.4f, 0.3f, 24);
                    var pg = new GameObject("Lookout Platform");
                    pg.transform.SetParent(parent, false);
                    pg.transform.position = c + Vector3.up * platform;
                    pg.AddComponent<MeshFilter>().sharedMesh = plat;
                    pg.AddComponent<MeshRenderer>().sharedMaterial = kit.Wood;
                    pg.AddComponent<MeshCollider>().sharedMesh = plat;
                    for (int i = 0; i < 12; i++)   // railing posts
                    {
                        float a = i * Mathf.PI * 2f / 12f;
                        Vector3 b = c + Vector3.up * (platform + 0.3f) + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 2.25f;
                        Tube(kit.WoodParts, kit, new[] { b, b + Vector3.up * 1.0f }, 0.06f, 0.05f, 4, 0f);
                    }
                    // a conical roof on four posts, an amber lantern under it
                    Ball(kit.MossParts, kit, c + Vector3.up * (platform + 3.4f), new Vector3(3.4f, 1.0f, 3.4f));
                    Ball(kit.GlowParts, kit, c + Vector3.up * (platform + 2.4f), new Vector3(0.3f, 0.4f, 0.3f));
                    // a ladder of rungs between two rails, leaning on the platform
                    Vector3 lb = c + fwd * 3.6f; lb.y = LakeGround.GroundY(lb.x, lb.z);
                    Vector3 lt = c + fwd * 2.1f + Vector3.up * platform;
                    foreach (float o in new[] { -0.3f, 0.3f }) Tube(kit.WoodParts, kit, new[] { lb + side * o, lt + side * o }, 0.05f, 0.05f, 4, 0f);
                    for (int i = 1; i < 12; i++) Tube(kit.WoodParts, kit, new[] { Vector3.Lerp(lb, lt, i / 12f) - side * 0.3f, Vector3.Lerp(lb, lt, i / 12f) + side * 0.3f }, 0.035f, 0.035f, 4, 0f);
                    // archery targets in a row, 12 to 20 m out in front of the hut
                    for (int i = 0; i < 3; i++)
                    {
                        Vector3 p = Local(12f + i * 4f, -3f + i * 3f);
                        Tube(kit.WoodParts, kit, new[] { p - Vector3.up * 0.1f, p + Vector3.up * 1.1f }, 0.05f, 0.05f, 4, 0f);
                        Ball(kit.StoneParts, kit, p + Vector3.up * 1.15f, new Vector3(0.1f, 0.85f, 0.85f), yaw + 180f);
                        Ball(kit.GlowParts, kit, p + Vector3.up * 1.15f - fwd * 0.07f, new Vector3(0.05f, 0.25f, 0.25f), yaw + 180f);
                    }
                    break;
                }
                case TreefolkClass.CauThe:   // the laboratory
                {
                    Hut(parent, kit, pos, yaw, 3.0f, 1.2f, new Color(0.8f, 0.95f, 0.95f), out surf);
                    var gel = LakeWater.TranslucentMaterial("Lake_GelDome", null, new Color(0.72f, 0.86f, 0.50f, 0.5f), Color.black, 0.92f, (int)RenderQueue.Transparent + 1);
                    var vats = new List<CombineInstance>();
                    var cores = LakeGround.OpaqueMaterial("Lake_GelCoreSpot", new Color(0.95f, 0.62f, 0.18f), 0.4f, false, new Color(0.9f, 0.5f, 0.12f));
                    var coreParts = new List<CombineInstance>();
                    for (int i = 0; i < 4; i++)
                    {
                        Vector3 p = Local(5.0f + (i % 2) * 2.2f, -3.6f + i * 2.4f);
                        float rad = 0.7f + 0.15f * (i % 3);
                        Tube(kit.StoneParts, kit, new[] { p - Vector3.up * 0.2f, p + Vector3.up * 0.55f }, rad * 0.9f, rad * 0.85f, 10, 0f);   // a plinth
                        var profile = new List<Vector2> { new Vector2(0f, rad * 1.6f), new Vector2(0.55f * rad, rad * 1.5f), new Vector2(0.95f * rad, rad * 0.9f), new Vector2(rad, rad * 0.25f), new Vector2(rad * 0.95f, 0f) };
                        vats.Add(new CombineInstance { mesh = FloraMeshes.Lathe(profile, 14, rad * 0.5f, 23000 + i, 0.04f), transform = Matrix4x4.Translate(p + Vector3.up * 0.55f) });
                        coreParts.Add(new CombineInstance { mesh = kit.Blob, transform = Matrix4x4.TRS(p + Vector3.up * (0.55f + rad * 0.7f), Quaternion.identity, Vector3.one * rad * 0.7f) });
                        Tube(kit.BarkParts, kit, new[] { p + Vector3.up * (0.55f + rad * 1.5f), p + Vector3.up * (2.0f + 0.2f * i) + fwd * -1.0f, surf(HutSink + 2.4f, -1.0f + i * 0.7f, 0.05f) }, 0.07f, 0.05f, 5, 0.3f);   // root tubes into the roof
                    }
                    LakeGround.AddCombined(parent, "Gel Vats", vats, gel);
                    LakeGround.AddCombined(parent, "Vat Cores", coreParts, cores);
                    // a bench with rows of little jars
                    Vector3 bench = Local(3.4f, 4.4f);
                    Tube(kit.WoodParts, kit, new[] { bench + Vector3.up * 0.85f - side * 1.3f, bench + Vector3.up * 0.85f + side * 1.3f }, 0.25f, 0.25f, 6, 0f);
                    for (int i = 0; i < 6; i++) Ball(kit.GlowParts, kit, bench + Vector3.up * 1.25f + side * (-1.0f + i * 0.4f), new Vector3(0.16f, 0.26f, 0.16f));
                    break;
                }
                default:   // Dã Thể: the camp
                {
                    Hut(parent, kit, pos, yaw, 2.4f, 1.0f, new Color(0.95f, 0.88f, 0.7f), out surf);
                    // a lean-to of two planks and a ridge, with a bedroll under it
                    Vector3 a0 = Local(5.5f, -3.5f), a1 = Local(5.5f, -6.5f);
                    Tube(kit.WoodParts, kit, new[] { a0 - Vector3.up * 0.2f, a0 + Vector3.up * 2.0f }, 0.1f, 0.09f, 5);
                    Tube(kit.WoodParts, kit, new[] { a1 - Vector3.up * 0.2f, a1 + Vector3.up * 2.0f }, 0.1f, 0.09f, 5);
                    for (int i = 0; i < 5; i++)
                    {
                        Vector3 e = Vector3.Lerp(a0, a1, i / 4f);
                        Tube(kit.BarkParts, kit, new[] { e + Vector3.up * 2.0f, e + fwd * 2.4f + Vector3.up * 0.3f }, 0.08f, 0.06f, 5, 0.1f);
                    }
                    Ball(kit.MossParts, kit, Local(6.8f, -5.0f) + Vector3.up * 1.2f, new Vector3(2.4f, 0.25f, 3.6f), yaw);
                    Ball(kit.WoodParts, kit, Local(6.3f, -5.0f) + Vector3.up * 0.15f, new Vector3(1.4f, 0.22f, 2.2f), yaw);
                    // a fire pit: stones, embers and a warm light
                    Vector3 fire = Local(7.5f, -0.5f);
                    for (int i = 0; i < 9; i++)
                    {
                        float a = i * Mathf.PI * 2f / 9f;
                        Ball(kit.StoneParts, kit, fire + new Vector3(Mathf.Cos(a) * 0.8f, 0.12f, Mathf.Sin(a) * 0.8f), new Vector3(0.36f, 0.26f, 0.3f), i * 40f);
                    }
                    Ball(kit.GlowParts, kit, fire + Vector3.up * 0.2f, new Vector3(0.7f, 0.3f, 0.7f));
                    for (int i = 0; i < 3; i++) Tube(kit.WoodParts, kit, new[] { fire + new Vector3(Mathf.Cos(i * 2.1f), 0.15f, Mathf.Sin(i * 2.1f)) * 0.7f, fire + Vector3.up * 0.5f }, 0.07f, 0.05f, 4, 0f);
                    var lightGo = new GameObject("Camp Fire Light");
                    lightGo.transform.SetParent(parent, false);
                    lightGo.transform.position = fire + Vector3.up * 1.0f;
                    var l = lightGo.AddComponent<Light>();
                    l.type = LightType.Point; l.range = 12f; l.intensity = 1.4f; l.color = new Color(1f, 0.6f, 0.25f); l.shadows = LightShadows.None;
                    // a map post (a flat board on a pole), a pile of gear and a hammock between two posts
                    Vector3 map = Local(4.4f, 3.8f);
                    Tube(kit.WoodParts, kit, new[] { map - Vector3.up * 0.2f, map + Vector3.up * 2.2f }, 0.09f, 0.08f, 5);
                    Ball(kit.StoneParts, kit, map + Vector3.up * 1.8f - fwd * 0.12f, new Vector3(0.06f, 0.7f, 0.9f), yaw + 180f);
                    Vector3 pile = Local(2.5f, 5.6f);
                    for (int i = 0; i < 5; i++) Ball(kit.WoodParts, kit, pile + new Vector3(Mathf.Sin(i * 2.2f) * 0.5f, 0.2f + 0.18f * (i % 3), Mathf.Cos(i * 1.7f) * 0.5f), new Vector3(0.5f, 0.3f, 0.4f), i * 55f);
                    Vector3 h0 = Local(9.5f, 3.5f), h1 = Local(9.5f, 7.0f);
                    Tube(kit.WoodParts, kit, new[] { h0 - Vector3.up * 0.2f, h0 + Vector3.up * 1.8f }, 0.11f, 0.09f, 5);
                    Tube(kit.WoodParts, kit, new[] { h1 - Vector3.up * 0.2f, h1 + Vector3.up * 1.8f }, 0.11f, 0.09f, 5);
                    var sling = new Vector3[7];
                    for (int i = 0; i < sling.Length; i++) { float t = i / 6f; sling[i] = Vector3.Lerp(h0 + Vector3.up * 1.6f, h1 + Vector3.up * 1.6f, t) - Vector3.up * 0.7f * Mathf.Sin(t * Mathf.PI); }
                    Tube(kit.MossParts, kit, sling, 0.2f, 0.2f, 6, 0.05f);
                    break;
                }
            }
        }
    }
}
