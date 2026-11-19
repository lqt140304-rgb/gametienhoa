using System.Collections.Generic;
using Evolve.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Evolve.EditorTools
{
    /// <summary>
    /// The grass carpet of the Slime Cavern: the same real 3D grass as the old map (Evolve/GrassBlade: tapering curved blades, dark at the root, bright at the tip,
    /// swaying in a faint breeze and bending away from whoever walks through, see World/GrassPushers), planted as GPU-instanced terrain details where
    /// <see cref="CavernLayout.GrassDensity"/> says. Runs on the cavern's terrain; part of Build Slime Cavern.
    /// </summary>
    public static class CavernGrass
    {
        const int DetailResolution = 256;   // 0.5 m cells over the 128 m terrain

        public static void Dress(Terrain terrain)
        {
            if (terrain == null) return;
            var shader = Shader.Find("Evolve/GrassBlade");
            if (shader == null) { Debug.LogWarning("Không tìm thấy shader Evolve/GrassBlade: bỏ qua thảm cỏ."); return; }
            PrototypeSceneBuilder.EnsureFolder("Assets/_Project/Models/Grass");
            PrototypeSceneBuilder.EnsureFolder("Assets/_Project/Prefabs");

            var data = terrain.terrainData;
            data.SetDetailResolution(DetailResolution, 16);
            int variants = LakeGrass.Variants;
            var protos = new DetailPrototype[variants];
            for (int v = 0; v < variants; v++)
                protos[v] = new DetailPrototype
                {
                    prototype = LakeGrass.MakePrefab(v, shader),
                    usePrototypeMesh = true,
                    useInstancing = true,
                    renderMode = DetailRenderMode.VertexLit,
                    minWidth = 0.9f, maxWidth = 1.35f, minHeight = 0.8f, maxHeight = 1.4f,
                    alignToGround = 0.1f,
                    positionJitter = 0.9f,
                    holeEdgePadding = 0f,
                    noiseSpread = 7f,
                    useDensityScaling = false
                };
            data.detailPrototypes = protos;

            Vector3 origin = terrain.transform.position, size = data.size;
            var maps = new int[variants][,];
            for (int v = 0; v < variants; v++) maps[v] = new int[DetailResolution, DetailResolution];
            long clumps = 0;
            for (int z = 0; z < DetailResolution; z++)
                for (int x = 0; x < DetailResolution; x++)
                {
                    float wx = origin.x + (x + 0.5f) / DetailResolution * size.x, wz = origin.z + (z + 0.5f) / DetailResolution * size.z;
                    float dens = CavernLayout.GrassDensity(wx, wz);
                    if (dens <= 0.03f) continue;
                    float amount = dens * 6f * 0.25f * 1.4f;     // about 8 clumps per square metre where the carpet is thickest (cells are a quarter of a square metre)
                    for (int v = 0; v < variants; v++)
                    {
                        int n = Mathf.Clamp(Mathf.FloorToInt(amount / variants + LakeGrass.Hash(x, z, v)), 0, 8);
                        maps[v][z, x] = n;
                        clumps += n;
                    }
                }
            for (int v = 0; v < variants; v++) data.SetDetailLayer(0, 0, v, maps[v]);
            terrain.detailObjectDistance = 90f;
            terrain.detailObjectDensity = 1f;
            terrain.drawInstanced = true;
            EditorUtility.SetDirty(data);
            EditorSceneManager.MarkAllScenesDirty();
            Debug.Log($"Đã trồng thảm cỏ thật trong hang: khoảng {clumps} bụi, đung đưa và rẽ ra khi Slime đi qua.");
        }
    }
}
