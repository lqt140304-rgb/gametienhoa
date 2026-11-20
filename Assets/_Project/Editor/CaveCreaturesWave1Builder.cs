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
            new CreatureSpec("OcHo", "Ốc Hồ", "BirthLake", false, false, "Retract", 0.30f, 0.60f, 0.30f, 0.7f, 14f, 3f, 16f, 0f, 4, 8),
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

            EnsureCreatureMaterials(out var orgMat, out var chitinMat, out var glowMat);

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
                var prefab = BuildCreaturePrefab(spec, fbx, controller, orgMat, chitinMat, glowMat);
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
                clips.TryGetValue(spec.ActionClipName, out var actClip) &&
                !clips.ContainsKey("Tell_" + spec.ActionClipName))
            {
                var actState = sm.AddState(spec.ActionClipName);
                actState.motion = actClip;
                var anyToAct = sm.AddAnyStateTransition(actState);
                anyToAct.AddCondition(AnimatorConditionMode.If, 0f, spec.ActionClipName);
                anyToAct.hasExitTime = false;
                anyToAct.canTransitionToSelf = false;
                anyToAct.duration = 0.14f;

                var back = actState.AddTransition(idleState);
                back.hasExitTime = true;
                back.exitTime = 0.90f;
                back.duration = 0.20f;
            }

            // Các state chiêu chiến đấu (Tell_*, Strike_*, Recover_*, Stun, Feed, Flee) với crossfade mượt
            foreach (var kv in clips)
            {
                string cname = kv.Key;
                bool isTell = cname.StartsWith("Tell_");
                bool isStrike = cname.StartsWith("Strike_");
                bool isRecover = cname.StartsWith("Recover_");
                bool isExtra = cname == "Stun" || cname == "Feed" || cname == "Flee";
                if (!isTell && !isStrike && !isRecover && !isExtra) continue;

                ctrl.AddParameter(cname, AnimatorControllerParameterType.Trigger);
                var st = sm.AddState(cname);
                st.motion = kv.Value;

                var anyTo = sm.AddAnyStateTransition(st);
                anyTo.AddCondition(AnimatorConditionMode.If, 0f, cname);
                anyTo.hasExitTime = false;
                anyTo.canTransitionToSelf = false;
                anyTo.duration = isStrike ? 0.08f : 0.16f;

                if (isRecover || cname == "Stun")
                {
                    var backIdle = st.AddTransition(idleState);
                    backIdle.hasExitTime = true;
                    backIdle.exitTime = 0.90f;
                    backIdle.duration = 0.25f;

                    var backWalk = st.AddTransition(walkState);
                    backWalk.AddCondition(AnimatorConditionMode.Greater, 0.12f, "Speed");
                    backWalk.hasExitTime = false;
                    backWalk.duration = 0.22f;
                }
                else if (cname == "Flee" || cname == "Feed")
                {
                    var backIdle = st.AddTransition(idleState);
                    backIdle.AddCondition(AnimatorConditionMode.Less, 0.08f, "Speed");
                    backIdle.hasExitTime = false;
                    backIdle.duration = 0.22f;
                }
            }

            // Hỗ trợ animation Bơi (Swim) cho Cóc Hồ Già
            if (clips.TryGetValue("Swim", out var swimClip))
            {
                ctrl.AddParameter("Swimming", AnimatorControllerParameterType.Bool);
                ctrl.AddParameter("Swim", AnimatorControllerParameterType.Trigger);
                var swimState = sm.AddState("Swim");
                swimState.motion = swimClip;

                var idleToSwim = idleState.AddTransition(swimState);
                idleToSwim.AddCondition(AnimatorConditionMode.If, 0f, "Swimming");
                idleToSwim.hasExitTime = false;
                idleToSwim.duration = 0.20f;

                var walkToSwim = walkState.AddTransition(swimState);
                walkToSwim.AddCondition(AnimatorConditionMode.If, 0f, "Swimming");
                walkToSwim.hasExitTime = false;
                walkToSwim.duration = 0.20f;

                var swimToIdle = swimState.AddTransition(idleState);
                swimToIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "Swimming");
                swimToIdle.hasExitTime = false;
                swimToIdle.duration = 0.22f;

                var anyToSwim = sm.AddAnyStateTransition(swimState);
                anyToSwim.AddCondition(AnimatorConditionMode.If, 0f, "Swim");
                anyToSwim.hasExitTime = false;
                anyToSwim.canTransitionToSelf = false;
                anyToSwim.duration = 0.18f;
            }

            // Hỗ trợ animation Đậu nghỉ (Perch) cho Đom Đóm Sắc Màu
            if (clips.TryGetValue("Perch", out var perchClip))
            {
                ctrl.AddParameter("Perching", AnimatorControllerParameterType.Bool);
                ctrl.AddParameter("Perch", AnimatorControllerParameterType.Trigger);
                var perchState = sm.AddState("Perch");
                perchState.motion = perchClip;

                var idleToPerch = idleState.AddTransition(perchState);
                idleToPerch.AddCondition(AnimatorConditionMode.If, 0f, "Perching");
                idleToPerch.hasExitTime = false;
                idleToPerch.duration = 0.25f;

                var perchToIdle = perchState.AddTransition(idleState);
                perchToIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "Perching");
                perchToIdle.hasExitTime = false;
                perchToIdle.duration = 0.25f;

                var anyToPerch = sm.AddAnyStateTransition(perchState);
                anyToPerch.AddCondition(AnimatorConditionMode.If, 0f, "Perch");
                anyToPerch.hasExitTime = false;
                anyToPerch.canTransitionToSelf = false;
                anyToPerch.duration = 0.20f;
            }

            // Hỗ trợ animation Bay nhẹ (Fly) cho Bọ Cánh Cứng Nhỏ
            if (clips.TryGetValue("Fly", out var flyClip))
            {
                ctrl.AddParameter("Flying", AnimatorControllerParameterType.Bool);
                ctrl.AddParameter("Fly", AnimatorControllerParameterType.Trigger);
                var flyState = sm.AddState("Fly");
                flyState.motion = flyClip;

                var idleToFly = idleState.AddTransition(flyState);
                idleToFly.AddCondition(AnimatorConditionMode.If, 0f, "Flying");
                idleToFly.hasExitTime = false;
                idleToFly.duration = 0.15f;

                var walkToFly = walkState.AddTransition(flyState);
                walkToFly.AddCondition(AnimatorConditionMode.If, 0f, "Flying");
                walkToFly.hasExitTime = false;
                walkToFly.duration = 0.15f;

                var flyToIdle = flyState.AddTransition(idleState);
                flyToIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "Flying");
                flyToIdle.hasExitTime = false;
                flyToIdle.duration = 0.18f;

                var anyToFly = sm.AddAnyStateTransition(flyState);
                anyToFly.AddCondition(AnimatorConditionMode.If, 0f, "Fly");
                anyToFly.hasExitTime = false;
                anyToFly.canTransitionToSelf = false;
                anyToFly.duration = 0.15f;
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
            anim.applyRootMotion = false;

            var loco = root.AddComponent<CreatureLocomotion>();
            loco.Configure(spec.Radius, spec.MoveSpeed);

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

        static void EnsureCreatureMaterials(out Material orgMat, out Material chitinMat, out Material glowMat)
        {
            var shader = Shader.Find("Evolve/CreatureVertexColor") ?? Shader.Find("Evolve/RockVertexColor");
            orgMat = UpsertCreatureMat("Assets/_Project/Materials/Wildlife_Organic.mat", shader,
                linearBlend: 0.25f, fill: 0.44f, subsurface: 0.24f, smoothness: 0.30f, spec: 0.16f, rim: 0.26f, glow: 0f);
            chitinMat = UpsertCreatureMat("Assets/_Project/Materials/Wildlife_Chitin.mat", shader,
                linearBlend: 0.25f, fill: 0.40f, subsurface: 0.06f, smoothness: 0.62f, spec: 0.45f, rim: 0.32f, glow: 0f);
            glowMat = UpsertCreatureMat("Assets/_Project/Materials/Wildlife_Crystal_Glow.mat", shader,
                linearBlend: 0.20f, fill: 0.35f, subsurface: 0.10f, smoothness: 0.72f, spec: 0.55f, rim: 0.40f, glow: 0.85f);
        }

        static Material UpsertCreatureMat(
            string path, Shader shader,
            float linearBlend, float fill, float subsurface, float smoothness, float spec, float rim, float glow)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else if (shader != null && mat.shader != shader)
            {
                mat.shader = shader;
            }

            if (mat.HasProperty("_Tint")) mat.SetColor("_Tint", Color.white);
            if (mat.HasProperty("_LinearBlend")) mat.SetFloat("_LinearBlend", linearBlend);
            if (mat.HasProperty("_Fill")) mat.SetFloat("_Fill", fill);
            if (mat.HasProperty("_Subsurface")) mat.SetFloat("_Subsurface", subsurface);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_SpecStrength")) mat.SetFloat("_SpecStrength", spec);
            if (mat.HasProperty("_RimStrength")) mat.SetFloat("_RimStrength", rim);
            if (mat.HasProperty("_Glow")) mat.SetFloat("_Glow", glow);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static void EnsureDir(string assetDir)
        {
            string full = Path.GetFullPath(assetDir);
            if (!Directory.Exists(full)) Directory.CreateDirectory(full);
        }
    }
}
