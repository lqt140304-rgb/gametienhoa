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
        public int BlockHits;
        public bool NearestOnly, Root, Climb, Sideways;

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
                Description = "Co lại 3 giây: nhận ít hơn 50% sát thương, không di chuyển.", Cooldown = 12f, Seconds = 3f, DamageTaken = 0.5f, Root = true },
            new CaveSkillDef { Id = "skill.luoi_dai", Cave = "Hang Khởi Sinh", Species = "Thằn Lằn Bờ Hồ", Role = 'S', Skill = "Lưỡi Dài", Family = SkillFamily.Agility, Shape = SkillShape.Pull,
                Description = "Phóng lưỡi 6 m, kéo con mồi nhỏ về miệng để nuốt.", Cooldown = 8f, Distance = 6f },
            new CaveSkillDef { Id = "skill.o_chan", Cave = "Hang Khởi Sinh", Species = "Cóc Hồ Già", Role = 'C', Skill = "Ợ Chấn", Family = SkillFamily.Agility, Shape = SkillShape.Area,
                Description = "Đẩy lùi mọi thứ trong 4 m.", Cooldown = 15f, Energy = 20f, Radius = 4f, Push = 9f },

            // Vườn Quặng Cầu Vồng
            new CaveSkillDef { Id = "skill.phat_sang", Cave = "Vườn Quặng Cầu Vồng", Species = "Đom Đóm Sắc Màu", Role = 'M', Skill = "Phát Sáng", Family = SkillFamily.Shadow, Shape = SkillShape.BodyState,
                Description = "Tỏa sáng quanh mình 10 giây.", Cooldown = 20f, Energy = 10f, Seconds = 10f, Light = 8f },
            new CaveSkillDef { Id = "skill.lach_ne", Cave = "Vườn Quặng Cầu Vồng", Species = "Thỏ Lá", Role = 'M', Skill = "Lách Né", Family = SkillFamily.Agility, Shape = SkillShape.Leap,
                Description = "Lách ngang 2 m, né đòn trong lúc lách.", Cooldown = 6f, Energy = 10f, Distance = 2f, Sideways = true, InvulnSeconds = 0.35f },
            new CaveSkillDef { Id = "skill.vo_cung", Cave = "Vườn Quặng Cầu Vồng", Species = "Bọ Giáp Quặng", Role = 'S', Skill = "Vỏ Cứng", Family = SkillFamily.Stone, Shape = SkillShape.Passive,
                Description = "Nhận ít hơn 15% sát thương.", DamageReduction = 0.15f },
            new CaveSkillDef { Id = "skill.khien_quang", Cave = "Vườn Quặng Cầu Vồng", Species = "Rùa Pha Lê", Role = 'C', Skill = "Khiên Quặng", Family = SkillFamily.Stone, Shape = SkillShape.BodyState,
                Description = "Khiên đỡ trọn một đòn trong 5 giây.", Cooldown = 20f, Energy = 20f, Seconds = 5f, BlockHits = 1 },

            // Vực Nứt Tím
            new CaveSkillDef { Id = "skill.song_do_manh", Cave = "Vực Nứt Tím", Species = "Dơi Âm Thoa", Role = 'M', Skill = "Sóng Dò Mạnh", Family = SkillFamily.Shadow, Shape = SkillShape.Passive,
                Description = "Sóng Dò đi xa hơn 50%.", SonarRangeMultiplier = 1.5f },
            new CaveSkillDef { Id = "skill.cam_gai", Cave = "Vực Nứt Tím", Species = "Bọ Gai Tím", Role = 'M', Skill = "Cắm Gai", Family = SkillFamily.Stone, Shape = SkillShape.Trap,
                Description = "Đặt bẫy gai trên đất, gây sát thương kẻ dẫm vào, tồn tại 20 giây.", Cooldown = 15f, Seconds = 20f, Radius = 1.2f, Damage = 12f },
            new CaveSkillDef { Id = "skill.bam_vach", Cave = "Vực Nứt Tím", Species = "Nhện Tinh Thể", Role = 'S', Skill = "Bám Vách", Family = SkillFamily.Agility, Shape = SkillShape.BodyState,
                Description = "Leo vách đứng 4 giây.", Cooldown = 6f, Energy = 10f, Seconds = 4f, Climb = true },
            new CaveSkillDef { Id = "skill.cham_choang", Cave = "Vực Nứt Tím", Species = "Bọ Cạp Thạch Anh", Role = 'C', Skill = "Châm Choáng", Family = SkillFamily.Poison, Shape = SkillShape.Area,
                Description = "Đâm làm kẻ địch choáng 1 giây.", Cooldown = 12f, Radius = 2.5f, StunSeconds = 1f, Damage = 4f, NearestOnly = true },
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

        /// <summary>Gene tier by role: prey common (C), hunter B, cave lord A (learnable even from a lord that is not a pack leader).</summary>
        public static Evolve.Organs.OrganTier TierOf(char role) => role switch
        {
            'S' => Evolve.Organs.OrganTier.B,
            'C' => Evolve.Organs.OrganTier.A,
            _ => Evolve.Organs.OrganTier.C,
        };
    }
}
