using UnityEngine;

namespace Evolve.Player
{
    /// <summary>Body size curve: grows with equipped organs, capped.</summary>
    public static class BodySize
    {
        public static float ScaleFor(int organCount) => Mathf.Min(1.5f, 1f + 0.06f * organCount);
    }
}
