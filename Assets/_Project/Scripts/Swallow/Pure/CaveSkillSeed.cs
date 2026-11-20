using System.Collections.Generic;
using UnityEngine;

namespace Evolve.Swallow
{
    /// <summary>Skill family, kept for fusing two skills after chapter 1 (spec 4).</summary>
    public enum SkillFamily { None, Agility, Stone, Poison, Shadow, Vitality, Heat }

    /// <summary>Which shared ability class a skill is built from (core brief A.1). Passive = no move, only OrganSO numbers.</summary>
    public enum SkillShape { Passive, Area, Leap, Pull, Trap, BodyState }

    /// <summary>One species and the skill its gene teaches. Numbers are provisional (spec 4); unused fields stay 0.</summary>
    public sealed class CaveSkillDef
    {
        public string Id, Cave, Species, Skill, Description;
        /// <summary>M prey, S hunter, C cave lord (spec 4).</summary>
        public char Role;
        public SkillFamily Family;
        public SkillShape Shape;
        public float Cooldown, Energy = 15f;

        // active moves
        public float Radius, Push, Damage, StunSeconds, Seconds, DamageTaken = 1f, Distance, InvulnSeconds, Light;
        /// <summary>0 = every creature in reach, else only the nearest few.</summary>
        public int BlockHits, MaxTargets;
        public bool Root, Climb, Sideways;

        // the small extra at ★3 (one per skill), and its text for the skill card
        public string Star3;
        public float Star3Stun, Star3Heal, Star3DamageTaken = 1f, Star3Dazzle, Star3EnergyFactor = 1f;
        public int Star3Targets, Star3ExtraHits;

        // passives (OrganSO fields)
        public float MaxHealthBonus, DamageReduction, SonarRangeMultiplier = 1f;
    }

    /// <summary>
    /// Plain data for the cave skills (spec 4). The editor menu Evolve → Create Cave Skills turns each row into an OrganSO
    /// (the gene), an AbilitySO (the move) and a DropTableSO (the species' genome). Wave 1: the 12 skills of the 3 caves that exist.
    /// </summary>
    public static class CaveSkillSeed
    {
        public static IReadOnlyList<CaveSkillDef> All { get; } = new List<CaveSkillDef>
        {
            // Hang Khởi Sinh
            new CaveSkillDef { Id = "skill.da_day", Cave = "Hang Khởi Sinh", Species = "Bọ Cánh Cứng Nhỏ", Role = 'M', Skill = "Da Dày", Family = SkillFamily.Vitality, Shape = SkillShape.Passive,
                Description = "+10 máu tối đa.", MaxHealthBonus = 10f },
            new CaveSkillDef { Id = "skill.thu_minh", Cave = "Hang Khởi Sinh", Species = "Ốc Hồ", Role = 'M', Skill = "Thu Mình", Family = SkillFamily.Stone, Shape = SkillShape.BodyState,
                Description = "Co lại 3 giây: nhận ít hơn 50% sát thương, không di chuyển.", Energy = 30f, Star3 = "Thu mình xong hồi 5% máu tối đa.", Star3Heal = 0.05f, Cooldown = 12f, Seconds = 3f, DamageTaken = 0.5f, Root = true },
            new CaveSkillDef { Id = "skill.luoi_dai", Cave = "Hang Khởi Sinh", Species = "Thằn Lằn Bờ Hồ", Role = 'S', Skill = "Lưỡi Dài", Family = SkillFamily.Agility, Shape = SkillShape.Pull,
                Description = "Phóng lưỡi 6 m, kéo con mồi nhỏ về miệng để nuốt.", Energy = 30f, Cooldown = 8f, Distance = 6f, MaxTargets = 1, Star3 = "Kéo được 2 con.", Star3Targets = 2 },
            new CaveSkillDef { Id = "skill.o_chan", Cave = "Hang Khởi Sinh", Species = "Cóc Hồ Già", Role = 'C', Skill = "Ợ Chấn", Family = SkillFamily.Agility, Shape = SkillShape.Area,
                Description = "Đẩy lùi mọi thứ trong 4 m.", Cooldown = 15f, Energy = 35f, Radius = 4f, Push = 9f, Star3 = "Kẻ bị đẩy còn choáng 0,5 giây.", Star3Stun = 0.5f },

            // Vườn Quặng Cầu Vồng
            new CaveSkillDef { Id = "skill.phat_sang", Cave = "Vườn Quặng Cầu Vồng", Species = "Đom Đóm Sắc Màu", Role = 'M', Skill = "Phát Sáng", Family = SkillFamily.Shadow, Shape = SkillShape.BodyState,
                Description = "Tỏa sáng quanh mình 10 giây.", Cooldown = 20f, Energy = 25f, Seconds = 10f, Light = 8f, Star3 = "Lúc bật sáng, con mồi trong 5 m bị lóa, đứng im 1 giây.", Star3Dazzle = 5f },
            new CaveSkillDef { Id = "skill.lach_ne", Cave = "Vườn Quặng Cầu Vồng", Species = "Thỏ Lá", Role = 'M', Skill = "Lách Né", Family = SkillFamily.Agility, Shape = SkillShape.Leap,
                Description = "Lách ngang 2 m, né đòn trong lúc lách.", Cooldown = 6f, Energy = 25f, Distance = 2f, Sideways = true, InvulnSeconds = 0.35f, Star3 = "Chỉ tốn nửa năng lượng.", Star3EnergyFactor = 0.5f },
            new CaveSkillDef { Id = "skill.vo_cung", Cave = "Vườn Quặng Cầu Vồng", Species = "Bọ Giáp Quặng", Role = 'S', Skill = "Vỏ Cứng", Family = SkillFamily.Stone, Shape = SkillShape.Passive,
                Description = "Nhận ít hơn 15% sát thương.", DamageReduction = 0.15f },
            new CaveSkillDef { Id = "skill.khien_quang", Cave = "Vườn Quặng Cầu Vồng", Species = "Rùa Pha Lê", Role = 'C', Skill = "Khiên Quặng", Family = SkillFamily.Stone, Shape = SkillShape.BodyState,
                Description = "Khiên đỡ trọn một đòn trong 5 giây.", Cooldown = 20f, Energy = 35f, Seconds = 5f, BlockHits = 1, Star3 = "Đỡ được 2 đòn.", Star3ExtraHits = 1 },

            // Vực Nứt Tím
            new CaveSkillDef { Id = "skill.song_do_manh", Cave = "Vực Nứt Tím", Species = "Dơi Âm Thoa", Role = 'M', Skill = "Sóng Dò Mạnh", Family = SkillFamily.Shadow, Shape = SkillShape.Passive,
                Description = "Sóng Dò đi xa hơn 50%.", SonarRangeMultiplier = 1.5f },
            new CaveSkillDef { Id = "skill.cam_gai", Cave = "Vực Nứt Tím", Species = "Bọ Gai Tím", Role = 'M', Skill = "Cắm Gai", Family = SkillFamily.Stone, Shape = SkillShape.Trap,
                Description = "Đặt bẫy gai trên đất, gây sát thương kẻ dẫm vào, tồn tại 20 giây.", Energy = 30f, Cooldown = 15f, Seconds = 20f, Radius = 1.2f, Damage = 12f, Star3 = "Kẻ dẫm vào còn choáng 0,5 giây.", Star3Stun = 0.5f },
            new CaveSkillDef { Id = "skill.bam_vach", Cave = "Vực Nứt Tím", Species = "Nhện Tinh Thể", Role = 'S', Skill = "Bám Vách", Family = SkillFamily.Agility, Shape = SkillShape.BodyState,
                Description = "Leo vách đứng 4 giây.", Cooldown = 6f, Energy = 25f, Seconds = 4f, Climb = true, Star3 = "Đang leo thì nhận ít hơn 30% sát thương.", Star3DamageTaken = 0.7f },
            new CaveSkillDef { Id = "skill.cham_choang", Cave = "Vực Nứt Tím", Species = "Bọ Cạp Thạch Anh", Role = 'C', Skill = "Châm Choáng", Family = SkillFamily.Poison, Shape = SkillShape.Area,
                Description = "Đâm làm kẻ địch choáng 1 giây.", Energy = 30f, Cooldown = 12f, Radius = 2.5f, StunSeconds = 1f, MaxTargets = 1, Star3 = "Choáng 2 con gần nhất.", Star3Targets = 2 },
        };
    }

    /// <summary>Pure numbers behind the cave skills, so they can be tested without Unity.</summary>
    public static class CaveSkillMath
    {
        /// <summary>
        /// Damage left after the body's defences: a running shield eats the whole hit (and one charge), otherwise the passive
        /// reduction (Vỏ Cứng, capped at 80%) and the active state multiplier (Thu Mình 0.5, Lách Né 0) both apply.
        /// </summary>
        public static float Incoming(float amount, float passiveReduction, float stateMultiplier, ref int shieldCharges)
        {
            if (amount <= 0f) return 0f;
            if (shieldCharges > 0) { shieldCharges--; return 0f; }
            return amount * (1f - Mathf.Clamp(passiveReduction, 0f, 0.8f)) * Mathf.Max(0f, stateMultiplier);
        }

        /// <summary>Push of an area move at a distance: full at the centre, 40% at the edge, none outside.</summary>
        public static float Falloff(float distance, float radius)
        {
            if (radius <= 0f || distance > radius) return 0f;
            return 1f - 0.6f * Mathf.Clamp01(distance / radius);
        }

        /// <summary>★1 base, ★2 +10%, ★3 +20%: stars help a little; the real gain at ★3 is the skill's small extra (CaveSkillDef.Star3).</summary>
        public static float StarPower(int stars) => 1f + 0.1f * (Mathf.Clamp(stars, 1, 3) - 1);

        /// <summary>Cooldown at ★2 is 5% shorter, at ★3 10%.</summary>
        public static float StarCooldown(float cooldown, int stars) => cooldown * (1f - 0.05f * (Mathf.Clamp(stars, 1, 3) - 1));

        /// <summary>Detection reach of hunters toward a glowing slime (Phát Sáng).</summary>
        public const float GlowDetectMultiplier = 1.5f;

        /// <summary>Gene tier by role: prey common (C), hunter B, cave lord A (learnable even from a lord that is not a pack leader).</summary>
        public static Evolve.Organs.OrganTier TierOf(char role) => role switch
        {
            'S' => Evolve.Organs.OrganTier.B,
            'C' => Evolve.Organs.OrganTier.A,
            _ => Evolve.Organs.OrganTier.C,
        };
    }
}
