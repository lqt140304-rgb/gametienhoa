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

        /// <summary>
        /// True for the ghost of another (remote) player. Its numbers come from the network, so it does not tick;
        /// creatures can still target it, and rewards and damage for it are sent to its owner (see NetWorld).
        /// </summary>
        public bool IsRemote { get; set; }
        public ulong RemoteClientId { get; set; }

        void Awake()
        {
            if (config == null) config = ScriptableObject.CreateInstance<SurvivalConfig>();
            state.Config = config;
            state.Ambient = config.DefaultAmbient;
            state.Reset();
            state.ResetLives();
            state.Died += () => Debug.Log(state.IsCorpse ? $"{name} died (out of lives)" : $"{name} died ({state.Lives} lives left)");
        }

        void OnEnable() => all.Add(this);
        void OnDisable() => all.Remove(this);

        void Update()
        {
            if (IsRemote) return;
            state.Tick(Time.deltaTime, config, IsMoving, IsSprinting);
        }
    }
}
