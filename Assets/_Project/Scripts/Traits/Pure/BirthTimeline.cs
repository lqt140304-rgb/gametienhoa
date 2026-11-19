using UnityEngine;

namespace Evolve.Traits
{
    public enum BirthPhase { Drop, Echo, DealCore, DealFlaw, Summary, Done }

    /// <summary>
    /// The timing of the birth scene (spec 2026-11-19-birth-scene-design 5.1): about 22 seconds in five phases.
    /// Pure functions of the elapsed time, so the scene and its skipping can be tested without the engine.
    /// </summary>
    public static class BirthTimeline
    {
        public const float DropSeconds = 2f, EchoSeconds = 5f, DealCoreSeconds = 7f, DealFlawSeconds = 5f, SummarySeconds = 3f;
        public const float Total = DropSeconds + EchoSeconds + DealCoreSeconds + DealFlawSeconds + SummarySeconds;
        /// <summary>Skipping is possible only after this many seconds.</summary>
        public const float SkipAfter = 3f;

        public static float PhaseStart(BirthPhase p) => p switch
        {
            BirthPhase.Drop => 0f,
            BirthPhase.Echo => DropSeconds,
            BirthPhase.DealCore => DropSeconds + EchoSeconds,
            BirthPhase.DealFlaw => DropSeconds + EchoSeconds + DealCoreSeconds,
            BirthPhase.Summary => DropSeconds + EchoSeconds + DealCoreSeconds + DealFlawSeconds,
            _ => Total
        };

        public static float PhaseLength(BirthPhase p) => p switch
        {
            BirthPhase.Drop => DropSeconds,
            BirthPhase.Echo => EchoSeconds,
            BirthPhase.DealCore => DealCoreSeconds,
            BirthPhase.DealFlaw => DealFlawSeconds,
            BirthPhase.Summary => SummarySeconds,
            _ => 0f
        };

        public static BirthPhase PhaseAt(float t)
        {
            if (t >= Total) return BirthPhase.Done;
            if (t >= PhaseStart(BirthPhase.Summary)) return BirthPhase.Summary;
            if (t >= PhaseStart(BirthPhase.DealFlaw)) return BirthPhase.DealFlaw;
            if (t >= PhaseStart(BirthPhase.DealCore)) return BirthPhase.DealCore;
            if (t >= PhaseStart(BirthPhase.Echo)) return BirthPhase.Echo;
            return BirthPhase.Drop;
        }

        /// <summary>0..1 through the current phase (1 once the scene is done).</summary>
        public static float PhaseFraction(float t)
        {
            var p = PhaseAt(t);
            if (p == BirthPhase.Done) return 1f;
            return Mathf.Clamp01((t - PhaseStart(p)) / PhaseLength(p));
        }

        /// <summary>Space or Enter: after SkipAfter seconds jump to the summary (the result is unchanged); before that, or already in the summary, nothing.</summary>
        public static float Skip(float t)
        {
            if (t < SkipAfter) return t;
            return PhaseAt(t) >= BirthPhase.Summary ? t : PhaseStart(BirthPhase.Summary);
        }

        /// <summary>
        /// How far card <paramref name="index"/> of <paramref name="count"/> has been dealt (0..1) at <paramref name="phaseFraction"/> of a dealing phase:
        /// the cards go out one after another during the first 40% of the phase.
        /// </summary>
        public static float DealProgress(float phaseFraction, int index, int count)
        {
            if (count <= 0) return 0f;
            float start = 0.3f * index / count;
            return Mathf.Clamp01((phaseFraction - start) / 0.1f);
        }

        /// <summary>The fate card turns face up between 65% and 90% of a dealing phase (0 = face down, 1 = face up).</summary>
        public static float FlipProgress(float phaseFraction) => Mathf.Clamp01((phaseFraction - 0.65f) / 0.25f);

        /// <summary>The scene runs for a player who has no core yet and has not seen it this run (death and respawn never run it again).</summary>
        public static bool ShouldRun(bool hasCore, bool ranThisRun) => !hasCore && !ranThisRun;
    }
}
