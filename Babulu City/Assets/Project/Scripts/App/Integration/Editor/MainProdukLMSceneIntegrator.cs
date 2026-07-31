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
        const int CurrentIntegrationVersion = 7;
        const string CardPrefabPath = "Assets/Project/Prefabs/UI/CardPrefab.prefab";
        const string CardDataFolder = "Assets/Project/Resource/CardData";

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
            SetupDesktopZoom(uiRoot.transform, window);

            ProjectFlowManager flow = GetOrAdd<ProjectFlowManager>(window.gameObject);
            flow.productTypeSelectPanel = page1.gameObject;
            flow.slotAndLibraryPanel = page2.gameObject;
            flow.resultPanel = page3.gameObject;

            SetupBuilder(page2);
            SetupDesktopAndProductSelection(uiRoot.transform, window, page1, page2, flow);
            SetupClock(uiRoot.transform, window);

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

        static void SetupBuilder(Transform page2)
        {
            CardData[] catalog = LoadCardCatalog();

            Transform libraryRoot = Require(page2, "TabsOpsi");
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

            Transform promptBox = Require(page2, "chatbox(aiprompt)");
            TMP_Text promptText = EnsureText(
                promptBox,
                "PromptPreviewText",
                FindReferenceText(page2),
                20f,
                TextAlignmentOptions.MidlineLeft);
            promptText.text = string.Empty;
            promptText.margin = new Vector4(25f, 12f, 25f, 12f);

            PromptPreviewUI preview = GetOrAdd<PromptPreviewUI>(promptBox.gameObject);
            preview.promptText = promptText;
            EditorUtility.SetDirty(preview);

            GetOrAdd<BackButtonUI>(EnsureButton(Require(page2, "sebelum")).gameObject);
            GetOrAdd<GenerateButtonUI>(EnsureButton(Require(page2, "sesudah")).gameObject);
        }

        static void SetupDesktopAndProductSelection(
            Transform uiRoot,
            Transform window,
            Transform page1,
            Transform page2,
            ProjectFlowManager flow)
        {
            CardData[] catalog = LoadCardCatalog();
            var options = new List<MainProdukLMWindowUI.ProductOption>
            {
                ProductOption(page1, "tab/surat", FindCard(catalog, "Template Dokumen")),
                ProductOption(page1, "tab/ebook", FindCard(catalog, "E-Book")),
                ProductOption(page1, "tab/ppt", FindCard(catalog, "Template PPT")),
                ProductOption(page1, "tab/blajar", FindCard(catalog, "Modul Belajar")),
                ProductOption(page1, "tab/planner", FindCard(catalog, "Digital Planner")),
                ProductOption(page1, "tab/infografis", FindCard(catalog, "Infografis"))
            };

            Transform descriptionPanel = Require(page1, "deskripsi");
            TMP_Text referenceText = FindReferenceText(page1);
            TMP_Text productName = EnsureText(
                descriptionPanel,
                "SelectedProductName",
                referenceText,
                26f,
                TextAlignmentOptions.Center);
            TMP_Text description = EnsureText(
                descriptionPanel,
                "SelectionDescription",
                referenceText,
                20f,
                TextAlignmentOptions.Center);
            Image productIcon = EnsureImage(descriptionPanel, "SelectedProductIcon");
            productIcon.preserveAspect = true;
            productIcon.raycastTarget = false;
            productIcon.color = Color.white;

            RectTransform iconRect = productIcon.rectTransform;
            iconRect.anchorMin = new Vector2(0.5f, 0.70f);
            iconRect.anchorMax = new Vector2(0.5f, 0.70f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = Vector2.zero;
            iconRect.sizeDelta = new Vector2(160f, 160f);

            RectTransform nameRect = productName.rectTransform;
            nameRect.anchorMin = new Vector2(0.08f, 0.42f);
            nameRect.anchorMax = new Vector2(0.92f, 0.57f);
            nameRect.offsetMin = Vector2.zero;
            nameRect.offsetMax = Vector2.zero;

            RectTransform descriptionRect = description.rectTransform;
            descriptionRect.anchorMin = new Vector2(0.08f, 0.08f);
            descriptionRect.anchorMax = new Vector2(0.92f, 0.38f);
            descriptionRect.offsetMin = Vector2.zero;
            descriptionRect.offsetMax = Vector2.zero;

            MainProdukLMWindowUI controller = GetOrAdd<MainProdukLMWindowUI>(uiRoot.gameObject);
            controller.windowRoot = window.gameObject;
            controller.windowTaskbar = Require(Require(uiRoot, "Desktop"), "taskbar");
            controller.flowManager = flow;
            controller.productOptions = options.ToArray();
            Transform larisIcon = RequireDescendant(uiRoot, "LARIS.ID");
            controller.openButton = EnsureButton(
                Require(larisIcon.parent, "ventra"));
            controller.confirmSelectionButton = EnsureButton(Require(page1, "selectbox"));

            // Tahap 1 cukup memakai tombol PILIH. Tombol navigasi kanan milik
            // template desainer tetap disimpan sebagai GameObject, tetapi
            // disembunyikan agar mudah dipakai lagi bila desain berubah.
            Transform stageOneNext = Require(page1, "sesudah (1)");
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

            Transform page2Close = FindDescendant(page2, "exittab (1)");
            if (page2Close != null)
                closeButtons.Add(EnsureButton(page2Close));

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
                previewIcon = FindProductIcon(button)
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

        static void SetupClock(Transform uiRoot, Transform productWindow)
        {
            Transform desktopTaskbar = Require(Require(uiRoot, "Desktop"), "taskbar");
            Transform productTaskbar = productWindow.Find("taskbar");

            TMP_Text desktopReference = FindReferenceText(desktopTaskbar);

            TMP_Text desktopClock = ConfigureClockText(
                EnsureText(desktopTaskbar, "jam", desktopReference, 16f, TextAlignmentOptions.MidlineRight),
                new Vector2(760f, -391f));
            TMP_Text desktopDate = ConfigureClockText(
                EnsureText(desktopTaskbar, "tanggal", desktopReference, 16f, TextAlignmentOptions.MidlineRight),
                new Vector2(760f, -412f));

            TMP_Text productClock = null;
            TMP_Text productDate = null;
            if (productTaskbar != null)
            {
                TMP_Text productReference = FindReferenceText(productTaskbar);
                Transform combinedClock = productTaskbar.Find("jam&tanggal");
                if (combinedClock != null)
                    combinedClock.gameObject.SetActive(false);

                productClock = ConfigureClockText(
                    EnsureText(productTaskbar, "jam", productReference, 16f, TextAlignmentOptions.MidlineRight),
                    new Vector2(760f, -391f));
                productDate = ConfigureClockText(
                    EnsureText(productTaskbar, "tanggal", productReference, 16f, TextAlignmentOptions.MidlineRight),
                    new Vector2(760f, -412f));
            }

            GameClockUI clock = GetOrAdd<GameClockUI>(uiRoot.gameObject);
            clock.clockTexts = productClock != null
                ? new[] { desktopClock, productClock }
                : new[] { desktopClock };
            clock.dateTexts = productDate != null
                ? new[] { desktopDate, productDate }
                : new[] { desktopDate };
            clock.startHour = 20;
            clock.endHour = 24;
            clock.realSecondsPerGameMinute = 7.5f;
            clock.startDate = "30/07/2026";
            clock.runAutomatically = true;
            clock.useUnscaledTime = true;
            EditorUtility.SetDirty(clock);
        }

        static void SetupDesktopZoom(Transform uiRoot, Transform productWindow)
        {
            Transform desktop = Require(uiRoot, "Desktop");
            Transform mainMenu = Require(desktop, "MainMenu");
            Transform taskbar = Require(desktop, "taskbar");

            StretchToParent(desktop);
            StretchToParent(productWindow);
            desktop.localScale = new Vector3(1.03f, 1.03f, 1f);

            Transform laptopFrame = mainMenu.Find("LaptopFrame");
            Transform background = mainMenu.Find("bg_mainmenu");
            Transform desktopContent = mainMenu.Find("mainmenu");

            SetFixedRect(mainMenu, Vector2.zero, new Vector2(100f, 100f));
            SetFixedRect(taskbar, Vector2.zero, new Vector2(100f, 100f));

            if (laptopFrame != null)
                SetFixedRect(
                    laptopFrame,
                    new Vector2(1.5463f, 0.88854f),
                    new Vector2(1869.2f, 1039.1f));
            if (background != null)
                SetFixedRect(
                    background,
                    new Vector2(1.514f, 43.929f),
                    new Vector2(1752.864f, 815.613f));
            if (desktopContent != null)
                SetFixedRect(
                    desktopContent,
                    new Vector2(1.536f, 0.877f),
                    new Vector2(1869.209f, 1039.108f));

            EditorUtility.SetDirty(desktop);
        }

        static void SetFixedRect(Transform target, Vector2 position, Vector2 size)
        {
            if (target is not RectTransform rect)
                return;

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            EditorUtility.SetDirty(rect);
        }

        static void StretchToParent(Transform target)
        {
            if (target is not RectTransform rect)
                return;

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            EditorUtility.SetDirty(rect);
        }

        static TMP_Text ConfigureClockText(TMP_Text text, Vector2 position)
        {
            RectTransform rect = text.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(200f, 24f);
            text.alignment = TextAlignmentOptions.MidlineRight;
            text.textWrappingMode = TextWrappingModes.NoWrap;
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
            if (background != null)
                slot.filledColor = background.color;

            Outline outline = GetOrAdd<Outline>(slotTransform.gameObject);
            outline.effectColor = slot.focusOutlineColor;
            outline.effectDistance = new Vector2(3f, -3f);
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
