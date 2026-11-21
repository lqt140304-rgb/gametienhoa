using UnityEditor;
using UnityEngine;
using Evolve.Creatures;

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
            BuildFromScratch("ChoXuong", 0.55f, 1.9f, 1.1f, 2.4f, 190f, 38f, 70f, 120f, 0.7f);   // 30% smaller than the model as Meshy made it
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

            if (model.GetComponentInChildren<Animator>(true) == null) model.AddComponent<Animator>();   // CreatureLocomotion looks for one
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

            string path = Prefabs + name + " (Meshy).prefab";
            if (AssetDatabase.LoadAssetAtPath<Object>(path) != null) AssetDatabase.DeleteAsset(path);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            Debug.Log("[Meshy] Đã tạo " + path);
        }
    }
}
