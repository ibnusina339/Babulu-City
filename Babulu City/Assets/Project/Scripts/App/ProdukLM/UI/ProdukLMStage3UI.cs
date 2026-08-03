using System;
using System.Linq;
using LarisID;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProdukLM
{
    [DisallowMultipleComponent]
    public sealed class ProdukLMStage3UI : MonoBehaviour
    {
        TMP_Text fallbackTierText;
        TMP_Text tierText => null; // kompatibilitas layout lama; header tidak lagi dipakai untuk tier
        GameObject freeTierText;
        GameObject plusTierText;
        GameObject proTierText;
        TMP_Text statusText;
        TMP_Text productTypeText;
        TMP_Text recommendationText;
        TMP_Text productNameText;
        TMP_InputField productNameInput;
        Image productIcon;
        Button retryButton;
        Button saveButton;
        Button exitButton;
        Button savedBackButton;
        Transform backgroundMenu;
        Transform topBar;
        Transform productLMBrand;
        Transform leftPanel;
        Transform rightPanel;
        GameObject savedMessageObject;
        Transform qualityBox;
        Transform relevanceBox;
        Transform sellValueBox;
        bool saved;
        string committedProductName;

        void Awake()
        {
            ResolveUI();
            BindButtons();
        }

        void OnEnable()
        {
            if (ProjectFlowManager.Instance != null)
                ProjectFlowManager.Instance.OnGenerated += Refresh;
            Refresh();
        }

        void OnDisable()
        {
            if (ProjectFlowManager.Instance != null)
                ProjectFlowManager.Instance.OnGenerated -= Refresh;
        }

        void OnDestroy()
        {
            retryButton?.onClick.RemoveListener(Retry);
            saveButton?.onClick.RemoveListener(SaveProduct);
            exitButton?.onClick.RemoveListener(CloseWindow);
            savedBackButton?.onClick.RemoveListener(ReturnToStageOne);
            if (productNameInput != null)
            {
                productNameInput.onSelect.RemoveListener(BeginNameEdit);
                productNameInput.onEndEdit.RemoveListener(CommitName);
            }
        }

        void ResolveUI()
        {
            ResolveTierTexts();
            statusText = TextOf(Find("Status.TXT"));
            productTypeText = TextOf(Find("JenisProduk.TXT"));
            recommendationText = TextOf(Find("RekomendasiTXT"));
            productNameText = TextOf(Find("namaProduk"));
            productIcon = (Find("Icon Product") ?? Find("edit.IKON"))?.GetComponent<Image>();
            qualityBox = Find("KualitasBOX");
            relevanceBox = Find("RelevansiBOX");
            sellValueBox = Find("NilaiJualBOX");
            retryButton = EnsureButton(Find("FrameBuatUlang"));
            saveButton = EnsureButton(Find("frameSimpanProduk"));
            exitButton = EnsureButton(Find("exittab"));
            backgroundMenu = Find("bg_menu (2)") ?? Find("bg_menu");
            topBar = Find("TopBar (1)");
            productLMBrand = Find("ProdukLM Title");
            leftPanel = Find("PanelKiri");
            rightPanel = Find("PanelKanan");
            savedMessageObject = Find("ProdukTersimpanMessage")?.gameObject;
            savedBackButton = ResolveSavedBackButton();

            if (exitButton != null)
            {
                exitButton.transform.SetAsLastSibling();
                foreach (Graphic graphic in exitButton.GetComponentsInChildren<Graphic>(true))
                {
                    graphic.raycastTarget = true;
                    graphic.raycastPadding = new Vector4(-10f, -10f, -10f, -10f);
                }
            }

            Transform inputBox = Find("NamaProdukBOX");
            if (inputBox != null)
            {
                productNameInput = inputBox.GetComponent<TMP_InputField>();
                if (productNameInput == null)
                    productNameInput = inputBox.gameObject.AddComponent<TMP_InputField>();
                productNameInput.targetGraphic = inputBox.GetComponent<Graphic>();
                productNameInput.textComponent = productNameText;
                productNameInput.textViewport = productNameText != null ? productNameText.rectTransform : null;
                productNameInput.characterLimit = 18;
                productNameInput.lineType = TMP_InputField.LineType.SingleLine;
                productNameInput.onSelect.AddListener(BeginNameEdit);
                productNameInput.onEndEdit.AddListener(CommitName);
            }
        }

        void BindButtons()
        {
            retryButton?.onClick.AddListener(Retry);
            saveButton?.onClick.AddListener(SaveProduct);
            exitButton?.onClick.AddListener(CloseWindow);
            savedBackButton?.onClick.AddListener(ReturnToStageOne);
        }

        void Refresh()
        {
            ProjectFlowManager flow = ProjectFlowManager.Instance;
            if (flow == null || !flow.HasGeneratedResult)
                return;

            saved = flow.LastResultSaved;
            SetResultContentVisible(!saved);
            CardData product = flow.State.GetCard(SlotType.ProductType);
            string defaultName = product != null ? product.displayName : "Produk Digital";

            if (tierText != null)
                tierText.text = $"HASIL PRODUK — AI {flow.CurrentTier.ToString().ToUpperInvariant()}";
            RefreshTier(flow.CurrentTier);
            if (statusText != null)
                statusText.text = "Status       : BERHASIL";
            if (productTypeText != null) productTypeText.text = defaultName;
            if (productIcon != null)
            {
                productIcon.sprite = ResolveProductIcon(product);
                productIcon.preserveAspect = true;
                productIcon.color = Color.white;
                productIcon.gameObject.SetActive(productIcon.sprite != null);
            }
            committedProductName = defaultName;
            if (productNameInput != null) productNameInput.SetTextWithoutNotify(defaultName);
            else if (productNameText != null) productNameText.text = defaultName;
            SetProductNameAlignment(TextAlignmentOptions.MidlineLeft);

            SetStat(qualityBox, flow.LastResult.Quality);
            SetStat(relevanceBox, flow.LastResult.Relevansi);
            SetStat(sellValueBox, flow.LastResult.NilaiJual);

            if (recommendationText != null)
                recommendationText.text = string.Join("\n", flow.LastFeedback.Select(line => $"• {line}"));
            if (saveButton != null) saveButton.interactable = !saved;
        }

        void Retry()
        {
            // "Buat Lagi" adalah proyek baru, jadi pilihan tipe produk dimulai
            // kembali dari Tahap 1 dan state hasil sebelumnya dibersihkan penuh.
            ProjectFlowManager.Instance?.BackToStart();
        }

        void CloseWindow()
        {
            CommitName(productNameInput != null ? productNameInput.text : productNameText?.text);
            MainProdukLMWindowUI window = UnityEngine.Object.FindAnyObjectByType<MainProdukLMWindowUI>(
                FindObjectsInactive.Include);
            window?.CloseWindow();
        }

        void BeginNameEdit(string _)
        {
            SetProductNameAlignment(TextAlignmentOptions.MidlineLeft);
        }

        void CommitName(string value)
        {
            string cleaned = value?.Trim();
            if (string.IsNullOrWhiteSpace(cleaned))
                cleaned = string.IsNullOrWhiteSpace(committedProductName)
                    ? "Produk Digital"
                    : committedProductName;
            if (cleaned.Length > 18)
                cleaned = cleaned.Substring(0, 18).TrimEnd();

            committedProductName = cleaned;
            if (productNameInput != null)
                productNameInput.SetTextWithoutNotify(cleaned);
            else if (productNameText != null)
                productNameText.text = cleaned;
            SetProductNameAlignment(TextAlignmentOptions.MidlineLeft);
        }

        void SaveProduct()
        {
            ProjectFlowManager flow = ProjectFlowManager.Instance;
            if (flow == null || !flow.HasGeneratedResult || saved)
                return;

            string requestedName = productNameInput != null ? productNameInput.text : productNameText?.text;
            if (string.IsNullOrWhiteSpace(requestedName))
            {
                if (productNameInput != null) productNameInput.ActivateInputField();
                return;
            }

            CommitName(requestedName);

            MainLarisIDWindowUI larisWindow = UnityEngine.Object.FindAnyObjectByType<MainLarisIDWindowUI>(
                FindObjectsInactive.Include);
            LarisIDManager laris = larisWindow != null ? larisWindow.manager : null;
            laris ??= UnityEngine.Object.FindAnyObjectByType<LarisIDManager>(FindObjectsInactive.Include);
            if (laris == null)
            {
                Debug.LogError("LarisIDManager tidak ditemukan; produk belum dapat disimpan.", this);
                return;
            }

            CardData product = flow.State.GetCard(SlotType.ProductType);
            laris.EnsureInitialized();
            laris.ImportProduct(new LarisProductImportData
            {
                productName = committedProductName,
                iconKey = product != null ? product.cardId : string.Empty,
                category = MapCategory(product),
                description = PromptBuilder.Build(flow.State),
                targetMarket = TargetMarket.General,
                quality = flow.LastResult.Quality,
                relevance = flow.LastResult.Relevansi,
                sellValue = flow.LastResult.NilaiJual
            });

            saved = true;
            flow.MarkLastResultSaved();
            if (saveButton != null) saveButton.interactable = false;
            ShowSavedState();
        }

        void ShowSavedState()
        {
            if (savedBackButton != null && backgroundMenu != null &&
                !savedBackButton.transform.IsChildOf(backgroundMenu))
                savedBackButton.transform.SetParent(backgroundMenu, true);

            SetResultContentVisible(false);
            if (backgroundMenu != null)
                backgroundMenu.gameObject.SetActive(true);
            if (savedBackButton != null)
                savedBackButton.gameObject.SetActive(true);
        }

        void SetResultContentVisible(bool visible)
        {
            if (topBar != null)
            {
                // Topbar tetap hidup sesudah produk disimpan agar tombol Exit
                // selalu dapat digunakan. Isi topbar lain disembunyikan.
                topBar.gameObject.SetActive(true);
                foreach (Transform child in topBar)
                {
                    bool containsExit = exitButton != null &&
                        (child == exitButton.transform || exitButton.transform.IsChildOf(child));
                    bool containsProductLMBrand = productLMBrand != null &&
                        (child == productLMBrand || productLMBrand.IsChildOf(child));
                    child.gameObject.SetActive(visible || containsExit || containsProductLMBrand);
                }
                if (exitButton != null)
                    exitButton.gameObject.SetActive(true);
                if (productLMBrand != null)
                    productLMBrand.gameObject.SetActive(true);
            }
            if (leftPanel != null) leftPanel.gameObject.SetActive(visible);
            if (rightPanel != null) rightPanel.gameObject.SetActive(visible);
            if (backgroundMenu != null) backgroundMenu.gameObject.SetActive(true);
            if (savedBackButton != null)
                savedBackButton.gameObject.SetActive(!visible);
            if (savedMessageObject != null)
                savedMessageObject.SetActive(!visible);
        }

        void ReturnToStageOne()
        {
            ProjectFlowManager.Instance?.BackToStart();
        }

        Button ResolveSavedBackButton()
        {
            Transform candidate = GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(item =>
                    item.name.Contains("kembali", StringComparison.OrdinalIgnoreCase) ||
                    item.name.Contains("back", StringComparison.OrdinalIgnoreCase));

            if (candidate == null)
            {
                TMP_Text label = GetComponentsInChildren<TMP_Text>(true)
                    .FirstOrDefault(text =>
                        text.text.Contains("kembali", StringComparison.OrdinalIgnoreCase));
                candidate = label != null ? label.transform : null;
            }

            while (candidate != null && candidate != transform &&
                   candidate.GetComponent<Graphic>() == null)
                candidate = candidate.parent;

            return candidate != null && candidate != transform
                ? EnsureButton(candidate)
                : null;
        }

        void ResolveTierTexts()
        {
            TMP_Text[] candidates = GetComponentsInChildren<TMP_Text>(true)
                .Where(text =>
                {
                    string descriptor = $"{text.name} {text.text}".ToUpperInvariant();
                    return descriptor.Contains("AI FREE") || descriptor.Contains("AI PLUS") ||
                           descriptor.Contains("AI PRO");
                })
                .ToArray();

            freeTierText = candidates.FirstOrDefault(text =>
                $"{text.name} {text.text}".Contains("FREE", StringComparison.OrdinalIgnoreCase))?.gameObject;
            plusTierText = candidates.FirstOrDefault(text =>
                $"{text.name} {text.text}".Contains("PLUS", StringComparison.OrdinalIgnoreCase))?.gameObject;
            proTierText = candidates.FirstOrDefault(text =>
                $"{text.name} {text.text}".Contains("PRO", StringComparison.OrdinalIgnoreCase))?.gameObject;
            fallbackTierText = candidates.FirstOrDefault() ?? TextOf(Find("AI TIER"));
        }

        void RefreshTier(AITier tier)
        {
            bool hasThreeTierObjects = freeTierText != null && plusTierText != null && proTierText != null;
            if (hasThreeTierObjects)
            {
                freeTierText.SetActive(tier == AITier.Free);
                plusTierText.SetActive(tier == AITier.Plus);
                proTierText.SetActive(tier == AITier.Pro);
                return;
            }

            if (fallbackTierText != null)
            {
                fallbackTierText.gameObject.SetActive(true);
                fallbackTierText.text = $"AI {tier.ToString().ToUpperInvariant()}";
            }
        }

        void SetProductNameAlignment(TextAlignmentOptions alignment)
        {
            if (productNameText != null)
                productNameText.alignment = alignment;
        }

        static ProductCategory MapCategory(CardData product)
        {
            string value = $"{product?.cardId} {product?.displayName}".ToLowerInvariant();
            if (value.Contains("ebook")) return ProductCategory.Ebook;
            if (value.Contains("ppt") || value.Contains("presentasi")) return ProductCategory.Presentation;
            if (value.Contains("belajar") || value.Contains("edukasi")) return ProductCategory.Education;
            if (value.Contains("bisnis")) return ProductCategory.BusinessPlan;
            if (value.Contains("sosial") || value.Contains("konten")) return ProductCategory.SocialMedia;
            return ProductCategory.Template;
        }

        static Sprite ResolveProductIcon(CardData product)
        {
            if (product != null && product.icon != null)
                return product.icon;

            MainProdukLMWindowUI window = UnityEngine.Object.FindAnyObjectByType<MainProdukLMWindowUI>(
                FindObjectsInactive.Include);
            if (window?.productOptions == null)
                return null;

            MainProdukLMWindowUI.ProductOption option = window.productOptions
                .FirstOrDefault(item => item != null && item.card == product);
            return option?.previewIcon;
        }

        static void SetStat(Transform box, int value)
        {
            if (box == null) return;
            TMP_Text score = box.GetComponentsInChildren<TMP_Text>(true)
                .FirstOrDefault(text => text.name.Equals("Skor", StringComparison.OrdinalIgnoreCase));
            if (score != null) score.text = $"{value}%";

            Transform progressFrame = box.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(item => item.name.StartsWith("ProggresFrame", StringComparison.OrdinalIgnoreCase));
            if (progressFrame == null) return;

            Transform[] blocks = progressFrame.GetComponentsInChildren<Transform>(true)
                .Where(item => item.name.StartsWith("BAR", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (blocks.Length == 0) return;

            // Gunakan satu Image sebagai fill bar kontinu. LayoutGroup lama
            // dimatikan supaya lebar fill dapat mengikuti persentase secara bebas.
            Transform fill = blocks[0];
            Transform fillParent = fill.parent;
            LayoutGroup layout = fillParent.GetComponent<LayoutGroup>();
            if (layout != null) layout.enabled = false;

            RectMask2D mask = fillParent.GetComponent<RectMask2D>();
            if (mask == null)
                mask = fillParent.gameObject.AddComponent<RectMask2D>();
            mask.padding = Vector4.zero;

            for (int i = 1; i < blocks.Length; i++)
                blocks[i].gameObject.SetActive(false);

            RectTransform fillRect = fill as RectTransform;
            if (fillRect == null) return;

            float normalized = Mathf.Clamp01(value / 100f);
            fill.gameObject.SetActive(normalized > 0f);
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(normalized, 1f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.offsetMin = new Vector2(1f, 1f);
            fillRect.offsetMax = new Vector2(-1f, -1f);
        }

        Transform Find(string name) => GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(item => item.name.Equals(name, StringComparison.OrdinalIgnoreCase));
        static TMP_Text TextOf(Transform target) => target != null ? target.GetComponent<TMP_Text>() : null;
        static Button EnsureButton(Transform target)
        {
            if (target == null) return null;
            Button button = target.GetComponent<Button>() ?? target.gameObject.AddComponent<Button>();
            button.targetGraphic ??= target.GetComponent<Graphic>();
            return button;
        }
    }

    static class ProdukLMStage3Bootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            foreach (Transform candidate in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            {
                if (!candidate.name.Equals("Page3-hasilproduk", StringComparison.OrdinalIgnoreCase)) continue;
                if (candidate.GetComponent<ProdukLMStage3UI>() == null)
                    candidate.gameObject.AddComponent<ProdukLMStage3UI>();
                break;
            }
        }
    }
}
