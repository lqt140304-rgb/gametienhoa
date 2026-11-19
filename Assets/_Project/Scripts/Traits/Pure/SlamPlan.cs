using System;
using System.Collections.Generic;

namespace Evolve.Traits
{
    public enum SlamKind { Core, Flaw, Gift }

    /// <summary>
    /// The name slam at the end of the scene (spec 5.6): the core, then each flaw, then the six gifts, each a huge name that crashes onto the screen
    /// at its own tilt with a screen shake. This only decides what, in what order, at what angle and how hard; the scene draws it.
    /// </summary>
    public static class SlamPlan
    {
        public struct Slam
        {
            public string Text;
            public SlamKind Kind;
            /// <summary>Degrees; negative leans left, positive right.</summary>
            public float Angle;
            /// <summary>Size of the name relative to the biggest (core = 1).</summary>
            public float Scale;
            /// <summary>Shake strength of the hit (core = 1; a mythic core 2).</summary>
            public float Shake;
            /// <summary>Seconds after the slam step began.</summary>
            public float At;
        }

        const float CoreAt = 0.5f, FlawGap = 2.2f, GiftLead = 1.8f, GiftGap = 0.6f, Hold = 2.5f;
        static readonly float[] GiftAngles = { 5f, -8f, 4f, -6f, 9f, -4f };

        public static List<Slam> Build(string coreName, TraitRarity rarity, IReadOnlyList<string> flawNames, IReadOnlyList<string> giftNames)
        {
            var list = new List<Slam>
            {
                new Slam { Text = coreName ?? "", Kind = SlamKind.Core, Angle = -7f, Scale = 1f, Shake = rarity == TraitRarity.Mythic ? 2f : 1f, At = CoreAt }
            };
            float at = CoreAt;
            if (flawNames != null)
                for (int i = 0; i < flawNames.Count; i++)
                {
                    at += FlawGap;
                    list.Add(new Slam { Text = flawNames[i], Kind = SlamKind.Flaw, Angle = i % 2 == 0 ? 6f : -5f, Scale = 0.95f, Shake = 0.9f, At = at });
                }
            if (giftNames != null)
                for (int i = 0; i < giftNames.Count; i++)
                {
                    at += i == 0 ? GiftLead : GiftGap;
                    list.Add(new Slam
                    {
                        Text = giftNames[i], Kind = SlamKind.Gift, Angle = GiftAngles[i % GiftAngles.Length], Scale = 0.6f,
                        Shake = Math.Max(0.25f, 0.5f - 0.05f * i), At = at
                    });
                }
            return list;
        }

        /// <summary>How long the slam step lasts: the last hit plus a moment to let the names sit. 0 for an empty plan.</summary>
        public static float TotalSeconds(IReadOnlyList<Slam> slams) => slams == null || slams.Count == 0 ? 0f : slams[slams.Count - 1].At + Hold;
    }
}
