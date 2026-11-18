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
                ("1/17 Dựng scene hồ (Build Underroot Lake)", UnderrootLakeBuilder.Build),
                ("2/17 Nước (Dress Lake: Water)", LakeWater.Dress),
                ("3/17 Nền địa hình (Dress Lake: Terrain)", LakeTerrain.Dress),
                ("4/17 Mặt đất (Dress Lake: Ground)", LakeGround.Dress),
                ("5/17 Quần xã thực vật (Dress Lake: Plant Communities)", LakeCommunities.Dress),
                ("6/17 Suối nguồn (Dress Lake: Spring)", LakeSpring.Dress),
                ("7/17 Nấm và dây leo (Dress Lake: Mushrooms and Vines)", LakeFlora.Dress),
                ("8/17 Mái lưới rễ (Dress Lake: Root Canopy)", LakeCanopy.Dress),
                ("9/17 Điểm nhấn các khu (Dress Lake: Landmarks)", LakeLandmarks.Dress),
                ("10/17 Rễ và ánh sáng (Dress Lake: Roots and Light)", LakeRootsLight.Dress),
                ("11/17 Thành phố người cây (Dress Lake: Treefolk City)", LakeTreefolkCity.Dress),
                ("12/17 Nhà các class (Dress Lake: Class Houses)", LakeClassHouses.Dress),
                ("13/17 Người cây (Dress Lake: Treefolk)", LakeTreefolk.Dress),
                ("14/17 Vật thể Blender (Dress Lake: Props)", LakeProps.Dress),
                ("15/17 Điểm nhấn Blender (Dress Lake: Landmark Props)", LakeLandmarkProps.Dress),
                ("16/17 Ánh sáng và nước (Dress Lake: Light and Water Look)", LakeLook.Dress),
                ("17/17 Mặt trời lọt kẽ rễ (Dress Lake: Dappled Sun)", LakeSun.Dress),
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
