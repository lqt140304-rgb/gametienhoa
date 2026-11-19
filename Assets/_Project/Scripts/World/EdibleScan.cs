using Evolve.Survival;
using UnityEngine;

namespace Evolve.World
{
    /// <summary>
    /// What the sound wave does to things that can be eaten: their surface takes on a tint of its own (by kind of food) with moving bands and a bright rim, from the moment
    /// the wave front reaches them until it fades, so after a scan you can see which things in range are food. The look itself is in the vertex-colour shader
    /// (<c>_ScanTint</c>, <c>EvolveEdibleEcho</c>); this class only picks the tint and gives it to the renderers.
    /// </summary>
    public static class EdibleScan
    {
        static readonly int ScanTintId = Shader.PropertyToID("_ScanTint");

        /// <summary>Plants mint green, mushrooms and fruit amber, minerals gold, meat red.</summary>
        public static Color TintFor(FoodKind kind)
        {
            switch (kind)
            {
                case FoodKind.PreyMeat:
                case FoodKind.PredatorMeat: return new Color(1.0f, 0.35f, 0.3f, 1f);
                case FoodKind.LimeCrystal:
                case FoodKind.IronNodule:
                case FoodKind.SaltCrust:
                case FoodKind.GlowQuartz: return new Color(1.0f, 0.82f, 0.3f, 1f);
                case FoodKind.FatMushroom:
                case FoodKind.SporePod:
                case FoodKind.AmberBud:
                case FoodKind.EmberBerry: return new Color(1.0f, 0.6f, 0.25f, 1f);
                default: return new Color(0.35f, 1.0f, 0.65f, 1f);
            }
        }

        /// <summary>Gives every renderer under <paramref name="root"/> the scan tint of this food (a property block, so the shared materials stay shared).</summary>
        public static void Apply(Component root, FoodKind kind)
        {
            var block = new MaterialPropertyBlock();
            var tint = TintFor(kind);
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                r.GetPropertyBlock(block);
                block.SetColor(ScanTintId, tint);
                r.SetPropertyBlock(block);
            }
        }
    }
}
