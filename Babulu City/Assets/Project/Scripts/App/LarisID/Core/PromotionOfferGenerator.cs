using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LarisID
{
    public static class PromotionOfferGenerator
    {
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
            new("Raka Belajar", PromotionPlatform.YouTube, 45000, 3, 35),
            new("Tech Santai", PromotionPlatform.YouTube, 90000, 4, 58),
            new("Bisnis Bareng", PromotionPlatform.YouTube, 180000, 5, 92),
            new("Kelas Digital TV", PromotionPlatform.YouTube, 360000, 6, 145),
            new("Creator Kampus", PromotionPlatform.YouTube, 70000, 3, 48),
            new("Studio Produktif", PromotionPlatform.YouTube, 240000, 5, 115),

            new("KreasiNala", PromotionPlatform.Instagram, 30000, 2, 27),
            new("VisualKita", PromotionPlatform.Instagram, 65000, 3, 45),
            new("UMKM Naik", PromotionPlatform.Instagram, 125000, 4, 72),
            new("Ruang Desain ID", PromotionPlatform.Instagram, 280000, 5, 125),
            new("Belajar Bareng", PromotionPlatform.Instagram, 85000, 3, 55),
            new("Daily Template", PromotionPlatform.Instagram, 190000, 4, 95)
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
                    cost = RoundToFiveThousand(profile.cost * costVariation),
                    durationDays = profile.days,
                    viewBoostPercent = Mathf.Max(10, profile.boost + boostVariation)
                });
            }
            return offers;
        }

        static long RoundToFiveThousand(float value) =>
            Math.Max(5000, (long)Math.Round(value / 5000f) * 5000);
    }
}
