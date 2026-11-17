namespace Evolve.World
{
    /// <summary>Size of one zone and how long it takes to cross it (500 m: about 8 minutes across at crawl speed).</summary>
    public static class WorldScale
    {
        /// <summary>Side of one zone in metres.</summary>
        public const float ZoneSize = 500f;
        /// <summary>Half side of the open middle (the lake and its shores).</summary>
        public const float CoreHalf = 60f;
        /// <summary>Slime crawl speed in m/s (SlimeController.crawlSpeed default).</summary>
        public const float CrawlSpeed = 1f;

        public static float CrossingMinutes(float distance, float speed) => distance / speed / 60f;
    }
}
