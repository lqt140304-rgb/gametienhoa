namespace Evolve.Combat
{
    /// <summary>Where the golem's axe is: stuck at the room's anchor (at the start), stuck on the ground where it was thrown, in the golem's hands, or flying.</summary>
    public enum AxePlace { Anchor, Ground, Held, Flying }

    /// <summary>Where a thrown axe came to rest: at its point of impact, or at the golem's feet when that spot cannot be reached (a wall, a pit), so the fight never stalls.</summary>
    public enum AxeLanding { AtImpact, AtGolem }

    /// <summary>
    /// The golem's axe and when it goes to get it. It starts stuck in the heart stone; at <see cref="FetchBelow"/> health it goes to pull it out (an axe form that, once reached, is never left for good:
    /// even if it mends above half it still fetches the axe back after a throw); after a throw it fights bare-handed for <see cref="BareHandedSeconds"/> and then fetches it again.
    /// It waits while it is busy or about to roar for a new phase.
    /// </summary>
    public sealed class AxeFsm
    {
        public const float FetchBelow = 0.5f, BareHandedSeconds = 1.5f;

        float bareHanded;

        public AxePlace Place { get; private set; } = AxePlace.Anchor;
        /// <summary>It has held the axe at least once.</summary>
        public bool EverArmed { get; private set; }
        public MoveForm Form => Place == AxePlace.Held ? MoveForm.Axe : MoveForm.Fist;

        /// <summary>Time passes; the bare-handed time counts while the axe is not in its hands.</summary>
        public void Tick(float dt) { if (Place != AxePlace.Held) bareHanded += dt > 0f ? dt : 0f; }

        public bool ShouldFetch(float health01, bool golemIdle, bool phaseRoarPending)
        {
            if (!golemIdle || phaseRoarPending) return false;
            if (Place == AxePlace.Anchor) return health01 <= FetchBelow + 1e-4f;
            return Place == AxePlace.Ground && bareHanded >= BareHandedSeconds;
        }

        public void Pulled() { if (Place == AxePlace.Anchor || Place == AxePlace.Ground) { Place = AxePlace.Held; EverArmed = true; bareHanded = 0f; } }

        public void Thrown() { if (Place == AxePlace.Held) { Place = AxePlace.Flying; bareHanded = 0f; } }

        /// <summary>The thrown axe came down. When <paramref name="reachable"/> is false the axe drops at the golem's feet instead.</summary>
        public AxeLanding Landed(bool reachable)
        {
            if (Place != AxePlace.Flying) return AxeLanding.AtImpact;
            Place = AxePlace.Ground;
            return reachable ? AxeLanding.AtImpact : AxeLanding.AtGolem;
        }
    }
}
