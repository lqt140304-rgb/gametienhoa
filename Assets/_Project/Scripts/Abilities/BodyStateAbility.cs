using UnityEngine;

namespace Evolve.Abilities
{
    /// <summary>
    /// Shared shape "change my body" (Thu Mình, Khiên Quặng, Phát Sáng, Bám Vách; later Hóa Đá, Ẩn Thân, Thu Nhỏ…): puts timed
    /// states on the slime (BodyState). Power by stars makes it last longer.
    /// </summary>
    [CreateAssetMenu(menuName = "Evolve/Abilities/Body State", fileName = "BodyStateSO")]
    public class BodyStateAbility : AbilitySO
    {
        public float Seconds = 3f;
        [Tooltip("Damage taken while it lasts (1 = normal, 0.5 = half)")]
        public float DamageTaken = 1f;
        [Tooltip("Whole hits a shield eats while it lasts (0 = no shield)")]
        public int BlockHits;
        [Tooltip("Cannot move while it lasts")]
        public bool Root;
        [Tooltip("Light range in metres (0 = no light)")]
        public float Light;
        [Tooltip("Walk up walls while it lasts")]
        public bool Climb;

        public override bool Activate(GameObject user) => Activate(user, 2);

        public override bool Activate(GameObject user, int level)
        {
            var s = BodyState.On(user);
            float t = Seconds * Power(level);
            if (DamageTaken < 1f) s.TakeLessDamage(DamageTaken, t);
            if (BlockHits > 0) s.Shield(BlockHits, t);
            if (Root) s.Root(t);
            if (Light > 0f) s.Glow(Light, t);
            if (Climb) s.Climb(t);
            return true;
        }
    }
}
