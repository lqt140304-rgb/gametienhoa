using System;
using UnityEngine;

namespace Evolve.World
{
    /// <summary>
    /// Ground height under a point, read from the active Terrain or a custom sampler (such as modular maze chambers).
    /// Without a Terrain (e.g. the flat Prototype_01 scene) every call returns the caller's fallback, so creatures keep the height they always had.
    /// </summary>
    public static class GroundHeight
    {
        static Func<float, float, float?> customSampler;
        static readonly RaycastHit[] physicalHits = new RaycastHit[12];

        public static void UseCustom(Func<float, float, float?> sampler) => customSampler = sampler;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset() => customSampler = null;

        /// <summary>World-space ground height at (x, z), checking physical colliders first in Play Mode, then custom sampler, active Terrain, or <paramref name="fallbackY"/>.</summary>
        public static float At(float x, float z, float fallbackY)
        {
            float baseEstimate = fallbackY;
            if (customSampler != null)
            {
                float? custom = customSampler(x, z);
                if (custom.HasValue) baseEstimate = custom.Value;
            }
            else
            {
                var terrain = Terrain.activeTerrain;
                if (terrain != null && terrain.terrainData != null)
                {
                    Vector3 tPos = terrain.transform.position;
                    Vector3 tSize = terrain.terrainData.size;
                    if (x >= tPos.x && x <= tPos.x + tSize.x && z >= tPos.z && z <= tPos.z + tSize.z)
                        baseEstimate = terrain.SampleHeight(new Vector3(x, 0f, z)) + tPos.y;
                }
            }

            if (Application.isPlaying && TrySamplePhysical(x, z, baseEstimate, out float physY))
                return physY;

            return baseEstimate;
        }

        static bool TrySamplePhysical(float x, float z, float baseEstimate, out float groundY)
        {
            float startY = Mathf.Max(baseEstimate + 10f, 16f);
            int count = Physics.RaycastNonAlloc(new Vector3(x, startY, z), Vector3.down, physicalHits, 42f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            float bestY = 0f;
            float bestDist = float.MaxValue;
            bool found = false;

            for (int i = 0; i < count; i++)
            {
                var h = physicalHits[i];
                if (h.collider == null || h.normal.y < 0.35f) continue;
                if (h.collider.attachedRigidbody != null) continue;   // bỏ qua slime và quái khác
                if (h.point.y > baseEstimate + 14f) continue;         // bỏ qua nóc/vòm hang phía trên

                float dist = Mathf.Abs(h.point.y - baseEstimate);
                if (!found || dist < bestDist)
                {
                    bestDist = dist;
                    bestY = h.point.y;
                    found = true;
                }
            }

            groundY = bestY;
            return found;
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
