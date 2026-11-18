using System;
using System.Collections.Generic;
using Evolve.Organs;
using UnityEngine;

namespace Evolve.Traits
{
    /// <summary>
    /// The core and flaws one player picked at the start. Effects ask this component whether the player has a core or flaw
    /// and how strongly a flaw is felt after the worn organs compensate it. Nothing here runs by itself.
    /// </summary>
    public class PlayerTraits : MonoBehaviour
    {
        static readonly string[] NoFlaws = new string[0];

        string coreId;
        string[] flawIds = NoFlaws;
        OrganHost organs;
        Evolve.Player.SlimeController slime;
        Evolve.Survival.SurvivalStats stats;
        readonly FamiliarityGrid familiarity = new FamiliarityGrid();
        readonly List<(OrganSlot slot, float integrity)> worn = new List<(OrganSlot, float)>();

        public string CoreId => coreId;
        public IReadOnlyList<string> FlawIds => flawIds;
        public event Action Changed;

        void Awake()
        {
            organs = GetComponent<OrganHost>();
            slime = GetComponent<Evolve.Player.SlimeController>();
            stats = GetComponent<Evolve.Survival.SurvivalStats>();
            if (slime != null) TraitNet.Local = this;   // only the local player owns a SlimeController; remote avatars get a PlayerTraits without one
        }

        void OnDestroy() { if (TraitNet.Local == this) TraitNet.Local = null; }

        void OnEnable()
        {
            if (stats == null) stats = GetComponent<Evolve.Survival.SurvivalStats>();
            if (stats != null) stats.State.Damaged += OnDamaged;
        }

        void OnDisable()
        {
            if (stats != null) stats.State.Damaged -= OnDamaged;
        }

        // Run Rẩy: a hit freezes the body for a moment.
        void OnDamaged(float amount)
        {
            if (slime != null && HasFlaw(FlawRules.RunRay)) slime.FreezeFor(FlawRules.HitFreezeSeconds(FlawStrength(FlawRules.RunRay)));
        }

        // Nhân Quen Đất: the ground under the player becomes familiar; swimming is not covered.
        void Update()
        {
            if (stats != null && slime != null)   // flags that change the local body; a remote avatar's numbers come from its owner
            {
                var state = stats.State;
                state.HungerBurnMultiplier = FlawRules.HungerBurnMultiplier(FlawStrength(FlawRules.DoiSom));
                state.AbsorbFlawStrength = FlawStrength(FlawRules.DaDayKen);
            }
            if (slime == null) return;
            if (!HasCore("core.quen_dat") || slime.IsSwimming) { slime.TraitSpeed = 1f; return; }
            var p = new Vector2(transform.position.x, transform.position.z);
            familiarity.Visit(p, Time.time);
            slime.TraitSpeed = familiarity.SpeedMultiplier(p, Time.time);
        }

        public bool HasCore(string id) => coreId != null && coreId == id;

        public bool HasFlaw(string id) => id != null && Array.IndexOf(flawIds, id) >= 0;

        /// <summary>How much of the flaw is still felt: 0 without the flaw, otherwise 1 minus the compensation of the worn organs (never below 0.3).</summary>
        public float FlawStrength(string id)
        {
            if (!HasFlaw(id)) return 0f;
            var flaw = TraitCatalog.Flaw(id);
            if (flaw == null || organs == null) return 1f;
            organs.GetWorn(worn);
            return FlawCompensation.Strength(FlawCompensation.Fraction(worn, flaw.CompensatedBy));
        }

        public void Set(string newCoreId, IReadOnlyList<string> newFlawIds)
        {
            coreId = string.IsNullOrEmpty(newCoreId) ? null : newCoreId;
            familiarity.Clear();
            flawIds = newFlawIds != null ? new List<string>(newFlawIds).ToArray() : NoFlaws;
            Changed?.Invoke();
        }
    }
}
