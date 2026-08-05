using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LarisID
{
    public static class PromotionOfferGenerator
    {
        /// <summary>Biaya promosi termurah yang boleh muncul.</summary>
        public const long MinimumCost = 50000;

        /// <summary>Biaya promosi termahal yang boleh muncul.</summary>
        public const long MaximumCost = 500000;

        // Rentang biaya mentah pada tabel Profiles di bawah. Dipakai untuk
        // memetakan harga lama ke rentang Rp50.000-Rp500.000 tanpa mengubah
        // urutan murah-mahal antar promotor.
        const float RawCheapest = 25000f;
        const float RawPriciest = 275000f;

        readonly struct Profile
        {
            public readonly string name;
            public readonly PromotionPlatform platform;
            public readonly long cost;
            public readonly int days;
            public readonly int boost;

            public Profile(
                string name,
                PromotionPlatform platform,
                long cost,
                int days,
                int boost)
            {
                this.name = name;
                this.platform = platform;
                this.cost = cost;
                this.days = days;
                this.boost = boost;
            }
        }

        static readonly Profile[] Profiles =
        {
            new("ululalbabinfo", PromotionPlatform.YouTube, 35000, 2, 24),
            new("laillahhturah", PromotionPlatform.YouTube, 50000, 3, 32),
            new("pejuangTKA", PromotionPlatform.YouTube, 85000, 3, 45),
            new("LolosPTNBersama", PromotionPlatform.YouTube, 145000, 4, 68),
            new("danauicturah", PromotionPlatform.YouTube, 45000, 2, 29),
            new("araakbarrumahlolosPTN", PromotionPlatform.YouTube, 190000, 4, 82),
            new("BCC (Babulu Course Center)", PromotionPlatform.YouTube, 275000, 5, 105),
            new("PT Besok Jadi", PromotionPlatform.YouTube, 110000, 3, 55),
            new("Gudang Wangsit", PromotionPlatform.YouTube, 75000, 3, 41),

            new("wifi tetangga", PromotionPlatform.Instagram, 25000, 2, 20),
            new("daruhfalahACservis", PromotionPlatform.Instagram, 40000, 2, 27),
            new("Babulugroup", PromotionPlatform.Instagram, 70000, 3, 39),
            new("adminbelummandi", PromotionPlatform.Instagram, 30000, 2, 23),
            new("KoneksiAsrama11Malam", PromotionPlatform.Instagram, 95000, 3, 48),
            new("CV Nanti Aja", PromotionPlatform.Instagram, 125000, 4, 62),
            new("Divisi Overthinking", PromotionPlatform.Instagram, 80000, 3, 44),
            new("NasigorengDept", PromotionPlatform.Instagram, 55000, 2, 34),
            new("Pantry Pusat", PromotionPlatform.Instagram, 65000, 3, 37),
            new("Rapat Gajelas", PromotionPlatform.Instagram, 45000, 2, 30),
            new("LapanganRKB", PromotionPlatform.Instagram, 60000, 3, 36),
            new("RUMDIN_GTK", PromotionPlatform.Instagram, 90000, 3, 49),
            new("Kemenag_id", PromotionPlatform.Instagram, 230000, 5, 96),

            new("Gabut Corporation", PromotionPlatform.TikTok, 70000, 2, 46),
            new("Kantor Mikir", PromotionPlatform.TikTok, 105000, 3, 59),
            new("Lab Ngasal", PromotionPlatform.TikTok, 55000, 2, 38),
            new("icsmadubersatu", PromotionPlatform.TikTok, 135000, 3, 72),
            new("Wifi-Gratis@PaserTuntas", PromotionPlatform.TikTok, 180000, 4, 88)
        };

        public static IReadOnlyList<PromoterOffer> Generate(int day)
        {
            var random = new System.Random(9187 + Mathf.Max(1, day) * 1291);
            List<Profile> shuffled = Profiles.OrderBy(_ => random.Next()).ToList();
            int count = 6 + Mathf.Abs(day * 7) % 3;
            var offers = new List<PromoterOffer>(count);

            for (int i = 0; i < count; i++)
            {
                Profile profile = shuffled[i];
                float costVariation = Mathf.Lerp(.92f, 1.10f, (float)random.NextDouble());
                int boostVariation = random.Next(-4, 6);
                offers.Add(new PromoterOffer
                {
                    id = $"DAY-{day:000}-{i:00}",
                    promoterName = profile.name,
                    platform = profile.platform,
                    cost = ToOfferCost(profile.cost * costVariation),
                    durationDays = profile.days,
                    viewBoostPercent = Mathf.Max(10, profile.boost + boostVariation)
                });
            }
            return offers;
        }

        /// <summary>
        /// Memetakan biaya mentah ke rentang Rp50.000-Rp500.000, dibulatkan ke
        /// kelipatan lima ribu, lalu dijepit supaya tidak pernah keluar rentang.
        /// </summary>
        static long ToOfferCost(float rawCost)
        {
            float position = Mathf.InverseLerp(RawCheapest, RawPriciest, rawCost);
            float scaled = Mathf.Lerp(MinimumCost, MaximumCost, position);
            long rounded = (long)Math.Round(scaled / 5000f) * 5000;
            return Math.Min(MaximumCost, Math.Max(MinimumCost, rounded));
        }
    }
}
