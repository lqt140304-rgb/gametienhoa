using System.Collections.Generic;

namespace Evolve.Traits
{
    public enum BirthStepKind { Boot, Line, Gift, DealCore, PickCore, RevealCore, DealFlaw, PickFlaw, RevealFlaw, Summary }

    /// <summary>One beat of the birth scene: what is on screen, what Tiếng Vọng says and how long it stays at least.</summary>
    public sealed class BirthStep
    {
        public BirthStepKind Kind;
        /// <summary>Shown in the system window.</summary>
        public string Text = "";
        /// <summary>Name of the voice clip (and the line on the voice list); empty = silent. Reveal steps get theirs from the result.</summary>
        public string VoiceId = "";
        /// <summary>What is read aloud when it differs from the shown text.</summary>
        public string VoiceText = "";
        /// <summary>The shortest time the step stays, so the text can be read (0 for a step that waits for the player).</summary>
        public float MinSeconds;
        public int GiftIndex = -1;
        /// <summary>True while the scene waits for the player to pick a card.</summary>
        public bool Interactive => Kind == BirthStepKind.PickCore || Kind == BirthStepKind.PickFlaw;
    }

    /// <summary>
    /// The birth scene as a list of steps (spec 2026-11-19-birth-scene-design 8). A timed step ends when its minimum time is over and its voice has
    /// been quiet for a moment (so Tiếng Vọng is always heard to the end); a pick step waits for the player. Pure, so it can be tested.
    /// </summary>
    public sealed class BirthFlow
    {
        /// <summary>A step ends this long after its voice stops.</summary>
        public const float GapAfterVoice = 0.35f;
        /// <summary>Space or Enter can cut a line only after it has been up this long.</summary>
        public const float SkipAfter = 1.5f;

        readonly IReadOnlyList<BirthStep> steps;
        int index;
        float stepTime, quiet;

        public BirthFlow(IReadOnlyList<BirthStep> steps) { this.steps = steps ?? new List<BirthStep>(); }

        public int Index => index;
        public int Count => steps.Count;
        public float StepTime => stepTime;
        public bool Finished => index >= steps.Count;
        public BirthStep Current => Finished ? null : steps[index];
        public BirthStep Step(int i) => i >= 0 && i < steps.Count ? steps[i] : null;

        public bool CanSkip => !Finished && !Current.Interactive && stepTime >= SkipAfter;

        public void Tick(float dt, bool voiceBusy)
        {
            if (Finished || dt <= 0f) return;
            stepTime += dt;
            if (voiceBusy) quiet = 0f; else quiet += dt;
            if (Current.Interactive) return;
            if (stepTime >= Current.MinSeconds && quiet >= GapAfterVoice) Advance();
        }

        /// <summary>Space or Enter: cut the current line (and its voice) once it has been up for SkipAfter seconds.</summary>
        public void Skip() { if (CanSkip) Advance(); }

        /// <summary>Tab: jump to the first card deal. Only before it, and not in the first moments.</summary>
        public void SkipIntro()
        {
            int deal = IndexOf(BirthStepKind.DealCore);
            if (Finished || deal < 0 || index >= deal || stepTime < SkipAfter) return;
            index = deal; stepTime = 0f; quiet = 0f;
        }

        /// <summary>The player picked a card. False when the scene is not waiting for one.</summary>
        public bool Pick()
        {
            if (Finished || !Current.Interactive) return false;
            Advance();
            return true;
        }

        public int IndexOf(BirthStepKind kind)
        {
            for (int i = 0; i < steps.Count; i++) if (steps[i].Kind == kind) return i;
            return -1;
        }

        void Advance() { index++; stepTime = 0f; quiet = 0f; }
    }
}
