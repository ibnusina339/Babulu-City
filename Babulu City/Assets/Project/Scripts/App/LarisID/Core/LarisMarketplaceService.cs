using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LarisID
{
    public class LarisMarketplaceService
    {
        public const long InitialBalance = 150000;

        public string ShopName { get; set; } = "Toko BRIDA";
        public string ShopDescription { get; set; } =
            "Produk digital untuk belajar, bekerja, dan berkarya.";
        public long Balance { get; private set; }
        public int Followers { get; private set; }
        public int CurrentDay { get; private set; }
        public ProductCategory ActiveTrend { get; private set; }
        public IReadOnlyList<LarisProduct> Products => products;
        public IReadOnlyList<DailyMarketResult> DailyHistory => dailyHistory;
        public DailyMarketResult LastDailyResult { get; private set; }
        public int GrowingTierRequirement { get; }
        public int FamousTierRequirement { get; }
        public StorePriceTier StoreTier { get; private set; }

        /// <summary>
        /// Pengatur ramai-sepinya pembeli. Diisi LarisIDManager dari Inspector;
        /// bila kosong dipakai nilai default agar service tetap bisa dites sendiri.
        /// </summary>
        public LarisMarketTuning Tuning { get; set; } = new LarisMarketTuning();

        readonly List<LarisProduct> products = new();
        readonly List<DailyMarketResult> dailyHistory = new();
        int nextProductId;
        int dummyIndex;

        public int ActiveProductCount => products.Count(p => p.status == ProductStatus.Active);
        public int TotalSales => products.Sum(p => p.sales);
        public long TotalRevenue => products.Sum(p => p.revenue);
        public int TotalReviews => products.Sum(p => p.reviewCount);
        public int ReputationPoints =>
            Followers +
            TotalSales * 4 +
            TotalReviews * 2 +
            Mathf.RoundToInt(Mathf.Max(0f, StoreRating - 3f) * 20f);
        public int NextTierRequirement => StoreTier switch
        {
            StorePriceTier.Pemula => GrowingTierRequirement,
            StorePriceTier.Berkembang => FamousTierRequirement,
            _ => FamousTierRequirement
        };
        public float StoreRating
        {
            get
            {
                int reviews = products.Sum(p => p.reviewCount);
                if (reviews == 0)
                    return 0f;
                return products.Sum(p => p.rating * p.reviewCount) / reviews;
            }
        }

        public LarisMarketplaceService(
            int growingTierRequirement = 120,
            int famousTierRequirement = 500)
        {
            GrowingTierRequirement = Mathf.Max(1, growingTierRequirement);
            FamousTierRequirement = Mathf.Max(
                GrowingTierRequirement + 1,
                famousTierRequirement);
            Reset();
        }

        public void Reset()
        {
            products.Clear();
            dailyHistory.Clear();
            Balance = InitialBalance;
            Followers = 0;
            CurrentDay = 1;
            StoreTier = StorePriceTier.Pemula;
            ActiveTrend = PickTrendForDay(CurrentDay);
            LastDailyResult = null;
            nextProductId = 1;
            dummyIndex = 0;
            ShopName = "Toko BRIDA";
            ShopDescription = "Produk digital untuk belajar, bekerja, dan berkarya.";
        }

        public LarisProduct AddDummyProduct()
        {
            var presets = new[]
            {
                new LarisProductImportData
                {
                    productName = "Planner Belajar Harian",
                    category = ProductCategory.Template,
                    description = "Template planner sederhana untuk membantu pelajar mengatur kegiatan.",
                    targetMarket = TargetMarket.Students,
                    quality = 72, relevance = 84, aesthetic = 76,
                    creativity = 61, professionalism = 58, sellValue = 70
                },
                new LarisProductImportData
                {
                    productName = "Ebook Strategi Konten",
                    category = ProductCategory.Ebook,
                    description = "Panduan praktis membuat strategi konten media sosial.",
                    targetMarket = TargetMarket.ContentCreators,
                    quality = 81, relevance = 78, aesthetic = 65,
                    creativity = 75, professionalism = 73, sellValue = 80
                },
                new LarisProductImportData
                {
                    productName = "Pitch Deck Startup",
                    category = ProductCategory.Presentation,
                    description = "Template presentasi profesional untuk pitch bisnis.",
                    targetMarket = TargetMarket.Businesses,
                    quality = 88, relevance = 85, aesthetic = 90,
                    creativity = 70, professionalism = 94, sellValue = 89
                },
                new LarisProductImportData
                {
                    productName = "Paket Cerita Rakyat Anak",
                    category = ProductCategory.Education,
                    description = "Materi cerita rakyat interaktif untuk kegiatan belajar.",
                    targetMarket = TargetMarket.Educators,
                    quality = 74, relevance = 91, aesthetic = 69,
                    creativity = 86, professionalism = 62, sellValue = 72
                },
                new LarisProductImportData
                {
                    productName = "Kalender Konten UMKM",
                    category = ProductCategory.SocialMedia,
                    description = "Ide dan jadwal konten promosi selama satu bulan.",
                    targetMarket = TargetMarket.Businesses,
                    quality = 67, relevance = 76, aesthetic = 71,
                    creativity = 64, professionalism = 78, sellValue = 73
                },
                new LarisProductImportData
                {
                    productName = "Business Plan Ringkas",
                    category = ProductCategory.BusinessPlan,
                    description = "Dokumen rencana bisnis siap isi untuk usaha baru.",
                    targetMarket = TargetMarket.Professionals,
                    quality = 83, relevance = 82, aesthetic = 55,
                    creativity = 52, professionalism = 92, sellValue = 85
                }
            };

            LarisProduct product = AddProductFromImport(presets[dummyIndex % presets.Length]);
            dummyIndex++;
            return product;
        }

        public LarisProduct AddProductFromImport(LarisProductImportData data)
        {
            var product = new LarisProduct
            {
                id = $"LR-{nextProductId++:0000}",
                productName = data.productName,
                iconKey = data.iconKey,
                category = data.category,
                description = data.description,
                targetMarket = data.targetMarket,
                status = ProductStatus.Draft,
                quality = ClampStat(data.quality),
                relevance = ClampStat(data.relevance),
                aesthetic = ClampStat(data.aesthetic),
                creativity = ClampStat(data.creativity),
                professionalism = ClampStat(data.professionalism),
                sellValue = ClampStat(data.sellValue),
                rating = 0f
            };

            product.price = LarisPricing.GetRecommendedRange(product, StoreTier).ideal;
            products.Add(product);
            return product;
        }

        public bool Publish(LarisProduct product, out string message)
        {
            if (product == null)
            {
                message = "Produk tidak ditemukan.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(product.productName))
            {
                message = "Nama produk wajib diisi.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(product.description))
            {
                message = "Deskripsi produk wajib diisi.";
                return false;
            }
            if (product.price <= 0)
            {
                message = "Harga produk harus lebih dari nol.";
                return false;
            }
            PriceRange unlockedRange = LarisPricing.GetMarketBand(product, StoreTier);
            if (product.price < unlockedRange.minimum || product.price > unlockedRange.maximum)
            {
                message =
                    $"Harga untuk toko {StoreTier} harus Rp {unlockedRange.minimum:N0}–" +
                    $"Rp {unlockedRange.maximum:N0}.";
                return false;
            }

            product.status = ProductStatus.Active;
            message = "Produk berhasil dipublikasikan.";
            return true;
        }

        public bool SetPrice(LarisProduct product, int requestedPrice, out string message)
        {
            if (product == null)
            {
                message = "Pilih produk terlebih dahulu.";
                return false;
            }

            PriceRange unlockedRange = LarisPricing.GetMarketBand(product, StoreTier);
            int applied = Mathf.Clamp(
                RoundToThousand(requestedPrice),
                unlockedRange.minimum,
                unlockedRange.maximum);
            product.price = applied;
            if (applied != requestedPrice)
            {
                message =
                    $"Harga dibatasi level toko {StoreTier}: Rp {unlockedRange.minimum:N0}–" +
                    $"Rp {unlockedRange.maximum:N0}.";
                return false;
            }

            message = $"Harga diatur ke Rp {applied:N0}.";
            return true;
        }

        public IReadOnlyList<PromoterOffer> GetDailyPromotionOffers() =>
            PromotionOfferGenerator.Generate(CurrentDay);

        public bool Promote(
            LarisProduct product,
            PromoterOffer offer,
            out string message)
        {
            if (product == null || product.status != ProductStatus.Active)
            {
                message = "Hanya produk aktif yang dapat dipromosikan.";
                return false;
            }
            if (offer == null)
            {
                message = "Pilih promotor terlebih dahulu.";
                return false;
            }
            if (product.IsPromoted)
            {
                message = $"Promosi masih aktif selama {product.promotionDaysRemaining} hari.";
                return false;
            }
            if (Balance < offer.cost)
            {
                message = $"Saldo tidak cukup. Biaya promosi Rp {offer.cost:N0}.";
                return false;
            }

            Balance -= offer.cost;
            product.promotionDaysRemaining = offer.durationDays;
            product.activePromoterName = offer.promoterName;
            product.activePromotionPlatform = offer.platform;
            product.promotionViewBoostPercent = offer.viewBoostPercent;
            product.promotionCostPaid = offer.cost;
            message =
                $"{offer.promoterName} ({offer.platform}) mempromosikan {product.productName} " +
                $"selama {offer.durationDays} hari. Estimasi tayangan +{offer.viewBoostPercent}%.";
            return true;
        }

        public bool TrySpend(long amount, out string message)
        {
            if (amount <= 0)
            {
                message = "Nominal pembayaran tidak valid.";
                return false;
            }
            if (Balance < amount)
            {
                message = $"Saldo tidak cukup. Dibutuhkan Rp {amount:N0}.";
                return false;
            }

            Balance -= amount;
            message = $"Pembayaran Rp {amount:N0} berhasil.";
            return true;
        }

        public void Archive(LarisProduct product)
        {
            if (product != null)
                product.status = ProductStatus.Archived;
        }

        public void Reactivate(LarisProduct product)
        {
            if (product != null)
                product.status = ProductStatus.Active;
        }

        public bool DeleteProduct(LarisProduct product)
        {
            return product != null && products.Remove(product);
        }

        public void CycleTrend()
        {
            ProductCategory[] categories = (ProductCategory[])Enum.GetValues(typeof(ProductCategory));
            int next = (Array.IndexOf(categories, ActiveTrend) + 1) % categories.Length;
            ActiveTrend = categories[next];
        }

        public DailyMarketResult SimulateOneDay()
        {
            int simulatedDay = CurrentDay;
            float previousRating = StoreRating;
            var result = new DailyMarketResult
            {
                day = simulatedDay,
                activeTrend = ActiveTrend,
                previousStoreRating = previousRating
            };

            var random = new System.Random(7391 + simulatedDay * 997 + products.Count * 37);
            LarisMarketTuning tuning = Tuning ??= new LarisMarketTuning();
            int remainingStoreSales = tuning.GetDailyStoreSalesCap(simulatedDay);
            foreach (LarisProduct product in products)
            {
                ProductDayResult productResult = LarisMarketSimulator.SimulateProduct(
                    product,
                    simulatedDay,
                    Followers,
                    StoreRating,
                    ActiveTrend,
                    StoreTier,
                    remainingStoreSales,
                    random,
                    tuning);

                if (product.status != ProductStatus.Active)
                    continue;

                result.productResults.Add(productResult);
                result.newImpressions += productResult.newImpressions;
                result.newClicks += productResult.newClicks;
                result.newSales += productResult.newSales;
                result.newRevenue += productResult.newRevenue;
                result.newFollowers += productResult.newFollowers;
                remainingStoreSales = Mathf.Max(
                    0,
                    remainingStoreSales - productResult.newSales);
            }

            Followers += result.newFollowers;
            Balance += result.newRevenue;
            UpdateStoreTier();
            result.currentStoreRating = StoreRating;
            LastDailyResult = result;
            dailyHistory.Add(result);

            CurrentDay++;
            ActiveTrend = PickTrendForDay(CurrentDay);

            return result;
        }

        public ShopAnalytics GetAnalytics()
        {
            var analytics = new ShopAnalytics
            {
                totalImpressions = products.Sum(p => p.impressions),
                totalClicks = products.Sum(p => p.clicks),
                totalSales = TotalSales,
                totalRevenue = TotalRevenue,
                averageConversion = products.Count > 0
                    ? products.Average(p => p.ConversionRate)
                    : 0f,
                bestSeller = products
                    .OrderByDescending(p => p.sales)
                    .Select(p => p.productName)
                    .FirstOrDefault() ?? "-",
                highestRated = products
                    .Where(p => p.reviewCount > 0)
                    .OrderByDescending(p => p.rating)
                    .Select(p => p.productName)
                    .FirstOrDefault() ?? "-"
            };

            analytics.mostProfitableCategory = products
                .GroupBy(p => p.category)
                .OrderByDescending(g => g.Sum(p => p.revenue))
                .Select(g => g.Key)
                .FirstOrDefault();
            return analytics;
        }

        public LarisProduct GetBestSeller() =>
            products.OrderByDescending(p => p.sales).FirstOrDefault();

        public LarisMarketplaceSaveData CaptureSaveData()
        {
            return new LarisMarketplaceSaveData
            {
                shopName = ShopName,
                shopDescription = ShopDescription,
                balance = Balance,
                followers = Followers,
                currentDay = CurrentDay,
                activeTrend = ActiveTrend,
                storeTier = StoreTier,
                nextProductId = nextProductId,
                dummyIndex = dummyIndex,
                products = products.ToList(),
                dailyHistory = dailyHistory.ToList()
            };
        }

        public void RestoreSaveData(LarisMarketplaceSaveData data)
        {
            if (data == null)
                return;

            ShopName = string.IsNullOrWhiteSpace(data.shopName) ? "Toko BRIDA" : data.shopName;
            ShopDescription = data.shopDescription ?? string.Empty;
            Balance = Math.Max(0L, data.balance);
            Followers = Mathf.Max(0, data.followers);
            CurrentDay = Mathf.Max(1, data.currentDay);
            ActiveTrend = data.activeTrend;
            StoreTier = data.storeTier;
            nextProductId = Mathf.Max(1, data.nextProductId);
            dummyIndex = Mathf.Max(0, data.dummyIndex);

            products.Clear();
            if (data.products != null)
                products.AddRange(data.products.Where(product => product != null));
            dailyHistory.Clear();
            if (data.dailyHistory != null)
                dailyHistory.AddRange(data.dailyHistory.Where(result => result != null));
            LastDailyResult = dailyHistory.LastOrDefault();

            // Save lama mungkin belum menyimpan counter ID.
            if (products.Count >= nextProductId)
                nextProductId = products.Count + 1;
        }

        void UpdateStoreTier()
        {
            if (StoreTier < StorePriceTier.Berkembang &&
                ReputationPoints >= GrowingTierRequirement)
                StoreTier = StorePriceTier.Berkembang;
            if (StoreTier < StorePriceTier.Terkenal &&
                ReputationPoints >= FamousTierRequirement)
                StoreTier = StorePriceTier.Terkenal;
        }

        static int RoundToThousand(int value) =>
            Mathf.Max(1000, Mathf.RoundToInt(value / 1000f) * 1000);

        static int ClampStat(int value) => Mathf.Clamp(value, 0, 100);

        static ProductCategory PickTrendForDay(int day)
        {
            ProductCategory[] categories =
                (ProductCategory[])Enum.GetValues(typeof(ProductCategory));
            var random = new System.Random(4111 + Mathf.Max(1, day) * 1877);
            return categories[random.Next(0, categories.Length)];
        }
    }
}
