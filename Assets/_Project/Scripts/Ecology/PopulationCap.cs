namespace Evolve.Ecology
{
    /// <summary>Global count of embodied (physically spawned) creatures across all regions.</summary>
    public static class PopulationCap
    {
        public static int Embodied;
        public static int Max = 30;

        // Domain reload may be off: clear leaked state when entering Play mode.
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Embodied = 0;

        public static bool TryReserve()
        {
            if (Embodied >= Max) return false;
            Embodied++;
            return true;
        }

        public static void Release()
        {
            if (Embodied > 0) Embodied--;
        }
    }
}
