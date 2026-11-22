#if !CSTEST
using Evolve.Creatures;
using NUnit.Framework;
using UnityEngine;

namespace Evolve.Tests.EditMode
{
    public class HangTwoUnityTests
    {
        [Test]
        public void GlowChangesOnlyEmissiveSlotsAndRestoresIdleBrightness()
        {
            var shader = Shader.Find("Evolve/CreatureVertexColor");
            Assert.That(shader, Is.Not.Null);
            var body = new Material(shader);
            var ore = new Material(shader);
            var creature = new GameObject("Glow test");
            try
            {
                body.SetFloat("_Glow", 0f);
                ore.SetFloat("_Glow", 0.85f);
                var renderer = creature.AddComponent<MeshRenderer>();
                renderer.sharedMaterials = new[] { body, ore };
                var glow = new CreatureGlow(creature);
                var block = new MaterialPropertyBlock();

                glow.Set(0.75f);
                renderer.GetPropertyBlock(block, 0);
                Assert.That(block.isEmpty, Is.True, "The body and wings keep their own material properties.");
                renderer.GetPropertyBlock(block, 1);
                Assert.That(block.GetFloat("_Glow"), Is.EqualTo(1.6f).Within(0.001f));

                glow.Set(0f);
                renderer.GetPropertyBlock(block, 1);
                Assert.That(block.GetFloat("_Glow"), Is.EqualTo(0.85f).Within(0.001f));
                Assert.That(ore.GetFloat("_Glow"), Is.EqualTo(0.85f).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(creature);
                Object.DestroyImmediate(body);
                Object.DestroyImmediate(ore);
            }
        }
    }
}
#endif
