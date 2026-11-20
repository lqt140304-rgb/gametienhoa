using Evolve.Survival;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class HudMenuTests
    {
        [Test]
        public void Toggle_Stomach()
        {
            Assert.AreEqual(HudMenu.Stomach, HudLayout.Toggle(HudMenu.None, HudMenu.Stomach));
            Assert.AreEqual(HudMenu.None, HudLayout.Toggle(HudMenu.Stomach, HudMenu.Stomach));
            Assert.AreEqual(HudMenu.Stomach, HudLayout.Toggle(HudMenu.Organs, HudMenu.Stomach));
        }
    }
}
