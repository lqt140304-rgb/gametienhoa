using Evolve.Swallow;
using UnityEngine;

namespace Evolve.Abilities
{
    /// <summary>The speed boost of a running Sprint (SprintAbility). SlimeController multiplies its speed by Multiplier.</summary>
    public class SprintBoost : MonoBehaviour
    {
        readonly SprintTimer timer = new SprintTimer();

        public float Multiplier => timer.Multiplier(Time.time);

        public void Apply(float multiplier, float seconds) => timer.Apply(Time.time, multiplier, seconds);
    }
}
