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
            // Golem Thạch Anh (cave lord): slams the ground, throws boulders, hardens its crystal armour, and at low health blows its core
            sets["golemthachanh"] = new[]
            {
                // the standard boss kit (everything slow to start, hits like a falling wall and takes long to recover from): 2 normal, a combo charge, no plain ranged skill (its special throws the rocks), 1 special (Giáng Thạch: it leaps into the sky, lands on a marked circle, stone spikes rise round it and it pulls them out and hurls them one after another, dashing to the next), 1 ultimate; the passive is Thân Tinh Thể (PassiveRules)
                new MoveDef { Name = "Nện Đấm", Form = MoveForm.Fist, Role = MoveRole.Normal, Shape = MoveShape.Lunge, Windup = 0.9f, Active = 0.25f, Recover = 1.8f, Cooldown = 3f, Combo = new[] { "Quét Tay" }, MinRange = 0f, MaxRange = 3.5f, Distance = 0.8f, Damage = 28f, Force = 6f, Anim = "Punch" },
                new MoveDef { Name = "Quét Tay", Form = MoveForm.Fist, Role = MoveRole.Normal, Shape = MoveShape.Spin, Windup = 0.8f, Active = 0.2f, Recover = 1.7f, Cooldown = 4f, MinRange = 0f, MaxRange = 3.6f, Radius = 3.6f, Damage = 24f, Force = 7f, Freeze = 0.3f, Anim = "Sweep" },
                new MoveDef { Name = "Húc Đầu", Form = MoveForm.Fist, Role = MoveRole.Combo, Shape = MoveShape.Charge, Combo = new[] { "Nện Đấm", "Quét Tay", "Nện Đấm", "Quét Tay" }, Ramp = true, Windup = 1f, Active = 1.7f, Recover = 1.8f, Cooldown = 24f, MinRange = 4f, MaxRange = 14f, Distance = 12f, Damage = 26f, Force = 9f, Armored = true, Anim = "Rush" },
                new MoveDef { Name = "Giáng Thạch", Form = MoveForm.Fist, Role = MoveRole.Special, Dodge = true, Shape = MoveShape.Meteor, Windup = 2.4f, Active = 0.35f, Recover = 0.2f, Cooldown = 55f, RoarRadius = 12f, RoarForce = 14f, MinRange = 4f, MaxRange = 16f, Radius = 4.2f, Damage = 30f, Force = 8f, Freeze = 0.4f, SpikeCount = 4, SpikeRadius = 6.5f, MissileDamage = 18f, MissileSize = 0.8f, FlightSeconds = 0.8f, RecoverFactor = 1f, Rest = 5f, Anim = "Meteor" },
                new MoveDef { Name = "Lõi Nổ", Role = MoveRole.Ultimate, Shape = MoveShape.Spin, Windup = 2.4f, Active = 0.3f, Recover = 2.6f, Cooldown = 55f, MinRange = 0f, MaxRange = 8f, Radius = 8f, Damage = 40f, Force = 12f, Freeze = 0.8f, Armored = true, UnlockBelow = 0.25f, Anim = "CoreBlast" },
                // the axe form (the golem has pulled its axe out of the heart stone): more blows, a throw, a shock wave; the blows run in a chain (Chém Dọc, Chém Ngang, Chém Dọc)
                new MoveDef { Name = "Chém Dọc", Form = MoveForm.Axe, Role = MoveRole.Normal, Shape = MoveShape.Lunge, Windup = 0.8f, Active = 0.25f, Recover = 1.2f, Cooldown = 3f, MinRange = 0f, MaxRange = 4f, Distance = 0.8f, Damage = 30f, Force = 6f, Combo = new[] { "Chém Ngang", "Chém Dọc" }, Anim = "AxeChop" },
                new MoveDef { Name = "Chém Ngang", Form = MoveForm.Axe, Role = MoveRole.Normal, Shape = MoveShape.Spin, Windup = 0.7f, Active = 0.2f, Recover = 1.3f, Cooldown = 4f, MinRange = 0f, MaxRange = 4f, Radius = 4f, Damage = 26f, Force = 8f, Freeze = 0.3f, Anim = "AxeSweep" },
                new MoveDef { Name = "Ném Rìu", Form = MoveForm.Axe, Role = MoveRole.Ranged, Shape = MoveShape.AxeThrow, Windup = 1.4f, Active = 1f, Recover = 1.2f, Cooldown = 14f, MinRange = 6f, MaxRange = 20f, FlightSeconds = 1f, Damage = 34f, Force = 8f, Armored = true, Anim = "AxeThrow" },
                new MoveDef { Name = "Bổ Đất", Form = MoveForm.Axe, Role = MoveRole.Special, Dodge = true, Shape = MoveShape.Shockwave, Windup = 1.8f, Active = 0.4f, Recover = 2.2f, Cooldown = 20f, MinRange = 3f, MaxRange = 14f, Damage = 30f, Force = 8f, Anim = "AxeSlam" },
            };
            // Chó Xương (cave lord, a skeletal hound): pounces and pins, bites hard, claws, leaps over low obstacles, roars smaller things away, summons small copies of itself from the ground
            sets["choxuong"] = new[]
            {
                // the standard boss kit: 2 normal attacks, 1 ranged, 1 special with a mini game (the clamping pounce), 1 ultimate near the end; the passive is Khát Máu (PassiveRules)
                new MoveDef { Name = "Vồ", Role = MoveRole.Special, Shape = MoveShape.Lunge, Windup = 0.9f, Active = 0.6f, Recover = 1.8f, Cooldown = 4.5f, MinRange = 3f, MaxRange = 5.5f, AbortBeyond = 7.5f, Distance = 5.5f, Damage = 24f, Pins = true, Anim = "Pounce" },
                new MoveDef { Name = "Cắn Nghiền", Role = MoveRole.Normal, Shape = MoveShape.Lunge, Windup = 0.35f, Active = 0.2f, Recover = 1.5f, Cooldown = 3f, MinRange = 0f, MaxRange = 2.5f, Distance = 1.5f, Damage = 20f, Anim = "Bite" },
                new MoveDef { Name = "Cào Xé", Role = MoveRole.Normal, Shape = MoveShape.Lunge, Windup = 0.55f, Active = 0.25f, Recover = 1.2f, Cooldown = 5f, MinRange = 0f, MaxRange = 3.5f, Distance = 0.8f, Damage = 16f, Anim = "Claw" },
                new MoveDef { Name = "Phun Xương", Role = MoveRole.Ranged, Shape = MoveShape.Throw, Windup = 0.9f, Active = 0.1f, Recover = 1.4f, Cooldown = 7f, MinRange = 6f, MaxRange = 16f, FlightSeconds = 0.8f, Damage = 14f, Anim = "Spit" },
                new MoveDef { Name = "Triệu Hồi", Role = MoveRole.Ultimate, Shape = MoveShape.Summon, Windup = 1.4f, Active = 0.2f, Recover = 1.5f, Cooldown = 40f, MinRange = 0f, MaxRange = 20f, Radius = 4f, SummonCount = 3, SummonSeconds = 25f, UnlockBelow = 0.25f, Anim = "Howl" },
            };
            // Chim Hang Mẹ (cave lord, a vulture): pecks up close, cuts the air with both wings, dives from the sky, and snatches the player up in its talons, throws them into the sky and rakes them as they fall (BirdMoves; numbers provisional)
            sets["chimhangme"] = new[]
            {
                new MoveDef { Name = "Mổ", Role = MoveRole.Normal, Shape = MoveShape.Lunge, Windup = 0.6f, Active = 0.25f, Recover = 1.4f, Cooldown = 3.5f, MinRange = 0f, MaxRange = 3.2f, Distance = 1.4f, Damage = 14f, RetreatDistance = 6f, Anim = "Peck" },   // hit and run: it flutters back 6 m, which puts the player back in range of the sky moves (they need 3-5 m)
                // the second close attack: a wing swept across in a wide arc (hits everything round it), a push to the side
                new MoveDef { Name = "Vả Cánh", Role = MoveRole.Normal, Shape = MoveShape.Spin, Windup = 0.7f, Active = 0.2f, Recover = 1.3f, Cooldown = 4f, MinRange = 0f, MaxRange = 3.6f, Radius = 3.6f, Damage = 16f, Force = 6f, Freeze = 0.3f, Anim = "WingSlap" },
                // two chops of the wings, each throws one blade of wind from each wing tip (Damage and Force are per blade, SpearSpeed is how fast they fly)
                new MoveDef { Name = "Chém Gió", Role = MoveRole.Ranged, Shape = MoveShape.WindSlash, Windup = 1.1f, Active = 0.9f, Recover = 1.6f, Cooldown = 7f, MinRange = 5f, MaxRange = 16f, Damage = 10f, Force = 6f, SpearSpeed = 24f, Anim = "Slash" },
                // climbs over the player flapping, then drops on the marked circle (Radius) with its wings folded
                new MoveDef { Name = "Bổ Nhào", Role = MoveRole.Special, Dodge = true, Shape = MoveShape.Swoop, Windup = 2f, Active = 0.8f, Recover = 2.2f, Rest = 1.5f, Cooldown = 18f, MinRange = 5f, MaxRange = 18f, AbortBeyond = 22f, Radius = 3f, Damage = 28f, Force = 8f, Freeze = 0.4f, Anim = "Swoop" },
                // the dash catches (Damage) and carries, tosses and rakes (MissileDamage per pass); a dash that catches nothing leaves it open to a counter
                new MoveDef { Name = "Quắp", Role = MoveRole.Combo, Shape = MoveShape.Talon, Windup = 1.4f, Active = 0.8f, Recover = 1.8f, Rest = 1.5f, Cooldown = 26f, MinRange = 4f, MaxRange = 11f, AbortBeyond = 15f, Damage = 8f, MissileDamage = 7f, Anim = "Talon" },
                // the ultimate (health 25% or less): a screech that stuns everyone within 9 m and throws them back, then three dives in a row onto the marked circle (the Combo chain, each shortly after the last)
                new MoveDef { Name = "Rít Gầm", Role = MoveRole.Ultimate, Shape = MoveShape.Spin, Windup = 1.6f, Active = 0.4f, Recover = 0.6f, Cooldown = 45f, MinRange = 0f, MaxRange = 16f, Radius = 9f, Damage = 14f, Force = 12f, Freeze = 1.2f, Armored = true, UnlockBelow = 0.25f, Combo = new[] { "Bổ Nhào", "Bổ Nhào", "Bổ Nhào" }, Anim = "Screech" },
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

        /// <summary>The moves a boss has in <paramref name="form"/>: those of that form and the ones every form shares.</summary>
        public static IReadOnlyList<MoveDef> ForForm(string speciesIdOrName, MoveForm form)
        {
            var r = new List<MoveDef>();
            foreach (var m in For(speciesIdOrName)) if (m.Form == MoveForm.Both || m.Form == form) r.Add(m);
            return r;
        }

        public static bool Has(string speciesIdOrName) => For(speciesIdOrName).Count > 0;

        /// <summary>How much of the model-wide pose (squash, pitch, shake) a species gets: a rigged hound turns its own bones, so the whole-body pose is softened.</summary>
        public static float PoseStrength(string speciesIdOrName)
        {
            string key = SpeciesNames.Key(speciesIdOrName ?? "");
            return key == "choxuong" ? 0.4f : key == "golemthachanh" ? 0.35f : key == "chimhangme" ? 0f : 1f;   // the bird pitches and spreads its own bones (BirdRigFx)
        }

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
                case MoveShape.Summon: return PoseKind.Roar;
                case MoveShape.Harden: return PoseKind.Withdraw;
                case MoveShape.Meteor: return PoseKind.Crouch;
                default: return PoseKind.Roar;
            }
        }
    }
}
