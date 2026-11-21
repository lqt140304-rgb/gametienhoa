using Evolve.Survival;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Evolve.Abilities
{
    /// <summary>
    /// Holds the player's one random active ability. Press Q (or gamepad West) to use.
    /// If none is assigned, one is picked at random from the pool on start.
    /// </summary>
    [RequireComponent(typeof(SurvivalStats))]
    public class AbilityCaster : MonoBehaviour
    {
        [SerializeField] AbilitySO ability;
        [SerializeField] AbilitySO[] randomPool;

        SurvivalStats stats;
        float readyAt;

        public AbilitySO Ability => ability;

        /// <summary>Debug: make the ability ready immediately.</summary>
        public void ResetCooldown() => readyAt = 0f;
        /// <summary>Seconds until the ability can be used again (0 = ready).</summary>
        public float CooldownRemaining => Mathf.Max(0f, readyAt - Time.time);
        /// <summary>0 = just used, 1 = ready.</summary>
        public float CooldownProgress => ability == null || ability.Cooldown <= 0f ? 1f : 1f - CooldownRemaining / ability.Cooldown;

        void Awake()
        {
            stats = GetComponent<SurvivalStats>();
            if (ability == null && randomPool != null && randomPool.Length > 0)
                ability = randomPool[Random.Range(0, randomPool.Length)];
            if (ability != null) Debug.Log($"Dị Năng: {ability.DisplayName} ({ability.Tier})");
        }

        void Update()
        {
            if (pending || SurvivalHUD.BlocksInput(gameObject) || ability == null || !Pressed() || Time.time < readyAt || stats.State.IsDead) return;
            if (stats.State.Energy < ability.EnergyCost) return;

            // The body winds up first; the ability leaves at the moment the animation releases.
            var motion = ability is BurstDashAbility || ability is ChargeAbility
                ? Evolve.Visuals.GelMotion.Dash : Evolve.Visuals.GelMotion.Release;
            Evolve.Visuals.SlimeGelAnimator.Play(gameObject, motion);
            StartCoroutine(FireAfter(Evolve.Visuals.SlimeGelAnimator.WindUp(motion)));
        }

        bool pending;

        System.Collections.IEnumerator FireAfter(float delay)
        {
            pending = true;
            var spike = ability as SpikeBurstAbility;
            var volley = spike != null ? spike.Prepare(gameObject, 2, delay) : null;
            if (delay > 0f) yield return new WaitForSeconds(delay);
            pending = false;
            if (stats.State.IsDead || stats.State.Energy < ability.EnergyCost) { volley?.Cancel(); yield break; }
            if (spike != null ? spike.Fire(gameObject, 2, volley) : ability.Activate(gameObject))
            {
                stats.State.TrySpendEnergy(ability.EnergyCost);
                readyAt = Time.time + ability.Cooldown;
            }
        }

        static bool Pressed()
        {
#if ENABLE_INPUT_SYSTEM
            return (Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame)
                || (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.Q);
#endif
        }
    }
}
