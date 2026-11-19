using System.Collections.Generic;
using Evolve.Maze;
using Evolve.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Evolve.EditorTools
{
    /// <summary>
    /// Dựng mesh địa hình tự nhiên (gò đồi, lòng trũng, đồi trú lũ, vách bao), vòm trần kín 360 độ
    /// và vòm hầm đá hữu cơ (CaveMeshes.ArchTunnel + đá tự nhiên) — không dùng Cube/Cylinder thô.
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
            const int grid = 72;
            float outer = chamberRadius + 6f;
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
                    uvs.Add(new Vector2(lx / 6f, lz / 6f));
                }
            }

            var tris = new List<int>(grid * grid * 6);
            float maxR2 = (outer + 1.5f) * (outer + 1.5f);
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
            const int grid = 56;
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
                    float r = Mathf.Sqrt(lx * lx + lz * lz);
                    float y = MazeTerrainMath.CeilingHeightAt(lx, lz, 1000 + chamberId * 73, chamberRadius);
                    // Kéo mép ngoài cùng xuống dưới đỉnh vách bao (22m -> 15m) để khép kín 100% không hở kẽ
                    if (r > chamberRadius * 0.96f)
                    {
                        float t = Mathf.Clamp01((r - chamberRadius * 0.96f) / 8f);
                        y = Mathf.Lerp(y, 14f, t);
                    }
                    verts.Add(new Vector3(lx, y, lz));
                    uvs.Add(new Vector2(lx / 6f, lz / 6f));
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
                    // Pháp tuyến hướng xuống lòng hang
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
            // Chặn toàn bộ ánh sáng mặt trời từ bên ngoài chiếu lọt vào khoang
            mr.shadowCastingMode = ShadowCastingMode.TwoSided;
            return go;
        }

        /// <summary>Dựng miệng hầm dạng vòm đá hữu cơ (ArchTunnel + đá tảng viền cửa + lối đi uốn nhẹ), không dùng Cube.</summary>
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

            float length = 9.5f;
            var path = new List<Vector3>
            {
                new Vector3(0f, 0f, -0.8f),
                new Vector3(0f, 0f, 2.5f),
                new Vector3(0.45f, 0f, 6.0f),
                new Vector3(0.95f, 0f, length)
            };

            // 1. Vòm hang cong tự nhiên (CaveMeshes.ArchTunnel)
            var archGo = new GameObject("Vòm Đá Đường Hầm");
            archGo.transform.SetParent(mouth.transform, false);
            var archMesh = CaveMeshes.ArchTunnel(path, width, height, 10);
            archGo.AddComponent<MeshFilter>().sharedMesh = archMesh;
            var archMr = archGo.AddComponent<MeshRenderer>();
            archMr.sharedMaterial = rockMat;
            archMr.shadowCastingMode = ShadowCastingMode.TwoSided;
            archGo.AddComponent<MeshCollider>().sharedMesh = archMesh;

            // 2. Nền dốc đá để đi lại trong hầm (dùng Disc dẹt / lưới dốc thay Cube)
            var floorPathGo = new GameObject("Nền Đá Lối Hầm");
            floorPathGo.transform.SetParent(mouth.transform, false);
            floorPathGo.transform.localPosition = new Vector3(0.3f, -0.35f, length * 0.45f);
            var floorDisc = CaveMeshes.Disc(Mathf.Max(width * 0.75f, length * 0.58f), 0.35f, 16);
            floorPathGo.AddComponent<MeshFilter>().sharedMesh = floorDisc;
            floorPathGo.AddComponent<MeshRenderer>().sharedMaterial = rockMat;
            floorPathGo.AddComponent<MeshCollider>().sharedMesh = floorDisc;

            // 3. Màn tối sâu cuối đoạn khuất hầm
            var darkGo = new GameObject("Bóng Tối Sâu Hầm");
            darkGo.transform.SetParent(mouth.transform, false);
            darkGo.transform.localPosition = new Vector3(0.95f, height * 0.5f, length - 0.5f);
            darkGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var darkDisc = CaveMeshes.Disc(Mathf.Max(width, height) * 0.65f, 0.25f, 12);
            darkGo.AddComponent<MeshFilter>().sharedMesh = darkDisc;
            darkGo.AddComponent<MeshRenderer>().sharedMaterial = darkMat;

            // 4. Đá tảng tự nhiên ôm hai bên cửa hầm cho ăn khớp với vách đá
            PlaceFramingBoulder(mouth.transform, new Vector3(-(width * 0.55f + 0.6f), 0.2f, 0.3f), width * 0.45f + 0.8f, seed + 1, rockMat);
            PlaceFramingBoulder(mouth.transform, new Vector3(width * 0.55f + 0.6f, 0.2f, 0.3f), width * 0.45f + 0.8f, seed + 2, rockMat);
            PlaceFramingBoulder(mouth.transform, new Vector3(0f, height + 0.4f, 0.5f), width * 0.5f + 0.9f, seed + 3, rockMat);

            return mouth.transform;
        }

        public static void PlaceFramingBoulder(Transform parent, Vector3 localPos, float scale, int seed, Material fallbackRockMat)
        {
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFolder + $"Rock_{Mathf.Abs(seed) % 5}.fbx");
            if (fbx != null)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(fbx, parent);
                go.name = "Đá Viền Hang";
                go.transform.localPosition = localPos;
                go.transform.localRotation = Quaternion.Euler(0f, (seed * 73) % 360, 0f);
                go.transform.localScale = Vector3.one * scale;
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

        /// <summary>Tạo vật liệu đá/đất có texture vân sần sùi (Cavern_Rock_v3.png / Cavern_Mud_v3.png) thay vì màu phẳng.</summary>
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
