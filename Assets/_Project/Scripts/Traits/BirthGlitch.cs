using UnityEngine;

namespace Evolve.Traits
{
    /// <summary>
    /// Turns the connection's instability into the post-processing look and the camera shake (spec 5.2): chromatic aberration, lens distortion, film grain,
    /// vignette and bloom all rise with it, but stay under caps so the text can still be read. The shake is the sum of a small constant tremor and the hits.
    /// </summary>
    internal sealed class BirthGlitch
    {
        readonly BirthStage stage;
        /// <summary>Camera shake in canvas units for a hit of strength 1 (a mythic core is 2).</summary>
        const float HitUnits = 0.2f;

        public BirthGlitch(BirthStage stage) { this.stage = stage; }

        /// <param name="instability">0..1.</param>
        /// <param name="hitShake">Strength of the shake from slams right now (0 = none; 1 = a core slam).</param>
        public void Apply(float instability, float hitShake, float time)
        {
            if (stage == null || stage.Cam == null) return;
            float k = Mathf.Clamp01(instability);
            float wobble = 0.6f + 0.4f * Mathf.Sin(time * 7f);
            stage.Chroma.intensity.value = Mathf.Lerp(0.05f, 0.85f, k);
            stage.Lens.intensity.value = -0.3f * k * wobble;
            stage.Grain.intensity.value = Mathf.Lerp(0.12f, 0.8f, k);
            stage.Vig.intensity.value = Mathf.Lerp(0.25f, 0.5f, k);
            stage.Bloom.intensity.value = Mathf.Lerp(0.6f, 1.4f, k);

            float tremor = 0.03f * k * k;                       // a constant shiver while the connection is bad
            var o = ShakeMath.Offset(time, tremor + HitUnits * hitShake);
            stage.Cam.transform.localPosition = new Vector3(o.x, o.y, 0f);
        }
    }
}
