using System;
using System.Globalization;
using UnityEngine;

namespace ProdukLM
{
    public enum AITier
    {
        Free,
        Plus,
        Pro
    }

    public static class AITierStats
    {
        public static StatsResult ApplyBoost(StatsResult source, float improvementPercent)
        {
            float boost = Mathf.Clamp01(improvementPercent);
            int quality = Improve(source.Quality, boost);
            int relevance = Improve(source.Relevansi, boost * 0.55f);
            int sellValue = Mathf.RoundToInt(quality * 0.60f + relevance * 0.40f);

            return new StatsResult
            {
                Quality = quality,
                Relevansi = relevance,
                NilaiJual = Mathf.Clamp(sellValue, 0, 100)
            };
        }

        static int Improve(int value, float percent)
        {
            int clamped = Mathf.Clamp(value, 0, 100);
            return Mathf.Clamp(
                clamped + Mathf.RoundToInt((100 - clamped) * percent),
                0,
                100);
        }
    }

    public static class DailyGenerationCounter
    {
        const string DailyDateKey = "ProdukLM.DailyProductDate";
        const string DailyCountKey = "ProdukLM.DailyProductCount";

        public static int Count
        {
            get
            {
                EnsureCurrent();
                return PlayerPrefs.GetInt(DailyCountKey, 0);
            }
        }

        public static int Remaining(int limit) =>
            Mathf.Max(0, Mathf.Max(1, limit) - Count);

        public static bool CanGenerate(int limit) => Remaining(limit) > 0;

        public static void Consume()
        {
            EnsureCurrent();
            PlayerPrefs.SetInt(DailyCountKey, Count + 1);
            PlayerPrefs.Save();
        }

        public static bool EnsureCurrent()
        {
            string today = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (PlayerPrefs.GetString(DailyDateKey, string.Empty) == today)
                return false;

            PlayerPrefs.SetString(DailyDateKey, today);
            PlayerPrefs.SetInt(DailyCountKey, 0);
            PlayerPrefs.Save();
            return true;
        }

        public static void ResetForTesting()
        {
            PlayerPrefs.SetInt(DailyCountKey, 0);
            PlayerPrefs.Save();
        }

        public static void RestoreCount(int count)
        {
            PlayerPrefs.SetString(
                DailyDateKey,
                DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            PlayerPrefs.SetInt(DailyCountKey, Mathf.Max(0, count));
            PlayerPrefs.Save();
        }

        // Data permainan saat ini memang hanya berlaku selama sesi scene aktif.
        // Reset sekali ketika Play dimulai agar sisa pengujian sebelumnya tidak
        // dianggap sebagai pemakaian limit pada permainan baru.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetForNewPlaySession()
        {
            PlayerPrefs.SetString(
                DailyDateKey,
                DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            PlayerPrefs.SetInt(DailyCountKey, 0);
            PlayerPrefs.Save();
        }
    }
}
