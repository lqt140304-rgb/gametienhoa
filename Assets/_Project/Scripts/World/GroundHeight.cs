using UnityEngine;

namespace Evolve.World
{
    /// <summary>
    /// Ground height under a point, read from the active Terrain. Without a Terrain (e.g. the flat Prototype_01
    /// scene) every call returns the caller's fallback, so creatures keep the height they always had.
    /// </summary>
    public static class GroundHeight
    {
        /// <summary>World-space ground height at (x, z), or <paramref name="fallbackY"/> when there is no Terrain.</summary>
        public static float At(float x, float z, float fallbackY)
        {
            var terrain = Terrain.activeTerrain;
            if (terrain == null) return fallbackY;
            Vector3 tPos = terrain.transform.position;
            Vector3 tSize = terrain.terrainData.size;
            if (x < tPos.x || x > tPos.x + tSize.x || z < tPos.z || z > tPos.z + tSize.z)
                return fallbackY;
            return terrain.SampleHeight(new Vector3(x, 0f, z)) + tPos.y;
        }

        /// <summary>How far a point sits above the ground (0 when there is no Terrain).</summary>
        public static float OffsetOf(Vector3 position) => position.y - At(position.x, position.z, position.y);

        /// <summary>Move <paramref name="next"/> vertically so it keeps <paramref name="offset"/> above the ground.</summary>
        public static Vector3 Follow(Vector3 next, float offset)
        {
            next.y = At(next.x, next.z, next.y) + offset;
            return next;
        }
    }
}
