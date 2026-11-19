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
        [MenuItem("Evolve/Bản đồ cũ (Underroot)/Rebuild Lake (all steps)")]
        public static void Run()
        {
            var steps = new (string name, Action run)[]
            {
                ("1/25 Dựng scene hồ (Build Underroot Lake)", UnderrootLakeBuilder.Build),
                ("2/25 Nước (Dress Lake: Water)", LakeWater.Dress),
                ("3/25 Nền địa hình (Dress Lake: Terrain)", LakeTerrain.Dress),
                ("4/25 Chi tiết 3D mặt đất (Dress Lake: Ground Clutter)", LakeClutter.Dress),
                ("5/25 Mặt đất (Dress Lake: Ground)", LakeGround.Dress),
                ("6/25 Quần xã thực vật (Dress Lake: Plant Communities)", LakeCommunities.Dress),
                ("7/25 Suối nguồn (Dress Lake: Spring)", LakeSpring.Dress),
                ("8/25 Nấm và dây leo (Dress Lake: Mushrooms and Vines)", LakeFlora.Dress),
                ("9/25 Mái lưới rễ (Dress Lake: Root Canopy)", LakeCanopy.Dress),
                ("10/25 Vòm rễ (Dress Lake: Root Vault)", LakeVault.Dress),
                ("11/25 Điểm nhấn các khu (Dress Lake: Landmarks)", LakeLandmarks.Dress),
                ("12/25 Rễ và ánh sáng (Dress Lake: Roots and Light)", LakeRootsLight.Dress),
                ("13/25 Thành phố người cây (Dress Lake: Treefolk City)", LakeTreefolkCity.Dress),
                ("14/25 Nhà các class (Dress Lake: Class Houses)", LakeClassHouses.Dress),
                ("15/25 Người cây (Dress Lake: Treefolk)", LakeTreefolk.Dress),
                ("16/25 Vật thể Blender (Dress Lake: Props)", LakeProps.Dress),
                ("17/25 Điểm nhấn Blender (Dress Lake: Landmark Props)", LakeLandmarkProps.Dress),
                ("18/25 Ánh sáng và nước (Dress Lake: Light and Water Look)", LakeLook.Dress),
                ("19/25 Mặt trời lọt kẽ rễ (Dress Lake: Dappled Sun)", LakeSun.Dress),
                ("20/25 Sự sống tầng đất (Dress Lake: Ground Life)", LakeGroundLife.Dress),
                ("21/25 Thực vật và quặng hấp thụ được (Dress Lake: Forage)", LakeForage.Dress),
                ("22/25 Thảm cỏ (Dress Lake: Grass)", LakeGrass.Dress),
                ("23/25 Ánh sáng thực tế hơn (Dress Lake: Realism)", LakeRealism.Dress),
                ("24/25 Nước thật hơn (Dress Lake: Water Realism)", LakeWaterRealism.Dress),
                ("25/25 Hố lộ thiên: bầu trời, nắng, thế giới trên miệng hố (Dress Lake: Open Sky)", LakeOpenSky.Dress),
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
