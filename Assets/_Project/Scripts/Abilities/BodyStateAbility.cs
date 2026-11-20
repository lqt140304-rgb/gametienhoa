using Evolve.Creatures;
using Evolve.Survival;
using UnityEngine;

namespace Evolve.Abilities
{
    /// <summary>
    /// Shared shape "change my body" (Thu Mình, Khiên Quặng, Phát Sáng, Bám Vách; later Hóa Đá, Ẩn Thân, Thu Nhỏ…): puts timed
    /// states on the slime (BodyState). Stars make it last a little longer.
    /// </summary>
    [CreateAssetMenu(menuName = "Evolve/Abilities/Body State", fileName = "BodyStateSO")]
    public class BodyStateAbility : CaveAbility
    {
        public float Seconds = 3f;
        [Tooltip("Damage taken while it lasts (1 = normal, 0.5 = half)")]
        public float DamageTaken = 1f;
        [Tooltip("Whole hits a shield eats while it lasts (0 = no shield)")]
        public int BlockHits;
        [Tooltip("Cannot move while it lasts")]
        public bool Root;
        [Tooltip("Light range in metres (0 = no light). A lit slime is spotted from further away.")]
        public float Light;
        [Tooltip("Walk up walls while it lasts")]
        public bool Climb;

        [Header("★3")]
        [Tooltip("Share of max health healed on use")]
        public float Star3Heal;
        public int Star3ExtraHits;
        [Tooltip("Damage taken while it lasts at ★3 (1 = unchanged)")]
        public float Star3DamageTaken = 1f;
        [Tooltip("Prey within this many metres freeze for 1 second when it starts (0 = none)")]
        public float Star3Dazzle;

        public override bool Activate(GameObject user, int stars)
        {
            var s = BodyState.On(user);
            float t = Seconds * StarPower(stars);
            bool star3 = stars >= 3;
            float taken = DamageTaken * (star3 ? Star3DamageTaken : 1f);
            if (taken < 1f) s.TakeLessDamage(taken, t);
            if (BlockHits > 0) s.Shield(BlockHits + (star3 ? Star3ExtraHits : 0), t);
            if (Root) s.Root(t);
            if (Light > 0f) s.Glow(Light, t);
            if (Climb) s.Climb(t);
            if (!star3) return true;

            if (Star3Heal > 0f && user.TryGetComponent(out SurvivalStats stats)) stats.State.Heal(stats.State.MaxHealth * Star3Heal);
            if (Star3Dazzle > 0f)
                foreach (var p in Prey.Active)
                    if (p != null && (p.transform.position - user.transform.position).sqrMagnitude <= Star3Dazzle * Star3Dazzle)
                        CreatureStatus.Stun(p, 1f);
            return true;
        }
    }
}
