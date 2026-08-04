using UnityEngine;

namespace ProdukLM
{
    // Sistem AI Tier (Free/Plus/Pro) sudah dihapus dari permainan. Kualitas
    // hasil kini murni berasal dari skor Affinity/Neutral/Conflict pada
    // StatsCalculator, dan limit harian memakai satu nilai umum di
    // ProjectFlowManager.

    public static class DailyGenerationCounter
    {
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
            // Reset limit mengikuti BeginNextDay pada waktu game, bukan tanggal
            // komputer. Ini mencegah limit tiba-tiba penuh/kosong setelah tidur,
            // ganti tanggal OS, atau kembali fokus ke Unity.
            return false;
        }

        public static void ResetForTesting()
        {
            PlayerPrefs.SetInt(DailyCountKey, 0);
            PlayerPrefs.Save();
        }

        public static void RestoreCount(int count)
        {
            PlayerPrefs.SetInt(DailyCountKey, Mathf.Max(0, count));
            PlayerPrefs.Save();
        }

        // Data permainan saat ini memang hanya berlaku selama sesi scene aktif.
        // Reset sekali ketika Play dimulai agar sisa pengujian sebelumnya tidak
        // dianggap sebagai pemakaian limit pada permainan baru.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetForNewPlaySession()
        {
            PlayerPrefs.SetInt(DailyCountKey, 0);
            PlayerPrefs.Save();
        }
    }
}
