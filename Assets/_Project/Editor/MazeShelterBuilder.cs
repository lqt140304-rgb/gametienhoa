using System.Collections.Generic;
using Evolve.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Evolve.EditorTools
{
    /// <summary>
    /// Dựng Hang Trú thật: hốc vòm đá tự nhiên (không dùng Cube) nằm cao hơn mực lũ,
    /// lối vào hẹp giữa các tảng đá (chặn sinh vật lớn), thảm rêu ẩm, cỏ mọc và ShelterZone bên trong.
    /// </summary>
    public static class MazeShelterBuilder
    {
        const string ModelFolder = "Assets/_Project/Models/Underroot/";

        public static GameObject BuildRealShelter(
            Transform parent,
            string name,
            Vector3 groundFootPos,
            Vector3 inwardDirection,
            Material rockMaterial,
            Material mossMaterial,
            float ledgeHeight = 5.6f,
            bool placeOnNaturalHill = false)
        {
            inwardDirection.y = 0f;
            if (inwardDirection.sqrMagnitude < 0.001f) inwardDirection = Vector3.forward;
            inwardDirection.Normalize();

            var shelterRoot = new GameObject(name);
            shelterRoot.transform.SetParent(parent, false);
            shelterRoot.transform.position = groundFootPos + Vector3.up * (placeOnNaturalHill ? 0f : ledgeHeight);
            shelterRoot.transform.rotation = Quaternion.LookRotation(inwardDirection, Vector3.up);

            // 1. Nền đá tự nhiên hoặc gò đá bậc thang (khi đặt ở vách Hang Khởi Sinh)
            if (!placeOnNaturalHill)
            {
                // Nền đá tròn tự nhiên làm bệ hang trú
                var baseDisc = new GameObject("Nền Đá Hang Trú");
                baseDisc.transform.SetParent(shelterRoot.transform, false);
                baseDisc.transform.localPosition = new Vector3(0f, -0.4f, 1.5f);
                var discMesh = CaveMeshes.Disc(4.2f, 0.45f, 14);
                baseDisc.AddComponent<MeshFilter>().sharedMesh = discMesh;
                baseDisc.AddComponent<MeshRenderer>().sharedMaterial = rockMaterial;
                baseDisc.AddComponent<MeshCollider>().sharedMesh = discMesh;

                // Bậc đá tảng tự nhiên xếp tầng dẫn từ chân đồi lên cửa hang trú
                for (int step = 0; step < 5; step++)
                {
                    float t = step / 4f;
                    Vector3 stepPos = new Vector3(
                        (step % 2 == 0 ? -0.35f : 0.35f),
                        Mathf.Lerp(-ledgeHeight + 0.1f, -0.6f, t),
                        Mathf.Lerp(-5.5f, -1.3f, t));
                    MazeTerrainMesh.PlaceFramingBoulder(shelterRoot.transform, stepPos, 2.2f - 0.15f * step, 400 + step, rockMaterial);
                }

                // Tảng đá đỡ bên dưới hốc hang tránh cảm giác bay lơ lửng
                MazeTerrainMesh.PlaceFramingBoulder(shelterRoot.transform, new Vector3(0f, -ledgeHeight * 0.55f, 1.8f), 4.8f, 499, rockMaterial);
            }

            // 2. Vòm đá tự nhiên bao bọc hốc hang trú (CaveMeshes.Shell hai mặt + MeshCollider)
            var shellGo = new GameObject("Vòm Đá Hang Trú");
            shellGo.transform.SetParent(shelterRoot.transform, false);
            shellGo.transform.localPosition = new Vector3(0f, -0.25f, 1.8f);
            var shellMesh = CaveMeshes.Shell(3.2f, 3.4f, 2.8f, 18, 8, 303, 0.14f);
            shellGo.AddComponent<MeshFilter>().sharedMesh = shellMesh;
            var shellMr = shellGo.AddComponent<MeshRenderer>();
            shellMr.sharedMaterial = rockMaterial;
            shellMr.shadowCastingMode = ShadowCastingMode.TwoSided;

            // Vòng đá tảng tự nhiên tạo vách sau và hẹp cửa vào (cửa hẹp ~1.4m rộng, 1.3m cao)
            MazeTerrainMesh.PlaceFramingBoulder(shelterRoot.transform, new Vector3(-2.3f, 0.8f, 1.8f), 1.9f, 501, rockMaterial);
            MazeTerrainMesh.PlaceFramingBoulder(shelterRoot.transform, new Vector3(2.3f, 0.8f, 1.8f), 1.9f, 502, rockMaterial);
            MazeTerrainMesh.PlaceFramingBoulder(shelterRoot.transform, new Vector3(0f, 1.1f, 4.2f), 2.5f, 503, rockMaterial);
            // Hai tảng đá bóp hẹp cửa vào -> quái lớn không chui lọt
            MazeTerrainMesh.PlaceFramingBoulder(shelterRoot.transform, new Vector3(-1.35f, 0.65f, -0.6f), 1.25f, 504, rockMaterial);
            MazeTerrainMesh.PlaceFramingBoulder(shelterRoot.transform, new Vector3(1.35f, 0.65f, -0.6f), 1.25f, 505, rockMaterial);
            MazeTerrainMesh.PlaceFramingBoulder(shelterRoot.transform, new Vector3(0f, 2.0f, -0.55f), 1.45f, 506, rockMaterial);

            // 3. Thảm rêu ẩm bên trong (ưu tiên FBX Flora_Moss hoặc Disc rêu mềm)
            var mossFbx = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFolder + "Flora_Moss_0.fbx");
            if (mossFbx != null)
            {
                for (int m = 0; m < 4; m++)
                {
                    float a = m * 90f * Mathf.Deg2Rad;
                    var mossGo = (GameObject)PrefabUtility.InstantiatePrefab(mossFbx, shelterRoot.transform);
                    mossGo.name = $"Rêu Hang Trú_{m}";
                    mossGo.transform.localPosition = new Vector3(Mathf.Cos(a) * 0.9f, 0.02f, 1.8f + Mathf.Sin(a) * 0.9f);
                    mossGo.transform.localRotation = Quaternion.Euler(0f, m * 75f, 0f);
                    mossGo.transform.localScale = Vector3.one * 1.4f;
                }
            }

            var mossBed = new GameObject("Thảm Rêu Ẩm");
            mossBed.transform.SetParent(shelterRoot.transform, false);
            mossBed.transform.localPosition = new Vector3(0f, 0.01f, 1.8f);
            mossBed.AddComponent<MeshFilter>().sharedMesh = CaveMeshes.Disc(2.1f, 0.04f, 14);
            mossBed.AddComponent<MeshRenderer>().sharedMaterial = mossMaterial;

            // 4. Ánh sáng dịu của rêu
            var light = new GameObject("Ánh Sáng Hang Trú").AddComponent<Light>();
            light.transform.SetParent(shelterRoot.transform, false);
            light.transform.localPosition = new Vector3(0f, 1.5f, 1.8f);
            light.type = LightType.Point;
            light.color = new Color(0.50f, 0.95f, 0.58f);
            light.range = 7.5f;
            light.intensity = 2.4f;
            light.shadows = LightShadows.None;

            // 5. Vùng kích hoạt ShelterZone (giữ nguyên cơ chế ShelterZone + ShelterSensor)
            var zoneObj = new GameObject("ShelterZone");
            zoneObj.transform.SetParent(shelterRoot.transform, false);
            zoneObj.transform.localPosition = new Vector3(0f, 0.9f, 1.8f);
            var col = zoneObj.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(3.2f, 2.2f, 3.4f);
            var zone = zoneObj.AddComponent<ShelterZone>();
            zone.MaxBodyScale = 1.25f;

            return shelterRoot;
        }
    }
}
