using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Evolve.Traits
{
    /// <summary>
    /// A "broken frame" window (spec 5.3): dark glass with a title bar, and a border made of separate segments and corner brackets. Each piece jitters,
    /// flickers and sometimes jumps sideways in proportion to the connection's instability; the whole window tilts a little. As the instability falls
    /// the pieces settle and the border closes into a clean frame.
    /// </summary>
    internal sealed class BirthWindow
    {
        struct Seg { public RectTransform Rt; public Image Img; public Vector2 Pos; public float Phase, Alpha; }

        public RectTransform Root;
        public Text Title, Body;
        public Color Accent;
        readonly List<Seg> segs = new List<Seg>();
        Image scan;
        Vector2 size, bodyBase;
        float seed;

        public static BirthWindow Create(RectTransform parent, Vector2 pos, Vector2 size, string title, Color accent, int seed, int bodySize = 34)
        {
            var w = new BirthWindow { Accent = accent, size = size, seed = seed * 1.37f };
            w.Root = BirthUiKit.Group(parent, "Window", pos);
            var glass = BirthUiKit.Box(w.Root, "Glass", Vector2.zero, size * 0.985f, new Color(0.02f, 0.05f, 0.09f, 0.84f));
            w.scan = BirthUiKit.Box(w.Root, "Scan", Vector2.zero, new Vector2(size.x * 0.97f, 3f), BirthUiKit.WithAlpha(accent, 0.1f));
            BirthUiKit.Box(w.Root, "TitleBar", new Vector2(0f, size.y / 2f - 22f), new Vector2(size.x - 14f, 32f), BirthUiKit.WithAlpha(accent, 0.15f));
            w.Title = BirthUiKit.Label(w.Root, "Title", new Vector2(18f, size.y / 2f - 22f), new Vector2(size.x - 60f, 32f), title, 22, TextAnchor.MiddleLeft, BirthUiKit.WithAlpha(Color.Lerp(accent, Color.white, 0.4f), 1f), true);
            w.bodyBase = new Vector2(0f, -22f);
            w.Body = BirthUiKit.Label(w.Root, "Body", w.bodyBase, new Vector2(size.x - 56f, size.y - 80f), "", bodySize, TextAnchor.UpperLeft, new Color(0.92f, 0.98f, 1f));

            float hx = size.x / 2f, hy = size.y / 2f;
            // each edge as three pieces with gaps; thickness 3
            float[][] cuts = { new[] { 0f, 0.42f }, new[] { 0.46f, 0.78f }, new[] { 0.82f, 1f } };
            int n = 0;
            foreach (var c in cuts)
            {
                float len = (c[1] - c[0]) * size.x, mid = (c[0] + c[1]) / 2f * size.x - hx;
                w.AddSeg(mid, hy, len, 3f, n++);
                w.AddSeg(mid, -hy, len, 3f, n++);
                float lenV = (c[1] - c[0]) * size.y, midV = (c[0] + c[1]) / 2f * size.y - hy;
                w.AddSeg(-hx, midV, 3f, lenV, n++);
                w.AddSeg(hx, midV, 3f, lenV, n++);
            }
            // corner brackets (thicker, bright)
            foreach (int sx in new[] { -1, 1 })
                foreach (int sy in new[] { -1, 1 })
                {
                    w.AddSeg(sx * (hx - 14f), sy * hy, 30f, 6f, n++, 1f);
                    w.AddSeg(sx * hx, sy * (hy - 14f), 6f, 30f, n++, 1f);
                }
            return w;
        }

        void AddSeg(float x, float y, float w, float h, int index, float alpha = 0.75f)
        {
            var img = BirthUiKit.Box(Root, "Seg" + index, new Vector2(x, y), new Vector2(w, h), BirthUiKit.WithAlpha(Accent, alpha));
            segs.Add(new Seg { Rt = img.rectTransform, Img = img, Pos = new Vector2(x, y), Phase = BirthUiKit.Hash(index * 31 + (int)seed), Alpha = alpha });
        }

        public void SetAccent(Color c) => Accent = c;

        /// <param name="instability">0..1: how broken the frame looks.</param>
        public void Tick(float time, float instability)
        {
            float k = Mathf.Clamp01(instability);
            for (int i = 0; i < segs.Count; i++)
            {
                var s = segs[i];
                float nx = Mathf.PerlinNoise(s.Phase * 10f, time * (6f + 10f * k)) - 0.5f;
                float ny = Mathf.PerlinNoise(s.Phase * 7f + 3f, time * 8f) - 0.5f;
                var off = new Vector2(nx * 2f * k * 18f, ny * 2f * k * 10f);
                float gate = Mathf.Repeat(time * 2.3f + s.Phase * 5f, 1f);
                if (gate < k * 0.10f) off.x += Mathf.Sign(nx) * 46f;           // a piece tears sideways for a moment
                s.Rt.anchoredPosition = s.Pos + off;
                bool dim = k > 0.25f && BirthUiKit.Hash(Mathf.FloorToInt(time * 14f) + (int)(s.Phase * 1000f)) < k * 0.30f;
                s.Img.color = BirthUiKit.WithAlpha(Accent, s.Alpha * (dim ? 0.12f : 1f));
            }
            Root.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(time * 1.7f + seed) * k * 2.5f);
            Root.localScale = Vector3.one * (1f + (Mathf.PerlinNoise(time * 9f, seed) - 0.5f) * 0.02f * k);
            Body.rectTransform.anchoredPosition = bodyBase + new Vector2(Mathf.Sin(time * 37f) * k * 3f, 0f);
            scan.rectTransform.anchoredPosition = new Vector2(0f, size.y * (Mathf.Repeat(time * 0.35f + seed, 1f) - 0.5f));
        }

        /// <summary>Set the body text: the letters typed so far, garbled while the connection is bad.</summary>
        public void SetBody(string full, float typedSeconds, float charsPerSecond, float instability, float time)
        {
            int n = GlitchText.TypedCount(full, typedSeconds, charsPerSecond);
            string shown = full == null ? "" : full.Substring(0, Mathf.Min(n, full.Length));
            Body.text = GlitchText.Corrupt(shown, instability, Mathf.FloorToInt(time * 6f));
        }
    }
}
