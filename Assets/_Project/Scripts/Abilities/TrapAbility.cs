using Evolve.Survival;
using UnityEngine;

namespace Evolve.Abilities
{
    /// <summary>Shared shape "place a thing" (Cắm Gai; later Tiết Nhớt, Ảo Ảnh): drops a spike trap on the ground at the slime's feet.</summary>
    [CreateAssetMenu(menuName = "Evolve/Abilities/Trap", fileName = "TrapSO")]
    public class TrapAbility : AbilitySO
    {
        public float Radius = 1.2f;
        public float Damage = 12f;
        public float Seconds = 20f;

        public override bool Activate(GameObject user) => Activate(user, 2);

        public override bool Activate(GameObject user, int level)
        {
            user.TryGetComponent(out SurvivalStats owner);
            Vector3 p = user.transform.position;
            p.y = Evolve.World.GroundHeight.At(p.x, p.z, p.y - 0.4f);
            SpikeTrap.Spawn(p, Radius, Damage * Power(level), Seconds, owner);
            return true;
        }
    }
}
