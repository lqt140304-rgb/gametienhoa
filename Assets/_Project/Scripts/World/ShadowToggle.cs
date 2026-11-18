using Evolve.Survival;
using UnityEngine;
using UnityEngine.Rendering;

namespace Evolve.World
{
    /// <summary>F9 turns the root roof's shadows off and on (to compare the frame rate). Put on the main light; the Editor tool fills in the roof.</summary>
    public class ShadowToggle : MonoBehaviour
    {
        [SerializeField] Renderer[] roof = new Renderer[0];
        ShadowCastingMode[] original;
        bool on = true;

        public void SetRoof(Renderer[] renderers) => roof = renderers ?? new Renderer[0];

        void Awake()
        {
            original = new ShadowCastingMode[roof.Length];
            for (int i = 0; i < roof.Length; i++) original[i] = roof[i] != null ? roof[i].shadowCastingMode : ShadowCastingMode.Off;
        }

        void Update()
        {
            if (!KeyPressed.F9()) return;
            on = !on;
            Apply();
            Debug.Log("Bóng đổ của mái: " + (on ? "bật" : "tắt"));
        }

        void OnDisable()
        {
            if (!on) { on = true; Apply(); }
        }

        void Apply()
        {
            if (original == null || original.Length != roof.Length) return;
            for (int i = 0; i < roof.Length; i++)
                if (roof[i] != null) roof[i].shadowCastingMode = on ? original[i] : ShadowCastingMode.Off;
        }
    }
}
