using Evolve.Player;
using UnityEngine;

namespace Evolve.World
{
    /// <summary>
    /// When the camera dips below the water surface, the screen is washed with the water's teal and the fog closes in,
    /// so being under water reads as under water. Only in the lake zone (added by SlimeController there).
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class UnderwaterTint : MonoBehaviour
    {
        static readonly Color Tint = new Color(0.10f, 0.42f, 0.44f, 0.45f);
        bool under;
        /// <summary>True while the camera is under the water (other fog users, like ZoneAtmosphere, step aside).</summary>
        public bool IsUnder => under;
        bool savedFog;
        Color savedFogColor;
        float savedFogDensity;
        FogMode savedFogMode;
        Texture2D pixel;

        void OnDisable() { if (under) Restore(); under = false; }

        void LateUpdate()
        {
            Vector3 eye = transform.position;
            // only over real water: the sinkhole and the gorge floor lie below the lake's level but are dry
            bool overWater = WaterField.CleanDepthAt(eye.x, eye.z) > 0f;
            bool now = overWater && SwimLogic.IsUnderwater(eye.y, WaterField.Level, under);
            if (now == under) return;
            under = now;
            if (under)
            {
                savedFog = RenderSettings.fog; savedFogColor = RenderSettings.fogColor;
                savedFogDensity = RenderSettings.fogDensity; savedFogMode = RenderSettings.fogMode;
                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.Exponential;
                RenderSettings.fogColor = new Color(Tint.r, Tint.g, Tint.b);
                RenderSettings.fogDensity = 0.12f;
            }
            else Restore();
        }

        void Restore()
        {
            RenderSettings.fog = savedFog; RenderSettings.fogColor = savedFogColor;
            RenderSettings.fogDensity = savedFogDensity; RenderSettings.fogMode = savedFogMode;
        }

        void OnGUI()
        {
            if (!under || Event.current.type != EventType.Repaint) return;
            if (pixel == null) { pixel = new Texture2D(1, 1); pixel.SetPixel(0, 0, Color.white); pixel.Apply(); }
            var old = GUI.color;
            GUI.depth = 100;   // behind the HUD
            GUI.color = Tint;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), pixel);
            GUI.color = old;
        }

        void OnDestroy() { if (pixel != null) Destroy(pixel); }
    }
}
