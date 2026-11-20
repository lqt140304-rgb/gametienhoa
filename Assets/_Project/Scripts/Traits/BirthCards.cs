using UnityEngine;
using UnityEngine.UI;

namespace Evolve.Traits
{
    /// <summary>
    /// A card of the fate draw (UGUI, built from code). The back is a dark panel with a slowly turning magic circle; the front shows a core (a glowing
    /// orb, its name, a line, its rarity) or a flaw (dark red, cracked). <see cref="SetFlip"/> squeezes the card to a line and widens it again, turning it
    /// over in the middle.
    /// </summary>
    internal sealed class BirthCard
    {
        public const float Width = 300f, Height = 430f;

        public RectTransform Root;
        public bool Flaw;
        GameObject back, face;
        RectTransform sq1, sq2, ring1;
        Image backGlow;
        Text title, body, footer;
        Image orb, orbRing;
        Image[] faceBorder = new Image[4];
        Image faceGlow, band, panel;
        Color accent = BirthUiKit.Cyan;

        public static BirthCard Create(RectTransform parent, string name, bool flaw)
        {
            var c = new BirthCard { Flaw = flaw };
            c.Root = BirthUiKit.Group(parent, name, Vector2.zero);
            Color tint = flaw ? BirthUiKit.Red : BirthUiKit.Cyan;
            var size = new Vector2(Width, Height);

            // ---- back
            var b = BirthUiKit.Group(c.Root, "Back", Vector2.zero);
            c.back = b.gameObject;
            c.backGlow = BirthUiKit.Glow(b, "Glow", Vector2.zero, Width * 1.9f, BirthUiKit.WithAlpha(tint, 0.12f));
            BirthUiKit.Box(b, "Panel", Vector2.zero, size, flaw ? new Color(0.08f, 0.02f, 0.03f, 0.97f) : new Color(0.02f, 0.05f, 0.095f, 0.97f));
            Border(b, size, BirthUiKit.WithAlpha(tint, 0.7f), 4f, null);
            Border(b, size - new Vector2(26f, 26f), BirthUiKit.WithAlpha(tint, 0.25f), 2f, null);
            c.ring1 = BirthUiKit.Circle(b, "Ring", Vector2.zero, Width * 0.78f, BirthUiKit.WithAlpha(tint, 0.6f)).rectTransform;
            BirthUiKit.Circle(b, "Ring2", Vector2.zero, Width * 0.36f, BirthUiKit.WithAlpha(tint, 0.7f));
            c.sq1 = Square(b, "Sq1", Width * 0.5f, BirthUiKit.WithAlpha(tint, 0.55f));
            c.sq2 = Square(b, "Sq2", Width * 0.5f, BirthUiKit.WithAlpha(tint, 0.55f));
            c.sq2.localRotation = Quaternion.Euler(0f, 0f, 45f);
            if (flaw)   // a crack across the back
            {
                var crack = BirthUiKit.Box(b, "Crack", Vector2.zero, new Vector2(4f, Height * 0.8f), BirthUiKit.WithAlpha(BirthUiKit.Red, 0.4f));
                crack.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 35f);
            }

            // ---- face
            var f = BirthUiKit.Group(c.Root, "Face", Vector2.zero);
            c.face = f.gameObject;
            c.faceGlow = BirthUiKit.Glow(f, "Glow", Vector2.zero, Width * 2.2f, BirthUiKit.WithAlpha(tint, 0.16f));
            c.panel = BirthUiKit.Box(f, "Panel", Vector2.zero, size, flaw ? new Color(0.09f, 0.025f, 0.035f, 0.98f) : new Color(0.022f, 0.055f, 0.1f, 0.98f));
            Border(f, size, tint, 6f, c.faceBorder);
            Border(f, size - new Vector2(26f, 26f), BirthUiKit.WithAlpha(tint, 0.35f), 2f, null);
            c.orb = BirthUiKit.Glow(f, "Orb", new Vector2(0f, 120f), 130f, tint);
            c.orbRing = BirthUiKit.Circle(f, "OrbRing", new Vector2(0f, 120f), 150f, tint);
            c.title = BirthUiKit.Label(f, "Title", new Vector2(0f, 12f), new Vector2(Width - 28f, 60f), "", 34, TextAnchor.MiddleCenter, Color.white, true);
            BirthUiKit.Box(f, "Divider", new Vector2(0f, -22f), new Vector2(Width * 0.6f, 2f), BirthUiKit.WithAlpha(tint, 0.6f));
            c.body = BirthUiKit.Label(f, "Body", new Vector2(0f, -100f), new Vector2(Width - 36f, 140f), "", 24, TextAnchor.UpperCenter, new Color(0.8f, 0.92f, 1f));
            c.band = BirthUiKit.Box(f, "Band", new Vector2(0f, -Height / 2f + 38f), new Vector2(Width - 36f, 36f), BirthUiKit.WithAlpha(tint, 0.25f));
            c.footer = BirthUiKit.Label(f, "Footer", new Vector2(0f, -Height / 2f + 38f), new Vector2(Width - 36f, 36f), "", 24, TextAnchor.MiddleCenter, tint, true);
            c.SetFlip(0f);
            return c;
        }

        static void Border(RectTransform parent, Vector2 size, Color color, float t, Image[] keep)
        {
            var a = BirthUiKit.Box(parent, "T", new Vector2(0f, size.y / 2f), new Vector2(size.x, t), color);
            var b = BirthUiKit.Box(parent, "B", new Vector2(0f, -size.y / 2f), new Vector2(size.x, t), color);
            var l = BirthUiKit.Box(parent, "L", new Vector2(-size.x / 2f, 0f), new Vector2(t, size.y), color);
            var r = BirthUiKit.Box(parent, "R", new Vector2(size.x / 2f, 0f), new Vector2(t, size.y), color);
            if (keep != null) { keep[0] = a; keep[1] = b; keep[2] = l; keep[3] = r; }
        }

        static RectTransform Square(RectTransform parent, string name, float side, Color color)
        {
            var g = BirthUiKit.Group(parent, name, Vector2.zero);
            Border(g, new Vector2(side, side), color, 2f, null);
            return g;
        }

        /// <summary>What the front shows. <paramref name="orbColor"/> colours a core's orb; a flaw has none.</summary>
        public void SetFace(string titleText, string bodyText, string footerText, Color accentColor, Color orbColor)
        {
            accent = accentColor;
            title.text = titleText; body.text = bodyText; footer.text = footerText;
            footer.color = accentColor;
            foreach (var e in faceBorder) e.color = accentColor;
            faceGlow.color = BirthUiKit.WithAlpha(accentColor, 0.2f);
            band.color = BirthUiKit.WithAlpha(accentColor, 0.25f);
            orb.gameObject.SetActive(!Flaw); orbRing.gameObject.SetActive(!Flaw);
            if (!Flaw) { orb.color = orbColor; orbRing.color = accentColor; }
            title.rectTransform.anchoredPosition = Flaw ? new Vector2(0f, 150f) : new Vector2(0f, 12f);
            body.rectTransform.anchoredPosition = Flaw ? new Vector2(0f, -10f) : new Vector2(0f, -100f);
            body.rectTransform.sizeDelta = Flaw ? new Vector2(Width - 36f, 250f) : new Vector2(Width - 36f, 140f);
            body.fontSize = Flaw ? 22 : 24;
        }

        /// <summary>0 = face down, 1 = face up; in between the card is squeezed and the sides swap in the middle.</summary>
        public void SetFlip(float flip)
        {
            float sx = Mathf.Max(0.03f, Mathf.Abs(Mathf.Cos(Mathf.PI * flip)));
            var s = Root.localScale;
            Root.localScale = new Vector3(sx * Mathf.Abs(baseScale), Mathf.Abs(baseScale), 1f);
            back.SetActive(flip < 0.5f);
            face.SetActive(flip >= 0.5f);
        }

        float baseScale = 1f;
        public void SetScale(float s) { baseScale = s; }

        public void Tick(float time, float instability)
        {
            if (sq1 == null || sq2 == null || ring1 == null || backGlow == null) return;
            sq1.localRotation = Quaternion.Euler(0f, 0f, time * 9f);
            sq2.localRotation = Quaternion.Euler(0f, 0f, 45f - time * 9f);
            ring1.localRotation = Quaternion.Euler(0f, 0f, time * 5f);
            float pulse = 0.5f + 0.5f * Mathf.Sin(time * 3f);
            backGlow.color = BirthUiKit.WithAlpha(Flaw ? BirthUiKit.Red : BirthUiKit.Cyan, 0.08f + 0.08f * pulse + 0.1f * instability);
        }
    }
}
