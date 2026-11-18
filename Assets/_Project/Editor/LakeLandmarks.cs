using System.Collections.Generic;
using Evolve.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Evolve.EditorTools
{
    /// <summary>
    /// Evolve → Dress Lake: Landmarks. Each area's landmark from the concept art (spec 2026-11-17-lake-map-500m-design):
    ///  01 Vũng Sinh Mệnh: gel nursery banks on the bay's shore, mossy mounds crowded with gel domes and amber cores.
    ///  02 Chân Rễ Cổ Thụ: the ancient root cave, a solid dome 12 m high wrapped in great roots, with a wide door toward the
    ///     lake and a narrow slit to the north that only a slime fits through; warm humus inside.
    ///  The root tunnel along the trail from the cave's side down into the sinkhole (03).
    ///  03 Bãi Mùn Rễ: a great hollow rotting log a slime can crawl through, bracket fungus and leaf drifts round it.
    ///  04 Vườn Bào Tử: a cluster of giant cream mushrooms (up to ~7 m) on the plateau.
    ///  The root bridge from the plateau (04) over the low ground to the ridge (06).
    ///  05 Khe Thủy Mạch: pale limestone boulders lining the gorge walls, small rapids of rocks and foam in the stream,
    ///     a ring of mossy limestone round each deep pool.
    ///  06 Rìa Săn Mồi: the root tunnel arch (4 m wide, 8 m high) where the hunter's way in leaves the ridge.
    /// Run again to rebuild.
    /// </summary>
    public static class LakeLandmarks
    {
        const string Root = "Assets/_Project";
        const string DressedName = "Lake Landmarks (Dressed)";
        /// <summary>Gap of the cave's slit, as in the concept art: 0.9 m wide, 0.7 m high (a slime 0.6 m across fits, a grown one up to scale 1.4 too).</summary>
        const float SlitWidth = 0.9f, SlitHeight = 0.7f;

        [MenuItem("Evolve/Dress Lake: Landmarks")]
        public static void Dress()
        {
            PrototypeSceneBuilder.EnsureFolder(Root + "/Materials");
            PrototypeSceneBuilder.EnsureFolder(Root + "/Models");
            Physics.SyncTransforms();
            var old = GameObject.Find(DressedName);
            if (old != null) Object.DestroyImmediate(old);
            var root = new GameObject(DressedName).transform;
            root.gameObject.AddComponent<DetailCulling>().ShowDistance = 5000f;   // only fades the glows and lights with sight

            var bark = LakeGround.OpaqueMaterial("Lake_CanopyBark", new Color(0.24f, 0.17f, 0.11f), 0.12f, false);
            var earth = LakeGround.OpaqueMaterial("Lake_CaveEarth", new Color(0.30f, 0.22f, 0.15f), 0.05f, true);

            GelBanks(root);
            RootCave(root, earth, bark);
            RootTunnel(root, earth, bark);
            RottingLog(root);
            GiantMushrooms(root);
            RootBridge(root, bark);
            WaterVein(root);
            RootArch(root);

            EditorSceneManager.MarkAllScenesDirty();
            AssetDatabase.SaveAssets();
            Debug.Log("Đã dựng điểm nhấn cả 6 khu: bờ ổ gel, hang rễ, đường hầm, khúc gỗ mục, cụm nấm khổng lồ, cầu rễ, khe đá vôi và vòm rễ lối vào. Bấm Ctrl+S để lưu scene.");
        }

        // ---------------------------------------------------------------- 01: gel nursery banks

        static void GelBanks(Transform root)
        {
            var parent = new GameObject("01 Gel Nursery Banks").transform;
            parent.SetParent(root, false);
            var shore = ZoneLayout.LakeShore(640);
            var picked = new List<Vector2>();
            for (int i = 0; i < shore.Length && picked.Count < 3; i += 7)
            {
                Vector2 p = shore[i] + (shore[i] - ZoneLayout.LakeCenter).normalized * 2.5f;   // just up the bank
                if (ZoneLayout.Influence(0, p.x, p.y) < 0.7f || ZoneLayout.DepthAt(p.x, p.y) > 0f) continue;
                bool far = true;
                foreach (var q in picked) if (Vector2.Distance(p, q) < 14f) far = false;
                if (far) picked.Add(p);
            }
            var rnd = new System.Random(101);
            var blob = FloraMeshes.Blob();
            var mounds = new List<CombineInstance>();
            var domes = new List<CombineInstance>();
            var cores = new List<CombineInstance>();
            foreach (var p in picked)
            {
                float y = LakeGround.GroundY(p.x, p.y);
                var rot = Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f);
                mounds.Add(new CombineInstance { mesh = blob, transform = Matrix4x4.TRS(new Vector3(p.x, y - 0.1f, p.y), rot, new Vector3(4.2f, 0.9f, 2.6f)) });
                int count = 5 + rnd.Next(3);
                for (int k = 0; k < count; k++)
                {
                    Vector3 local = rot * new Vector3(((float)rnd.NextDouble() - 0.5f) * 3.2f, 0f, ((float)rnd.NextDouble() - 0.5f) * 1.8f);
                    float radius = 0.3f + (float)rnd.NextDouble() * 0.45f;
                    float lx = local.x / 2.1f, lz = local.z / 1.3f;
                    float top = y - 0.1f + 0.45f * Mathf.Sqrt(Mathf.Max(0f, 1f - lx * lx - lz * lz));   // on the mound's surface
                    var at = new Vector3(p.x + local.x, top - 0.05f, p.y + local.z);
                    var profile = new List<Vector2>
                    {
                        new Vector2(0f, radius * 0.62f), new Vector2(0.55f * radius, radius * 0.56f), new Vector2(0.85f * radius, radius * 0.34f),
                        new Vector2(1.0f * radius, radius * 0.12f), new Vector2(1.06f * radius, 0f)
                    };
                    domes.Add(new CombineInstance { mesh = FloraMeshes.Lathe(profile, 14, radius * 0.3f, 6100 + domes.Count, 0.06f), transform = Matrix4x4.Translate(at) });
                    cores.Add(new CombineInstance { mesh = blob, transform = Matrix4x4.TRS(at + Vector3.up * radius * 0.22f, Quaternion.identity, new Vector3(radius * 0.5f, radius * 0.4f, radius * 0.5f)) });
                }
            }
            LakeGround.AddCombined(parent, "Mounds", mounds, LakeGround.MossMaterial());
            LakeGround.AddCombined(parent, "Gel Domes", domes, LakeWater.TranslucentMaterial("Lake_GelDome", null, new Color(0.72f, 0.86f, 0.50f, 0.5f), Color.black, 0.92f, (int)RenderQueue.Transparent + 1));
            LakeGround.AddCombined(parent, "Gel Cores", cores, LakeGround.OpaqueMaterial("Lake_GelCoreSpot", new Color(0.95f, 0.62f, 0.18f), 0.4f, false, new Color(0.9f, 0.5f, 0.12f)));
        }

        // ---------------------------------------------------------------- 02: the root cave

        static void RootCave(Transform root, Material earth, Material bark)
        {
            var parent = new GameObject("02 Root Cave").transform;
            parent.SetParent(root, false);
            Vector2 c = ZoneLayout.RootFoot.Center;
            float floor = LakeGround.GroundY(c.x, c.y);
            const float sink = 1.2f;
            float rx = ZoneLayout.CaveRadii.x, rz = ZoneLayout.CaveRadii.y, h = ZoneLayout.CaveHeight + sink;
            Vector3 origin = new Vector3(c.x, floor - sink, c.y);

            // the shell, with a wide door toward the lake (east) and a hole to the north narrowed into a slit below
            var shell = CaveMeshes.Shell(rx, rz, h, 120, 28, 202, 0.06f);
            WaterMeshes.RemoveTriangles(shell, v => v.x > 0f && Mathf.Abs(v.z) < 2.6f && v.y < sink + 3.6f);       // the door
            WaterMeshes.RemoveTriangles(shell, v => v.z > 0f && Mathf.Abs(v.x) < 1.6f && v.y < sink + 2.0f);       // the slit's hole
            var go = new GameObject("Cave Shell");
            go.transform.SetParent(parent, false);
            go.transform.position = origin;
            go.AddComponent<MeshFilter>().sharedMesh = shell;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = earth;
            mr.shadowCastingMode = ShadowCastingMode.TwoSided;
            go.AddComponent<MeshCollider>().sharedMesh = shell;   // solid: only the door and the slit lead in
            go.isStatic = true;

            // the slit: root blocks narrow the hole to a gap only a slime fits through
            float wallZ = c.y + rz * Mathf.Cos(Mathf.Asin(Mathf.Clamp01(sink / h))) - 0.2f;
            float side = SlitWidth * 0.5f + (3.2f - SlitWidth) * 0.25f;
            float blockW = (3.2f - SlitWidth) * 0.5f;
            Block(parent, "Slit Root L", new Vector3(c.x - side, floor + 1.0f, wallZ), new Vector3(blockW, 2.6f, 2.2f), bark);
            Block(parent, "Slit Root R", new Vector3(c.x + side, floor + 1.0f, wallZ), new Vector3(blockW, 2.6f, 2.2f), bark);
            Block(parent, "Slit Root Top", new Vector3(c.x, floor + SlitHeight + 0.6f, wallZ), new Vector3(SlitWidth + 0.2f, 1.2f, 2.2f), bark);

            // great roots wrapping the dome (never across the door or the slit), and buttresses splaying at its foot
            var rnd = new System.Random(203);
            var roots = new List<CombineInstance>();
            for (int i = 0; i < 12; i++)
            {
                float a = i * Mathf.PI / 12f + (float)rnd.NextDouble() * 0.12f;
                float deg = Mathf.Repeat(a * Mathf.Rad2Deg, 180f);
                if (deg < 14f || deg > 166f || Mathf.Abs(deg - 90f) < 12f) continue;   // the door (east/west line) and the slit (north/south line)
                var path = new Vector3[14];
                for (int k = 0; k < path.Length; k++)
                {
                    float t = k / (float)(path.Length - 1) * Mathf.PI;
                    float s = 1.04f;
                    path[k] = origin + new Vector3(Mathf.Cos(a) * rx * Mathf.Cos(t) * s, h * Mathf.Sin(t) * s - (k == 0 || k == path.Length - 1 ? 0.8f : 0f), Mathf.Sin(a) * rz * Mathf.Cos(t) * s);
                }
                float r = 0.9f + (float)rnd.NextDouble() * 0.6f;
                roots.Add(new CombineInstance { mesh = RootMeshes.RootTube(path, r, r * 0.8f, 2100 + i, 0.25f, 9), transform = Matrix4x4.identity });
            }
            for (int i = 0; i < 10; i++)
            {
                float a = i * Mathf.PI * 2f / 10f + 0.2f;
                if (Mathf.Abs(Mathf.DeltaAngle(a * Mathf.Rad2Deg, 0f)) < 18f || Mathf.Abs(Mathf.DeltaAngle(a * Mathf.Rad2Deg, 90f)) < 14f) continue;
                Vector3 dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                Vector3 start = origin + new Vector3(dir.x * rx * 0.93f, sink + 3.5f, dir.z * rz * 0.93f);
                float len = 5f + (float)rnd.NextDouble() * 3f;
                var path = new Vector3[7];
                for (int k = 0; k < path.Length; k++)
                {
                    float t = k / (float)(path.Length - 1);
                    Vector3 p = start + dir * len * t;
                    p.y = Mathf.Lerp(start.y, LakeGround.GroundY(p.x, p.z) - 0.5f, Mathf.Sqrt(t));
                    path[k] = p;
                }
                roots.Add(new CombineInstance { mesh = RootMeshes.RootTube(path, 1.3f, 0.35f, 2200 + i, 0.3f, 8), transform = Matrix4x4.identity });
            }
            LakeGround.AddCombined(parent, "Cave Roots", roots, bark);

            // warm humus floor and a dim warm glow inside
            var outline = WaterOutline.Make(rx * 1.5f, rz * 1.5f, false, 204, 1.5f, 64);
            var humus = LakeGround.Drape(WaterMeshes.Organic(outline, 1f), Matrix4x4.Translate(new Vector3(c.x, 0f, c.y)), 0.04f);
            if (humus != null) LakeGround.AddMesh(parent, "Humus Floor", humus, LakeGround.OpaqueMaterial("Lake_Humus", new Color(0.36f, 0.24f, 0.14f), 0.15f, false));
            var lightGo = new GameObject("Cave Glow");
            lightGo.transform.SetParent(parent, false);
            lightGo.transform.position = new Vector3(c.x, floor + 9f, c.y);
            var l = lightGo.AddComponent<Light>();
            l.type = LightType.Point;
            l.range = 50f;
            l.intensity = 1.6f;
            l.color = new Color(1f, 0.72f, 0.42f);
            l.shadows = LightShadows.None;
        }

        static void Block(Transform parent, string name, Vector3 centre, Vector3 size, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = centre;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            go.isStatic = true;
        }

        // ---------------------------------------------------------------- the tunnel 02 -> 03

        static void RootTunnel(Transform root, Material earth, Material bark)
        {
            var parent = new GameObject("Root Tunnel (02 to 03)").transform;
            parent.SetParent(root, false);
            Vector2 a = ZoneLayout.TunnelLine[0], b = ZoneLayout.TunnelLine[1];
            int n = Mathf.CeilToInt(Vector2.Distance(a, b) / 1.5f);
            var path = new List<Vector3>();
            for (int i = 0; i <= n; i++)
            {
                var p = Vector2.Lerp(a, b, i / (float)n);
                path.Add(new Vector3(p.x, LakeGround.GroundY(p.x, p.y), p.y));
            }
            const float width = 4.5f, height = 3.2f;
            var tube = CaveMeshes.ArchTunnel(path, width, height, 12);
            var go = new GameObject("Tunnel Shell");
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = tube;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = earth;
            mr.shadowCastingMode = ShadowCastingMode.TwoSided;
            go.AddComponent<MeshCollider>().sharedMesh = tube;
            go.isStatic = true;

            // a root rib every ~4.5 m, hugging the arch from the outside
            var ribs = new List<CombineInstance>();
            for (int i = 1; i < path.Count - 1; i += 3)
            {
                Vector3 dir = path[i + 1] - path[i - 1]; dir.y = 0f; dir.Normalize();
                Vector3 side = new Vector3(dir.z, 0f, -dir.x);
                var rib = new Vector3[9];
                for (int k = 0; k < rib.Length; k++)
                {
                    float t = k / (float)(rib.Length - 1) * Mathf.PI;
                    float sink = k == 0 || k == rib.Length - 1 ? -0.6f : 0f;
                    rib[k] = path[i] + side * Mathf.Cos(t) * (width * 0.5f + 0.25f) + Vector3.up * (Mathf.Sin(t) * (height + 0.25f) + sink);
                }
                ribs.Add(new CombineInstance { mesh = RootMeshes.RootTube(rib, 0.45f, 0.35f, 2300 + i, 0.2f, 7), transform = Matrix4x4.identity });
            }
            LakeGround.AddCombined(parent, "Tunnel Ribs", ribs, bark);
        }

        // ---------------------------------------------------------------- 03: the rotting log

        static void RottingLog(Transform root)
        {
            var parent = new GameObject("03 Rotting Log").transform;
            parent.SetParent(root, false);
            Vector2 c = LandmarkLayout.LogCenter;
            Vector2 dir = new Vector2(Mathf.Cos(LandmarkLayout.LogAngle * Mathf.Deg2Rad), Mathf.Sin(LandmarkLayout.LogAngle * Mathf.Deg2Rad));
            float r = LandmarkLayout.LogRadius, len = LandmarkLayout.LogLength;
            var path = new Vector3[10];
            for (int k = 0; k < path.Length; k++)
            {
                float t = k / (float)(path.Length - 1) - 0.5f;
                Vector2 p = c + dir * t * len + new Vector2(-dir.y, dir.x) * Mathf.Sin(t * 2.4f) * 0.4f;
                path[k] = new Vector3(p.x, LakeGround.GroundY(p.x, p.y) + r * 0.82f, p.y);   // sunk a little into the humus
            }
            // a hollow shell: the inside is open at both ends, so a slime can crawl through
            var shell = RootMeshes.RootTube(path, r, r * 0.92f, 3030, 0.12f, 16);
            var logMat = LakeGround.OpaqueMaterial("Lake_LogBarkInside", new Color(0.30f, 0.20f, 0.13f), 0.1f, true);
            var go = new GameObject("Hollow Log");
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = shell;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = logMat;
            go.AddComponent<MeshCollider>().sharedMesh = shell;
            go.isStatic = true;

            // bracket fungus in shelves along its back, two broken branch stubs, moss on top
            var rnd = new System.Random(3031);
            var blob = FloraMeshes.Blob();
            var fungus = new List<CombineInstance>();
            var moss = new List<CombineInstance>();
            for (int k = 1; k < path.Length - 1; k++)
            {
                Vector3 side = Vector3.Cross(path[k + 1] - path[k - 1], Vector3.up).normalized * (rnd.NextDouble() < 0.5 ? 1f : -1f);
                for (int f = 0; f < 3; f++)
                    fungus.Add(new CombineInstance { mesh = blob, transform = Matrix4x4.TRS(path[k] + side * r * 0.95f + Vector3.up * (0.1f + f * 0.22f - 0.25f),
                        Quaternion.LookRotation(side), new Vector3(0.55f - f * 0.1f, 0.08f, 0.38f)) });
                moss.Add(new CombineInstance { mesh = blob, transform = Matrix4x4.TRS(path[k] + Vector3.up * r * 0.88f, Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f), new Vector3(1.3f, 0.28f, 1.0f)) });
            }
            var stubs = new List<CombineInstance>();
            foreach (int k in new[] { 3, 6 })
            {
                Vector3 up = (Vector3.up + Vector3.Cross(path[k + 1] - path[k - 1], Vector3.up).normalized * 0.6f).normalized;
                var branch = new[] { path[k] + up * r * 0.6f, path[k] + up * (r + 0.9f), path[k] + up * (r + 1.7f) + Vector3.up * 0.3f };
                stubs.Add(new CombineInstance { mesh = RootMeshes.RootTube(branch, 0.32f, 0.18f, 3040 + k, 0.2f, 8), transform = Matrix4x4.identity });
            }
            LakeGround.AddCombined(parent, "Bracket Fungus", fungus, LakeGround.OpaqueMaterial("Lake_EarFungus", new Color(0.78f, 0.60f, 0.38f), 0.25f, false));
            LakeGround.AddCombined(parent, "Log Moss", moss, LakeGround.OpaqueMaterial("Lake_RockMoss", new Color(0.30f, 0.50f, 0.22f), 0.1f, false));
            LakeGround.AddCombined(parent, "Branch Stubs", stubs, logMat);
        }

        // ---------------------------------------------------------------- 04: giant mushrooms

        static void GiantMushrooms(Transform root)
        {
            var parent = new GameObject("04 Giant Mushrooms").transform;
            parent.SetParent(root, false);
            Vector2 c = LandmarkLayout.GiantMushrooms;
            var rnd = new System.Random(4040);
            var stalks = new List<CombineInstance>();
            var caps = new[] { new List<CombineInstance>(), new List<CombineInstance>(), new List<CombineInstance>() };
            void Mushroom(Vector2 p, float height, int colour, bool solid)
            {
                FloraMeshes.Mushroom(4100 + stalks.Count, height, out var stalk, out var cap, out _);
                float y = LakeGround.GroundY(p.x, p.y) - 0.05f;
                var m = Matrix4x4.TRS(new Vector3(p.x, y, p.y), Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f), Vector3.one);
                stalks.Add(new CombineInstance { mesh = stalk, transform = m });
                caps[colour].Add(new CombineInstance { mesh = cap, transform = m });
                if (!solid) return;
                var col = new GameObject("Stalk Collider").AddComponent<CapsuleCollider>();   // the big ones are solid to bump into
                col.transform.SetParent(parent, false);
                col.transform.position = new Vector3(p.x, y + height * 0.38f, p.y);
                col.radius = height * 0.075f * 1.3f;
                col.height = height * 0.76f;
            }
            Mushroom(c, 7f, 0, true);
            Mushroom(c + new Vector2(6.5f, 2.5f), 5.5f, 1, true);
            Mushroom(c + new Vector2(-3f, 6f), 4.5f, 2, true);
            for (int i = 0; i < 14; i++)
            {
                float a = (float)rnd.NextDouble() * Mathf.PI * 2f, d = 5f + (float)rnd.NextDouble() * 10f;
                Vector2 p = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * d;
                if (ZoneLayout.DistanceToPath(p.x, p.y) < ZoneLayout.PathWidth) continue;
                float h = i < 5 ? 2f + (float)rnd.NextDouble() * 1.5f : 0.8f + (float)rnd.NextDouble() * 1.2f;
                Mushroom(p, h, rnd.Next(3), h > 2f);
            }
            var capColours = new[] { new Color(0.94f, 0.89f, 0.74f), new Color(0.88f, 0.70f, 0.42f), new Color(0.80f, 0.72f, 0.58f) };
            LakeGround.AddCombined(parent, "Giant Stalks", stalks, LakeGround.OpaqueMaterial("Lake_MushroomStalk", new Color(0.93f, 0.89f, 0.78f), 0.3f, false));
            for (int k = 0; k < 3; k++)
                LakeGround.AddCombined(parent, "Giant Caps " + k, caps[k], LakeGround.OpaqueMaterial("Lake_MushroomCap" + k, capColours[k], 0.45f, false, capColours[k] * 0.10f));
            var glow = new GameObject("Under-cap Glow");
            glow.transform.SetParent(parent, false);
            glow.transform.position = new Vector3(c.x, LakeGround.GroundY(c.x, c.y) + 3f, c.y);
            var l = glow.AddComponent<Light>();
            l.type = LightType.Point;
            l.range = 16f;
            l.intensity = 0.9f;
            l.color = new Color(1f, 0.9f, 0.62f);
            l.shadows = LightShadows.None;
        }

        // ---------------------------------------------------------------- the root bridge 04 -> 06

        static void RootBridge(Transform root, Material bark)
        {
            if (!LandmarkLayout.RootBridge(1337, out var a, out var b)) { Debug.LogWarning("Không tìm được chỗ đặt cầu rễ giữa cao nguyên 04 và sống đất 06."); return; }
            var parent = new GameObject("Root Bridge (04 to 06)").transform;
            parent.SetParent(root, false);
            const float radius = 1.3f;
            // the root runs from a little back on the plateau to a little onto the ridge, sagging gently; its top is the walk
            Vector3 dir = (b - a); dir.y = 0f; dir.Normalize();
            Vector3 start = a - dir * 3f, end = b + dir * 3f;
            start.y = LakeGround.GroundY(start.x, start.z) - radius * 0.6f;
            end.y = LakeGround.GroundY(end.x, end.z) - radius * 0.6f;
            int n = Mathf.CeilToInt(Vector3.Distance(start, end) / 2f);
            var path = new Vector3[n + 1];
            for (int k = 0; k <= n; k++)
            {
                float t = k / (float)n;
                path[k] = Vector3.Lerp(start, end, t) + Vector3.down * Mathf.Sin(t * Mathf.PI) * 0.8f
                          + Vector3.Cross(dir, Vector3.up) * Mathf.Sin(t * Mathf.PI * 2f) * 0.6f;
            }
            var bridge = new List<CombineInstance> { new CombineInstance { mesh = RootMeshes.RootTube(path, radius * 1.15f, radius, 5050, 0.12f, 12), transform = Matrix4x4.identity } };
            // a second, thinner root twisting alongside, and two roots hanging down to the ground under the middle
            var twin = new Vector3[path.Length];
            for (int k = 0; k < path.Length; k++) twin[k] = path[k] + Vector3.Cross(dir, Vector3.up) * (1.3f + 0.3f * Mathf.Sin(k * 0.7f)) + Vector3.up * 0.2f;
            bridge.Add(new CombineInstance { mesh = RootMeshes.RootTube(twin, 0.5f, 0.4f, 5051, 0.2f, 8), transform = Matrix4x4.identity });
            foreach (float t in new[] { 0.35f, 0.65f })
            {
                Vector3 top = path[Mathf.RoundToInt(t * n)];
                Vector3 foot = new Vector3(top.x + 1.5f, LakeGround.GroundY(top.x + 1.5f, top.z + 1f) - 0.4f, top.z + 1f);
                if (top.y - foot.y < 1.5f) continue;
                bridge.Add(new CombineInstance { mesh = RootMeshes.RootTube(new[] { top, Vector3.Lerp(top, foot, 0.5f) + Vector3.right * 0.6f, foot }, 0.6f, 0.3f, 5052, 0.2f, 8), transform = Matrix4x4.identity });
            }
            LakeGround.AddCombined(parent, "Bridge Roots", bridge, bark);

            // the walk: a flat strip along the root's top that the slime can crawl on
            for (int k = 0; k < n; k++)
            {
                Vector3 p0 = path[k] + Vector3.up * radius, p1 = path[k + 1] + Vector3.up * radius;
                var walk = new GameObject("Bridge Walk " + k);
                walk.transform.SetParent(parent, false);
                walk.transform.position = (p0 + p1) * 0.5f - Vector3.up * 0.2f;
                walk.transform.rotation = Quaternion.LookRotation(p1 - p0, Vector3.up);
                walk.AddComponent<BoxCollider>().size = new Vector3(2.2f, 0.4f, Vector3.Distance(p0, p1) + 0.3f);
                walk.isStatic = true;
            }
        }

        // ---------------------------------------------------------------- 05: the limestone gorge

        static void WaterVein(Transform root)
        {
            var parent = new GameObject("05 Limestone Gorge").transform;
            parent.SetParent(root, false);
            var rnd = new System.Random(5055);
            var rocks = new List<(Mesh rock, Mesh moss)>();
            for (int v = 0; v < 5; v++) rocks.Add((GroundMeshes.Rock(5060 + v, out var m, 0.8f), m));
            var stone = new List<CombineInstance>();
            var moss = new List<CombineInstance>();
            void Rock(Vector3 at, Vector3 scale)
            {
                var r = rocks[rnd.Next(rocks.Count)];
                var m = Matrix4x4.TRS(at, Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f), scale);
                stone.Add(new CombineInstance { mesh = r.rock, transform = m });
                moss.Add(new CombineInstance { mesh = r.moss, transform = m });
            }

            // boulders set into both walls of the gorge, along the stream where the gorge is
            var line = ZoneLayout.StreamLine;
            for (int i = 1; i < line.Length - 1; i++)
            {
                if (ZoneLayout.Influence(4, line[i].x, line[i].y) < 0.5f) continue;
                Vector2 dir = (line[i + 1] - line[i - 1]).normalized, side = new Vector2(-dir.y, dir.x);
                float hw = ZoneLayout.StreamHalfWidthAt(i / (float)(line.Length - 1));
                foreach (float s in new[] { -1f, 1f })
                    for (int k = 0; k < 2; k++)
                    {
                        Vector2 p = line[i] + side * s * (hw + 5f + k * 2.5f + (float)rnd.NextDouble() * 1.5f);
                        if (ZoneLayout.DepthAt(p.x, p.y) > 0f || ZoneLayout.DistanceToPath(p.x, p.y) < ZoneLayout.PathWidth) continue;
                        float size = 1.8f + (float)rnd.NextDouble() * 2.2f;
                        Rock(new Vector3(p.x, LakeGround.GroundY(p.x, p.y) - size * 0.25f, p.y), new Vector3(size * 1.2f, size, size));
                    }
            }

            // small rapids: a line of stones across the stream with white water round them, every so often
            var foam = new List<CombineInstance>();
            var foamMesh = WaterMeshes.Organic(WaterOutline.Make(2f, 2f, true, 5070, 0.4f, 24), 0.5f);
            for (int i = 6; i < line.Length - 6; i += 9)
            {
                Vector2 dir = (line[i + 1] - line[i - 1]).normalized, side = new Vector2(-dir.y, dir.x);
                float hw = ZoneLayout.StreamHalfWidthAt(i / (float)(line.Length - 1));
                for (float o = -hw * 0.8f; o <= hw * 0.8f; o += 1.1f)
                {
                    Vector2 p = line[i] + side * o + dir * (float)(rnd.NextDouble() - 0.5);
                    float size = 0.35f + (float)rnd.NextDouble() * 0.35f;
                    Rock(new Vector3(p.x, -ZoneLayout.StreamDepth - 0.05f, p.y), new Vector3(size, size * 0.8f, size));
                    foam.Add(new CombineInstance { mesh = foamMesh, transform = Matrix4x4.TRS(new Vector3(p.x, ZoneLayout.WaterLevel + 0.015f, p.y), Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f), Vector3.one * (0.6f + size)) });
                }
            }

            // a ring of mossy limestone round each deep pool
            for (int pool = 0; pool < ZoneLayout.PoolCenters.Length; pool++)
            {
                var shore = ZoneLayout.PoolShore(pool, 48);
                for (int k = 0; k < shore.Length; k++)
                {
                    Vector2 p = shore[k] + (shore[k] - ZoneLayout.PoolCenters[pool]).normalized * (0.4f + (float)rnd.NextDouble() * 0.6f);
                    if (ZoneLayout.CleanDepthAt(p.x, p.y) > ZoneLayout.StreamDepth + 0.01f) continue;   // never inside the pool itself
                    float size = 0.6f + (float)rnd.NextDouble() * 0.9f;
                    Rock(new Vector3(p.x, LakeGround.GroundY(p.x, p.y) - size * 0.3f, p.y), new Vector3(size * 1.2f, size * 0.7f, size));
                }
            }
            LakeGround.AddCombined(parent, "Limestone", stone, LakeGround.OpaqueMaterial("Lake_Limestone", new Color(0.74f, 0.72f, 0.64f), 0.2f, false));
            LakeGround.AddCombined(parent, "Limestone Moss", moss, LakeGround.OpaqueMaterial("Lake_RockMoss", new Color(0.30f, 0.50f, 0.22f), 0.1f, false));
            LakeGround.AddCombined(parent, "Rapids Foam", foam, LakeWater.TranslucentMaterial("Lake_RapidsFoam", null, new Color(0.95f, 1f, 1f, 0.55f), Color.black, 0.6f, (int)RenderQueue.Transparent + 22));
        }

        // ---------------------------------------------------------------- 06: the root tunnel arch

        static void RootArch(Transform root)
        {
            var parent = new GameObject("06 Root Arch").transform;
            parent.SetParent(root, false);
            // where the hunter's way in leaves the ridge, just before the rim cliff
            Vector2 from = ZoneLayout.HuntEdge.Center, to = ZoneLayout.NorthEntry;
            Vector2 dir = (to - from).normalized, side = new Vector2(-dir.y, dir.x);
            Vector2 at = from + dir * Mathf.Min(Vector2.Distance(from, to) - 20f, 40f);
            const float width = 4f, height = 8f;
            var rnd = new System.Random(6066);
            var roots = new List<CombineInstance>();
            for (int i = 0; i < 6; i++)
            {
                float spread = ((float)rnd.NextDouble() - 0.5f) * 1.6f, lean = ((float)rnd.NextDouble() - 0.5f) * 2f;
                Vector2 l = at + side * (width * 0.5f + 0.9f + (float)rnd.NextDouble() * 0.8f) + dir * spread;
                Vector2 r = at - side * (width * 0.5f + 0.9f + (float)rnd.NextDouble() * 0.8f) + dir * spread;
                var a = new Vector3(l.x, LakeGround.GroundY(l.x, l.y), l.y);
                var b = new Vector3(r.x, LakeGround.GroundY(r.x, r.y), r.y);
                var path = RootMeshes.ArchPath(a, b, height + 0.6f * i * 0.3f + (float)rnd.NextDouble() * 0.8f, 6070 + i, 0.8f, 18, 0.8f);
                for (int k = 0; k < path.Length; k++) path[k] += new Vector3(dir.x, 0f, dir.y) * lean * Mathf.Sin(k / (float)(path.Length - 1) * Mathf.PI);
                float radius = i < 2 ? 0.9f : 0.45f + (float)rnd.NextDouble() * 0.3f;
                roots.Add(new CombineInstance { mesh = RootMeshes.RootTube(path, radius * 1.3f, radius, 6080 + i, 0.25f, 9), transform = Matrix4x4.identity });
            }
            // thin roots hanging from the top of the arch like a curtain
            var top = new Vector3(at.x, LakeGround.GroundY(at.x, at.y) + height, at.y);
            for (int i = 0; i < 9; i++)
            {
                Vector3 start = top + new Vector3(side.x, 0f, side.y) * (((float)rnd.NextDouble() - 0.5f) * width * 0.9f) + Vector3.down * 0.4f;
                var hang = RootMeshes.BranchPath(start, Vector3.down, 2f + (float)rnd.NextDouble() * 3f, 0.3f, 6090 + i, 6);
                roots.Add(new CombineInstance { mesh = RootMeshes.RootTube(hang, 0.09f, 0.03f, 6100 + i, 0.2f, 5), transform = Matrix4x4.identity });
            }
            LakeGround.AddCombined(parent, "Arch Roots", roots, LakeGround.OpaqueMaterial("Lake_DarkRoot", new Color(0.16f, 0.12f, 0.09f), 0.1f, false));
            // the two legs are solid
            foreach (float s in new[] { 1f, -1f })
            {
                Vector2 leg = at + side * s * (width * 0.5f + 1.2f);
                var col = new GameObject("Arch Leg").AddComponent<CapsuleCollider>();
                col.transform.SetParent(parent, false);
                col.transform.position = new Vector3(leg.x, LakeGround.GroundY(leg.x, leg.y) + 2.5f, leg.y);
                col.radius = 1f;
                col.height = 5f;
            }
        }
    }
}
