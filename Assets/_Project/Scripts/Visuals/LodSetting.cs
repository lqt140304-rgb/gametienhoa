using UnityEngine;

namespace Evolve.Visuals
{
    /// <summary>
    /// Level of detail on or off. With LOD OFF (the default for now) every model keeps its full detail at every distance and textures stay at full resolution with
    /// anisotropic filtering: nothing is swapped for a simpler copy or culled for being small. With LOD ON the project's normal level-of-detail bias is restored (faster,
    /// less detailed far away). Switch it in the F10 panel; it is remembered.
    /// </summary>
    public static class LodSetting
    {
        public const string PrefKey = "evolve.lodOff";
        public const float OffBias = 20f;

        static float defaultBias = -1f;
        static int defaultMaxLevel;
        static int defaultMipLimit;
        static AnisotropicFiltering defaultAniso;

        public static bool Off { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { defaultBias = -1f; Off = false; }

        public static bool Saved => PlayerPrefs.GetInt(PrefKey, 1) == 1;

        public static void Apply(bool off)
        {
            if (defaultBias < 0f)      // remember what the project had before we first touched it
            {
                defaultBias = QualitySettings.lodBias;
                defaultMaxLevel = QualitySettings.maximumLODLevel;
                defaultMipLimit = QualitySettings.globalTextureMipmapLimit;
                defaultAniso = QualitySettings.anisotropicFiltering;
            }
            Off = off;
            if (off)
            {
                QualitySettings.lodBias = OffBias;                            // thresholds divided by 20: even a small model stays at its best level
                QualitySettings.maximumLODLevel = 0;
                QualitySettings.globalTextureMipmapLimit = 0;                 // textures at full resolution
                QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
            }
            else
            {
                QualitySettings.lodBias = defaultBias;
                QualitySettings.maximumLODLevel = defaultMaxLevel;
                QualitySettings.globalTextureMipmapLimit = defaultMipLimit;
                QualitySettings.anisotropicFiltering = defaultAniso;
            }
            PlayerPrefs.SetInt(PrefKey, off ? 1 : 0);
        }
    }
}
