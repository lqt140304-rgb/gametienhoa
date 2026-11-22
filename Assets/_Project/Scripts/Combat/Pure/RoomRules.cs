using System;

namespace Evolve.Combat
{
    public enum DoorState { Open, Closing, Closed, Opening }

    /// <summary>The boss room: it wakes the boss only when every living, connected player is inside (alone, just you), and a steel door shuts the way in. Pure.</summary>
    public static class RoomRules
    {
        public static bool ShouldActivate(int insideAlive, int connectedAlive) => connectedAlive >= 1 && insideAlive >= connectedAlive;
    }

    /// <summary>The steel door of a boss room: open at the start, shuts in <see cref="CloseSeconds"/> when the room wakes, opens again (at the same speed, from wherever it is) when the boss dies or every player has.</summary>
    public sealed class DoorFsm
    {
        public const float CloseSeconds = 1.5f;

        public DoorState State { get; private set; } = DoorState.Open;
        /// <summary>1 wide open, 0 shut tight.</summary>
        public float Openness { get; private set; } = 1f;

        public void Close() { if (State == DoorState.Open || State == DoorState.Opening) State = DoorState.Closing; }
        public void Open() { if (State == DoorState.Closed || State == DoorState.Closing) State = DoorState.Opening; }

        public void Tick(float dt)
        {
            float step = Math.Max(0f, dt) / CloseSeconds;
            if (State == DoorState.Closing) { Openness = Math.Max(0f, Openness - step); if (Openness <= 0f) State = DoorState.Closed; }
            else if (State == DoorState.Opening) { Openness = Math.Min(1f, Openness + step); if (Openness >= 1f) State = DoorState.Open; }
        }
    }
}
