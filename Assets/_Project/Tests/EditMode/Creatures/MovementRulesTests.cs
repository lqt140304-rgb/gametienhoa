using Evolve.Creatures;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class MovementRulesTests
    {
        [Test] public void Profile_BySpeciesOrPrefabName()
        {
            Assert.AreEqual(MoveStyle.Flyer, MovementRules.For("Dơi Âm Thoa").Style);
            Assert.AreEqual(MoveStyle.Climber, MovementRules.For("ThanLanBoHo(Clone)").Style);
            Assert.AreEqual(MoveStyle.Hopper, MovementRules.For("Thỏ Lá").Style);
            Assert.AreEqual(MoveStyle.Walker, MovementRules.For("Ốc Hồ").Style);
            Assert.AreEqual(MoveStyle.Walker, MovementRules.For("").Style);
        }

        [Test] public void WaterAndSlopeRules_PerSpecies()
        {
            var thanLan = MovementRules.For("ThanLanBoHo");
            Assert.IsTrue(thanLan.AvoidsWater, "ThanLanBoHo must avoid water");
            Assert.IsFalse(thanLan.CanSwim, "ThanLanBoHo cannot swim");

            var thoLa = MovementRules.For("ThoLa");
            Assert.IsTrue(thoLa.AvoidsWater, "ThoLa must avoid water");
            Assert.IsFalse(thoLa.CanSwim, "ThoLa cannot swim");

            var cocHoGia = MovementRules.For("CocHoGia");
            Assert.IsFalse(cocHoGia.AvoidsWater, "CocHoGia does not avoid water");
            Assert.IsTrue(cocHoGia.CanSwim, "CocHoGia can swim in water");
            Assert.GreaterOrEqual(cocHoGia.MaxClimbSlope, 0.85f, "CocHoGia avoids climbing steep cavern walls");
        }

        [Test] public void Walker_StepsSmallRocks_NotWallsOrCliffs()
        {
            var w = MovementRules.Walker;
            Assert.IsTrue(MovementRules.CanEnter(0f, 0.2f, w));    // small rock
            Assert.IsFalse(MovementRules.CanEnter(0f, 0.8f, w));   // platform it would keep jumping at
            Assert.IsFalse(MovementRules.CanEnter(0f, 3f, w));     // bridge deck above it
            Assert.IsTrue(MovementRules.CanEnter(0f, -0.8f, w));
            Assert.IsFalse(MovementRules.CanEnter(0f, -2f, w));    // ledge
        }

        [Test] public void Hopper_ReachesThePlatform_FlyerIgnoresDrops()
        {
            Assert.IsTrue(MovementRules.CanEnter(0f, 0.8f, MovementRules.Hopper));
            Assert.IsTrue(MovementRules.CanEnter(0f, -20f, MovementRules.Flyer));
            Assert.IsFalse(MovementRules.CanEnter(0f, 4f, MovementRules.Flyer));
        }

        [Test] public void SmoothY_ApproachesWithoutOvershoot()
        {
            float y = MovementRules.SmoothY(0f, 1f, 0.02f);
            Assert.Greater(y, 0f); Assert.Less(y, 1f);
            for (int i = 0; i < 100; i++) y = MovementRules.SmoothY(y, 1f, 0.02f);
            Assert.AreEqual(1f, y, 1e-3f);
        }
    }
}
