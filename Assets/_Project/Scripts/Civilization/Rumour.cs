using System.Collections.Generic;
using UnityEngine;

namespace Evolve.Civilization
{
    /// <summary>The offenders one creature has heard about.</summary>
    public class Knowledge
    {
        readonly HashSet<object> offenders = new HashSet<object>();

        public bool Knows(object offender) => offender != null && offenders.Contains(offender);

        /// <summary>True if this was news.</summary>
        public bool Learn(object offender) => offender != null && offenders.Add(offender);

        public IEnumerable<object> Offenders => offenders;

        public int Count => offenders.Count;
    }

    /// <summary>News spreads from creature to creature: whoever knows tells whoever is near enough to talk to.</summary>
    public static class Rumour
    {
        /// <summary>
        /// One round of talk: every pair closer than <paramref name="radius"/> where both can talk shares what they know.
        /// Returns how many pieces of news were learned. Run about once a second, so news crosses one gap per round.
        /// </summary>
        public static int Spread(IList<Vector3> positions, IList<Knowledge> minds, IList<bool> canTalk, float radius)
        {
            int n = positions.Count;
            float r2 = radius * radius;
            // decide from a snapshot, so news moves at most one step per round
            var heard = new List<(int listener, object offender)>();
            for (int i = 0; i < n; i++)
            {
                if (!canTalk[i] || minds[i].Count == 0) continue;
                for (int j = 0; j < n; j++)
                {
                    if (i == j || !canTalk[j]) continue;
                    if ((positions[i] - positions[j]).sqrMagnitude > r2) continue;
                    foreach (var o in minds[i].Offenders) if (!minds[j].Knows(o)) heard.Add((j, o));
                }
            }
            int learned = 0;
            foreach (var h in heard) if (minds[h.listener].Learn(h.offender)) learned++;
            return learned;
        }
    }
}
