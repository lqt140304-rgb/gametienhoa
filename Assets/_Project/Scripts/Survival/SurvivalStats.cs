using UnityEngine;

namespace Evolve.Survival
{
    /// <summary>Unity wrapper that ticks a SurvivalState.</summary>
    public class SurvivalStats : MonoBehaviour
    {
        [SerializeField] SurvivalConfig config;
        [SerializeField] SurvivalState state = new SurvivalState();

        public SurvivalState State => state;
        public SurvivalConfig Config => config;
        public bool IsMoving { get; set; }
        /// <summary>Set by effects such as Giả Xác to stop energy regen.</summary>
        public bool BlockEnergyRegen { get; set; }

        void Awake()
        {
            if (config == null) config = ScriptableObject.CreateInstance<SurvivalConfig>();
            state.Reset();
            state.Died += () => Debug.Log($"{name} died");
        }

        void Update() => state.Tick(Time.deltaTime, config, IsMoving || BlockEnergyRegen);
    }
}
