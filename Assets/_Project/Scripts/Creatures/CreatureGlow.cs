using System.Collections.Generic;
using UnityEngine;

namespace Evolve.Creatures
{
    /// <summary>Changes only authored emissive material slots and preserves their idle brightness.</summary>
    public sealed class CreatureGlow
    {
        readonly struct Slot
        {
            public readonly Renderer Renderer;
            public readonly int Index;
            public readonly float Rest;
            public Slot(Renderer renderer, int index, float rest) { Renderer = renderer; Index = index; Rest = rest; }
        }
        static readonly int GlowId = Shader.PropertyToID("_Glow");
        readonly Slot[] slots;
        readonly MaterialPropertyBlock block = new MaterialPropertyBlock();

        public CreatureGlow(GameObject creature)
        {
            var found = new List<Slot>();
            foreach (var renderer in creature.GetComponentsInChildren<Renderer>())
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    var material = materials[i];
                    if (material == null || !material.HasProperty(GlowId)) continue;
                    float glow = material.GetFloat(GlowId);
                    if (glow > 0f) found.Add(new Slot(renderer, i, glow));
                }
            }
            slots = found.ToArray();
        }

        public void Set(float extra)
        {
            foreach (var slot in slots)
            {
                if (slot.Renderer == null) continue;
                slot.Renderer.GetPropertyBlock(block, slot.Index);
                block.SetFloat(GlowId, slot.Rest + extra);
                slot.Renderer.SetPropertyBlock(block, slot.Index);
            }
        }
    }
}
