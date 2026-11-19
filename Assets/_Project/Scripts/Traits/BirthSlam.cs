using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Evolve.Traits
{
    /// <summary>
    /// The name slam (spec 5.6): each name is a huge block of text that comes in at about three times its size, crashes down, bounces a little and stays,
    /// tilted at its angle from <see cref="SlamPlan"/>. At the moment of impact the scene gets a callback to shake the screen, flash, burst and play the sound.
    /// </summary>
    internal sealed class BirthSlam
    {
        const float FlyIn = 0.22f;

        sealed class Item
        {
            public SlamPlan.Slam Plan;
            public RectTransform Root;
            public Text Main, Shadow;
            public Vector2 Pos;
            public bool Hit;
            public Color Color;
        }

        readonly List<Item> items = new List<Item>();

        /// <summary>Called once at each impact: the slam, where it landed on the canvas, and its colour.</summary>
        public System.Action<SlamPlan.Slam, Vector2, Color> OnImpact;

        public BirthSlam(RectTransform parent, IReadOnlyList<SlamPlan.Slam> plan, Vector2 canvas, TraitRarity coreRarity)
        {
            int gift = 0;
            for (int i = 0; i < plan.Count; i++)
            {
                var s = plan[i];
                var item = new Item { Plan = s };
                Color color; Vector2 pos;
                switch (s.Kind)
                {
                    case SlamKind.Core:
                        color = coreRarity == TraitRarity.Mythic ? BirthUiKit.Gold : coreRarity == TraitRarity.Rare ? new Color(0.45f, 0.72f, 1f) : new Color(0.9f, 0.96f, 1f);
                        pos = new Vector2(-canvas.x * 0.04f, canvas.y * 0.2f);
                        break;
                    case SlamKind.Flaw:
                        color = BirthUiKit.Red;
                        pos = new Vector2(canvas.x * (i % 2 == 1 ? 0.07f : -0.07f), canvas.y * (i == 1 ? -0.02f : -0.14f));
                        break;
                    default:
                        color = BirthUiKit.Cyan;
                        // six gifts in two columns of three, low on the screen
                        pos = new Vector2(canvas.x * (gift % 2 == 0 ? -0.22f : 0.22f), canvas.y * (-0.27f - 0.075f * (gift / 2)));
                        gift++;
                        break;
                }
                item.Color = color; item.Pos = pos;
                float width = canvas.x * (s.Kind == SlamKind.Gift ? 0.34f : 0.8f) * (s.Kind == SlamKind.Core ? 1f : s.Scale);
                float height = canvas.y * (s.Kind == SlamKind.Gift ? 0.1f : 0.26f) * s.Scale;
                item.Root = BirthUiKit.Group(parent, "Slam" + i, pos);
                item.Shadow = BirthUiKit.Label(item.Root, "Shadow", new Vector2(6f, -6f), new Vector2(width, height), s.Text, 200, TextAnchor.MiddleCenter, new Color(0f, 0f, 0f, 0.7f), true);
                item.Main = BirthUiKit.Label(item.Root, "Text", Vector2.zero, new Vector2(width, height), s.Text, 200, TextAnchor.MiddleCenter, color, true);
                foreach (var t in new[] { item.Shadow, item.Main })
                {
                    t.resizeTextForBestFit = true; t.resizeTextMinSize = 20; t.resizeTextMaxSize = 220;
                    t.horizontalOverflow = HorizontalWrapMode.Wrap;
                }
                item.Root.gameObject.SetActive(false);
                items.Add(item);
            }
        }

        public void Tick(float age)
        {
            foreach (var it in items)
            {
                float t = age - it.Plan.At;
                if (t < 0f) { it.Root.gameObject.SetActive(false); continue; }
                it.Root.gameObject.SetActive(true);
                float scale, alpha = 1f;
                if (t < FlyIn)
                {
                    float k = t / FlyIn;
                    scale = Mathf.Lerp(3f, 1f, k * k);
                    alpha = Mathf.Clamp01(k * 2f);
                }
                else
                {
                    if (!it.Hit) { it.Hit = true; OnImpact?.Invoke(it.Plan, it.Pos, it.Color); }
                    float tt = t - FlyIn;
                    scale = 1f + 0.09f * Mathf.Exp(-tt * 9f) * Mathf.Cos(tt * 38f);   // a small bounce after the hit
                }
                it.Root.localScale = Vector3.one * scale;
                it.Root.localRotation = Quaternion.Euler(0f, 0f, it.Plan.Angle);
                it.Main.color = BirthUiKit.WithAlpha(it.Color, alpha);
                it.Shadow.color = new Color(0f, 0f, 0f, 0.7f * alpha);
            }
        }
    }
}
