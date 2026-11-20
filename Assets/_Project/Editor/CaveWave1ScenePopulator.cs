using System.Collections.Generic;
using Evolve.Creatures;
using Evolve.Ecology;
using Evolve.Maze;
using Evolve.World;
using UnityEditor;
using UnityEngine;

namespace Evolve.EditorTools
{
    /// <summary>
    /// Đặt Hệ Sinh Thái (Region, Den, Spawner quái Đợt 1), LightShafts và Kỳ Ngộ
    /// cho 3 hang đầu tiên: Hang Khởi Sinh, Vườn Quặng Cầu Vồng, Vực Nứt Tím.
    /// </summary>
    public static class CaveWave1ScenePopulator
    {
        public static void PopulateBirthCavern(Transform chamberRoot, Vector3 origin)
        {
            PopulateEcosystem(
                chamberRoot,
                origin,
                null,
                isBirth: true,
                speciesNames: new[] { "BoCanhCungNho", "OcHo", "ThanLanBoHo", "CocHoGia" },
                denOffsets: new[]
                {
                    new Vector2(-13f, 8f),
                    new Vector2(14f, 6f),
                    new Vector2(-16f, -11f),
                    new Vector2(15f, -12f)
                },
                counts: new[] { 7, 7, 2, 1 });

            // Kỳ ngộ 1: Tia Nắng Đầu Tiên (ngay vùng tia nắng qua lỗ trần trên mặt hồ)
            var sunGo = new GameObject("Kỳ Ngộ - Tia Nắng Đầu Tiên");
            sunGo.transform.SetParent(chamberRoot, false);
            sunGo.transform.position = new Vector3(0f, CavernLayout.GroundHeight(0f, 0f) + 0.5f, 0f);
            var sunEnc = sunGo.AddComponent<FirstSunbeamEncounter>();
            sunEnc.Configure(6.0f, 28f);
        }

        public static void PopulateLumaChamber(Transform chamberRoot, Vector3 origin, MeshCollider floorCollider)
        {
            PopulateEcosystem(
                chamberRoot,
                origin,
                floorCollider,
                isBirth: false,
                speciesNames: new[] { "DomDomSacMau", "ThoLa", "BoGiapQuang", "RuaPhaLe" },
                denOffsets: new[]
                {
                    new Vector2(-18f, 12f),
                    new Vector2(20f, 10f),
                    new Vector2(-16f, -16f),
                    new Vector2(12f, -18f)
                },
                counts: new[] { 7, 7, 2, 1 });

            // LightShafts.Register cho Vườn Quặng Cầu Vồng
            var shaftHost = chamberRoot.gameObject.AddComponent<ChamberLightShaftRegistrar>();
            shaftHost.Configure(new List<ChamberLightShaftRegistrar.ShaftSpec>
            {
                new ChamberLightShaftRegistrar.ShaftSpec { localCenter = new Vector3(0f, 0f, 0f), radius = 8f, topLocalY = 26f },
                new ChamberLightShaftRegistrar.ShaftSpec { localCenter = new Vector3(-18f, 0f, 14f), radius = 6f, topLocalY = 24f },
                new ChamberLightShaftRegistrar.ShaftSpec { localCenter = new Vector3(16f, 0f, -12f), radius = 6f, topLocalY = 24f },
            });

            // Kỳ ngộ 2: Cầu Vồng Trong Thân (5 mảnh quặng 5 màu + suối rửa)
            var encGo = new GameObject("Kỳ Ngộ - Cầu Vồng Trong Thân");
            encGo.transform.SetParent(chamberRoot, false);
            encGo.transform.localPosition = Vector3.zero;
            var encounter = encGo.AddComponent<RainbowInBodyEncounter>();

            Color[] oreColors =
            {
                new Color(0.95f, 0.22f, 0.24f), // Đỏ
                new Color(0.98f, 0.68f, 0.14f), // Cam Vàng
                new Color(0.20f, 0.88f, 0.42f), // Lục Bảo
                new Color(0.18f, 0.72f, 0.98f), // Lam Ngọc
                new Color(0.72f, 0.28f, 0.96f), // Tím Thạch Anh
            };
            Vector2[] oreOffsets =
            {
                new Vector2(-22f, -8f),
                new Vector2(-10f, 18f),
                new Vector2(14f, 16f),
                new Vector2(24f, -6f),
                new Vector2(2f, -22f),
            };
            for (int i = 0; i < 5; i++)
            {
                Vector3 pos = SnapToFloor(origin, oreOffsets[i].x, oreOffsets[i].y, floorCollider, false) + Vector3.up * 0.45f;
                var ore = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ore.name = $"Quặng Màu #{i + 1}";
                ore.transform.SetParent(encGo.transform, false);
                ore.transform.position = pos;
                ore.transform.localScale = new Vector3(0.85f, 1.15f, 0.85f);
                ore.transform.localRotation = Quaternion.Euler(14f * i, 36f * i, 10f);
                var r = ore.GetComponent<Renderer>();
                if (r != null) r.sharedMaterial = LakeGround.OpaqueMaterial($"RainbowOre_{i}", oreColors[i], 0.15f, false, oreColors[i] * 0.7f);
                ore.AddComponent<RainbowOrePiece>().Configure(i, encounter);
            }

            var streamZone = new GameObject("Vùng Suối Cầu Vồng");
            streamZone.transform.SetParent(encGo.transform, false);
            streamZone.transform.position = SnapToFloor(origin, -4f, 6f, floorCollider, false);
            var box = streamZone.AddComponent<BoxCollider>();
            box.size = new Vector3(20f, 5f, 14f);
            box.isTrigger = true;
            streamZone.AddComponent<RainbowStreamZone>().Configure(encounter);
        }

        public static void PopulateCrystalChamber(Transform chamberRoot, Vector3 origin, MeshCollider floorCollider)
        {
            PopulateEcosystem(
                chamberRoot,
                origin,
                floorCollider,
                isBirth: false,
                speciesNames: new[] { "DoiAmThoa", "BoGaiTim", "NhenTinhThe", "BoCapThachAnh" },
                denOffsets: new[]
                {
                    new Vector2(-26f, -16f),
                    new Vector2(18f, -18f),
                    new Vector2(-18f, 18f),
                    new Vector2(26f, 14f)
                },
                counts: new[] { 7, 7, 2, 1 });

            // LightShafts.Register cho Vực Nứt Tím
            var shaftHost = chamberRoot.gameObject.AddComponent<ChamberLightShaftRegistrar>();
            shaftHost.Configure(new List<ChamberLightShaftRegistrar.ShaftSpec>
            {
                new ChamberLightShaftRegistrar.ShaftSpec { localCenter = new Vector3(-12f, 0f, 8f), radius = 7f, topLocalY = 30f },
                new ChamberLightShaftRegistrar.ShaftSpec { localCenter = new Vector3(6f, 0f, -4f), radius = 6f, topLocalY = 28f },
            });

            // Kỳ ngộ 3: Hợp Âm Tinh Thể (3 tinh thể Trầm -> Trung -> Cao trúng Sóng Dò)
            var encGo = new GameObject("Kỳ Ngộ - Hợp Âm Tinh Thể");
            encGo.transform.SetParent(chamberRoot, false);
            encGo.transform.localPosition = Vector3.zero;
            var encounter = encGo.AddComponent<CrystalChordEncounter>();

            Vector2[] resonatorOffsets =
            {
                new Vector2(-24f, -18f), // 0: Trầm
                new Vector2(4f, -20f),   // 1: Trung
                new Vector2(24f, 16f),   // 2: Cao
            };
            string[] labels = { "Trầm", "Trung", "Cao" };
            Color[] colors =
            {
                new Color(0.45f, 0.14f, 0.72f),
                new Color(0.70f, 0.20f, 0.92f),
                new Color(0.95f, 0.35f, 0.98f),
            };
            for (int i = 0; i < 3; i++)
            {
                Vector3 pos = SnapToFloor(origin, resonatorOffsets[i].x, resonatorOffsets[i].y, floorCollider, false) + Vector3.up * 1.4f;
                var pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pillar.name = $"Tinh Thể Cộng Hưởng ({labels[i]})";
                pillar.transform.SetParent(encGo.transform, false);
                pillar.transform.position = pos;
                pillar.transform.localScale = new Vector3(1.2f, 3.2f + i * 0.5f, 1.2f);
                pillar.transform.localRotation = Quaternion.Euler(8f, 30f * i, -6f);
                var r = pillar.GetComponent<Renderer>();
                if (r != null) r.sharedMaterial = LakeGround.OpaqueMaterial($"ChordCrystal_{i}", colors[i], 0.15f, false, colors[i] * 0.55f);
                pillar.AddComponent<CrystalChordResonator>().Configure(i, encounter);
            }

            // Tinh thể lõi lộ ra sau khi nứt toác
            Vector3 corePos = SnapToFloor(origin, -4f, -16f, floorCollider, false) + Vector3.up * 0.8f;
            var coreGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            coreGo.name = "Tinh Thể Lõi (Phần Thưởng)";
            coreGo.transform.SetParent(encGo.transform, false);
            coreGo.transform.position = corePos;
            coreGo.transform.localScale = new Vector3(1.1f, 1.6f, 1.1f);
            coreGo.transform.localRotation = Quaternion.Euler(25f, 45f, 15f);
            var coreRend = coreGo.GetComponent<Renderer>();
            if (coreRend != null) coreRend.sharedMaterial = LakeGround.OpaqueMaterial("ChordCoreCrystal", new Color(0.96f, 0.42f, 1f), 0.10f, false, new Color(0.9f, 0.35f, 1f) * 1.1f);
            coreGo.AddComponent<CrystalCorePickup>().Configure(encounter);

            var coreLightGo = new GameObject("Ánh Sáng Tinh Thể Lõi");
            coreLightGo.transform.SetParent(coreGo.transform, false);
            var coreLight = coreLightGo.AddComponent<Light>();
            coreLight.type = LightType.Point;
            coreLight.range = 16f;
            coreLight.intensity = 18f;
            coreLight.color = new Color(0.92f, 0.36f, 1f);

            encounter.Configure(coreGo, coreLight);
        }

        static void PopulateEcosystem(
            Transform chamberRoot,
            Vector3 origin,
            MeshCollider floorCollider,
            bool isBirth,
            string[] speciesNames,
            Vector2[] denOffsets,
            int[] counts)
        {
            var ecoRoot = new GameObject("Hệ Sinh Thái Hang");
            ecoRoot.transform.SetParent(chamberRoot, false);
            ecoRoot.transform.localPosition = Vector3.zero;

            var region = ecoRoot.AddComponent<Region>();
            var speciesList = new List<SpeciesSO>();
            for (int i = 0; i < speciesNames.Length; i++)
            {
                var so = CaveCreaturesWave1Builder.LoadSpeciesSO(speciesNames[i]);
                if (so != null) speciesList.Add(so);
            }

            var regSo = new SerializedObject(region);
            regSo.FindProperty("size").vector3Value = new Vector3(135f, 42f, 135f);
            regSo.FindProperty("activateRadius").floatValue = 72f;
            regSo.FindProperty("deactivateRadius").floatValue = 95f;
            var spArr = regSo.FindProperty("species");
            spArr.arraySize = speciesList.Count;
            for (int i = 0; i < speciesList.Count; i++)
                spArr.GetArrayElementAtIndex(i).objectReferenceValue = speciesList[i];
            regSo.ApplyModifiedPropertiesWithoutUndo();

            for (int i = 0; i < speciesNames.Length; i++)
            {
                string spName = speciesNames[i];
                Vector2 off = denOffsets[i % denOffsets.Length];
                Vector3 pos = SnapToFloor(origin, off.x, off.y, floorCollider, isBirth);

                // Đặt Tổ (Den)
                var denGo = new GameObject($"Tổ ({spName})");
                denGo.transform.SetParent(ecoRoot.transform, false);
                denGo.transform.position = pos;
                var den = denGo.AddComponent<Den>();
                den.Species = CaveCreaturesWave1Builder.LoadSpeciesSO(spName);
                den.Region = region;

                // Spawner trực tiếp để khoang có sinh vật ngay khi bước vào
                var spec = FindSpec(spName);
                if (!spec.IsPredator)
                {
                    var preyPrefab = CaveCreaturesWave1Builder.LoadWave1CreaturePrefab<Prey>(spName);
                    if (preyPrefab != null)
                    {
                        var spawnerGo = new GameObject($"Bầy ({spName})");
                        spawnerGo.transform.SetParent(ecoRoot.transform, false);
                        spawnerGo.transform.position = pos;
                        var spSo = new SerializedObject(spawnerGo.AddComponent<PreySpawner>());
                        spSo.FindProperty("prefab").objectReferenceValue = preyPrefab;
                        spSo.FindProperty("count").intValue = counts[i];
                        spSo.FindProperty("radius").floatValue = 16f;
                        spSo.FindProperty("minDistanceFromCenter").floatValue = 2f;
                        spSo.ApplyModifiedPropertiesWithoutUndo();
                    }
                }
                else
                {
                    var predPrefab = CaveCreaturesWave1Builder.LoadWave1CreaturePrefab<Predator>(spName);
                    if (predPrefab != null)
                    {
                        var spawnerGo = new GameObject($"Lãnh Địa ({spName})");
                        spawnerGo.transform.SetParent(ecoRoot.transform, false);
                        spawnerGo.transform.position = pos;
                        var spSo = new SerializedObject(spawnerGo.AddComponent<PredatorSpawner>());
                        spSo.FindProperty("prefab").objectReferenceValue = predPrefab;
                        spSo.FindProperty("count").intValue = counts[i];
                        spSo.FindProperty("radius").floatValue = spec.IsLeader ? 8f : 18f;
                        spSo.ApplyModifiedPropertiesWithoutUndo();
                    }
                }
            }
        }

        public static void PopulateBreathingMossChamber(Transform chamberRoot, Vector3 origin)
        {
            PopulateEcosystem(
                chamberRoot,
                origin,
                null,
                isBirth: false,
                speciesNames: new[] { "SenReu", "MuoiReu", "RetReu", "ReuMe" },
                denOffsets: new[]
                {
                    new Vector2(-18f, 12f),
                    new Vector2(18f, 10f),
                    new Vector2(-14f, -18f),
                    new Vector2(14f, -14f)
                },
                counts: new[] { 7, 7, 2, 1 });

            var shaftHost = chamberRoot.gameObject.AddComponent<ChamberLightShaftRegistrar>();
            shaftHost.Configure(new List<ChamberLightShaftRegistrar.ShaftSpec>
            {
                new ChamberLightShaftRegistrar.ShaftSpec { localCenter = new Vector3(0f, 0f, 0f), radius = 7f, topLocalY = 24f },
            });

            // Kỳ ngộ: Hơi Thở Của Rêu (thảm rêu phát sáng theo nhịp thở)
            var mossGo = new GameObject("Kỳ Ngộ - Hơi Thở Của Rêu");
            mossGo.transform.SetParent(chamberRoot, false);
            mossGo.transform.position = SnapToFloor(origin, 0f, 0f, null, false);
            var pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pad.name = "Thảm Rêu Thở";
            pad.transform.SetParent(mossGo.transform, false);
            pad.transform.localScale = new Vector3(9f, 0.25f, 9f);
            var padRend = pad.GetComponent<Renderer>();
            if (padRend != null) padRend.sharedMaterial = LakeGround.OpaqueMaterial("BreathingMossPad", new Color(0.22f, 0.86f, 0.50f), 0.2f, false, new Color(0.18f, 0.72f, 0.42f));
            var col = mossGo.AddComponent<SphereCollider>();
            col.radius = 5.0f;
            col.isTrigger = true;
            var light = new GameObject("Ánh Sáng Rêu Thở").AddComponent<Light>();
            light.transform.SetParent(mossGo.transform, false);
            light.transform.localPosition = Vector3.up * 3.5f;
            light.type = LightType.Point;
            light.range = 24f;
            light.color = new Color(0.35f, 0.96f, 0.60f);
            mossGo.AddComponent<BreathingMossEncounter>().Configure(light, padRend);
        }

        public static void PopulateAbandonedNestChamber(Transform chamberRoot, Vector3 origin)
        {
            PopulateEcosystem(
                chamberRoot,
                origin,
                null,
                isBirth: false,
                speciesNames: new[] { "ChimNon", "RanTromTrung", "ChimHangMe" },
                denOffsets: new[]
                {
                    new Vector2(-16f, 10f),
                    new Vector2(18f, -14f),
                    new Vector2(0f, 20f)
                },
                counts: new[] { 7, 2, 1 });

            var shaftHost = chamberRoot.gameObject.AddComponent<ChamberLightShaftRegistrar>();
            shaftHost.Configure(new List<ChamberLightShaftRegistrar.ShaftSpec>
            {
                new ChamberLightShaftRegistrar.ShaftSpec { localCenter = new Vector3(0f, 0f, 12f), radius = 6.5f, topLocalY = 26f },
            });

            // Kỳ ngộ: Quả Trứng Còn Sống trong khe tổ
            var encGo = new GameObject("Kỳ Ngộ - Quả Trứng Còn Sống");
            encGo.transform.SetParent(chamberRoot, false);
            encGo.transform.position = SnapToFloor(origin, 0f, 12f, null, false);
            var encounter = encGo.AddComponent<LivingEggEncounter>();

            var eggFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Models/Wildlife/Creature_TrungLa.fbx");
            GameObject eggObj = eggFbx != null
                ? (GameObject)PrefabUtility.InstantiatePrefab(eggFbx, encGo.transform)
                : GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eggObj.name = "Trứng Lạ";
            eggObj.transform.localPosition = Vector3.zero;
            eggObj.transform.localScale = Vector3.one * 1.4f;
            var sc = eggObj.AddComponent<SphereCollider>();
            sc.radius = 0.9f;
            sc.isTrigger = true;
            eggObj.AddComponent<LivingEggPickup>().Configure(encounter);
        }

        public static void PopulateGlowMushroomChamber(Transform chamberRoot, Vector3 origin)
        {
            PopulateEcosystem(
                chamberRoot,
                origin,
                null,
                isBirth: false,
                speciesNames: new[] { "NamDiBo", "BuomBaoTu", "EchDocDom", "NamKhongLo" },
                denOffsets: new[]
                {
                    new Vector2(-18f, 12f),
                    new Vector2(16f, 14f),
                    new Vector2(-14f, -16f),
                    new Vector2(18f, -14f)
                },
                counts: new[] { 6, 6, 2, 1 });

            var shaftHost = chamberRoot.gameObject.AddComponent<ChamberLightShaftRegistrar>();
            shaftHost.Configure(new List<ChamberLightShaftRegistrar.ShaftSpec>
            {
                new ChamberLightShaftRegistrar.ShaftSpec { localCenter = new Vector3(0f, 0f, 0f), radius = 7.5f, topLocalY = 26f },
            });

            // Kỳ ngộ: Vòng Nấm (bào tử sáng theo nhịp -> đổi Khuyết Điểm)
            var ringGo = new GameObject("Kỳ Ngộ - Vòng Nấm");
            ringGo.transform.SetParent(chamberRoot, false);
            ringGo.transform.position = SnapToFloor(origin, 0f, 0f, null, false);
            var ringRends = new Renderer[6];
            for (int i = 0; i < 6; i++)
            {
                float a = i * (Mathf.PI * 2f / 6f);
                var shroom = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                shroom.name = $"Nấm Vòng {i + 1}";
                shroom.transform.SetParent(ringGo.transform, false);
                shroom.transform.localPosition = new Vector3(Mathf.Cos(a) * 3.0f, 0.35f, Mathf.Sin(a) * 3.0f);
                shroom.transform.localScale = new Vector3(0.75f, 0.35f, 0.75f);
                var r = shroom.GetComponent<Renderer>();
                if (r != null) r.sharedMaterial = LakeGround.OpaqueMaterial("SporeRingGlow", new Color(0.32f, 0.96f, 0.88f), 0.2f, false, new Color(0.28f, 0.90f, 0.85f));
                ringRends[i] = r;
            }
            var enc = ringGo.AddComponent<SporeRingEncounter>();
            enc.RingGlowRenderers = ringRends;
        }

        public static void PopulateHotSpringChamber(Transform chamberRoot, Vector3 origin)
        {
            PopulateEcosystem(
                chamberRoot,
                origin,
                null,
                isBirth: false,
                speciesNames: new[] { "OcLuuHuynh", "CuaDaNong", "KyNhongLua", "RanDungNham" },
                denOffsets: new[]
                {
                    new Vector2(-18f, 10f),
                    new Vector2(18f, 12f),
                    new Vector2(-14f, -16f),
                    new Vector2(16f, -16f)
                },
                counts: new[] { 6, 5, 2, 1 });

            var shaftHost = chamberRoot.gameObject.AddComponent<ChamberLightShaftRegistrar>();
            shaftHost.Configure(new List<ChamberLightShaftRegistrar.ShaftSpec>
            {
                new ChamberLightShaftRegistrar.ShaftSpec { localCenter = new Vector3(0f, 0f, 0f), radius = 6.5f, topLocalY = 24f },
            });

            // Kỳ ngộ: Tôi Luyện (vũng suối nước nóng sôi theo đợt -> tăng chịu nhiệt suốt vòng)
            var poolGo = new GameObject("Kỳ Ngộ - Tôi Luyện");
            poolGo.transform.SetParent(chamberRoot, false);
            poolGo.transform.position = SnapToFloor(origin, 0f, 0f, null, false);
            var water = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            water.name = "Vũng Suối Nóng";
            water.transform.SetParent(poolGo.transform, false);
            water.transform.localScale = new Vector3(8.5f, 0.2f, 8.5f);
            var wr = water.GetComponent<Renderer>();
            if (wr != null) wr.sharedMaterial = LakeGround.OpaqueMaterial("HotSpringPool", new Color(0.95f, 0.52f, 0.22f), 0.1f, false, new Color(0.92f, 0.44f, 0.14f));
            var enc = poolGo.AddComponent<HotSpringTemperEncounter>();
            enc.SteamGlowRenderer = wr;
        }

        public static void PopulateHowlingWindChamber(Transform chamberRoot, Vector3 origin)
        {
            PopulateEcosystem(
                chamberRoot,
                origin,
                null,
                isBirth: false,
                speciesNames: new[] { "DeGio", "ChuotGio", "ChonKhe", "CaoGio" },
                denOffsets: new[]
                {
                    new Vector2(-18f, 14f),
                    new Vector2(18f, 12f),
                    new Vector2(-16f, -14f),
                    new Vector2(16f, -16f)
                },
                counts: new[] { 6, 6, 2, 1 });

            var shaftHost = chamberRoot.gameObject.AddComponent<ChamberLightShaftRegistrar>();
            shaftHost.Configure(new List<ChamberLightShaftRegistrar.ShaftSpec>
            {
                new ChamberLightShaftRegistrar.ShaftSpec { localCenter = new Vector3(0f, 0f, 0f), radius = 6.5f, topLocalY = 28f },
            });

            // Kỳ ngộ: Túi Của Kẻ Đi Trước (khe đá hẹp chỉ slime nhỏ lọt)
            var stashGo = new GameObject("Kỳ Ngộ - Túi Của Kẻ Đi Trước");
            stashGo.transform.SetParent(chamberRoot, false);
            stashGo.transform.position = SnapToFloor(origin, 6f, -6f, null, false);
            var bag = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bag.name = "Túi Kẻ Đi Trước";
            bag.transform.SetParent(stashGo.transform, false);
            bag.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            bag.transform.localScale = new Vector3(0.7f, 0.45f, 0.7f);
            var br = bag.GetComponent<Renderer>();
            if (br != null) br.sharedMaterial = LakeGround.OpaqueMaterial("PredecessorStash", new Color(0.48f, 0.72f, 0.88f), 0.3f, false, new Color(0.28f, 0.56f, 0.76f));
            stashGo.AddComponent<WindGorgeStashEncounter>();
        }

        static CaveCreaturesWave1Builder.CreatureSpec FindSpec(string pascalName)
        {
            foreach (var s in CaveCreaturesWave1Builder.Specs)
                if (s.PascalName == pascalName) return s;
            foreach (var s in CaveCreaturesWave2Builder.Specs)
                if (s.PascalName == pascalName) return s;
            foreach (var s in CaveCreaturesWave3Builder.Specs)
                if (s.PascalName == pascalName) return s;
            return default;
        }

        static Vector3 SnapToFloor(Vector3 origin, float lx, float lz, MeshCollider floorCollider, bool isBirth)
        {
            float wx = origin.x + lx;
            float wz = origin.z + lz;
            if (isBirth)
                return new Vector3(wx, CavernLayout.GroundHeight(wx, wz), wz);

            if (floorCollider != null)
            {
                Physics.SyncTransforms();
                var ray = new Ray(new Vector3(wx, origin.y + 32f, wz), Vector3.down);
                if (floorCollider.Raycast(ray, out var hit, 64f))
                    return hit.point;
            }
            return new Vector3(wx, origin.y + 0.5f, wz);
        }
    }
}
