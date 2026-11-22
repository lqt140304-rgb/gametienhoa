using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Evolve.Combat;
using Evolve.Creatures;

namespace Evolve.EditorTools
{
    /// <summary>
    /// Menu Evolve → Build Bọ Cạp Thạch Anh (Meshy) Prefab. The scorpion's FBX carries its own bones and 25 clips (tools/blender/rig_scorpion.py): this sets the file up
    /// (Generic rig, one clip per take, the walks and the idle looping), builds an Animator Controller and rebuilds "BoCapThachAnh (Meshy).prefab" from the cave team's
    /// scorpion prefab (same stats and scripts), minus CreatureLocomotion (the clips do the walking; its bone-name gait would fight them).
    /// Controller: "Walk" is a 2D blend tree on MoveX / MoveY (the four walk clips around the idle); for each move Tell_X, Strike_X and Recover_X are reached by the triggers of the same
    /// name (ScorpionMoves fires them, one for each phase of the move), "Reset" returns to Walk, "Hit" flinches, "Die" plays the death.
    /// Stop Play mode first. Untested in Unity: check the Console for the lines starting with [Bọ Cạp].
    /// </summary>
    public static class MeshyScorpionPrefabBuilder
    {
        const string Dir = "Assets/_Project/Models/Wildlife/Meshy";
        const string Model = Dir + "/Creature_BoCapThachAnh_Meshy.fbx";
        const string ControllerPath = Dir + "/Creature_BoCapThachAnh_Meshy.controller";
        const string Source = "Assets/_Project/Prefabs/Wildlife/BoCapThachAnh.prefab";
        const string Target = "Assets/_Project/Prefabs/Wildlife/BoCapThachAnh (Meshy).prefab";
        static readonly string[] MoveAnims = { "Sting", "TailSlam", "Volley", "Burrow", "Pinch", "Burst" };
        static readonly string[] Looping = { "Idle", "Walk_F", "Walk_B", "Walk_L", "Walk_R", "Exposed", "Stuck" };

        [MenuItem("Evolve/Build Bọ Cạp Thạch Anh (Meshy) Prefab")]
        static void Build()
        {
            if (EditorApplication.isPlaying) { Debug.LogError("[Bọ Cạp] Tắt Play trước khi chạy menu này."); return; }
            if (AssetDatabase.LoadAssetAtPath<Object>(Model) == null) { Debug.LogError("[Bọ Cạp] Chưa có " + Model); return; }

            var clips = SetupClips();
            if (clips == null) return;
            var controller = BuildController(clips);

            MeshyFoxPrefabBuilder.Build(Source, Model, Target, "BoCapThachAnh (Meshy)");   // the cave prefab with the model swapped
            var root = PrefabUtility.LoadPrefabContents(Target);
            try
            {
                var loco = root.GetComponent<CreatureLocomotion>();
                if (loco != null) Object.DestroyImmediate(loco);
                var model = root.transform.Find("Model");
                var anim = model != null ? model.GetComponentInChildren<Animator>(true) : null;
                if (anim == null) { Debug.LogError("[Bọ Cạp] Prefab không có Animator ở Model."); return; }
                anim.runtimeAnimatorController = controller;
                anim.applyRootMotion = false;
                anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                var driver = root.GetComponent<Evolve.Maze.CreatureAnimDriver>() ?? root.AddComponent<Evolve.Maze.CreatureAnimDriver>();
                driver.Configure(anim, "Attack");
                PrefabUtility.SaveAsPrefabAsset(root, Target);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets();
            Debug.Log("[Bọ Cạp] Đã tạo " + Target + " (" + clips.Count + " clip). Tạo con từ menu thần F1 (Chúa Hang) rồi mở tab Combat để xem pha.");
        }

        static System.Collections.Generic.Dictionary<string, AnimationClip> SetupClips()
        {
            var mi = (ModelImporter)AssetImporter.GetAtPath(Model);
            mi.animationType = ModelImporterAnimationType.Generic;
            mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            mi.importAnimation = true;
            var defaults = mi.defaultClipAnimations;
            if (defaults.Length == 0) { Debug.LogError("[Bọ Cạp] FBX không có clip nào: chạy lại tools/blender/rig_scorpion.py."); return null; }
            var list = new System.Collections.Generic.List<ModelImporterClipAnimation>();
            var seen = new System.Collections.Generic.HashSet<string>();
            Debug.Log("[Bọ Cạp] Take trong FBX (" + defaults.Length + "): " + string.Join(", ", defaults.Select(x => x.name)));
            foreach (var d in defaults)
            {
                var c = d;
                c.name = d.name.Substring(d.name.LastIndexOf('|') + 1);   // "Armature|Armature|Walk_F" -> "Walk_F"
                if (!seen.Add(c.name)) continue;   // the same take twice: keep the first
                bool loop = Looping.Contains(c.name);
                c.loopTime = loop; c.loopPose = loop;
                list.Add(c);
            }
            mi.clipAnimations = list.ToArray();
            mi.SaveAndReimport();
            var found = AssetDatabase.LoadAllAssetsAtPath(Model).OfType<AnimationClip>().Where(a => !a.name.StartsWith("__preview__")).GroupBy(a => a.name).ToDictionary(g => g.Key, g => g.First());
            foreach (var need in new[] { "Idle", "Walk_F", "Walk_B", "Walk_L", "Walk_R", "Hit", "Death", "Flick", "Exposed", "Stuck" }.Concat(MoveAnims.SelectMany(a => new[] { "Tell_" + a, "Strike_" + a, "Recover_" + a })))
                if (!found.ContainsKey(need)) { Debug.LogError("[Bọ Cạp] Thiếu clip " + need + " (có: " + string.Join(", ", found.Keys) + ")"); return null; }
            return found;
        }

        static AnimatorController BuildController(System.Collections.Generic.Dictionary<string, AnimationClip> clips)
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(ControllerPath) != null) AssetDatabase.DeleteAsset(ControllerPath);
            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            foreach (var f in new[] { "Speed", "MoveX", "MoveY" }) ctrl.AddParameter(f, AnimatorControllerParameterType.Float);   // Speed is fed by CreatureAnimDriver, MoveX/MoveY by ScorpionMoves
            ctrl.AddParameter("Alarming", AnimatorControllerParameterType.Bool);
            foreach (var t in new[] { "Hit", "Die", "Reset", "Attack", "Flick", "Exposed", "Stuck" }) ctrl.AddParameter(t, AnimatorControllerParameterType.Trigger);
            foreach (var a in MoveAnims) foreach (var ph in new[] { "Tell_", "Strike_", "Recover_" }) ctrl.AddParameter(ph + a, AnimatorControllerParameterType.Trigger);
            var sm = ctrl.layers[0].stateMachine;

            var loco = sm.AddState("Walk");
            var tree = new BlendTree { name = "Walk4", blendType = BlendTreeType.FreeformDirectional2D, blendParameter = "MoveX", blendParameterY = "MoveY", useAutomaticThresholds = false };
            AssetDatabase.AddObjectToAsset(tree, ctrl);
            tree.AddChild(clips["Idle"], new Vector2(0f, 0f));
            tree.AddChild(clips["Walk_F"], new Vector2(0f, 1f));   // MoveY is forward
            tree.AddChild(clips["Walk_B"], new Vector2(0f, -1f));
            tree.AddChild(clips["Walk_R"], new Vector2(1f, 0f));   // MoveX is to its right
            tree.AddChild(clips["Walk_L"], new Vector2(-1f, 0f));
            loco.motion = tree;
            sm.defaultState = loco;

            foreach (var a in MoveAnims)
                foreach (var ph in new[] { "Tell_", "Strike_", "Recover_" })
                {
                    var s = sm.AddState(ph + a);
                    s.motion = clips[ph + a];
                    var enter = sm.AddAnyStateTransition(s);
                    enter.AddCondition(AnimatorConditionMode.If, 0f, ph + a);
                    enter.hasExitTime = false; enter.duration = 0.06f; enter.canTransitionToSelf = false;
                    if (ph == "Recover_")   // the after-cast ends back in Walk; a charge-up and a cast hold their last pose until the next phase's trigger
                    {
                        var back = s.AddTransition(loco);
                        back.hasExitTime = true; back.exitTime = 0.98f; back.duration = 0.2f;
                    }
                }

            foreach (var extra in new[] { "Flick", "Exposed", "Stuck" })   // the tail's crystal flick, the lie-open after a big move, claws jammed in a wall: each by its trigger, left by "Reset" (or the next phase's trigger)
            {
                var st = sm.AddState(extra); st.motion = clips[extra];
                var go = sm.AddAnyStateTransition(st);
                go.AddCondition(AnimatorConditionMode.If, 0f, extra);
                go.hasExitTime = false; go.duration = 0.08f; go.canTransitionToSelf = extra == "Flick";   // three flicks in a row
            }
            var hit = sm.AddState("Hit"); hit.motion = clips["Hit"];
            var toHit = loco.AddTransition(hit); toHit.AddCondition(AnimatorConditionMode.If, 0f, "Hit"); toHit.hasExitTime = false; toHit.duration = 0.05f;   // only from Walk: a flinch never cuts a move short
            var fromHit = hit.AddTransition(loco); fromHit.hasExitTime = true; fromHit.exitTime = 0.95f; fromHit.duration = 0.12f;
            var die = sm.AddState("Die"); die.motion = clips["Death"];
            var toDie = sm.AddAnyStateTransition(die); toDie.AddCondition(AnimatorConditionMode.If, 0f, "Die"); toDie.hasExitTime = false; toDie.duration = 0.1f; toDie.canTransitionToSelf = false;
            var reset = sm.AddAnyStateTransition(loco); reset.AddCondition(AnimatorConditionMode.If, 0f, "Reset"); reset.hasExitTime = false; reset.duration = 0.25f; reset.canTransitionToSelf = false;   // a move cut short (stunned, aborted)

            EditorUtility.SetDirty(ctrl);
            AssetDatabase.SaveAssets();
            return ctrl;
        }
    }
}
