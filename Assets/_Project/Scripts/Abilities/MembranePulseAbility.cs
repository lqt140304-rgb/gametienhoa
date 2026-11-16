using Evolve.World;
using UnityEngine;

namespace Evolve.Abilities
{
    /// <summary>Rung Màng (C): one strong vibration pulse that reveals everything far around.</summary>
    [CreateAssetMenu(menuName = "Evolve/Abilities/Membrane Pulse", fileName = "MembranePulseSO")]
    public class MembranePulseAbility : AbilitySO
    {
        [Tooltip("Much wider than the passive VibrationSense radius")]
        public float Radius = 60f;

        void Reset() { DisplayName = "Rung Màng"; EnergyCost = 25f; Cooldown = 10f; }

        public override bool Activate(GameObject user)
        {
            if (!user.TryGetComponent(out VibrationSense sense)) return false;
            sense.Pulse(Radius);
            return true;
        }
    }
}
