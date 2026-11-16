using Evolve.Player;
using Evolve.Survival;
using UnityEngine;

namespace Evolve.Abilities
{
    /// <summary>
    /// Runtime effect of Giả Xác: locks input, blocks energy regen, and marks the
    /// slime hidden. Future predator AI should skip targets where IsHidden is true.
    /// </summary>
    public class PlayingDead : MonoBehaviour
    {
        float endsAt = -1f;
        SlimeController controller;
        SurvivalStats stats;

        public bool Active => Time.time < endsAt;

        public static bool IsHidden(GameObject target) =>
            target.TryGetComponent(out PlayingDead pd) && pd.Active;

        void Awake()
        {
            controller = GetComponent<SlimeController>();
            stats = GetComponent<SurvivalStats>();
        }

        public void Begin(float duration)
        {
            endsAt = Time.time + duration;
            Set(true);
        }

        void Update()
        {
            if (endsAt > 0f && !Active) { endsAt = -1f; Set(false); }
        }

        void Set(bool on)
        {
            if (controller) controller.InputLocked = on;
            if (stats) stats.BlockEnergyRegen = on;
        }

        void OnDisable() => Set(false);
    }
}
