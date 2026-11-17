using System;

namespace Evolve.Survival
{
    /// <summary>
    /// Pure data + rules for survival stats. No MonoBehaviour, so it can later be
    /// owned by a server (Netcode) and mirrored to clients without changes.
    /// </summary>
    [Serializable]
    public class SurvivalState
    {
        public float MaxHealth = 100f;
        public float MaxHunger = 100f;
        public float MaxEnergy = 100f;

        public float Health;
        public float Hunger;   // 100 = full, 0 = starving
        public float Energy;

        public bool IsDead => Health <= 0f;

        public event Action Changed;
        public event Action Died;

        public void Reset()
        {
            Health = MaxHealth;
            Hunger = MaxHunger;
            Energy = MaxEnergy;
            Changed?.Invoke();
        }

        /// <summary>Advance stats by dt seconds.</summary>
        public void Tick(float dt, SurvivalConfig cfg, bool isMoving, bool isSprinting)
        {
            if (IsDead) return;

            // Metabolism only burns food while active; resting costs nothing.
            float burn = (isMoving ? cfg.HungerDecayPerSec : 0f) + (isSprinting ? cfg.SprintHungerPerSec : 0f);
            Hunger = Clamp(Hunger - burn * dt, MaxHunger);

            if (Hunger > 0f && !isMoving)
                Energy = Clamp(Energy + cfg.EnergyRegenPerSec * dt, MaxEnergy);

            if (Hunger <= 0f)
                Damage(cfg.StarveDamagePerSec * dt);
            else if (Hunger >= MaxHunger * cfg.HealHungerThreshold)
                Health = Clamp(Health + cfg.HealPerSec * dt, MaxHealth);

            Changed?.Invoke();
        }

        public bool TrySpendEnergy(float amount)
        {
            if (Energy < amount) return false;
            Energy -= amount;
            Changed?.Invoke();
            return true;
        }

        /// <summary>Spend stored food (e.g. on growing organs).</summary>
        public void ConsumeFood(float amount)
        {
            if (IsDead || amount <= 0f) return;
            Hunger = Clamp(Hunger - amount, MaxHunger);
            Changed?.Invoke();
        }

        public void Feed(float nutrition)
        {
            if (IsDead) return;
            Hunger = Clamp(Hunger + nutrition, MaxHunger);
            Changed?.Invoke();
        }

        public void Damage(float amount)
        {
            if (IsDead) return;
            Health = Clamp(Health - amount, MaxHealth);
            Changed?.Invoke();
            if (IsDead) Died?.Invoke();
        }

        static float Clamp(float v, float max) => v < 0f ? 0f : (v > max ? max : v);
    }
}
