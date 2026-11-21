using System.Collections.Generic;
using Evolve.Creatures;

namespace Evolve.Combat
{
    /// <summary>The move sets of the species in slice 1 of monster combat (spec 2026-11-20, provisional numbers): data only.</summary>
    public static class MovesetSeed
    {
        static readonly IReadOnlyList<MoveDef> None = new MoveDef[0];

        static readonly Dictionary<string, IReadOnlyList<MoveDef>> sets = new Dictionary<string, IReadOnlyList<MoveDef>>
        {
            // Thằn Lằn Bờ Hồ: lies in wait, then a short explosive lunge
            { "thanlanboho", new[] { new MoveDef { Name = "Rình-Vọt", Shape = MoveShape.Lunge, Windup = 0.6f, Active = 0.25f, Recover = 1.0f, Cooldown = 2.5f, MinRange = 0f, MaxRange = 5f, Distance = 3f, Damage = 10f, Anim = "Attack" } } },
            // Bọ Giáp Quặng: scrapes the ground, then charges in a straight line; hits a wall and it is stunned
            { "bogiapquang", new[] { new MoveDef { Name = "Húc", Shape = MoveShape.Charge, Windup = 0.9f, Active = 2.3f, Recover = 1.6f, Cooldown = 3f, MinRange = 3f, MaxRange = 9f, Distance = 7f, Damage = 14f, Armored = true, Anim = "Attack" } } },
            // Muỗi Rêu: lands on the player and drains blood (driven by LeechRules; Damage is per second)
            { "muoireu", new[] { new MoveDef { Name = "Chích-Hút", Shape = MoveShape.Drain, Windup = 0.4f, Active = 0f, Recover = 0.8f, Cooldown = 0f, MinRange = 0f, MaxRange = 14f, Damage = 1.5f, Anim = "Attack" } } },
            // Rùa Pha Lê (cave lord): moves unlock as it loses health
            { "ruaphale", new[]
            {
                new MoveDef { Name = "Cắn Cổ", Shape = MoveShape.Lunge, Windup = 0.5f, Active = 0.2f, Recover = 1.5f, Cooldown = 2f, MinRange = 0f, MaxRange = 2.5f, Distance = 2.5f, Damage = 22f, Anim = "Attack" },
                new MoveDef { Name = "Rụt Mai–Lao", Shape = MoveShape.Charge, Windup = 0.8f, Active = 1.5f, Recover = 1.5f, Cooldown = 5f, MinRange = 3f, MaxRange = 8f, Distance = 6f, Damage = 18f, Armored = true, Anim = "Retract" },
                new MoveDef { Name = "Xoay Mai", Shape = MoveShape.Spin, Windup = 0.8f, Active = 1.2f, Recover = 1.5f, Cooldown = 6f, MinRange = 0f, MaxRange = 4.5f, Radius = 4f, Damage = 8f, Armored = true, Anim = "Spin", UnlockBelow = 0.66f },
                new MoveDef { Name = "Gọi Bầy", Shape = MoveShape.Call, Windup = 1.0f, Active = 0.1f, Recover = 1.5f, Cooldown = 20f, MinRange = 0f, MaxRange = 25f, Radius = 25f, TargetSpecies = SpeciesNames.Key("Bọ Giáp Quặng"), Anim = "Roar", UnlockBelow = 0.33f },
            } },
        };

        static MoveDef weaselLeap;

        static MovesetSeed()
        {
            // Chồn Khe: hides, bursts out, bites from afar and runs (hit and run)
            weaselLeap = new MoveDef { Name = "Vồ Xa", Shape = MoveShape.Lunge, Windup = 0.35f, Active = 0.5f, Recover = 1.5f, Cooldown = 4f, MinRange = 3f, MaxRange = 9f, Distance = 8f, Damage = 16f, RetreatDistance = 6f, Anim = "Attack" };
            sets["chonkhe"] = new[] { weaselLeap };
            // Khỉ Hang (a new species): throws rocks on an arc, bites and backs off, leaps up to a ledge to throw from above, calls the troop
            sets["khihang"] = new[]
            {
                new MoveDef { Name = "Ném Đá", Shape = MoveShape.Throw, Windup = 0.9f, Active = 0.1f, Recover = 1.0f, Cooldown = 3f, MinRange = 5f, MaxRange = 14f, FlightSeconds = 0.9f, Damage = 9f, Anim = "Throw" },
                new MoveDef { Name = "Cắn Vặt", Shape = MoveShape.Lunge, Windup = 0.4f, Active = 0.2f, Recover = 0.8f, Cooldown = 2f, MinRange = 0f, MaxRange = 2.5f, Distance = 2f, Damage = 8f, RetreatDistance = 4f, Anim = "Attack" },
                new MoveDef { Name = "Nhảy Bục", Shape = MoveShape.Perch, Windup = 0.5f, Active = 0.8f, Recover = 0f, Cooldown = 15f, MinRange = 0f, MaxRange = 6f, Distance = 7f, MinHeight = 1.5f, MaxHeight = 3f, PerchSeconds = 8f, Anim = "Perch" },
                new MoveDef { Name = "Hú Báo Động", Shape = MoveShape.Call, Windup = 0.8f, Active = 0.1f, Recover = 1.0f, Cooldown = 20f, MinRange = 0f, MaxRange = 30f, Radius = 30f, TargetSpecies = SpeciesNames.Key("Khỉ Hang"), UnlockBelow = 0.66f, Anim = "Roar" },
            };
            // Chó Xương (cave lord, a skeletal hound): pounces from range, bites hard, leaps over low obstacles, roars smaller things away, calls scavengers
            sets["choxuong"] = new[]
            {
                new MoveDef { Name = "Vồ", Shape = MoveShape.Lunge, Windup = 0.6f, Active = 0.45f, Recover = 1.5f, Cooldown = 2.5f, MinRange = 2.5f, MaxRange = 7f, Distance = 5f, Damage = 24f, Pins = true, Anim = "Pounce" },
                new MoveDef { Name = "Cắn Nghiền", Shape = MoveShape.Lunge, Windup = 0.35f, Active = 0.2f, Recover = 1.5f, Cooldown = 1.5f, MinRange = 0f, MaxRange = 2.5f, Distance = 1.5f, Damage = 20f, Anim = "Bite" },
                new MoveDef { Name = "Cào Xé", Shape = MoveShape.Lunge, Windup = 0.55f, Active = 0.25f, Recover = 1.2f, Cooldown = 3f, MinRange = 0f, MaxRange = 3.5f, Distance = 0.8f, Damage = 16f, Anim = "Claw" },
                new MoveDef { Name = "Nhảy Vượt", Shape = MoveShape.Perch, Windup = 0.5f, Active = 0.8f, Recover = 0.3f, Cooldown = 12f, MinRange = 0f, MaxRange = 6f, Distance = 6f, MinHeight = 0.3f, MaxHeight = 1.5f, PerchSeconds = 1.5f, Anim = "Leap" },
                new MoveDef { Name = "Gầm", Shape = MoveShape.Roar, Windup = 0.9f, Active = 0.3f, Recover = 1.2f, Cooldown = 20f, MinRange = 0f, MaxRange = 8f, Radius = 8f, Force = 8f, Freeze = 0.8f, FearSeconds = 4f, UnlockBelow = 0.66f, Anim = "Roar" },
                new MoveDef { Name = "Hú Gọi Bầy", Shape = MoveShape.Call, Windup = 1.2f, Active = 0.1f, Recover = 1.5f, Cooldown = 20f, MinRange = 0f, MaxRange = 25f, Radius = 25f, TargetSpecies = SpeciesNames.Key("Bọ Ăn Xác"), UnlockBelow = 0.66f, Anim = "Howl" },
            };
            // Cáo Gió (cave lord): a bite, then three wind spears circling above its tails and thrown one after another, then it calls the weasels
            sets["caogio"] = new[]
            {
                new MoveDef { Name = "Vồ Cắn", Shape = MoveShape.Lunge, Windup = 0.5f, Active = 0.25f, Recover = 1.5f, Cooldown = 2f, MinRange = 0f, MaxRange = 4f, Distance = 4f, Damage = 22f, Anim = "Attack" },
                new MoveDef { Name = "Giáo Gió", Shape = MoveShape.Spears, Windup = 1.3f, Active = 0.9f, Recover = 1.6f, Cooldown = 6f, MinRange = 4f, MaxRange = 16f, Damage = 26f, Force = 9f, SpearCount = 3, SpearSpeed = 26f, LaunchInterval = 0.3f, Anim = "Cast" },
                new MoveDef { Name = "Gọi Bầy", Shape = MoveShape.Call, Windup = 1.0f, Active = 0.1f, Recover = 1.5f, Cooldown = 20f, MinRange = 0f, MaxRange = 30f, Radius = 30f, TargetSpecies = SpeciesNames.Key("Chồn Khe"), UnlockBelow = 0.33f, Anim = "Roar" },
            };
        }

        /// <summary>Species that lie hidden until a player comes close (the weasel).</summary>
        public static bool IsAmbusher(string speciesIdOrName) =>
            !string.IsNullOrEmpty(speciesIdOrName) && SpeciesNames.Key(speciesIdOrName) == "chonkhe";

        /// <summary>The move an ambusher bursts out with (null for a species that does not ambush).</summary>
        public static MoveDef AmbushMove(string speciesIdOrName) => IsAmbusher(speciesIdOrName) ? weaselLeap : null;

        public static IReadOnlyList<MoveDef> For(string speciesIdOrName) =>
            !string.IsNullOrEmpty(speciesIdOrName) && sets.TryGetValue(SpeciesNames.Key(speciesIdOrName), out var s) ? s : None;

        /// <summary>
        /// Species (by <c>SpeciesNames.Key</c>) whose real animation clips for these moves exist (Tell_*, Strike_*, Recover_* triggers; see the
        /// cave-team handoff). For them CreatureCombat stops drawing the stand-in body pose. Empty until the cave team delivers clips.
        /// </summary>
        public static readonly HashSet<string> AuthoredClips = new HashSet<string>
        {
            "thanlanboho",
            "bogiapquang",
            "muoireu",
            "ruaphale",
            "chonkhe",
            "caogio",
            "khihang",
        };

        public static bool Has(string speciesIdOrName) => For(speciesIdOrName).Count > 0;

        /// <summary>How much of the model-wide pose (squash, pitch, shake) a species gets: a rigged hound turns its own bones, so the whole-body pose is softened.</summary>
        public static float PoseStrength(string speciesIdOrName) => SpeciesNames.Key(speciesIdOrName ?? "") == "choxuong" ? 0.4f : 1f;

        /// <summary>The body language of a move: by shape, except the shell moves (animation "Retract") tuck in.</summary>
        public static PoseKind PoseOf(MoveDef m)
        {
            if (m.Anim == "Retract" && m.Shape != MoveShape.Spin) return PoseKind.Withdraw;
            switch (m.Shape)
            {
                case MoveShape.Lunge: return PoseKind.Crouch;
                case MoveShape.Charge: return PoseKind.HornDown;
                case MoveShape.Drain: return PoseKind.Hover;
                case MoveShape.Spin: return PoseKind.Spin;
                case MoveShape.IceDrop: return PoseKind.Cast;
                case MoveShape.Spears: return PoseKind.Cast;
                case MoveShape.Throw: return PoseKind.Throw;
                case MoveShape.Perch: return PoseKind.Crouch;
                case MoveShape.Gust: return PoseKind.Roar;
                case MoveShape.Roar: return PoseKind.Roar;
                default: return PoseKind.Roar;
            }
        }
    }
}
