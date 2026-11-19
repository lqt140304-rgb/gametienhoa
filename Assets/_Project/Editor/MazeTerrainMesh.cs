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
            const int grid = 96;
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
                    tris.Add(a); tris.Add(c); tris.Add(b);
                    tris.Add(b); tris.Add(c); tris.Add(d);
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
        /// Dựng miệng hang đồ sộ, thông thoáng 100% (lòng hang đi thẳng trên mặt đất tự nhiên,
        /// đá viền tách sang hai bên mép ngoài và cắm sâu xuống đất, không chặn hay lấp cửa).
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
            int seed = 1)
        {
            float rad = angleDeg * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad));
            Vector3 pos = dir * radius;
            pos.y = baseY;

            var mouth = new GameObject(name);
            mouth.transform.SetParent(parent, false);
            mouth.transform.position = pos;
            mouth.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);

            const float length = 6.2f;
            var path = new List<Vector3>
            {
                new Vector3(0f, -0.45f, -0.6f),
                new Vector3(0f, -0.45f, 2.2f),
                new Vector3(0f, -0.45f, length)
            };

            // 1. Vòm hang cong lớn (ArchTunnel rỗng lòng, chân cắm sâu -0.95m xuống đất)
            var archGo = new GameObject("Vòm Đá Đường Hầm");
            archGo.transform.SetParent(mouth.transform, false);
            var archMesh = CaveMeshes.ArchTunnel(path, width, height, 12);
            archGo.AddComponent<MeshFilter>().sharedMesh = archMesh;
            var archMr = archGo.AddComponent<MeshRenderer>();
            archMr.sharedMaterial = rockMat;
            archMr.shadowCastingMode = ShadowCastingMode.TwoSided;

            // 2. Màn tối sâu cuối hầm (chỉ là hình ảnh, không collider cản đường)
            var darkGo = new GameObject("Bóng Tối Sâu Hầm");
            darkGo.transform.SetParent(mouth.transform, false);
            darkGo.transform.localPosition = new Vector3(0f, height * 0.42f, length - 0.8f);
            darkGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var darkDisc = CaveMeshes.Disc(Mathf.Max(width, height) * 0.56f, 0.25f, 14);
            darkGo.AddComponent<MeshFilter>().sharedMesh = darkDisc;
            darkGo.AddComponent<MeshRenderer>().sharedMaterial = darkMat;

            // 3. Đá viền đặt TÁCH hẳn sang 2 mép ngoài (không lấn vào lòng cửa hang) và cắm chân sâu xuống đất
            float sideOffset = width * 0.65f + 1.35f;
            PlaceFramingBoulder(mouth.transform, new Vector3(-sideOffset, -0.7f, 0.6f), 1.45f, seed + 1, rockMat, disableCollider: true);
            PlaceFramingBoulder(mouth.transform, new Vector3(sideOffset, -0.7f, 0.6f), 1.45f, seed + 2, rockMat, disableCollider: true);

            // 4. Đèn hắt ấm nhẹ ở đỉnh vòm giúp người chơi nhận biết rõ miệng hang từ xa
            var gateLight = new GameObject("Ánh Sáng Cửa Hang").AddComponent<Light>();
            gateLight.transform.SetParent(mouth.transform, false);
            gateLight.transform.localPosition = new Vector3(0f, height * 0.72f, 0.8f);
            gateLight.type = LightType.Point;
            gateLight.range = 14f;
            gateLight.color = new Color(0.55f, 0.90f, 0.85f);
            gateLight.intensity = 1.6f;
            gateLight.shadows = LightShadows.None;

            return mouth.transform;
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
