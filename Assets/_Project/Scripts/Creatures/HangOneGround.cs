using UnityEngine;

namespace Evolve.Creatures
{
    /// <summary>Uses the painted Cavern terrain weights, independent of lighting or texture color.</summary>
    public static class HangOneGround
    {
        public const float CamouflageDetectionFactor = 0.6f;
        static Terrain terrain;
        static float[,,] weights;
        static int mud, moss;
        static readonly RaycastHit[] hits = new RaycastHit[24];

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { terrain = null; weights = null; }

        public static bool IsWet(float mudWeight) => mudWeight >= 0.5f;
        public static bool IsMoss(float mossWeight) => mossWeight >= 0.5f;
        public static bool Wet(Vector3 pos) => Sample(pos, out float wet, out _) && IsWet(wet);
        public static bool Moss(Vector3 pos) => Sample(pos, out _, out float green) && IsMoss(green);

        static bool Sample(Vector3 pos, out float wet, out float green)
        {
            wet = green = 0f;
            var active = Terrain.activeTerrain;
            if (active == null || active.terrainData == null) return false;
            if (active != terrain)
            {
                terrain = active;
                mud = moss = -1;
                var data = active.terrainData;
                var layers = data.terrainLayers;
                for (int i = 0; i < layers.Length; i++)
                {
                    if (layers[i] == null) continue;
                    var layer = layers[i];
                    if (layer.name == "Cavern_Mud" || layer.diffuseTexture?.name == "Cavern_Mud_v3") mud = i;
                    if (layer.name == "Cavern_Moss" || layer.diffuseTexture?.name == "Cavern_Moss_v3") moss = i;
                }
                weights = mud >= 0 && moss >= 0 ? data.GetAlphamaps(0, 0, data.alphamapWidth, data.alphamapHeight) : null;
            }
            if (weights == null) return false;
            Vector3 local = pos - active.transform.position;
            Vector3 size = active.terrainData.size;
            float u = local.x / size.x, v = local.z / size.z;
            if (u < 0f || u >= 1f || v < 0f || v >= 1f) return false;
            if (Mathf.Abs(active.SampleHeight(pos) - local.y) > 0.65f) return false;
            int x = (int)(u * weights.GetLength(1)), z = (int)(v * weights.GetLength(0));
            wet = weights[z, x, mud];
            green = weights[z, x, moss];
            return true;
        }

        /// <summary>Static cave geometry blocks the path. Bodies and triggers are ignored.</summary>
        public static bool Clear(Vector3 from, Vector3 to, float radius = 0f)
        {
            Vector3 delta = to - from;
            float length = delta.magnitude;
            if (length < 0.001f) return true;
            int count = radius > 0f
                ? Physics.SphereCastNonAlloc(from, radius, delta / length, hits, length, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                : Physics.RaycastNonAlloc(from, delta / length, hits, length, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            if (count == hits.Length) return false;
            for (int i = 0; i < count; i++)
                if (hits[i].collider.attachedRigidbody == null) return false;
            return true;
        }
    }
}
