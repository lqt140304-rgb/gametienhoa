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

            EditorSceneManager.MarkAllScenesDirty();
            AssetDatabase.SaveAssets();
            Debug.Log("Đã dựng Đại Sảnh người cây (8 cột rễ, bục, ngai, lò hổ phách) và " + CityLayout.Houses().Length + " nhà quanh sảnh (3 nhà là chỗ trú cho slime). Bấm Ctrl+S để lưu scene.");
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
                float r = h.Radius, height = r * 1.35f;
                var shell = CaveMeshes.Shell(r, r * 0.92f, height + 0.3f, 28, 9, 9100 + i, 0.1f);
                // the door: 1.1 m wide, 1.4 m high, on the local +x side (turned toward the hall)
                WaterMeshes.RemoveTriangles(shell, v => v.x > 0f && Mathf.Abs(v.z) < 0.55f && v.y < 1.7f);
                var go = new GameObject("House " + i);
                go.transform.SetParent(parent, false);
                go.transform.position = new Vector3(h.Position.x, ground - 0.3f, h.Position.y);
                go.transform.rotation = Quaternion.Euler(0f, h.Yaw, 0f);
                go.AddComponent<MeshFilter>().sharedMesh = shell;
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = earth;
                mr.shadowCastingMode = ShadowCastingMode.TwoSided;
                go.AddComponent<MeshCollider>().sharedMesh = shell;
                go.isStatic = true;

                // roots wrapping the hut, a door frame, and a lamp beside the door
                var rnd = new System.Random(9200 + i);
                for (int k = 0; k < 4; k++)
                {
                    float a0 = k * Mathf.PI * 2f / 4f + 0.4f;
                    if (Mathf.Abs(Mathf.DeltaAngle(a0 * Mathf.Rad2Deg, 0f)) < 40f) continue;    // not across the door
                    var path = new Vector3[8];
                    for (int s = 0; s < path.Length; s++)
                    {
                        float t = s / (float)(path.Length - 1) * Mathf.PI * 0.5f;
                        Vector3 local = new Vector3(Mathf.Cos(a0) * r * 1.03f * Mathf.Cos(t), height * 1.02f * Mathf.Sin(t) - 0.25f, Mathf.Sin(a0) * r * 0.95f * Mathf.Cos(t));
                        path[s] = go.transform.TransformPoint(local);
                    }
                    roots.Add(new CombineInstance { mesh = RootMeshes.RootTube(path, 0.28f, 0.14f, 9300 + i * 4 + k, 0.25f, 7), transform = Matrix4x4.identity });
                }
                Vector3 doorTop = go.transform.TransformPoint(new Vector3(r, 1.5f, 0f));
                Vector3 doorL = go.transform.TransformPoint(new Vector3(r - 0.1f, -0.2f, -0.65f)), doorR = go.transform.TransformPoint(new Vector3(r - 0.1f, -0.2f, 0.65f));
                roots.Add(new CombineInstance { mesh = RootMeshes.RootTube(new[] { doorL, go.transform.TransformPoint(new Vector3(r + 0.02f, 1.1f, -0.7f)), doorTop, go.transform.TransformPoint(new Vector3(r + 0.02f, 1.1f, 0.7f)), doorR }, 0.16f, 0.14f, 9400 + i, 0.1f, 7), transform = Matrix4x4.identity });
                lamps.Add(new CombineInstance { mesh = blob, transform = Matrix4x4.TRS(go.transform.TransformPoint(new Vector3(r + 0.25f, 1.6f, 0.95f)), Quaternion.identity, new Vector3(0.22f, 0.3f, 0.22f)) });

                if (!h.Shelter) continue;
                // this hut shelters a slime: a trigger over its floor (the door lets a slime of body scale up to 1.4 in)
                var zoneGo = new GameObject("Shelter Zone");
                zoneGo.transform.SetParent(go.transform, false);
                zoneGo.transform.localPosition = new Vector3(-0.3f, 1.0f + 0.3f, 0f);
                var col = zoneGo.AddComponent<BoxCollider>();
                col.isTrigger = true;
                col.size = new Vector3(r * 1.3f, 1.4f, r * 1.3f);
                zoneGo.AddComponent<ShelterZone>().MaxBodyScale = 1.4f;
            }
            LakeGround.AddCombined(parent, "House Roots", roots, bark);
            LakeGround.AddCombined(parent, "House Lamps", lamps, amber);
        }
    }
}
