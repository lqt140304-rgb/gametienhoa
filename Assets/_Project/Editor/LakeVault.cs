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
        const string RimName = "Vault Rim Roots";               // Blender roots that replace the straight code-built rim roots
        const string CollarName = "Vault Hole Collars";         // soft earthy lips round the light holes (hide the stepped edge of the shell)
        const string PropsName = "Vault Hanging Props";          // long roots, curtains, glow pods, stalactites by zone
        const string PillarsName = "Vault Pillars";              // Blender replacements for the three smooth "Landmark" columns
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
            DestroyChild(parent, PillarsName);
            DestroyChild(parent, PropsName);
            DestroyChild(parent, RimName);
            DestroyChild(parent, CollarName);

            Func<float, float, float> ceiling = MakeCeiling();
            var ribs = VaultLayout.Ribs(Seed, ceiling);
            var slots = VaultLayout.Slots(Seed, ribs);

            int chunks = BuildShell(parent, ceiling, slots);
            int pieces = BuildRibs(parent, ribs, prefabs);
            int hanging = ReplaceHangingCylinders(parent);
            hanging += ReplacePillars(parent);
            int props = PlaceHangingProps(parent, ribs, slots, ceiling);
            props += PlaceBones(parent, ribs, slots, ceiling);
            int rim = ReplaceRimRoots(parent, ceiling);
            BuildHoleCollars(parent, ceiling);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"Vòm rễ: {chunks} ô vòm, {ribs.Length} sườn rễ ({pieces} đoạn), {slots.Length} khe sáng, {hanging} rễ buông thay cho các cột trụ tròn cũ, {props} vật treo theo khu (rễ dài, màn rễ, túi sáng, nhũ đá), {rim} rễ rìa Blender thay rễ thẳng cũ. Chạy lại Dress Lake: Dappled Sun để mái mới đổ bóng, rồi Ctrl+S.");
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
            DestroyChild(parent, PillarsName);
            DestroyChild(parent, PropsName);
            DestroyChild(parent, RimName);
            DestroyChild(parent, CollarName);
            SetRim(true);
            SetOldCylinders(true);
            SetPillars(true);
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

        // ------------------------------------------------------------------ soft lips round the holes

        /// <summary>The shell's holes are cut cell by cell, which leaves a stepped, square-cornered edge. Round each hole with a thick, slightly lumpy earthy lip
        /// (a tube bent into a ring that follows the roof height), so the opening reads as a smooth round mouth.</summary>
        static void BuildHoleCollars(Transform canopy, Func<float, float, float> ceiling)
        {
            var parent = new GameObject(CollarName).transform;
            parent.SetParent(canopy, false);
            var earth = LakeGround.OpaqueMaterial("Lake_VaultEarth", new Color(0.08f, 0.065f, 0.05f), 0.05f, true);
            const int ring = 72, around = 10;
            int index = 0;
            foreach (var sh in ZoneLayout.LightShafts)
            {
                var verts = new List<Vector3>();
                var tris = new List<int>();
                float seed = 17f * (++index);
                for (int i = 0; i < ring; i++)
                {
                    float a = i * Mathf.PI * 2f / ring;
                    float wob = (Mathf.PerlinNoise(Mathf.Cos(a) * 1.7f + seed, Mathf.Sin(a) * 1.7f + seed) - 0.5f) * 1.0f;   // the ring itself wanders a little
                    float r = HoleRadius + 0.3f + wob;
                    float x = sh.x + Mathf.Cos(a) * r, z = sh.y + Mathf.Sin(a) * r;
                    float y = VaultLayout.VaultHeight(x, z, ceiling(x, z)) + 0.5f;
                    float tube = 1.9f + 0.7f * (Mathf.PerlinNoise(Mathf.Cos(a) * 3.1f + seed + 40f, Mathf.Sin(a) * 3.1f + seed + 40f) - 0.5f) * 2f;
                    for (int k = 0; k < around; k++)
                    {
                        float b = k * Mathf.PI * 2f / around;
                        float radial = Mathf.Cos(b) * tube, vertical = Mathf.Sin(b) * tube * 0.9f;
                        verts.Add(new Vector3(x + Mathf.Cos(a) * radial, y + vertical, z + Mathf.Sin(a) * radial));
                    }
                }
                for (int i = 0; i < ring; i++)
                    for (int k = 0; k < around; k++)
                    {
                        int a0 = i * around + k, a1 = i * around + (k + 1) % around;
                        int b0 = ((i + 1) % ring) * around + k, b1 = ((i + 1) % ring) * around + (k + 1) % around;
                        tris.Add(a0); tris.Add(b0); tris.Add(a1);
                        tris.Add(a1); tris.Add(b0); tris.Add(b1);
                    }
                var mesh = new Mesh { name = "Hole Collar", indexFormat = IndexFormat.UInt16 };
                mesh.SetVertices(verts); mesh.SetTriangles(tris, 0);
                mesh.RecalculateNormals(); mesh.RecalculateBounds();
                var go = new GameObject("Hole Collar " + index);
                go.transform.SetParent(parent, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = earth;
                mr.shadowCastingMode = ShadowCastingMode.On;
            }
        }

        // ------------------------------------------------------------------ the rim roots

        static void SetRim(bool visible)
        {
            var rim = GameObject.Find("Rim Roots");   // the code-built straight roots (LakeCanopy); only their renderers go, the object stays
            if (rim == null) return;
            foreach (var r in rim.GetComponentsInChildren<Renderer>(true)) r.enabled = visible;
        }

        /// <summary>Hide the straight, smooth rim roots and hang Blender roots (thick at the roof, tapering, with side roots) from the vault down the wall
        /// in three rows, each stretched to reach the ground, in varied widths and leans. The hunter's way in on the north side stays open.</summary>
        static int ReplaceRimRoots(Transform canopy, Func<float, float, float> ceiling)
        {
            var prefabs = new List<GameObject>();
            for (int i = 0; i < 4; i++) { var p = Load("HangRoot_" + i); if (p != null) prefabs.Add(p); }
            if (prefabs.Count == 0 || GameObject.Find("Rim Roots") == null) return 0;
            var parent = new GameObject(RimName).transform;
            parent.SetParent(canopy, false);
            var rnd = new System.Random(Seed + 131);
            float[] rows = { 206f, 220f, 234f };
            float[] spacing = { 17f, 14f, 12f };
            int made = 0;
            for (int row = 0; row < rows.Length; row++)
            {
                float d = rows[row];
                for (float s = -d; s < d; s += spacing[row])
                    for (int side = 0; side < 4; side++)
                    {
                        float along = s + ((float)rnd.NextDouble() - 0.5f) * spacing[row] * 0.7f;
                        Vector2 p = side == 0 ? new Vector2(along, d) : side == 1 ? new Vector2(along, -d) : side == 2 ? new Vector2(d, along) : new Vector2(-d, along);
                        if (side == 0 && Mathf.Abs(p.x - ZoneLayout.NorthEntry.x) < 20f) continue;
                        float roof = VaultLayout.VaultHeight(p.x, p.y, ceiling(p.x, p.y));
                        float ground = LakeGround.GroundY(p.x, p.y) - 0.6f;
                        float h = roof - ground;
                        if (h < 4f) continue;
                        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[rnd.Next(prefabs.Count)], parent);
                        go.transform.position = new Vector3(p.x, roof + 2.2f, p.y);   // its flat top sits well inside the roof shell, never under it
                        go.transform.rotation = Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f);
                        float w = (row == 0 ? 2.2f : row == 1 ? 2.8f : 3.4f) * (0.7f + 0.6f * (float)rnd.NextDouble());
                        go.transform.localScale = new Vector3(w, (h + 2.2f) / 10f, w);
                        foreach (var tr in go.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(tr.gameObject, (StaticEditorFlags)(-1));
                        made++;
                    }
            }
            SetRim(false);
            return made;
        }

        // ------------------------------------------------------------------ hanging props by zone

        static GameObject Load(string name) => AssetDatabase.LoadAssetAtPath<GameObject>(ModelFolder + name + ".fbx");

        /// <summary>Things hanging from the vault, by zone: 01 turquoise pods over the lake; 02 thick old roots; 03 root curtains; 04 purple spore pods;
        /// 05 stalactites; 06 sparse dry roots. Roots come in three lengths so they form layers. Clear of the hall, the light holes and slits, and the ribs. No collision.</summary>
        static int PlaceHangingProps(Transform canopy, RibPath[] ribs, Vector2[] slots, Func<float, float, float> ceiling)
        {
            var parent = new GameObject(PropsName).transform;
            parent.SetParent(canopy, false);
            var ribPoints = new List<Vector2>();
            foreach (var r in ribs) foreach (var q in r.Points) ribPoints.Add(new Vector2(q.x, q.z));
            var placed = new List<Vector2>();
            var rnd = new System.Random(Seed + 53);
            // per zone: (model prefix, variants, weight); the first entries are the zone's character
            var kinds = new (string prefix, int variants, float w)[][]
            {
                new[] { ("VaultPod", 3, 5f), ("VaultRoot", 2, 1f) },                                  // 01
                new[] { ("VaultRoot", 4, 5f) },                                                      // 02 (long ones favoured below)
                new[] { ("VaultCurtain", 2, 3f), ("VaultRoot", 3, 2f) },                             // 03
                new[] { ("VaultPod", 3, 4f), ("VaultRoot", 2, 1f) },                                  // 04
                new[] { ("VaultStalactite", 3, 5f), ("VaultRoot", 2, 1f) },                           // 05
                new[] { ("VaultRoot", 2, 1f) },                                                      // 06 sparse
            };
            float[] density = { 1f, 1.1f, 1f, 1f, 1f, 0.35f };
            Color[] glow = { new Color(0.25f, 0.95f, 0.8f), default, default, new Color(0.7f, 0.35f, 1f), default, default };
            int made = 0, lights = 0;
            for (int attempt = 0; attempt < 6000 && made < 170; attempt++)
            {
                float x = ((float)rnd.NextDouble() - 0.5f) * 2f * 228f, z = ((float)rnd.NextDouble() - 0.5f) * 2f * 228f;
                if (ZoneLayout.InsideCave(x, z, 9f)) continue;
                var p = new Vector2(x, z);
                int zone = 0; float best = -1f;
                for (int i = 0; i < ZoneLayout.Zones.Length; i++) { float v = ZoneLayout.Influence(i, x, z); if (v > best) { best = v; zone = i; } }
                if (rnd.NextDouble() > density[zone]) continue;
                bool clear = true;
                foreach (var sh in ZoneLayout.LightShafts) if (Vector2.Distance(p, sh) < 11f) { clear = false; break; }
                if (clear) foreach (var sl in slots) if (Vector2.Distance(p, sl) < 9f) { clear = false; break; }
                if (clear) foreach (var q in ribPoints) if (Vector2.Distance(p, q) < 6f) { clear = false; break; }
                if (clear) foreach (var o in placed) if (Vector2.Distance(p, o) < 6f) { clear = false; break; }
                if (!clear) continue;

                var set = kinds[zone];
                float total = 0f; foreach (var k in set) total += k.w;
                float pick = (float)rnd.NextDouble() * total;
                var kind = set[0];
                foreach (var k in set) { if (pick < k.w) { kind = k; break; } pick -= k.w; }
                int variant = rnd.Next(kind.variants);
                if (kind.prefix == "VaultRoot")
                {
                    if (zone == 1) variant = 1 + rnd.Next(3);   // the old-root core hangs the longest: 8, 14 or 20 m
                }
                var prefab = Load(kind.prefix + "_" + variant);
                if (prefab == null) continue;
                float roof = VaultLayout.VaultHeight(x, z, ceiling(x, z));
                float ground = LakeGround.GroundY(x, z);
                if (roof - ground < 8f) continue;
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                go.transform.position = new Vector3(x, roof + 0.3f, z);
                go.transform.rotation = Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f);
                float k2 = 0.85f + 0.4f * (float)rnd.NextDouble();
                if (kind.prefix == "VaultRoot" || kind.prefix == "VaultStalactite") k2 = Mathf.Min(k2, (roof - ground - 4f) / (kind.prefix == "VaultRoot" ? new[] { 4f, 8f, 14f, 20f }[variant] : new[] { 3f, 4.5f, 6f }[variant]));
                go.transform.localScale = Vector3.one * Mathf.Max(0.4f, k2);
                foreach (var tr in go.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(tr.gameObject, (StaticEditorFlags)(-1));
                if (kind.prefix == "VaultStalactite" && rnd.NextDouble() < 0.6)
                {
                    var tip = new GameObject("Drip Point");
                    tip.transform.SetParent(go.transform, false);
                    tip.transform.position = go.transform.position + Vector3.down * (new[] { 3f, 4.5f, 6f }[variant] * go.transform.localScale.y * 0.95f);
                    tip.AddComponent<VaultLifeFx>().Effect = VaultLifeFx.Kind.Drip;
                }
                if (kind.prefix == "VaultPod" && glow[zone].a > 0f)
                {
                    var sp = new GameObject("Spore Point");
                    sp.transform.SetParent(go.transform, false);
                    sp.transform.position = go.transform.position + Vector3.down * 3.8f * go.transform.localScale.y;
                    var fx = sp.AddComponent<VaultLifeFx>();
                    fx.Effect = VaultLifeFx.Kind.Spores;
                    fx.Tint = glow[zone];
                }
                if (kind.prefix == "VaultPod" && lights < 14 && glow[zone].a > 0f)
                {
                    var lg = new GameObject("Pod Light");
                    lg.transform.SetParent(go.transform, false);
                    lg.transform.position = go.transform.position + Vector3.down * 3.6f * go.transform.localScale.y;
                    var l = lg.AddComponent<Light>();
                    l.type = LightType.Point; l.color = glow[zone]; l.range = 9f; l.intensity = 1.6f; l.shadows = LightShadows.None;
                    var lv = lg.AddComponent<LightByVision>();
                    lv.Pulse = 0.25f;
                    lights++;
                }
                placed.Add(p);
                made++;
            }
            Debug.Log($"Vật treo trên vòm: {made}, {lights} đèn túi.");
            return made;
        }

        /// <summary>A few dragon skulls and ribcages held up in the dark by root cables, far apart, high above the ground, clear of the hall, light holes, slits and ribs.
        /// Teal eye lights (at most 6, dark without eyes).</summary>
        static int PlaceBones(Transform canopy, RibPath[] ribs, Vector2[] slots, Func<float, float, float> ceiling)
        {
            var parent = new GameObject("Dragon Bones").transform;
            parent.SetParent(canopy.Find(PropsName), false);
            var ribPoints = new List<Vector2>();
            foreach (var r in ribs) foreach (var q in r.Points) ribPoints.Add(new Vector2(q.x, q.z));
            var placed = new List<Vector2>();
            var rnd = new System.Random(Seed + 97);
            string[] names = { "DragonSkull_0", "DragonSkull_1", "DragonRibcage_0", "DragonSkull_1", "DragonSkull_0", "DragonRibcage_0" };
            int made = 0, lights = 0;
            for (int attempt = 0; attempt < 4000 && made < 8; attempt++)
            {
                float x = ((float)rnd.NextDouble() - 0.5f) * 2f * 200f, z = ((float)rnd.NextDouble() - 0.5f) * 2f * 200f;
                var p = new Vector2(x, z);
                if (ZoneLayout.InsideCave(x, z, 14f)) continue;
                bool clear = true;
                foreach (var sh in ZoneLayout.LightShafts) if (Vector2.Distance(p, sh) < 16f) { clear = false; break; }
                if (clear) foreach (var sl in slots) if (Vector2.Distance(p, sl) < 14f) { clear = false; break; }
                if (clear) foreach (var q in ribPoints) if (Vector2.Distance(p, q) < 10f) { clear = false; break; }
                if (clear) foreach (var o in placed) if (Vector2.Distance(p, o) < 45f) { clear = false; break; }
                if (!clear) continue;
                float roof = VaultLayout.VaultHeight(x, z, ceiling(x, z));
                float ground = LakeGround.GroundY(x, z);
                if (roof - ground < 17f) continue;   // the bones hang 9 m below the anchor: keep 8 m clear above the ground
                string name = names[made % names.Length];
                var prefab = Load(name);
                if (prefab == null) continue;
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                go.transform.position = new Vector3(x, roof + 0.3f, z);
                go.transform.rotation = Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f);
                foreach (var tr in go.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(tr.gameObject, (StaticEditorFlags)(-1));
                if (name.StartsWith("DragonSkull") && lights < 6)
                {
                    var lg = new GameObject("Eye Light");
                    lg.transform.SetParent(go.transform, false);
                    lg.transform.position = go.transform.position + go.transform.right * 1f + Vector3.down * 7.3f;
                    var l = lg.AddComponent<Light>();
                    l.type = LightType.Point; l.color = new Color(0.35f, 1f, 0.85f); l.range = 12f; l.intensity = 2f; l.shadows = LightShadows.None;
                    lg.AddComponent<LightByVision>().Pulse = 0.3f;
                    lights++;
                }
                placed.Add(p);
                made++;
            }
            Debug.Log($"Xương rồng treo: {made} ({lights} đèn mắt).");
            return made;
        }

        // ------------------------------------------------------------------ the three landmark pillars

        static IEnumerable<Transform> OldPillars()
        {
            var lm = GameObject.Find("Landmarks");
            if (lm == null) yield break;
            foreach (Transform t in lm.transform) if (t.name.StartsWith("Landmark ")) yield return t;
        }

        /// <summary>Show or hide the smooth cylinders. Hidden ones stay as invisible colliders, narrowed to the width of the trunk that replaces them.</summary>
        static void SetPillars(bool visible)
        {
            foreach (var t in OldPillars())
            {
                foreach (var r in t.GetComponentsInChildren<Renderer>(true)) r.enabled = visible;
                var sc = t.localScale; t.localScale = new Vector3(visible ? 7f : 4f, sc.y, visible ? 7f : 4f);
            }
        }

        /// <summary>Each landmark pillar becomes a trunk: one Blender root hung from the dome (thick at the top) and one standing from the ground
        /// (thick at the foot), both stretched to meet in the middle.</summary>
        static int ReplacePillars(Transform canopy)
        {
            var prefabs = new List<GameObject>();
            for (int i = 0; i < 4; i++)
            {
                var p = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFolder + "HangRoot_" + i + ".fbx");
                if (p != null) prefabs.Add(p);
            }
            if (prefabs.Count == 0) return 0;
            var parent = new GameObject(PillarsName).transform;
            parent.SetParent(canopy, false);
            var rnd = new System.Random(Seed + 31);
            int made = 0;
            foreach (var t in OldPillars())
            {
                float x = t.position.x, z = t.position.z;
                float ground = LakeGround.GroundY(x, z) - 1f;
                float top = LakeLayout.CeilingY(x, z, 28f) + 0.5f;
                float h = Mathf.Max(8f, top - ground);
                for (int c = 0; c < 4; c++)
                {
                    bool fromTop = c % 2 == 0;
                    float a = c * 1.57f + (float)rnd.NextDouble();
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[rnd.Next(prefabs.Count)], parent);
                    go.transform.position = new Vector3(x + Mathf.Cos(a) * 0.7f, fromTop ? top : ground, z + Mathf.Sin(a) * 0.7f);
                    var yaw = Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f);
                    go.transform.rotation = fromTop ? yaw : yaw * Quaternion.Euler(180f, 0f, 0f);   // the root hangs down from its origin; turn it over to stand
                    float w = 2.6f - 0.4f * (c / 2);
                    go.transform.localScale = new Vector3(w, h * 0.65f / 10f, w);
                    foreach (var tr in go.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(tr.gameObject, (StaticEditorFlags)(-1));
                    made++;
                }
            }
            SetPillars(false);
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
