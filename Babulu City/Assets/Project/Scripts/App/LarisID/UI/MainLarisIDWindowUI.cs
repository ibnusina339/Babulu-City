using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using IntegratedApps;
using ProdukLM;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LarisID
{
    /// <summary>
    /// Adapter mekanik untuk prefab desain Laris.ID di scene Main.
    /// Semua referensi dicari dari hierarchy yang sudah ada agar layout karya
    /// desainer tidak perlu dibuat ulang oleh script.
    /// </summary>
    [DisallowMultipleComponent]
    public class MainLarisIDWindowUI : MonoBehaviour
    {
        enum Page
        {
            Dashboard,
            Products,
            Promotion,
            History,
            Analytics
        }

        sealed class ProductRow
        {
            public GameObject root;
            public TMP_Text nameText;
            public TMP_Text categoryText;
            public TMP_Text priceText;
            public TMP_Text salesText;
            public GameObject activeBadge;
            public GameObject inactiveBadge;
            public Button activeButton;
            public Button inactiveButton;
            public Button editButton;
            public Button deleteButton;
            public TMP_InputField nameInput;
            public TMP_InputField priceInput;
        }

        sealed class PromotionRow
        {
            public GameObject root;
            public TMP_Text promoterText;
            public TMP_Text platformText;
            public TMP_Text detailsText;
            public Button selectButton;
        }

        sealed class HistoryRow
        {
            public GameObject root;
            public TMP_Text nameText;
            public TMP_Text impressionsText;
            public TMP_Text clicksText;
            public TMP_Text salesText;
            public TMP_Text revenueText;
        }

        [HideInInspector] public int integrationVersion;

        [Header("Referensi scene (diisi integrator)")]
        public LarisIDManager manager;
        public GameObject windowRoot;
        public GameObject produkLMWindow;
        public Transform desktopTaskbar;

        GameObject dashboardPage;
        GameObject productsPage;
        GameObject promotionPage;
        GameObject historyPage;
        GameObject analyticsPage;
        GameObject analyticsPageOne;
        GameObject analyticsPageTwo;

        TMP_Text shopNameText;
        TMP_Text shopDescriptionText;
        TMP_InputField shopNameInput;
        TMP_InputField shopDescriptionInput;
        TMP_Text dashboardBalance;
        TMP_Text dashboardRevenue;
        TMP_Text dashboardActiveProducts;
        TMP_Text dashboardRating;
        TMP_Text dashboardFollowers;
        TMP_Text dashboardDay;
        TMP_Text dashboardSales;
        TMP_Text dashboardTrend;

        TMP_Text productCountText;
        RectTransform productContent;
        readonly List<ProductRow> productRows = new();

        TMP_Text promotionProductName;
        TMP_Text promotionProductPrice;
        Image promotionProductIcon;
        TMP_Text promotionCountText;
        RectTransform promotionContent;
        TMP_Text promotionConfirmationTitle;
        TMP_Text promotionConfirmationCost;
        TMP_Text promotionConfirmationDays;
        TMP_Text promotionConfirmationBoost;
        Button previousPromotionProductButton;
        Button nextPromotionProductButton;
        Button runPromotionButton;
        readonly List<PromotionRow> promotionRows = new();
        IReadOnlyList<PromoterOffer> currentOffers = Array.Empty<PromoterOffer>();
        PromoterOffer selectedOffer;
        int promotionProductIndex;

        TMP_Text historyTitle;
        TMP_Text historyProductCount;
        TMP_Text historyImpressions;
        TMP_Text historyClicks;
        TMP_Text historySales;
        TMP_Text historyRevenue;
        TMP_Text historyConversion;
        RectTransform historyContent;
        Button previousHistoryButton;
        Button nextHistoryButton;
        readonly List<HistoryRow> historyRows = new();
        int selectedHistoryDay = 1;

        TMP_Text analyticsProductCount;
        TMP_Text analyticsImpressions;
        TMP_Text analyticsClicks;
        TMP_Text analyticsSales;
        TMP_Text analyticsRevenue;
        TMP_Text analyticsConversion;
        TMP_Text analyticsBestSellerLabel;
        TMP_Text analyticsHighestRatingLabel;
        TMP_Text analyticsCategoryLabel;
        TMP_Text analyticsBestName;
        TMP_Text analyticsBestImpressions;
        TMP_Text analyticsBestClicks;
        TMP_Text analyticsBestSales;
        TMP_Text analyticsBestRevenue;
        TMP_Text analyticsCategoryName;
        TMP_Text analyticsCategorySales;
        TMP_Text analyticsCategoryRevenue;
        Button analyticsNextButton;
        Button analyticsBackButton;

        GameClockUI gameClock;
        Page currentPage = Page.Dashboard;
        bool isBound;

        void Awake()
        {
            if (!ResolveHierarchy())
            {
                enabled = false;
                return;
            }

            BindStaticButtons();
            BindDashboard();
            BindProducts();
            BindPromotion();
            BindHistory();
            BindAnalytics();

            manager.StateChanged += RefreshAll;
            manager.DaySimulated += HandleDaySimulated;
            gameClock = GetComponent<GameClockUI>();
            if (gameClock != null)
                gameClock.DayEnded += HandleGameDayEnded;

            isBound = true;
            ShowPage(Page.Dashboard);
            RefreshAll();
        }

        void Start()
        {
            if (windowRoot != null)
                windowRoot.SetActive(false);
        }

        void OnDestroy()
        {
            if (manager != null)
            {
                manager.StateChanged -= RefreshAll;
                manager.DaySimulated -= HandleDaySimulated;
            }
            if (gameClock != null)
                gameClock.DayEnded -= HandleGameDayEnded;
        }

        bool ResolveHierarchy()
        {
            manager ??= GetComponent<LarisIDManager>();
            if (manager == null)
                manager = gameObject.AddComponent<LarisIDManager>();
            manager.EnsureInitialized();

            Transform root = transform;
            if (windowRoot == null)
            {
                Transform found = Descendants(root)
                    .FirstOrDefault(item =>
                        item.name.Equals("Laris.ID", StringComparison.OrdinalIgnoreCase) &&
                        FindNamed(item, "BG.Laris") != null);
                windowRoot = found != null ? found.gameObject : null;
            }

            if (windowRoot == null)
            {
                Debug.LogError("Window Laris.ID tidak ditemukan di bawah UI scene Main.", this);
                return false;
            }

            Transform mainBox = FindNamed(windowRoot.transform, "MainBox");
            dashboardPage = FindNamed(mainBox, "Dashboard.TAB")?.gameObject;
            productsPage = FindNamed(mainBox, "Produk.TAB")?.gameObject;
            promotionPage = FindNamed(mainBox, "Promosi.TAB")?.gameObject;
            historyPage = FindNamed(mainBox, "Ringkasan.TAB")?.gameObject;
            analyticsPage = FindNamed(mainBox, "Analisis.TAB")?.gameObject;

            if (dashboardPage == null || productsPage == null || promotionPage == null ||
                historyPage == null || analyticsPage == null)
            {
                Debug.LogError("Satu atau lebih panel utama Laris.ID tidak ditemukan.", this);
                return false;
            }

            produkLMWindow ??= FindNamed(root, "produk.LM")?.gameObject;
            desktopTaskbar ??= FindNamed(root, "taskbar");
            return true;
        }

        void BindStaticButtons()
        {
            Transform sideBar = FindNamed(windowRoot.transform, "MainTab");
            BindClick(FindNamed(sideBar, "Tab.DASBOARD"), () => ShowPage(Page.Dashboard));
            BindClick(FindNamed(sideBar, "Tab.PRODUK"), () => ShowPage(Page.Products));
            BindClick(FindNamed(sideBar, "Tab.PROMOSI"), () => ShowPage(Page.Promotion));
            BindClick(FindNamed(sideBar, "Tab.RIWAYATHARIAN"), () => ShowPage(Page.History));
            BindClick(FindNamed(sideBar, "Tab.ANALISIS"), () => ShowPage(Page.Analytics));

            Transform exitButton = FindNamed(windowRoot.transform, "Icon.Exit");
            if (exitButton != null)
            {
                // TopBar desain memiliki elemen dekorasi lebar setelah Icon.Exit.
                // Matikan raycast dekorasinya dan letakkan hit area Exit paling depan
                // saat runtime. Layout/prefab desain tidak ikut berubah.
                Transform topBar = exitButton.parent;
                foreach (Graphic decoration in topBar.GetComponentsInChildren<Graphic>(true))
                {
                    if (decoration.transform != exitButton &&
                        !decoration.transform.IsChildOf(exitButton))
                        decoration.raycastTarget = false;
                }

                exitButton.SetAsLastSibling();
                Button button = BindClick(exitButton, CloseWindow);
                button.interactable = true;

                // Sprite X merah berada pada child dengan skala bertingkat. Pasang
                // listener juga pada Graphic yang benar-benar diraycast oleh UI.
                foreach (Graphic graphic in exitButton.GetComponentsInChildren<Graphic>(true))
                {
                    graphic.raycastTarget = true;
                    graphic.raycastPadding = new Vector4(-12f, -12f, -12f, -12f);
                    Button graphicButton = BindClick(graphic.transform, CloseWindow);
                    graphicButton.interactable = true;
                }
            }

            Transform desktopMenu = FindNamed(transform, "MainMenu");
            BindClick(FindNamed(desktopMenu, "LARIS.ID App"), OpenWindow);
            BindAdditionalClick(FindNamed(desktopMenu, "ProdukLM App"), CloseWindow);
        }

        void BindDashboard()
        {
            Transform page = dashboardPage.transform;
            Transform nameFrame = FindNamed(page, "NamaToko.Frame");
            shopNameText = FindText(nameFrame, "NamaToko.TEXT");
            Transform descriptionPanel = FindNamed(page, "panel.deskripsi");
            // "Text (TMP)" di frameDeskripsi adalah judul "Deskripsi Toko".
            // Isi yang boleh diedit adalah teks badan bernama "Teks Deskripsi".
            shopDescriptionText = FindText(descriptionPanel, "Teks Deskripsi");

            shopNameInput = BuildInput(
                nameFrame,
                shopNameText,
                TMP_InputField.ContentType.Standard,
                TMP_InputField.LineType.SingleLine);
            shopDescriptionInput = BuildInput(
                shopDescriptionText?.transform,
                shopDescriptionText,
                TMP_InputField.ContentType.Standard,
                TMP_InputField.LineType.MultiLineNewline);

            if (shopNameInput != null)
            {
                shopNameInput.onEndEdit.RemoveAllListeners();
                shopNameInput.onEndEdit.AddListener(value =>
                    manager.SetShopIdentity(value, manager.Marketplace.ShopDescription));
            }
            if (shopDescriptionInput != null)
            {
                shopDescriptionInput.onEndEdit.RemoveAllListeners();
                shopDescriptionInput.onEndEdit.AddListener(value =>
                    manager.SetShopIdentity(manager.Marketplace.ShopName, value));
            }

            BindClick(FindNamed(nameFrame, "Tombol Edit nama Toko", "edit.frame"),
                () => ActivateInput(shopNameInput));
            BindClick(FindNamed(descriptionPanel, "Tombol edit deskripsi", "edit.frame (1)"),
                () => ActivateInput(shopDescriptionInput));

            dashboardBalance = CardValue(page, "TabSaldo");
            dashboardRevenue = CardValue(page, "TabTotalPendapatan");
            dashboardActiveProducts = CardValue(page, "TabProdukAktif");
            dashboardRating = CardValue(page, "TabRatingToko");
            dashboardFollowers = CardValue(page, "TabPengikut");
            dashboardDay = CardValue(page, "TabHariPemasaran");
            dashboardSales = CardValue(page, "TabTotalPenjualan");
            dashboardTrend = CardValue(page, "TabTrenAktif");
        }

        void BindProducts()
        {
            Transform page = productsPage.transform;
            productCountText = FindText(page, "jumlahProduk.TEXT");
            Transform panelContent = FindNamed(page, "Panel.Content");
            productContent = FindNamed(panelContent, "PanelProduk") as RectTransform;
            ConfigureGridScroll(panelContent as RectTransform, productContent);

            if (productContent == null)
                return;

            foreach (Transform child in DirectChildren(productContent))
            {
                if (child.name.StartsWith("FrameProdukIN", StringComparison.OrdinalIgnoreCase))
                    productRows.Add(CreateProductRow(child.gameObject));
            }
        }

        void BindPromotion()
        {
            Transform page = promotionPage.transform;
            promotionCountText = FindText(page, "jumlahProduk.TEXT");
            Transform productPanel = FindNamed(page, "Paneldata.frame");
            promotionProductName = FindText(productPanel, "NamaProduk.TXT");
            promotionProductPrice = FindText(productPanel, "HargaProduk.TXT");
            promotionProductIcon = FindNamed(productPanel, "ikon Produk", "produk.icon")?.GetComponent<Image>();

            previousPromotionProductButton = BindClick(
                FindNamed(productPanel, "PreviousProductButton", "minus.Frame"),
                () => ChangePromotionProduct(-1));
            nextPromotionProductButton = BindClick(
                FindNamed(productPanel, "NextProductButton", "Plus.Frame"),
                () => ChangePromotionProduct(1));

            Transform offersViewport = FindNamed(page, "PanelPilihanPromosi");
            promotionContent = DirectChildren(offersViewport)
                .FirstOrDefault(child => child.name == "PanelPilihanPromosi") as RectTransform;
            ConfigureGridScroll(offersViewport as RectTransform, promotionContent);
            if (promotionContent != null)
            {
                foreach (Transform child in DirectChildren(promotionContent))
                {
                    if (child.name.StartsWith("PanelPilihanPromosi", StringComparison.OrdinalIgnoreCase))
                        promotionRows.Add(CreatePromotionRow(child.gameObject));
                }
            }

            Transform confirmation = FindNamed(page, "PanelPromosikan");
            promotionConfirmationTitle = FindText(FindNamed(confirmation, "FrameAtas"), "Text (TMP)");
            promotionConfirmationCost = FindText(FindNamed(confirmation, "IconDuit"), "NilaiBiaya", "data.TXT (1)");
            promotionConfirmationDays = FindText(FindNamed(confirmation, "IconPenayanganKelas"), "data.TXT");
            promotionConfirmationBoost = FindText(FindNamed(confirmation, "iconEstimasiTayangan"), "Nilai Estimasi", "data.TXT (2)");
            runPromotionButton = BindClick(
                FindNamed(confirmation, "JalankanPromosiButton", "frame.Jalankan"),
                RunSelectedPromotion);
        }

        void BindHistory()
        {
            Transform page = historyPage.transform;
            historyTitle = FindText(page, "RingkasanTEXT");
            historyProductCount = FindText(page, "jumlahProduk.TEXT");
            previousHistoryButton = BindClick(FindNamed(page, "BackPage"), () => ChangeHistoryDay(-1));
            nextHistoryButton = BindClick(FindNamed(page, "NextPage"), () => ChangeHistoryDay(1));

            Transform dataPanel = FindNamed(page, "Paneldata.frame");
            historyImpressions = CardCount(dataPanel, "frameData.TAYANGAN");
            historyClicks = CardCount(dataPanel, "frameData.KLIK");
            historySales = CardCount(dataPanel, "frameData.PENJUALAN");
            historyRevenue = CardCount(dataPanel, "frameData.PENDAPATAN");
            historyConversion = CardCount(dataPanel, "frameData.KONVERSI");

            // Pada prefab desain, karakter '/' adalah bagian nama GameObject,
            // bukan pemisah hierarchy.
            Transform produkUtama = FindNamed(page, "PanelHasil/produkUtama");
            historyContent = FindNamed(produkUtama, "PanelHasil/Produk") as RectTransform;
            ConfigureGridScroll(produkUtama as RectTransform, historyContent);
            if (historyContent != null)
            {
                foreach (Transform child in DirectChildren(historyContent))
                {
                    if (child.name.StartsWith("PanelInformasi", StringComparison.OrdinalIgnoreCase))
                        historyRows.Add(CreateHistoryRow(child.gameObject));
                }
            }
        }

        void BindAnalytics()
        {
            Transform page = analyticsPage.transform;
            analyticsPageOne = FindNamed(page, "Page1AnalisisUtama")?.gameObject;
            analyticsPageTwo = FindNamed(page, "Page2.HasilAnalisisSeluruhTipeProduk")?.gameObject;
            analyticsProductCount = FindText(page, "jumlahProduk.TEXT");

            Transform dataPanel = FindNamed(analyticsPageOne?.transform, "Paneldata.frame");
            analyticsImpressions = CardCount(dataPanel, "frameData.TAYANGAN");
            analyticsClicks = CardCount(dataPanel, "frameData.KLIK");
            analyticsSales = CardCount(dataPanel, "frameData.PENJUALAN");
            analyticsRevenue = CardCount(dataPanel, "frameData.PENDAPATAN");
            analyticsConversion = CardCount(dataPanel, "frameData.KONVERSI");

            Transform bestPanel = Descendants(analyticsPageOne?.transform)
                .FirstOrDefault(item => FindNamed(item, "PanelInformasiProdukTerbaik") != null);
            Transform bestInfo = FindNamed(bestPanel, "PanelInformasiProdukTerbaik");
            analyticsBestName = FindText(bestInfo, "NamaProduk.TXT");
            analyticsBestImpressions = FrameValue(bestInfo, "framePenonton");
            analyticsBestClicks = FrameValue(bestInfo, "frameKlik");
            analyticsBestSales = FrameValue(bestInfo, "framePenjualan");
            analyticsBestRevenue = FrameValue(bestInfo, "framePendapatan");

            TMP_Text[] bestLabels = bestPanel != null
                ? bestPanel.GetComponentsInChildren<TMP_Text>(true)
                : Array.Empty<TMP_Text>();
            analyticsBestSellerLabel = bestLabels.FirstOrDefault(t => t.text.Contains("Produk Terlaris:"));
            analyticsHighestRatingLabel = bestLabels.FirstOrDefault(t => t.text.Contains("Rating Tertinggi:"));
            analyticsCategoryLabel = bestLabels.FirstOrDefault(t => t.text.Contains("Kategori Teruntung:"));

            analyticsNextButton = BindClick(
                FindNamed(analyticsPageOne?.transform, "NextPage"),
                () => ShowAnalyticsSubPage(false));
            analyticsBackButton = BindClick(
                FindNamed(analyticsPageTwo?.transform, "BackPage"),
                () => ShowAnalyticsSubPage(true));

            Transform categoryData = FindNamed(analyticsPageTwo?.transform, "Paneldata.frame");
            analyticsCategoryName = FindText(categoryData, "kategori.TXT");
            analyticsCategorySales = FindText(categoryData, "Penjualan.TXT");
            analyticsCategoryRevenue = FindText(categoryData, "Pendapatan.TXT");
            ShowAnalyticsSubPage(true);
        }

        public void OpenWindow()
        {
            if (!isBound)
                return;
            if (produkLMWindow != null)
                produkLMWindow.SetActive(false);
            windowRoot.SetActive(true);
            windowRoot.transform.SetAsLastSibling();
            if (desktopTaskbar != null)
                desktopTaskbar.SetAsLastSibling();
            ShowPage(currentPage);
            RefreshAll();
        }

        public void CloseWindow()
        {
            if (windowRoot != null)
                windowRoot.SetActive(false);
        }

        public void SimulateNextMarketingDay()
        {
            manager.SimulateOneDay();
        }

        void ShowPage(Page page)
        {
            currentPage = page;
            dashboardPage.SetActive(page == Page.Dashboard);
            productsPage.SetActive(page == Page.Products);
            promotionPage.SetActive(page == Page.Promotion);
            historyPage.SetActive(page == Page.History);
            analyticsPage.SetActive(page == Page.Analytics);
            RefreshAll();
        }

        void RefreshAll()
        {
            if (!isBound && !Application.isPlaying)
                return;
            RefreshDashboard();
            RefreshProducts();
            RefreshPromotion();
            RefreshHistory();
            RefreshAnalytics();
        }

        void RefreshDashboard()
        {
            LarisMarketplaceService shop = manager.Marketplace;
            SetInput(shopNameInput, shop.ShopName);
            SetInput(shopDescriptionInput, shop.ShopDescription);
            SetText(shopNameText, shop.ShopName);
            SetText(shopDescriptionText, shop.ShopDescription);
            SetText(dashboardBalance, Rupiah(shop.Balance));
            SetText(dashboardRevenue, Rupiah(shop.TotalRevenue));
            SetText(dashboardActiveProducts, shop.ActiveProductCount.ToString());
            SetText(dashboardRating, shop.TotalReviews > 0 ? shop.StoreRating.ToString("0.0") : "-");
            SetText(dashboardFollowers, shop.Followers.ToString());
            SetText(dashboardDay, shop.CurrentDay.ToString());
            SetText(dashboardSales, shop.TotalSales.ToString());
            SetText(dashboardTrend, CategoryName(shop.ActiveTrend));
        }

        void RefreshProducts()
        {
            IReadOnlyList<LarisProduct> products = manager.Marketplace.Products;
            SetText(productCountText, $"'{products.Count} PRODUK");
            EnsureProductRows(products.Count);
            for (int i = 0; i < productRows.Count; i++)
            {
                bool visible = i < products.Count;
                ProductRow row = productRows[i];
                row.root.SetActive(visible);
                if (!visible)
                    continue;
                BindProductRow(row, products[i]);
            }
            Rebuild(productContent);
        }

        void BindProductRow(ProductRow row, LarisProduct product)
        {
            SetText(row.nameText, product.productName);
            if (row.nameText != null)
                row.nameText.alignment = TextAlignmentOptions.Center;
            SetText(row.categoryText, CategoryName(product.category));
            SetText(row.priceText, Rupiah(product.price));
            SetText(row.salesText, product.sales.ToString());
            SetInput(row.nameInput, product.productName);
            SetInput(row.priceInput, product.price.ToString(CultureInfo.InvariantCulture));

            bool active = product.status == ProductStatus.Active;
            if (row.activeBadge != null) row.activeBadge.SetActive(active);
            if (row.inactiveBadge != null) row.inactiveBadge.SetActive(!active);

            row.editButton?.onClick.RemoveAllListeners();
            row.editButton?.onClick.AddListener(() => ActivateInput(row.nameInput));
            row.deleteButton?.onClick.RemoveAllListeners();
            row.deleteButton?.onClick.AddListener(() => manager.DeleteProduct(product));
            row.activeButton?.onClick.RemoveAllListeners();
            row.activeButton?.onClick.AddListener(() => manager.ToggleProductActive(product));
            row.inactiveButton?.onClick.RemoveAllListeners();
            row.inactiveButton?.onClick.AddListener(() => manager.ToggleProductActive(product));

            if (row.nameInput != null)
            {
                row.nameInput.onEndEdit.RemoveAllListeners();
                row.nameInput.onEndEdit.AddListener(value =>
                {
                    manager.SetProductName(product, value);
                    if (row.nameText != null)
                        row.nameText.alignment = TextAlignmentOptions.Center;
                    row.nameInput.SetTextWithoutNotify(product.productName);
                });
            }
            if (row.priceInput != null)
            {
                row.priceInput.onEndEdit.RemoveAllListeners();
                row.priceInput.onEndEdit.AddListener(value =>
                {
                    string digits = new string((value ?? string.Empty).Where(char.IsDigit).ToArray());
                    if (int.TryParse(digits, out int price))
                        manager.SetProductPrice(product, price);
                    else
                        RefreshProducts();
                });
            }
        }

        void RefreshPromotion()
        {
            LarisMarketplaceService shop = manager.Marketplace;
            List<LarisProduct> eligible = EligiblePromotionProducts();
            if (eligible.Count == 0)
                promotionProductIndex = 0;
            else
                promotionProductIndex = Mathf.Clamp(promotionProductIndex, 0, eligible.Count - 1);

            LarisProduct product = eligible.Count > 0 ? eligible[promotionProductIndex] : null;
            SetText(promotionProductName, product != null ? product.productName : "Belum ada produk aktif");
            SetText(promotionProductPrice, product != null ? Rupiah(product.price) : "Harga Rp -");
            if (promotionProductIcon != null)
            {
                promotionProductIcon.sprite = ResolveProductIcon(product);
                promotionProductIcon.preserveAspect = true;
                promotionProductIcon.color = Color.white;
                promotionProductIcon.gameObject.SetActive(
                    product != null && promotionProductIcon.sprite != null);
            }
            SetText(promotionCountText, $"'{eligible.Count} PRODUK");
            if (previousPromotionProductButton != null)
                previousPromotionProductButton.interactable = eligible.Count > 1;
            if (nextPromotionProductButton != null)
                nextPromotionProductButton.interactable = eligible.Count > 1;

            currentOffers = shop.GetDailyPromotionOffers();
            if (selectedOffer != null && currentOffers.All(item => item.id != selectedOffer.id))
                selectedOffer = null;
            EnsurePromotionRows(currentOffers.Count);
            for (int i = 0; i < promotionRows.Count; i++)
            {
                bool visible = i < currentOffers.Count;
                PromotionRow row = promotionRows[i];
                row.root.SetActive(visible);
                if (!visible)
                    continue;

                PromoterOffer offer = currentOffers[i];
                SetText(row.promoterText, offer.promoterName);
                SetText(row.platformText, offer.platform.ToString().ToUpperInvariant());
                SetText(row.detailsText,
                    $"{Rupiah(offer.cost)} • {offer.durationDays} Hari • Estimasi Tayangan +{offer.viewBoostPercent}%");
                row.selectButton.onClick.RemoveAllListeners();
                row.selectButton.onClick.AddListener(() =>
                {
                    selectedOffer = offer;
                    RefreshPromotionConfirmation(product);
                });
            }
            Rebuild(promotionContent);
            RefreshPromotionConfirmation(product);
        }

        void RefreshPromotionConfirmation(LarisProduct product)
        {
            SetText(promotionConfirmationTitle,
                selectedOffer != null ? selectedOffer.promoterName : "Ringkasan Promosi");
            SetText(promotionConfirmationCost,
                selectedOffer != null ? Rupiah(selectedOffer.cost) : "Rp 0");
            SetText(promotionConfirmationDays,
                selectedOffer != null ? $"{selectedOffer.durationDays} Hari" : "0 Hari");
            SetText(promotionConfirmationBoost,
                selectedOffer != null ? $"+{selectedOffer.viewBoostPercent}%" : "+0%");
            if (runPromotionButton != null)
            {
                runPromotionButton.interactable =
                    product != null && selectedOffer != null &&
                    !product.IsPromoted && manager.Marketplace.Balance >= selectedOffer.cost;
            }
        }

        void RunSelectedPromotion()
        {
            LarisProduct product = CurrentPromotionProduct();
            if (product == null || selectedOffer == null)
                return;
            manager.PromoteProduct(product, selectedOffer);
        }

        void ChangePromotionProduct(int direction)
        {
            List<LarisProduct> eligible = EligiblePromotionProducts();
            if (eligible.Count == 0)
                return;
            promotionProductIndex =
                (promotionProductIndex + direction + eligible.Count) % eligible.Count;
            RefreshPromotion();
        }

        void RefreshHistory()
        {
            IReadOnlyList<DailyMarketResult> history = manager.Marketplace.DailyHistory;
            if (history.Count > 0)
                selectedHistoryDay = Mathf.Clamp(selectedHistoryDay, 1, history.Max(item => item.day));
            else
                selectedHistoryDay = 1;

            DailyMarketResult result = history.FirstOrDefault(item => item.day == selectedHistoryDay);
            SetText(historyTitle, $"RINGKASAN HARI KE-{selectedHistoryDay}");
            SetText(historyProductCount, $"'{(result?.productResults.Count ?? 0)} PRODUK");
            SetText(historyImpressions, (result?.newImpressions ?? 0).ToString());
            SetText(historyClicks, (result?.newClicks ?? 0).ToString());
            SetText(historySales, (result?.newSales ?? 0).ToString());
            SetText(historyRevenue, Rupiah(result?.newRevenue ?? 0));
            float conversion = result != null && result.newClicks > 0
                ? (float)result.newSales / result.newClicks * 100f
                : 0f;
            SetText(historyConversion, $"{conversion:0.0}%");

            int count = result?.productResults.Count ?? 0;
            EnsureHistoryRows(count);
            for (int i = 0; i < historyRows.Count; i++)
            {
                bool visible = i < count;
                HistoryRow row = historyRows[i];
                row.root.SetActive(visible);
                if (!visible)
                    continue;
                ProductDayResult item = result.productResults[i];
                SetText(row.nameText, item.productName);
                SetText(row.impressionsText, $"+{item.newImpressions} Tayangan");
                SetText(row.clicksText, $"+{item.newClicks} Klik");
                SetText(row.salesText, $"+{item.newSales} Terjual");
                SetText(row.revenueText, $"+{Rupiah(item.newRevenue)}");
            }
            Rebuild(historyContent);

            if (previousHistoryButton != null)
                previousHistoryButton.interactable = history.Count > 0 && selectedHistoryDay > 1;
            if (nextHistoryButton != null)
                nextHistoryButton.interactable = history.Count > 0 && selectedHistoryDay < history.Max(item => item.day);
        }

        void ChangeHistoryDay(int direction)
        {
            IReadOnlyList<DailyMarketResult> history = manager.Marketplace.DailyHistory;
            if (history.Count == 0)
                return;
            int maximum = history.Max(item => item.day);
            selectedHistoryDay = Mathf.Clamp(selectedHistoryDay + direction, 1, maximum);
            RefreshHistory();
        }

        void RefreshAnalytics()
        {
            LarisMarketplaceService shop = manager.Marketplace;
            ShopAnalytics data = shop.GetAnalytics();
            SetText(analyticsProductCount, $"'{shop.Products.Count} PRODUK");
            SetText(analyticsImpressions, data.totalImpressions.ToString());
            SetText(analyticsClicks, data.totalClicks.ToString());
            SetText(analyticsSales, data.totalSales.ToString());
            SetText(analyticsRevenue, Rupiah(data.totalRevenue));
            SetText(analyticsConversion, $"{data.averageConversion:0.0}%");
            SetText(analyticsBestSellerLabel, $"'Produk Terlaris: {data.bestSeller}'");
            SetText(analyticsHighestRatingLabel, $"'Rating Tertinggi: {data.highestRated}'");
            SetText(analyticsCategoryLabel, $"'Kategori Teruntung: {CategoryName(data.mostProfitableCategory)}'");

            LarisProduct best = shop.GetBestSeller();
            SetText(analyticsBestName, best?.productName ?? "-");
            SetText(analyticsBestImpressions, best != null ? best.impressions.ToString() : "0");
            SetText(analyticsBestClicks, best != null ? best.clicks.ToString() : "0");
            SetText(analyticsBestSales, best != null ? best.sales.ToString() : "0");
            SetText(analyticsBestRevenue, Rupiah(best?.revenue ?? 0));

            SetText(analyticsCategoryName, CategoryName(data.mostProfitableCategory));
            int categorySales = shop.Products
                .Where(item => item.category == data.mostProfitableCategory)
                .Sum(item => item.sales);
            long categoryRevenue = shop.Products
                .Where(item => item.category == data.mostProfitableCategory)
                .Sum(item => item.revenue);
            SetText(analyticsCategorySales, categorySales.ToString());
            SetText(analyticsCategoryRevenue, Rupiah(categoryRevenue));
        }

        void ShowAnalyticsSubPage(bool firstPage)
        {
            if (analyticsPageOne != null) analyticsPageOne.SetActive(firstPage);
            if (analyticsPageTwo != null) analyticsPageTwo.SetActive(!firstPage);
        }

        void HandleDaySimulated(DailyMarketResult result)
        {
            selectedHistoryDay = result.day;
            selectedOffer = null;
            promotionProductIndex = 0;
            RefreshAll();
        }

        void HandleGameDayEnded()
        {
            manager.SimulateOneDay();
        }

        ProductRow CreateProductRow(GameObject root)
        {
            Transform row = root.transform;
            TMP_Text name = FindText(FindNamed(row, "produk"), "Text (TMP)");
            TMP_Text price = FindText(row, "harga.txt (1)");
            Transform active = FindNamed(row, "ActiveStatus", "ActiveStatusJual");
            Transform inactive = FindNamed(row, "unActiveStatus (1)", "unActiveStatusJual");
            return new ProductRow
            {
                root = root,
                nameText = name,
                categoryText = FindText(row, "Kategori.txt"),
                priceText = price,
                salesText = FindText(row, "stok.txt (2)"),
                activeBadge = active?.gameObject,
                inactiveBadge = inactive?.gameObject,
                activeButton = BindClick(active, null),
                inactiveButton = BindClick(inactive, null),
                editButton = BindClick(FindNamed(row, "Edit button", "edit.frame"), null),
                deleteButton = BindClick(FindNamed(row, "delete.frame (1)"), null),
                nameInput = BuildInput(name?.transform.parent, name,
                    TMP_InputField.ContentType.Standard, TMP_InputField.LineType.SingleLine),
                priceInput = BuildInput(price?.transform, price,
                    TMP_InputField.ContentType.IntegerNumber, TMP_InputField.LineType.SingleLine)
            };
        }

        PromotionRow CreatePromotionRow(GameObject root)
        {
            return new PromotionRow
            {
                root = root,
                promoterText = FindText(root.transform, "NamaPromosi", "NamaPromotor"),
                platformText = FindText(root.transform, "NamaPlatformPromosi"),
                detailsText = FindText(root.transform, "Ket.Harga,Hari,EstimasiTayangan", "keterangan"),
                selectButton = BindClick(FindNamed(root.transform, "SelectProduct", "framePilih"), null)
            };
        }

        HistoryRow CreateHistoryRow(GameObject root)
        {
            Transform row = root.transform;
            return new HistoryRow
            {
                root = root,
                nameText = FindText(row, "NamaProduk.TXT"),
                impressionsText = FrameValue(row, "framePenonton"),
                clicksText = FrameValue(row, "frameKlik"),
                salesText = FrameValue(row, "framePenjualan"),
                revenueText = FrameValue(row, "framePendapatan")
            };
        }

        void EnsureProductRows(int count)
        {
            while (productRows.Count < count && productRows.Count > 0)
            {
                GameObject clone = Instantiate(productRows[0].root, productContent);
                clone.name = $"FrameProdukIN Runtime {productRows.Count + 1}";
                productRows.Add(CreateProductRow(clone));
            }
        }

        void EnsurePromotionRows(int count)
        {
            while (promotionRows.Count < count && promotionRows.Count > 0)
            {
                GameObject clone = Instantiate(promotionRows[0].root, promotionContent);
                clone.name = $"PanelPilihanPromosi Runtime {promotionRows.Count + 1}";
                promotionRows.Add(CreatePromotionRow(clone));
            }
        }

        void EnsureHistoryRows(int count)
        {
            while (historyRows.Count < count && historyRows.Count > 0)
            {
                GameObject clone = Instantiate(historyRows[0].root, historyContent);
                clone.name = $"PanelInformasi Runtime {historyRows.Count + 1}";
                historyRows.Add(CreateHistoryRow(clone));
            }
        }

        List<LarisProduct> EligiblePromotionProducts()
        {
            return manager.Marketplace.Products
                .Where(item => item.status == ProductStatus.Active)
                .ToList();
        }

        LarisProduct CurrentPromotionProduct()
        {
            List<LarisProduct> products = EligiblePromotionProducts();
            return products.Count == 0
                ? null
                : products[Mathf.Clamp(promotionProductIndex, 0, products.Count - 1)];
        }

        static void ConfigureGridScroll(RectTransform viewport, RectTransform content)
        {
            if (viewport == null || content == null)
                return;

            ScrollRect scroll = viewport.GetComponent<ScrollRect>();
            if (scroll == null) scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.inertia = true;
            scroll.decelerationRate = .135f;
            scroll.scrollSensitivity = 28f;

            if (viewport.GetComponent<RectMask2D>() == null)
                viewport.gameObject.AddComponent<RectMask2D>();
            ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
            if (fitter == null) fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        static TMP_InputField BuildInput(
            Transform host,
            TMP_Text text,
            TMP_InputField.ContentType contentType,
            TMP_InputField.LineType lineType)
        {
            if (host == null || text == null)
                return null;
            TMP_InputField input = host.GetComponent<TMP_InputField>();
            if (input == null) input = host.gameObject.AddComponent<TMP_InputField>();
            input.textViewport = text.rectTransform;
            input.textComponent = text;
            text.raycastTarget = true;
            input.targetGraphic = host.GetComponent<Graphic>() ?? text;
            input.contentType = contentType;
            input.lineType = lineType;
            input.richText = false;
            input.customCaretColor = true;
            input.caretColor = Color.white;
            input.selectionColor = new Color(.25f, .65f, 1f, .45f);
            return input;
        }

        static Sprite ResolveProductIcon(LarisProduct product)
        {
            if (product == null || string.IsNullOrWhiteSpace(product.iconKey))
                return null;

            MainProdukLMWindowUI produkWindow = UnityEngine.Object.FindAnyObjectByType<MainProdukLMWindowUI>(
                FindObjectsInactive.Include);
            MainProdukLMWindowUI.ProductOption option = produkWindow?.productOptions?
                .FirstOrDefault(item => item?.card != null &&
                    item.card.cardId.Equals(product.iconKey, StringComparison.OrdinalIgnoreCase));
            return option?.previewIcon ?? option?.card?.icon;
        }

        static void ActivateInput(TMP_InputField input)
        {
            if (input == null)
                return;
            input.Select();
            input.ActivateInputField();
            input.MoveTextEnd(false);
        }

        static Button BindClick(Transform target, Action action)
        {
            if (target == null)
                return null;
            Button button = target.GetComponent<Button>();
            if (button == null) button = target.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic ??= target.GetComponent<Graphic>();
            if (action != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => action());
            }
            return button;
        }

        static Button BindAdditionalClick(Transform target, Action action)
        {
            if (target == null)
                return null;
            Button button = target.GetComponent<Button>();
            if (button == null) button = target.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic ??= target.GetComponent<Graphic>();
            if (action != null)
                button.onClick.AddListener(() => action());
            return button;
        }

        static TMP_Text CardValue(Transform page, string cardName)
        {
            Transform card = FindNamed(page, cardName);
            if (card == null) return null;
            TMP_Text[] texts = card.GetComponentsInChildren<TMP_Text>(true);
            return texts.FirstOrDefault(item => item.name.StartsWith("Nilai", StringComparison.OrdinalIgnoreCase))
                   ?? texts.LastOrDefault();
        }

        static TMP_Text CardCount(Transform root, string cardName)
        {
            Transform card = FindNamed(root, cardName);
            return FindText(card, "count'", "Nilai");
        }

        static TMP_Text FrameValue(Transform root, string frameName)
        {
            Transform frame = FindNamed(root, frameName);
            if (frame == null) return null;
            TMP_Text[] texts = frame.GetComponentsInChildren<TMP_Text>(true);
            return texts.LastOrDefault();
        }

        static TMP_Text FindText(Transform root, params string[] names)
        {
            if (root == null)
                return null;
            foreach (string name in names)
            {
                TMP_Text exact = root.GetComponentsInChildren<TMP_Text>(true)
                    .FirstOrDefault(item => item.name.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (exact != null)
                    return exact;
            }
            return null;
        }

        static Transform FindNamed(Transform root, params string[] names)
        {
            if (root == null)
                return null;
            foreach (Transform item in Descendants(root))
            {
                foreach (string name in names)
                {
                    if (item.name.Equals(name, StringComparison.OrdinalIgnoreCase))
                        return item;
                }
            }
            return null;
        }

        static IEnumerable<Transform> Descendants(Transform root)
        {
            if (root == null)
                yield break;
            var stack = new Stack<Transform>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                Transform current = stack.Pop();
                yield return current;
                for (int i = current.childCount - 1; i >= 0; i--)
                    stack.Push(current.GetChild(i));
            }
        }

        static IEnumerable<Transform> DirectChildren(Transform root)
        {
            if (root == null)
                yield break;
            for (int i = 0; i < root.childCount; i++)
                yield return root.GetChild(i);
        }

        static void SetText(TMP_Text target, string value)
        {
            if (target != null)
                target.text = value;
        }

        static void SetInput(TMP_InputField target, string value)
        {
            if (target != null && !target.isFocused)
                target.SetTextWithoutNotify(value ?? string.Empty);
        }

        static void Rebuild(RectTransform target)
        {
            if (target == null)
                return;
            LayoutRebuilder.ForceRebuildLayoutImmediate(target);
        }

        static string Rupiah(long value) => $"Rp {value:N0}";

        static string CategoryName(ProductCategory category)
        {
            return category switch
            {
                ProductCategory.Ebook => "E-Book",
                ProductCategory.BusinessPlan => "Business Plan",
                ProductCategory.SocialMedia => "Media Sosial",
                ProductCategory.Education => "Edukasi",
                ProductCategory.Presentation => "Presentasi",
                ProductCategory.Template => "Template",
                _ => "Lainnya"
            };
        }
    }
}
