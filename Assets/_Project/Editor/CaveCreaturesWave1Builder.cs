using System.Collections.Generic;
using System.IO;
using Evolve.Creatures;
using Evolve.Ecology;
using Evolve.Maze;
using Evolve.Organs;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Evolve.EditorTools
{
    /// <summary>
    /// Tạo AnimatorController, Prefab (Prey/Predator để trống ô drops) và SpeciesSO cho 12 loài sinh vật Đợt 1
    /// từ các file FBX đã gắn xương & animation ở Assets/_Project/Models/Wildlife/Creature_*.fbx.
    /// </summary>
    public static class CaveCreaturesWave1Builder
    {
        const string ModelDir = "Assets/_Project/Models/Wildlife";
        const string ControllerDir = ModelDir + "/Controllers";
        const string PrefabDir = "Assets/_Project/Prefabs/Wildlife";
        const string SpeciesDir = "Assets/_Project/ScriptableObjects/Ecology/Species";

        public readonly struct CreatureSpec
        {
            public readonly string PascalName;
            public readonly string DisplayName;
            public readonly string CaveThemeKey;
            public readonly bool IsPredator;
            public readonly bool IsLeader;
            public readonly string ActionClipName;
            public readonly float Radius;
            public readonly float Height;
            public readonly float MoveSpeed;
            public readonly float FastSpeed;
            public readonly float MaxHealth;
            public readonly float BodyMass;
            public readonly float Nutrition;
            public readonly float BiteDamage;
            public readonly int StartCount;
            public readonly int Capacity;
            public readonly string PreyId;

            public CreatureSpec(
                string pascalName,
                string displayName,
                string caveThemeKey,
                bool isPredator,
                bool isLeader,
                string actionClipName,
                float radius,
                float height,
                float moveSpeed,
                float fastSpeed,
                float maxHealth,
                float bodyMass,
                float nutrition,
                float biteDamage,
                int startCount,
                int capacity,
                string preyId = null)
            {
                PascalName = pascalName;
                DisplayName = displayName;
                CaveThemeKey = caveThemeKey;
                IsPredator = isPredator;
                IsLeader = isLeader;
                ActionClipName = actionClipName;
                Radius = radius;
                Height = height;
                MoveSpeed = moveSpeed;
                FastSpeed = fastSpeed;
                MaxHealth = maxHealth;
                BodyMass = bodyMass;
                Nutrition = nutrition;
                BiteDamage = biteDamage;
                StartCount = startCount;
                Capacity = capacity;
                PreyId = preyId;
            }
        }

        public static readonly CreatureSpec[] Specs =
        {
            // Hang 1: Hang Khởi Sinh
            new CreatureSpec("BoCanhCungNho", "Bọ Cánh Cứng Nhỏ", "BirthLake", false, false, "Hit", 0.28f, 0.55f, 0.55f, 1.5f, 10f, 2f, 14f, 0f, 8, 22),
            new CreatureSpec("OcHo", "Ốc Hồ", "BirthLake", false, false, "Retract", 0.30f, 0.60f, 0.30f, 0.7f, 14f, 3f, 16f, 0f, 8, 20),
            new CreatureSpec("ThanLanBoHo", "Thằn Lằn Bờ Hồ", "BirthLake", true, false, "Attack", 0.38f, 1.25f, 0.95f, 2.2f, 34f, 10f, 28f, 10f, 3, 8, "BoCanhCungNho"),
            new CreatureSpec("CocHoGia", "Cóc Hồ Già", "BirthLake", true, true, "Attack", 0.78f, 1.65f, 0.55f, 1.3f, 85f, 32f, 55f, 18f, 1, 2, "ThanLanBoHo"),

            // Hang 2: Vườn Quặng Cầu Vồng
            new CreatureSpec("DomDomSacMau", "Đom Đóm Sắc Màu", "LumaMushroomGrove", false, false, "Action", 0.30f, 0.75f, 0.85f, 2.1f, 12f, 2.5f, 16f, 0f, 8, 24),
            new CreatureSpec("ThoLa", "Thỏ Lá", "LumaMushroomGrove", false, false, "Evade", 0.32f, 0.85f, 1.10f, 2.8f, 15f, 4f, 20f, 0f, 8, 22),
            new CreatureSpec("BoGiapQuang", "Bọ Giáp Quặng", "LumaMushroomGrove", true, false, "Attack", 0.55f, 1.30f, 0.65f, 1.4f, 52f, 18f, 36f, 14f, 3, 8, "DomDomSacMau"),
            new CreatureSpec("RuaPhaLe", "Rùa Pha Lê", "LumaMushroomGrove", true, true, "Retract", 0.92f, 2.10f, 0.45f, 1.0f, 115f, 42f, 65f, 22f, 1, 2, "BoGiapQuang"),

            // Hang 5: Vực Nứt Tím
            new CreatureSpec("DoiAmThoa", "Dơi Âm Thoa", "CrystalVein", false, false, "Action", 0.45f, 0.95f, 1.05f, 2.6f, 16f, 3.5f, 18f, 0f, 8, 22),
            new CreatureSpec("BoGaiTim", "Bọ Gai Tím", "CrystalVein", false, false, "Attack", 0.36f, 0.80f, 0.65f, 1.6f, 20f, 5f, 22f, 0f, 8, 20),
            new CreatureSpec("NhenTinhThe", "Nhện Tinh Thể", "CrystalVein", true, false, "Attack", 0.58f, 1.35f, 1.10f, 2.6f, 55f, 16f, 38f, 16f, 3, 8, "DoiAmThoa"),
            new CreatureSpec("BoCapThachAnh", "Bọ Cạp Thạch Anh", "CrystalVein", true, true, "Attack", 0.88f, 2.20f, 0.75f, 1.8f, 130f, 45f, 72f, 24f, 1, 2, "NhenTinhThe"),
        };

        [MenuItem("Evolve/Build Wave 1 Creatures & Prefabs")]
        public static void BuildMenu()
        {
            EnsureAll();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Wave1Creatures] Đã dựng 12 AnimatorController, 12 Prefab (để trống drops) và 12 SpeciesSO.");
        }

        public static void EnsureAll()
        {
            BuildBatch(Specs);
        }

        public static void BuildBatch(CreatureSpec[] specs)
        {
            EnsureDir(ControllerDir);
            EnsureDir(PrefabDir);
            EnsureDir(SpeciesDir);

            var vcMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Rock_VC.mat");
            var glowMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Rock_Glow.mat");
            var orgMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Wildlife_Organic.mat") ?? vcMat;

            var speciesAssets = new Dictionary<string, SpeciesSO>();

            for (int i = 0; i < specs.Length; i++)
            {
                var spec = specs[i];
                string fbxPath = $"{ModelDir}/Creature_{spec.PascalName}.fbx";
                var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
                if (fbx == null)
                {
                    Debug.LogWarning($"[WaveCreatures] Thiếu FBX: {fbxPath}");
                    continue;
                }

                var controller = BuildAnimatorController(spec, fbxPath);
                var prefab = BuildCreaturePrefab(spec, fbx, controller, orgMat, vcMat, glowMat);
                var species = BuildSpeciesSO(spec, prefab);
                speciesAssets[spec.PascalName] = species;
            }

            // Liên kết chuỗi thức ăn Prey trên SpeciesSO
            for (int i = 0; i < specs.Length; i++)
            {
                var spec = specs[i];
                if (string.IsNullOrEmpty(spec.PreyId)) continue;
                if (speciesAssets.TryGetValue(spec.PascalName, out var pred) &&
                    speciesAssets.TryGetValue(spec.PreyId, out var prey))
                {
                    pred.Prey = prey;
                    pred.KillsPerHeadPerMinute = spec.IsLeader ? 0.12f : 0.25f;
                    EditorUtility.SetDirty(pred);
                }
            }
        }

        static AnimatorController BuildAnimatorController(CreatureSpec spec, string fbxPath)
        {
            string ctrlPath = $"{ControllerDir}/AC_{spec.PascalName}.controller";
            var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath);
            if (existing != null) AssetDatabase.DeleteAsset(ctrlPath);

            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
            ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
            ctrl.AddParameter("Alarming", AnimatorControllerParameterType.Bool);
            ctrl.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
            if (!string.IsNullOrEmpty(spec.ActionClipName) && spec.ActionClipName != "Hit")
                ctrl.AddParameter(spec.ActionClipName, AnimatorControllerParameterType.Trigger);

            var clips = new Dictionary<string, AnimationClip>();
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
            {
                if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                {
                    int bar = clip.name.IndexOf('|');
                    string shortName = bar >= 0 ? clip.name.Substring(bar + 1) : clip.name;
                    clips[shortName] = clip;
                }
            }

            var sm = ctrl.layers[0].stateMachine;
            var idleState = sm.AddState("Idle");
            idleState.motion = clips.TryGetValue("Idle", out var idleClip) ? idleClip : null;
            sm.defaultState = idleState;

            var walkState = sm.AddState("Walk");
            walkState.motion = clips.TryGetValue("Walk", out var walkClip) ? walkClip : null;

            var toWalk = idleState.AddTransition(walkState);
            toWalk.AddCondition(AnimatorConditionMode.Greater, 0.08f, "Speed");
            toWalk.hasExitTime = false;
            toWalk.duration = 0.15f;

            var toIdle = walkState.AddTransition(idleState);
            toIdle.AddCondition(AnimatorConditionMode.Less, 0.08f, "Speed");
            toIdle.hasExitTime = false;
            toIdle.duration = 0.15f;

            if (clips.TryGetValue("Hit", out var hitClip))
            {
                var hitState = sm.AddState("Hit");
                hitState.motion = hitClip;
                var anyToHit = sm.AddAnyStateTransition(hitState);
                anyToHit.AddCondition(AnimatorConditionMode.If, 0f, "Hit");
                anyToHit.hasExitTime = false;
                anyToHit.duration = 0.08f;

                var back = hitState.AddTransition(idleState);
                back.hasExitTime = true;
                back.exitTime = 0.9f;
                back.duration = 0.12f;
            }

            if (!string.IsNullOrEmpty(spec.ActionClipName) && spec.ActionClipName != "Hit" &&
                clips.TryGetValue(spec.ActionClipName, out var actClip))
            {
                var actState = sm.AddState(spec.ActionClipName);
                actState.motion = actClip;
                var anyToAct = sm.AddAnyStateTransition(actState);
                anyToAct.AddCondition(AnimatorConditionMode.If, 0f, spec.ActionClipName);
                anyToAct.hasExitTime = false;
                anyToAct.duration = 0.10f;

                var back = actState.AddTransition(idleState);
                back.hasExitTime = true;
                back.exitTime = 0.92f;
                back.duration = 0.15f;
            }

            EditorUtility.SetDirty(ctrl);
            return ctrl;
        }

        static GameObject BuildCreaturePrefab(
            CreatureSpec spec,
            GameObject fbx,
            AnimatorController controller,
            Material orgMat,
            Material vcMat,
            Material glowMat)
        {
            string prefabPath = $"{PrefabDir}/{spec.PascalName}.prefab";
            var root = new GameObject(spec.PascalName);

            var visual = (GameObject)PrefabUtility.InstantiatePrefab(fbx, root.transform);
            visual.name = "Model";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;

            foreach (var r in visual.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                for (int m = 0; m < mats.Length; m++)
                {
                    string mname = mats[m] != null ? mats[m].name : "";
                    if (mname.Contains("Crystal") || mname.Contains("Glow"))
                        mats[m] = glowMat ?? vcMat ?? orgMat;
                    else if (mname.Contains("Chitin"))
                        mats[m] = vcMat ?? orgMat;
                    else
                        mats[m] = orgMat ?? vcMat;
                }
                r.sharedMaterials = mats;
            }

            var anim = visual.GetComponentInChildren<Animator>(true);
            if (anim == null) anim = visual.AddComponent<Animator>();
            anim.runtimeAnimatorController = controller;

            var rb = root.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = true;

            var col = root.AddComponent<CapsuleCollider>();
            col.direction = 2; // Z forward
            col.radius = spec.Radius;
            col.height = spec.Height;
            col.center = new Vector3(0f, spec.Radius * 0.9f, 0f);

            if (!spec.IsPredator)
            {
                var prey = root.AddComponent<Prey>();
                var so = new SerializedObject(prey);
                so.FindProperty("wanderSpeed").floatValue = spec.MoveSpeed;
                so.FindProperty("fleeSpeed").floatValue = spec.FastSpeed;
                so.FindProperty("maxHealth").floatValue = spec.MaxHealth;
                so.FindProperty("nutrition").floatValue = spec.Nutrition;
                so.FindProperty("bodyMass").floatValue = spec.BodyMass;
                so.FindProperty("drops").objectReferenceValue = null; // Để trống theo brief
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            else
            {
                var pred = root.AddComponent<Predator>();
                var so = new SerializedObject(pred);
                so.FindProperty("patrolSpeed").floatValue = spec.MoveSpeed;
                so.FindProperty("chaseSpeed").floatValue = spec.FastSpeed;
                so.FindProperty("biteDamage").floatValue = spec.BiteDamage;
                so.FindProperty("maxHealth").floatValue = spec.MaxHealth;
                so.FindProperty("nutrition").floatValue = spec.Nutrition;
                so.FindProperty("bodyMass").floatValue = spec.BodyMass;
                so.FindProperty("isLeader").boolValue = spec.IsLeader;
                so.FindProperty("drops").objectReferenceValue = null; // Để trống theo brief
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            var driver = root.AddComponent<CreatureAnimDriver>();
            driver.Configure(anim, spec.ActionClipName);

            if (AssetDatabase.LoadAssetAtPath<Object>(prefabPath) != null)
                AssetDatabase.DeleteAsset(prefabPath);
            var saved = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return saved;
        }

        static SpeciesSO BuildSpeciesSO(CreatureSpec spec, GameObject prefab)
        {
            string soPath = $"{SpeciesDir}/{spec.PascalName}.asset";
            var so = AssetDatabase.LoadAssetAtPath<SpeciesSO>(soPath);
            if (so == null)
            {
                so = ScriptableObject.CreateInstance<SpeciesSO>();
                AssetDatabase.CreateAsset(so, soPath);
            }

            so.Id = spec.PascalName;
            so.Prefab = prefab;
            so.Tier = spec.IsLeader ? OrganTier.S : (spec.IsPredator ? OrganTier.B : OrganTier.C);
            so.StartCount = spec.StartCount;
            so.Capacity = spec.Capacity;
            so.BirthsPerMinute = spec.IsLeader ? 0.11f : (spec.IsPredator ? 0.25f : 0.65f);
            so.FoodPerHeadPerMinute = spec.IsPredator ? 0f : 0.45f;
            EditorUtility.SetDirty(so);
            return so;
        }

        public static SpeciesSO LoadSpeciesSO(string pascalName) =>
            AssetDatabase.LoadAssetAtPath<SpeciesSO>($"{SpeciesDir}/{pascalName}.asset");

        public static T LoadWave1CreaturePrefab<T>(string pascalName) where T : Component
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/{pascalName}.prefab");
            return go != null ? go.GetComponent<T>() : null;
        }

        static void EnsureDir(string assetDir)
        {
            string full = Path.GetFullPath(assetDir);
            if (!Directory.Exists(full)) Directory.CreateDirectory(full);
        }
    }
}
