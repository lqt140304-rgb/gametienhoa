using System;
using UnityEngine;

namespace Evolve.Survival
{
    /// <summary>A creature (predator or prey) took a hit: the target and the damage. The screen feedback listens to it.</summary>
    public static class CombatEvents
    {
        public static event Action<Transform, float> Damaged;
        public static void Raise(Transform target, float amount) => Damaged?.Invoke(target, amount);
    }
}
