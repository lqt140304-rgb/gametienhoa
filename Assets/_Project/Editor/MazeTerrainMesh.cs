using System.Collections.Generic;
using Evolve.Maze;
using Evolve.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Evolve.EditorTools
{
    /// <summary>
    /// Dựng mesh địa hình tự nhiên quy mô lớn (đường kính 150m), vòm trần kín 360 độ
    /// và miệng hang đồ sộ thông thoáng (không lấp, không khối hộp thô, không vật thể lơ lửng).
    /// </summary>
    public static class MazeTerrainMesh
    {
        const string Root = "Assets/_Project";
        const string ModelFolder = Root + "/Models/Underroot/";

        public static GameObject BuildFloorAndWalls(
            Transform parent,
            ChamberNode node,
            float chamberRadius,
            IReadOnlyList<float> tunnelAnglesDeg,
            Material floorMat)
        {
            const int grid = 180;
            float outer = chamberRadius + 8f;
            float cell = (outer * 2f) / grid;

            var verts = new List<Vector3>((grid + 1) * (grid + 1));
            var uvs = new List<Vector2>((grid + 1) * (grid + 1));
            for (int zi = 0; zi <= grid; zi++)
            {
                for (int xi = 0; xi <= grid; xi++)
                {
                    float lx = -outer + xi * cell;
                    float lz = -outer + zi * cell;
                    float y = MazeTerrainMath.GroundHeightAt(lx, lz, 1000 + node.Id * 73, chamberRadius, tunnelAnglesDeg, node.HasHighLedge);
                    verts.Add(new Vector3(lx, y, lz));
                    uvs.Add(new Vector2(lx / 8f, lz / 8f));
                }
            }

            var tris = new List<int>(grid * grid * 6);
            float maxR2 = (outer + 2f) * (outer + 2f);
            for (int zi = 0; zi < grid; zi++)
            {
                for (int xi = 0; xi < grid; xi++)
                {
                    float cx = -outer + (xi + 0.5f) * cell;
                    float cz = -outer + (zi + 0.5f) * cell;
                    if (cx * cx + cz * cz > maxR2) continue;

                    int a = zi * (grid + 1) + xi;
                    int b = a + 1;
                    int c = a + (grid + 1);
                    int d = c + 1;
                    if (((xi + zi) & 1) == 0)
                    {
                        tris.Add(a); tris.Add(c); tris.Add(b);
                        tris.Add(b); tris.Add(c); tris.Add(d);
                    }
                    else
                    {
                        tris.Add(a); tris.Add(c); tris.Add(d);
                        tris.Add(a); tris.Add(d); tris.Add(b);
                    }
                }
            }

            var mesh = new Mesh { name = $"MazeFloor_{node.Id:00}", indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var go = new GameObject("Địa Hình Khoang Tự Nhiên");
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = floorMat;
            var mc = go.AddComponent<MeshCollider>();
            mc.sharedMesh = mesh;
            return go;
        }

        public static GameObject BuildClosedCeiling(
            Transform parent,
            int chamberId,
            float chamberRadius,
            Material wallMat)
        {
            const int grid = 72;
            float outer = chamberRadius + 10f;
            float cell = (outer * 2f) / grid;

            var verts = new List<Vector3>((grid + 1) * (grid + 1));
            var uvs = new List<Vector2>((grid + 1) * (grid + 1));
            for (int zi = 0; zi <= grid; zi++)
            {
                for (int xi = 0; xi <= grid; xi++)
                {
                    float lx = -outer + xi * cell;
                    float lz = -outer + zi * cell;
                    float r = Mathf.Sqrt(lx * lx + lz * lz);
                    float y = MazeTerrainMath.CeilingHeightAt(lx, lz, 1000 + chamberId * 73, chamberRadius);
                    if (r > chamberRadius * 0.94f)
                    {
                        float t = Mathf.Clamp01((r - chamberRadius * 0.94f) / 10f);
                        y = Mathf.Lerp(y, 18f, t);
                    }
                    verts.Add(new Vector3(lx, y, lz));
                    uvs.Add(new Vector2(lx / 8f, lz / 8f));
                }
            }

            var tris = new List<int>(grid * grid * 6);
            float maxR2 = (outer + 2f) * (outer + 2f);
            for (int zi = 0; zi < grid; zi++)
            {
                for (int xi = 0; xi < grid; xi++)
                {
                    float cx = -outer + (xi + 0.5f) * cell;
                    float cz = -outer + (zi + 0.5f) * cell;
                    if (cx * cx + cz * cz > maxR2) continue;

                    int a = zi * (grid + 1) + xi;
                    int b = a + 1;
                    int c = a + (grid + 1);
                    int d = c + 1;
                    tris.Add(a); tris.Add(b); tris.Add(c);
                    tris.Add(b); tris.Add(d); tris.Add(c);
                }
            }

            var mesh = new Mesh { name = $"MazeCeiling_{chamberId:00}", indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var go = new GameObject("Vòm Trần Hang Kín");
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = wallMat;
            mr.shadowCastingMode = ShadowCastingMode.TwoSided;
            return go;
        }

        /// <summary>
        /// Dựng miệng &amp; lòng đường hầm Blender tự nhiên bất đối xứng (MazeTunnel_Small/Medium/Large.fbx),
        /// căn chuẩn hướng mở vào trong khoang (-Z) và khúc uốn sâu + dấu hiệu sinh thái hướng ra ngoài (+Z).
        /// Tự động giữ tương thích với các lời gọi chỉ truyền kích thước width/height.
        /// </summary>
        public static Transform BuildNaturalTunnelMouth(
            Transform parent,
            string name,
            float angleDeg,
            float radius,
            float baseY,
            float width,
            float height,
            Material rockMat,
            Material darkMat,
            int seed = 1,
            TunnelSize? sizeHint = null,
            Color? deepGlowColor = null)
        {
            float rad = angleDeg * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad));
            Vector3 pos = dir * radius;
            pos.y = baseY;

            var mouth = new GameObject(name);
            mouth.transform.SetParent(parent, false);
            mouth.transform.position = pos;
            mouth.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);

            TunnelSize resolvedSize = sizeHint ?? (width <= 5.2f ? TunnelSize.Small : width <= 7.8f ? TunnelSize.Medium : TunnelSize.Large);
            string variant = resolvedSize switch
            {
                TunnelSize.Small => "Small",
                TunnelSize.Medium => "Medium",
                _ => "Large"
            };

            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelFolder}MazeTunnel_{variant}.fbx");
            if (fbx != null)
            {
                const float uniformScale = 1.4f;
                var tunnelGo = (GameObject)PrefabUtility.InstantiatePrefab(fbx, mouth.transform);
                tunnelGo.name = $"Đường Hầm Blender ({variant})";
                tunnelGo.transform.localPosition = new Vector3(0f, -0.05f, 0f);
                tunnelGo.transform.localScale = Vector3.one * uniformScale;
                OrientTunnelDeepAlongPlusZ(tunnelGo);
                EnsureTunnelColliderAndGlowTint(tunnelGo, deepGlowColor);

                // Đèn hắt viền đá ngoài cửa giúp nhận biết miệng hang từ xa
                var rimLight = new GameObject("Ánh Sáng Cửa Hang").AddComponent<Light>();
                rimLight.transform.SetParent(mouth.transform, false);
                rimLight.transform.localPosition = new Vector3(0f, height * 0.65f, -1.6f);
                rimLight.type = LightType.Point;
                rimLight.range = 18f;
                rimLight.color = new Color(0.72f, 0.86f, 0.80f);
                rimLight.intensity = 10f;
                rimLight.shadows = LightShadows.None;

                // Đèn dấu hiệu sinh thái nằm sâu bên trong khúc cua để càng vào sâu càng rõ màu khoang đích
                var deepLight = new GameObject("Ánh Sáng Dấu Hiệu Sâu").AddComponent<Light>();
                deepLight.transform.SetParent(mouth.transform, false);
                deepLight.transform.localPosition = new Vector3(0f, height * 0.45f, 6.0f);
                deepLight.type = LightType.Point;
                deepLight.range = 22f;
                deepLight.color = deepGlowColor ?? new Color(0.45f, 0.92f, 0.82f);
                deepLight.intensity = 14f;
                deepLight.shadows = LightShadows.None;

                return mouth.transform;
            }

            Debug.LogWarning($"Không tìm thấy {ModelFolder}MazeTunnel_{variant}.fbx, dùng hình học dự phòng.");
            const float length = 6.2f;
            var path = new List<Vector3>
            {
                new Vector3(0f, -0.45f, -0.6f),
                new Vector3(0f, -0.45f, 2.2f),
                new Vector3(0f, -0.45f, length)
            };

            var archGo = new GameObject("Vòm Đá Đường Hầm");
            archGo.transform.SetParent(mouth.transform, false);
            var archMesh = CaveMeshes.ArchTunnel(path, width, height, 12);
            archGo.AddComponent<MeshFilter>().sharedMesh = archMesh;
            var archMr = archGo.AddComponent<MeshRenderer>();
            archMr.sharedMaterial = rockMat;
            archMr.shadowCastingMode = ShadowCastingMode.TwoSided;
            archGo.AddComponent<MeshCollider>().sharedMesh = archMesh;

            var darkGo = new GameObject("Bóng Tối Sâu Hầm");
            darkGo.transform.SetParent(mouth.transform, false);
            darkGo.transform.localPosition = new Vector3(0f, height * 0.42f, length - 0.8f);
            darkGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var darkDisc = CaveMeshes.Disc(Mathf.Max(width, height) * 0.56f, 0.25f, 14);
            darkGo.AddComponent<MeshFilter>().sharedMesh = darkDisc;
            darkGo.AddComponent<MeshRenderer>().sharedMaterial = darkMat;

            var gateLight = new GameObject("Ánh Sáng Cửa Hang").AddComponent<Light>();
            gateLight.transform.SetParent(mouth.transform, false);
            gateLight.transform.localPosition = new Vector3(0f, height * 0.72f, 0.8f);
            gateLight.type = LightType.Point;
            gateLight.range = 14f;
            gateLight.color = deepGlowColor ?? new Color(0.55f, 0.90f, 0.85f);
            gateLight.intensity = 1.6f;
            gateLight.shadows = LightShadows.None;

            return mouth.transform;
        }

        static void OrientTunnelDeepAlongPlusZ(GameObject tunnelGo)
        {
            MeshFilter shellMf = null;
            foreach (var mf in tunnelGo.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null) continue;
                string n = mf.gameObject.name;
                if (n.EndsWith("_COL") || n.EndsWith("_Glow") || n.EndsWith("_Stalks")) continue;
                shellMf = mf;
                break;
            }
            if (shellMf == null) return;

            var verts = shellMf.sharedMesh.vertices;
            if (verts == null || verts.Length == 0) return;

            float minX = float.MaxValue, maxX = float.MinValue;
            float minZ = float.MaxValue, maxZ = float.MinValue;
            int step = Mathf.Max(1, verts.Length / 400);
            var rootTr = tunnelGo.transform;
            var meshTr = shellMf.transform;

            for (int i = 0; i < verts.Length; i += step)
            {
                Vector3 p = rootTr.InverseTransformPoint(meshTr.TransformPoint(verts[i]));
                if (p.x < minX) minX = p.x;
                if (p.x > maxX) maxX = p.x;
                if (p.z < minZ) minZ = p.z;
                if (p.z > maxZ) maxZ = p.z;
            }

            // Đầu mở miệng hang ở ngắn hơn (-2.6m), còn đầu kín sâu tới (+8.2..9.8m) nên dấu của (max + min) chỉ rõ chiều sâu.
            float asymZ = maxZ + minZ;
            float asymX = maxX + minX;
            if (Mathf.Abs(asymZ) >= Mathf.Abs(asymX))
            {
                tunnelGo.transform.localRotation = asymZ < 0f
                    ? Quaternion.Euler(0f, 180f, 0f)
                    : Quaternion.identity;
            }
            else
            {
                tunnelGo.transform.localRotation = asymX > 0f
                    ? Quaternion.Euler(0f, -90f, 0f)
                    : Quaternion.Euler(0f, 90f, 0f);
            }
        }

        static void EnsureTunnelColliderAndGlowTint(GameObject tunnelGo, Color? deepGlowColor)
        {
            // Kiểm tra xem FBX có node *_COL con không; nếu có thì luôn gắn MeshCollider trên chính node *_COL đó
            // (tránh lỗi collider gắn trên root làm lệch transform nếu node con có xoay/dịch từ Blender).
            bool hasColChild = false;
            foreach (var mf in tunnelGo.GetComponentsInChildren<MeshFilter>(true))
            {
                if (!mf.gameObject.name.EndsWith("_COL") || mf.sharedMesh == null) continue;
                hasColChild = true;
                if (!mf.gameObject.TryGetComponent<MeshCollider>(out var mc) || mc == null)
                    mc = mf.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
                mc.convex = false;
                var mr = mf.GetComponent<MeshRenderer>();
                if (mr != null) mr.enabled = false;
            }

            if (hasColChild)
            {
                // Xóa MeshCollider gắn nhầm trên root của tunnelGo nếu có
                var rootMc = tunnelGo.GetComponent<MeshCollider>();
                if (rootMc != null) Object.DestroyImmediate(rootMc);
            }
            else if (tunnelGo.GetComponentInChildren<Collider>(true) == null)
            {
                foreach (var mf in tunnelGo.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (mf.sharedMesh == null) continue;
                    string n = mf.gameObject.name;
                    if (n.Contains("Glow") || n.Contains("Stalks") || n.Contains("Roots")) continue;
                    var mc = mf.gameObject.AddComponent<MeshCollider>();
                    mc.sharedMesh = mf.sharedMesh;
                    mc.convex = false;
                }
            }

            if (!deepGlowColor.HasValue) return;
            var block = new MaterialPropertyBlock();
            block.SetColor("_Tint", Color.Lerp(Color.white, deepGlowColor.Value, 0.55f));
            foreach (var r in tunnelGo.GetComponentsInChildren<Renderer>(true))
            {
                if (r.gameObject.name.Contains("Glow"))
                    r.SetPropertyBlock(block);
            }
        }

        public static void PlaceFramingBoulder(
            Transform parent,
            Vector3 localPos,
            float scale,
            int seed,
            Material fallbackRockMat,
            bool disableCollider = false)
        {
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFolder + $"Rock_{Mathf.Abs(seed) % 5}.fbx");
            if (fbx != null)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(fbx, parent);
                go.name = "Đá Viền Hang";
                go.transform.localPosition = localPos;
                go.transform.localRotation = Quaternion.Euler(0f, (seed * 73) % 360, 0f);
                go.transform.localScale = Vector3.one * scale;
                if (disableCollider)
                {
                    foreach (var c in go.GetComponentsInChildren<Collider>(true))
                        Object.DestroyImmediate(c);
                }
                return;
            }

            var mesh = GroundMeshes.Rock(seed, out _);
            var rockGo = new GameObject("Đá Viền Hang");
            rockGo.transform.SetParent(parent, false);
            rockGo.transform.localPosition = localPos;
            rockGo.transform.localScale = Vector3.one * scale;
            rockGo.AddComponent<MeshFilter>().sharedMesh = mesh;
            rockGo.AddComponent<MeshRenderer>().sharedMaterial = fallbackRockMat;
            if (!disableCollider) rockGo.AddComponent<MeshCollider>().sharedMesh = mesh;
        }

        public static Material TexturedSurfaceMaterial(string name, Color tint, float smoothness, bool twoSided, string texFile = "Cavern_Rock_v3.png", Color emission = default)
        {
            var mat = LakeGround.OpaqueMaterial(name, tint, smoothness, twoSided, emission);
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{Root}/Terrain/{texFile}");
            if (tex == null) tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{Root}/Terrain/Cavern_Mud_v3.png");
            if (tex != null)
            {
                mat.SetTexture("_BaseMap", tex);
                EditorUtility.SetDirty(mat);
            }
            return mat;
        }
    }
}
