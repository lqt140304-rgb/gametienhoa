using System;
using System.Collections.Generic;

namespace Evolve.Traits
{
    /// <summary>
    /// The second wave of cores and flaws (plan 2026-11-20-traits-wave2) as pure functions. <c>strength</c> is how much of a flaw is felt
    /// (PlayerTraits.FlawStrength: 0 = no flaw, 1 = full). All numbers are provisional.
    /// </summary>
    public static class EnvTraitRules
    {
        public const string NguySac = "core.nguy_sac", MangThamThau = "core.mang_tham_thau", QuangHop = "core.quang_hop", AnMon = "core.an_mon";
        public const string DeBiChuY = "flaw.de_bi_chu_y", VetMui = "flaw.vet_mui", NgaiDong = "flaw.ngai_dong", MauKho = "flaw.mau_kho", BamBun = "flaw.bam_bun";

        // Nhân Ngụy Sắc
        public const float NguySacStillSeconds = 2f;
        public const float NguySacDetectFactor = 0.4f;
        // Nhân Màng Thẩm Thấu
        public const float ThamThauStillSpeed = 0.3f;
        public const float ThamThauPerSecond = 0.015f;   // share of the hunger bar per second
        public const float ThamThauCap = 0.6f;
        public const float ThamThauHitPause = 3f;
        // Nhân Quang Hợp
        public const float QuangHopEnergyPerSecond = 3f;
        // Nhân Ăn Mòn
        public const float AnMonNutrition = 1.3f;
        public const float AnMonDissolveSpeed = 1.3f;
        // Dễ Bị Chú Ý
        public const float LureRadius = 20f;
        // Vệt Mùi
        public const float ScentSeconds = 20f;
        public const float ScentRadius = 15f;
        public const float ScentSpacing = 1.5f;          // metres between two trail points
        // Ngại Đông
        public const float CrowdRadius = 6f;
        public const int CrowdCount = 3;
        public const float CrowdRegenLoss = 0.2f;
        // Mau Khô
        public const float DrySeconds = 90f;
        public const float DrySlow = 0.08f;
        // Bám Bùn
        public const float MudSeconds = 2f;
        public const float MudSlow = 0.3f;

        static float Clamp01(float v) => float.IsNaN(v) ? 0f : Math.Min(1f, Math.Max(0f, v));

        /// <summary>Ngụy Sắc: multiplier on a predator's detection range for a player who has been still this long.</summary>
        public static float NguySacDetect(bool hasCore, float stillSeconds) =>
            hasCore && stillSeconds >= NguySacStillSeconds ? NguySacDetectFactor : 1f;

        /// <summary>
        /// Màng Thẩm Thấu: hunger points gained per second (0 when it does not work). <paramref name="secondsSinceHit"/> &lt; 0 means never hit.
        /// While it works, the normal hunger burn also stops (see <see cref="ThamThauStopsBurn"/>).
        /// </summary>
        public static float ThamThauGain(bool hasCore, float speed, bool wetGround, bool swimming, float hunger, float maxHunger, float secondsSinceHit)
        {
            if (!ThamThauActive(hasCore, speed, wetGround, swimming, secondsSinceHit)) return 0f;
            float cap = ThamThauCap * maxHunger;
            return hunger >= cap - 1e-3f ? 0f : ThamThauPerSecond * maxHunger;
        }

        public static bool ThamThauStopsBurn(bool hasCore, float speed, bool wetGround, bool swimming, float secondsSinceHit) =>
            ThamThauActive(hasCore, speed, wetGround, swimming, secondsSinceHit);

        static bool ThamThauActive(bool hasCore, float speed, bool wetGround, bool swimming, float secondsSinceHit) =>
            hasCore && wetGround && !swimming && speed < ThamThauStillSpeed && (secondsSinceHit < 0f || secondsSinceHit >= ThamThauHitPause);

        /// <summary>Quang Hợp: extra energy per second.</summary>
        public static float QuangHopEnergy(bool hasCore, bool inLight, bool underwater) =>
            hasCore && inLight && !underwater ? QuangHopEnergyPerSecond : 0f;

        /// <summary>Ăn Mòn: multiplier on the nutrition of anything swallowed.</summary>
        public static float AnMonNutritionMultiplier(bool hasCore) => hasCore ? AnMonNutrition : 1f;

        /// <summary>Ăn Mòn: multiplier on how fast a big carcass dissolves.</summary>
        public static float AnMonDissolveMultiplier(bool hasCore) => hasCore ? AnMonDissolveSpeed : 1f;

        /// <summary>Dễ Bị Chú Ý: how far away a patrolling predator is drawn toward the player (0 = not at all).</summary>
        public static float LureDistance(float strength) => LureRadius * Clamp01(strength);

        /// <summary>Ngại Đông: multiplier on energy regeneration with <paramref name="creaturesNearby"/> creatures within 6 m.</summary>
        public static float CrowdRegen(int creaturesNearby, float strength) =>
            creaturesNearby >= CrowdCount ? 1f - CrowdRegenLoss * Clamp01(strength) : 1f;

        /// <summary>Mau Khô: speed multiplier after this many seconds away from water and shelter.</summary>
        public static float DrySpeed(float secondsDry, float strength) =>
            secondsDry > DrySeconds ? 1f - DrySlow * Clamp01(strength) : 1f;

        /// <summary>Bám Bùn: speed multiplier this many seconds after leaving shallow water (negative = still in it or never was).</summary>
        public static float MudSpeed(float secondsSinceLeft, float strength) =>
            secondsSinceLeft >= 0f && secondsSinceLeft < MudSeconds ? 1f - MudSlow * Clamp01(strength) : 1f;
    }

    /// <summary>
    /// Vệt Mùi: the trail a player leaves behind, as points on the ground (x, z) with the time they were laid. Points older than
    /// <see cref="EnvTraitRules.ScentSeconds"/> are forgotten. Pure, so it can be tested.
    /// </summary>
    public sealed class ScentTrail
    {
        readonly List<(float x, float z, float t)> points = new List<(float, float, float)>();

        public int Count => points.Count;

        /// <summary>Called as the player moves: lays a new point once they are far enough from the last one.</summary>
        public void Visit(float x, float z, float now)
        {
            Forget(now);
            if (points.Count > 0)
            {
                var last = points[points.Count - 1];
                float dx = x - last.x, dz = z - last.z;
                if (dx * dx + dz * dz < EnvTraitRules.ScentSpacing * EnvTraitRules.ScentSpacing) return;
            }
            points.Add((x, z, now));
        }

        /// <summary>Is a point of the trail (still fresh) within <paramref name="radius"/> of (x, z)?</summary>
        public bool Near(float x, float z, float now, float radius)
        {
            float r2 = radius * radius;
            foreach (var p in points)
            {
                if (now - p.t > EnvTraitRules.ScentSeconds) continue;
                float dx = x - p.x, dz = z - p.z;
                if (dx * dx + dz * dz <= r2) return true;
            }
            return false;
        }

        public void Clear() => points.Clear();

        void Forget(float now)
        {
            int n = 0;
            while (n < points.Count && now - points[n].t > EnvTraitRules.ScentSeconds) n++;
            if (n > 0) points.RemoveRange(0, n);
        }
    }
}
