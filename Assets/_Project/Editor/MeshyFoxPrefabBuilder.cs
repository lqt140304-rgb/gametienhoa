using UnityEditor;
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
        static void BuildDreadwing() => Build("Assets/_Project/Prefabs/Wildlife/ChimHangMe.prefab",
            "Assets/_Project/Models/Wildlife/Meshy/Creature_ChimHangMe_Dreadwing.fbx",
            "Assets/_Project/Prefabs/Wildlife/ChimHangMe (Meshy).prefab", "ChimHangMe (Meshy)");

        internal static void Build(string sourcePath, string modelPath, string target, string rootName)
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
            anim.runtimeAnimatorController = controller;   // old clips find no matching bones here: they do nothing, but the triggers stay valid
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
