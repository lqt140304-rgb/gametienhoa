using Evolve.Visuals;
using NUnit.Framework;
using UnityEngine;

namespace Evolve.Tests.EditMode
{
    public class LodSettingTests
    {
        [Test]
        public void TurningLodOffKeepsFullDetailAndOnRestoresTheProjectsValues()
        {
            float bias = QualitySettings.lodBias;
            int max = QualitySettings.maximumLODLevel;
            int mip = QualitySettings.globalTextureMipmapLimit;
            var aniso = QualitySettings.anisotropicFiltering;
            bool savedOff = LodSetting.Off;
            try
            {
                LodSetting.Apply(true);
                Assert.IsTrue(LodSetting.Off);
                Assert.GreaterOrEqual(QualitySettings.lodBias, 10f, "thresholds scaled so models stay detailed");
                Assert.AreEqual(0, QualitySettings.maximumLODLevel);
                Assert.AreEqual(0, QualitySettings.globalTextureMipmapLimit, "full resolution textures");
                LodSetting.Apply(false);
                Assert.IsFalse(LodSetting.Off);
                Assert.Less(QualitySettings.lodBias, 10f, "back to the project's bias");
            }
            finally
            {
                QualitySettings.lodBias = bias;
                QualitySettings.maximumLODLevel = max;
                QualitySettings.globalTextureMipmapLimit = mip;
                QualitySettings.anisotropicFiltering = aniso;
                PlayerPrefs.DeleteKey(LodSetting.PrefKey);
            }
        }
    }
}
