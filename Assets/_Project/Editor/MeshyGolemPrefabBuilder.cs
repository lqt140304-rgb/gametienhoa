using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Evolve.Creatures;
using Evolve.Maze;

namespace Evolve.EditorTools
{
    /// <summary>
    /// Menu Evolve → Build Golem Thạch Anh (Meshy) Prefab. The golem came out of Meshy's auto-rig (a humanoid skeleton with its own walking and running
    /// clips), so it is animated by an Animator, not by CreatureLocomotion's bone names. This sets the three FBX files up (Humanoid rig: each file makes its own avatar, Unity retargets the clips; looping), builds an Animator Controller (Idle, Walk, Run on the "Speed" float that CreatureAnimDriver feeds) and a
    /// prefab "GolemThachAnh (Meshy)" with the same components the cave team's creatures get (kinematic body, capsule, Predator as a lord).
    /// The cave team has no Golem Thạch Anh prefab yet, so the numbers below are ours; they can replace them.
    /// </summary>
    public static class MeshyGolemPrefabBuilder
    {
        const string Dir = "Assets/_Project/Models/Wildlife/Meshy";
        const string Model = Dir + "/Creature_GolemThachAnh_Meshy.fbx";
        const string WalkFbx = Dir + "/Creature_GolemThachAnh_Meshy_Walk.fbx";
        const string RunFbx = Dir + "/Creature_GolemThachAnh_Meshy_Run.fbx";
        const string ControllerPath = Dir + "/Creature_GolemThachAnh_Meshy.controller";
        const string PrefabPath = "Assets/_Project/Prefabs/Wildlife/GolemThachAnh (Meshy).prefab";

        [MenuItem("Evolve/Build Golem Thạch Anh (Meshy) Prefab")]
        static void Build()
        {
            foreach (var p in new[] { Model, WalkFbx })
                if (AssetDatabase.LoadAssetAtPath<Object>(p) == null) { Debug.LogError("[Meshy] Chưa có " + p); return; }

            var main = (ModelImporter)AssetImporter.GetAtPath(Model);
            main.animationType = ModelImporterAnimationType.Human;   // Meshy's clips come from a rig with another rest pose: Humanoid retargets them
            main.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            main.importAnimation = false;
            main.SaveAndReimport();
            var avatar = AssetDatabase.LoadAllAssetsAtPath(Model).OfType<Avatar>().FirstOrDefault();
            if (avatar == null || !avatar.isValid || !avatar.isHuman) { Debug.LogError("[Meshy] Avatar Humanoid của golem không hợp lệ: mở Creature_GolemThachAnh_Meshy.fbx, tab Rig, bấm Configure để ghép xương."); return; }

            var walk = SetupClip(WalkFbx, "Walk");
            if (walk == null) { Debug.LogError("[Meshy] Không đọc được clip đi (dùng làm tư thế đứng)."); return; }

            var controller = BuildController(walk);
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(Model);
            var root = new GameObject("GolemThachAnh (Meshy)");
            var model = (GameObject)PrefabUtility.InstantiatePrefab(fbx, root.transform);
            model.name = "Model";
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one;
            var anim = model.GetComponentInChildren<Animator>(true) ?? model.AddComponent<Animator>();
            anim.runtimeAnimatorController = controller;
            anim.avatar = avatar;
            anim.applyRootMotion = false;

            var rb = root.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = true;
            var col = root.AddComponent<CapsuleCollider>();   // a biped stands tall: capsule along Y
            col.direction = 1;
            col.radius = 0.85f;
            col.height = 2.5f;
            col.center = new Vector3(0f, 1.25f, 0f);

            var pred = root.AddComponent<Predator>();
            var so = new SerializedObject(pred);
            so.FindProperty("patrolSpeed").floatValue = 1.6f;
            so.FindProperty("chaseSpeed").floatValue = 3.4f;
            so.FindProperty("biteDamage").floatValue = 40f;
            so.FindProperty("maxHealth").floatValue = 260f;
            so.FindProperty("nutrition").floatValue = 90f;
            so.FindProperty("bodyMass").floatValue = 140f;
            so.FindProperty("isLeader").boolValue = true;
            so.FindProperty("drops").objectReferenceValue = null;
            so.ApplyModifiedPropertiesWithoutUndo();

            var driver = root.AddComponent<CreatureAnimDriver>();
            driver.Configure(anim, "Attack");

            if (AssetDatabase.LoadAssetAtPath<Object>(PrefabPath) != null) AssetDatabase.DeleteAsset(PrefabPath);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            Debug.Log("[Meshy] Đã tạo " + PrefabPath);
        }

        static AnimationClip SetupClip(string path, string name)
        {
            var mi = (ModelImporter)AssetImporter.GetAtPath(path);
            mi.animationType = ModelImporterAnimationType.Human;      // the clip's own rig makes its own avatar, so each rest pose is retargeted
            mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            mi.importAnimation = true;
            mi.materialImportMode = ModelImporterMaterialImportMode.None;
            var clips = mi.defaultClipAnimations;
            if (clips.Length == 0) return null;
            var c = clips[0];
            c.name = name;
            c.loopTime = true;
            c.loopPose = true;
            c.lockRootHeightY = true; c.lockRootRotation = true; c.lockRootPositionXZ = true;   // it walks by being moved, not by root motion
            mi.clipAnimations = new[] { c };
            mi.SaveAndReimport();
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().FirstOrDefault(x => x.name == name);
        }

        static AnimatorController BuildController(AnimationClip walk)
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(ControllerPath) != null) AssetDatabase.DeleteAsset(ControllerPath);
            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);   // fed by CreatureAnimDriver; nothing reads them any more
            ctrl.AddParameter("Alarming", AnimatorControllerParameterType.Bool);
            ctrl.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            var sm = ctrl.layers[0].stateMachine;
            // One state: a standing pose = the walk clip stopped (speed 0) at the frame where the feet are together (frame 2 of 26). Meshy's walk and run clips never moved this
            // golem's legs in the game, so walking is written as code (GolemRigFx, GaitRules); the Animator only holds the standing pose every frame, which the code's turns are added to.
            var idle = sm.AddState("Idle");
            idle.motion = walk; idle.speed = 0f; idle.cycleOffset = 0.04f;
            sm.defaultState = idle;
            EditorUtility.SetDirty(ctrl);
            return ctrl;
        }

        static void Link(AnimatorState from, AnimatorState to, AnimatorConditionMode mode, float threshold)
        {
            var t = from.AddTransition(to);
            t.AddCondition(mode, threshold, "Speed");
            t.hasExitTime = false;
            t.duration = 0.2f;
        }
    }
}
