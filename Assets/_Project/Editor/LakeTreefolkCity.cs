using System.Collections.Generic;
using Evolve.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Evolve.EditorTools
{
    /// <summary>
    /// Evolve → Dress Lake: Treefolk City (spec 2026-11-18-treefolk-city-design). Builds the city from World/CityLayout.
    /// Step 1: the inside of the Great Hall (the cave dome itself comes from Dress Lake: Landmarks): eight great root pillars
    /// from floor to roof, the carved dais with its three steps and the chieftain's throne of petrified wood, the amber
    /// furnace at the far end, and amber lamps; then the round root huts standing round the hall (the old cube shelters are
    /// gone: three of the huts are the slime shelters). Run again to rebuild.
    /// </summary>
    public static class LakeTreefolkCity
    {
        const string DressedName = "Treefolk City (Dressed)";
        const float Sink = 1.2f;   // how far the hall's dome base is sunk below the floor (LakeLandmarks.RootCave)

        [MenuItem("Evolve/Dress Lake: Treefolk City")]
        public static void Dress()
        {
            Physics.SyncTransforms();
            var old = GameObject.Find(DressedName);
            if (old != null) Object.DestroyImmediate(old);
            var root = new GameObject(DressedName).transform;
            root.gameObject.AddComponent<DetailCulling>().ShowDistance = 5000f;   // only the glows fade with the slime's sight

            var bark = LakeGround.OpaqueMaterial("Lake_CanopyBark", new Color(0.24f, 0.17f, 0.11f), 0.12f, false);
            var wood = LakeGround.OpaqueMaterial("Lake_PetrifiedWood", new Color(0.46f, 0.36f, 0.26f), 0.35f, false);
            var stone = LakeGround.OpaqueMaterial("Lake_Limestone", new Color(0.74f, 0.72f, 0.64f), 0.2f, false);
            var amber = LakeGround.OpaqueMaterial("Lake_AmberLampSpot", new Color(0.95f, 0.62f, 0.18f), 0.6f, false, new Color(1.2f, 0.62f, 0.14f));

            Pillars(root, bark, amber);
            Dais(root, stone, wood, amber);
            Furnace(root, stone, bark, amber);
            Houses(root, bark, amber);
            Village(root, bark, wood, stone, amber);

            EditorSceneManager.MarkAllScenesDirty();
            AssetDatabase.SaveAssets();
            Debug.Log("Đã dựng Đại Sảnh người cây (8 cột rễ, bục, ngai, lò hổ phách), " + CityLayout.Houses().Length + " nhà quanh sảnh (3 nhà là chỗ trú cho slime), đường lát đá, cột đèn, tượng, vườn nhỏ và đống củi. Bấm Ctrl+S để lưu scene.");
        }

        static float Floor(Vector2 p) => LakeGround.GroundY(p.x, p.y);

        // ---------------------------------------------------------------- pillars

        static void Pillars(Transform root, Material bark, Material amber)
        {
            var parent = new GameObject("Hall Pillars").transform;
            parent.SetParent(root, false);
            var roots = new List<CombineInstance>();
            var lamps = new List<CombineInstance>();
            var blob = FloraMeshes.Blob();
            var rnd = new System.Random(8008);
            var pillars = CityLayout.Pillars;
            for (int i = 0; i < pillars.Length; i++)
            {
                Vector2 p = pillars[i];
                float floor = Floor(p);
                float roof = floor + CityLayout.RoofAbove(p, Sink);
                float mid = Mathf.Lerp(floor, roof, 0.5f);
                // one root growing up from the floor and one growing down from the roof, meeting in the middle: flared at both ends
                var up = new Vector3[8];
                var down = new Vector3[8];
                float twist = (float)rnd.NextDouble() * Mathf.PI * 2f;
                for (int k = 0; k < up.Length; k++)
                {
                    float t = k / (float)(up.Length - 1);
                    Vector3 sway = new Vector3(Mathf.Cos(twist + t * 3f), 0f, Mathf.Sin(twist + t * 3f)) * 0.25f * t;
                    up[k] = new Vector3(p.x, Mathf.Lerp(floor - 0.6f, mid + 0.4f, t), p.y) + sway;
                    down[k] = new Vector3(p.x, Mathf.Lerp(roof + 0.6f, mid - 0.4f, t), p.y) - sway;
                }
                roots.Add(new CombineInstance { mesh = RootMeshes.RootTube(up, CityLayout.PillarRadius * 1.25f, CityLayout.PillarRadius * 0.85f, 8100 + i, 0.18f, 12), transform = Matrix4x4.identity });
                roots.Add(new CombineInstance { mesh = RootMeshes.RootTube(down, CityLayout.PillarRadius * 1.25f, CityLayout.PillarRadius * 0.85f, 8200 + i, 0.18f, 12), transform = Matrix4x4.identity });
                // a ring of thin roots winding round the shaft
                for (int k = 0; k < 3; k++)
                {
                    var wind = new Vector3[9];
                    float a0 = (float)rnd.NextDouble() * Mathf.PI * 2f;
                    for (int s = 0; s < wind.Length; s++)
                    {
                        float t = s / (float)(wind.Length - 1);
                        float a = a0 + t * 5f;
                        wind[s] = new Vector3(p.x + Mathf.Cos(a) * CityLayout.PillarRadius * 0.95f, Mathf.Lerp(floor + 0.3f, roof - 0.5f, t), p.y + Mathf.Sin(a) * CityLayout.PillarRadius * 0.95f);
                    }
                    roots.Add(new CombineInstance { mesh = RootMeshes.RootTube(wind, 0.18f, 0.12f, 8300 + i * 3 + k, 0.2f, 6), transform = Matrix4x4.identity });
                }
                // an amber lamp on the shaft, facing the aisle
                Vector2 toCentre = (CityLayout.HallCenter - p).normalized;
                lamps.Add(new CombineInstance { mesh = blob, transform = Matrix4x4.TRS(new Vector3(p.x + toCentre.x * (CityLayout.PillarRadius + 0.1f), floor + 3.2f, p.y + toCentre.y * (CityLayout.PillarRadius + 0.1f)), Quaternion.identity, new Vector3(0.45f, 0.6f, 0.45f)) });
                // the pillar is solid
                var col = new GameObject("Pillar Collider " + i).AddComponent<CapsuleCollider>();
                col.transform.SetParent(parent, false);
                col.transform.position = new Vector3(p.x, mid, p.y);
                col.radius = CityLayout.PillarRadius * 1.05f;
                col.height = roof - floor;
            }
            LakeGround.AddCombined(parent, "Pillar Roots", roots, bark);
            LakeGround.AddCombined(parent, "Pillar Lamps", lamps, amber);
        }

        // ---------------------------------------------------------------- dais and throne

        static void Dais(Transform root, Material stone, Material wood, Material amber)
        {
            var parent = new GameObject("Dais And Throne").transform;
            parent.SetParent(root, false);
            Vector2 c = CityLayout.DaisCenter;
            float floor = Floor(c);
            for (int s = 0; s < CityLayout.DaisSteps; s++)
            {
                float radius = CityLayout.DaisRadius * (1f - 0.2f * s);
                var disc = CaveMeshes.Disc(radius, CityLayout.StepHeight * (s + 1) + 0.4f, 48);   // the base sinks 0.4 m into the floor
                var go = new GameObject("Step " + s);
                go.transform.SetParent(parent, false);
                go.transform.position = new Vector3(c.x, floor - 0.4f, c.y);
                go.AddComponent<MeshFilter>().sharedMesh = disc;
                go.AddComponent<MeshRenderer>().sharedMaterial = stone;
                go.AddComponent<MeshCollider>().sharedMesh = disc;
                go.isStatic = true;
            }
            float top = floor + CityLayout.DaisTop;

            // carved ring of amber inlay round the top step
            var blob = FloraMeshes.Blob();
            var inlay = new List<CombineInstance>();
            float inlayR = CityLayout.DaisRadius * 0.6f - 0.25f;
            for (int k = 0; k < 24; k++)
            {
                float a = k * Mathf.PI * 2f / 24f;
                inlay.Add(new CombineInstance { mesh = blob, transform = Matrix4x4.TRS(new Vector3(c.x + Mathf.Cos(a) * inlayR, top + 0.02f, c.y + Mathf.Sin(a) * inlayR), Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f), new Vector3(0.28f, 0.04f, 0.12f)) });
            }
            LakeGround.AddCombined(parent, "Dais Inlay", inlay, amber);

            // the throne: seat, tall carved back with two horns, arms; facing east (the door)
            Vector2 t = CityLayout.ThroneAt;
            var throne = new GameObject("Throne").transform;
            throne.SetParent(parent, false);
            throne.position = new Vector3(t.x, top, t.y);
            Piece(throne, "Seat", new Vector3(0.1f, 0.35f, 0f), new Vector3(1.3f, 0.7f, 1.4f), wood);
            Piece(throne, "Back", new Vector3(-0.55f, 1.35f, 0f), new Vector3(0.35f, 2.0f, 1.5f), wood);
            Piece(throne, "Horn L", new Vector3(-0.55f, 2.5f, -0.55f), new Vector3(0.3f, 0.7f, 0.3f), wood).transform.localRotation = Quaternion.Euler(18f, 0f, 0f);
            Piece(throne, "Horn R", new Vector3(-0.55f, 2.5f, 0.55f), new Vector3(0.3f, 0.7f, 0.3f), wood).transform.localRotation = Quaternion.Euler(-18f, 0f, 0f);
            Piece(throne, "Arm L", new Vector3(0.1f, 0.85f, -0.7f), new Vector3(1.0f, 0.2f, 0.22f), wood);
            Piece(throne, "Arm R", new Vector3(0.1f, 0.85f, 0.7f), new Vector3(1.0f, 0.2f, 0.22f), wood);
            Piece(throne, "Amber Eye", new Vector3(-0.35f, 1.9f, 0f), new Vector3(0.12f, 0.45f, 0.45f), amber);

            // a warm light over the dais
            var lightGo = new GameObject("Dais Light");
            lightGo.transform.SetParent(parent, false);
            lightGo.transform.position = new Vector3(c.x, top + 5f, c.y);
            var l = lightGo.AddComponent<Light>();
            l.type = LightType.Point;
            l.range = 22f;
            l.intensity = 1.4f;
            l.color = new Color(1f, 0.74f, 0.4f);
            l.shadows = LightShadows.None;
        }

        static GameObject Piece(Transform parent, string name, Vector3 local, Vector3 size, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            go.isStatic = true;
            return go;
        }

        // ---------------------------------------------------------------- furnace

        static void Furnace(Transform root, Material stone, Material bark, Material amber)
        {
            var parent = new GameObject("Amber Furnace").transform;
            parent.SetParent(root, false);
            Vector2 f = CityLayout.FurnaceAt;
            float floor = Floor(f);

            // a broad round hearth of stone with a hollow glowing mouth facing the hall, and a chimney root up to the roof
            var hearth = CaveMeshes.Disc(2.6f, 1.6f, 32);
            var go = new GameObject("Hearth");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(f.x, floor - 0.3f, f.y);
            go.AddComponent<MeshFilter>().sharedMesh = hearth;
            go.AddComponent<MeshRenderer>().sharedMaterial = stone;
            go.AddComponent<MeshCollider>().sharedMesh = hearth;
            go.isStatic = true;

            var blob = FloraMeshes.Blob();
            var embers = new List<CombineInstance>();
            // the glowing mouth: a flattened amber ball seen from the east, with embers heaped in front
            embers.Add(new CombineInstance { mesh = blob, transform = Matrix4x4.TRS(new Vector3(f.x + 2.3f, floor + 0.8f, f.y), Quaternion.identity, new Vector3(0.5f, 1.1f, 1.4f)) });
            var rnd = new System.Random(8800);
            for (int i = 0; i < 9; i++)
                embers.Add(new CombineInstance { mesh = blob, transform = Matrix4x4.TRS(new Vector3(f.x + 2.9f + (float)rnd.NextDouble() * 0.9f, floor + 0.1f, f.y + ((float)rnd.NextDouble() - 0.5f) * 2.6f), Quaternion.identity, new Vector3(0.35f, 0.18f, 0.35f)) });
            LakeGround.AddCombined(parent, "Embers", embers, amber);

            // the chimney: a great root rising from the hearth to the roof
            float roof = floor + CityLayout.RoofAbove(f, Sink);
            var path = new Vector3[8];
            for (int k = 0; k < path.Length; k++)
            {
                float t = k / (float)(path.Length - 1);
                path[k] = new Vector3(f.x - 0.5f * Mathf.Sin(t * 3f), Mathf.Lerp(floor + 1.2f, roof + 0.6f, t), f.y);
            }
            var chimney = RootMeshes.RootTube(path, 1.3f, 0.9f, 8810, 0.2f, 10);
            var cgo = new GameObject("Chimney Root");
            cgo.transform.SetParent(parent, false);
            cgo.AddComponent<MeshFilter>().sharedMesh = chimney;
            cgo.AddComponent<MeshRenderer>().sharedMaterial = bark;
            cgo.AddComponent<MeshCollider>().sharedMesh = chimney;
            cgo.isStatic = true;

            var lightGo = new GameObject("Furnace Light");
            lightGo.transform.SetParent(parent, false);
            lightGo.transform.position = new Vector3(f.x + 3.2f, floor + 1.4f, f.y);
            var l = lightGo.AddComponent<Light>();
            l.type = LightType.Point;
            l.range = 20f;
            l.intensity = 1.6f;
            l.color = new Color(1f, 0.55f, 0.2f);
            l.shadows = LightShadows.None;
        }

        // ---------------------------------------------------------------- the houses round the hall

        const float HutSink = 0.3f;   // the dome's base is sunk this far below the ground, so it grows out of it

        static void Houses(Transform root, Material bark, Material amber)
        {
            var parent = new GameObject("Houses").transform;
            parent.SetParent(root, false);
            var earth = LakeGround.OpaqueMaterial("Lake_HutEarth", new Color(0.33f, 0.24f, 0.16f), 0.05f, true);
            var blob = FloraMeshes.Blob();
            var roots = new List<CombineInstance>();
            var lamps = new List<CombineInstance>();
            var houses = CityLayout.Houses();
            for (int i = 0; i < houses.Length; i++)
            {
                var h = houses[i];
                float ground = LakeGround.GroundY(h.Position.x, h.Position.y);
                float r = h.Radius, rz = r * 0.92f, H = r * 1.35f + HutSink;
                // a smooth dome (little wobble, so the door frame and the roots can hug its surface exactly)
                var shell = CaveMeshes.Shell(r, rz, H, 48, 16, 9100 + i, 0.0f);
                // the arched doorway on the local +x side: straight sides to 0.9 m, then a round top to 1.4 m (above the ground)
                WaterMeshes.RemoveTriangles(shell, v =>
                {
                    if (v.x <= 0f) return false;
                    float y = v.y - HutSink;
                    if (y < -0.25f) return false;
                    if (y <= 0.9f) return Mathf.Abs(v.z) < 0.58f;
                    float e = (y - 0.9f) / 0.55f, f = v.z / 0.58f;
                    return e * e + f * f < 1f;
                });
                var go = new GameObject("House " + i);
                go.transform.SetParent(parent, false);
                go.transform.position = new Vector3(h.Position.x, ground - HutSink, h.Position.y);
                go.transform.rotation = Quaternion.Euler(0f, h.Yaw, 0f);
                go.AddComponent<MeshFilter>().sharedMesh = shell;
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = earth;
                mr.shadowCastingMode = ShadowCastingMode.TwoSided;
                go.AddComponent<MeshCollider>().sharedMesh = shell;
                go.isStatic = true;

                // a point on the dome's surface at local height y and sideways z (x from the ellipsoid), nudged out by `push`
                Vector3 Surface(float y, float z, float push)
                {
                    float q = 1f - (y / H) * (y / H) - (z / rz) * (z / rz);
                    float x = r * Mathf.Sqrt(Mathf.Max(0.001f, q)) + push;
                    return go.transform.TransformPoint(new Vector3(x, y, z));
                }
                // the door frame: an arch lying on the surface round the doorway, its feet sunk into the ground
                var frame = new List<Vector3> { Surface(0.05f, -0.64f, 0.02f) - Vector3.up * 0.25f };
                frame.Add(Surface(0.3f, -0.64f, 0.02f));
                for (int k = 0; k <= 8; k++)
                {
                    float t = k / 8f * Mathf.PI;
                    float zz = -0.64f * Mathf.Cos(t), yy = HutSink + 0.9f + 0.62f * Mathf.Sin(t);
                    frame.Add(Surface(yy, zz, 0.03f));
                }
                frame.Add(Surface(0.3f, 0.64f, 0.02f));
                frame.Add(Surface(0.05f, 0.64f, 0.02f) - Vector3.up * 0.25f);
                roots.Add(new CombineInstance { mesh = RootMeshes.RootTube(frame.ToArray(), 0.14f, 0.14f, 9400 + i, 0.08f, 7), transform = Matrix4x4.identity });

                // roots wrapping the dome, lying on its surface (slightly sunk into it), never across the door
                var rnd = new System.Random(9200 + i);
                for (int k = 0; k < 5; k++)
                {
                    float a0 = (0.5f + k) * Mathf.PI * 2f / 5f;
                    if (Mathf.Abs(Mathf.DeltaAngle(a0 * Mathf.Rad2Deg, 0f)) < 45f) continue;
                    var path = new Vector3[9];
                    for (int s2 = 0; s2 < path.Length; s2++)
                    {
                        float t = s2 / (float)(path.Length - 1);
                        float y = Mathf.Lerp(HutSink - 0.35f, H * 0.93f, t);
                        float sway = 0.25f * Mathf.Sin(t * 5f + k);
                        float ca = Mathf.Cos(a0 + sway * 0.4f), sa = Mathf.Sin(a0 + sway * 0.4f);
                        float q = Mathf.Sqrt(Mathf.Max(0.001f, 1f - (y / H) * (y / H)));
                        path[s2] = go.transform.TransformPoint(new Vector3(ca * r * q * 0.985f, y, sa * rz * q * 0.985f));
                    }
                    roots.Add(new CombineInstance { mesh = RootMeshes.RootTube(path, 0.26f, 0.12f, 9500 + i * 5 + k, 0.2f, 7), transform = Matrix4x4.identity });
                }
                lamps.Add(new CombineInstance { mesh = blob, transform = Matrix4x4.TRS(Surface(HutSink + 1.0f, 0.95f, 0.18f), Quaternion.identity, new Vector3(0.2f, 0.28f, 0.2f)) });

                if (!h.Shelter) continue;
                // this hut shelters a slime: a trigger over its floor (the doorway lets a slime of body scale up to 1.4 in)
                var zoneGo = new GameObject("Shelter Zone");
                zoneGo.transform.SetParent(go.transform, false);
                zoneGo.transform.localPosition = new Vector3(-0.3f, HutSink + 0.7f, 0f);
                var col = zoneGo.AddComponent<BoxCollider>();
                col.isTrigger = true;
                col.size = new Vector3(r * 1.3f, 1.4f, r * 1.3f);
                zoneGo.AddComponent<ShelterZone>().MaxBodyScale = 1.4f;
            }
            LakeGround.AddCombined(parent, "House Roots", roots, bark);
            LakeGround.AddCombined(parent, "House Lamps", lamps, amber);
        }

        // ---------------------------------------------------------------- the village: paving, lamp posts, statues, gardens, woodpiles

        static Material FlagstoneMaterial()
        {
            const string path = "Assets/_Project/Models/LakeFlagstones.png";
            var tex = LakeWater.PaintedTexture(path, 128, 64, (u, v) =>
            {
                // two rows of slabs, the second shifted by half a slab; dark mortar between them
                float row = v < 0.5f ? 0f : 1f, uu = (u * 4f + row * 0.5f) % 1f, vv = (v * 2f) % 1f;
                float edge = Mathf.Min(Mathf.Min(uu, 1f - uu) * 4f, Mathf.Min(vv, 1f - vv) * 2f);
                int slab = Mathf.FloorToInt(u * 4f + row * 0.5f) + (int)row * 7;
                float tone = 0.62f + 0.14f * LakeWater.TileableNoise(slab * 1.7f, row * 3f, 64, 64) - (edge < 0.09f ? 0.28f : 0f);
                return new Color(tone * 0.92f, tone * 0.88f, tone * 0.78f, 1f);
            });
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer != null && importer.wrapModeV != TextureWrapMode.Repeat)
            {
                importer.wrapModeU = TextureWrapMode.Repeat; importer.wrapModeV = TextureWrapMode.Repeat;
                importer.SaveAndReimport();
                tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }
            var mat = LakeGround.OpaqueMaterial("Lake_Flagstones", Color.white, 0.12f, true);
            mat.SetTexture("_BaseMap", tex);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static void Village(Transform root, Material bark, Material wood, Material stone, Material amber)
        {
            var parent = new GameObject("Village").transform;
            parent.SetParent(root, false);
            var blob = FloraMeshes.Blob();
            var rnd = new System.Random(9900);

            // paving: a flagstone ribbon from every doorstep, in to the ring and round to the hall door
            var paving = FlagstoneMaterial();
            var ribbons = new List<Mesh>();
            var ringPoints = new List<Vector2>();
            foreach (var path in CityLayout.HousePaths())
            {
                var dense = new List<Vector2>();
                for (int i = 0; i + 1 < path.Length; i++)
                {
                    int n = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(path[i], path[i + 1]) / 1.2f));
                    for (int k = 0; k < n; k++) dense.Add(Vector2.Lerp(path[i], path[i + 1], k / (float)n));
                }
                dense.Add(path[path.Length - 1]);
                ribbons.Add(GroundMeshes.Ribbon(dense, 0.9f, 9910 + ribbons.Count, LakeGround.GroundY, 0.04f));
            }
            var ring = new List<Vector2>();
            for (float a = -180f; a <= 180f; a += 4f) ring.Add(CityLayout.RingPoint(a));
            ribbons.Add(GroundMeshes.Ribbon(ring, 1.5f, 9999, LakeGround.GroundY, 0.035f));   // the paved ring itself, a little wider
            var pave = new List<CombineInstance>();
            foreach (var m in ribbons) pave.Add(new CombineInstance { mesh = m, transform = Matrix4x4.identity });
            LakeGround.AddCombined(parent, "Paving", pave, paving);

            // lamp posts along the ring: a root post with an amber lamp, every 24 degrees (skipping the east, where the road comes)
            var posts = new List<CombineInstance>();
            var lamps = new List<CombineInstance>();
            for (float a = -168f; a <= 168f; a += 24f)
            {
                if (Mathf.Abs(a) < 20f) continue;
                Vector2 p = CityLayout.RingPoint(a) + (CityLayout.RingPoint(a) - CityLayout.HallCenter).normalized * 1.9f;
                if (ZoneLayout.DepthAt(p.x, p.y) > 0f) continue;
                float y = LakeGround.GroundY(p.x, p.y);
                posts.Add(new CombineInstance { mesh = RootMeshes.RootTube(new[] { new Vector3(p.x, y - 0.3f, p.y), new Vector3(p.x + 0.1f, y + 1.1f, p.y), new Vector3(p.x, y + 2.1f, p.y) }, 0.16f, 0.1f, 9920 + posts.Count, 0.2f, 6), transform = Matrix4x4.identity });
                lamps.Add(new CombineInstance { mesh = blob, transform = Matrix4x4.TRS(new Vector3(p.x, y + 2.25f, p.y), Quaternion.identity, new Vector3(0.38f, 0.5f, 0.38f)) });
            }
            LakeGround.AddCombined(parent, "Lamp Posts", posts, bark);
            LakeGround.AddCombined(parent, "Post Lamps", lamps, amber);

            // statues
            var statuePieces = new List<CombineInstance>();
            var statueLamps = new List<CombineInstance>();
            foreach (var (pos, yaw) in CityLayout.Statues())
            {
                float y = LakeGround.GroundY(pos.x, pos.y);
                var at = new Vector3(pos.x, y, pos.y);
                var rot = Quaternion.Euler(0f, yaw, 0f);
                var plinth = CaveMeshes.Disc(0.8f, 0.55f, 16);
                statuePieces.Add(new CombineInstance { mesh = plinth, transform = Matrix4x4.TRS(at - Vector3.up * 0.2f, rot, Vector3.one) });
                var body = new[] { at + Vector3.up * 0.3f, at + Vector3.up * 1.3f, at + Vector3.up * 2.2f };
                statuePieces.Add(new CombineInstance { mesh = RootMeshes.RootTube(body, 0.5f, 0.3f, 9930 + statuePieces.Count, 0.12f, 10), transform = Matrix4x4.identity });
                statuePieces.Add(new CombineInstance { mesh = blob, transform = Matrix4x4.TRS(at + Vector3.up * 2.55f, rot, new Vector3(0.55f, 0.6f, 0.55f)) });                    // the head
                Vector3 side = rot * new Vector3(0f, 0f, 1f);
                var arm = new[] { at + Vector3.up * 1.9f + side * 0.2f, at + Vector3.up * 2.4f + side * 0.6f, at + Vector3.up * 3.0f + side * 0.7f };
                statuePieces.Add(new CombineInstance { mesh = RootMeshes.RootTube(arm, 0.13f, 0.09f, 9940 + statuePieces.Count, 0.1f, 6), transform = Matrix4x4.identity });          // a raised arm
                statueLamps.Add(new CombineInstance { mesh = blob, transform = Matrix4x4.TRS(at + Vector3.up * 3.2f + side * 0.7f, Quaternion.identity, new Vector3(0.3f, 0.4f, 0.3f)) });   // holding a lamp
                var col = new GameObject("Statue Collider").AddComponent<CapsuleCollider>();
                col.transform.SetParent(parent, false);
                col.transform.position = at + Vector3.up * 1.3f;
                col.radius = 0.6f; col.height = 2.8f;
            }
            LakeGround.AddCombined(parent, "Statues", statuePieces, stone);
            LakeGround.AddCombined(parent, "Statue Lamps", statueLamps, amber);

            // beside each house: a little garden bed (a mossy mound with mushrooms and stones) and, at some, a woodpile
            var mounds = new List<CombineInstance>();
            var stalks = new List<CombineInstance>();
            var caps = new List<CombineInstance>();
            var rocks = new List<CombineInstance>();
            var piles = new List<CombineInstance>();
            var rockMesh = GroundMeshes.Rock(9950, out var rockMoss, 0.8f);
            var houses = CityLayout.Houses();
            for (int i = 0; i < houses.Length; i++)
            {
                var h = houses[i];
                float yaw = -h.Yaw * Mathf.Deg2Rad;
                Vector2 front = new Vector2(Mathf.Cos(yaw), Mathf.Sin(yaw)), side2 = new Vector2(-front.y, front.x) * (i % 2 == 0 ? 1f : -1f);
                Vector2 bed = h.Position + front * (h.Radius + 1.2f) + side2 * 2.3f;
                if (ZoneLayout.DepthAt(bed.x, bed.y) > 0f || ZoneLayout.DistanceToPath(bed.x, bed.y) < 2f) continue;
                float y = LakeGround.GroundY(bed.x, bed.y);
                mounds.Add(new CombineInstance { mesh = blob, transform = Matrix4x4.TRS(new Vector3(bed.x, y - 0.12f, bed.y), Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f), new Vector3(1.9f, 0.5f, 1.4f)) });
                for (int k = 0; k < 3; k++)
                {
                    Vector2 q = bed + new Vector2((float)rnd.NextDouble() - 0.5f, (float)rnd.NextDouble() - 0.5f) * 1.2f;
                    FloraMeshes.Mushroom(9960 + i * 3 + k, 0.35f + (float)rnd.NextDouble() * 0.35f, out var stalk, out var cap, out _);
                    var m = Matrix4x4.Translate(new Vector3(q.x, LakeGround.GroundY(q.x, q.y) + 0.12f, q.y));
                    stalks.Add(new CombineInstance { mesh = stalk, transform = m });
                    caps.Add(new CombineInstance { mesh = cap, transform = m });
                }
                for (int k = 0; k < 6; k++)   // a ring of little stones round the bed
                {
                    float a = k * Mathf.PI * 2f / 6f;
                    Vector2 q = bed + new Vector2(Mathf.Cos(a) * 1.15f, Mathf.Sin(a) * 0.85f);
                    rocks.Add(new CombineInstance { mesh = rockMesh, transform = Matrix4x4.TRS(new Vector3(q.x, LakeGround.GroundY(q.x, q.y) - 0.04f, q.y), Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f), Vector3.one * 0.22f) });
                }
                if (i % 3 != 1) continue;
                Vector2 pile = h.Position - front * 0.2f + side2 * -(h.Radius + 0.6f);   // against the hut's flank
                float py = LakeGround.GroundY(pile.x, pile.y);
                Vector2 along = new Vector2(-front.y, front.x);
                for (int row = 0; row < 3; row++)
                    for (int k = 0; k < 3 - row; k++)
                    {
                        Vector2 c = pile + along * (k - (2 - row) * 0.5f) * 0.4f;
                        var log = new[] { new Vector3(c.x - front.x * 0.6f, py + 0.17f + row * 0.3f, c.y - front.y * 0.6f), new Vector3(c.x + front.x * 0.6f, py + 0.17f + row * 0.3f, c.y + front.y * 0.6f) };
                        piles.Add(new CombineInstance { mesh = RootMeshes.RootTube(log, 0.17f, 0.15f, 9970 + piles.Count, 0.1f, 6), transform = Matrix4x4.identity });
                    }
            }
            LakeGround.AddCombined(parent, "Garden Mounds", mounds, LakeGround.MossMaterial());
            LakeGround.AddCombined(parent, "Garden Stalks", stalks, LakeGround.OpaqueMaterial("Lake_MushroomStalk", new Color(0.93f, 0.89f, 0.78f), 0.3f, false));
            LakeGround.AddCombined(parent, "Garden Caps", caps, LakeGround.OpaqueMaterial("Lake_MushroomCap0", new Color(0.94f, 0.89f, 0.74f), 0.45f, false, new Color(0.094f, 0.089f, 0.074f)));
            LakeGround.AddCombined(parent, "Garden Stones", rocks, stone);
            LakeGround.AddCombined(parent, "Woodpiles", piles, wood);
        }
    }
}
