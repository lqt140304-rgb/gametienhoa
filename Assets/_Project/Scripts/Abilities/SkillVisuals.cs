using System.Collections.Generic;
using Evolve.Organs;
using Evolve.Swallow;
using UnityEngine;

namespace Evolve.Abilities
{
    /// <summary>
    /// The slime has no worn organs: the body part of an active move (spikes on the back, legs) exists only while the move is used.
    /// It grows out of its socket, stays for the move's duration, then melts back into the body (SkillRules.VisualScale).
    /// While spikes are out they hurt whoever bites the slime (ContactDamage, read by OrganHost).
    /// </summary>
    public class SkillVisuals : MonoBehaviour
    {
        class Show
        {
            public OrganSO Organ;
            public GameObject Visual;
            public Vector3 BaseScale = Vector3.one;
            public float Start, Hold;
        }

        readonly Dictionary<OrganSO, Show> shows = new Dictionary<OrganSO, Show>();
        readonly List<OrganSO> done = new List<OrganSO>();
        readonly Dictionary<OrganSlot, Transform> sockets = new Dictionary<OrganSlot, Transform>();
        bool socketsFound;

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
            if (shows.TryGetValue(organ, out var existing)) { existing.Start = Time.time; existing.Hold = hold; return; }
            var show = new Show { Organ = organ, Start = Time.time, Hold = hold };
            if (organ.VisualPrefab != null)
            {
                if (!socketsFound) FindSockets();
                Transform parent = sockets.TryGetValue(organ.Slot, out var s) ? s : transform;
                show.Visual = Instantiate(organ.VisualPrefab, parent, false);
                show.BaseScale = show.Visual.transform.localScale;
                show.Visual.transform.localScale = show.BaseScale * 0.001f;
            }
            shows[organ] = show;
        }

        void FindSockets()
        {
            socketsFound = true;
            foreach (var s in GetComponentsInChildren<OrganSocket>(true)) sockets[s.Slot] = s.transform;
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
                if (show.Visual != null) show.Visual.transform.localScale = show.BaseScale * Mathf.Max(0.001f, k);
                if (k >= 0.5f) contact += show.Organ.ContactDamage;
            }
            foreach (var o in done)
            {
                if (shows[o].Visual != null) Destroy(shows[o].Visual);
                shows.Remove(o);
            }
            ContactDamage = contact;
        }

        void OnDestroy()
        {
            foreach (var show in shows.Values) if (show.Visual != null) Destroy(show.Visual);
            shows.Clear();
        }
    }
}
