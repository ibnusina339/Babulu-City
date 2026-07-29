using UnityEngine;

namespace LarisID
{
    public static class LarisPricing
    {
        public static PriceRange GetRecommendedRange(
            LarisProduct product,
            StorePriceTier storeTier)
        {
            PriceRange marketBand = GetMarketBand(product, storeTier);
            int score = Mathf.RoundToInt(
                product.sellValue * .55f +
                product.quality * .25f +
                product.relevance * .20f);
            float position = Mathf.Lerp(.28f, .78f, Mathf.Clamp01(score / 100f));
            int ideal = RoundToThousand(
                Mathf.Lerp(marketBand.minimum, marketBand.maximum, position));
            return new PriceRange
            {
                minimum = marketBand.minimum,
                ideal = Mathf.Clamp(ideal, marketBand.minimum, marketBand.maximum),
                maximum = marketBand.maximum
            };
        }

        public static PriceRange GetRecommendedRange(LarisProduct product) =>
            GetRecommendedRange(product, StorePriceTier.Pemula);

        public static PriceRange GetMarketBand(
            LarisProduct product,
            StorePriceTier storeTier)
        {
            int[] values = ResolveBands(product);
            int offset = (int)storeTier * 2;
            return new PriceRange
            {
                minimum = values[offset],
                ideal = RoundToThousand((values[offset] + values[offset + 1]) * .5f),
                maximum = values[offset + 1]
            };
        }

        public static float GetPurchaseMultiplier(
            LarisProduct product,
            StorePriceTier storeTier)
        {
            PriceRange range = GetRecommendedRange(product, storeTier);
            if (range.ideal <= 0)
                return 1f;

            float ratio = (float)product.price / range.ideal;
            if (ratio <= .65f) return 1.25f;
            if (ratio <= 1f) return Mathf.Lerp(1.25f, 1.05f, (ratio - .65f) / .35f);
            if (ratio <= 1.35f) return Mathf.Lerp(1.05f, .65f, (ratio - 1f) / .35f);
            if (ratio <= 2f) return Mathf.Lerp(.65f, .15f, (ratio - 1.35f) / .65f);
            return .08f;
        }

        public static string GetPriceAssessment(
            LarisProduct product,
            StorePriceTier storeTier)
        {
            PriceRange range = GetRecommendedRange(product, storeTier);
            if (product.price < range.minimum) return "Di bawah pasar toko";
            if (product.price > range.maximum) return "Belum terbuka untuk level toko";
            if (product.price > range.ideal * 1.20f) return "Agak mahal";
            return "Harga ideal";
        }

        static int[] ResolveBands(LarisProduct product)
        {
            string name = product.productName?.ToLowerInvariant() ?? string.Empty;
            if (name.Contains("planner"))
                return new[] { 15000, 30000, 35000, 75000, 100000, 250000 };
            if (product.category == ProductCategory.Ebook)
                return new[] { 20000, 50000, 50000, 100000, 100000, 300000 };
            if (name.Contains("infograf") || product.category == ProductCategory.SocialMedia)
                return new[] { 15000, 40000, 40000, 80000, 100000, 200000 };
            if (name.Contains("modul") ||
                product.category == ProductCategory.Education ||
                product.category == ProductCategory.BusinessPlan)
                return new[] { 30000, 75000, 75000, 150000, 150000, 400000 };
            if (product.category == ProductCategory.Presentation)
                return new[] { 20000, 50000, 50000, 120000, 120000, 300000 };
            if (product.category == ProductCategory.Template)
                return new[] { 10000, 30000, 30000, 70000, 70000, 150000 };
            return new[] { 15000, 40000, 40000, 90000, 90000, 220000 };
        }

        static int RoundToThousand(float value) =>
            Mathf.Max(1000, Mathf.RoundToInt(value / 1000f) * 1000);
    }
}
