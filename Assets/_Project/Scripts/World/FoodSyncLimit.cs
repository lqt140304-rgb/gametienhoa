namespace Evolve.World
{
    /// <summary>How many food blobs the co-op sync carries (one byte each in a 4096-byte message). Shared by WorldSync and the scene builders.</summary>
    public static class FoodSyncLimit
    {
        public const int MaxBlobs = 1000;
    }
}
