using UnityEngine;

namespace Evolve.Creatures
{
    /// <summary>Marks a creature as already tuned, so a second Apply (adopted then re-embodied) never scales it twice.</summary>
    sealed class CreatureTuned : MonoBehaviour { }

    /// <summary>
    /// When a creature enters a cave (Region.Spawn / Region.Adopt): sizes it by role and tier, and tunes a hunter's danger
    /// (<see cref="CaveTier"/>). A creature with no cave (caveId null) keeps its prefab size and stats.
    /// </summary>
    public static class CreatureTuning
    {
        public static void Apply(GameObject creature, string caveId)
        {
            if (creature == null || caveId == null || creature.GetComponent<CreatureTuned>() != null) return;
            creature.AddComponent<CreatureTuned>();
            int tier = CaveTier.Of(caveId);
            var role = CaveTier.RoleOf(creature.name);
            float k = CaveTier.SizeFactor(role, tier);
            creature.transform.localScale *= k;
            if (creature.TryGetComponent(out CreatureMotor motor)) motor.Resize(k);
            if (creature.TryGetComponent(out CreatureLocomotion loco)) loco.ScaleBy(k);
            if (creature.TryGetComponent(out Predator hunter))
            {
                var danger = CaveTier.DangerOf(tier);
                hunter.ScaleDanger(danger.Damage, danger.Detect, role == CreatureRole.Lord ? danger.LordSpeed : 1f, k);
                hunter.Gentle = role == CreatureRole.Lord && danger.LordGentle;
            }
        }
    }
}
