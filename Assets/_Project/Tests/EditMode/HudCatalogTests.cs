using Evolve.Survival;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class HudCatalogTests
    {
        [TestCase("Bộc phát gai", "Gai lưng", "boc phat", true)]
        [TestCase("Lao mạnh", "Chân cơ bắp", "chan co", true)]
        [TestCase("Lao mạnh", "Chân", "mat", false)]
        public void SearchIgnoresVietnameseAccents(string name,string organ,string query,bool expected)
            => Assert.AreEqual(expected,HudCatalog.Matches(name,organ,query));
        [Test] public void PagesHandleLargeAndEmptyLibraries()
        {
            Assert.AreEqual(13,HudCatalog.PageCount(103,8));
            Assert.AreEqual(12,HudCatalog.ClampPage(99,103,8));
            Assert.AreEqual(0,HudCatalog.ClampPage(-1,0,8));
        }
        [Test] public void SpikeCalculationUsesAssetValues()
        {
            Assert.AreEqual(30,HudCatalog.SpikeTotal(6,5,2));
            Assert.AreEqual(63,HudCatalog.SpikeTotal(6,5,3));
            Assert.AreEqual(72,HudCatalog.SpikeTotal(8,4,3));
        }
        [TestCase(1280,800)]
        [TestCase(1366,768)]
        [TestCase(1920,1080)]
        [TestCase(800,600)]
        public void MenuLeavesExpandedHudVisible(float width,float height)
        {
            float hud=System.Math.Max(.1f,System.Math.Min(1f,System.Math.Min(width/960,height/600)));
            Assert.LessOrEqual(52+650*HudCatalog.MenuScale(width,height,hud),height-272*hud-12);
        }
    }
}
