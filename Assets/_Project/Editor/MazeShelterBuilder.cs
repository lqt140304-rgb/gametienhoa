using Evolve.World;
using UnityEngine;

namespace Evolve.EditorTools
{
    /// <summary>
    /// Dựng Hang Trú thật: hốc hang nhỏ nằm cao hơn mực lũ, có dốc dẫn lên, lối vào hẹp (chặn sinh vật lớn),
    /// thảm rêu ẩm và ShelterZone bên trong (giữ nguyên cơ chế ShelterZone + ShelterSensor).
    /// </summary>
    public static class MazeShelterBuilder
    {
        public static GameObject BuildRealShelter(
            Transform parent,
            string name,
            Vector3 groundFootPos,
            Vector3 inwardDirection,
            Material rockMaterial,
            Material mossMaterial,
            float ledgeHeight = 5.6f)
        {
            inwardDirection.y = 0f;
            if (inwardDirection.sqrMagnitude < 0.001f) inwardDirection = Vector3.forward;
            inwardDirection.Normalize();

            var shelterRoot = new GameObject(name);
            shelterRoot.transform.SetParent(parent, false);
            shelterRoot.transform.position = groundFootPos + Vector3.up * ledgeHeight;
            shelterRoot.transform.rotation = Quaternion.LookRotation(inwardDirection, Vector3.up);

            // 1. Nền đá gờ cao & dốc dẫn từ mặt đất lên cửa hang
            CreateBlock(shelterRoot.transform, "Nền Hang Trú", new Vector3(0f, -0.25f, 1.5f), new Vector3(5.2f, 0.5f, 5.5f), Vector3.zero, rockMaterial);
            CreateBlock(shelterRoot.transform, "Dốc Lên Hang Trú", new Vector3(0f, -ledgeHeight * 0.5f, -3.2f), new Vector3(2.4f, 0.5f, ledgeHeight * 1.45f), new Vector3(-38f, 0f, 0f), rockMaterial);

            // 2. Vách đá tạo hốc hang kín gió và lối vào hẹp (cửa rộng ~1.5 m, cao ~1.35 m -> quái to không lọt)
            CreateBlock(shelterRoot.transform, "Vách Trái", new Vector3(-2.1f, 1.2f, 1.8f), new Vector3(1.0f, 2.6f, 4.4f), Vector3.zero, rockMaterial);
            CreateBlock(shelterRoot.transform, "Vách Phải", new Vector3(2.1f, 1.2f, 1.8f), new Vector3(1.0f, 2.6f, 4.4f), Vector3.zero, rockMaterial);
            CreateBlock(shelterRoot.transform, "Vách Sau", new Vector3(0f, 1.2f, 3.8f), new Vector3(5.2f, 2.6f, 0.8f), Vector3.zero, rockMaterial);
            CreateBlock(shelterRoot.transform, "Mái Hang", new Vector3(0f, 2.6f, 1.8f), new Vector3(5.4f, 0.7f, 4.8f), new Vector3(6f, 0f, 0f), rockMaterial);

            // Hai trụ đá bóp hẹp cửa vào
            CreateBlock(shelterRoot.transform, "Trụ Cửa Trái", new Vector3(-1.3f, 0.9f, -0.4f), new Vector3(1.1f, 1.9f, 0.8f), Vector3.zero, rockMaterial);
            CreateBlock(shelterRoot.transform, "Trụ Cửa Phải", new Vector3(1.3f, 0.9f, -0.4f), new Vector3(1.1f, 1.9f, 0.8f), Vector3.zero, rockMaterial);
            CreateBlock(shelterRoot.transform, "Mí Cửa Trên", new Vector3(0f, 1.85f, -0.4f), new Vector3(3.6f, 0.9f, 0.8f), Vector3.zero, rockMaterial);

            // 3. Thảm rêu ẩm bên trong
            var moss = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            moss.name = "Rêu Ẩm";
            Object.DestroyImmediate(moss.GetComponent<Collider>());
            moss.transform.SetParent(shelterRoot.transform, false);
            moss.transform.localPosition = new Vector3(0f, 0.03f, 1.8f);
            moss.transform.localScale = new Vector3(3.0f, 0.03f, 3.0f);
            if (mossMaterial != null) moss.GetComponent<Renderer>().sharedMaterial = mossMaterial;

            // 4. Ánh sáng dịu của rêu
            var light = new GameObject("Ánh Sáng Hang Trú").AddComponent<Light>();
            light.transform.SetParent(shelterRoot.transform, false);
            light.transform.localPosition = new Vector3(0f, 1.5f, 1.8f);
            light.type = LightType.Point;
            light.color = new Color(0.50f, 0.95f, 0.58f);
            light.range = 6.5f;
            light.intensity = 2.2f;
            light.shadows = LightShadows.None;

            // 5. Vùng kích hoạt ShelterZone (giữ nguyên cơ chế ShelterZone + ShelterSensor)
            var zoneObj = new GameObject("ShelterZone");
            zoneObj.transform.SetParent(shelterRoot.transform, false);
            zoneObj.transform.localPosition = new Vector3(0f, 0.9f, 1.8f);
            var col = zoneObj.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(3.0f, 2.0f, 3.2f);
            var zone = zoneObj.AddComponent<ShelterZone>();
            zone.MaxBodyScale = 1.25f;

            return shelterRoot;
        }

        static void CreateBlock(Transform parent, string name, Vector3 localPos, Vector3 scale, Vector3 euler, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            go.transform.localEulerAngles = euler;
            if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
        }
    }
}
