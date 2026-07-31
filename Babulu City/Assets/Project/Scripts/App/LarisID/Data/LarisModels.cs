using System;
using System.Collections.Generic;

namespace LarisID
{
    public enum ProductStatus
    {
        Draft,
        Active,
        Archived
    }

    public enum ProductCategory
    {
        Template,
        Ebook,
        Presentation,
        BusinessPlan,
        Education,
        SocialMedia,
        Other
    }

    public enum TargetMarket
    {
        Students,
        Educators,
        ContentCreators,
        Professionals,
        Businesses,
        General
    }

    public enum StorePriceTier
    {
        Pemula,
        Berkembang,
        Terkenal
    }

    public enum PromotionPlatform
    {
        YouTube,
        Instagram,
        TikTok
    }

    [Serializable]
    public class PromoterOffer
    {
        public string id;
        public string promoterName;
        public PromotionPlatform platform;
        public long cost;
        public int durationDays;
        public int viewBoostPercent;
    }

    [Serializable]
    public class LarisReview
    {
        public int day;
        public int rating;
        public string text;
    }

    [Serializable]
    public class LarisProduct
    {
        public string id;
        public string productName;
        public string iconKey;
        public ProductCategory category;
        public int price;
        public string description;
        public TargetMarket targetMarket;
        public ProductStatus status;

        public int quality;
        public int relevance;
        public int aesthetic;
        public int creativity;
        public int professionalism;
        public int sellValue;

        public int impressions;
        public int clicks;
        public int sales;
        public long revenue;
        public float rating;
        public int reviewCount;
        public int promotionDaysRemaining;
        public string activePromoterName;
        public PromotionPlatform activePromotionPlatform;
        public int promotionViewBoostPercent;
        public long promotionCostPaid;
        public List<LarisReview> reviews = new();

        public float ConversionRate => clicks > 0 ? (float)sales / clicks * 100f : 0f;
        public bool IsPromoted => promotionDaysRemaining > 0;
    }

    [Serializable]
    public struct PriceRange
    {
        public int minimum;
        public int ideal;
        public int maximum;
    }

    [Serializable]
    public class ProductDayResult
    {
        public string productId;
        public string productName;
        public int newImpressions;
        public int newClicks;
        public int newSales;
        public long newRevenue;
        public int newFollowers;
        public float previousRating;
        public float currentRating;
    }

    [Serializable]
    public class DailyMarketResult
    {
        public int day;
        public int newImpressions;
        public int newClicks;
        public int newSales;
        public long newRevenue;
        public int newFollowers;
        public float previousStoreRating;
        public float currentStoreRating;
        public List<ProductDayResult> productResults = new();
    }

    [Serializable]
    public class ShopAnalytics
    {
        public int totalImpressions;
        public int totalClicks;
        public int totalSales;
        public long totalRevenue;
        public float averageConversion;
        public string bestSeller;
        public string highestRated;
        public ProductCategory mostProfitableCategory;
    }

    // DTO integrasi. ProdukLM nanti cukup membuat object ini tanpa menyentuh UI Laris.ID.
    [Serializable]
    public class LarisProductImportData
    {
        public string productName;
        public string iconKey;
        public ProductCategory category;
        public string description;
        public TargetMarket targetMarket;
        public int quality;
        public int relevance;
        public int aesthetic;
        public int creativity;
        public int professionalism;
        public int sellValue;
    }
}
