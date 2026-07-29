using System;
using UnityEngine;

namespace LarisID
{
    public static class LarisMarketSimulator
    {
        public static ProductDayResult SimulateProduct(
            LarisProduct product,
            int day,
            int followers,
            float storeRating,
            ProductCategory activeTrend,
            StorePriceTier storeTier,
            int maximumSales,
            System.Random random)
        {
            var result = new ProductDayResult
            {
                productId = product.id,
                productName = product.productName,
                previousRating = product.rating
            };

            if (product.status != ProductStatus.Active)
                return result;

            float baseReach =
                20f +
                Mathf.Min(150f, followers * 0.08f) +
                product.quality * 0.25f +
                product.creativity * 0.30f +
                Mathf.Max(0f, storeRating - 3f) * 12f;

            if (product.category == activeTrend)
                baseReach *= 1.55f;
            if (product.IsPromoted)
                baseReach *= 1f + Mathf.Clamp(
                    product.promotionViewBoostPercent,
                    10,
                    300) / 100f;

            baseReach *= Mathf.Lerp(0.82f, 1.18f, (float)random.NextDouble());
            int impressions = Mathf.Max(0, Mathf.RoundToInt(baseReach));

            float clickChance =
                0.04f +
                product.aesthetic / 100f * 0.20f +
                product.creativity / 100f * 0.08f;
            clickChance = Mathf.Clamp(clickChance, 0.03f, 0.38f);
            int clicks = RollCount(impressions, clickChance, random);

            float conversionChance =
                0.015f +
                product.relevance / 100f * 0.10f +
                product.quality / 100f * 0.07f +
                product.professionalism / 100f * 0.04f;
            conversionChance *= LarisPricing.GetPurchaseMultiplier(product, storeTier);
            if (product.category == activeTrend)
                conversionChance *= 1.08f;
            conversionChance = Mathf.Clamp(conversionChance, 0.005f, 0.35f);

            int sales = Mathf.Min(
                Mathf.Max(0, maximumSales),
                RollCount(clicks, conversionChance, random));
            long revenue = (long)sales * product.price;
            int followersGained = 0;

            for (int i = 0; i < sales; i++)
            {
                if (random.NextDouble() <= 0.48)
                {
                    int rating = GenerateRating(product, storeTier, random);
                    product.rating =
                        (product.rating * product.reviewCount + rating) /
                        (product.reviewCount + 1);
                    product.reviewCount++;
                    product.reviews.Add(new LarisReview
                    {
                        day = day,
                        rating = rating,
                        text = LarisReviewGenerator.Generate(product, rating, random)
                    });
                }

                float followChance =
                    0.03f +
                    product.quality / 100f * 0.08f +
                    product.rating / 5f * 0.08f;
                if (random.NextDouble() <= followChance)
                    followersGained++;
            }

            product.impressions += impressions;
            product.clicks += clicks;
            product.sales += sales;
            product.revenue += revenue;
            if (product.promotionDaysRemaining > 0)
                product.promotionDaysRemaining--;

            result.newImpressions = impressions;
            result.newClicks = clicks;
            result.newSales = sales;
            result.newRevenue = revenue;
            result.newFollowers = followersGained;
            result.currentRating = product.rating;
            return result;
        }

        static int GenerateRating(
            LarisProduct product,
            StorePriceTier storeTier,
            System.Random random)
        {
            float score =
                2.15f +
                product.quality / 100f * 1.45f +
                product.relevance / 100f * 0.75f +
                product.professionalism / 100f * 0.55f;

            if (LarisPricing.GetPriceAssessment(product, storeTier) == "Agak mahal")
                score -= 0.45f;

            score += Mathf.Lerp(-0.45f, 0.45f, (float)random.NextDouble());
            return Mathf.Clamp(Mathf.RoundToInt(score), 1, 5);
        }

        static int RollCount(int attempts, float chance, System.Random random)
        {
            int successes = 0;
            for (int i = 0; i < attempts; i++)
            {
                if (random.NextDouble() <= chance)
                    successes++;
            }
            return successes;
        }
    }
}
