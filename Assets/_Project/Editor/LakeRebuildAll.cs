using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Evolve.EditorTools
{
    /// <summary>
    /// One menu to rebuild the whole lake, in the right order: Evolve -> Rebuild Lake (all steps).
    /// Build Underroot Lake, then Water, Terrain, Ground, Spring, Mushrooms and Vines, Roots and Light, then saves the scene.
    /// (Each step is also available alone in the Evolve menu.) If a step fails the others are not run and the
    /// message says which step it was.
    /// </summary>
    public static class LakeRebuildAll
    {
        [MenuItem("Evolve/Rebuild Lake (all steps)")]
        public static void Run()
        {
            var steps = new (string name, Action run)[]
            {
                ("1/7 Dựng scene hồ (Build Underroot Lake)", UnderrootLakeBuilder.Build),
                ("2/7 Nước (Dress Lake: Water)", LakeWater.Dress),
                ("3/7 Nền địa hình (Dress Lake: Terrain)", LakeTerrain.Dress),
                ("4/7 Mặt đất (Dress Lake: Ground)", LakeGround.Dress),
                ("5/7 Suối nguồn (Dress Lake: Spring)", LakeSpring.Dress),
                ("6/7 Nấm và dây leo (Dress Lake: Mushrooms and Vines)", LakeFlora.Dress),
                ("7/7 Rễ và ánh sáng (Dress Lake: Roots and Light)", LakeRootsLight.Dress),
            };
            try
            {
                for (int i = 0; i < steps.Length; i++)
                {
                    EditorUtility.DisplayProgressBar("Dựng lại hồ", steps[i].name, i / (float)steps.Length);
                    steps[i].run();
                }
                EditorSceneManager.SaveOpenScenes();
                Debug.Log("Đã dựng lại toàn bộ hồ và lưu scene. Bấm Play để xem (F6: bay ghost, Page Up: mở rộng góc nhìn).");
            }
            catch (Exception e)
            {
                Debug.LogError("Dựng lại hồ dừng ở một bước bị lỗi (xem lỗi bên dưới): " + e.Message);
                Debug.LogException(e);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }
    }
}
