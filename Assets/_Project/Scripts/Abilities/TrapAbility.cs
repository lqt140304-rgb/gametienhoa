using Evolve.Survival;
using UnityEngine;

namespace Evolve.Abilities
{
    /// <summary>Shared shape "place a thing" (Cắm Gai; later Tiết Nhớt, Ảo Ảnh): drops a spike trap on the ground at the slime's feet.</summary>
    [CreateAssetMenu(menuName = "Evolve/Abilities/Trap", fileName = "TrapSO")]
    public class TrapAbility : CaveAbility
    {
        public float Radius = 1.2f;
        public float Damage = 12f;
        public float Seconds = 20f;
        [Header("★3")]
        [Tooltip("Stun on whoever steps in")]
        public float Star3Stun;

        public override bool Activate(GameObject user, int stars)
        {
            user.TryGetComponent(out SurvivalStats owner);
            Vector3 p = user.transform.position;
            p.y = Evolve.World.GroundHeight.At(p.x, p.z, p.y - 0.4f);
            SpikeTrap.Spawn(p, Radius, Damage * StarPower(stars), Seconds, stars >= 3 ? Star3Stun : 0f, owner, Evolve.Swallow.SkillFxMath.FamilyColor(Family));
            return true;
        }
    }
}
