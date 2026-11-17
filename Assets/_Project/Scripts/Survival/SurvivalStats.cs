using System.Collections.Generic;
using UnityEngine;

namespace Evolve.Survival
{
    /// <summary>Unity wrapper that ticks a SurvivalState.</summary>
    public class SurvivalStats : MonoBehaviour
    {
        static readonly List<SurvivalStats> all = new List<SurvivalStats>();
        /// <summary>Every living player/creature with survival stats (for AI targeting).</summary>
        public static IReadOnlyList<SurvivalStats> All => all;

        [SerializeField] SurvivalConfig config;
        [SerializeField] SurvivalState state = new SurvivalState();

        public SurvivalState State => state;
        public SurvivalConfig Config => config;
        public bool IsMoving { get; set; }
        public bool IsSprinting { get; set; }

        void Awake()
        {
            if (config == null) config = ScriptableObject.CreateInstance<SurvivalConfig>();
            state.Ambient = config.DefaultAmbient;
            state.Reset();
            state.Died += () => Debug.Log($"{name} died");
        }

        void OnEnable() => all.Add(this);
        void OnDisable() => all.Remove(this);

        void Update() => state.Tick(Time.deltaTime, config, IsMoving, IsSprinting);
    }
}
