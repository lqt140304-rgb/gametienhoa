using System.Collections.Generic;
using Evolve.Civilization;
using Evolve.Visuals;
using Evolve.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Evolve.EditorTools
{
    /// <summary>
    /// Evolve → Dress Lake: Treefolk. Puts the placeholder band of treefolk in the Root Foot cave (spec 2026-11-17-treefolk
    /// -grudge-design): 12 of them (3 guards: two at the wide door, one at the slit), home points round the cave floor, the
    /// hall at its centre, the root walls that close the door and the slit to a hated player, and the carved atonement stone
    /// 25 m outside the door. Run again to rebuild.
    /// </summary>
    public static class LakeTreefolk
    {
        const string DressedName = "Treefolk (Root Foot)";

        [MenuItem("Evolve/Dress Lake: Treefolk")]
        public static void Dress()
        {
            Physics.SyncTransforms();
            var old = GameObject.Find(DressedName);
            if (old != null) Object.DestroyImmediate(old);
            var root = new GameObject(DressedName).transform;
            var tribe = root.gameObject.AddComponent<TreefolkTribe>();
            tribe.TribeId = 2;   // the area's number
            tribe.DisplayName = "Người cây";

            Vector2 c = ZoneLayout.RootFoot.Center;
            float rx = ZoneLayout.CaveRadii.x, rz = ZoneLayout.CaveRadii.y;
            Vector3 At(float x, float z) => new Vector3(x, LakeGround.GroundY(x, z), z);
            tribe.CaveCenter = At(c.x, c.y);

            var hall = new GameObject("Hall").transform;
            hall.SetParent(root, false);
            hall.position = At(c.x, c.y);
            tribe.Hall = hall;

            // home points: a ring on the cave floor, plus a few just outside the door
            var homes = new List<Transform>();
            void Home(float x, float z)
            {
                var t = new GameObject("Home " + homes.Count).transform;
                t.SetParent(root, false);
                t.position = At(x, z);
                homes.Add(t);
            }
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI * 2f / 8f + 0.2f;
                Home(c.x + Mathf.Cos(a) * rx * 0.6f, c.y + Mathf.Sin(a) * rz * 0.6f);
            }
            Home(c.x + rx + 4f, c.y + 3f);
            Home(c.x + rx + 5f, c.y - 4f);
            tribe.HomePoints = homes.ToArray();

            // the band
            var bark = LakeGround.OpaqueMaterial("Lake_TreefolkBark", new Color(0.38f, 0.27f, 0.17f), 0.15f, false);
            var guardBark = LakeGround.OpaqueMaterial("Lake_TreefolkGuard", new Color(0.26f, 0.19f, 0.12f), 0.2f, false);
            var posts = new[]
            {
                At(c.x + rx + 1.5f, c.y + 2.2f), At(c.x + rx + 1.5f, c.y - 2.2f),   // the wide door (east)
                At(c.x + 1.8f, c.y + rz - 1.5f),                                  // the slit (north), inside
            };
            var rnd = new System.Random(2020);
            for (int i = 0; i < 12; i++)
            {
                bool guard = i < posts.Length;
                Vector3 pos = guard ? posts[i] : homes[i % 8].position + new Vector3((float)rnd.NextDouble() - 0.5f, 0f, (float)rnd.NextDouble() - 0.5f);
                float height = guard ? 1.45f : 1.1f + (float)rnd.NextDouble() * 0.3f;
                var go = new GameObject((guard ? "Treefolk Guard " : "Treefolk ") + i);
                go.transform.SetParent(root, false);
                go.transform.position = pos;
                go.transform.rotation = Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f);
                go.AddComponent<MeshFilter>().sharedMesh = TreefolkMesh.Build(700 + i, height);
                go.AddComponent<MeshRenderer>().sharedMaterial = guard ? guardBark : bark;
                var rb = go.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                var col = go.AddComponent<CapsuleCollider>();
                col.height = height;
                col.radius = 0.32f;
                col.center = new Vector3(0f, height * 0.5f, 0f);
                var t = go.AddComponent<Treefolk>();
                t.Kind = guard ? Treefolk.Role.Guard : Treefolk.Role.Citizen;
                t.Index = i;
                t.Post = pos;
            }

            // root walls that close the door and the slit (hidden until a hated player comes near)
            var gateMat = LakeGround.OpaqueMaterial("Lake_CanopyBark", new Color(0.24f, 0.17f, 0.11f), 0.12f, false);
            float floor = tribe.CaveCenter.y;
            tribe.Gates = new[]
            {
                Gate(root, "Door Roots", new Vector3(c.x + rx - 0.3f, floor, c.y), new Vector3(1.6f, 4.2f, 6.2f), gateMat, 11),
                Gate(root, "Slit Roots", new Vector3(c.x, floor, c.y + rz - 0.3f), new Vector3(3.6f, 2.6f, 1.6f), gateMat, 12),
            };

            // the atonement stone, out of the door guards' reach
            var stoneXZ = new Vector2(c.x + rx + 25f, c.y);
            for (int k = 0; k < 20 && ZoneLayout.DepthAt(stoneXZ.x, stoneXZ.y) > 0f; k++) stoneXZ += new Vector2(0f, 3f);
            var stone = new GameObject("Atonement Stone");
            stone.transform.SetParent(root, false);
            stone.transform.position = At(stoneXZ.x, stoneXZ.y);
            var rockMesh = GroundMeshes.Rock(2025, out var mossCap, 0.9f);
            var rock = new GameObject("Stone");
            rock.transform.SetParent(stone.transform, false);
            rock.transform.localScale = new Vector3(1.4f, 1.9f, 1.2f);
            rock.AddComponent<MeshFilter>().sharedMesh = rockMesh;
            rock.AddComponent<MeshRenderer>().sharedMaterial = LakeGround.OpaqueMaterial("Lake_Limestone", new Color(0.74f, 0.72f, 0.64f), 0.2f, false);
            rock.AddComponent<MeshCollider>().sharedMesh = rockMesh;
            // carved rings round it (the treefolk's chiselled marks)
            var blob = FloraMeshes.Blob();
            var rings = new List<CombineInstance>();
            for (int k = 0; k < 10; k++)
            {
                float a = k * Mathf.PI * 2f / 10f;
                rings.Add(new CombineInstance { mesh = blob, transform = Matrix4x4.TRS(stone.transform.position + new Vector3(Mathf.Cos(a) * 1.2f, 0.9f, Mathf.Sin(a) * 1.05f), Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f), new Vector3(0.12f, 0.35f, 0.06f)) });
            }
            LakeGround.AddCombined(stone.transform, "Carvings", rings, LakeGround.OpaqueMaterial("Lake_Amber", new Color(0.95f, 0.62f, 0.18f), 0.6f, false, new Color(0.45f, 0.24f, 0.05f)));
            stone.AddComponent<AtonementStone>().Tribe = tribe;

            EditorSceneManager.MarkAllScenesDirty();
            Debug.Log("Đã đặt 12 người cây (3 lính gác), rễ bít cửa và đá tạ lỗi ở hang Chân Rễ. Bấm Ctrl+S để lưu scene.");
        }

        /// <summary>A wall of upright roots with a solid box collider, hidden at first.</summary>
        static GameObject Gate(Transform parent, string name, Vector3 footCentre, Vector3 size, Material mat, int seed)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = footCentre + Vector3.up * size.y * 0.5f;
            var box = go.AddComponent<BoxCollider>();
            box.size = size;
            var rnd = new System.Random(seed);
            var roots = new List<CombineInstance>();
            bool alongZ = size.z > size.x;
            float span = alongZ ? size.z : size.x;
            for (float s = -span * 0.5f + 0.35f; s < span * 0.5f; s += 0.6f)
            {
                Vector3 off = alongZ ? new Vector3(0f, 0f, s) : new Vector3(s, 0f, 0f);
                var path = new[]
                {
                    footCentre + off - Vector3.up * 0.3f,
                    footCentre + off + Vector3.up * size.y * 0.5f + new Vector3((float)rnd.NextDouble() - 0.5f, 0f, (float)rnd.NextDouble() - 0.5f) * 0.3f,
                    footCentre + off + Vector3.up * (size.y + 0.2f)
                };
                for (int k = 0; k < path.Length; k++) path[k] -= go.transform.position;   // local to the gate
                roots.Add(new CombineInstance { mesh = RootMeshes.RootTube(path, 0.32f, 0.22f, seed * 100 + roots.Count, 0.25f, 7), transform = Matrix4x4.identity });
            }
            var mesh = new Mesh { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.CombineMeshes(roots.ToArray(), true, true);
            mesh.RecalculateBounds();
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            go.SetActive(false);
            return go;
        }
    }
}
