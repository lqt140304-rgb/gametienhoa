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
                ("1/10 Dựng scene hồ (Build Underroot Lake)", UnderrootLakeBuilder.Build),
                ("2/10 Nước (Dress Lake: Water)", LakeWater.Dress),
                ("3/10 Nền địa hình (Dress Lake: Terrain)", LakeTerrain.Dress),
                ("4/10 Mặt đất (Dress Lake: Ground)", LakeGround.Dress),
                ("5/10 Quần xã thực vật (Dress Lake: Plant Communities)", LakeCommunities.Dress),
                ("6/10 Suối nguồn (Dress Lake: Spring)", LakeSpring.Dress),
                ("7/10 Nấm và dây leo (Dress Lake: Mushrooms and Vines)", LakeFlora.Dress),
                ("8/10 Mái lưới rễ (Dress Lake: Root Canopy)", LakeCanopy.Dress),
                ("9/10 Điểm nhấn các khu (Dress Lake: Landmarks)", LakeLandmarks.Dress),
                ("10/10 Rễ và ánh sáng (Dress Lake: Roots and Light)", LakeRootsLight.Dress),
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
