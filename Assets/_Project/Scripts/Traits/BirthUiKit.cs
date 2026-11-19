using UnityEngine;
using UnityEngine.UI;

namespace Evolve.Traits
{
    /// <summary>Small helpers that build the birth scene's UGUI objects from code (solid rectangles, soft discs, rings, text) so no prefab or asset is needed.</summary>
    internal static class BirthUiKit
    {
        public static readonly Color Cyan = new Color(0.72f, 0.58f, 1f);
        public static readonly Color Red = new Color(0.95f, 0.28f, 0.34f);
        public static readonly Color Gold = new Color(1f, 0.8f, 0.25f);
        public static readonly Color Deep = new Color(0.035f, 0.012f, 0.075f);

        static Sprite disc, ring;
        static Font font;

        public static Font Font
        {
            get
            {
                if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");   // the built-in font; it shows Vietnamese like the old IMGUI text did
                return font;
            }
        }

        public static Sprite Disc => disc != null ? disc : (disc = MakeSprite(64, (d) => { float a = Mathf.Clamp01(1f - d); return a * a; }));

        public static Sprite Ring => ring != null ? ring : (ring = MakeSprite(128, (d) => Mathf.Clamp01(1f - Mathf.Abs(d - 0.92f) / 0.05f)));

        static Sprite MakeSprite(int n, System.Func<float, float> alphaAtRadius)
        {
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f)) / (n / 2f);
                    px[y * n + x] = new Color(1f, 1f, 1f, alphaAtRadius(d));
                }
            tex.SetPixels(px); tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        public static void Release()
        {
            if (disc != null) { Object.Destroy(disc.texture); Object.Destroy(disc); }
            if (ring != null) { Object.Destroy(ring.texture); Object.Destroy(ring); }
            disc = ring = null;
        }

        // ---------------------------------------------------------------- building

        static RectTransform NewRect(RectTransform parent, string name, Vector2 pos, Vector2 size, params System.Type[] components)
        {
            var go = new GameObject(name, components);
            var rt = go.GetComponent<RectTransform>();
            if (rt == null) rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Group(RectTransform parent, string name, Vector2 pos) => NewRect(parent, name, pos, Vector2.zero, typeof(RectTransform));

        /// <summary>A solid rectangle.</summary>
        public static Image Box(RectTransform parent, string name, Vector2 pos, Vector2 size, Color color)
        {
            var rt = NewRect(parent, name, pos, size, typeof(RectTransform), typeof(Image));
            var img = rt.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>A soft glowing disc.</summary>
        public static Image Glow(RectTransform parent, string name, Vector2 pos, float size, Color color)
        {
            var img = Box(parent, name, pos, new Vector2(size, size), color);
            img.sprite = Disc;
            return img;
        }

        /// <summary>A thin circle outline.</summary>
        public static Image Circle(RectTransform parent, string name, Vector2 pos, float size, Color color)
        {
            var img = Box(parent, name, pos, new Vector2(size, size), color);
            img.sprite = Ring;
            return img;
        }

        public static Text Label(RectTransform parent, string name, Vector2 pos, Vector2 size, string text, int fontSize, TextAnchor anchor, Color color, bool bold = false)
        {
            var rt = NewRect(parent, name, pos, size, typeof(RectTransform), typeof(Text));
            var t = rt.GetComponent<Text>();
            t.font = Font;
            t.fontSize = fontSize;
            t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            t.alignment = anchor;
            t.color = color;
            t.text = text;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.supportRichText = false;
            t.raycastTarget = false;
            return t;
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

        public static Color WithAlpha(Color c, float a) => new Color(c.r, c.g, c.b, a);
    }
}
