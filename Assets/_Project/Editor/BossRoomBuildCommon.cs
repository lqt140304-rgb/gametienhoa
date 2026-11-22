using System;
using System.Collections.Generic;
using System.IO;
using Evolve.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Evolve.EditorTools
{
    internal sealed class BossRoomSpec
    {
        public string RoomName;
        public string ModelFbxPath;
        public string PrefabPath;
        public string ScenePath;
        public string ProfileAssetPath;
        public string RockMatName;
        public string GlowMatName;
        public Vector3 BossWaitLocal;
        public Color AmbientColor;
        public Color FogColor;
        public Color DomeLightColor;
        public Color AccentLightColor;
        public Color BossLightColor;
        public float MinCeilingRequired;
        public bool RequiresAxeAnchor;
        public bool RequiresOpenSkyAboveCenter;
        public string[] NonColliderObjectNames;
        public string[] BossPrefabPaths;
    }

    /// <summary>
    /// Hạ tầng dựng, kiểm tra hợp đồng và chụp ảnh Unity cho cả 3 phòng trùm
    /// (Golem Thạch Anh, Chim Hang Mẹ, Bọ Cạp Thạch Anh).
    /// </summary>
    internal static class BossRoomBuildCommon
    {
        internal const string Root = "Assets/_Project";
        internal const string ModelFolder = Root + "/Models/Underroot/";
        internal const string SteelDoorFbxPath = ModelFolder + "BossRoom_SteelDoor.fbx";
        internal const string PrefabFolder = Root + "/Prefabs/BossRooms";

        static readonly Vector3 PlayerEntryLocal = new Vector3(0f, 0f, -26.0f);
        static readonly Vector3 DoorMouthLocal = new Vector3(0f, 0f, -31.5f);
        static readonly Vector3 DoorOpenLocal = new Vector3(0f, 11.2f, -31.5f);
        static readonly Vector3 DoorClosedLocal = new Vector3(0f, 3.6f, -31.5f);

        internal static bool BuildRoom(BossRoomSpec spec, bool interactiveConfirm, Action<GameObject, Material, Material> onRoomExtras)
        {
            var roomFbx = AssetDatabase.LoadAssetAtPath<GameObject>(spec.ModelFbxPath);
            var doorFbx = AssetDatabase.LoadAssetAtPath<GameObject>(SteelDoorFbxPath);
            if (roomFbx == null || doorFbx == null)
            {
                Debug.LogError($"[{spec.RoomName}] Thiếu FBX: {spec.ModelFbxPath} hoặc {SteelDoorFbxPath}");
                return false;
            }

            if (interactiveConfirm &&
                AssetDatabase.LoadAssetAtPath<SceneAsset>(spec.ScenePath) != null &&
                !EditorUtility.DisplayDialog(
                    spec.RoomName,
                    $"Scene {spec.RoomName} đã tồn tại. Dựng lại từ đầu? (thay đổi thủ công trong scene sẽ mất)",
                    "Dựng lại",
                    "Huỷ"))
            {
                return false;
            }
            if (interactiveConfirm && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return false;

            PrototypeSceneBuilder.EnsureFolder(Root + "/Scenes");
            PrototypeSceneBuilder.EnsureFolder(Root + "/Materials");
            PrototypeSceneBuilder.EnsureFolder(Root + "/Settings");
            PrototypeSceneBuilder.EnsureFolder(PrefabFolder);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var shared = PrototypeSceneBuilder.CreateSharedAssets();

            var rockMat = UpsertVertexMaterial(spec.RockMatName, Color.white, fill: 0.38f, glow: 0f);
            var glowMat = UpsertVertexMaterial(spec.GlowMatName, new Color(1.0f, 0.96f, 0.90f), fill: 0.18f, glow: 1.05f);
            var steelMat = UpsertVertexMaterial("BossRoom_Shared_SteelDoor", Color.white, fill: 0.36f, glow: 0f);

            var roomRoot = AssembleHierarchy(spec, roomFbx, doorFbx, rockMat, glowMat, steelMat);
            onRoomExtras?.Invoke(roomRoot, rockMat, glowMat);

            PrefabUtility.SaveAsPrefabAssetAndConnect(roomRoot, spec.PrefabPath, InteractionMode.AutomatedAction);
            TryPlaceBossInScene(roomRoot, spec);
            SetupSceneEnvironmentAndPlayer(spec, shared, roomRoot);

            EditorSceneManager.SaveScene(scene, spec.ScenePath);
            AssetDatabase.SaveAssets();

            bool valid = ValidateContract(roomRoot, spec, out string report);
            Selection.activeGameObject = roomRoot;
            if (valid)
                Debug.Log($"[{spec.RoomName}] Đã dựng {spec.ScenePath} và {spec.PrefabPath}.\n{report}");
            else
                Debug.LogError($"[{spec.RoomName}] Lỗi hợp đồng sau khi dựng:\n{report}");
            return valid;
        }

        static GameObject AssembleHierarchy(
            BossRoomSpec spec,
            GameObject roomFbx,
            GameObject doorFbx,
            Material rockMat,
            Material glowMat,
            Material steelMat)
        {
            var root = new GameObject(spec.RoomName);
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            // 1. Mô hình phòng từ Blender (xoay 180° quanh Y để khớp 1:1 (X, Z, Y)_unity)
            var archGo = (GameObject)PrefabUtility.InstantiatePrefab(roomFbx, root.transform);
            archGo.name = "Architecture";
            archGo.transform.localPosition = Vector3.zero;
            archGo.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            archGo.transform.localScale = Vector3.one;
            PrefabUtility.UnpackPrefabInstance(archGo, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

            var reparentList = new List<Transform>();
            foreach (Transform child in archGo.transform)
            {
                if (IsContractPropName(child.name))
                    reparentList.Add(child);
            }
            foreach (var prop in reparentList)
                prop.SetParent(root.transform, worldPositionStays: true);

            ConfigureArchitecture(archGo, spec, rockMat, glowMat, steelMat);
            ConfigureProps(root.transform, rockMat, glowMat, steelMat);

            // 2. Điểm mốc hợp đồng
            CreateMarker(root.transform, "Player_Entry", PlayerEntryLocal, Quaternion.identity);
            CreateMarker(root.transform, "Boss_Wait", spec.BossWaitLocal, Quaternion.Euler(0f, 180f, 0f));
            CreateMarker(root.transform, "Door_Mouth", DoorMouthLocal, Quaternion.identity);
            CreateMarker(root.transform, "Door_Open_Pos", DoorOpenLocal, Quaternion.identity);
            CreateMarker(root.transform, "Door_Closed_Pos", DoorClosedLocal, Quaternion.identity);

            // 3. Cánh cửa thép cũ (không đặt static để BossRoom trượt mượt giữa Open/Closed)
            var doorGo = (GameObject)PrefabUtility.InstantiatePrefab(doorFbx, root.transform);
            doorGo.name = "Door_Steel";
            doorGo.transform.localPosition = DoorOpenLocal;
            doorGo.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            doorGo.transform.localScale = Vector3.one;
            PrefabUtility.UnpackPrefabInstance(doorGo, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            foreach (var tr in doorGo.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.SetStaticEditorFlags(tr.gameObject, 0);
            foreach (var col in doorGo.GetComponentsInChildren<Collider>(true))
                UnityEngine.Object.DestroyImmediate(col);
            foreach (var r in doorGo.GetComponentsInChildren<Renderer>(true))
                r.sharedMaterial = steelMat;
            var doorCol = doorGo.AddComponent<BoxCollider>();
            doorCol.isTrigger = false;
            doorCol.center = Vector3.zero;
            doorCol.size = new Vector3(5.4f, 7.6f, 0.9f);

            // 4. Activate_Zone & Room_Bounds
            var activateGo = new GameObject("Activate_Zone");
            activateGo.transform.SetParent(root.transform, false);
            activateGo.transform.localPosition = new Vector3(0f, 15.0f, 3.0f); // y in [-2, +32] ôm trọn sàn; z in [-23, +29], cách cửa 8.5 m (> 6 m)
            var activateCol = activateGo.AddComponent<BoxCollider>();
            activateCol.isTrigger = true;
            activateCol.size = new Vector3(60f, 34f, 52f);

            var boundsGo = new GameObject("Room_Bounds");
            boundsGo.transform.SetParent(root.transform, false);
            boundsGo.transform.localPosition = new Vector3(0f, 20.0f, -1.5f);
            var boundsCol = boundsGo.AddComponent<BoxCollider>();
            boundsCol.isTrigger = true;
            boundsCol.size = new Vector3(68f, 42f, 74f);

            // 5. Đèn trong phòng
            BuildRoomLights(root.transform, spec);

            // 6. Gắn script BossRoom lên gốc phòng theo docs/BOSS_ROOM_CONTRACT.md (nối bằng tên GameObject con)
            if (root.GetComponent<BossRoom>() == null)
                root.AddComponent<BossRoom>();
            return root;
        }

        static void ConfigureArchitecture(GameObject archGo, BossRoomSpec spec, Material rockMat, Material glowMat, Material steelMat)
        {
            var nonCollider = new HashSet<string>(spec.NonColliderObjectNames ?? Array.Empty<string>());
            foreach (var tr in archGo.GetComponentsInChildren<Transform>(true))
            {
                string n = tr.gameObject.name;
                var mf = tr.GetComponent<MeshFilter>();
                var mr = tr.GetComponent<MeshRenderer>();

                if (n.EndsWith("_COL"))
                {
                    if (mr != null) UnityEngine.Object.DestroyImmediate(mr);
                    if (mf != null && mf.sharedMesh != null)
                    {
                        var mc = tr.gameObject.GetComponent<MeshCollider>() ?? tr.gameObject.AddComponent<MeshCollider>();
                        mc.sharedMesh = mf.sharedMesh;
                        mc.convex = false;
                    }
                    continue;
                }

                if (mr != null)
                {
                    if (n.Contains("Glow")) mr.sharedMaterial = glowMat;
                    else if (n.Contains("Steel") || n.Contains("Rail") || n.Contains("Chain")) mr.sharedMaterial = steelMat;
                    else mr.sharedMaterial = rockMat;
                }

                foreach (var c in tr.GetComponents<Collider>())
                    UnityEngine.Object.DestroyImmediate(c);

                if (mf != null && mf.sharedMesh != null &&
                    !nonCollider.Contains(n) &&
                    (n.Contains("Arch") || n.Contains("Mound") || n.Contains("CoreStone") || n.Contains("Nest") || n.Contains("FailedStatues")))
                {
                    if (!n.Contains("Glow") && !n.Contains("Bones"))
                    {
                        var mc = tr.gameObject.AddComponent<MeshCollider>();
                        mc.sharedMesh = mf.sharedMesh;
                        mc.convex = false;
                    }
                }
            }
        }

        static void ConfigureProps(Transform roomRoot, Material rockMat, Material glowMat, Material steelMat)
        {
            foreach (Transform child in roomRoot)
            {
                if (!IsContractPropName(child.name)) continue;

                foreach (var c in child.GetComponentsInChildren<Collider>(true))
                    UnityEngine.Object.DestroyImmediate(c);

                foreach (var r in child.GetComponentsInChildren<Renderer>(true))
                {
                    string n = r.gameObject.name;
                    if (n.Contains("Glow")) r.sharedMaterial = glowMat;
                    else if (n.Contains("Chain")) r.sharedMaterial = steelMat;
                    else r.sharedMaterial = rockMat;
                }

                var mf = child.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    var mc = child.gameObject.AddComponent<MeshCollider>();
                    mc.sharedMesh = mf.sharedMesh;
                    mc.convex = false;
                }
            }
        }

        static bool IsContractPropName(string n) =>
            n.StartsWith("Prop_Pillar_") || n.StartsWith("Prop_Crystal_") || n.StartsWith("Prop_Perch_");

        static void BuildRoomLights(Transform roomRoot, BossRoomSpec spec)
        {
            var group = new GameObject("Room_Lights").transform;
            group.SetParent(roomRoot, false);

            AddPointLight(group, "Light_Dome_South", new Vector3(0f, 24f, -14f), spec.DomeLightColor, 125f, 95f);
            AddPointLight(group, "Light_Dome_North", new Vector3(0f, 24f, 14f), spec.DomeLightColor, 125f, 95f);
            AddPointLight(group, "Light_Vault_Mid", new Vector3(0f, 14f, 0f), spec.DomeLightColor, 90f, 75f);
            AddPointLight(group, "Light_Center", new Vector3(0f, 6.0f, 0f), spec.AccentLightColor, 38f, 40f);
            AddPointLight(group, "Light_Door_Arch", new Vector3(0f, 5.5f, -27.5f), spec.AccentLightColor, 38f, 36f);
            AddPointLight(group, "Light_Boss_Wait", spec.BossWaitLocal + new Vector3(0f, 6.0f, -1.5f), spec.BossLightColor, 42f, 40f);
            AddPointLight(group, "Light_Wall_East", new Vector3(23f, 7.0f, 0f), spec.AccentLightColor, 32f, 38f);
            AddPointLight(group, "Light_Wall_West", new Vector3(-23f, 7.0f, 0f), spec.AccentLightColor, 32f, 38f);
        }

        static void AddPointLight(Transform parent, string name, Vector3 localPos, Color color, float intensity, float range)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.intensity = intensity;
            l.range = range;
            l.shadows = LightShadows.None;
        }

        static void SetupSceneEnvironmentAndPlayer(BossRoomSpec spec, PrototypeSceneBuilder.SharedAssets shared, GameObject roomRoot)
        {
            var sun = UnityEngine.Object.FindAnyObjectByType<Light>();
            if (sun != null)
            {
                sun.name = "Room_Ambient_Dir";
                sun.color = spec.DomeLightColor;
                sun.intensity = 0.25f;
                sun.shadows = LightShadows.None;
            }

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = spec.AmbientColor;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = spec.FogColor;
            RenderSettings.fogStartDistance = 16f;
            RenderSettings.fogEndDistance = 90f;
            RenderSettings.skybox = null;

            AssetDatabase.DeleteAsset(spec.ProfileAssetPath);
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, spec.ProfileAssetPath);

            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(0.85f);
            bloom.intensity.Override(0.70f);
            bloom.scatter.Override(0.62f);
            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.24f);
            vignette.smoothness.Override(0.5f);
            var tone = profile.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.ACES);
            foreach (var c in profile.components) AssetDatabase.AddObjectToAsset(c, profile);
            EditorUtility.SetDirty(profile);

            var lookGo = new GameObject($"{spec.RoomName} Look");
            var vol = lookGo.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.priority = 1f;
            vol.sharedProfile = profile;

            var entryTr = roomRoot.transform.Find("Player_Entry");
            Vector3 spawnPos = (entryTr != null ? entryTr.position : PlayerEntryLocal) + Vector3.up * 0.6f;
            var slime = PrototypeSceneBuilder.CreatePlayer(shared, spawnPos);
            slime.transform.rotation = Quaternion.identity;

            // Tắt bộ lọc xám MonochromeVision để xem rõ màu cảnh khi kiểm tra phòng trùm
            var dark = GameObject.Find("Vision Volume");
            if (dark != null) UnityEngine.Object.DestroyImmediate(dark);

            var cam = Camera.main;
            if (cam != null)
            {
                cam.farClipPlane = 200f;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = spec.FogColor * 0.5f;
                cam.transform.SetPositionAndRotation(spawnPos + new Vector3(0f, 1.8f, -2.5f), Quaternion.Euler(4f, 0f, 0f));
            }
        }

        internal static Transform CreateMarker(Transform parent, string name, Vector3 localPos, Quaternion localRot)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            return go.transform;
        }

        internal static Material UpsertVertexMaterial(string name, Color tint, float fill, float glow)
        {
            string path = $"{Root}/Materials/{name}.mat";
            var shader = Shader.Find("Evolve/RockVertexColor") ?? Shader.Find("Universal Render Pipeline/Lit");
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(mat, path);
            }
            if (shader != null) mat.shader = shader;
            if (mat.HasProperty("_Tint")) mat.SetColor("_Tint", new Color(tint.r, tint.g, tint.b, 1f));
            if (mat.HasProperty("_Fill")) mat.SetFloat("_Fill", fill);
            if (mat.HasProperty("_Glow")) mat.SetFloat("_Glow", glow);
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        internal static bool ValidateContract(GameObject roomRoot, BossRoomSpec spec, out string report)
        {
            Physics.SyncTransforms();
            var issues = new List<string>();
            var t = roomRoot.transform;

            string[] required =
            {
                "Player_Entry", "Boss_Wait",
                "Activate_Zone", "Room_Bounds",
                "Door_Mouth", "Door_Steel", "Door_Open_Pos", "Door_Closed_Pos",
            };
            foreach (var req in required)
            {
                if (t.Find(req) == null)
                    issues.Add($"Thiếu GameObject bắt buộc: {req}");
            }

            if (roomRoot.GetComponent<BossRoom>() == null)
                issues.Add("Gốc phòng thiếu component BossRoom");

            var actCol = t.Find("Activate_Zone")?.GetComponent<BoxCollider>();
            var doorMouth = t.Find("Door_Mouth");
            if (actCol == null || !actCol.isTrigger)
                issues.Add("Activate_Zone phải có Collider isTrigger = true");
            else
            {
                float minY = actCol.transform.position.y + actCol.center.y - actCol.size.y * 0.5f;
                if (minY > -0.5f)
                    issues.Add($"Activate_Zone đáy quá cao ({minY:F2} m > -0.5 m), dễ lỡ ClosestPoint khi người chơi đứng trên sàn");
                if (doorMouth != null)
                {
                    float minZ = actCol.transform.position.z + actCol.center.z - actCol.size.z * 0.5f;
                    float margin = minZ - doorMouth.position.z;
                    if (margin < 5.5f)
                        issues.Add($"Activate_Zone quá sát Door_Mouth ({margin:F1} m < 6 m)");
                }
            }

            var bndCol = t.Find("Room_Bounds")?.GetComponent<Collider>();
            if (bndCol == null || !bndCol.isTrigger)
                issues.Add("Room_Bounds phải có Collider isTrigger = true");

            var doorSteel = t.Find("Door_Steel");
            var doorSteelCol = doorSteel?.GetComponent<BoxCollider>();
            if (doorSteelCol == null || doorSteelCol.isTrigger)
                issues.Add("Door_Steel phải có Collider chặn (isTrigger = false)");
            else if (doorSteel.gameObject.isStatic)
                issues.Add("Door_Steel không được đánh dấu Static vì phải trượt khi đóng/mở");

            // Đo chiều cao trần thực tế bằng Raycast từ giữa phòng lên trên
            float measuredCeiling = -1f;
            if (Physics.Raycast(new Vector3(0f, 1.5f, 0f), Vector3.up, out var ceilHit, 60f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                measuredCeiling = ceilHit.point.y;
            else
                issues.Add("Không dò thấy trần phía trên tâm phòng (nguy cơ trần hở)");

            if (measuredCeiling > 0f && measuredCeiling < spec.MinCeilingRequired)
                issues.Add($"Trần ở giữa quá thấp: {measuredCeiling:F2} m < {spec.MinCeilingRequired:F1} m");

            // Kiểm tra sàn phẳng có collider (đo trực tiếp trên *_Shell_COL across bán kính 28 m, 40 điểm)
            int floorMisses = 0;
            for (int r = 4; r <= 28; r += 6)
            {
                for (int a = 0; a < 8; a++)
                {
                    float rad = a * Mathf.PI * 0.25f;
                    var p = new Vector3(Mathf.Cos(rad) * r, 4.0f, Mathf.Sin(rad) * r);
                    var hits = Physics.RaycastAll(p, Vector3.down, 8.0f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                    bool hitShellFloor = false;
                    for (int hi = 0; hi < hits.Length; hi++)
                    {
                        var h = hits[hi];
                        if (h.collider != null && h.collider.name.EndsWith("_Shell_COL") && Mathf.Abs(h.point.y) <= 0.35f)
                        {
                            hitShellFloor = true;
                            break;
                        }
                    }
                    if (!hitShellFloor)
                        floorMisses++;
                }
            }
            if (floorMisses > 0)
                issues.Add($"Sàn phẳng Shell_COL bị thiếu collider hoặc gồ ghề tại {floorMisses} điểm kiểm tra");

            // Kiểm tra tường xung quanh (bắn tia ngang ở y = 2.5 m theo 24 góc, trừ góc cửa Nam 270° +- 10°)
            int wallGaps = 0;
            for (int i = 0; i < 24; i++)
            {
                float deg = i * 15f;
                if (Mathf.Abs(Mathf.DeltaAngle(deg, 270f)) <= 12f) continue;
                float rad = deg * Mathf.Deg2Rad;
                var dir = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad));
                if (!Physics.Raycast(new Vector3(0f, 2.5f, 0f), dir, out _, 40f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                    wallGaps++;
            }
            if (wallGaps > 0)
                issues.Add($"Phát hiện {wallGaps} khe hở trên tường bao phòng");

            int propsWithCol = 0;
            foreach (Transform child in t)
            {
                if (!IsContractPropName(child.name)) continue;
                if (child.GetComponent<Collider>() == null)
                    issues.Add($"{child.name} thiếu Collider");
                else
                    propsWithCol++;
            }
            if (propsWithCol == 0)
                issues.Add("Chưa có Prop_Pillar_*, Prop_Crystal_* hoặc Prop_Perch_* nào");

            if (spec.RequiresAxeAnchor)
            {
                var axeAnchor = t.Find("Axe_Anchor");
                if (axeAnchor == null)
                    issues.Add("Thiếu Axe_Anchor");
                else
                {
                    if (axeAnchor.GetComponentsInChildren<Collider>(true).Length > 0)
                        issues.Add("Axe_Anchor không được có Collider chặn người chơi");
                    if (axeAnchor.childCount == 0)
                        issues.Add("Axe_Anchor chưa có prefab GolemAxe_Planted làm con");
                }
            }

            if (spec.RequiresOpenSkyAboveCenter)
            {
                for (int i = 0; i < 4; i++)
                {
                    float rad = i * Mathf.PI * 0.5f;
                    var origin = new Vector3(Mathf.Cos(rad) * 8f, 0.5f, Mathf.Sin(rad) * 8f);
                    if (Physics.Raycast(origin, Vector3.up, out var skyHit, 12f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                        issues.Add($"Có vật cản phía trên giữa phòng Chim ở {skyHit.point} (yêu cầu thoáng >= 9 m)");
                }
            }

            if (issues.Count == 0)
            {
                report = $"Đạt hợp đồng: trần giữa = {measuredCeiling:F2} m, tường kín 100% (0 khe hở), {propsWithCol} Prop_* có collider, cửa thép & Activate_Zone hợp lệ.";
                return true;
            }

            report = string.Join("\n", issues);
            return false;
        }

        internal static void CaptureEntryScreenshot(string outputPath, Vector3 lookAtWorld)
        {
            var entry = GameObject.Find("Player_Entry");
            if (entry == null)
            {
                Debug.LogWarning($"[BossRoomCapture] Không tìm thấy Player_Entry để chụp {outputPath}");
                return;
            }
            CaptureScreenshotFrom(outputPath, entry.transform.position + new Vector3(0f, 1.85f, 0.25f), lookAtWorld);
        }

        internal static void CaptureDoorScreenshot(string outputPath)
        {
            var doorMouth = GameObject.Find("Door_Mouth");
            if (doorMouth == null) return;
            Vector3 from = doorMouth.transform.position + new Vector3(3.2f, 2.8f, 16.5f);
            Vector3 target = doorMouth.transform.position + new Vector3(0f, 4.4f, 0f);
            CaptureScreenshotFrom(outputPath, from, target);
        }

        static void CaptureScreenshotFrom(string outputPath, Vector3 camPos, Vector3 lookAtWorld)
        {
            var cam = Camera.main;
            if (cam == null)
            {
                Debug.LogWarning($"[BossRoomCapture] Không tìm thấy Camera.main để chụp {outputPath}");
                return;
            }

            var prevRt = cam.targetTexture;
            var prevActive = RenderTexture.active;
            var prevPos = cam.transform.position;
            var prevRot = cam.transform.rotation;
            var prevFov = cam.fieldOfView;

            const int width = 1280;
            const int height = 720;
            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);

            try
            {
                cam.transform.position = camPos;
                cam.transform.LookAt(lookAtWorld, Vector3.up);
                cam.fieldOfView = 62f;
                cam.targetTexture = rt;
                cam.Render();

                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();

                Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                File.WriteAllBytes(outputPath, tex.EncodeToPNG());
                Debug.Log($"[BossRoomCapture] Đã chụp ảnh Unity -> {outputPath}");
            }
            finally
            {
                cam.targetTexture = prevRt;
                RenderTexture.active = prevActive;
                cam.transform.SetPositionAndRotation(prevPos, prevRot);
                cam.fieldOfView = prevFov;
                UnityEngine.Object.DestroyImmediate(rt);
                UnityEngine.Object.DestroyImmediate(tex);
            }
        }

        static void TryPlaceBossInScene(GameObject roomRoot, BossRoomSpec spec)
        {
            if (spec.BossPrefabPaths == null || spec.BossPrefabPaths.Length == 0)
                return;

            GameObject bossPrefab = null;
            string foundPath = null;
            foreach (var p in spec.BossPrefabPaths)
            {
                if (string.IsNullOrEmpty(p)) continue;
                var candidate = AssetDatabase.LoadAssetAtPath<GameObject>(p);
                if (candidate != null)
                {
                    bossPrefab = candidate;
                    foundPath = p;
                    break;
                }
            }

            if (bossPrefab == null)
            {
                Debug.LogWarning($"[{spec.RoomName}] Không tìm thấy prefab trùm nào trong danh sách BossPrefabPaths.");
                return;
            }

            var wait = roomRoot.transform.Find("Boss_Wait");
            Vector3 pos = wait != null ? wait.position : roomRoot.transform.TransformPoint(spec.BossWaitLocal);
            Quaternion rot = wait != null ? wait.rotation : Quaternion.Euler(0f, 180f, 0f);

            var bossInstance = (GameObject)PrefabUtility.InstantiatePrefab(bossPrefab);
            bossInstance.transform.SetPositionAndRotation(pos, rot);

            var bossRoom = roomRoot.GetComponent<BossRoom>();
            if (bossRoom != null)
            {
                var so = new SerializedObject(bossRoom);
                var prop = so.FindProperty("boss");
                if (prop != null)
                {
                    prop.objectReferenceValue = bossInstance;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            Debug.Log($"[{spec.RoomName}] Đã đặt trùm {bossInstance.name} (từ {foundPath}) tại Boss_Wait {pos}.");
        }
    }
}
