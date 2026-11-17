using Evolve.Survival;
using UnityEngine;

namespace Evolve.World
{
    /// <summary>
    /// Thối Gel spreads by contact: standing next to another player who has it can infect this one. Each machine decides
    /// for its own player, from the position and disease mask of the other players' ghosts.
    /// </summary>
    [RequireComponent(typeof(SurvivalStats))]
    public class ContagionSensor : MonoBehaviour
    {
        SurvivalStats stats;

        /// <summary>Pure test: can this other player pass the disease on right now?</summary>
        public static bool ShouldTryInfect(bool sourceIsRemote, bool sourceDead, bool sourceSick, float distanceXZ)
            => sourceIsRemote && !sourceDead && sourceSick && distanceXZ <= DiseaseRules.ContagionRange;

        void Awake() => stats = GetComponent<SurvivalStats>();

        void Update()
        {
            if (stats.IsRemote || stats.State.IsDead || stats.State.Has(DiseaseKind.RottenGel)) return;
            var all = SurvivalStats.All;
            Vector3 mine = transform.position;
            for (int i = 0; i < all.Count; i++)
            {
                var other = all[i];
                if (other == null || other == stats) continue;
                Vector3 d = other.transform.position - mine; d.y = 0f;
                if (!ShouldTryInfect(other.IsRemote, other.State.IsDead, other.State.Has(DiseaseKind.RottenGel), d.magnitude)) continue;
                stats.State.TryInfect(DiseaseKind.RottenGel, DiseaseRules.ContagionPerSecond * Time.deltaTime);
                return; // one roll per frame is enough
            }
        }
    }
}
