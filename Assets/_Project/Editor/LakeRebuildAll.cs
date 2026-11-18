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
                ("1/22 Dựng scene hồ (Build Underroot Lake)", UnderrootLakeBuilder.Build),
                ("2/22 Nước (Dress Lake: Water)", LakeWater.Dress),
                ("3/22 Nền địa hình (Dress Lake: Terrain)", LakeTerrain.Dress),
                ("4/22 Chi tiết 3D mặt đất (Dress Lake: Ground Clutter)", LakeClutter.Dress),
                ("5/22 Mặt đất (Dress Lake: Ground)", LakeGround.Dress),
                ("6/22 Quần xã thực vật (Dress Lake: Plant Communities)", LakeCommunities.Dress),
                ("7/22 Suối nguồn (Dress Lake: Spring)", LakeSpring.Dress),
                ("8/22 Nấm và dây leo (Dress Lake: Mushrooms and Vines)", LakeFlora.Dress),
                ("9/22 Mái lưới rễ (Dress Lake: Root Canopy)", LakeCanopy.Dress),
                ("10/22 Vòm rễ (Dress Lake: Root Vault)", LakeVault.Dress),
                ("11/22 Điểm nhấn các khu (Dress Lake: Landmarks)", LakeLandmarks.Dress),
                ("12/22 Rễ và ánh sáng (Dress Lake: Roots and Light)", LakeRootsLight.Dress),
                ("13/22 Thành phố người cây (Dress Lake: Treefolk City)", LakeTreefolkCity.Dress),
                ("14/22 Nhà các class (Dress Lake: Class Houses)", LakeClassHouses.Dress),
                ("15/22 Người cây (Dress Lake: Treefolk)", LakeTreefolk.Dress),
                ("16/22 Vật thể Blender (Dress Lake: Props)", LakeProps.Dress),
                ("17/22 Điểm nhấn Blender (Dress Lake: Landmark Props)", LakeLandmarkProps.Dress),
                ("18/22 Ánh sáng và nước (Dress Lake: Light and Water Look)", LakeLook.Dress),
                ("19/22 Mặt trời lọt kẽ rễ (Dress Lake: Dappled Sun)", LakeSun.Dress),
                ("20/22 Sự sống tầng đất (Dress Lake: Ground Life)", LakeGroundLife.Dress),
                ("21/22 Thảm cỏ (Dress Lake: Grass)", LakeGrass.Dress),
                ("22/22 Ánh sáng thực tế hơn (Dress Lake: Realism)", LakeRealism.Dress),
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
