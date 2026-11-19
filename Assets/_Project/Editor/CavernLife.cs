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
            Shelter(root, CavernLayout.ShorePoint(250f, 6f));

            EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
            Debug.Log("Hang Khởi Sinh: đã thả 7 con mồi (2 ổ), 1 kẻ săn mồi và một tổ tạm (vòng rêu sáng, phía tây nam gần chỗ xuất hiện). Ctrl+S để lưu scene.");
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

        /// <summary>A temporary shelter: a soft green moss ring on the ground, a faint light, and a ShelterZone trigger over it.</summary>
        static void Shelter(Transform root, Vector3 pos)
        {
            var go = new GameObject("Tổ tạm (Shelter)");
            go.transform.SetParent(root, false);
            go.transform.position = pos;
            var col = go.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 2.6f;
            go.AddComponent<ShelterZone>();

            var moss = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            moss.name = "Moss Ring";
            Object.DestroyImmediate(moss.GetComponent<Collider>());
            moss.transform.SetParent(go.transform, false);
            moss.transform.localPosition = new Vector3(0f, 0.03f, 0f);
            moss.transform.localScale = new Vector3(5f, 0.02f, 5f);
            moss.GetComponent<Renderer>().sharedMaterial = MossMaterial();

            var light = new GameObject("Shelter Glow").AddComponent<Light>();
            light.transform.SetParent(go.transform, false);
            light.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            light.type = LightType.Point;
            light.color = new Color(0.55f, 1f, 0.6f);
            light.range = 6f;
            light.intensity = 2.5f;
            light.shadows = LightShadows.None;
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
