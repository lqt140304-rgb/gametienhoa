using UnityEditor;
using UnityEngine;
using Evolve.Maze;

namespace Evolve.EditorTools
{
    /// <summary>
    /// Menu Evolve → Build Cáo Gió (Meshy) Prefab: copies the cave team's Cáo Gió prefab (scripts, collider, stats stay the same),
    /// swaps its "Model" child for the Meshy model and saves it next to it as "CaoGio (Meshy).prefab". The old prefab is untouched.
    /// The god menu prefers the (Meshy) prefab when spawning a Cáo Gió.
    /// </summary>
    public static class MeshyFoxPrefabBuilder
    {
        const string Source = "Assets/_Project/Prefabs/Wildlife/CaoGio.prefab";
        const string Model = "Assets/_Project/Models/Wildlife/Meshy/Creature_CaoGio_Meshy.fbx";
        const string Target = "Assets/_Project/Prefabs/Wildlife/CaoGio (Meshy).prefab";

        [MenuItem("Evolve/Build Cáo Gió (Meshy) Prefab")]
        static void Build()
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(Source);
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(Model);
            if (source == null) { Debug.LogError("[Meshy] Chưa có " + Source + " — chạy bước tạo sinh vật hang của bên làm hang trước."); return; }
            if (fbx == null) { Debug.LogError("[Meshy] Chưa có " + Model); return; }

            var root = (GameObject)PrefabUtility.InstantiatePrefab(source);
            PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);   // an independent copy, not a variant
            root.name = "CaoGio (Meshy)";

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

            if (AssetDatabase.LoadAssetAtPath<Object>(Target) != null) AssetDatabase.DeleteAsset(Target);
            PrefabUtility.SaveAsPrefabAsset(root, Target);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            Debug.Log("[Meshy] Đã tạo " + Target);
        }
    }
}
