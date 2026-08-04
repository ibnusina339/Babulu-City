using System;
using UnityEngine;

namespace LarisID
{
    /// <summary>
    /// Pengatur ramai-sepinya pembeli harian. Seluruh nilai dapat diubah dari
    /// Inspector pada LarisIDManager tanpa menyentuh rumus simulasi.
    /// </summary>
    [Serializable]
    public sealed class LarisMarketTuning
    {
        [Header("Frekuensi pembeli")]
        [Tooltip("Pengali jangkauan tayangan produk. Naikkan agar produk lebih sering dilihat.")]
        [Range(0.5f, 3f)] public float reachMultiplier = 1.2f;

        [Tooltip("Pengali peluang pengunjung membuka produk.")]
        [Range(0.5f, 3f)] public float clickMultiplier = 1.2f;

        [Tooltip("Pengali peluang pengunjung jadi membeli.")]
        [Range(0.5f, 3f)] public float conversionMultiplier = 1.35f;

        [Header("Variasi hari sepi dan ramai")]
        [Tooltip("Pengali penjualan pada hari paling sepi.")]
        [Range(0.1f, 1f)] public float quietDayMultiplier = 0.65f;

        [Tooltip("Pengali penjualan pada hari paling ramai.")]
        [Range(1f, 3f)] public float busyDayMultiplier = 1.45f;

        [Header("Batas penjualan harian seluruh toko")]
        [Tooltip("Batas untuk minggu pertama (hari 1-7).")]
        [Min(1)] public int salesCapWeek1 = 5;
        [Tooltip("Batas untuk hari 8-14.")]
        [Min(1)] public int salesCapWeek2 = 8;
        [Tooltip("Batas untuk hari 15-30.")]
        [Min(1)] public int salesCapMonth1 = 12;
        [Tooltip("Batas setelah hari ke-30.")]
        [Min(1)] public int salesCapLater = 16;

        /// <summary>
        /// Kapasitas berlaku untuk seluruh toko, bukan per produk. Batas ini
        /// yang menjaga agar tidak semua produk terjual habis setiap hari.
        /// </summary>
        public int GetDailyStoreSalesCap(int day)
        {
            if (day <= 7) return Mathf.Max(1, salesCapWeek1);
            if (day <= 14) return Mathf.Max(1, salesCapWeek2);
            if (day <= 30) return Mathf.Max(1, salesCapMonth1);
            return Mathf.Max(1, salesCapLater);
        }

        /// <summary>
        /// Suasana pasar per hari. Memakai seed harian sehingga hari yang sama
        /// selalu menghasilkan angka sama, tetapi tetap ada hari sepi dan ramai.
        /// </summary>
        public float GetDayMoodMultiplier(int day)
        {
            var random = new System.Random(5273 + Mathf.Max(1, day) * 613);
            float low = Mathf.Min(quietDayMultiplier, busyDayMultiplier);
            float high = Mathf.Max(quietDayMultiplier, busyDayMultiplier);
            return Mathf.Lerp(low, high, (float)random.NextDouble());
        }
    }
}
