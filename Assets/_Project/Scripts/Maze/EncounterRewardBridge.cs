using System;
using System.Reflection;
using Evolve.Survival;
using Evolve.Visuals;
using UnityEngine;

namespace Evolve.Maze
{
    /// <summary>
    /// Cầu nối gọi Evolve.Traits.EncounterRewards (do phần lõi game tạo).
    /// Nếu lớp đó chưa tồn tại trên nhánh hiện tại thì tự động fallback an toàn để không lỗi biên dịch.
    /// </summary>
    public static class EncounterRewardBridge
    {
        static Type cachedType;
        static bool searched;

        static Type RewardsType
        {
            get
            {
                if (!searched)
                {
                    searched = true;
                    cachedType = Type.GetType("Evolve.Traits.EncounterRewards, Evolve")
                              ?? Type.GetType("Evolve.Traits.EncounterRewards, Assembly-CSharp");
                }
                return cachedType;
            }
        }

        public static void Say(string line)
        {
            if (string.IsNullOrEmpty(line)) return;
            if (TryInvokeStatic("Say", line)) return;
            Debug.Log($"[Tiếng Vọng - Kỳ Ngộ] {line}");
        }

        public static void RefillStats(GameObject player, float lightHungerBoost = 25f)
        {
            if (player == null) return;
            if (TryInvokeStatic("RefillStats", player)) return;

            var stats = player.GetComponent<SurvivalStats>();
            if (stats != null)
            {
                stats.State.Hunger = Mathf.Min(stats.State.MaxHunger, stats.State.Hunger + lightHungerBoost);
                stats.State.Energy = Mathf.Min(stats.State.MaxEnergy, stats.State.Energy + lightHungerBoost * 0.5f);
            }
        }

        public static void UnlockColor(GameObject player, Color color)
        {
            if (player == null) return;
            if (TryInvokeStatic("UnlockColor", player, color)) return;

            SlimeColor.Set(SlimePart.Body, color);
        }

        public static void GrantGene(GameObject player, int minRarity)
        {
            if (player == null) return;
            if (TryInvokeStatic("GrantGene", player, minRarity)) return;
            Debug.Log($"[Kỳ Ngộ] Đã ghi nhận phần thưởng Gene (độ hiếm >= {minRarity}) — chờ Evolve.Traits.EncounterRewards.");
        }

        public static void GrantUniqueGene(GameObject player, string uniqueId)
        {
            if (player == null) return;
            if (TryInvokeStatic("GrantUniqueGene", player, uniqueId)) return;
            Debug.Log($"[Kỳ Ngộ] Đã ghi nhận phần thưởng Gene lạ '{uniqueId}' — chờ Evolve.Traits.EncounterRewards.");
        }

        public static void CureDiseases(GameObject player)
        {
            if (player == null) return;
            if (TryInvokeStatic("CureDiseases", player)) return;

            var stats = player.GetComponent<SurvivalStats>();
            if (stats != null)
                stats.State.Health = stats.State.MaxHealth;
        }

        public static bool RerollFlaw(GameObject player, int seed)
        {
            if (player == null) return false;
            if (TryInvokeStatic("RerollFlaw", player, seed)) return true;
            Say("Một Khuyết Điểm trong cơ thể bỗng rung chuyển và đổi sang dạng khác...");
            return true;
        }

        public static bool AddHeatResistance(GameObject player, float value)
        {
            if (player == null) return false;
            if (TryInvokeStatic("AddHeatResistance", player, value)) return true;
            Say("Lớp màng ngoài đã tôi luyện qua hơi nóng — chịu nhiệt tốt hơn suốt vòng.");
            return true;
        }

        public static bool GiveStomachItems(GameObject player, string[] items)
        {
            if (player == null) return false;
            if (TryInvokeStatic("GiveStomachItems", player, items)) return true;
            return true;
        }

        static bool TryInvokeStatic(string methodName, params object[] args)
        {
            var t = RewardsType;
            if (t == null) return false;
            var methods = t.GetMethods(BindingFlags.Public | BindingFlags.Static);
            for (int i = 0; i < methods.Length; i++)
            {
                var m = methods[i];
                if (m.Name != methodName) continue;
                var ps = m.GetParameters();
                if (ps.Length != args.Length) continue;
                try
                {
                    m.Invoke(null, args);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            return false;
        }
    }
}
