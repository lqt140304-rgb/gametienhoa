using System;
using UnityEngine;
using UnityEngine.UI;

namespace Evolve.Traits
{
    /// <summary>
    /// The six gift scenes (spec 5.5): each Ân Điển has a scene of its own, simple on purpose: a shape built from a few discs, rings and boxes and one
    /// movement that says what the ability does. Created from code; <see cref="Tick"/> animates it from the time since the scene began.
    /// </summary>
    internal sealed class BirthGiftScene
    {
        Action<float, float> tick;
        public RectTransform Root;

        public static BirthGiftScene Create(int index, RectTransform parent, Vector2 center)
        {
            var g = new BirthGiftScene { Root = BirthUiKit.Group(parent, "Gift" + index, center) };
            switch (index)
            {
                case 0: g.Pouch(); break;
                case 1: g.Swallow(); break;
                case 2: g.Decode(); break;
                case 3: g.Morph(); break;
                case 4: g.Sonar(); break;
                default: g.Split(); break;
            }
            return g;
        }

        public void Tick(float t, float time) => tick?.Invoke(t, time);

        static readonly Color Blue = new Color(0.3f, 0.7f, 1f);

        // 1. Túi Không Đáy: a dark pouch; glowing orbs spiral in and it swells with each one.
        void Pouch()
        {
            var halo = BirthUiKit.Glow(Root, "Halo", Vector2.zero, 420f, BirthUiKit.WithAlpha(Blue, 0.2f));
            var pouch = BirthUiKit.Glow(Root, "Pouch", Vector2.zero, 240f, new Color(0.01f, 0.02f, 0.05f, 1f));
            var rim = BirthUiKit.Circle(Root, "Rim", Vector2.zero, 250f, BirthUiKit.WithAlpha(Blue, 0.9f));
            var orbs = new Image[9];
            for (int i = 0; i < orbs.Length; i++) orbs[i] = BirthUiKit.Glow(Root, "Orb" + i, Vector2.zero, 60f, new Color(0.7f, 0.95f, 1f));
            tick = (t, time) =>
            {
                int arrived = 0;
                for (int i = 0; i < orbs.Length; i++)
                {
                    float u = Mathf.Clamp01((t - 0.3f * i) / 1.8f);
                    if (u >= 1f) arrived++;
                    float ang = i * 0.7f + u * 4f, r = 380f * (1f - u * u);
                    orbs[i].rectTransform.anchoredPosition = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * r;
                    orbs[i].rectTransform.sizeDelta = Vector2.one * (60f * (1f - 0.6f * u));
                    orbs[i].color = BirthUiKit.WithAlpha(orbs[i].color, u <= 0f ? 0f : (u >= 1f ? 0f : 1f));
                }
                float s = 1f + 0.05f * arrived + 0.03f * Mathf.Sin(time * 6f);
                pouch.rectTransform.localScale = rim.rectTransform.localScale = Vector3.one * s;
                halo.color = BirthUiKit.WithAlpha(Blue, 0.15f + 0.04f * arrived);
            };
        }

        // 2. Nuốt Chửng: a blob stretches toward a small orb and swallows it.
        void Swallow()
        {
            var blob = BirthUiKit.Glow(Root, "Blob", new Vector2(-140f, 0f), 260f, BirthUiKit.WithAlpha(Blue, 0.95f));
            var core = BirthUiKit.Glow(Root, "Core", new Vector2(-140f, 0f), 140f, new Color(0.7f, 0.95f, 1f, 0.7f));
            var prey = BirthUiKit.Glow(Root, "Prey", new Vector2(260f, 0f), 80f, new Color(1f, 0.85f, 0.4f));
            tick = (t, time) =>
            {
                float u = Mathf.Clamp01(t / 2.6f);
                float reach = BirthUiKit.Smooth(Mathf.Clamp01(u * 1.6f));                // stretch out
                float back = BirthUiKit.Smooth(Mathf.Clamp01((u - 0.55f) / 0.45f));      // settle after the gulp
                float x = Mathf.Lerp(-140f, 150f, reach) - 60f * back;
                blob.rectTransform.anchoredPosition = core.rectTransform.anchoredPosition = new Vector2(x, 0f);
                float stretch = 1f + 0.8f * Mathf.Sin(Mathf.Clamp01(u / 0.6f) * Mathf.PI);
                blob.rectTransform.localScale = new Vector3(stretch, 1f / Mathf.Sqrt(stretch), 1f);
                prey.color = BirthUiKit.WithAlpha(prey.color, u < 0.5f ? 1f : Mathf.Clamp01(1f - (u - 0.5f) * 6f));
                prey.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, 0.3f, Mathf.Clamp01((u - 0.35f) * 3f));
            };
        }

        // 3. Giải Mã: columns of falling symbols resolve into a bright double helix.
        void Decode()
        {
            const int cols = 9, rows = 7;
            var texts = new Text[cols, rows];
            for (int c = 0; c < cols; c++)
                for (int r = 0; r < rows; r++)
                    texts[c, r] = BirthUiKit.Label(Root, "g", new Vector2((c - 4) * 70f, 0f), new Vector2(60f, 50f), "0", 40, TextAnchor.MiddleCenter, BirthUiKit.WithAlpha(BirthUiKit.Cyan, 0.8f), true);
            var dots = new Image[2, 14];
            for (int s = 0; s < 2; s++)
                for (int i = 0; i < 14; i++) dots[s, i] = BirthUiKit.Glow(Root, "d", Vector2.zero, 34f, new Color(0.8f, 1f, 1f, 0f));
            const string glyphs = "01ΣΔ▓#%&§";
            tick = (t, time) =>
            {
                float fade = 1f - BirthUiKit.Smooth((t - 1.8f) / 1.0f);
                for (int c = 0; c < cols; c++)
                    for (int r = 0; r < rows; r++)
                    {
                        float speed = 180f + 90f * BirthUiKit.Hash(c * 17);
                        float y = 320f - Mathf.Repeat(time * speed + r * 105f + c * 40f, 740f);
                        texts[c, r].rectTransform.anchoredPosition = new Vector2((c - 4) * 70f, y);
                        int pick = (int)(BirthUiKit.Hash(Mathf.FloorToInt(time * 8f) + c * 7 + r * 13) * glyphs.Length) % glyphs.Length;
                        texts[c, r].text = glyphs[pick].ToString();
                        texts[c, r].color = BirthUiKit.WithAlpha(BirthUiKit.Cyan, 0.8f * fade);
                    }
                float show = BirthUiKit.Smooth((t - 1.6f) / 1.2f);
                for (int s = 0; s < 2; s++)
                    for (int i = 0; i < 14; i++)
                    {
                        float x = (i - 6.5f) * 46f, ph = i * 0.55f + time * 2f + s * Mathf.PI;
                        dots[s, i].rectTransform.anchoredPosition = new Vector2(x, Mathf.Sin(ph) * 120f);
                        dots[s, i].rectTransform.sizeDelta = Vector2.one * (26f + 14f * Mathf.Cos(ph));
                        dots[s, i].color = new Color(0.8f, 1f, 1f, show);
                    }
            };
        }

        // 4. Biến Hình: one shape turns from a blob into a spiky star, then a long bar, then back.
        void Morph()
        {
            var blob = BirthUiKit.Glow(Root, "Blob", Vector2.zero, 280f, BirthUiKit.WithAlpha(Blue, 0.95f));
            var bar = BirthUiKit.Glow(Root, "Bar", Vector2.zero, 280f, BirthUiKit.WithAlpha(Blue, 0f));
            var spikes = new Image[8];
            for (int i = 0; i < 8; i++)
            {
                spikes[i] = BirthUiKit.Box(Root, "Spike" + i, Vector2.zero, new Vector2(26f, 300f), BirthUiKit.WithAlpha(Blue, 0f));
                spikes[i].rectTransform.localRotation = Quaternion.Euler(0f, 0f, i * 22.5f);
            }
            tick = (t, time) =>
            {
                float ph = Mathf.Repeat(t, 3.6f) / 3.6f;   // blob -> star -> bar -> blob
                float wStar = Mathf.Clamp01(1f - Mathf.Abs(ph - 0.33f) * 5f);
                float wBar = Mathf.Clamp01(1f - Mathf.Abs(ph - 0.66f) * 5f);
                float wBlob = Mathf.Clamp01(1f - wStar - wBar);
                blob.color = BirthUiKit.WithAlpha(Blue, 0.95f * Mathf.Max(wBlob, 0.25f * wStar));
                blob.rectTransform.localScale = Vector3.one * (1f + 0.05f * Mathf.Sin(time * 5f));
                bar.color = BirthUiKit.WithAlpha(Blue, 0.95f * wBar);
                bar.rectTransform.localScale = new Vector3(2.6f, 0.45f, 1f);
                foreach (var s in spikes) s.color = BirthUiKit.WithAlpha(Blue, 0.9f * wStar);
                Root.localRotation = Quaternion.Euler(0f, 0f, wStar * 12f);
            };
        }

        // 5. Sóng Dò: rings of sound spread out and light up what they touch.
        void Sonar()
        {
            var centre = BirthUiKit.Glow(Root, "Centre", Vector2.zero, 90f, new Color(0.8f, 1f, 1f));
            var rings = new Image[4];
            for (int i = 0; i < rings.Length; i++) rings[i] = BirthUiKit.Circle(Root, "Ring" + i, Vector2.zero, 100f, BirthUiKit.WithAlpha(BirthUiKit.Cyan, 0.9f));
            var targets = new Vector2[] { new Vector2(-300f, 120f), new Vector2(240f, 200f), new Vector2(330f, -110f), new Vector2(-170f, -190f), new Vector2(40f, 300f) };
            var dots = new Image[targets.Length];
            for (int i = 0; i < dots.Length; i++) dots[i] = BirthUiKit.Glow(Root, "T" + i, targets[i], 36f, new Color(1f, 0.85f, 0.4f, 0.2f));
            tick = (t, time) =>
            {
                centre.rectTransform.localScale = Vector3.one * (1f + 0.15f * Mathf.Sin(time * 6f));
                for (int i = 0; i < rings.Length; i++)
                {
                    float u = Mathf.Repeat(t * 0.55f - i * 0.25f, 1f);
                    float radius = u * 760f;
                    rings[i].rectTransform.sizeDelta = Vector2.one * (radius * 2f + 4f);
                    rings[i].color = BirthUiKit.WithAlpha(BirthUiKit.Cyan, 0.9f * (1f - u));
                }
                for (int j = 0; j < dots.Length; j++)
                {
                    float best = 0f, d = targets[j].magnitude;
                    for (int i = 0; i < rings.Length; i++)
                    {
                        float u = Mathf.Repeat(t * 0.55f - i * 0.25f, 1f);
                        best = Mathf.Max(best, Mathf.Clamp01(1f - Mathf.Abs(u * 760f - d) / 70f));
                    }
                    dots[j].color = new Color(1f, 0.85f, 0.4f, 0.2f + 0.8f * best);
                    dots[j].rectTransform.localScale = Vector3.one * (1f + 0.8f * best);
                }
            };
        }

        // 6. Phân Thân: a small blob splits off from the big one, drifts away and comes back (the ability is not built yet).
        void Split()
        {
            var big = BirthUiKit.Glow(Root, "Big", new Vector2(-60f, 0f), 280f, BirthUiKit.WithAlpha(Blue, 0.95f));
            var small = BirthUiKit.Glow(Root, "Small", new Vector2(-60f, 0f), 120f, BirthUiKit.WithAlpha(new Color(0.6f, 0.95f, 1f), 0.95f));
            var note = BirthUiKit.Label(Root, "Note", new Vector2(0f, -250f), new Vector2(600f, 50f), "(sắp có)", 36, TextAnchor.MiddleCenter, BirthUiKit.WithAlpha(BirthUiKit.Gold, 0.9f), true);
            tick = (t, time) =>
            {
                float u = Mathf.Repeat(t / 3f, 1f);
                float out01 = u < 0.5f ? BirthUiKit.Smooth(u / 0.35f) : BirthUiKit.Smooth(1f - (u - 0.55f) / 0.35f);
                float x = Mathf.Lerp(-60f, 290f, Mathf.Clamp01(out01));
                small.rectTransform.anchoredPosition = new Vector2(x, 30f * Mathf.Sin(time * 4f) * out01);
                float pull = Mathf.Clamp01(out01 * 3f) * (1f - Mathf.Clamp01((out01 - 0.7f) * 3.3f));
                big.rectTransform.localScale = new Vector3(1f + 0.12f * pull, 1f, 1f);
                note.color = BirthUiKit.WithAlpha(BirthUiKit.Gold, 0.6f + 0.35f * Mathf.Sin(time * 3f));
            };
        }
    }
}
