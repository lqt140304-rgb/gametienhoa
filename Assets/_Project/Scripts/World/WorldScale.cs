namespace Evolve.World
{
    /// <summary>Size of one zone and how long it takes to cross it (spec 6.9: about 8 minutes at crawl speed).</summary>
    public static class WorldScale
    {
        /// <summary>Side of one zone in metres.</summary>
        public const float ZoneSize = 480f;
        /// <summary>Half side of the flat core (130 x 130 m) around the spawn pool.</summary>
        public const float CoreHalf = 65f;
        /// <summary>Slime crawl speed in m/s (SlimeController.crawlSpeed default).</summary>
        public const float CrawlSpeed = 1f;

        public static float CrossingMinutes(float distance, float speed) => distance / speed / 60f;
    }
}
