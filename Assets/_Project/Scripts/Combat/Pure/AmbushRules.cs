namespace Evolve.Combat
{
    public enum AmbushState { Lurking, Revealed, Roaming }

    /// <summary>
    /// A creature that lies in wait (the weasel): hidden until a player comes within <see cref="RevealRange"/>, then it bursts out once;
    /// after its strike it roams, and goes back to lurking after <see cref="LurkAfter"/> seconds with no player within <see cref="RoamNearRange"/>.
    /// </summary>
    public sealed class AmbushRules
    {
        public const float RevealRange = 5f, LurkAfter = 20f, RoamNearRange = 15f;
        const float Eps = 1e-4f;
        float calm;

        public AmbushState State { get; private set; } = AmbushState.Lurking;

        /// <summary>Returns true exactly once, when the creature has just been revealed. <paramref name="playerDistance"/> is MaxValue with no valid target.</summary>
        public bool Update(float dt, float playerDistance)
        {
            switch (State)
            {
                case AmbushState.Lurking:
                    if (playerDistance <= RevealRange) { State = AmbushState.Revealed; return true; }
                    return false;
                case AmbushState.Roaming:
                    if (playerDistance <= RoamNearRange) calm = 0f; else calm += dt;
                    if (calm + Eps >= LurkAfter) { State = AmbushState.Lurking; calm = 0f; }
                    return false;
                default:
                    return false;
            }
        }

        public void StrikeDone() { if (State == AmbushState.Revealed) { State = AmbushState.Roaming; calm = 0f; } }
        public void Reset() { State = AmbushState.Lurking; calm = 0f; }
    }
}
