using UnityEngine;

namespace Evolve.Traits
{
    /// <summary>
    /// Drawing helpers for the birth scene: a cold "system" look (dark virtual space with a grid, drifting motes and a slow scan line,
    /// panels with glowing edges and corner brackets, magic-circle sigils) built from IMGUI rectangles and two generated textures.
    /// </summary>
    internal static class BirthUi
    {
        public static readonly Color Cyan = new Color(0.35f, 0.85f, 1f);
        public static readonly Color Deep = new Color(0.006f, 0.014f, 0.032f);

        static Texture2D disc, vignette;

        public static Texture2D Pixel => Texture2D.whiteTexture;

        public static void EnsureTextures()
        {
            if (disc != null) return;
            disc = new Texture2D(64, 64, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp };
            var px = new Color[64 * 64];
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(32f, 32f)) / 32f;
                    px[y * 64 + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(1f - d) * Mathf.Clamp01(1f - d));   // soft glow: bright core, long falloff
                }
            disc.SetPixels(px); disc.Apply();

            vignette = new Texture2D(64, 64, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(32f, 32f)) / 45f;
                    px[y * 64 + x] = new Color(0f, 0f, 0f, Mathf.Clamp01((d - 0.45f) * 1.3f));
                }
            vignette.SetPixels(px); vignette.Apply();
        }

        public static void Release()
        {
            if (disc != null) Object.Destroy(disc);
            if (vignette != null) Object.Destroy(vignette);
            disc = vignette = null;
        }

        // ---------------------------------------------------------------- primitives

        public static void Fill(Rect r, Color c) { GUI.color = c; GUI.DrawTexture(r, Pixel); GUI.color = Color.white; }

        public static void Rounded(Rect r, Color c, float radius) =>
            GUI.DrawTexture(r, Pixel, ScaleMode.StretchToFill, true, 0f, c, 0f, radius);

        public static void Outline(Rect r, Color c, float width, float radius) =>
            GUI.DrawTexture(r, Pixel, ScaleMode.StretchToFill, true, 0f, c, width, radius);

        public static void Glow(Vector2 center, float size, Color c)
        {
            GUI.color = c;
            GUI.DrawTexture(new Rect(center.x - size / 2f, center.y - size / 2f, size, size), disc);
            GUI.color = Color.white;
        }

        public static void Ring(Vector2 center, float radius, Color c, float width) =>
            Outline(new Rect(center.x - radius, center.y - radius, radius * 2f, radius * 2f), c, width, radius);

        /// <summary>A rotating magic circle: rings, two counter-rotating squares and tick marks.</summary>
        public static void Sigil(Vector2 center, float radius, float time, Color c)
        {
            var old = GUI.matrix;
            Ring(center, radius, c, 2f);
            Ring(center, radius * 0.86f, new Color(c.r, c.g, c.b, c.a * 0.6f), 1f);
            Ring(center, radius * 0.45f, new Color(c.r, c.g, c.b, c.a * 0.8f), 1.5f);
            for (int k = 0; k < 2; k++)
            {
                GUI.matrix = old;
                GUIUtility.RotateAroundPivot((k == 0 ? 1f : -1f) * time * 9f + k * 45f, center);
                float s = radius * 0.62f;
                Outline(new Rect(center.x - s, center.y - s, s * 2f, s * 2f), new Color(c.r, c.g, c.b, c.a * 0.7f), 1.5f, 0f);
            }
            for (int i = 0; i < 24; i++)
            {
                GUI.matrix = old;
                GUIUtility.RotateAroundPivot(i * 15f + time * 4f, center);
                Fill(new Rect(center.x - 1f, center.y - radius - 2f, 2f, i % 6 == 0 ? 12f : 6f), new Color(c.r, c.g, c.b, c.a * 0.9f));
            }
            GUI.matrix = old;
        }

        // ---------------------------------------------------------------- the virtual space

        /// <summary>The opaque background: dark, a cool glow, a drifting grid, rising motes, a slow scan band and a vignette. Nothing of the game shows through.</summary>
        public static void Background(float w, float h, float time, float ui)
        {
            Fill(new Rect(0, 0, w, h), Deep);
            Glow(new Vector2(w / 2f, h * 0.5f), Mathf.Max(w, h) * 1.2f, new Color(0.08f, 0.22f, 0.4f, 0.28f));

            float sp = 64f * ui;
            float off = (time * 10f) % sp;
            for (int i = -1; i * sp < w + sp; i++)
                Fill(new Rect(i * sp + off, 0, 1f, h), new Color(Cyan.r, Cyan.g, Cyan.b, (i % 4 == 0) ? 0.07f : 0.035f));
            for (int j = -1; j * sp < h + sp; j++)
                Fill(new Rect(0, j * sp + off, w, 1f), new Color(Cyan.r, Cyan.g, Cyan.b, (j % 4 == 0) ? 0.07f : 0.035f));

            Sigil(new Vector2(w / 2f, h * 0.5f), Mathf.Min(w, h) * 0.46f, time, new Color(Cyan.r, Cyan.g, Cyan.b, 0.07f));

            for (int i = 0; i < 70; i++)   // digital motes drifting up
            {
                float a = Hash(i * 3 + 1), b = Hash(i * 3 + 2), c = Hash(i * 3 + 3);
                float y = h - ((time * (14f + 26f * c) + b * h) % h);
                float tw = 0.35f + 0.65f * Mathf.Abs(Mathf.Sin(time * (0.6f + c) + a * 6f));
                float sz = (1.5f + 2.5f * c) * ui;
                Fill(new Rect(a * w, y, sz, sz), new Color(Cyan.r, Cyan.g, Cyan.b, 0.35f * tw));
            }

            float scan = (time * 80f) % (h + 240f) - 120f;
            Fill(new Rect(0, scan, w, 90f * ui), new Color(Cyan.r, Cyan.g, Cyan.b, 0.025f));
            Fill(new Rect(0, scan + 45f * ui, w, 2f), new Color(Cyan.r, Cyan.g, Cyan.b, 0.10f));

            GUI.color = new Color(1f, 1f, 1f, 0.95f);
            GUI.DrawTexture(new Rect(0, 0, w, h), vignette);
            GUI.color = Color.white;
        }

        /// <summary>A system window: dark glass panel, glowing edge, title bar and bright corner brackets.</summary>
        public static void Window(Rect r, string title, GUIStyle titleStyle, float time, float alpha = 1f)
        {
            Rounded(new Rect(r.x - 3f, r.y - 3f, r.width + 6f, r.height + 6f), new Color(Cyan.r, Cyan.g, Cyan.b, 0.05f * alpha), 12f);
            Rounded(r, new Color(0.016f, 0.045f, 0.085f, 0.94f * alpha), 9f);
            Outline(r, new Color(Cyan.r, Cyan.g, Cyan.b, 0.55f * alpha), 1.5f, 9f);
            float bar = 30f;
            Rounded(new Rect(r.x + 2f, r.y + 2f, r.width - 4f, bar), new Color(Cyan.r, Cyan.g, Cyan.b, 0.11f * alpha), 7f);
            if (!string.IsNullOrEmpty(title))
            {
                GUI.color = new Color(1f, 1f, 1f, alpha);
                GUI.Label(new Rect(r.x + 12f, r.y + 2f, r.width - 40f, bar), title, titleStyle);
                GUI.color = Color.white;
            }
            float blink = ((int)(time * 2f) % 2 == 0) ? 0.9f : 0.25f;
            Fill(new Rect(r.xMax - 22f, r.y + 11f, 8f, 8f), new Color(Cyan.r, Cyan.g, Cyan.b, blink * alpha));
            Corners(r, new Color(Cyan.r, Cyan.g, Cyan.b, 0.95f * alpha), 16f);
        }

        public static void Corners(Rect r, Color c, float len)
        {
            const float t = 2f, o = 3f;
            Fill(new Rect(r.x - o, r.y - o, len, t), c); Fill(new Rect(r.x - o, r.y - o, t, len), c);
            Fill(new Rect(r.xMax + o - len, r.y - o, len, t), c); Fill(new Rect(r.xMax + o - t, r.y - o, t, len), c);
            Fill(new Rect(r.x - o, r.yMax + o - t, len, t), c); Fill(new Rect(r.x - o, r.yMax + o - len, t, len), c);
            Fill(new Rect(r.xMax + o - len, r.yMax + o - t, len, t), c); Fill(new Rect(r.xMax + o - t, r.yMax + o - len, t, len), c);
        }

        /// <summary>The part of <paramref name="text"/> typed so far, with a blinking cursor while it is still being typed.</summary>
        public static string Typewriter(string text, float seconds, float charsPerSecond = 34f)
        {
            if (string.IsNullOrEmpty(text)) return "";
            int n = Mathf.Clamp(Mathf.FloorToInt(seconds * charsPerSecond), 0, text.Length);
            bool typing = n < text.Length;
            return text.Substring(0, n) + (typing && ((int)(Time.unscaledTime * 3f) % 2 == 0) ? "_" : "");
        }

        public static float Hash(int n)
        {
            unchecked
            {
                int h = n * 374761393 + 668265263;
                h = (h ^ (h >> 13)) * 1274126177;
                h ^= h >> 16;
                return (h & 0xffff) / 65535f;
            }
        }

        public static float Smooth(float x) { x = Mathf.Clamp01(x); return x * x * (3f - 2f * x); }
    }
}
