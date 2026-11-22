using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Evolve.Maze;

namespace Evolve.EditorTools
{
    /// <summary>
    /// Menus Evolve → Build Cáo Gió (Meshy) Prefab and Build Chim Hang Mẹ (Meshy) Prefab: copy the cave team's Cáo Gió prefab (scripts, collider, stats stay the same),
    /// swaps its "Model" child for the Meshy model and saves it next to it as "CaoGio (Meshy).prefab". The old prefab is untouched.
    /// The god menu prefers the (Meshy) prefab when spawning a Cáo Gió.
    /// </summary>
    public static class MeshyFoxPrefabBuilder
    {
        const string Source = "Assets/_Project/Prefabs/Wildlife/CaoGio.prefab";
        const string Model = "Assets/_Project/Models/Wildlife/Meshy/Creature_CaoGio_Meshy.fbx";
        const string Target = "Assets/_Project/Prefabs/Wildlife/CaoGio (Meshy).prefab";

        [MenuItem("Evolve/Build Cáo Gió (Meshy) Prefab")]
        static void BuildFox() => Build(Source, Model, Target, "CaoGio (Meshy)");

        [MenuItem("Evolve/Build Chim Hang Mẹ (Meshy) Prefab")]
        static void BuildBird() => Build("Assets/_Project/Prefabs/Wildlife/ChimHangMe.prefab",
            "Assets/_Project/Models/Wildlife/Meshy/Creature_ChimHangMe_Meshy.fbx",
            "Assets/_Project/Prefabs/Wildlife/ChimHangMe (Meshy).prefab", "ChimHangMe (Meshy)");

        /// <summary>The Dreadwing Carrion model (rig_bird_dreadwing.py) replaces the first Meshy bird: same prefab name, so the god menu and the cave ecology pick it up; git keeps the old prefab.</summary>
        [MenuItem("Evolve/Build Chim Hang Mẹ (Dreadwing) Prefab")]
        static void BuildDreadwing()
        {
            const string dir = "Assets/_Project/Models/Wildlife/Meshy/";
            var controller = BuildDreadwingController(dir + "Creature_ChimHangMe_Dreadwing_Walk.fbx", dir + "Creature_ChimHangMe_Dreadwing.controller");
            if (controller == null) return;
            Build("Assets/_Project/Prefabs/Wildlife/ChimHangMe.prefab", dir + "Creature_ChimHangMe_Dreadwing.fbx",
                "Assets/_Project/Prefabs/Wildlife/ChimHangMe (Meshy).prefab", "ChimHangMe (Meshy)", controller);
        }

        /// <summary>
        /// The Blender clips of the Dreadwing (tools/blender/bird_walk_clip.py: "Idle" and the walk in 8 directions "Walk_F" ... "Walk_FL", wings folded) become looping Generic clips. An Animator Controller plays Idle,
        /// or the state "Walk": a 2D blend tree on (MoveX, MoveY), the unit direction it moves in relative to where it faces (BirdRigFx feeds it), so it steps sideways and diagonally while it circles the player.
        /// Idle and Walk switch on the "Speed" float that CreatureLocomotion / CreatureAnimDriver feed (the state names are what CreatureLocomotion treats as non-combat). The parameters the golem controller also has
        /// are here so those scripts find their triggers. BirdRigFx recognises the controller by its name (it contains "Dreadwing") and leaves the legs to the clip.
        /// </summary>
        static AnimatorController BuildDreadwingController(string clipFbx, string controllerPath)
        {
            var mi = AssetImporter.GetAtPath(clipFbx) as ModelImporter;
            if (mi == null) { Debug.LogError("[Meshy] Chưa có " + clipFbx + " (chạy tools/blender/rig_bird_dreadwing.py)"); return null; }
            mi.animationType = ModelImporterAnimationType.Generic;
            mi.importAnimation = true;
            mi.materialImportMode = ModelImporterMaterialImportMode.None;
            var clips = mi.defaultClipAnimations;
            foreach (var c in clips)
            {
                c.name = c.name.Substring(c.name.LastIndexOf('|') + 1);   // "Armature|Walk_FR" -> "Walk_FR"
                c.loopTime = true; c.loopPose = true;
            }
            mi.clipAnimations = clips;
            mi.SaveAndReimport();
            var all = AssetDatabase.LoadAllAssetsAtPath(clipFbx).OfType<AnimationClip>().Where(x => !x.name.StartsWith("__preview")).ToArray();
            var idle = all.FirstOrDefault(x => x.name == "Idle");
            // direction -> (MoveX = to its right, MoveY = forward)
            var dirs = new[] { ("F", 0f, 1f), ("FR", 0.7071f, 0.7071f), ("R", 1f, 0f), ("BR", 0.7071f, -0.7071f), ("B", 0f, -1f), ("BL", -0.7071f, -0.7071f), ("L", -1f, 0f), ("FL", -0.7071f, 0.7071f) };
            var walks = dirs.Select(d => (clip: all.FirstOrDefault(x => x.name == "Walk_" + d.Item1), x: d.Item2, y: d.Item3)).ToArray();
            if (idle == null || walks.Any(w => w.clip == null)) { Debug.LogError("[Meshy] Thiếu clip trong " + clipFbx + ": có " + string.Join(", ", all.Select(x => x.name))); return null; }

            if (AssetDatabase.LoadAssetAtPath<Object>(controllerPath) != null) AssetDatabase.DeleteAsset(controllerPath);
            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
            ctrl.AddParameter("MoveX", AnimatorControllerParameterType.Float);
            ctrl.AddParameter("MoveY", AnimatorControllerParameterType.Float);
            ctrl.AddParameter("Alarming", AnimatorControllerParameterType.Bool);
            ctrl.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            var sm = ctrl.layers[0].stateMachine;
            var sIdle = sm.AddState("Idle"); sIdle.motion = idle;
            ctrl.CreateBlendTreeInController("Walk", out BlendTree tree);
            tree.blendType = BlendTreeType.FreeformCartesian2D;
            tree.blendParameter = "MoveX"; tree.blendParameterY = "MoveY";
            foreach (var w in walks) tree.AddChild(w.clip, new Vector2(w.x, w.y));
            var sWalk = sm.states.First(s => s.state.name == "Walk").state;
            sm.defaultState = sIdle;
            var toWalk = sIdle.AddTransition(sWalk); toWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed"); toWalk.hasExitTime = false; toWalk.duration = 0.15f;
            var toIdle = sWalk.AddTransition(sIdle); toIdle.AddCondition(AnimatorConditionMode.Less, 0.08f, "Speed"); toIdle.hasExitTime = false; toIdle.duration = 0.2f;
            EditorUtility.SetDirty(ctrl);
            AssetDatabase.SaveAssets();
            return ctrl;
        }

        internal static void Build(string sourcePath, string modelPath, string target, string rootName, RuntimeAnimatorController controllerOverride = null)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (source == null) { Debug.LogError("[Meshy] Chưa có " + sourcePath + " — chạy bước tạo sinh vật hang của bên làm hang trước."); return; }
            if (fbx == null) { Debug.LogError("[Meshy] Chưa có " + modelPath); return; }

            var root = (GameObject)PrefabUtility.InstantiatePrefab(source);
            PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);   // an independent copy, not a variant
            root.name = rootName;

            var old = root.transform.Find("Model");
            RuntimeAnimatorController controller = null;
            if (old != null)
            {
                var oldAnim = old.GetComponentInChildren<Animator>(true);
                if (oldAnim != null) controller = oldAnim.runtimeAnimatorController;
                Object.DestroyImmediate(old.gameObject);
            }

            var model = (GameObject)PrefabUtility.InstantiatePrefab(fbx, root.transform);
            model.name = "Model";
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one;

            var anim = model.GetComponentInChildren<Animator>(true);
            if (anim == null) anim = model.AddComponent<Animator>();
            anim.runtimeAnimatorController = controllerOverride != null ? controllerOverride : controller;   // the cave team's clips find no matching bones here: they do nothing, but the triggers stay valid
            anim.applyRootMotion = false;

            var driver = root.GetComponent<CreatureAnimDriver>();
            if (driver != null)
            {
                var so = new SerializedObject(driver);
                so.FindProperty("animator").objectReferenceValue = anim;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            if (AssetDatabase.LoadAssetAtPath<Object>(target) != null) AssetDatabase.DeleteAsset(target);
            PrefabUtility.SaveAsPrefabAsset(root, target);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            Debug.Log("[Meshy] Đã tạo " + target);
        }
    }
}
