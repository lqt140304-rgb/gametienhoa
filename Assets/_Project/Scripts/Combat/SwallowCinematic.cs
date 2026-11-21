using System.Collections;
using Evolve.Survival;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Evolve.Combat
{
    /// <summary>
    /// The finishing moment of a latch fight (spec 2026-11-20): time slows and the camera pushes in for about two seconds while the slime
    /// swallows its prey. Skippable with Escape or the pounce button. Local player only; the time scale and field of view are always restored.
    /// </summary>
    public static class SwallowCinematic
    {
        static bool playing;
        internal static bool IsPlaying => playing;
        const float RealSeconds = LatchGelTiming.AbsorbSeconds, SlowScale = 0.3f, FovFactor = 0.72f;

        public static void Play(SurvivalStats slime) => Play(slime, null);

        internal static void Play(SurvivalStats slime, LatchGelFx gel)
        {
            if (slime == null || slime.IsRemote || playing) return;   // one at a time: a second would remember the slowed time scale as normal
            playing = true;
            new GameObject("SwallowCinematic").AddComponent<Runner>().Gel = gel;
        }

        sealed class Runner : MonoBehaviour
        {
            public LatchGelFx Gel;
            float oldScale = 1f, oldFov;
            bool started;
            Camera cam;

            IEnumerator Start()
            {
                started = true;
                oldScale = Time.timeScale;
                cam = Camera.main;
                oldFov = cam != null ? cam.fieldOfView : 60f;
                Time.timeScale = SlowScale;
                for (float t = 0f; t < RealSeconds; t += Time.unscaledDeltaTime)
                {
                    bool skip = (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                        || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);
                    if (skip) break;
                    float k = Mathf.Sin(Mathf.Clamp01(t / RealSeconds) * Mathf.PI);   // in and back out
                    if (cam != null) cam.fieldOfView = Mathf.Lerp(oldFov, oldFov * FovFactor, k);
                    yield return null;
                }
                Destroy(gameObject);
            }

            void OnDestroy()
            {
                playing = false;
                if (Gel != null) Gel.Stop();
                if (!started) return;
                Time.timeScale = oldScale;
                if (cam != null) cam.fieldOfView = oldFov;
            }
        }
    }
}
