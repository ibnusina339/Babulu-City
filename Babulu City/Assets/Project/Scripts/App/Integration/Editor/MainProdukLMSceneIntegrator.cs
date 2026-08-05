#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using IntegratedApps;
using ProdukLM;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace IntegratedApps.Editor
{
    public static class MainProdukLMSceneIntegrator
    {
        const int CurrentIntegrationVersion = 22;
        const string CardPrefabPath = "Assets/Project/Prefabs/UI/CardPrefab.prefab";
        const string GenerationLoadingPrefabPath = "Assets/Project/Prefabs/UI/SpecialPage-GenerateScene.prefab";
        const string CardDataFolder = "Assets/Project/Resource/CardData";
        const string PublicPixelFontPath = "Assets/Tilemap/PublicPixel-rv0pA SDF.asset";

        [InitializeOnLoadMethod]
        static void FinishInterruptedMainIntegration()
        {
            EditorApplication.delayCall += () =>
            {
                Scene scene = FindLoadedScene("Main");
                if (!scene.IsValid() || !scene.isLoaded)
                    return;

                // Jangan jalankan integrasi otomatis ketika Unity sedang membuka
                // scene yang belum selesai dimuat atau gagal diparse (misalnya
                // karena masih memiliki marker merge).
                if (!scene.GetRootGameObjects().Any(go => go.name == "UI"))
                    return;

                MainProdukLMWindowUI existing = Resources
                    .FindObjectsOfTypeAll<MainProdukLMWindowUI>()
                    .FirstOrDefault(component => component.gameObject.scene == scene);

                // Nomor versi mencegah integrator menyimpan ulang scene setiap
                // kali Play Mode dimulai. Integrasi manual tetap tersedia.
                if (existing != null
                    && existing.integrationVersion >= CurrentIntegrationVersion)
                    return;

                try
                {
                    Integrate(scene);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            };
        }

        [MenuItem("Tools/BRIDA/Integrate ProdukLM Into Main Scene")]
        public static void Integrate()
        {
            Integrate(SceneManager.GetActiveScene());
        }

        static void Integrate(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
                throw new InvalidOperationException("Tidak ada scene aktif yang bisa diintegrasikan.");

            GameObject uiRoot = scene.GetRootGameObjects().FirstOrDefault(go => go.name == "UI");
            if (uiRoot == null)
                throw new InvalidOperationException("Root GameObject 'UI' tidak ditemukan di scene aktif.");

            Transform window = RequireDescendant(uiRoot.transform, "produk.LM");
            Transform page1 = Require(window, "Page1-seleksiProduk");
            Transform page2 = Require(window, "Page2-prompting");
            Transform page3 = Require(window, "Page3-hasilproduk");

            EnsureEventSystem(scene);
            SetupDesktopZoom(uiRoot.transform);
            DisableDecorativeRaycasts(uiRoot.transform);

            ProjectFlowManager flow = GetOrAdd<ProjectFlowManager>(window.gameObject);
            flow.productTypeSelectPanel = page1.gameObject;
            flow.slotAndLibraryPanel = page2.gameObject;
            flow.resultPanel = page3.gameObject;

            SetupBuilder(page2);
            SetupGenerationLoading(page2);
            SetupDesktopAndProductSelection(uiRoot.transform, window, page1, page2, flow);
            SetupClock(uiRoot.transform);

            page1.gameObject.SetActive(true);
            page2.gameObject.SetActive(false);
            page3.gameObject.SetActive(false);
            window.gameObject.SetActive(false);

            EditorUtility.SetDirty(flow);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log(
                "ProdukLM berhasil diintegrasikan ke scene Main. " +
                "Desktop icon, tahap 1, drag-and-drop tahap 2, Back, Generate, dan Close sudah terhubung.");
        }

        static void SetupGenerationLoading(Transform page2)
        {
            Transform window = page2.parent;
            Transform loading = window.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(item => item.name.Equals(
                    "SpecialPage-GenerateScene", StringComparison.OrdinalIgnoreCase));

            if (loading == null)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GenerationLoadingPrefabPath);
                if (prefab == null)
                    throw new InvalidOperationException(
                        $"Prefab loading tidak ditemukan di '{GenerationLoadingPrefabPath}'.");

                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, page2);
                instance.name = "SpecialPage-GenerateScene";
                loading = instance.transform;
            }

            // Loading adalah bagian dari Tahap 2. Page2 sengaja tetap aktif
            // selama coroutine agar overlay ini tidak ikut berhenti.
            if (loading.parent != page2)
                loading.SetParent(page2, false);

            RectTransform rect = loading as RectTransform;
            if (rect != null)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                rect.localScale = Vector3.one;
            }

            ProdukLMGenerationLoadingUI controller =
                GetOrAdd<ProdukLMGenerationLoadingUI>(loading.gameObject);
            controller.generationDurationSeconds = 15f;
            controller.consumedGameMinutes = 10f;
            loading.gameObject.SetActive(false);

            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(loading);
        }

        static void SetupBuilder(Transform page2)
        {
            CardData[] catalog = LoadCardCatalog();

            Transform libraryRoot = Require(page2, "TabsOpsi");
            libraryRoot.gameObject.SetActive(true);
            CardLibraryManager library = GetOrAdd<CardLibraryManager>(libraryRoot.gameObject);
            library.cardContainer = libraryRoot;
            library.cardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CardPrefabPath)
                ?.GetComponent<CardUI>();
            library.allCards = catalog;
            EditorUtility.SetDirty(library);

            if (library.cardPrefab == null)
                throw new InvalidOperationException($"CardPrefab tidak ditemukan di '{CardPrefabPath}'.");

            Transform slotRoot = Require(page2, "promptcategory");
            SetupSlot(Require(slotRoot, "Tujuan"), SlotType.Purpose);
            SetupSlot(Require(slotRoot, "Target Pengguna"), SlotType.Audience);
            SetupSlot(Require(slotRoot, "Konten"), SlotType.ContentFocus);
            SetupSlot(Require(slotRoot, "Gaya Penyajian"), SlotType.Style);
            SetupSlot(Require(slotRoot, "Fokus AI"), SlotType.AIOptimization);

            Transform promptBox = RequireAny(page2, "ChatBox", "chatbox(aiprompt)");
            TMP_Text promptText = EnsureText(
                promptBox,
                "PromptPreviewText",
                FindReferenceText(page2),
                20f,
                TextAlignmentOptions.MidlineLeft);
            promptText.text = string.Empty;
            promptText.margin = new Vector4(25f, 12f, 25f, 12f);
            TMP_FontAsset publicPixel = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PublicPixelFontPath);
            if (publicPixel != null)
                promptText.font = publicPixel;

            PromptPreviewUI preview = GetOrAdd<PromptPreviewUI>(promptBox.gameObject);
            preview.promptText = promptText;
            EditorUtility.SetDirty(preview);

            // Desain terbaru memakai tombol ikon. Visual ikon sengaja tidak
            // dibuat oleh script agar nanti bisa langsung diganti desainer.
            Transform backButton = RequireAny(page2, "Previous Button", "sebelum");
            Transform generateButton = RequireAny(page2, "Send Button", "sesudah");
            GetOrAdd<BackButtonUI>(EnsureButton(backButton).gameObject);
            GetOrAdd<GenerateButtonUI>(EnsureButton(generateButton).gameObject);
            SetupPromptResetButton(page2, generateButton, FindReferenceText(page2));
            SetupCurrentOptionIndicator(page2);
        }

        static void SetupSavedProductConfirmation(Transform libraryRoot, TMP_Text referenceText)
        {
            TMP_Text message = EnsureText(
                libraryRoot,
                "ProdukTersimpanMessage",
                referenceText,
                20f,
                TextAlignmentOptions.Center);
            message.text = "PRODUK SUDAH TERSIMPAN KE LARIS.ID";
            message.textWrappingMode = TextWrappingModes.Normal;

            Image buttonImage = EnsureImage(libraryRoot, "KembaliKeTahap1Button");
            buttonImage.color = new Color(0.12f, 0.29f, 0.52f, 1f);
            EnsureButton(buttonImage.transform);
            TMP_Text label = EnsureText(
                buttonImage.transform,
                "Text",
                referenceText,
                16f,
                TextAlignmentOptions.Center);
            label.text = "KEMBALI KE TAHAP 1";

            message.gameObject.SetActive(false);
            buttonImage.gameObject.SetActive(false);
            EditorUtility.SetDirty(message);
            EditorUtility.SetDirty(buttonImage);
        }

        static void SetupCurrentOptionIndicator(Transform page2)
        {
            Transform optionNow = page2.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(item =>
                {
                    string normalized = item.name.Replace(" ", string.Empty)
                        .Replace("_", string.Empty).Replace("-", string.Empty);
                    return normalized.Equals("OptionNow", StringComparison.OrdinalIgnoreCase) ||
                           normalized.Equals("OpsiSekarang", StringComparison.OrdinalIgnoreCase) ||
                           normalized.Equals("CurrentOption", StringComparison.OrdinalIgnoreCase);
                });
            if (optionNow != null)
                GetOrAdd<CurrentPromptOptionUI>(optionNow.gameObject);
        }

        static void SetupPromptResetButton(Transform page2, Transform generateButton, TMP_Text referenceText)
        {
            Transform existing = page2.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(item => item.name == "Reset Prompt Button");
            GameObject resetObject;

            if (existing != null)
            {
                resetObject = existing.gameObject;
            }
            else
            {
                resetObject = UnityEngine.Object.Instantiate(generateButton.gameObject, generateButton.parent);
                resetObject.name = "Reset Prompt Button";
                foreach (MonoBehaviour behaviour in resetObject.GetComponents<MonoBehaviour>())
                    if (behaviour is GenerateButtonUI)
                        UnityEngine.Object.DestroyImmediate(behaviour);

                RectTransform rect = resetObject.GetComponent<RectTransform>();
                RectTransform sendRect = generateButton.GetComponent<RectTransform>();
                rect.anchoredPosition = sendRect.anchoredPosition + new Vector2(-165f, 0f);
                rect.sizeDelta = new Vector2(180f, Mathf.Max(64f, sendRect.sizeDelta.y * 0.65f));

                foreach (Transform child in resetObject.GetComponentsInChildren<Transform>(true))
                    if (child != resetObject.transform)
                        child.gameObject.SetActive(false);

                TMP_Text label = EnsureText(resetObject.transform, "Reset Label", referenceText, 22f,
                    TextAlignmentOptions.Center);
                label.text = "RESET";
                label.gameObject.SetActive(true);
                RectTransform labelRect = label.rectTransform;
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;
            }

            GetOrAdd<ResetPromptButtonUI>(EnsureButton(resetObject.transform).gameObject);
            EditorUtility.SetDirty(resetObject);
        }

        static void SetupDesktopAndProductSelection(
            Transform uiRoot,
            Transform window,
            Transform page1,
            Transform page2,
            ProjectFlowManager flow)
        {
            CardData[] catalog = LoadCardCatalog();
            Transform limitLabel = page1.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(item => item.name.Equals("Limit", StringComparison.OrdinalIgnoreCase));
            if (limitLabel != null && limitLabel.GetComponent<TMP_Text>() != null)
                GetOrAdd<DailyLimitUI>(limitLabel.gameObject);

            var options = new List<MainProdukLMWindowUI.ProductOption>
            {
                ProductOption(page1, "tab/surat", FindCard(catalog, "Template Dokumen")),
                ProductOption(page1, "tab/ebook", FindCard(catalog, "E-Book")),
                ProductOption(page1, "tab/ppt", FindCard(catalog, "Template PPT")),
                ProductOption(page1, "tab/blajar", FindCard(catalog, "Modul Belajar")),
                ProductOption(page1, "tab/planner", FindCard(catalog, "Digital Planner")),
                ProductOption(page1, "tab/infografis", FindCard(catalog, "Infografis"))
            };

            Transform descriptionPanel = RequireAny(page1, "Description Panel", "deskripsi");
            Transform descriptionContent = FindChildByNames(
                descriptionPanel,
                "deskripsi",
                "Description");
            if (descriptionContent == null)
                descriptionContent = descriptionPanel;

            TMP_Text referenceText = FindReferenceText(page1);
            TMP_Text productName = EnsureText(
                descriptionContent,
                "SelectedProductName",
                referenceText,
                32f,
                TextAlignmentOptions.Center);
            TMP_Text description = EnsureText(
                descriptionContent,
                "SelectionDescription",
                referenceText,
                26f,
                TextAlignmentOptions.Center);

            Transform designedPreview = descriptionContent.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(item =>
                    item.name.Equals("SelectedProductIcon", StringComparison.OrdinalIgnoreCase) ||
                    item.name.Equals("Image", StringComparison.OrdinalIgnoreCase));
            Image productIcon = designedPreview != null
                ? designedPreview.GetComponent<Image>()
                : null;
            if (productIcon == null)
                productIcon = EnsureImage(descriptionPanel, "SelectedProductIcon");

            // Matikan fallback lama yang pernah dibuat langsung di tengah
            // Description Panel, tetapi pertahankan Image desain di dalam box.
            foreach (Image candidate in descriptionPanel.GetComponentsInChildren<Image>(true))
            {
                if (candidate == productIcon) continue;
                if (candidate.name.Equals("SelectedProductIcon", StringComparison.OrdinalIgnoreCase) &&
                    candidate.transform.parent == descriptionPanel)
                    candidate.gameObject.SetActive(false);
            }

            productIcon.preserveAspect = true;
            productIcon.raycastTarget = false;
            productIcon.color = Color.white;

            RectTransform nameRect = productName.rectTransform;
            nameRect.anchorMin = new Vector2(0.08f, 0.34f);
            nameRect.anchorMax = new Vector2(0.92f, 0.44f);
            nameRect.offsetMin = Vector2.zero;
            nameRect.offsetMax = Vector2.zero;

            RectTransform descriptionRect = description.rectTransform;
            descriptionRect.anchorMin = new Vector2(0.08f, 0.08f);
            descriptionRect.anchorMax = new Vector2(0.92f, 0.31f);
            descriptionRect.offsetMin = Vector2.zero;
            descriptionRect.offsetMax = Vector2.zero;

            MainProdukLMWindowUI controller = GetOrAdd<MainProdukLMWindowUI>(uiRoot.gameObject);
            controller.windowRoot = window.gameObject;
            controller.windowTaskbar = RequireDescendant(uiRoot, "taskbar");
            controller.flowManager = flow;
            controller.productOptions = options.ToArray();
            controller.openButton = EnsureButton(
                RequireDescendant(uiRoot, "ProdukLM App"));
            controller.confirmSelectionButton = EnsureButton(
                RequireAny(descriptionPanel, "selectbox", "Pilih"));

            // Tahap 1 sekarang hanya memakai tombol PILIH. Jika scene lama
            // masih memiliki tombol navigasi tambahan, sembunyikan tanpa
            // menjadikannya syarat integrasi.
            Transform stageOneNext = FindChildByNames(
                page1,
                "sesudah (1)",
                "Next Button");
            if (stageOneNext != null)
                stageOneNext.gameObject.SetActive(false);
            controller.nextStageButton = null;

            controller.selectedProductIcon = productIcon;
            controller.selectedProductText = productName;
            controller.selectionDescriptionText = description;
            controller.integrationVersion = CurrentIntegrationVersion;

            var closeButtons = new List<Button>();

            Transform page1Close = FindDescendant(page1, "exittab");
            if (page1Close != null)
                closeButtons.Add(EnsureButton(page1Close));

            Transform page2Close = FindDescendant(page2, "exittab");
            if (page2Close != null)
                closeButtons.Add(EnsureButton(page2Close));

            Transform page3 = window.Find("Page3-hasilproduk");
            Transform page3Close = page3 != null
                ? FindDescendant(page3, "exittab")
                : null;
            if (page3Close != null)
                closeButtons.Add(EnsureButton(page3Close));

            Transform optionalExit = FindDescendant(window, "Exit Button");
            if (optionalExit != null)
                closeButtons.Add(EnsureButton(optionalExit));

            controller.closeButtons = closeButtons.Distinct().ToArray();
            EditorUtility.SetDirty(controller);
        }

        static MainProdukLMWindowUI.ProductOption ProductOption(
            Transform page,
            string path,
            CardData card)
        {
            Button button = EnsureButton(Require(page, path));
            return new MainProdukLMWindowUI.ProductOption
            {
                button = button,
                card = card,
                highlightGraphic = button.targetGraphic,
                previewIcon = FindProductIcon(button) ?? card.icon
            };
        }

        static Sprite FindProductIcon(Button button)
        {
            Image[] childImages = button.GetComponentsInChildren<Image>(true);
            Image icon = childImages
                .Where(image => image.transform != button.transform && image.sprite != null)
                .OrderByDescending(image => IconScore(image))
                .FirstOrDefault();

            return icon != null
                ? icon.sprite
                : (button.targetGraphic as Image)?.sprite;
        }

        static int IconScore(Image image)
        {
            string identity = $"{image.name} {image.sprite.name}".ToLowerInvariant();
            int score = 0;

            if (identity.Contains("icon") || identity.Contains("logo"))
                score += 1000;
            if (identity.Contains("dialog") || identity.Contains("box")
                || identity.Contains("frame") || identity.Contains("outline")
                || identity.Contains("select") || identity.Contains("button"))
                score -= 1000;

            score += Mathf.RoundToInt(
                Mathf.Min(image.rectTransform.rect.width, image.rectTransform.rect.height));
            return score;
        }

        static void SetupClock(Transform uiRoot)
        {
            Transform taskbar = RequireDescendant(uiRoot, "taskbar");
            TMP_Text clockText = RequireText(taskbar, "Jam", "jam");
            TMP_Text dateText = RequireText(taskbar, "Tanggal", "tanggal");

            ConfigureClockText(clockText);
            ConfigureClockText(dateText);

            GameClockUI clock = GetOrAdd<GameClockUI>(uiRoot.gameObject);
            clock.clockTexts = new[] { clockText };
            clock.dateTexts = new[] { dateText };
            clock.startHour = 20;
            clock.endHour = 24;
            clock.realSecondsPerGameMinute = 2f;
            clock.startDate = GameClockUI.DefaultStartDate;
            clock.runAutomatically = true;
            clock.useUnscaledTime = true;
            EditorUtility.SetDirty(clock);
        }

        static void SetupDesktopZoom(Transform uiRoot)
        {
            Transform desktop = Require(uiRoot, "Desktop");
            Transform mainMenu = Require(desktop, "MainMenu");
            Transform taskbar = RequireDescendant(uiRoot, "taskbar");

            // Layout terbaru sudah diatur langsung sebagai GameObject di scene.
            // Integrator hanya memastikan referensinya valid dan tidak lagi
            // menimpa posisi/ukuran karya desainer.
            EditorUtility.SetDirty(desktop);
            EditorUtility.SetDirty(mainMenu);
            EditorUtility.SetDirty(taskbar);
        }

        static void DisableDecorativeRaycasts(Transform uiRoot)
        {
            SetRaycastTarget(uiRoot.Find("Laptop Frame"), false);
            SetRaycastTarget(uiRoot.Find("Desktop"), false);
            SetRaycastTarget(uiRoot.Find("Desktop/MainMenu"), false);
            SetRaycastTarget(uiRoot.Find("Desktop/MainMenu/bg_mainmenu"), false);
        }

        static void SetRaycastTarget(Transform target, bool enabled)
        {
            if (target == null)
                return;

            foreach (Graphic graphic in target.GetComponents<Graphic>())
            {
                graphic.raycastTarget = enabled;
                EditorUtility.SetDirty(graphic);
            }
        }

        static TMP_Text ConfigureClockText(TMP_Text text)
        {
            text.alignment = TextAlignmentOptions.MidlineRight;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            EditorUtility.SetDirty(text);
            return text;
        }

        static void SetupSlot(Transform slotTransform, SlotType slotType)
        {
            SlotUI slot = GetOrAdd<SlotUI>(slotTransform.gameObject);
            slot.slotType = slotType;
            slot.label = slotTransform.GetComponentInChildren<TMP_Text>(true);
            slot.activeIndicator = null;

            Graphic background = slotTransform.GetComponent<Graphic>();
            slot.backgroundGraphic = background;
            slot.emptyColor = new Color(0.32f, 0.33f, 0.35f, 0.9f);
            slot.focusedColor = new Color(0.08f, 0.40f, 0.70f, 1f);
            slot.filledColor = Color.white;
            slot.emptyTextColor = new Color(0.72f, 0.73f, 0.75f, 1f);
            slot.focusedTextColor = Color.white;
            slot.filledTextColor = Color.white;
            slot.focusOutlineColor = new Color(0.35f, 0.82f, 1f, 1f);

            Outline outline = GetOrAdd<Outline>(slotTransform.gameObject);
            outline.effectColor = slot.focusOutlineColor;
            outline.effectDistance = new Vector2(4f, -4f);
            outline.useGraphicAlpha = false;
            outline.enabled = false;
            slot.focusOutline = outline;

            EditorUtility.SetDirty(outline);
            EditorUtility.SetDirty(slot);
        }

        static Button EnsureButton(Transform target)
        {
            Button button = target.GetComponent<Button>();
            if (button == null)
                button = Undo.AddComponent<Button>(target.gameObject);

            Graphic graphic = target.GetComponent<Graphic>();
            if (graphic != null)
                button.targetGraphic = graphic;

            EditorUtility.SetDirty(button);
            return button;
        }

        static T GetOrAdd<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null ? component : Undo.AddComponent<T>(target);
        }

        static TMP_Text EnsureText(
            Transform parent,
            string name,
            TMP_Text reference,
            float fontSize,
            TextAlignmentOptions alignment)
        {
            Transform existing = parent.Find(name);
            TextMeshProUGUI text;
            if (existing != null)
            {
                text = existing.GetComponent<TextMeshProUGUI>();
            }
            else
            {
                var textObject = new GameObject(
                    name,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(TextMeshProUGUI));
                Undo.RegisterCreatedObjectUndo(textObject, $"Create {name}");
                textObject.layer = parent.gameObject.layer;
                textObject.transform.SetParent(parent, false);
                text = textObject.GetComponent<TextMeshProUGUI>();
            }

            RectTransform rect = text.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            if (reference != null)
            {
                text.font = reference.font;
                text.fontSharedMaterial = reference.fontSharedMaterial;
                text.color = reference.color;
            }

            text.fontSize = fontSize;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;
            EditorUtility.SetDirty(text);
            return text;
        }

        static Image EnsureImage(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            Image image;
            if (existing != null)
            {
                image = existing.GetComponent<Image>();
            }
            else
            {
                var imageObject = new GameObject(
                    name,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                Undo.RegisterCreatedObjectUndo(imageObject, $"Create {name}");
                imageObject.layer = parent.gameObject.layer;
                imageObject.transform.SetParent(parent, false);
                image = imageObject.GetComponent<Image>();
            }

            EditorUtility.SetDirty(image);
            return image;
        }

        static TMP_Text FindReferenceText(Transform root)
        {
            return root.GetComponentInChildren<TMP_Text>(true);
        }

        static CardData[] LoadCardCatalog()
        {
            return AssetDatabase.FindAssets("t:CardData", new[] { CardDataFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<CardData>)
                .Where(card => card != null)
                .OrderBy(card => card.slotType)
                .ThenBy(card => card.displayName)
                .ToArray();
        }

        static CardData FindCard(IEnumerable<CardData> cards, string displayName)
        {
            CardData card = cards.FirstOrDefault(
                candidate => string.Equals(
                    candidate.displayName,
                    displayName,
                    StringComparison.OrdinalIgnoreCase));
            if (card == null)
                throw new InvalidOperationException($"CardData '{displayName}' tidak ditemukan.");
            return card;
        }

        static Transform Require(Transform root, string path)
        {
            Transform result = root.Find(path);
            if (result == null)
                throw new InvalidOperationException(
                    $"GameObject '{root.name}/{path}' tidak ditemukan. " +
                    "Pastikan nama hierarchy desain belum diubah.");
            return result;
        }

        static Transform RequireAny(Transform root, params string[] paths)
        {
            foreach (string path in paths)
            {
                Transform result = root.Find(path);
                if (result != null)
                    return result;
            }

            throw new InvalidOperationException(
                $"Tidak ada GameObject yang cocok di bawah '{root.name}'. " +
                $"Nama yang dicari: {string.Join(", ", paths)}.");
        }

        static Transform FindChildByNames(Transform root, params string[] names)
        {
            foreach (string name in names)
            {
                Transform result = root.Find(name);
                if (result != null)
                    return result;
            }
            return null;
        }

        static TMP_Text RequireText(Transform root, params string[] names)
        {
            Transform textTransform = FindChildByNames(root, names);
            TMP_Text text = textTransform != null
                ? textTransform.GetComponent<TMP_Text>()
                : null;
            if (text == null)
            {
                throw new InvalidOperationException(
                    $"Text jam/tanggal tidak ditemukan di bawah '{root.name}'. " +
                    $"Nama yang dicari: {string.Join(", ", names)}.");
            }
            return text;
        }

        static Transform FindDescendant(Transform root, string objectName)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == objectName)
                    return child;
            }
            return null;
        }

        static Transform RequireDescendant(Transform root, string objectName)
        {
            Transform result = FindDescendant(root, objectName);
            if (result == null)
                throw new InvalidOperationException(
                    $"GameObject bernama '{objectName}' tidak ditemukan di bawah '{root.name}'.");
            return result;
        }

        static Scene FindLoadedScene(string sceneName)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded && scene.name == sceneName)
                    return scene;
            }
            return default;
        }

        static void EnsureEventSystem(Scene scene)
        {
            EventSystem eventSystem = Resources.FindObjectsOfTypeAll<EventSystem>()
                .FirstOrDefault(system => system.gameObject.scene == scene);
            if (eventSystem != null)
            {
                if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
                {
                    InputSystemUIInputModule inputModule =
                        Undo.AddComponent<InputSystemUIInputModule>(eventSystem.gameObject);
                    inputModule.AssignDefaultActions();
                }
                return;
            }

            var eventSystemObject = new GameObject(
                "EventSystem",
                typeof(EventSystem),
                typeof(InputSystemUIInputModule));
            Undo.RegisterCreatedObjectUndo(eventSystemObject, "Create ProdukLM EventSystem");
            SceneManager.MoveGameObjectToScene(eventSystemObject, scene);
            eventSystemObject.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }
    }
}
#endif
