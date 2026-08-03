using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LarisID.Editor
{
    /// <summary>
    /// Membuat UI testing sebagai GameObject sungguhan satu kali.
    /// Setelah LarisID_UIRoot ada, layout tidak dibangun ulang supaya aman diedit desainer.
    /// </summary>
    [InitializeOnLoad]
    public static class LarisIDTestSceneBuilder
    {
        public const string ScenePath = "Assets/Project/Scenes/LarisID_Test.unity";
        public const string PrefabPath = "Assets/Project/Prefabs/UI/LarisIDFullUI.prefab";

        static readonly Color Background = Hex("#0C1120");
        static readonly Color Panel = Hex("#192036");
        static readonly Color PanelSoft = Hex("#222B47");
        static readonly Color Accent = Hex("#6C63FF");
        static readonly Color Cyan = Hex("#31D2BE");
        static readonly Color Text = Hex("#F4F7FF");
        static readonly Color Muted = Hex("#A4B0CC");
        static readonly Color Danger = Hex("#D55D73");

        static LarisIDTestSceneBuilder()
        {
            EditorApplication.delayCall += CreateIfMissing;
        }

        [MenuItem("Tools/Laris.ID/Create Test Scene GameObjects")]
        public static void CreateOrUpdateFromMenu()
        {
            CreateOrUpdate(true);
        }

        static void CreateIfMissing()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
                CreateOrUpdate(false);
        }

        static void CreateOrUpdate(bool showLog)
        {
            bool sceneAssetExists = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null;
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedForBuild = !scene.IsValid() || !scene.isLoaded;

            if (openedForBuild)
            {
                scene = sceneAssetExists
                    ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive)
                    : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            }

            bool changed = EnsureSceneObjects(scene);

            if (!sceneAssetExists)
            {
                EditorSceneManager.SaveScene(scene, ScenePath);
                changed = false;
            }
            else if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            if (changed)
            {
                GameObject uiRoot = FindRoot(scene, "LarisID_UIRoot");
                if (uiRoot != null)
                    PrefabUtility.SaveAsPrefabAsset(uiRoot, PrefabPath);
            }

            EnsureInBuildSettings();

            if (openedForBuild)
                EditorSceneManager.CloseScene(scene, true);

            AssetDatabase.SaveAssets();
            if (showLog)
                Debug.Log($"Laris.ID GameObject UI siap: {ScenePath}");
        }

        static bool EnsureSceneObjects(Scene scene)
        {
            bool changed = false;
            GameObject system = FindRoot(scene, "LarisID_System");
            if (system == null)
            {
                system = new GameObject("LarisID_System");
                SceneManager.MoveGameObjectToScene(system, scene);
                changed = true;
            }

            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(system);
            LarisIDManager manager = system.GetComponent<LarisIDManager>();
            if (manager == null)
            {
                manager = system.AddComponent<LarisIDManager>();
                changed = true;
            }

            GameObject cameraObject = FindRoot(scene, "Main Camera");
            if (cameraObject == null)
            {
                cameraObject = new GameObject("Main Camera");
                SceneManager.MoveGameObjectToScene(cameraObject, scene);
                cameraObject.tag = "MainCamera";
                cameraObject.transform.position = new Vector3(0, 0, -10);
                changed = true;
            }

            Camera camera = cameraObject.GetComponent<Camera>();
            if (camera == null)
            {
                camera = cameraObject.AddComponent<Camera>();
                changed = true;
            }
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Background;

            GameObject eventSystem = FindRoot(scene, "EventSystem");
            if (eventSystem == null)
            {
                eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                SceneManager.MoveGameObjectToScene(eventSystem, scene);
                eventSystem.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
                changed = true;
            }

            GameObject uiRoot = FindRoot(scene, "LarisID_UIRoot");
            if (uiRoot == null)
            {
                BuildGameObjectUI(scene, manager);
                changed = true;
            }
            else if (EnsurePromotionUI(uiRoot))
            {
                changed = true;
            }

            return changed;
        }

        static bool EnsurePromotionUI(GameObject root)
        {
            LarisIDSceneUI ui = root.GetComponent<LarisIDSceneUI>();
            if (ui == null)
                return false;

            bool changed = false;
            Transform sidebar = root.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(item => item.name == "Sidebar");
            Transform content = root.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(item => item.name == "ContentRoot");
            if (sidebar == null || content == null)
                return false;

            if (ui.promotionButton == null)
            {
                ui.promotionButton = ButtonObject(
                    "PromotionButton",
                    sidebar,
                    "PROMOSI",
                    PanelSoft,
                    V(.07f, .60f),
                    V(.93f, .68f));
                changed = true;
            }
            if (ui.promotionPage == null)
            {
                ui.promotionPage = BuildPromotion((RectTransform)content, ui);
                ui.promotionPage.SetActive(false);
                changed = true;
            }

            if (SetAnchors(ui.analyticsButton, V(.07f, .50f), V(.93f, .58f)))
                changed = true;
            if (SetAnchors(ui.dailySummaryButton, V(.07f, .40f), V(.93f, .48f)))
                changed = true;

            Transform hint = sidebar.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(item => item.name == "TestingHint");
            if (hint != null &&
                SetAnchors(hint.GetComponent<RectTransform>(), V(.08f, .15f), V(.92f, .34f)))
                changed = true;

            if (changed)
                EditorUtility.SetDirty(ui);
            return changed;
        }

        static bool SetAnchors(Selectable selectable, Vector2 min, Vector2 max) =>
            selectable != null && SetAnchors(selectable.GetComponent<RectTransform>(), min, max);

        static bool SetAnchors(RectTransform rect, Vector2 min, Vector2 max)
        {
            if (rect == null || (rect.anchorMin == min && rect.anchorMax == max))
                return false;

            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return true;
        }

        static void BuildGameObjectUI(Scene scene, LarisIDManager manager)
        {
            GameObject root = new GameObject(
                "LarisID_UIRoot",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            SceneManager.MoveGameObjectToScene(root, scene);

            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            scaler.matchWidthOrHeight = 0.5f;

            Image backdrop = root.AddComponent<Image>();
            backdrop.color = Background;
            backdrop.raycastTarget = false;
            Stretch(root.GetComponent<RectTransform>());

            LarisIDSceneUI ui = root.AddComponent<LarisIDSceneUI>();
            ui.manager = manager;

            RectTransform topBar = PanelObject("TopBar", root.transform, Panel, V(0.018f, 0.905f), V(0.982f, 0.985f));
            TextObject("Logo", topBar, "LARIS.ID", 30, Accent, FontStyles.Bold, TextAlignmentOptions.Left,
                V(0.025f, 0.15f), V(0.18f, 0.85f));
            TextObject("Tagline", topBar, "MARKETPLACE PRODUK DIGITAL", 10, Muted, FontStyles.Normal,
                TextAlignmentOptions.Left, V(0.025f, 0.04f), V(0.25f, 0.32f));
            ui.dayTrendText = TextObject("DayAndTrend", topBar, "Hari 1  |  Tren: None", 15, Text,
                FontStyles.Normal, TextAlignmentOptions.Center, V(0.24f, 0.18f), V(0.46f, 0.82f));
            ui.balanceHeaderText = TextObject("Balance", topBar, "Saldo Rp 0", 16, Cyan, FontStyles.Bold,
                TextAlignmentOptions.Center, V(0.46f, 0.18f), V(0.62f, 0.82f));
            ui.simulateDayButton = ButtonObject("SimulateDayButton", topBar, "SIMULASIKAN 1 HARI", Accent,
                V(0.635f, 0.18f), V(0.785f, 0.82f));
            ui.cycleTrendButton = ButtonObject("CycleTrendButton", topBar, "GANTI TREN", PanelSoft,
                V(0.795f, 0.18f), V(0.89f, 0.82f));
            ui.resetButton = ButtonObject("ResetButton", topBar, "RESET", Danger,
                V(0.90f, 0.18f), V(0.975f, 0.82f));

            RectTransform sidebar = PanelObject("Sidebar", root.transform, Panel, V(0.018f, 0.04f), V(0.17f, 0.885f));
            TextObject("StoreLabel", sidebar, "TOKO SAYA", 13, Muted, FontStyles.Bold,
                TextAlignmentOptions.Left, V(0.10f, 0.91f), V(0.90f, 0.97f));
            ui.dashboardButton = ButtonObject("DashboardButton", sidebar, "DASHBOARD", Accent,
                V(0.07f, 0.80f), V(0.93f, 0.88f));
            ui.productsButton = ButtonObject("ProductsButton", sidebar, "PRODUK", PanelSoft,
                V(0.07f, 0.70f), V(0.93f, 0.78f));
            ui.promotionButton = ButtonObject("PromotionButton", sidebar, "PROMOSI", PanelSoft,
                V(0.07f, 0.60f), V(0.93f, 0.68f));
            ui.analyticsButton = ButtonObject("AnalyticsButton", sidebar, "ANALITIK", PanelSoft,
                V(0.07f, 0.50f), V(0.93f, 0.58f));
            ui.dailySummaryButton = ButtonObject("DailySummaryButton", sidebar, "RINGKASAN HARIAN", PanelSoft,
                V(0.07f, 0.40f), V(0.93f, 0.48f));
            TextObject("TestingHint", sidebar,
                "SCENE TESTING\n\nSemua panel dan tombol ini adalah GameObject yang bisa diatur dari Inspector.",
                12, Muted, FontStyles.Normal, TextAlignmentOptions.TopLeft,
                V(0.08f, 0.15f), V(0.92f, 0.34f));
            ui.statusMessageText = TextObject("StatusMessage", sidebar, "Laris.ID siap diuji.", 11, Cyan,
                FontStyles.Normal, TextAlignmentOptions.BottomLeft, V(0.08f, 0.035f), V(0.92f, 0.14f));

            RectTransform content = PanelObject("ContentRoot", root.transform, Background,
                V(0.182f, 0.04f), V(0.982f, 0.885f));
            content.GetComponent<Image>().raycastTarget = false;

            ui.dashboardPage = BuildDashboard(content, ui);
            ui.productsPage = BuildProducts(content, ui);
            ui.detailPage = BuildDetail(content, ui);
            ui.promotionPage = BuildPromotion(content, ui);
            ui.analyticsPage = BuildAnalytics(content, ui);
            ui.dailySummaryPage = BuildDailySummary(content, ui);

            ui.dashboardPage.SetActive(true);
            ui.productsPage.SetActive(false);
            ui.detailPage.SetActive(false);
            ui.promotionPage.SetActive(false);
            ui.analyticsPage.SetActive(false);
            ui.dailySummaryPage.SetActive(false);

            Selection.activeGameObject = root;
        }

        static GameObject BuildDashboard(RectTransform parent, LarisIDSceneUI ui)
        {
            RectTransform page = RectObject("DashboardPage", parent, V(0, 0), V(1, 1));
            TextObject("Title", page, "Dashboard Toko", 30, Text, FontStyles.Bold,
                TextAlignmentOptions.Left, V(0.02f, 0.91f), V(0.48f, 0.98f));
            TextObject("Subtitle", page, "Pantau kondisi bisnis digitalmu hari ini.", 13, Muted,
                FontStyles.Normal, TextAlignmentOptions.Left, V(0.02f, 0.87f), V(0.50f, 0.92f));
            TextObject("ShopNameLabel", page, "NAMA TOKO", 11, Muted, FontStyles.Bold,
                TextAlignmentOptions.Left, V(0.64f, 0.93f), V(0.75f, 0.97f));
            ui.shopNameInput = InputObject("ShopNameInput", page, "Nama toko", false,
                V(0.74f, 0.91f), V(0.98f, 0.975f));

            ui.dashboardBalanceText = MetricCard(page, "BalanceCard", "SALDO", "Rp 0", 0, 0);
            ui.dashboardActiveProductsText = MetricCard(page, "ActiveCard", "PRODUK AKTIF", "0", 1, 0);
            ui.dashboardFollowersText = MetricCard(page, "FollowersCard", "PENGIKUT", "0", 2, 0);
            ui.dashboardSalesText = MetricCard(page, "SalesCard", "TOTAL PENJUALAN", "0", 3, 0);
            ui.dashboardRevenueText = MetricCard(page, "RevenueCard", "TOTAL PENDAPATAN", "Rp 0", 0, 1);
            ui.dashboardRatingText = MetricCard(page, "RatingCard", "RATING TOKO", "-", 1, 1);
            ui.dashboardDayText = MetricCard(page, "DayCard", "HARI PERMAINAN", "1", 2, 1);
            ui.dashboardTrendText = MetricCard(page, "TrendCard", "TREN AKTIF", "None", 3, 1);

            RectTransform summary = PanelObject("StoreSummaryPanel", page, Panel, V(0.02f, 0.07f), V(0.72f, 0.38f));
            TextObject("Label", summary, "RINGKASAN TOKO", 12, Accent, FontStyles.Bold,
                TextAlignmentOptions.Left, V(0.035f, 0.78f), V(0.45f, 0.94f));
            ui.dashboardSummaryText = TextObject("SummaryText", summary, "-", 16, Text, FontStyles.Normal,
                TextAlignmentOptions.TopLeft, V(0.035f, 0.12f), V(0.95f, 0.75f));

            RectTransform action = PanelObject("QuickActionPanel", page, PanelSoft, V(0.74f, 0.07f), V(0.98f, 0.38f));
            TextObject("Label", action, "QUICK TEST", 12, Cyan, FontStyles.Bold,
                TextAlignmentOptions.Left, V(0.08f, 0.78f), V(0.92f, 0.94f));
            TextObject("Help", action, "Tambahkan produk dummy sebagai draft untuk menguji alur publikasi.",
                13, Text, FontStyles.Normal, TextAlignmentOptions.TopLeft,
                V(0.08f, 0.42f), V(0.92f, 0.74f));
            ui.dashboardAddDummyButton = ButtonObject("AddDummyButton", action, "+ TAMBAH PRODUK DUMMY", Cyan,
                V(0.08f, 0.12f), V(0.92f, 0.34f), Background);
            return page.gameObject;
        }

        static GameObject BuildProducts(RectTransform parent, LarisIDSceneUI ui)
        {
            RectTransform page = RectObject("ProductsPage", parent, V(0, 0), V(1, 1));
            TextObject("Title", page, "Daftar Produk", 30, Text, FontStyles.Bold,
                TextAlignmentOptions.Left, V(0.02f, 0.91f), V(0.42f, 0.98f));
            ui.productCountText = TextObject("ProductCount", page, "0 produk", 13, Muted, FontStyles.Normal,
                TextAlignmentOptions.Left, V(0.02f, 0.86f), V(0.25f, 0.91f));
            ui.productsAddDummyButton = ButtonObject("AddDummyButton", page, "+ PRODUK DUMMY", Accent,
                V(0.82f, 0.91f), V(0.98f, 0.975f));

            RectTransform header = PanelObject("TableHeader", page, PanelSoft, V(0.02f, 0.80f), V(0.98f, 0.865f));
            HorizontalLayoutGroup headerLayout = header.gameObject.AddComponent<HorizontalLayoutGroup>();
            SetupHorizontal(headerLayout, 10, 6);
            Cell("Nama", header, "PRODUK", 250, Muted, FontStyles.Bold);
            Cell("Kategori", header, "KATEGORI", 120, Muted, FontStyles.Bold);
            Cell("Harga", header, "HARGA", 120, Muted, FontStyles.Bold);
            Cell("Status", header, "STATUS", 90, Muted, FontStyles.Bold);
            Cell("Quality", header, "Q", 55, Muted, FontStyles.Bold);
            Cell("Views", header, "VIEW", 70, Muted, FontStyles.Bold);
            Cell("Clicks", header, "KLIK", 60, Muted, FontStyles.Bold);
            Cell("Sales", header, "JUAL", 60, Muted, FontStyles.Bold);
            Cell("Rating", header, "RATE", 65, Muted, FontStyles.Bold);
            Cell("Revenue", header, "PENDAPATAN", 130, Muted, FontStyles.Bold);

            ui.productListContent = ScrollArea("ProductScrollView", page, V(0.02f, 0.055f), V(0.98f, 0.79f));
            ui.productRowTemplate = ProductRowTemplate(ui.productListContent);
            return page.gameObject;
        }

        static GameObject BuildDetail(RectTransform parent, LarisIDSceneUI ui)
        {
            RectTransform page = RectObject("ProductDetailPage", parent, V(0, 0), V(1, 1));
            ui.detailBackButton = ButtonObject("BackButton", page, "< DAFTAR PRODUK", PanelSoft,
                V(0.02f, 0.925f), V(0.17f, 0.98f));
            TextObject("Title", page, "Detail Produk", 28, Text, FontStyles.Bold,
                TextAlignmentOptions.Left, V(0.20f, 0.925f), V(0.50f, 0.98f));
            ui.detailStatusText = TextObject("Status", page, "Status: Draft", 13, Cyan, FontStyles.Bold,
                TextAlignmentOptions.Right, V(0.72f, 0.93f), V(0.98f, 0.98f));
            ui.detailIdText = TextObject("ProductId", page, "ID:", 10, Muted, FontStyles.Normal,
                TextAlignmentOptions.Left, V(0.20f, 0.895f), V(0.60f, 0.93f));

            RectTransform form = PanelObject("PublishFormPanel", page, Panel, V(0.02f, 0.14f), V(0.51f, 0.88f));
            TextObject("FormTitle", form, "INFORMASI PUBLIKASI", 12, Accent, FontStyles.Bold,
                TextAlignmentOptions.Left, V(0.04f, 0.92f), V(0.50f, 0.98f));
            Label(form, "Nama Produk", .84f, .90f);
            ui.detailNameInput = InputObject("NameInput", form, "Nama produk", false, V(.04f, .76f), V(.96f, .84f));
            Label(form, "Kategori", .69f, .75f);
            ui.categoryPreviousButton = ButtonObject("CategoryPrevious", form, "<", PanelSoft, V(.04f, .61f), V(.12f, .69f));
            ui.detailCategoryText = TextObject("CategoryValue", form, "-", 15, Text, FontStyles.Bold,
                TextAlignmentOptions.Center, V(.13f, .61f), V(.87f, .69f));
            ui.categoryNextButton = ButtonObject("CategoryNext", form, ">", PanelSoft, V(.88f, .61f), V(.96f, .69f));
            Label(form, "Target Pasar", .54f, .60f);
            ui.targetPreviousButton = ButtonObject("TargetPrevious", form, "<", PanelSoft, V(.04f, .46f), V(.12f, .54f));
            ui.detailTargetText = TextObject("TargetValue", form, "-", 15, Text, FontStyles.Bold,
                TextAlignmentOptions.Center, V(.13f, .46f), V(.87f, .54f));
            ui.targetNextButton = ButtonObject("TargetNext", form, ">", PanelSoft, V(.88f, .46f), V(.96f, .54f));
            Label(form, "Deskripsi", .39f, .45f);
            ui.detailDescriptionInput = InputObject("DescriptionInput", form, "Deskripsi produk", true,
                V(.04f, .23f), V(.96f, .39f));
            Label(form, "Harga (Rp)", .16f, .22f);
            ui.priceMinusButton = ButtonObject("PriceMinusButton", form, "−", PanelSoft,
                V(.04f, .08f), V(.12f, .16f));
            ui.detailPriceInput = InputObject("PriceInput", form, "25000", false,
                V(.13f, .08f), V(.31f, .16f));
            ui.detailPriceInput.contentType = TMP_InputField.ContentType.IntegerNumber;
            ui.pricePlusButton = ButtonObject("PricePlusButton", form, "+", PanelSoft,
                V(.32f, .08f), V(.40f, .16f));
            ui.recommendedPriceText = TextObject("RecommendedPrice", form, "Rekomendasi -", 12, Cyan,
                FontStyles.Normal, TextAlignmentOptions.Left, V(.42f, .06f), V(.96f, .18f));

            RectTransform productStats = PanelObject("ProductStatsPanel", page, Panel, V(0.53f, 0.51f), V(0.75f, 0.88f));
            TextObject("Label", productStats, "STATS PRODUKLM", 12, Accent, FontStyles.Bold,
                TextAlignmentOptions.Left, V(.07f, .87f), V(.93f, .97f));
            ui.productStatsText = TextObject("StatsText", productStats, "-", 13, Text, FontStyles.Normal,
                TextAlignmentOptions.TopLeft, V(.07f, .08f), V(.93f, .83f));

            RectTransform marketStats = PanelObject("MarketStatsPanel", page, Panel, V(0.77f, 0.51f), V(0.98f, 0.88f));
            TextObject("Label", marketStats, "PERFORMA PASAR", 12, Cyan, FontStyles.Bold,
                TextAlignmentOptions.Left, V(.07f, .87f), V(.93f, .97f));
            ui.marketStatsText = TextObject("StatsText", marketStats, "-", 13, Text, FontStyles.Normal,
                TextAlignmentOptions.TopLeft, V(.07f, .08f), V(.93f, .83f));

            RectTransform reviews = PanelObject("ReviewsPanel", page, Panel, V(0.53f, 0.14f), V(0.98f, 0.48f));
            TextObject("Label", reviews, "ULASAN PEMBELI", 12, Muted, FontStyles.Bold,
                TextAlignmentOptions.Left, V(.035f, .86f), V(.50f, .97f));
            ui.reviewContent = ScrollArea("ReviewScrollView", reviews, V(.03f, .06f), V(.97f, .84f));
            ui.reviewRowTemplate = TextTemplate("ReviewRowTemplate", ui.reviewContent,
                "Hari 1 | Rating 5/5\nUlasan tampil di sini.", 12, 62);

            RectTransform actions = RectObject("ActionButtons", page, V(.02f, .035f), V(.98f, .12f));
            ui.publishButton = ButtonObject("PublishButton", actions, "PUBLIKASIKAN", Cyan, V(0, .08f), V(.20f, .92f), Background);
            ui.promoteButton = ButtonObject("PromoteButton", actions, "PROMOSIKAN", Accent, V(.215f, .08f), V(.46f, .92f));
            ui.promoteButtonText = ui.promoteButton.GetComponentInChildren<TMP_Text>();
            ui.archiveButton = ButtonObject("ArchiveButton", actions, "ARSIPKAN", Danger, V(.475f, .08f), V(.64f, .92f));
            ui.reactivateButton = ButtonObject("ReactivateButton", actions, "AKTIFKAN KEMBALI", Cyan,
                V(.655f, .08f), V(.88f, .92f), Background);
            return page.gameObject;
        }

        static GameObject BuildPromotion(RectTransform parent, LarisIDSceneUI ui)
        {
            RectTransform page = RectObject("PromotionPage", parent, V(0, 0), V(1, 1));
            TextObject("Title", page, "Pusat Promosi", 30, Text, FontStyles.Bold,
                TextAlignmentOptions.Left, V(.02f, .91f), V(.48f, .98f));
            TextObject("Subtitle", page,
                "Pilih produk aktif dan kreator. Penawaran berganti setiap hari.",
                13, Muted, FontStyles.Normal, TextAlignmentOptions.Left,
                V(.02f, .86f), V(.72f, .92f));

            RectTransform productPanel = PanelObject(
                "PromotionProductPanel", page, Panel, V(.02f, .72f), V(.98f, .84f));
            TextObject("Label", productPanel, "PRODUK YANG DIPROMOSIKAN", 10, Muted,
                FontStyles.Bold, TextAlignmentOptions.Left, V(.025f, .68f), V(.34f, .92f));
            ui.promotionProductPreviousButton = ButtonObject(
                "PreviousProductButton", productPanel, "−", PanelSoft,
                V(.025f, .15f), V(.085f, .62f));
            ui.promotionSelectedProductText = TextObject(
                "SelectedProduct", productPanel, "Belum ada produk aktif", 18, Text,
                FontStyles.Bold, TextAlignmentOptions.Left, V(.105f, .30f), V(.54f, .66f));
            ui.promotionProductStatusText = TextObject(
                "ProductStatus", productPanel, "-", 11, Cyan,
                FontStyles.Normal, TextAlignmentOptions.Left, V(.105f, .08f), V(.82f, .31f));
            ui.promotionProductNextButton = ButtonObject(
                "NextProductButton", productPanel, "+", PanelSoft,
                V(.84f, .15f), V(.90f, .62f));
            ui.promotionEmptyText = TextObject(
                "EmptyProductText", productPanel, "PUBLIKASIKAN PRODUK DULU", 10, Danger,
                FontStyles.Bold, TextAlignmentOptions.Center, V(.90f, .15f), V(.985f, .62f));

            RectTransform offerPanel = PanelObject(
                "DailyPromoterOffersPanel", page, Panel, V(.02f, .07f), V(.60f, .69f));
            TextObject("Label", offerPanel, "PENAWARAN PROMOTOR HARI INI", 11, Accent,
                FontStyles.Bold, TextAlignmentOptions.Left, V(.035f, .91f), V(.62f, .98f));
            ui.promotionOfferCountText = TextObject(
                "OfferCount", offerPanel, "6 penawaran tersedia", 10, Muted,
                FontStyles.Normal, TextAlignmentOptions.Right, V(.56f, .91f), V(.965f, .98f));
            ui.promotionOfferContent = ScrollArea(
                "PromotionOfferScrollView", offerPanel, V(.025f, .035f), V(.975f, .89f));
            ui.promotionOfferRowTemplate =
                PromotionOfferRowTemplate(ui.promotionOfferContent);

            RectTransform checkout = PanelObject(
                "PromotionCheckoutPanel", page, PanelSoft, V(.62f, .07f), V(.98f, .69f));
            TextObject("Label", checkout, "RINGKASAN PROMOSI", 11, Cyan,
                FontStyles.Bold, TextAlignmentOptions.Left, V(.07f, .91f), V(.93f, .98f));
            ui.promotionSelectedOfferText = TextObject(
                "SelectedOffer", checkout,
                "Pilih salah satu promotor dari daftar.", 15, Text,
                FontStyles.Normal, TextAlignmentOptions.TopLeft, V(.07f, .23f), V(.93f, .88f));
            ui.confirmPromotionButton = ButtonObject(
                "ConfirmPromotionButton", checkout, "JALANKAN PROMOSI", Cyan,
                V(.07f, .07f), V(.93f, .19f), Background);
            return page.gameObject;
        }

        static GameObject BuildAnalytics(RectTransform parent, LarisIDSceneUI ui)
        {
            RectTransform page = RectObject("AnalyticsPage", parent, V(0, 0), V(1, 1));
            TextObject("Title", page, "Analitik Toko", 30, Text, FontStyles.Bold,
                TextAlignmentOptions.Left, V(.02f, .91f), V(.50f, .98f));
            TextObject("Subtitle", page, "Gunakan data ini untuk mengevaluasi harga, klik, dan konversi.",
                13, Muted, FontStyles.Normal, TextAlignmentOptions.Left, V(.02f, .86f), V(.70f, .92f));
            RectTransform panel = PanelObject("AnalyticsPanel", page, Panel, V(.02f, .07f), V(.98f, .84f));
            ui.analyticsText = TextObject("AnalyticsText", panel, "-", 17, Text, FontStyles.Normal,
                TextAlignmentOptions.TopLeft, V(.04f, .06f), V(.96f, .94f));
            return page.gameObject;
        }

        static GameObject BuildDailySummary(RectTransform parent, LarisIDSceneUI ui)
        {
            RectTransform page = RectObject("DailySummaryPage", parent, V(0, 0), V(1, 1));
            ui.dailySummaryTitleText = TextObject("Title", page, "Belum Ada Simulasi", 30, Text,
                FontStyles.Bold, TextAlignmentOptions.Left, V(.02f, .91f), V(.60f, .98f));
            RectTransform totals = PanelObject("DailyTotalsPanel", page, PanelSoft, V(.02f, .73f), V(.98f, .88f));
            ui.dailySummaryTotalsText = TextObject("TotalsText", totals,
                "Tekan tombol SIMULASIKAN 1 HARI untuk menjalankan pasar.", 17, Cyan,
                FontStyles.Bold, TextAlignmentOptions.Left, V(.035f, .10f), V(.965f, .90f));
            TextObject("ListLabel", page, "HASIL PER PRODUK", 12, Muted, FontStyles.Bold,
                TextAlignmentOptions.Left, V(.02f, .66f), V(.40f, .71f));
            ui.dailyProductResultContent = ScrollArea("DailyResultScrollView", page, V(.02f, .06f), V(.98f, .65f));
            ui.dailyProductResultTemplate = TextTemplate("DailyProductResultTemplate",
                ui.dailyProductResultContent, "Nama produk\n+0 tayangan | +0 klik | +0 terjual | +Rp 0", 14, 66);
            return page.gameObject;
        }

        static TMP_Text MetricCard(Transform parent, string name, string label, string initial, int column, int row)
        {
            float gap = .015f;
            float width = (.96f - gap * 3) / 4f;
            float xMin = .02f + column * (width + gap);
            float xMax = xMin + width;
            float yMax = row == 0 ? .82f : .59f;
            float yMin = yMax - .18f;
            RectTransform card = PanelObject(name, parent, row == 0 ? Panel : PanelSoft,
                V(xMin, yMin), V(xMax, yMax));
            TextObject("Label", card, label, 10, Muted, FontStyles.Bold,
                TextAlignmentOptions.Left, V(.08f, .65f), V(.92f, .90f));
            return TextObject("Value", card, initial, 22, column == 0 ? Cyan : Text, FontStyles.Bold,
                TextAlignmentOptions.Left, V(.08f, .13f), V(.92f, .65f));
        }

        static LarisProductRowUI ProductRowTemplate(Transform parent)
        {
            RectTransform row = PanelObject("ProductRowTemplate", parent, Panel, V(0, 0), V(1, 0));
            LayoutElement layout = row.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 58;
            layout.minHeight = 58;
            HorizontalLayoutGroup group = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            SetupHorizontal(group, 10, 6);

            Button button = row.gameObject.AddComponent<Button>();
            button.targetGraphic = row.GetComponent<Image>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.80f, 0.82f, 1f);
            colors.pressedColor = new Color(0.65f, 0.70f, 0.90f);
            button.colors = colors;

            LarisProductRowUI view = row.gameObject.AddComponent<LarisProductRowUI>();
            view.productNameText = Cell("ProductName", row, "Nama Produk", 250, Text, FontStyles.Bold);
            view.categoryText = Cell("Category", row, "-", 120, Muted);
            view.priceText = Cell("Price", row, "Rp 0", 120, Text);
            view.statusText = Cell("Status", row, "Draft", 90, Cyan, FontStyles.Bold);
            view.qualityText = Cell("Quality", row, "0", 55, Text);
            view.impressionsText = Cell("Impressions", row, "0", 70, Text);
            view.clicksText = Cell("Clicks", row, "0", 60, Text);
            view.salesText = Cell("Sales", row, "0", 60, Text);
            view.ratingText = Cell("Rating", row, "-", 65, Text);
            view.revenueText = Cell("Revenue", row, "Rp 0", 130, Cyan);
            view.detailButton = button;
            row.gameObject.SetActive(false);
            return view;
        }

        static PromotionOfferRowUI PromotionOfferRowTemplate(Transform parent)
        {
            RectTransform row = PanelObject(
                "PromotionOfferRowTemplate", parent, PanelSoft, V(0, 0), V(1, 0));
            LayoutElement layout = row.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 76;
            layout.minHeight = 76;

            PromotionOfferRowUI view = row.gameObject.AddComponent<PromotionOfferRowUI>();
            view.platformText = TextObject(
                "Platform", row, "YOUTUBE", 10, Cyan, FontStyles.Bold,
                TextAlignmentOptions.Left, V(.025f, .61f), V(.23f, .91f));
            view.promoterNameText = TextObject(
                "PromoterName", row, "Nama Promotor", 15, Text, FontStyles.Bold,
                TextAlignmentOptions.Left, V(.025f, .24f), V(.58f, .64f));
            view.offerDetailText = TextObject(
                "OfferDetail", row, "Rp 0 • 0 hari • +0% tayangan", 10, Muted,
                FontStyles.Normal, TextAlignmentOptions.Left, V(.025f, .04f), V(.76f, .28f));
            view.selectButton = ButtonObject(
                "SelectOfferButton", row, "PILIH", Panel,
                V(.80f, .18f), V(.97f, .82f));
            row.gameObject.SetActive(false);
            return view;
        }

        static TMP_Text TextTemplate(string name, Transform parent, string value, float size, float height)
        {
            TMP_Text text = TextObject(name, parent, value, size, Text, FontStyles.Normal,
                TextAlignmentOptions.TopLeft, V(0, 0), V(1, 0));
            LayoutElement layout = text.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = height;
            layout.minHeight = height;
            text.gameObject.SetActive(false);
            return text;
        }

        static Transform ScrollArea(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            RectTransform root = PanelObject(name, parent, PanelSoft, anchorMin, anchorMax);
            ScrollRect scroll = root.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 24;

            RectTransform viewport = PanelObject("Viewport", root, Color.clear, V(.012f, .012f), V(.988f, .988f));
            viewport.gameObject.AddComponent<RectMask2D>();
            viewport.GetComponent<Image>().raycastTarget = true;
            RectTransform content = RectObject("Content", viewport, V(0, 1), V(1, 1));
            content.pivot = new Vector2(.5f, 1);
            content.sizeDelta = new Vector2(0, 0);
            VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(4, 4, 4, 4);
            layout.spacing = 6;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport;
            scroll.content = content;
            return content;
        }

        static TMP_InputField InputObject(
            string name,
            Transform parent,
            string placeholderValue,
            bool multiline,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            RectTransform root = PanelObject(name, parent, PanelSoft, anchorMin, anchorMax);
            root.GetComponent<Image>().raycastTarget = true;
            TMP_InputField input = root.gameObject.AddComponent<TMP_InputField>();
            input.lineType = multiline ? TMP_InputField.LineType.MultiLineNewline : TMP_InputField.LineType.SingleLine;
            input.selectionColor = new Color(Accent.r, Accent.g, Accent.b, .45f);
            input.caretColor = Cyan;

            RectTransform viewport = RectObject("Text Area", root, V(.035f, .10f), V(.965f, .90f));
            viewport.gameObject.AddComponent<RectMask2D>();
            TMP_Text placeholder = TextObject("Placeholder", viewport, placeholderValue, 13, Muted,
                FontStyles.Italic, TextAlignmentOptions.Left, V(0, 0), V(1, 1));
            TMP_Text text = TextObject("Text", viewport, "", 13, Text, FontStyles.Normal,
                multiline ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.Left, V(0, 0), V(1, 1));
            text.textWrappingMode = TextWrappingModes.Normal;
            input.textViewport = viewport;
            input.textComponent = text;
            input.placeholder = placeholder;
            return input;
        }

        static Button ButtonObject(
            string name,
            Transform parent,
            string label,
            Color background,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Color? foreground = null)
        {
            RectTransform rect = PanelObject(name, parent, background, anchorMin, anchorMax);
            Image image = rect.GetComponent<Image>();
            image.raycastTarget = true;
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1);
            colors.pressedColor = new Color(.75f, .75f, .82f, 1);
            colors.disabledColor = new Color(.55f, .55f, .60f, .55f);
            button.colors = colors;
            TextObject("Label", rect, label, 12, foreground ?? Text, FontStyles.Bold,
                TextAlignmentOptions.Center, V(.04f, .08f), V(.96f, .92f));
            return button;
        }

        static void Label(Transform parent, string value, float yMin, float yMax)
        {
            TextObject(value.Replace(" ", "") + "Label", parent, value.ToUpperInvariant(), 10, Muted,
                FontStyles.Bold, TextAlignmentOptions.Left, V(.04f, yMin), V(.96f, yMax));
        }

        static TMP_Text Cell(
            string name,
            Transform parent,
            string value,
            float width,
            Color color,
            FontStyles style = FontStyles.Normal)
        {
            TMP_Text text = TextObject(name, parent, value, 11, color, style,
                TextAlignmentOptions.MidlineLeft, V(0, 0), V(1, 1));
            LayoutElement layout = text.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = width;
            layout.minWidth = width;
            return text;
        }

        static TMP_Text TextObject(
            string name,
            Transform parent,
            string value,
            float size,
            Color color,
            FontStyles style,
            TextAlignmentOptions alignment,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            RectTransform rect = RectObject(name, parent, anchorMin, anchorMax);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.fontStyle = style;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Ellipsis;
            return text;
        }

        static RectTransform PanelObject(
            string name,
            Transform parent,
            Color color,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            RectTransform rect = RectObject(name, parent, anchorMin, anchorMax);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }

        static RectTransform RectObject(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
            return rect;
        }

        static void SetupHorizontal(HorizontalLayoutGroup group, int horizontalPadding, int spacing)
        {
            group.padding = new RectOffset(horizontalPadding, horizontalPadding, 4, 4);
            group.spacing = spacing;
            group.childAlignment = TextAnchor.MiddleLeft;
            group.childControlWidth = false;
            group.childControlHeight = true;
            group.childForceExpandWidth = false;
            group.childForceExpandHeight = true;
        }

        static GameObject FindRoot(Scene scene, string name)
        {
            return scene.GetRootGameObjects().FirstOrDefault(go => go.name == name);
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        static Vector2 V(float x, float y) => new Vector2(x, y);

        static Color Hex(string value)
        {
            ColorUtility.TryParseHtmlString(value, out Color color);
            return color;
        }

        static void EnsureInBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            int index = scenes.FindIndex(item => item.path == ScenePath);
            if (index >= 0)
            {
                if (!scenes[index].enabled)
                {
                    scenes[index] = new EditorBuildSettingsScene(ScenePath, true);
                    EditorBuildSettings.scenes = scenes.ToArray();
                }
                return;
            }

            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
