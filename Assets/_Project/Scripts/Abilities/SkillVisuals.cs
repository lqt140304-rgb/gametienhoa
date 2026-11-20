using System.Collections.Generic;
using Evolve.Organs;
using Evolve.Swallow;
using Evolve.Visuals;
using UnityEngine;

namespace Evolve.Abilities
{
    /// <summary>
    /// The slime has no worn organs: the body part of an active move exists only while the move is used, and it is pulled out of the slime's own
    /// body (SlimeGelAnimator.Limb), so it is one smooth piece with it: legs for Chạy Nước Rút, spikes on the back for Phóng Gai. They grow out,
    /// stay for the move's duration and flow back in (SkillRules.VisualScale). While spikes are out they hurt whoever bites the slime
    /// (ContactDamage, read by OrganHost). Cave skills draw their own limbs (SkillFx).
    /// </summary>
    public class SkillVisuals : MonoBehaviour
    {
        class Show
        {
            public OrganSO Organ;
            public float Start, Hold;
            public readonly List<SlimeGelAnimator.Limb> Limbs = new List<SlimeGelAnimator.Limb>();
            public Vector3[] Dirs;
            public bool Legs;
        }

        // legs: four, out to the sides and a little down; spikes: a fan over the back of the hump
        static readonly Vector3[] LegDirs =
        {
            new Vector3(1f, -0.35f, 1f).normalized, new Vector3(-1f, -0.35f, 1f).normalized,
            new Vector3(-1f, -0.35f, -1f).normalized, new Vector3(1f, -0.35f, -1f).normalized,
        };
        static readonly Vector3[] SpikeDirs =
        {
            new Vector3(0f, 1f, -0.45f).normalized, new Vector3(0.45f, 1f, -0.5f).normalized, new Vector3(-0.45f, 1f, -0.5f).normalized,
            new Vector3(0.8f, 0.8f, -0.2f).normalized, new Vector3(-0.8f, 0.8f, -0.2f).normalized, new Vector3(0f, 0.75f, -0.9f).normalized,
        };

        readonly Dictionary<OrganSO, Show> shows = new Dictionary<OrganSO, Show>();
        readonly List<OrganSO> done = new List<OrganSO>();

        /// <summary>Damage dealt back to attackers from the spikes that are out right now.</summary>
        public float ContactDamage { get; private set; }

        void OnEnable()
        {
            var host = GetComponent<OrganHost>();
            if (host != null) host.Visuals = this;
        }

        void OnDisable()
        {
            var host = GetComponent<OrganHost>();
            if (host != null && host.Visuals == this) host.Visuals = null;
        }

        /// <summary>Hold time of a move's body part: Sprint lasts as long as the boost; shots stay out a moment.</summary>
        public static float HoldFor(AbilitySO ability, int stars)
        {
            if (ability is SprintAbility) return SkillRules.SprintSeconds(stars);
            if (ability is SpikeBurstAbility) return 1f;
            return 0.6f;
        }

        /// <summary>Grow the organ's body part now and keep it for <paramref name="hold"/> seconds. Using the move again restarts it (one part, never two).</summary>
        public void Play(OrganSO organ, float hold)
        {
            if (organ == null) return;
            if (shows.TryGetValue(organ, out var existing))
            {
                // using it again while the part is still out: keep its current size and carry on (no pop down to nothing)
                float k = SkillRules.VisualScale(Time.time - existing.Start, existing.Hold);
                existing.Start = Time.time - SkillRules.GrowSeconds * Mathf.Clamp01(k);
                existing.Hold = hold;
                return;
            }
            bool legs = organ.GrantedAbility == null && organ.SpeedMultiplier > 1.01f;
            bool spikes = organ.GrantedAbility is SpikeBurstAbility || organ.ContactDamage > 0f;
            var show = new Show { Organ = organ, Start = Time.time, Hold = hold, Legs = legs, Dirs = legs ? LegDirs : spikes ? SpikeDirs : null };
            if (show.Dirs != null)
                foreach (var _ in show.Dirs)
                {
                    var limb = SlimeGelAnimator.AddLimb(gameObject);
                    if (limb == null) break;
                    limb.Local = true;
                    if (legs) { limb.Width = 24f; limb.Sharp = 2.5f; limb.Squeeze = 0.35f; }
                    else { limb.Width = 16f; limb.Sharp = 6f; limb.Squeeze = 0.5f; }
                    show.Limbs.Add(limb);
                }
            shows[organ] = show;
        }

        void Update()
        {
            float contact = 0f;
            done.Clear();
            foreach (var kv in shows)
            {
                var show = kv.Value;
                float elapsed = Time.time - show.Start;
                if (elapsed >= SkillRules.ShowSeconds(show.Hold)) { done.Add(kv.Key); continue; }
                float k = SkillRules.VisualScale(elapsed, show.Hold);
                float length = show.Legs ? 0.32f : 0.3f;
                for (int i = 0; i < show.Limbs.Count; i++)
                {
                    var limb = show.Limbs[i];
                    // legs step in diagonal pairs; spikes just stand out
                    float step = show.Legs ? 0.75f + 0.25f * Mathf.Sin(Time.time * 14f + (i % 2) * Mathf.PI) : 1f;
                    limb.Target = SlimeGelAnimator.LimbCentre + show.Dirs[i] * (SlimeGelAnimator.SurfaceRadius + length);
                    limb.Reach = k * step;
                }
                if (k >= 0.5f) contact += show.Organ.ContactDamage;
            }
            foreach (var o in done) { Release(shows[o]); shows.Remove(o); }
            ContactDamage = contact;
        }

        static void Release(Show show) { foreach (var l in show.Limbs) l.Remove(); show.Limbs.Clear(); }

        /// <summary>Death and respawn: every part goes away at once.</summary>
        public void ClearAll()
        {
            foreach (var show in shows.Values) Release(show);
            shows.Clear();
            ContactDamage = 0f;
        }

        void OnDestroy() => ClearAll();
    }
}
