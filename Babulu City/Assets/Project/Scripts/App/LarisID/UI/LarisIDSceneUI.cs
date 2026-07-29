using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LarisID
{
    public class LarisIDSceneUI : MonoBehaviour
    {
        enum Page
        {
            Dashboard,
            Products,
            Detail,
            Promotion,
            Analytics,
            DailySummary
        }

        [Header("System")]
        public LarisIDManager manager;

        [Header("Pages")]
        public GameObject dashboardPage;
        public GameObject productsPage;
        public GameObject detailPage;
        public GameObject promotionPage;
        public GameObject analyticsPage;
        public GameObject dailySummaryPage;

        [Header("Global navigation")]
        public Button dashboardButton;
        public Button productsButton;
        public Button promotionButton;
        public Button analyticsButton;
        public Button dailySummaryButton;
        public Button simulateDayButton;
        public Button cycleTrendButton;
        public Button resetButton;
        public TMP_Text dayTrendText;
        public TMP_Text balanceHeaderText;
        public TMP_Text statusMessageText;

        [Header("Dashboard")]
        public TMP_InputField shopNameInput;
        public TMP_Text dashboardBalanceText;
        public TMP_Text dashboardActiveProductsText;
        public TMP_Text dashboardFollowersText;
        public TMP_Text dashboardSalesText;
        public TMP_Text dashboardRevenueText;
        public TMP_Text dashboardRatingText;
        public TMP_Text dashboardDayText;
        public TMP_Text dashboardTrendText;
        public TMP_Text dashboardSummaryText;
        public Button dashboardAddDummyButton;

        [Header("Product list")]
        public Transform productListContent;
        public LarisProductRowUI productRowTemplate;
        public Button productsAddDummyButton;
        public TMP_Text productCountText;

        [Header("Product detail - form")]
        public TMP_Text detailIdText;
        public TMP_Text detailStatusText;
        public TMP_InputField detailNameInput;
        public TMP_InputField detailDescriptionInput;
        public TMP_InputField detailPriceInput;
        public Button priceMinusButton;
        public Button pricePlusButton;
        public TMP_Text detailCategoryText;
        public TMP_Text detailTargetText;
        public TMP_Text recommendedPriceText;
        public Button categoryPreviousButton;
        public Button categoryNextButton;
        public Button targetPreviousButton;
        public Button targetNextButton;
        public Button detailBackButton;
        public Button publishButton;
        public Button promoteButton;
        public TMP_Text promoteButtonText;
        public Button archiveButton;
        public Button reactivateButton;

        [Header("Product detail - stats")]
        public TMP_Text productStatsText;
        public TMP_Text marketStatsText;
        public Transform reviewContent;
        public TMP_Text reviewRowTemplate;

        [Header("Promotion")]
        public Button promotionProductPreviousButton;
        public Button promotionProductNextButton;
        public TMP_Text promotionSelectedProductText;
        public TMP_Text promotionProductStatusText;
        public Transform promotionOfferContent;
        public PromotionOfferRowUI promotionOfferRowTemplate;
        public TMP_Text promotionOfferCountText;
        public TMP_Text promotionSelectedOfferText;
        public TMP_Text promotionEmptyText;
        public Button confirmPromotionButton;

        [Header("Analytics")]
        public TMP_Text analyticsText;

        [Header("Daily summary")]
        public TMP_Text dailySummaryTitleText;
        public TMP_Text dailySummaryTotalsText;
        public Transform dailyProductResultContent;
        public TMP_Text dailyProductResultTemplate;

        Page currentPage;
        bool settingDetailFields;
        LarisProduct promotionProduct;
        int selectedOfferIndex;

        void Awake()
        {
            if (manager == null)
                manager = GetComponent<LarisIDManager>();

            dashboardButton.onClick.AddListener(() => ShowPage(Page.Dashboard));
            productsButton.onClick.AddListener(() => ShowPage(Page.Products));
            if (promotionButton != null)
                promotionButton.onClick.AddListener(OpenPromotionPage);
            analyticsButton.onClick.AddListener(() => ShowPage(Page.Analytics));
            dailySummaryButton.onClick.AddListener(() => ShowPage(Page.DailySummary));
            simulateDayButton.onClick.AddListener(SimulateDay);
            cycleTrendButton.onClick.AddListener(CycleTrend);
            resetButton.onClick.AddListener(ResetAll);
            dashboardAddDummyButton.onClick.AddListener(AddDummy);
            productsAddDummyButton.onClick.AddListener(AddDummy);
            detailBackButton.onClick.AddListener(() => ShowPage(Page.Products));
            publishButton.onClick.AddListener(() => manager.PublishSelected());
            promoteButton.onClick.AddListener(OpenPromotionPage);
            archiveButton.onClick.AddListener(() => manager.ArchiveSelected());
            reactivateButton.onClick.AddListener(() => manager.ReactivateSelected());
            categoryPreviousButton.onClick.AddListener(() => ChangeCategory(-1));
            categoryNextButton.onClick.AddListener(() => ChangeCategory(1));
            targetPreviousButton.onClick.AddListener(() => ChangeTarget(-1));
            targetNextButton.onClick.AddListener(() => ChangeTarget(1));
            if (priceMinusButton != null)
                priceMinusButton.onClick.AddListener(() => ChangePrice(-1000));
            if (pricePlusButton != null)
                pricePlusButton.onClick.AddListener(() => ChangePrice(1000));
            if (promotionProductPreviousButton != null)
                promotionProductPreviousButton.onClick.AddListener(() => ChangePromotionProduct(-1));
            if (promotionProductNextButton != null)
                promotionProductNextButton.onClick.AddListener(() => ChangePromotionProduct(1));
            if (confirmPromotionButton != null)
                confirmPromotionButton.onClick.AddListener(ConfirmPromotion);

            shopNameInput.onEndEdit.AddListener(value =>
            {
                manager.Marketplace.ShopName = value;
                manager.NotifyProductEdited();
            });
            detailNameInput.onEndEdit.AddListener(value =>
            {
                if (manager.SelectedProduct == null || settingDetailFields) return;
                manager.SelectedProduct.productName = value;
                manager.NotifyProductEdited();
            });
            detailDescriptionInput.onEndEdit.AddListener(value =>
            {
                if (manager.SelectedProduct == null || settingDetailFields) return;
                manager.SelectedProduct.description = value;
                manager.NotifyProductEdited();
            });
            detailPriceInput.onEndEdit.AddListener(value =>
            {
                if (manager.SelectedProduct == null || settingDetailFields) return;
                if (int.TryParse(value, out int price))
                    manager.SetSelectedProductPrice(price);
            });
        }

        void Start()
        {
            manager.StateChanged += RefreshAll;
            ShowPage(Page.Dashboard);
        }

        void OnDestroy()
        {
            if (manager != null)
                manager.StateChanged -= RefreshAll;
        }

        void ShowPage(Page page)
        {
            currentPage = page;
            dashboardPage.SetActive(page == Page.Dashboard);
            productsPage.SetActive(page == Page.Products);
            detailPage.SetActive(page == Page.Detail);
            if (promotionPage != null)
                promotionPage.SetActive(page == Page.Promotion);
            analyticsPage.SetActive(page == Page.Analytics);
            dailySummaryPage.SetActive(page == Page.DailySummary);
            RefreshAll();
        }

        void RefreshAll()
        {
            RefreshHeader();
            RefreshDashboard();
            RefreshProductList();
            RefreshDetail();
            RefreshPromotion();
            RefreshAnalytics();
            RefreshDailySummary();
        }

        void RefreshHeader()
        {
            var shop = manager.Marketplace;
            dayTrendText.text =
                $"Hari {shop.CurrentDay}  •  Toko {shop.StoreTier}  •  Tren: {shop.ActiveTrend}";
            balanceHeaderText.text = $"Saldo  Rp {shop.Balance:N0}";
            statusMessageText.text = manager.LastMessage;
        }

        void RefreshDashboard()
        {
            var shop = manager.Marketplace;
            if (!shopNameInput.isFocused)
                shopNameInput.SetTextWithoutNotify(shop.ShopName);
            dashboardBalanceText.text = $"Rp {shop.Balance:N0}";
            dashboardActiveProductsText.text = shop.ActiveProductCount.ToString("N0");
            dashboardFollowersText.text = shop.Followers.ToString("N0");
            dashboardSalesText.text = shop.TotalSales.ToString("N0");
            dashboardRevenueText.text = $"Rp {shop.TotalRevenue:N0}";
            dashboardRatingText.text = shop.StoreRating > 0 ? $"{shop.StoreRating:0.00}/5" : "-";
            dashboardDayText.text = shop.CurrentDay.ToString();
            dashboardTrendText.text = shop.ActiveTrend.ToString();

            LarisProduct best = shop.GetBestSeller();
            dashboardSummaryText.text =
                $"LEVEL HARGA: {shop.StoreTier}  •  Reputasi {shop.ReputationPoints:N0}" +
                $"{(shop.StoreTier != StorePriceTier.Terkenal ? $"/{shop.NextTierRequirement:N0}" : " (MAX)")}\n" +
                $"Produk terlaris: {(best != null ? best.productName : "-")}\n" +
                $"Total produk: {shop.Products.Count}\n" +
                $"Draft: {shop.Products.Count(p => p.status == ProductStatus.Draft)}  •  " +
                $"Aktif: {shop.ActiveProductCount}  •  " +
                $"Arsip: {shop.Products.Count(p => p.status == ProductStatus.Archived)}";
        }

        void RefreshProductList()
        {
            if (productListContent == null || productRowTemplate == null)
                return;

            foreach (Transform child in productListContent)
            {
                if (child == productRowTemplate.transform) continue;
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }

            foreach (LarisProduct product in manager.Marketplace.Products)
            {
                LarisProductRowUI row = Instantiate(productRowTemplate, productListContent);
                row.Setup(product, () =>
                {
                    manager.SelectProduct(product);
                    ShowPage(Page.Detail);
                });
            }

            productRowTemplate.gameObject.SetActive(false);
            productCountText.text = $"{manager.Marketplace.Products.Count} produk";
        }

        void RefreshDetail()
        {
            LarisProduct product = manager.SelectedProduct;
            bool hasProduct = product != null;
            detailIdText.text = hasProduct ? $"ID: {product.id}" : "Pilih produk dari daftar.";
            detailStatusText.text = hasProduct ? $"Status: {product.status}" : string.Empty;

            settingDetailFields = true;
            detailNameInput.SetTextWithoutNotify(hasProduct ? product.productName : string.Empty);
            detailDescriptionInput.SetTextWithoutNotify(hasProduct ? product.description : string.Empty);
            detailPriceInput.SetTextWithoutNotify(hasProduct ? product.price.ToString() : string.Empty);
            settingDetailFields = false;

            if (!hasProduct)
            {
                detailCategoryText.text = "-";
                detailTargetText.text = "-";
                recommendedPriceText.text = "-";
                productStatsText.text = "-";
                marketStatsText.text = "-";
                SetDetailActions(false, false, false);
                RefreshReviews(null);
                return;
            }

            detailCategoryText.text = product.category.ToString();
            detailTargetText.text = product.targetMarket.ToString();
            var shop = manager.Marketplace;
            PriceRange range = LarisPricing.GetRecommendedRange(product, shop.StoreTier);
            string nextTierLine = string.Empty;
            if (shop.StoreTier != StorePriceTier.Terkenal)
            {
                StorePriceTier nextTier = shop.StoreTier == StorePriceTier.Pemula
                    ? StorePriceTier.Berkembang
                    : StorePriceTier.Terkenal;
                PriceRange nextRange = LarisPricing.GetMarketBand(product, nextTier);
                nextTierLine =
                    $"\nTerkunci {nextTier}: Rp {nextRange.minimum:N0}–Rp {nextRange.maximum:N0}";
            }
            recommendedPriceText.text =
                $"Toko {shop.StoreTier}: Rp {range.minimum:N0} – Rp {range.maximum:N0}\n" +
                $"Ideal Rp {range.ideal:N0}  •  " +
                $"{LarisPricing.GetPriceAssessment(product, shop.StoreTier)}" +
                nextTierLine;

            productStatsText.text =
                $"QUALITY                     {product.quality}%\n" +
                $"RELEVANSI                 {product.relevance}%\n" +
                $"NILAI JUAL                 {product.sellValue}%";

            marketStatsText.text =
                $"Tayangan          {product.impressions:N0}\n" +
                $"Klik                    {product.clicks:N0}\n" +
                $"Pembelian         {product.sales:N0}\n" +
                $"Pendapatan      Rp {product.revenue:N0}\n" +
                $"Konversi           {product.ConversionRate:0.00}%\n" +
                $"Rating                {(product.reviewCount > 0 ? $"{product.rating:0.00}/5" : "-")}\n" +
                $"Ulasan               {product.reviewCount:N0}";

            bool draft = product.status == ProductStatus.Draft;
            bool active = product.status == ProductStatus.Active;
            bool archived = product.status == ProductStatus.Archived;
            SetDetailActions(draft, active, archived);
            if (priceMinusButton != null)
                priceMinusButton.interactable = product.price > range.minimum;
            if (pricePlusButton != null)
                pricePlusButton.interactable = product.price < range.maximum;
            promoteButtonText.text = product.IsPromoted
                ? $"PROMOSI {product.activePromotionPlatform} • {product.promotionDaysRemaining} HARI"
                : "BUKA PANEL PROMOSI";
            promoteButton.interactable = !product.IsPromoted;
            RefreshReviews(product);
        }

        void SetDetailActions(bool draft, bool active, bool archived)
        {
            publishButton.gameObject.SetActive(draft);
            promoteButton.gameObject.SetActive(active);
            archiveButton.gameObject.SetActive(active);
            reactivateButton.gameObject.SetActive(archived);
        }

        void OpenPromotionPage()
        {
            if (manager.SelectedProduct != null &&
                manager.SelectedProduct.status == ProductStatus.Active)
                promotionProduct = manager.SelectedProduct;
            ShowPage(Page.Promotion);
        }

        void RefreshPromotion()
        {
            if (promotionPage == null || promotionOfferContent == null)
                return;

            List<LarisProduct> activeProducts = manager.Marketplace.Products
                .Where(product => product.status == ProductStatus.Active)
                .ToList();
            if (promotionProduct == null || !activeProducts.Contains(promotionProduct))
                promotionProduct = activeProducts.FirstOrDefault();

            bool hasProduct = promotionProduct != null;
            promotionEmptyText.gameObject.SetActive(!hasProduct);
            promotionSelectedProductText.text =
                hasProduct ? promotionProduct.productName : "Belum ada produk aktif";
            promotionProductStatusText.text = hasProduct
                ? promotionProduct.IsPromoted
                    ? $"Sedang dipromosikan oleh {promotionProduct.activePromoterName} " +
                      $"({promotionProduct.activePromotionPlatform}) • " +
                      $"{promotionProduct.promotionDaysRemaining} hari tersisa • " +
                      $"+{promotionProduct.promotionViewBoostPercent}% tayangan"
                    : $"Harga Rp {promotionProduct.price:N0} • " +
                      $"Tayangan {promotionProduct.impressions:N0} • " +
                      $"Terjual {promotionProduct.sales:N0}"
                : "Publikasikan produk terlebih dahulu sebelum menjalankan promosi.";
            promotionProductPreviousButton.interactable = activeProducts.Count > 1;
            promotionProductNextButton.interactable = activeProducts.Count > 1;

            foreach (Transform child in promotionOfferContent)
            {
                if (child == promotionOfferRowTemplate.transform) continue;
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }

            IReadOnlyList<PromoterOffer> offers =
                manager.Marketplace.GetDailyPromotionOffers();
            selectedOfferIndex = Mathf.Clamp(selectedOfferIndex, 0, offers.Count - 1);
            for (int i = 0; i < offers.Count; i++)
            {
                int captured = i;
                PromotionOfferRowUI row =
                    Instantiate(promotionOfferRowTemplate, promotionOfferContent);
                row.Setup(offers[i], i == selectedOfferIndex, () =>
                {
                    selectedOfferIndex = captured;
                    RefreshPromotion();
                });
            }
            promotionOfferRowTemplate.gameObject.SetActive(false);
            promotionOfferCountText.text =
                $"{offers.Count} penawaran tersedia pada Hari {manager.Marketplace.CurrentDay}";

            PromoterOffer selected = offers.Count > 0 ? offers[selectedOfferIndex] : null;
            promotionSelectedOfferText.text = selected != null
                ? $"{selected.promoterName}\n" +
                  $"{selected.platform}  •  {selected.durationDays} hari\n\n" +
                  $"BIAYA\nRp {selected.cost:N0}\n\n" +
                  $"ESTIMASI KENAIKAN TAYANGAN\n+{selected.viewBoostPercent}%\n\n" +
                  "Promosi menaikkan jangkauan, tetapi tidak menjamin pembelian."
                : "Tidak ada penawaran hari ini.";
            confirmPromotionButton.interactable =
                hasProduct &&
                selected != null &&
                !promotionProduct.IsPromoted &&
                manager.Marketplace.Balance >= selected.cost;
        }

        void ChangePromotionProduct(int direction)
        {
            List<LarisProduct> activeProducts = manager.Marketplace.Products
                .Where(product => product.status == ProductStatus.Active)
                .ToList();
            if (activeProducts.Count == 0)
                return;

            int index = Mathf.Max(0, activeProducts.IndexOf(promotionProduct));
            promotionProduct =
                activeProducts[(index + direction + activeProducts.Count) % activeProducts.Count];
            selectedOfferIndex = 0;
            RefreshPromotion();
        }

        void ConfirmPromotion()
        {
            IReadOnlyList<PromoterOffer> offers =
                manager.Marketplace.GetDailyPromotionOffers();
            if (promotionProduct == null || offers.Count == 0)
                return;

            selectedOfferIndex = Mathf.Clamp(selectedOfferIndex, 0, offers.Count - 1);
            manager.PromoteProduct(promotionProduct, offers[selectedOfferIndex]);
        }

        void RefreshReviews(LarisProduct product)
        {
            foreach (Transform child in reviewContent)
            {
                if (child == reviewRowTemplate.transform) continue;
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }

            if (product != null)
            {
                foreach (LarisReview review in product.reviews.OrderByDescending(r => r.day))
                {
                    TMP_Text row = Instantiate(reviewRowTemplate, reviewContent);
                    row.gameObject.SetActive(true);
                    row.text = $"Hari {review.day}  •  {new string('★', review.rating)}\n{review.text}";
                }
            }
            reviewRowTemplate.gameObject.SetActive(false);
        }

        void RefreshAnalytics()
        {
            ShopAnalytics data = manager.Marketplace.GetAnalytics();
            string categories = string.Join(
                "\n",
                Enum.GetValues(typeof(ProductCategory))
                    .Cast<ProductCategory>()
                    .Select(category =>
                    {
                        long revenue = manager.Marketplace.Products
                            .Where(p => p.category == category)
                            .Sum(p => p.revenue);
                        int sales = manager.Marketplace.Products
                            .Where(p => p.category == category)
                            .Sum(p => p.sales);
                        return $"{category,-18}  Rp {revenue,12:N0}  •  {sales:N0} penjualan";
                    }));

            analyticsText.text =
                $"TOTAL TAYANGAN          {data.totalImpressions:N0}\n" +
                $"TOTAL KLIK                    {data.totalClicks:N0}\n" +
                $"TOTAL PENJUALAN         {data.totalSales:N0}\n" +
                $"TOTAL PENDAPATAN      Rp {data.totalRevenue:N0}\n" +
                $"RATA-RATA KONVERSI   {data.averageConversion:0.00}%\n\n" +
                $"Produk terlaris: {data.bestSeller}\n" +
                $"Rating tertinggi: {data.highestRated}\n" +
                $"Kategori teruntung: {data.mostProfitableCategory}\n\n" +
                $"PENDAPATAN PER KATEGORI\n{categories}";
        }

        void RefreshDailySummary()
        {
            DailyMarketResult result = manager.Marketplace.LastDailyResult;
            foreach (Transform child in dailyProductResultContent)
            {
                if (child == dailyProductResultTemplate.transform) continue;
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }

            if (result == null)
            {
                dailySummaryTitleText.text = "Belum Ada Simulasi";
                dailySummaryTotalsText.text = "Tekan tombol SIMULASIKAN 1 HARI untuk menjalankan pasar.";
                dailyProductResultTemplate.gameObject.SetActive(false);
                return;
            }

            dailySummaryTitleText.text = $"Ringkasan Hari {result.day}";
            dailySummaryTotalsText.text =
                $"+{result.newImpressions:N0} tayangan     " +
                $"+{result.newClicks:N0} klik     " +
                $"+{result.newSales:N0} penjualan     " +
                $"+Rp {result.newRevenue:N0}     " +
                $"+{result.newFollowers:N0} pengikut\n" +
                $"Rating toko {result.previousStoreRating:0.00} → {result.currentStoreRating:0.00}";

            foreach (ProductDayResult product in result.productResults)
            {
                TMP_Text row = Instantiate(dailyProductResultTemplate, dailyProductResultContent);
                row.gameObject.SetActive(true);
                row.text =
                    $"{product.productName}\n" +
                    $"+{product.newImpressions} tayangan  •  +{product.newClicks} klik  •  " +
                    $"+{product.newSales} terjual  •  +Rp {product.newRevenue:N0}";
            }
            dailyProductResultTemplate.gameObject.SetActive(false);
        }

        void AddDummy()
        {
            manager.AddDummyProduct();
            ShowPage(Page.Detail);
        }

        void SimulateDay()
        {
            manager.SimulateOneDay();
            ShowPage(Page.DailySummary);
        }

        void CycleTrend()
        {
            manager.CycleTrend();
            RefreshAll();
        }

        void ResetAll()
        {
            manager.ResetAll();
            ShowPage(Page.Dashboard);
        }

        void ChangeCategory(int direction)
        {
            LarisProduct product = manager.SelectedProduct;
            if (product == null) return;
            ProductCategory[] values = (ProductCategory[])Enum.GetValues(typeof(ProductCategory));
            int index = Array.IndexOf(values, product.category);
            product.category = values[(index + direction + values.Length) % values.Length];
            manager.NotifyProductEdited();
        }

        void ChangeTarget(int direction)
        {
            LarisProduct product = manager.SelectedProduct;
            if (product == null) return;
            TargetMarket[] values = (TargetMarket[])Enum.GetValues(typeof(TargetMarket));
            int index = Array.IndexOf(values, product.targetMarket);
            product.targetMarket = values[(index + direction + values.Length) % values.Length];
            manager.NotifyProductEdited();
        }

        void ChangePrice(int amount)
        {
            LarisProduct product = manager.SelectedProduct;
            if (product == null)
                return;

            manager.SetSelectedProductPrice(product.price + amount);
        }
    }
}
