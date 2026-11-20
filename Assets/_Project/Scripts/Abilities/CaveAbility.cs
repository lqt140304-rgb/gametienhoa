using Evolve.Swallow;

namespace Evolve.Abilities
{
    /// <summary>
    /// Base of the cave skills: they run on the gene's stars (1..3). Stars help a little (CaveSkillMath.StarPower, StarCooldown);
    /// at ★3 each skill also gets one small extra, described by <see cref="Star3Text"/>.
    /// </summary>
    public abstract class CaveAbility : AbilitySO
    {
        [UnityEngine.Tooltip("Skill family: sets the colour of its effects, and later what it fuses with")]
        public SkillFamily Family;
        public string Star3Text;
        [UnityEngine.Tooltip("Energy at ★3 as a share of the normal cost (1 = same)")]
        public float Star3EnergyFactor = 1f;

        public override bool UsesStars => true;
        public override float CooldownFor(int stars) => CaveSkillMath.StarCooldown(Cooldown, stars);
        public override float EnergyFor(int stars) => stars >= 3 ? EnergyCost * Star3EnergyFactor : EnergyCost;
        public override bool Activate(UnityEngine.GameObject user) => Activate(user, 1);

        protected static float StarPower(int stars) => CaveSkillMath.StarPower(stars);
    }
}
