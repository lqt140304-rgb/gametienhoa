using System.Collections.Generic;
using UnityEngine;

namespace Evolve.Civilization
{
    /// <summary>How a tribe treats one player (spec 2026-11-17-treefolk-grudge-design).</summary>
    public enum Standing { Hostile, Cold, Neutral, Friendly, Member }

    /// <summary>The numbers of the grudge system, in one place.</summary>
    public static class ReputationRules
    {
        public const float Min = -100f, Max = 100f;
        public const float HitPenalty = -15f;
        public const float KillPenalty = -60f;
        /// <summary>Share of the penalty that companions standing near the attacker get.</summary>
        public const float CompanionShare = 0.25f;
        public const float CompanionRadius = 15f;
        /// <summary>Offerings at the atonement stone never raise a reputation above this.</summary>
        public const float AtonementCap = -10f;
        public const float OfferingValue = 12f;
        /// <summary>Food (hunger points) one offering costs.</summary>
        public const float OfferingCost = 25f;
        public const float WitnessRadius = 20f;
        public const float SpreadRadius = 4f;
        public const float CorpseRadius = 6f;

        public static Standing StandingOf(float value)
        {
            if (value <= -50f) return Standing.Hostile;
            if (value < -10f) return Standing.Cold;
            if (value < 30f) return Standing.Neutral;
            if (value < 70f) return Standing.Friendly;
            return Standing.Member;
        }

        public static string Name(Standing s) => s switch
        {
            Standing.Hostile => "Thù",
            Standing.Cold => "Lạnh nhạt",
            Standing.Neutral => "Bình thường",
            Standing.Friendly => "Thân thiện",
            _ => "Thành viên"
        };
    }

    /// <summary>What one tribe thinks of each player. Keys are any object that stands for a player (on the host: its SurvivalStats).</summary>
    public class TribeLedger
    {
        readonly Dictionary<object, float> values = new Dictionary<object, float>();

        public float Get(object player) => player != null && values.TryGetValue(player, out float v) ? v : 0f;

        public Standing StandingOf(object player) => ReputationRules.StandingOf(Get(player));

        void Add(object player, float delta)
        {
            if (player == null) return;
            values[player] = Mathf.Clamp(Get(player) + delta, ReputationRules.Min, ReputationRules.Max);
        }

        /// <summary>The offender loses <paramref name="penalty"/> (negative); each companion a quarter of it.</summary>
        public void Offend(object offender, float penalty, IEnumerable<object> companions)
        {
            Add(offender, penalty);
            if (companions == null) return;
            foreach (var c in companions)
                if (c != null && !ReferenceEquals(c, offender)) Add(c, penalty * ReputationRules.CompanionShare);
        }

        /// <summary>An offering at the stone: raises a bad reputation, never above the cap. False (and no change) if already at or above it.</summary>
        public bool Offer(object player)
        {
            float v = Get(player);
            if (player == null || v >= ReputationRules.AtonementCap) return false;
            values[player] = Mathf.Min(ReputationRules.AtonementCap, v + ReputationRules.OfferingValue);
            return true;
        }
    }
}
