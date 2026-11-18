using Evolve.Organs;

namespace Evolve.Traits
{
    /// <summary>
    /// Every number of the starting-choices system in one place (spec 2026-11-18-starting-choices-design).
    /// All values are provisional and meant to be tuned in play.
    /// </summary>
    public static class TraitTuning
    {
        /// <summary>Core cards each player is dealt.</summary>
        public const int HandSize = 3;

        /// <summary>Share of a flaw one matching organ at full integrity compensates.</summary>
        public const float PerOrganCompensation = 0.35f;
        /// <summary>No flaw is ever compensated beyond this share (30% always stays).</summary>
        public const float MaxCompensation = 0.7f;

        // Nhân Quen Đất
        public const float QuenDatCell = 4f;
        public const float QuenDatMaxBonus = 0.15f;
        public const float QuenDatGain = 0.2f;                    // familiarity added each time a cell is entered
        public const float QuenDatDecayPerSecond = 1f / 180f;     // a fully familiar cell is forgotten after three minutes away

        // Nhân Con Bạc
        public const float BossLuckFactor = 0.5f;
        /// <summary>Luck never makes a drop certain.</summary>
        public const float DropChanceCap = 0.95f;

        // Khuyết Điểm
        public const float RunRayFreeze = 0.7f;          // Run Rẩy: seconds frozen after a hit
        public const float HungerBurnExtra = 0.10f;      // Đói Sớm: hunger burns this much faster
        public const float LuongCuongHealth = 0.25f;     // Luống Cuống: below this health fraction, turning is slower
        public const float LuongCuongMinTurn = 0.5f;
        public const float DaDayKenAbsorb = 0.70f;       // Dạ Dày Kén: share absorbed from a first-time food
        public const float DaDayKenWindow = 20f;         // ...during this many seconds

        /// <summary>Relative increase of the drop chance by organ tier (Nhân Con Bạc): D +20% ... S +4%.</summary>
        public static float LuckBonus(OrganTier tier) => tier switch
        {
            OrganTier.D => 0.20f,
            OrganTier.C => 0.16f,
            OrganTier.B => 0.12f,
            OrganTier.A => 0.08f,
            OrganTier.S => 0.04f,
            _ => 0f
        };
    }
}
