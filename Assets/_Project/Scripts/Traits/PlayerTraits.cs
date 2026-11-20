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

        readonly ScentTrail trail = new ScentTrail();
        float stillSeconds, lastHitAt = -1e6f, drySeconds, leftWaterAt = -1e6f, nextCrowdCheck;
        bool wasWading;
        int crowd;
        Vector3 lastPos;
        bool hasLastPos;

        public string CoreId => coreId;
        /// <summary>How long the body has not moved (Nhân Ngụy Sắc); reset by a hit.</summary>
        public float StillSeconds => stillSeconds;
        /// <summary>The scent trail (Vệt Mùi); only laid while the player has that flaw.</summary>
        public ScentTrail Trail => trail;
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
            stillSeconds = 0f;           // Ngụy Sắc is lost when hit
            lastHitAt = Time.time;       // Màng Thẩm Thấu pauses
            if (slime != null && HasFlaw(FlawRules.RunRay)) slime.FreezeFor(FlawRules.HitFreezeSeconds(FlawStrength(FlawRules.RunRay)));
        }

        void Update()
        {
            float dt = Time.deltaTime;
            var pos = transform.position;
            float speed = hasLastPos && dt > 0f ? new Vector2(pos.x - lastPos.x, pos.z - lastPos.z).magnitude / dt : 0f;
            lastPos = pos; hasLastPos = true;

            // tracked on every avatar (the host's predators read them for remote players too)
            bool moving = stats != null ? stats.IsMoving : speed > EnvTraitRules.ThamThauStillSpeed;
            stillSeconds = moving ? 0f : stillSeconds + dt;
            if (HasFlaw(EnvTraitRules.VetMui)) trail.Visit(pos.x, pos.z, Time.time); else if (trail.Count > 0) trail.Clear();

            if (stats == null || slime == null) return;   // the rest changes the local body; a remote avatar's numbers come from its owner
            var state = stats.State;
            state.HungerBurnMultiplier = FlawRules.HungerBurnMultiplier(FlawStrength(FlawRules.DoiSom));
            state.AbsorbFlawStrength = FlawStrength(FlawRules.DaDayKen);

            bool wading = slime.IsWading;
            bool wetGround = wading || state.InShelter;                                   // shallow water, or the damp moss of a shelter
            bool wet = wetGround || slime.IsSwimming || state.InCleanWater;

            // Nhân Màng Thẩm Thấu
            state.HungerGainPerSec = EnvTraitRules.ThamThauGain(HasCore(EnvTraitRules.MangThamThau), speed, wetGround, slime.IsSwimming,
                state.Hunger, state.MaxHunger, Time.time - lastHitAt);
            // Nhân Quang Hợp (floating on the surface is fine; the slime never dives)
            state.EnergyBonusPerSec = HasCore(EnvTraitRules.QuangHop) && LightShafts.InLight(pos)
                ? EnvTraitRules.QuangHopEnergy(true, true, false) : 0f;
            // Ngại Đông: count the creatures close by twice a second
            if (HasFlaw(EnvTraitRules.NgaiDong))
            {
                if (Time.time >= nextCrowdCheck) { nextCrowdCheck = Time.time + 0.5f; crowd = CountCreatures(pos, EnvTraitRules.CrowdRadius); }
                state.EnergyRegenMultiplier = EnvTraitRules.CrowdRegen(crowd, FlawStrength(EnvTraitRules.NgaiDong));
            }
            else state.EnergyRegenMultiplier = 1f;

            // speed: Nhân Quen Đất x Mau Khô x Bám Bùn
            drySeconds = wet ? 0f : drySeconds + dt;
            if (wasWading && !wading) leftWaterAt = Time.time;
            wasWading = wading;
            float mul = EnvTraitRules.DrySpeed(drySeconds, FlawStrength(EnvTraitRules.MauKho))
                      * EnvTraitRules.MudSpeed(wading ? -1f : Time.time - leftWaterAt, FlawStrength(EnvTraitRules.BamBun));
            if (HasCore("core.quen_dat") && !slime.IsSwimming)
            {
                var p = new Vector2(pos.x, pos.z);
                familiarity.Visit(p, Time.time);
                mul *= familiarity.SpeedMultiplier(p, Time.time);
            }
            slime.TraitSpeed = mul;
        }

        static int CountCreatures(Vector3 at, float radius)
        {
            float r2 = radius * radius;
            int n = 0;
            foreach (var c in Evolve.Creatures.Prey.Active) if (c != null && (c.transform.position - at).sqrMagnitude <= r2) n++;
            foreach (var c in Evolve.Creatures.Predator.Active) if (c != null && (c.transform.position - at).sqrMagnitude <= r2) n++;
            return n;
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
            trail.Clear();
            flawIds = newFlawIds != null ? new List<string>(newFlawIds).ToArray() : NoFlaws;
            Changed?.Invoke();
        }
    }
}
