using System;
using Evolve.Survival;
using UnityEngine;

namespace Evolve.Visuals
{
    /// <summary>
    /// F4: a small performance read-out in the corner: frames per second, the worst frame of the last second,
    /// garbage collections per second and how long the slime body animation takes per frame.
    /// Helps tell whether a hitch comes from the animation or from somewhere else.
    /// </summary>
    public class PerfOverlay : MonoBehaviour
    {
        int mode; // 0 hidden, 1 numbers, 2 numbers + body animation off (to test)
        int frames;
        float accumulated, worst;
        string text = "";
        int lastGc;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (FindAnyObjectByType<PerfOverlay>() != null) return;
            var go = new GameObject("Performance (F4)");
            DontDestroyOnLoad(go);
            go.AddComponent<PerfOverlay>();
        }

        void Update()
        {
            if (KeyPressed.F4())
            {
                mode = (mode + 1) % 3;
                SlimeGelAnimator.Paused = mode == 2;
            }
            float dt = Time.unscaledDeltaTime;
            frames++;
            accumulated += dt;
            if (dt > worst) worst = dt;
            if (accumulated < 1f) return;

            int gc = GC.CollectionCount(0);
            double animMs = SlimeGelAnimator.CostTicks * 1000.0 / System.Diagnostics.Stopwatch.Frequency / Math.Max(1, frames);
            SlimeGelAnimator.CostTicks = 0;
            text = $"FPS {frames / accumulated:0}   khung xấu nhất {worst * 1000f:0} ms\nGC {gc - lastGc}/giây   animation thân {animMs:0.00} ms/khung";
            lastGc = gc;
            frames = 0; accumulated = 0f; worst = 0f;
        }

        void OnGUI()
        {
            if (mode == 0) return;
            string head = mode == 2 ? "ANIMATION THÂN ĐANG TẮT (F4 để bật lại)\n" : "";
            GUI.Box(new Rect(Screen.width - 330, 8, 322, mode == 2 ? 64 : 46), head + (text.Length > 0 ? text : "đang đo..."));
        }
    }
}
