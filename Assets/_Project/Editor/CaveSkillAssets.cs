using Evolve.Abilities;
using Evolve.Organs;
using Evolve.Swallow;
using UnityEditor;
using UnityEngine;

namespace Evolve.EditorTools
{
    /// <summary>
    /// Evolve → Create Cave Skills: for every row of <see cref="CaveSkillSeed"/>, makes (or updates in place) the gene (OrganSO),
    /// its move (AbilitySO, none for a passive) and the species' genome (DropTableSO) in Assets/_Project/Resources/CaveSkills.
    /// Running it again never duplicates anything. Put a species' DropTable_* asset in the drops field of its Prey / Predator.
    /// </summary>
    public static class CaveSkillAssets
    {
        const string Folder = "Assets/_Project/Resources/CaveSkills";

        [MenuItem("Evolve/Create Cave Skills")]
        public static void Create()
        {
            PrototypeSceneBuilder.EnsureFolder("Assets/_Project/Resources");
            PrototypeSceneBuilder.EnsureFolder(Folder);

            foreach (var d in CaveSkillSeed.All)
            {
                string key = d.Id.Replace("skill.", "");
                var organ = LoadOrCreate<OrganSO>($"{Folder}/Gene_{key}.asset");
                organ.DisplayName = d.Skill;
                organ.Description = $"{d.Description} (từ {d.Species}, {d.Cave})";
                organ.Tier = CaveSkillMath.TierOf(d.Role);
                organ.Family = d.Family;
                organ.Slot = SlotOf(d.Family);
                organ.MaxHealthBonus = d.MaxHealthBonus;
                organ.DamageReduction = d.DamageReduction;
                organ.SonarRangeMultiplier = d.SonarRangeMultiplier;
                organ.GrantedAbility = d.Shape == SkillShape.Passive ? null : Ability(d, $"{Folder}/Skill_{key}.asset");
                EditorUtility.SetDirty(organ);

                var table = LoadOrCreate<DropTableSO>($"{Folder}/DropTable_{key}.asset");
                table.DropChance = 1f;
                table.Organs.Clear();
                table.Organs.Add(organ);
                EditorUtility.SetDirty(table);
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"Đã tạo hoặc cập nhật {CaveSkillSeed.All.Count} kỹ năng hang trong {Folder}.");
        }

        /// <summary>Passives take a body slot each (one per slot): stone → back, vitality → skin, shadow → eyes, the rest → legs.</summary>
        static OrganSlot SlotOf(SkillFamily f) => f switch
        {
            SkillFamily.Stone => OrganSlot.Back,
            SkillFamily.Vitality => OrganSlot.Skin,
            SkillFamily.Shadow => OrganSlot.Eyes,
            _ => OrganSlot.Legs,
        };

        static CaveAbility Ability(CaveSkillDef d, string path)
        {
            CaveAbility a;
            switch (d.Shape)
            {
                case SkillShape.Area:
                    var area = Replace<AreaAbility>(path);
                    area.Radius = d.Radius; area.Push = d.Push; area.Damage = d.Damage; area.StunSeconds = d.StunSeconds; area.MaxTargets = d.MaxTargets;
                    area.Star3Stun = d.Star3Stun; area.Star3Targets = d.Star3Targets;
                    a = area; break;
                case SkillShape.Leap:
                    var leap = Replace<LeapAbility>(path);
                    leap.Distance = d.Distance; leap.Sideways = d.Sideways; leap.InvulnSeconds = d.InvulnSeconds;
                    a = leap; break;
                case SkillShape.Pull:
                    var pull = Replace<PullAbility>(path);
                    pull.Distance = d.Distance; pull.MaxTargets = Mathf.Max(1, d.MaxTargets); pull.Star3Targets = d.Star3Targets;
                    a = pull; break;
                case SkillShape.Trap:
                    var trap = Replace<TrapAbility>(path);
                    trap.Radius = d.Radius; trap.Damage = d.Damage; trap.Seconds = d.Seconds; trap.Star3Stun = d.Star3Stun;
                    a = trap; break;
                default:
                    var body = Replace<BodyStateAbility>(path);
                    body.Seconds = d.Seconds; body.DamageTaken = d.DamageTaken; body.BlockHits = d.BlockHits;
                    body.Root = d.Root; body.Light = d.Light; body.Climb = d.Climb;
                    body.Star3Heal = d.Star3Heal; body.Star3ExtraHits = d.Star3ExtraHits; body.Star3DamageTaken = d.Star3DamageTaken; body.Star3Dazzle = d.Star3Dazzle;
                    a = body; break;
            }
            a.DisplayName = d.Skill;
            a.Description = $"{d.Description}\n★3: {d.Star3}";
            a.Star3Text = d.Star3;
            a.Star3EnergyFactor = d.Star3EnergyFactor;
            a.Cooldown = d.Cooldown;
            a.EnergyCost = d.Energy;
            EditorUtility.SetDirty(a);
            return a;
        }

        /// <summary>Load the asset at the path; if it is missing or of another ability class (the table changed shape), make it anew.</summary>
        static T Replace<T>(string path) where T : CaveAbility
        {
            var existing = AssetDatabase.LoadAssetAtPath<AbilitySO>(path);
            if (existing is T same) return same;
            if (existing != null) AssetDatabase.DeleteAsset(path);
            var created = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(created, path);
            return created;
        }

        static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;
            var created = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(created, path);
            return created;
        }
    }
}
