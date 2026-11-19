using UnityEngine;

namespace Evolve.World
{
    /// <summary>Put on a scene object: while it is enabled the scene's water is the Slime Cavern's lake.</summary>
    public class CavernWater : MonoBehaviour
    {
        void OnEnable() => WaterField.Use(CavernLayout.DepthAt, CavernLayout.DepthAt, CavernLayout.WaterLevel);
        void OnDisable() => WaterField.Reset();
    }
}
