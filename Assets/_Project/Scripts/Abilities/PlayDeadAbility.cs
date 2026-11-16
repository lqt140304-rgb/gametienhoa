using UnityEngine;

namespace Evolve.Abilities
{
    /// <summary>Giả Xác (C): go limp; predators lose track. No energy regen meanwhile.</summary>
    [CreateAssetMenu(menuName = "Evolve/Abilities/Play Dead", fileName = "PlayDeadSO")]
    public class PlayDeadAbility : AbilitySO
    {
        public float Duration = 5f;

        void Reset() { DisplayName = "Giả Xác"; EnergyCost = 10f; Cooldown = 12f; }

        public override bool Activate(GameObject user)
        {
            if (!user.TryGetComponent(out PlayingDead pd)) pd = user.AddComponent<PlayingDead>();
            pd.Begin(Duration);
            return true;
        }
    }
}
