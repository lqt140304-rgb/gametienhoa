using System;
using System.Collections.Generic;
using Evolve.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Evolve.EditorTools
{
    /// <summary>
    /// The root vault (design 2026-11-18-vault-and-light): replaces the flat woven roof and the flat earth sheet of "Dress Lake: Root Canopy"
    /// with (1) a vault of many bays (high over the lake and the spore garden, low over the root cave and the mulch bank, alcoves and
    /// ridges) with holes and slits where the light falls, and (2) great ribs of twisted cable roots (Blender pieces, two levels of detail)
    /// that fan out from the Great Hall, fork into branches and twigs, and come down as feet at the rim. The canopy's rim roots are kept
    /// (the feet stand among them). The old roof and earth sheet are hidden, not deleted: "Restore Lake Canopy (undo)" brings them back.
    /// Menu: Evolve -> Dress Lake: Root Vault. Run after "Dress Lake: Root Canopy"; rerunnable.
    /// </summary>
    public static class LakeVault
    {
        const string Root = "Assets/_Project";
        const string ModelFolder = Root + "/Models/Underroot/";
        const string CanopyName = "Lake Canopy (Dressed)";
        const string OldRoof = "Roof (old lattice)";
        const string NewRoof = "Roof";
        const string EarthSheet = "Ceiling Earth";
        const string RibsName = "Vault Ribs";
        const string OldHangingRoots = "Hanging Roots";          // 28 plain smooth cylinders from the scene builder
        const string HangingName = "Vault Hanging Roots";       // their Blender replacement
        const int Seed = 11;
        const float Chunk = 64f;
        const float Cell = 4f;
        const float PieceLength = 12f;
        const float HoleRadius = 8f;
        const float SlitHalfLength = 7f;

        [MenuItem("Evolve/Dress Lake: Root Vault")]
        public static void Dress()
        {
            var canopy = GameObject.Find(CanopyName);
            if (canopy == null) { Debug.LogWarning("Chưa có " + CanopyName + " (chạy Dress Lake: Root Canopy trước): bỏ qua vòm rễ."); return; }
            PropInstaller.EnsureLandmarks();
            var prefabs = LoadPrefabs();
            if (prefabs == null) return;   // a warning was printed; nothing touched

            var parent = canopy.transform;
            // hide (not delete) the old roof and the earth sheet, once
            var old = parent.Find(NewRoof);
            if (old != null && parent.Find(OldRoof) == null && old.GetComponentInChildren<LODGroup>() != null && old.Find("Vault Shell Marker") == null)
            {
                old.name = OldRoof;
                old.gameObject.SetActive(false);
                var earth = parent.Find(EarthSheet);
                if (earth != null) earth.gameObject.SetActive(false);
            }
            DestroyChild(parent, NewRoof);
            DestroyChild(parent, RibsName);
            DestroyChild(parent, HangingName);

            Func<float, float, float> ceiling = MakeCeiling();
            var ribs = VaultLayout.Ribs(Seed, ceiling);
            var slots = VaultLayout.Slots(Seed, ribs);

            int chunks = BuildShell(parent, ceiling, slots);
            int pieces = BuildRibs(parent, ribs, prefabs);
            int hanging = ReplaceHangingCylinders(parent);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"Vòm rễ: {chunks} ô vòm, {ribs.Length} sườn rễ ({pieces} đoạn), {slots.Length} khe sáng, {hanging} rễ buông thay cho các cột trụ tròn cũ. Chạy lại Dress Lake: Dappled Sun để mái mới đổ bóng, rồi Ctrl+S.");
        }

        [MenuItem("Evolve/Restore Lake Canopy (undo)")]
        public static void Restore()
        {
            var canopy = GameObject.Find(CanopyName);
            if (canopy == null) { Debug.LogWarning("Không thấy " + CanopyName + "."); return; }
            var parent = canopy.transform;
            var old = parent.Find(OldRoof);
            if (old == null) { Debug.LogWarning("Không có mái cũ để trả lại (chưa chạy Root Vault)."); return; }
            DestroyChild(parent, NewRoof);
            DestroyChild(parent, RibsName);
            DestroyChild(parent, HangingName);
            SetOldCylinders(true);
            old.name = NewRoof;
            old.gameObject.SetActive(true);
            var earth = parent.Find(EarthSheet);
            if (earth != null) earth.gameObject.SetActive(true);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("Đã trả lớp mái cũ. Bấm Ctrl+S để lưu scene.");
        }

        // ------------------------------------------------------------------ the vault

        static int BuildShell(Transform canopy, Func<float, float, float> ceiling, Vector2[] slots)
        {
            var roof = new GameObject(NewRoof).transform;
            roof.SetParent(canopy, false);
            new GameObject("Vault Shell Marker").transform.SetParent(roof, false);
            var earth = LakeGround.OpaqueMaterial("Lake_VaultEarth", new Color(0.08f, 0.065f, 0.05f), 0.05f, true);
            float half = WorldScale.ZoneSize * 0.5f;
            int chunks = 0;
            for (float x = -half; x < half; x += Chunk)
                for (float z = -half; z < half; z += Chunk)
                {
                    var near = ShellMesh(new Rect(x, z, Chunk, Chunk), ceiling, slots, Cell);
                    var far = ShellMesh(new Rect(x, z, Chunk, Chunk), ceiling, slots, Cell * 2f);
                    if (near.vertexCount == 0) continue;
                    var go = new GameObject($"Vault {x:0},{z:0}");
                    go.transform.SetParent(roof, false);
                    var r0 = Part(go.transform, "LOD0", near, earth);
                    var r1 = Part(go.transform, "LOD1", far, earth);
                    var group = go.AddComponent<LODGroup>();
                    group.fadeMode = LODFadeMode.CrossFade;
                    group.animateCrossFading = true;
                    group.SetLODs(new[] { new LOD(0.30f, new Renderer[] { r0 }), new LOD(0.0001f, new Renderer[] { r1 }) });   // never culled: it is the view
                    group.RecalculateBounds();
                    chunks++;
                }
            return chunks;
        }

        /// <summary>The vault over one chunk as a grid facing down, with the quads over a light hole or slit left out and a little relief on the surface.</summary>
        static Mesh ShellMesh(Rect area, Func<float, float, float> ceiling, Vector2[] slots, float cell)
        {
            int n = Mathf.RoundToInt(area.width / cell);
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            var index = new int[n + 1, n + 1];
            for (int i = 0; i <= n; i++)
                for (int j = 0; j <= n; j++)
                {
                    float x = area.xMin + i * cell, z = area.yMin + j * cell;
                    float h = VaultLayout.VaultHeight(x, z, ceiling(x, z)) + 1.2f * (Mathf.PerlinNoise(x * 0.11f + 40f, z * 0.11f + 40f) - 0.5f);
                    index[i, j] = verts.Count;
                    verts.Add(new Vector3(x, h, z));
                    uvs.Add(new Vector2(x / 8f, z / 8f));
                }
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                {
                    float cx = area.xMin + (i + 0.5f) * cell, cz = area.yMin + (j + 0.5f) * cell;
                    if (InHole(cx, cz, slots, cell)) continue;
                    int a = index[i, j], b = index[i + 1, j], c = index[i, j + 1], d = index[i + 1, j + 1];
                    tris.Add(a); tris.Add(b); tris.Add(c);
                    tris.Add(b); tris.Add(d); tris.Add(c);
                }
            var mesh = new Mesh { name = "Vault", indexFormat = verts.Count > 60000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            if (tris.Count == 0) return mesh;
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        static bool InHole(float x, float z, Vector2[] slots, float cell)
        {
            foreach (var s in ZoneLayout.LightShafts)
                if (Vector2.Distance(new Vector2(x, z), s) < HoleRadius + cell * 0.4f) return true;
            for (int i = 0; i < slots.Length; i++)
            {
                float a = i * 37f * Mathf.Deg2Rad;
                var d = new Vector2(x - slots[i].x, z - slots[i].y);
                float along = d.x * Mathf.Cos(a) + d.y * Mathf.Sin(a), across = -d.x * Mathf.Sin(a) + d.y * Mathf.Cos(a);
                if (Mathf.Abs(along) < SlitHalfLength && Mathf.Abs(across) < VaultLayout.SlotHalfWidth(i) + cell * 0.5f) return true;
            }
            return false;
        }

        static MeshRenderer Part(Transform parent, string name, Mesh mesh, Material mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.On;   // the vault casts the shadows that make the patches of sun
            r.receiveShadows = false;
            go.isStatic = true;
            return r;
        }

        // ------------------------------------------------------------------ the old hanging cylinders

        static void SetOldCylinders(bool visible)
        {
            var old = GameObject.Find(OldHangingRoots);   // stays active: LakeLayout and the vines look it up by name
            if (old == null) return;
            foreach (var r in old.GetComponentsInChildren<Renderer>(true)) r.enabled = visible;
        }

        /// <summary>The scene builder hung 28 plain smooth cylinders from the dome (the black flat-ended columns). Hide them and hang a Blender
        /// root, two cables thick, from the same top point with the same length.</summary>
        static int ReplaceHangingCylinders(Transform canopy)
        {
            var old = GameObject.Find(OldHangingRoots);
            if (old == null) return 0;
            var prefabs = new List<GameObject>();
            for (int i = 0; i < 4; i++)
            {
                var p = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFolder + "HangRoot_" + i + ".fbx");
                if (p != null) prefabs.Add(p);
            }
            if (prefabs.Count == 0) return 0;   // keep the cylinders if there is nothing to replace them with
            var parent = new GameObject(HangingName).transform;
            parent.SetParent(canopy, false);
            var rnd = new System.Random(Seed + 9);
            int made = 0;
            foreach (Transform t in old.transform)
            {
                var r = t.GetComponent<Renderer>();
                if (r == null) continue;
                float len = t.lossyScale.y * 2f;                    // Unity's cylinder is 2 m tall
                Vector3 top = t.position + t.up * (len * 0.5f);
                float k = len / 10f;                                // the Blender roots are 10 m long
                for (int c = 0; c < 2; c++)
                {
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[rnd.Next(prefabs.Count)], parent);
                    go.transform.position = top + new Vector3((float)(rnd.NextDouble() - 0.5) * 1.6f, 0.4f, (float)(rnd.NextDouble() - 0.5) * 1.6f);
                    go.transform.rotation = Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f);
                    go.transform.localScale = Vector3.one * (k * (c == 0 ? 1f : 0.7f));
                    foreach (var tr in go.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(tr.gameObject, (StaticEditorFlags)(-1));
                    made++;
                }
            }
            SetOldCylinders(false);
            return made;
        }

        // ------------------------------------------------------------------ the ribs

        static GameObject[] LoadPrefabs()
        {
            var names = new List<string>();
            for (int i = 0; i < 3; i++) { names.Add("Rib_Straight_" + i); }
            for (int i = 0; i < 2; i++) { names.Add("Rib_Tip_" + i); names.Add("Rib_Foot_" + i); }   // 3 Tip0, 4 Foot0, 5 Tip1, 6 Foot1
            var list = new List<GameObject>();
            foreach (var n in names)
            {
                var p = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFolder + n + ".fbx");
                if (p == null)
                {
                    Debug.LogWarning("Không nạp được " + n + ".fbx (chạy Evolve → Reinstall Blender Props (overwrite) rồi thử lại): giữ nguyên mái cũ.");
                    return null;
                }
                list.Add(p);
            }
            return list.ToArray();
        }

        // prefabs: 0-2 straight, 3 tip0, 4 foot0, 5 tip1, 6 foot1
        static int BuildRibs(Transform canopy, RibPath[] ribs, GameObject[] prefabs)
        {
            var root = new GameObject(RibsName).transform;
            root.SetParent(canopy, false);
            var footPoints = VaultLayout.FootPoints(ribs);
            var rnd = new System.Random(Seed + 5);
            int pieces = 0;
            for (int r = 0; r < ribs.Length; r++)
            {
                var rib = ribs[r];
                var poly = rib.Points;
                float total = 0f;
                var cum = new float[poly.Length];
                for (int i = 1; i < poly.Length; i++) { total += Vector3.Distance(poly[i - 1], poly[i]); cum[i] = total; }
                if (total < 6f) continue;
                bool toRim = false;
                foreach (var f in footPoints) if (Vector2.Distance(f, new Vector2(poly[poly.Length - 1].x, poly[poly.Length - 1].z)) < 1f) toRim = true;

                var holder = new GameObject($"Rib {r} (level {rib.Level})").transform;
                holder.SetParent(root, false);
                float s = 0f;
                while (s < total - 1f)
                {
                    float remaining = total - s;
                    bool last = remaining <= PieceLength + 1f;
                    Vector3 pos = Sample(poly, cum, s);
                    Vector3 next = Sample(poly, cum, Mathf.Min(total, s + PieceLength));
                    Vector3 tangent = (next - pos);
                    if (tangent.sqrMagnitude < 0.01f) break;
                    tangent.Normalize();
                    float radius = Mathf.Lerp(rib.RadiusStart, rib.RadiusEnd, s / total);
                    GameObject prefab;
                    bool isFoot = last && toRim;
                    if (isFoot) prefab = prefabs[rnd.Next(2) == 0 ? 4 : 6];       // Foot_0 / Foot_1
                    else if (last) prefab = prefabs[rnd.Next(2) == 0 ? 3 : 5];   // Tip_0 / Tip_1
                    else prefab = prefabs[rnd.Next(3)];                           // Straight_0..2
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, holder);
                    // the last piece (a tip) is shortened to what is left of the rib, so it never reaches past its end
                    Orient(go, pos, tangent, radius, isFoot ? (pos.y - LakeGround.GroundY(pos.x, pos.z)) : 0f, last && !isFoot ? Mathf.Clamp(remaining / PieceLength, 0.3f, 1f) : 1f);
                    if (isFoot) LakeProps.EnsureCollision(go, false);
                    pieces++;
                    s += PieceLength - 0.6f;   // overlap a little so the joins are hidden
                    if (last) break;
                }
            }
            return pieces;
        }

        static Vector3 Sample(Vector3[] poly, float[] cum, float s)
        {
            for (int i = 1; i < poly.Length; i++)
                if (s <= cum[i]) return Vector3.Lerp(poly[i - 1], poly[i], (s - cum[i - 1]) / Mathf.Max(1e-3f, cum[i] - cum[i - 1]));
            return poly[poly.Length - 1];
        }

        /// <summary>Puts a rib piece at pos running along tangent: its mouth (the thick end) at pos, its length axis along the tangent, its up axis up.
        /// The FBX axes are measured from the mesh itself (which end is thicker, which axis is long) so the import's axis conversion does not matter.</summary>
        static void Orient(GameObject go, Vector3 pos, Vector3 tangent, float radius, float footDrop, float length = 1f)
        {
            DetectAxes(go, out Vector3 fwdLocal, out Vector3 upLocal, out int fwdAxis, out int upAxis);
            Vector3 up = Vector3.up;
            Vector3 fw = tangent;
            if (Mathf.Abs(Vector3.Dot(fw, up)) > 0.95f) up = Vector3.forward;
            var from = Quaternion.LookRotation(fwdLocal, upLocal);
            var to = Quaternion.LookRotation(fw, Vector3.Cross(Vector3.Cross(fw, up), fw).normalized);
            go.transform.rotation = to * Quaternion.Inverse(from);
            go.transform.position = pos;
            var scale = Vector3.one * radius;
            scale[fwdAxis] = length;                                     // the length stays 12 m (shorter for the last piece), only the thickness follows the rib
            if (footDrop > 0f) scale[upAxis] = Mathf.Max(radius, footDrop / 7.2f);   // a foot reaches down to the ground
            go.transform.localScale = scale;
        }

        static void DetectAxes(GameObject go, out Vector3 fwd, out Vector3 up, out int fwdAxis, out int upAxis)
        {
            var mf = go.GetComponentInChildren<MeshFilter>();
            fwd = Vector3.right; up = Vector3.up; fwdAxis = 0; upAxis = 1;
            if (mf == null || mf.sharedMesh == null) return;
            var verts = mf.sharedMesh.vertices;
            var tr = mf.transform;
            Vector3 lo = Vector3.one * float.MaxValue, hi = Vector3.one * float.MinValue;
            var pts = new List<Vector3>(verts.Length / 6 + 1);
            for (int i = 0; i < verts.Length; i += 6)
            {
                var p = go.transform.InverseTransformPoint(tr.TransformPoint(verts[i]));
                pts.Add(p);
                lo = Vector3.Min(lo, p); hi = Vector3.Max(hi, p);
            }
            var ext = hi - lo;
            fwdAxis = ext.x >= ext.y && ext.x >= ext.z ? 0 : ext.y >= ext.z ? 1 : 2;
            // the pivot is the mouth (the piece starts at its origin and runs away from it): the mesh lies on the exit side of the origin
            float meanAlong = 0f;
            foreach (var p in pts) meanAlong += p[fwdAxis];
            meanAlong /= Mathf.Max(1, pts.Count);
            float sign = meanAlong >= 0f ? 1f : -1f;
            fwd = Vector3.zero; fwd[fwdAxis] = sign;
            upAxis = fwdAxis == 1 ? 2 : 1;       // Blender's up axis comes out as Y in Unity
            up = Vector3.zero; up[upAxis] = 1f;
        }

        // ------------------------------------------------------------------ helpers

        static Func<float, float, float> MakeCeiling()
        {
            var dome = GameObject.Find("Dome");
            var r = dome != null ? dome.GetComponent<Renderer>() : null;
            if (r == null) return (x, z) => 30f;
            Bounds b = r.bounds;
            float radius = Mathf.Max(1f, b.extents.x);
            Vector2 centre = new Vector2(b.center.x, b.center.z);
            float minY = b.min.y, sizeY = b.size.y;
            return (x, z) =>
            {
                float d = Vector2.Distance(new Vector2(x, z), centre) / radius;
                return minY + sizeY * Mathf.Sqrt(Mathf.Max(0f, 1f - d * d));
            };
        }

        static void DestroyChild(Transform parent, string name)
        {
            var c = parent.Find(name);
            if (c != null) UnityEngine.Object.DestroyImmediate(c.gameObject);
        }
    }
}
