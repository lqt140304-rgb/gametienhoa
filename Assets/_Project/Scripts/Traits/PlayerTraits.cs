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
        readonly List<(OrganSlot slot, float integrity)> worn = new List<(OrganSlot, float)>();

        public string CoreId => coreId;
        public IReadOnlyList<string> FlawIds => flawIds;
        public event Action Changed;

        void Awake() => organs = GetComponent<OrganHost>();

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
            flawIds = newFlawIds != null ? new List<string>(newFlawIds).ToArray() : NoFlaws;
            Changed?.Invoke();
        }
    }
}
