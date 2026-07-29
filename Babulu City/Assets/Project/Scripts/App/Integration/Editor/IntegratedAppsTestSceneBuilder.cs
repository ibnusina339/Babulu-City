using System.Collections.Generic;
using System.Linq;
using IntegratedApps;
using LarisID;
using ProdukLM;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace IntegratedAppsEditor
{
    [InitializeOnLoad]
    public static class IntegratedAppsTestSceneBuilder
    {
        public const string ScenePath = "Assets/Project/Scenes/ProdukLM_LarisID_Test.unity";
        public const string LarisSourceScenePath = "Assets/Project/Scenes/LarisID_Test.unity";
        public const string LarisPrefabPath = "Assets/Project/Prefabs/UI/LarisIDFullUI.prefab";

        static readonly Color Desktop = Hex("#091225");
        static readonly Color DesktopGlow = Hex("#102A46");
        static readonly Color Window = Hex("#0E1528");
        static readonly Color Panel = Hex("#192036");
        static readonly Color PanelSoft = Hex("#222B47");
        static readonly Color Accent = Hex("#6C63FF");
        static readonly Color Cyan = Hex("#31D2BE");
        static readonly Color Text = Hex("#F4F7FF");
        static readonly Color Muted = Hex("#A4B0CC");
        static readonly Color Danger = Hex("#D55D73");
        static readonly Color Gold = Hex("#F4BD61");

        static IntegratedAppsTestSceneBuilder()
        {
            EditorApplication.delayCall += CreateIfMissing;
        }

        [MenuItem("Tools/BRIDA/Create ProdukLM + Laris.ID Desktop Test")]
        public static void BuildFromMenu() => Build(true);

        [MenuItem("Tools/BRIDA/Rebuild Laris.ID Full UI Prefab")]
        public static void RebuildLarisPrefabFromMenu()
        {
            EnsureLarisUIPrefab(true);
            Build(true);
        }

        static void CreateIfMissing()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
                Build(false);
        }

        static void Build(bool showLog)
        {
            GameObject larisPrefab = EnsureLarisUIPrefab(false);
            bool assetExists = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null;
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool opened = !scene.IsValid() || !scene.isLoaded;

            if (opened)
            {
                scene = assetExists
                    ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive)
                    : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            }

            bool changed = EnsureScene(scene, larisPrefab);
            if (!assetExists)
            {
                EditorSceneManager.SaveScene(scene, ScenePath);
                changed = false;
            }
            else if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            EnsureBuildSettings();
            if (opened)
                EditorSceneManager.CloseScene(scene, true);
            AssetDatabase.SaveAssets();

            if (showLog)
                Debug.Log($"Scene integrasi desktop siap: {ScenePath}");
        }

        static GameObject EnsureLarisUIPrefab(bool rebuild)
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(LarisPrefabPath);
            if (existing != null && !rebuild)
                return existing;

            Scene sourceScene = SceneManager.GetSceneByPath(LarisSourceScenePath);
            bool opened = !sourceScene.IsValid() || !sourceScene.isLoaded;
            if (opened)
                sourceScene = EditorSceneManager.OpenScene(LarisSourceScenePath, OpenSceneMode.Additive);

            GameObject sourceRoot = FindRoot(sourceScene, "LarisID_UIRoot");
            if (sourceRoot == null)
            {
                if (opened)
                    EditorSceneManager.CloseScene(sourceScene, true);
                Debug.LogError($"LarisID_UIRoot tidak ditemukan di {LarisSourceScenePath}.");
                return existing;
            }

            bool sourceChanged = EnsurePriceStepButtons(sourceRoot);
            if (sourceChanged)
            {
                EditorSceneManager.MarkSceneDirty(sourceScene);
                EditorSceneManager.SaveScene(sourceScene);
            }

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(sourceRoot, LarisPrefabPath);
            if (opened)
                EditorSceneManager.CloseScene(sourceScene, true);
            return prefab;
        }

        static bool EnsurePriceStepButtons(GameObject sourceRoot)
        {
            LarisIDSceneUI ui = sourceRoot.GetComponent<LarisIDSceneUI>();
            if (ui == null || ui.detailPriceInput == null)
                return false;

            bool changed = false;
            Transform parent = ui.detailPriceInput.transform.parent;
            RectTransform priceRect = ui.detailPriceInput.GetComponent<RectTransform>();
            Vector2 expectedMin = V(.13f, .08f);
            Vector2 expectedMax = V(.31f, .16f);
            if (priceRect.anchorMin != expectedMin || priceRect.anchorMax != expectedMax)
            {
                priceRect.anchorMin = expectedMin;
                priceRect.anchorMax = expectedMax;
                priceRect.offsetMin = Vector2.zero;
                priceRect.offsetMax = Vector2.zero;
                changed = true;
            }

            if (ui.priceMinusButton == null)
            {
                ui.priceMinusButton = ButtonObject("PriceMinusButton", parent, "−", PanelSoft,
                    V(.04f, .08f), V(.12f, .16f));
                changed = true;
            }
            if (ui.pricePlusButton == null)
            {
                ui.pricePlusButton = ButtonObject("PricePlusButton", parent, "+", PanelSoft,
                    V(.32f, .08f), V(.40f, .16f));
                changed = true;
            }
            if (ui.recommendedPriceText != null)
            {
                RectTransform recommendedRect = ui.recommendedPriceText.rectTransform;
                if (recommendedRect.anchorMin.x != .42f)
                {
                    recommendedRect.anchorMin = V(.42f, .06f);
                    recommendedRect.anchorMax = V(.96f, .18f);
                    recommendedRect.offsetMin = Vector2.zero;
                    recommendedRect.offsetMax = Vector2.zero;
                    changed = true;
                }
            }

            if (changed)
                EditorUtility.SetDirty(ui);
            return changed;
        }

        static bool EnsureScene(Scene scene, GameObject larisPrefab)
        {
            bool changed = false;
            GameObject system = FindRoot(scene, "IntegratedApps_System");
            if (system == null)
            {
                system = new GameObject("IntegratedApps_System");
                SceneManager.MoveGameObjectToScene(system, scene);
                changed = true;
            }

            LarisIDManager larisManager = system.GetComponent<LarisIDManager>();
            if (larisManager == null)
            {
                larisManager = system.AddComponent<LarisIDManager>();
                changed = true;
            }

            if (FindRoot(scene, "EventSystem") == null)
            {
                GameObject eventSystem = new GameObject(
                    "EventSystem",
                    typeof(EventSystem),
                    typeof(InputSystemUIInputModule));
                SceneManager.MoveGameObjectToScene(eventSystem, scene);
                eventSystem.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
                changed = true;
            }

            GameObject cameraObject = FindRoot(scene, "Main Camera");
            if (cameraObject == null)
            {
                cameraObject = new GameObject("Main Camera", typeof(Camera));
                SceneManager.MoveGameObjectToScene(cameraObject, scene);
                cameraObject.tag = "MainCamera";
                cameraObject.transform.position = new Vector3(0, 0, -10);
                changed = true;
            }
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Desktop;

            GameObject desktop = FindRoot(scene, "BRIDA_Desktop_UI");
            if (desktop == null)
            {
                BuildDesktop(scene, larisManager, larisPrefab);
                changed = true;
            }
            else
            {
                IntegratedDesktopUI ui = desktop.GetComponent<IntegratedDesktopUI>();
                if (ui != null &&
                    ui.plusUpgradePrice == 350000 &&
                    ui.proUpgradePrice == 1250000)
                {
                    ui.plusUpgradePrice = 1500000;
                    ui.proUpgradePrice = 5000000;
                    EditorUtility.SetDirty(ui);
                    changed = true;
                }
                Transform prefabHost = desktop.transform.Find(
                    "LarisID_Window/WindowContent/LarisIDFullUIPrefabHost");
                if (ui != null && larisPrefab != null && prefabHost == null)
                {
                    GameObject oldWindow = ui.larisIDWindow;
                    if (oldWindow == null)
                    {
                        Transform foundWindow = desktop.transform.Find("LarisID_Window");
                        if (foundWindow != null)
                            oldWindow = foundWindow.gameObject;
                    }
                    if (oldWindow != null)
                        Object.DestroyImmediate(oldWindow);

                    ui.larisManager = larisManager;
                    ui.larisIDWindow = BuildLarisPrefabWindow(desktop.transform, ui, larisPrefab);
                    ui.larisIDWindow.SetActive(false);
                    EditorUtility.SetDirty(ui);
                    changed = true;
                }
                else if (ui != null && prefabHost != null)
                {
                    if (PrepareEmbeddedLarisUI(prefabHost.gameObject))
                        changed = true;
                    LarisIDSceneUI larisUI = prefabHost.GetComponent<LarisIDSceneUI>();
                    if (larisUI != null && larisUI.manager != larisManager)
                    {
                        larisUI.manager = larisManager;
                        EditorUtility.SetDirty(larisUI);
                        changed = true;
                    }
                }
            }
            return changed;
        }

        static void BuildDesktop(Scene scene, LarisIDManager larisManager, GameObject larisPrefab)
        {
            GameObject root = new GameObject(
                "BRIDA_Desktop_UI",
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
            scaler.matchWidthOrHeight = .5f;
            Image background = root.AddComponent<Image>();
            background.color = Desktop;
            background.raycastTarget = false;
            Stretch(root.GetComponent<RectTransform>());

            RectTransform wallpaperGlow = PanelObject("WallpaperGlow", root.transform, DesktopGlow,
                V(.55f, .08f), V(.98f, .72f));
            wallpaperGlow.GetComponent<Image>().raycastTarget = false;
            TextObject("DesktopBrand", root.transform, "BRIDA", 80,
                new Color(1, 1, 1, .045f), FontStyles.Bold, TextAlignmentOptions.BottomRight,
                V(.55f, .06f), V(.96f, .25f));
            TextObject("DesktopSubtitle", root.transform, "DIGITAL CREATOR DESKTOP", 15,
                new Color(1, 1, 1, .18f), FontStyles.Bold, TextAlignmentOptions.BottomRight,
                V(.60f, .04f), V(.96f, .09f));

            RectTransform taskbar = PanelObject("Taskbar", root.transform, Panel, V(0, .93f), V(1, 1));
            TextObject("StartBadge", taskbar, "B", 24, Cyan, FontStyles.Bold,
                TextAlignmentOptions.Center, V(.015f, .12f), V(.055f, .88f));
            TextObject("DesktopTitle", taskbar, "BRIDA OS  •  Testing Produk Digital", 14, Text,
                FontStyles.Bold, TextAlignmentOptions.Left, V(.065f, .12f), V(.45f, .88f));
            TextObject("TestingBadge", taskbar, "INTEGRATION TEST", 11, Accent,
                FontStyles.Bold, TextAlignmentOptions.Center, V(.82f, .18f), V(.94f, .82f));
            TextObject("ClockPlaceholder", taskbar, "09:41", 13, Text,
                FontStyles.Normal, TextAlignmentOptions.Center, V(.94f, .12f), V(.995f, .88f));

            IntegratedDesktopUI ui = root.AddComponent<IntegratedDesktopUI>();
            ui.larisManager = larisManager;

            ui.openProdukLMButton = DesktopIcon(
                "ProdukLM_DesktopIcon",
                root.transform,
                "P",
                "ProdukLM",
                Accent,
                V(.035f, .69f),
                V(.125f, .90f),
                out Image produkIcon);
            ui.produkLMDesktopIcon = produkIcon;

            ui.openLarisIDButton = DesktopIcon(
                "LarisID_DesktopIcon",
                root.transform,
                "L",
                "Laris.ID",
                Cyan,
                V(.035f, .44f),
                V(.125f, .65f),
                out Image larisIcon);
            ui.larisIDDesktopIcon = larisIcon;

            RectTransform hint = PanelObject("DesktopHint", root.transform, new Color(0, 0, 0, .25f),
                V(.025f, .07f), V(.28f, .20f));
            TextObject("HintText", hint,
                "Klik ikon untuk membuka aplikasi.\nIcon Image dapat diganti lewat Inspector.",
                13, Muted, FontStyles.Normal, TextAlignmentOptions.Left,
                V(.06f, .12f), V(.94f, .88f));

            ui.produkLMWindow = BuildProdukLMWindow(root.transform, ui);
            ui.larisIDWindow = BuildLarisPrefabWindow(root.transform, ui, larisPrefab);
            ui.produkLMWindow.SetActive(false);
            ui.larisIDWindow.SetActive(false);

            ui.cardCatalog = AssetDatabase.FindAssets("t:CardData")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<CardData>)
                .Where(card => card != null)
                .OrderBy(card => (int)card.slotType)
                .ThenBy(card => card.displayName)
                .ToList();

            Selection.activeGameObject = root;
        }

        static GameObject BuildProdukLMWindow(Transform desktop, IntegratedDesktopUI ui)
        {
            RectTransform window = WindowObject(
                "ProdukLM_Window",
                desktop,
                "ProdukLM",
                "AI PRODUCT GENERATOR",
                Accent,
                out RectTransform content,
                out Button close);
            ui.closeProdukLMButton = close;

            RectTransform aiPanel = PanelObject("AITierPanel", content, Panel, V(.025f, .80f), V(.975f, .965f));
            TextObject("Title", aiPanel, "MODEL AI", 11, Muted, FontStyles.Bold,
                TextAlignmentOptions.Left, V(.025f, .70f), V(.14f, .93f));
            ui.freeTierButton = ButtonObject("FreeTierButton", aiPanel, "AI FREE", Accent,
                V(.02f, .17f), V(.14f, .66f));
            ui.plusTierButton = ButtonObject("PlusTierButton", aiPanel, "AI PLUS", PanelSoft,
                V(.15f, .17f), V(.27f, .66f));
            ui.proTierButton = ButtonObject("ProTierButton", aiPanel, "AI PRO", PanelSoft,
                V(.28f, .17f), V(.40f, .66f));
            ui.tierDescriptionText = TextObject("TierDescription", aiPanel,
                "FREE • kualitas dasar", 12, Text, FontStyles.Normal,
                TextAlignmentOptions.Left, V(.43f, .42f), V(.73f, .82f));
            ui.dailyLimitText = TextObject("DailyLimit", aiPanel, "Sisa hari ini 5/5", 17, Cyan,
                FontStyles.Bold, TextAlignmentOptions.Center, V(.73f, .18f), V(.88f, .82f));
            ui.resetLimitTestButton = ButtonObject("ResetLimitTestButton", aiPanel, "RESET LIMIT (TEST)",
                PanelSoft, V(.885f, .18f), V(.98f, .82f));

            RectTransform builder = PanelObject("PromptBuilderPanel", content, Panel, V(.025f, .055f), V(.505f, .775f));
            TextObject("Title", builder, "PROMPT BUILDER", 17, Text, FontStyles.Bold,
                TextAlignmentOptions.Left, V(.04f, .92f), V(.50f, .98f));
            TextObject("Help", builder, "Gunakan tombol -/+ untuk mengganti pilihan kartu.",
                11, Muted, FontStyles.Normal, TextAlignmentOptions.Right,
                V(.45f, .925f), V(.96f, .975f));

            var selectors = new List<SlotSelectorReferences>();
            for (int i = 0; i < 6; i++)
            {
                float yMax = .90f - i * .105f;
                float yMin = yMax - .085f;
                SlotType slot = (SlotType)i;
                RectTransform row = PanelObject($"Slot_{slot}", builder, PanelSoft, V(.04f, yMin), V(.96f, yMax));
                TextObject("SlotLabel", row, SlotLabel(slot), 10, Muted, FontStyles.Bold,
                    TextAlignmentOptions.Left, V(.025f, .18f), V(.22f, .82f));
                Button previous = ButtonObject("PreviousButton", row, "−", Window,
                    V(.23f, .14f), V(.30f, .86f));
                TMP_Text value = TextObject("SelectedCard", row, "-", 12, Text, FontStyles.Bold,
                    TextAlignmentOptions.Center, V(.31f, .10f), V(.91f, .90f));
                Button next = ButtonObject("NextButton", row, "+", Window,
                    V(.92f, .14f), V(.985f, .86f));
                selectors.Add(new SlotSelectorReferences
                {
                    slotType = slot,
                    previousButton = previous,
                    nextButton = next,
                    valueText = value
                });
            }
            ui.slotSelectors = selectors.ToArray();

            TextObject("PromptLabel", builder, "LIVE PROMPT PREVIEW", 10, Cyan, FontStyles.Bold,
                TextAlignmentOptions.Left, V(.04f, .18f), V(.35f, .22f));
            RectTransform preview = PanelObject("PromptPreview", builder, Window, V(.04f, .075f), V(.96f, .18f));
            ui.promptPreviewText = TextObject("PromptText", preview, "Prompt akan tampil di sini.", 10,
                Muted, FontStyles.Normal, TextAlignmentOptions.TopLeft, V(.025f, .10f), V(.975f, .90f));
            ui.generateButton = ButtonObject("GenerateButton", builder, "GENERATE PRODUK", Accent,
                V(.04f, .008f), V(.42f, .065f));
            ui.produkStatusText = TextObject("StatusText", builder, "Pilih kombinasi dan tekan Generate.",
                10, Muted, FontStyles.Normal, TextAlignmentOptions.Left,
                V(.45f, .008f), V(.96f, .065f));

            RectTransform result = PanelObject("GeneratedResultPanel", content, Panel, V(.52f, .055f), V(.975f, .775f));
            TextObject("Title", result, "HASIL GENERATE", 17, Text, FontStyles.Bold,
                TextAlignmentOptions.Left, V(.04f, .92f), V(.55f, .98f));
            TextObject("LimitWarning", result, "Limit terpakai saat Generate, bukan saat Save.", 10, Gold,
                FontStyles.Bold, TextAlignmentOptions.Right, V(.45f, .93f), V(.96f, .975f));

            ui.resultEmptyState = RectObject("ResultEmptyState", result, V(.04f, .08f), V(.96f, .90f)).gameObject;
            TextObject("EmptyIcon", ui.resultEmptyState.transform, "AI", 54, Accent, FontStyles.Bold,
                TextAlignmentOptions.Center, V(.30f, .48f), V(.70f, .70f));
            TextObject("EmptyTitle", ui.resultEmptyState.transform, "Belum ada produk", 20, Text,
                FontStyles.Bold, TextAlignmentOptions.Center, V(.15f, .38f), V(.85f, .49f));
            TextObject("EmptyHelp", ui.resultEmptyState.transform,
                "Hasil AI beserta icon file contoh akan muncul setelah Generate.",
                12, Muted, FontStyles.Normal, TextAlignmentOptions.Center,
                V(.15f, .26f), V(.85f, .38f));

            ui.resultContent = RectObject("ResultContent", result, V(.04f, .04f), V(.96f, .91f)).gameObject;
            RectTransform fileIcon = PanelObject("GeneratedFileIcon", ui.resultContent.transform, Danger,
                V(.02f, .74f), V(.16f, .96f));
            ui.generatedFileIcon = fileIcon.GetComponent<Image>();
            ui.generatedFileTypeText = TextObject("FileType", fileIcon, "PDF", 17, Text, FontStyles.Bold,
                TextAlignmentOptions.Center, V(.08f, .08f), V(.92f, .92f));
            ui.generatedProductTypeText = TextObject("ProductType", ui.resultContent.transform,
                "Produk Digital", 20, Text, FontStyles.Bold, TextAlignmentOptions.Left,
                V(.19f, .84f), V(.70f, .96f));
            ui.generatedTierText = TextObject("Tier", ui.resultContent.transform, "Dibuat dengan AI Free",
                11, Cyan, FontStyles.Bold, TextAlignmentOptions.Left, V(.19f, .76f), V(.70f, .84f));

            ui.generatedQualityText = ResultMetric(ui.resultContent.transform, "QualityMetric", "QUALITY", .02f);
            ui.generatedRelevanceText = ResultMetric(ui.resultContent.transform, "RelevanceMetric", "RELEVANSI", .35f);
            ui.generatedSellValueText = ResultMetric(ui.resultContent.transform, "SellValueMetric", "NILAI JUAL", .68f);

            TextObject("PromptLabel", ui.resultContent.transform, "PROMPT HASIL", 10, Muted,
                FontStyles.Bold, TextAlignmentOptions.Left, V(.02f, .48f), V(.30f, .53f));
            RectTransform resultPrompt = PanelObject("GeneratedPrompt", ui.resultContent.transform, Window,
                V(.02f, .33f), V(.98f, .48f));
            ui.generatedPromptText = TextObject("PromptText", resultPrompt, "-", 11, Text,
                FontStyles.Normal, TextAlignmentOptions.TopLeft, V(.025f, .08f), V(.975f, .92f));

            TextObject("SaveLabel", ui.resultContent.transform, "NAMA PRODUK UNTUK DISIMPAN", 10, Muted,
                FontStyles.Bold, TextAlignmentOptions.Left, V(.02f, .26f), V(.55f, .31f));
            ui.saveProductNameInput = InputObject("ProductNameInput", ui.resultContent.transform,
                "Contoh: Planner Belajar Hebat", false, V(.02f, .18f), V(.98f, .26f));
            ui.saveToLarisButton = ButtonObject("SaveToLarisButton", ui.resultContent.transform,
                "SAVE KE LIBRARY LARIS.ID", Cyan, V(.02f, .095f), V(.59f, .165f), Window);
            ui.discardResultButton = ButtonObject("DiscardResultButton", ui.resultContent.transform,
                "BUANG HASIL", Danger, V(.61f, .095f), V(.98f, .165f));
            ui.saveMessageText = TextObject("SaveMessage", ui.resultContent.transform,
                "Isi nama produk untuk menyimpan.", 10, Gold, FontStyles.Normal,
                TextAlignmentOptions.Left, V(.02f, .01f), V(.98f, .085f));
            ui.resultContent.SetActive(false);
            return window.gameObject;
        }

        static GameObject BuildLarisPrefabWindow(
            Transform desktop,
            IntegratedDesktopUI ui,
            GameObject larisPrefab)
        {
            RectTransform window = WindowObject(
                "LarisID_Window",
                desktop,
                "Laris.ID",
                "DIGITAL MARKETPLACE",
                Cyan,
                out RectTransform content,
                out Button close);
            ui.closeLarisIDButton = close;

            if (larisPrefab == null)
            {
                TextObject("MissingPrefabMessage", content,
                    "Prefab UI Laris.ID belum tersedia.", 18, Danger, FontStyles.Bold,
                    TextAlignmentOptions.Center, V(.15f, .35f), V(.85f, .65f));
                return window.gameObject;
            }

            GameObject instance =
                (GameObject)PrefabUtility.InstantiatePrefab(larisPrefab, content);
            instance.name = "LarisIDFullUIPrefabHost";
            RectTransform instanceRect = instance.GetComponent<RectTransform>();
            Stretch(instanceRect);
            instanceRect.pivot = new Vector2(.5f, .5f);
            instanceRect.localScale = Vector3.one;

            PrepareEmbeddedLarisUI(instance);

            LarisIDSceneUI larisUI = instance.GetComponent<LarisIDSceneUI>();
            if (larisUI != null)
            {
                larisUI.manager = ui.larisManager;
                EditorUtility.SetDirty(larisUI);
            }
            return window.gameObject;
        }

        static bool PrepareEmbeddedLarisUI(GameObject instance)
        {
            bool changed = false;
            GraphicRaycaster nestedRaycaster = instance.GetComponent<GraphicRaycaster>();
            if (nestedRaycaster != null)
            {
                Object.DestroyImmediate(nestedRaycaster);
                changed = true;
            }
            CanvasScaler nestedScaler = instance.GetComponent<CanvasScaler>();
            if (nestedScaler != null)
            {
                Object.DestroyImmediate(nestedScaler);
                changed = true;
            }
            Canvas nestedCanvas = instance.GetComponent<Canvas>();
            if (nestedCanvas != null)
            {
                Object.DestroyImmediate(nestedCanvas);
                changed = true;
            }

            RectTransform rect = instance.GetComponent<RectTransform>();
            if (rect != null)
            {
                Stretch(rect);
                rect.pivot = new Vector2(.5f, .5f);
                rect.localScale = Vector3.one;
            }
            return changed;
        }

        static void SelectorRow(
            string name,
            Transform parent,
            string label,
            float yMin,
            float yMax,
            out Button previous,
            out TMP_Text value,
            out Button next)
        {
            RectTransform row = PanelObject(name, parent, PanelSoft, V(.01f, yMin), V(.98f, yMax));
            TextObject("Label", row, label, 9, Muted, FontStyles.Bold,
                TextAlignmentOptions.Left, V(.025f, .15f), V(.25f, .85f));
            previous = ButtonObject("PreviousButton", row, "−", Window, V(.27f, .13f), V(.36f, .87f));
            value = TextObject("Value", row, "-", 11, Text, FontStyles.Bold,
                TextAlignmentOptions.Center, V(.37f, .10f), V(.88f, .90f));
            next = ButtonObject("NextButton", row, "+", Window, V(.89f, .13f), V(.98f, .87f));
        }

        static TMP_Text ResultMetric(Transform parent, string name, string label, float x)
        {
            RectTransform panel = PanelObject(name, parent, PanelSoft, V(x, .55f), V(x + .30f, .70f));
            TextObject("Label", panel, label, 9, Muted, FontStyles.Bold,
                TextAlignmentOptions.Center, V(.05f, .62f), V(.95f, .92f));
            return TextObject("Value", panel, "0%", 22, Cyan, FontStyles.Bold,
                TextAlignmentOptions.Center, V(.05f, .08f), V(.95f, .64f));
        }

        static IntegratedProductRowUI ProductRowTemplate(Transform parent)
        {
            RectTransform row = PanelObject("ProductRowTemplate", parent, PanelSoft, V(0, 0), V(1, 0));
            LayoutElement layout = row.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = 82;
            layout.preferredHeight = 82;
            Button button = row.gameObject.AddComponent<Button>();
            button.targetGraphic = row.GetComponent<Image>();

            IntegratedProductRowUI view = row.gameObject.AddComponent<IntegratedProductRowUI>();
            RectTransform icon = PanelObject("FileIcon", row, Danger, V(.018f, .14f), V(.11f, .86f));
            view.productIcon = icon.GetComponent<Image>();
            view.fileTypeText = TextObject("FileType", icon, "PDF", 10, Text,
                FontStyles.Bold, TextAlignmentOptions.Center, V(.04f, .04f), V(.96f, .96f));
            view.productNameText = TextObject("ProductName", row, "Nama Produk", 14, Text,
                FontStyles.Bold, TextAlignmentOptions.Left, V(.13f, .52f), V(.58f, .88f));
            view.statusText = TextObject("Status", row, "Draft", 10, Cyan,
                FontStyles.Bold, TextAlignmentOptions.Left, V(.13f, .16f), V(.27f, .49f));
            view.qualityText = TextObject("Quality", row, "Q 0%", 10, Muted,
                FontStyles.Normal, TextAlignmentOptions.Left, V(.28f, .16f), V(.40f, .49f));
            view.priceText = TextObject("Price", row, "Rp 0", 13, Text,
                FontStyles.Bold, TextAlignmentOptions.Right, V(.58f, .52f), V(.80f, .88f));
            view.ratingText = TextObject("Rating", row, "Belum ada rating", 10, Muted,
                FontStyles.Normal, TextAlignmentOptions.Right, V(.55f, .16f), V(.80f, .49f));
            TextObject("OpenHint", row, "ATUR >", 10, Accent, FontStyles.Bold,
                TextAlignmentOptions.Center, V(.83f, .20f), V(.98f, .80f));
            view.openButton = button;
            row.gameObject.SetActive(false);
            return view;
        }

        static RectTransform WindowObject(
            string name,
            Transform parent,
            string title,
            string subtitle,
            Color accent,
            out RectTransform content,
            out Button closeButton)
        {
            RectTransform window = PanelObject(name, parent, Window, V(.075f, .06f), V(.97f, .915f));
            RectTransform titleBar = PanelObject("TitleBar", window, PanelSoft, V(0, .925f), V(1, 1));
            RectTransform appIcon = PanelObject("WindowAppIcon", titleBar, accent,
                V(.015f, .16f), V(.052f, .84f));
            TextObject("IconLetter", appIcon, title.Substring(0, 1), 16, Text,
                FontStyles.Bold, TextAlignmentOptions.Center, V(0, 0), V(1, 1));
            TextObject("WindowTitle", titleBar, title, 17, Text, FontStyles.Bold,
                TextAlignmentOptions.Left, V(.065f, .42f), V(.30f, .90f));
            TextObject("WindowSubtitle", titleBar, subtitle, 9, Muted, FontStyles.Bold,
                TextAlignmentOptions.Left, V(.065f, .10f), V(.32f, .44f));
            closeButton = ButtonObject("CloseAppButton", titleBar, "×", Danger,
                V(.952f, .16f), V(.988f, .84f));
            content = RectObject("WindowContent", window, V(0, 0), V(1, .925f));
            return window;
        }

        static Button DesktopIcon(
            string name,
            Transform parent,
            string letter,
            string label,
            Color color,
            Vector2 anchorMin,
            Vector2 anchorMax,
            out Image iconImage)
        {
            RectTransform root = PanelObject(name, parent, Color.clear, anchorMin, anchorMax);
            root.GetComponent<Image>().raycastTarget = true;
            Button button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = root.GetComponent<Image>();
            RectTransform icon = PanelObject("ReplaceableIcon", root, color, V(.20f, .30f), V(.80f, .92f));
            iconImage = icon.GetComponent<Image>();
            TextObject("PlaceholderLetter", icon, letter, 30, Text, FontStyles.Bold,
                TextAlignmentOptions.Center, V(0, 0), V(1, 1));
            TextObject("AppName", root, label, 14, Text, FontStyles.Bold,
                TextAlignmentOptions.Center, V(0, .02f), V(1, .28f));
            return button;
        }

        static Transform ScrollArea(string name, Transform parent, Vector2 min, Vector2 max)
        {
            RectTransform root = PanelObject(name, parent, Window, min, max);
            ScrollRect scroll = root.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 24;
            RectTransform viewport = PanelObject("Viewport", root, Color.clear, V(.012f, .012f), V(.988f, .988f));
            viewport.GetComponent<Image>().raycastTarget = true;
            viewport.gameObject.AddComponent<RectMask2D>();
            RectTransform content = RectObject("Content", viewport, V(0, 1), V(1, 1));
            content.pivot = new Vector2(.5f, 1);
            VerticalLayoutGroup group = content.gameObject.AddComponent<VerticalLayoutGroup>();
            group.padding = new RectOffset(7, 7, 7, 7);
            group.spacing = 7;
            group.childControlHeight = true;
            group.childControlWidth = true;
            group.childForceExpandHeight = false;
            group.childForceExpandWidth = true;
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
            Vector2 min,
            Vector2 max)
        {
            RectTransform root = PanelObject(name, parent, PanelSoft, min, max);
            root.GetComponent<Image>().raycastTarget = true;
            TMP_InputField input = root.gameObject.AddComponent<TMP_InputField>();
            input.lineType = multiline ? TMP_InputField.LineType.MultiLineNewline : TMP_InputField.LineType.SingleLine;
            input.selectionColor = new Color(Accent.r, Accent.g, Accent.b, .45f);
            input.caretColor = Cyan;
            RectTransform viewport = RectObject("Text Area", root, V(.035f, .10f), V(.965f, .90f));
            viewport.gameObject.AddComponent<RectMask2D>();
            TMP_Text placeholder = TextObject("Placeholder", viewport, placeholderValue, 12, Muted,
                FontStyles.Italic, TextAlignmentOptions.Left, V(0, 0), V(1, 1));
            TMP_Text text = TextObject("Text", viewport, "", 12, Text, FontStyles.Normal,
                TextAlignmentOptions.Left, V(0, 0), V(1, 1));
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
            Vector2 min,
            Vector2 max,
            Color? foreground = null)
        {
            RectTransform rect = PanelObject(name, parent, background, min, max);
            Image image = rect.GetComponent<Image>();
            image.raycastTarget = true;
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
            colors.pressedColor = new Color(.72f, .72f, .82f);
            colors.disabledColor = new Color(.48f, .48f, .55f, .55f);
            button.colors = colors;
            TextObject("Label", rect, label, 11, foreground ?? Text, FontStyles.Bold,
                TextAlignmentOptions.Center, V(.03f, .06f), V(.97f, .94f));
            return button;
        }

        static TMP_Text TextObject(
            string name,
            Transform parent,
            string value,
            float size,
            Color color,
            FontStyles style,
            TextAlignmentOptions alignment,
            Vector2 min,
            Vector2 max)
        {
            RectTransform rect = RectObject(name, parent, min, max);
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

        static RectTransform PanelObject(string name, Transform parent, Color color, Vector2 min, Vector2 max)
        {
            RectTransform rect = RectObject(name, parent, min, max);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }

        static RectTransform RectObject(string name, Transform parent, Vector2 min, Vector2 max)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
            return rect;
        }

        static string SlotLabel(SlotType slot) => slot switch
        {
            SlotType.ProductType => "JENIS PRODUK",
            SlotType.Purpose => "TUJUAN",
            SlotType.Audience => "AUDIENS",
            SlotType.ContentFocus => "FOKUS KONTEN",
            SlotType.Style => "GAYA",
            SlotType.AIOptimization => "OPTIMASI AI",
            _ => slot.ToString().ToUpperInvariant()
        };

        static GameObject FindRoot(Scene scene, string name) =>
            scene.GetRootGameObjects().FirstOrDefault(go => go.name == name);

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

        static void EnsureBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
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
