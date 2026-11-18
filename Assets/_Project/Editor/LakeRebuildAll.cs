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
                ("1/21 Dựng scene hồ (Build Underroot Lake)", UnderrootLakeBuilder.Build),
                ("2/21 Nước (Dress Lake: Water)", LakeWater.Dress),
                ("3/21 Nền địa hình (Dress Lake: Terrain)", LakeTerrain.Dress),
                ("4/21 Chi tiết 3D mặt đất (Dress Lake: Ground Clutter)", LakeClutter.Dress),
                ("5/21 Mặt đất (Dress Lake: Ground)", LakeGround.Dress),
                ("6/21 Quần xã thực vật (Dress Lake: Plant Communities)", LakeCommunities.Dress),
                ("7/21 Suối nguồn (Dress Lake: Spring)", LakeSpring.Dress),
                ("8/21 Nấm và dây leo (Dress Lake: Mushrooms and Vines)", LakeFlora.Dress),
                ("9/21 Mái lưới rễ (Dress Lake: Root Canopy)", LakeCanopy.Dress),
                ("10/21 Vòm rễ (Dress Lake: Root Vault)", LakeVault.Dress),
                ("11/21 Điểm nhấn các khu (Dress Lake: Landmarks)", LakeLandmarks.Dress),
                ("12/21 Rễ và ánh sáng (Dress Lake: Roots and Light)", LakeRootsLight.Dress),
                ("13/21 Thành phố người cây (Dress Lake: Treefolk City)", LakeTreefolkCity.Dress),
                ("14/21 Nhà các class (Dress Lake: Class Houses)", LakeClassHouses.Dress),
                ("15/21 Người cây (Dress Lake: Treefolk)", LakeTreefolk.Dress),
                ("16/21 Vật thể Blender (Dress Lake: Props)", LakeProps.Dress),
                ("17/21 Điểm nhấn Blender (Dress Lake: Landmark Props)", LakeLandmarkProps.Dress),
                ("18/21 Ánh sáng và nước (Dress Lake: Light and Water Look)", LakeLook.Dress),
                ("19/21 Mặt trời lọt kẽ rễ (Dress Lake: Dappled Sun)", LakeSun.Dress),
                ("20/21 Sự sống tầng đất (Dress Lake: Ground Life)", LakeGroundLife.Dress),
                ("21/21 Ánh sáng thực tế hơn (Dress Lake: Realism)", LakeRealism.Dress),
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
