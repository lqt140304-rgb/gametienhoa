using Evolve.Creatures;
using Evolve.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Evolve.EditorTools
{
    /// <summary>
    /// Evolve -> Dress Cavern: Creatures + Shelter. Puts what step 1 (Nuốt, Giải Mã, Biến Hình) needs into the Slime Cavern:
    /// two prey spawners on the dry shore, one predator on the far side, and a temporary shelter (a ring of glowing moss
    /// with a ShelterZone) a few metres from where the slimes appear. Real shelters come with the cave maze (step 3).
    /// Rerunnable: replaces what it made before.
    /// </summary>
    public static class CavernLife
    {
        const string Root = "Assets/_Project";
        const string RootName = "Cavern Life";

        [MenuItem("Evolve/Dress Cavern: Creatures + Shelter")]
        public static void Dress()
        {
            var preyPrefab = AssetDatabase.LoadAssetAtPath<Prey>(Root + "/Prefabs/Prey.prefab");
            var predatorPrefab = AssetDatabase.LoadAssetAtPath<Predator>(Root + "/Prefabs/Predator.prefab");
            if (preyPrefab == null || predatorPrefab == null)
            {
                Debug.LogError("Dress Cavern: Creatures + Shelter: thiếu Prefabs/Prey.prefab hoặc Prefabs/Predator.prefab.");
                return;
            }
            var old = GameObject.Find(RootName);
            if (old != null) Object.DestroyImmediate(old);
            var root = new GameObject(RootName).transform;

            PreySpawner(root, "Prey Spawner (south-west)", CavernLayout.ShorePoint(215f, 8f), 4, 6f, preyPrefab);
            PreySpawner(root, "Prey Spawner (north)", CavernLayout.ShorePoint(100f, 8f), 3, 6f, preyPrefab);
            PredatorSpawner(root, "Predator Spawner (north-east)", CavernLayout.ShorePoint(35f, 9f), 1, 6f, predatorPrefab);
            RealFirstShelter(root, CavernLayout.ShorePoint(248f, 16f));

            EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
            Debug.Log("Hang Khởi Sinh: đã thả 7 con mồi (2 ổ), 1 kẻ săn mồi và Hang Trú đầu tiên trên gờ cao (phía tây nam). Ctrl+S để lưu scene.");
        }

        static void PreySpawner(Transform root, string name, Vector3 pos, int count, float radius, Prey prefab)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            go.transform.position = pos;
            var so = new SerializedObject(go.AddComponent<PreySpawner>());
            so.FindProperty("prefab").objectReferenceValue = prefab;
            so.FindProperty("count").intValue = count;
            so.FindProperty("radius").floatValue = radius;
            so.FindProperty("minDistanceFromCenter").floatValue = 0f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void PredatorSpawner(Transform root, string name, Vector3 pos, int count, float radius, Predator prefab)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            go.transform.position = pos;
            var so = new SerializedObject(go.AddComponent<PredatorSpawner>());
            so.FindProperty("prefab").objectReferenceValue = prefab;
            so.FindProperty("count").intValue = count;
            so.FindProperty("radius").floatValue = radius;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Hang Trú đầu tiên: khoang đá nhỏ nằm trên gờ cao hơn mực lũ, lối vào hẹp và thảm rêu ẩm.</summary>
        static void RealFirstShelter(Transform root, Vector3 footPos)
        {
            var inward = new Vector3(footPos.x, 0f, footPos.z).normalized;
            MazeShelterBuilder.BuildRealShelter(
                root,
                "Hang Trú Đầu Tiên (Shelter)",
                footPos,
                inward,
                RockMaterial(),
                MossMaterial(),
                ledgeHeight: 5.6f);
        }

        static Material RockMaterial()
        {
            const string path = Root + "/Materials/Cavern_Rock.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.SetColor("_BaseColor", new Color(0.28f, 0.27f, 0.25f));
            m.SetFloat("_Smoothness", 0.1f);
            return m;
        }

        static Material MossMaterial()
        {
            const string path = Root + "/Materials/Cavern_ShelterMoss.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.SetColor("_BaseColor", new Color(0.18f, 0.45f, 0.22f));
            m.SetFloat("_Smoothness", 0.15f);
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", new Color(0.12f, 0.5f, 0.2f));
            AssetDatabase.CreateAsset(m, path);
            return m;
        }
    }
}
