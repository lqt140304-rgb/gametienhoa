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
            if (Evolve.Combat.ChoXuongRigFx.Has(creature.name)) k = Mathf.Min(k, 1.2f);
            else if (Evolve.Combat.BirdRigFx.Has(creature.name)) k = Mathf.Min(k, 1.3f);   // the Dreadwing bird is already 1.7 m tall with a 2.1 m span in its prefab: a lord's 2.4-3.2x cave size would make a 5 m monster
            else if (Evolve.Combat.GolemRigFx.Has(creature.name)) k = Mathf.Min(k, 1.44f);   // 1.2 times 1.2: the golem was asked to be 20% bigger   // the Meshy hound is already small in its prefab and a golem scaled to 6-8 m got stuck on everything: a lord's 2.4-3.2x cave size would be too much
            creature.transform.localScale *= k;
            if (creature.TryGetComponent(out CreatureMotor motor)) motor.Resize(k);
            if (creature.TryGetComponent(out CreatureLocomotion loco)) loco.ScaleBy(k);
            if (role == CreatureRole.Lord && Evolve.Combat.LordRegalia.Has(creature.name)) Evolve.Combat.LordRegalia.Attach(creature);
            if (Evolve.Combat.FoxRigFx.Has(creature.name)) Evolve.Combat.FoxRigFx.Attach(creature);
            if (Evolve.Combat.BirdRigFx.Has(creature.name)) Evolve.Combat.BirdRigFx.Attach(creature);   // the Meshy bird: wings and beak
            if (Evolve.Combat.ChoXuongRigFx.Has(creature.name)) Evolve.Combat.ChoXuongRigFx.Attach(creature);
            if (Evolve.Combat.GolemRigFx.Has(creature.name)) Evolve.Combat.GolemRigFx.Attach(creature);   // the Meshy golem: arms, spine, glowing core   // the Meshy hound: neck, jaw, paw, flinch
            if (creature.TryGetComponent(out Evolve.Combat.CreatureCombat combat))
            {
                combat.TellScale = tier == 0 ? 1.3f : 1f;   // the start cavern gives the player longer to read the tell
                if (creature.TryGetComponent(out Predator _)) combat.DamageScale = CaveTier.DangerOf(tier).Damage;
            }
            if (creature.TryGetComponent(out Predator hunter))
            {
                var danger = CaveTier.DangerOf(tier);
                hunter.ScaleDanger(danger.Damage, danger.Detect, role == CreatureRole.Lord ? danger.LordSpeed : 1f, k);
                hunter.Gentle = role == CreatureRole.Lord && danger.LordGentle;
            }
        }
    }
}
