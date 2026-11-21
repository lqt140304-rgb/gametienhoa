using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Evolve.Creatures;
using Evolve.Maze;

namespace Evolve.EditorTools
{
    /// <summary>
    /// Menu Evolve → Build Meshy Static Prefabs. Cóc Hồ Già, Rắn Dung Nham, Bọ Cạp Thạch Anh and Chó Xương have a Meshy model but no bones yet:
    /// this puts each model into a prefab named "... (Meshy)" so you can see its look, size and colours in the game (it moves stiffly, with no
    /// animation). The first three copy the cave team's prefab (same stats and scripts); Chó Xương has no cave prefab yet, so it is built from
    /// scratch with our own numbers (a lord Predator); its model is rigged (Root, Head, Tail1-5, Leg_*), so it gets CreatureLocomotion too. The old prefabs are untouched; the god menu (F1) prefers the "(Meshy)" ones.
    /// </summary>
    public static class MeshyStaticPrefabBuilder
    {
        const string Models = "Assets/_Project/Models/Wildlife/Meshy/";
        const string Prefabs = "Assets/_Project/Prefabs/Wildlife/";

        [MenuItem("Evolve/Build Meshy Static Prefabs (Cóc, Rắn, Bọ Cạp, Chó Xương)")]
        static void BuildAll()
        {
            foreach (var n in new[] { "CocHoGia", "RanDungNham", "BoCapThachAnh" })
                MeshyFoxPrefabBuilder.Build(Prefabs + n + ".prefab", Models + "Creature_" + n + "_Meshy.fbx", Prefabs + n + " (Meshy).prefab", n + " (Meshy)");
            BuildFromScratch("ChoXuong", 0.55f, 1.9f, 1.1f, 2.4f, 190f, 38f, 70f, 120f, 0.5f);   // half the size Meshy made it: 30%, 10% and 20% smaller in three steps
        }

        static void BuildFromScratch(string name, float radius, float height, float patrol, float chase, float health, float bite, float nutrition, float mass, float scale = 1f)
        {
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(Models + "Creature_" + name + "_Meshy.fbx");
            if (fbx == null) { Debug.LogError("[Meshy] Chưa có mô hình " + name); return; }
            var root = new GameObject(name + " (Meshy)");
            var model = (GameObject)PrefabUtility.InstantiatePrefab(fbx, root.transform);
            model.name = "Model";
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one * scale;
            radius *= scale; height *= scale;   // the collider shrinks with the model

            var animator = model.GetComponentInChildren<Animator>(true) ?? model.AddComponent<Animator>();   // CreatureLocomotion looks for one
            if (name == "ChoXuong") animator.runtimeAnimatorController = BuildHoundController(Models + "Creature_ChoXuong_Meshy.fbx");
            var loco = root.AddComponent<CreatureLocomotion>();   // drives the rig's Root, Head, Tail1-5 and Leg_FL/FR/BL/BR bones (Chó Xương has them)
            loco.Configure(radius, patrol);

            var rb = root.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = true;
            var col = root.AddComponent<CapsuleCollider>();
            col.direction = 2;   // a four-legged body lies along Z
            col.radius = radius;
            col.height = height;
            col.center = new Vector3(0f, radius * 1.1f, 0f);

            var pred = root.AddComponent<Predator>();
            var so = new SerializedObject(pred);
            so.FindProperty("patrolSpeed").floatValue = patrol;
            so.FindProperty("chaseSpeed").floatValue = chase;
            so.FindProperty("biteDamage").floatValue = bite;
            so.FindProperty("maxHealth").floatValue = health;
            so.FindProperty("nutrition").floatValue = nutrition;
            so.FindProperty("bodyMass").floatValue = mass;
            so.FindProperty("isLeader").boolValue = true;
            so.FindProperty("drops").objectReferenceValue = null;
            so.ApplyModifiedPropertiesWithoutUndo();

            if (animator.runtimeAnimatorController != null) root.AddComponent<CreatureAnimDriver>().Configure(animator, "Attack");   // feeds Speed to the controller
            string path = Prefabs + name + " (Meshy).prefab";
            if (AssetDatabase.LoadAssetAtPath<Object>(path) != null) AssetDatabase.DeleteAsset(path);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            Debug.Log("[Meshy] Đã tạo " + path);
        }

        /// <summary>
        /// The hand-authored clips baked into the hound's FBX (tools/blender/author_cho_clips.py): Idle, Walk, Pounce_Tell, Pounce_Strike, Pounce_Recover.
        /// Names the clips, loops Idle and Walk, and builds the Animator Controller: Speed (fed by CreatureAnimDriver) switches Idle and Walk, the
        /// triggers CreatureCombat fires (Tell_Pounce, Strike_Pounce, Recover_Pounce) play the three pounce clips. WalkRate scales the Walk speed
        /// with how fast the dog really moves (set by ChoXuongRigFx). Returns null when the FBX has no clips (an older file).
        /// </summary>
        static RuntimeAnimatorController BuildHoundController(string fbxPath)
        {
            var mi = (ModelImporter)AssetImporter.GetAtPath(fbxPath);
            mi.animationType = ModelImporterAnimationType.Generic;
            mi.importAnimation = true;
            var clips = mi.defaultClipAnimations;
            if (clips.Length == 0) { Debug.LogWarning("[Meshy] Creature_ChoXuong_Meshy.fbx không có clip; Chó Xương dùng xoay xương bằng mã."); return null; }
            foreach (var c in clips)
            {
                string take = c.takeName ?? c.name;
                int bar = take.LastIndexOf('|');
                c.name = bar >= 0 ? take.Substring(bar + 1) : take;
                c.loopTime = c.name == "Idle" || c.name == "Walk";
                c.loopPose = c.loopTime;
            }
            mi.clipAnimations = clips;
            mi.SaveAndReimport();
            var found = AssetDatabase.LoadAllAssetsAtPath(fbxPath).OfType<AnimationClip>().Where(x => !x.name.StartsWith("__preview")).ToDictionary(x => x.name, x => x, System.StringComparer.Ordinal);
            foreach (var need in new[] { "Idle", "Walk", "Pounce_Tell", "Pounce_Strike", "Pounce_Recover" })
                if (!found.ContainsKey(need)) { Debug.LogWarning("[Meshy] Thiếu clip " + need); return null; }

            string path = Models + "Creature_ChoXuong_Meshy.controller";
            if (AssetDatabase.LoadAssetAtPath<Object>(path) != null) AssetDatabase.DeleteAsset(path);
            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(path);
            ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
            ctrl.AddParameter("WalkRate", AnimatorControllerParameterType.Float);
            ctrl.AddParameter("Alarming", AnimatorControllerParameterType.Bool);
            ctrl.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            foreach (var t in new[] { "Tell_Pounce", "Strike_Pounce", "Recover_Pounce" }) ctrl.AddParameter(t, AnimatorControllerParameterType.Trigger);
            var sm = ctrl.layers[0].stateMachine;
            var idle = sm.AddState("Idle"); idle.motion = found["Idle"];
            var walk = sm.AddState("Walk"); walk.motion = found["Walk"]; walk.speedParameterActive = true; walk.speedParameter = "WalkRate";
            var tell = sm.AddState("Pounce_Tell"); tell.motion = found["Pounce_Tell"];
            var strike = sm.AddState("Pounce_Strike"); strike.motion = found["Pounce_Strike"];
            var recover = sm.AddState("Pounce_Recover"); recover.motion = found["Pounce_Recover"];
            sm.defaultState = idle;
            Link(idle, walk, 0.1f, true);
            Link(walk, idle, 0.1f, false);
            var anyTell = sm.AddAnyStateTransition(tell); anyTell.AddCondition(AnimatorConditionMode.If, 0, "Tell_Pounce"); anyTell.hasExitTime = false; anyTell.duration = 0.12f; anyTell.canTransitionToSelf = false;
            var toStrike = tell.AddTransition(strike); toStrike.AddCondition(AnimatorConditionMode.If, 0, "Strike_Pounce"); toStrike.hasExitTime = false; toStrike.duration = 0.05f;
            var anyStrike = sm.AddAnyStateTransition(strike); anyStrike.AddCondition(AnimatorConditionMode.If, 0, "Strike_Pounce"); anyStrike.hasExitTime = false; anyStrike.duration = 0.05f; anyStrike.canTransitionToSelf = false;
            var toRecover = strike.AddTransition(recover); toRecover.AddCondition(AnimatorConditionMode.If, 0, "Recover_Pounce"); toRecover.hasExitTime = false; toRecover.duration = 0.08f;
            var anyRecover = sm.AddAnyStateTransition(recover); anyRecover.AddCondition(AnimatorConditionMode.If, 0, "Recover_Pounce"); anyRecover.hasExitTime = false; anyRecover.duration = 0.08f; anyRecover.canTransitionToSelf = false;
            var back = recover.AddTransition(idle); back.hasExitTime = true; back.exitTime = 0.96f; back.duration = 0.2f;
            EditorUtility.SetDirty(ctrl);
            AssetDatabase.SaveAssets();
            return ctrl;
        }

        static void Link(AnimatorState from, AnimatorState to, float threshold, bool greater)
        {
            var t = from.AddTransition(to);
            t.AddCondition(greater ? AnimatorConditionMode.Greater : AnimatorConditionMode.Less, threshold, "Speed");
            t.hasExitTime = false;
            t.duration = 0.2f;
        }
    }
}
