using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProdukLM
{
    /// <summary>
    /// Adapter untuk memakai mekanik ProdukLM di desktop scene Main tanpa
    /// mengubah layout karya desainer. Komponen ini diletakkan di UI root yang
    /// selalu aktif, sedangkan window ProdukLM boleh dinonaktifkan.
    /// </summary>
    public class MainProdukLMWindowUI : MonoBehaviour
    {
        [HideInInspector] public int integrationVersion;

        [Serializable]
        public class ProductOption
        {
            public Button button;
            public CardData card;
            public Graphic highlightGraphic;
            public Sprite previewIcon;
        }

        [Header("Desktop dan Window")]
        public GameObject windowRoot;
        public Transform windowTaskbar;
        public Button openButton;
        public Button[] closeButtons;

        [Header("Tahap 1 - Pilih Produk")]
        public ProjectFlowManager flowManager;
        public ProductOption[] productOptions;
        public Button confirmSelectionButton;
        public Button nextStageButton;
        public Image selectedProductIcon;
        public TMP_Text selectedProductText;
        public TMP_Text selectionDescriptionText;
        public Color selectedColor = new Color(0.55f, 0.85f, 1f, 1f);

        CardData selectedProduct;
        Color[] defaultOptionColors = Array.Empty<Color>();

        void Awake()
        {
            CacheDefaultColors();
            BindButtons();
            RefreshSelection();
        }

        void Start()
        {
            if (windowRoot != null)
                windowRoot.SetActive(false);
        }

        void CacheDefaultColors()
        {
            if (productOptions == null)
                productOptions = Array.Empty<ProductOption>();

            defaultOptionColors = new Color[productOptions.Length];
            for (int i = 0; i < productOptions.Length; i++)
            {
                Graphic graphic = productOptions[i]?.highlightGraphic;
                defaultOptionColors[i] = graphic != null ? graphic.color : Color.white;
            }
        }

        void BindButtons()
        {
            if (openButton != null)
                openButton.onClick.AddListener(OpenWindow);

            if (closeButtons != null)
            {
                foreach (Button closeButton in closeButtons)
                {
                    if (closeButton != null)
                        closeButton.onClick.AddListener(CloseWindow);
                }
            }

            if (confirmSelectionButton != null)
                confirmSelectionButton.onClick.AddListener(ContinueToBuilder);
            if (nextStageButton != null && nextStageButton != confirmSelectionButton)
                nextStageButton.onClick.AddListener(ContinueToBuilder);

            for (int i = 0; i < productOptions.Length; i++)
            {
                int capturedIndex = i;
                ProductOption option = productOptions[i];
                if (option?.button != null)
                    option.button.onClick.AddListener(() => SelectProduct(capturedIndex));
            }
        }

        public void OpenWindow()
        {
            if (windowRoot == null)
                return;

            windowRoot.SetActive(true);
            windowRoot.transform.SetAsLastSibling();
            KeepTaskbarOnTop();

            if (flowManager != null)
                flowManager.BackToStart();

            selectedProduct = null;
            RefreshSelection();
        }

        public void CloseWindow()
        {
            if (flowManager != null && flowManager.gameObject.activeInHierarchy)
                flowManager.BackToStart();

            selectedProduct = null;
            RefreshSelection();

            if (windowRoot != null)
                windowRoot.SetActive(false);
        }

        public void SelectProduct(int optionIndex)
        {
            if (optionIndex < 0 || optionIndex >= productOptions.Length)
                return;

            ProductOption option = productOptions[optionIndex];
            if (option == null || option.card == null)
                return;

            selectedProduct = option.card;
            RefreshSelection();
        }

        public void ContinueToBuilder()
        {
            if (selectedProduct == null || flowManager == null)
                return;

            flowManager.StartProject(selectedProduct);
            KeepTaskbarOnTop();
        }

        public void KeepTaskbarOnTop()
        {
            if (windowTaskbar != null)
                windowTaskbar.SetAsLastSibling();
        }

        void RefreshSelection()
        {
            bool hasSelection = selectedProduct != null;

            if (confirmSelectionButton != null)
                confirmSelectionButton.interactable = hasSelection;
            if (nextStageButton != null)
                nextStageButton.interactable = hasSelection;

            if (selectedProductText != null)
                selectedProductText.text = hasSelection
                    ? selectedProduct.displayName
                    : "Pilih jenis produk";

            if (selectedProductIcon != null)
            {
                ProductOption selectedOption = Array.Find(
                    productOptions,
                    option => option?.card == selectedProduct);
                selectedProductIcon.sprite = selectedOption?.previewIcon;
                selectedProductIcon.gameObject.SetActive(
                    hasSelection && selectedProductIcon.sprite != null);
            }

            if (selectionDescriptionText != null)
                selectionDescriptionText.text = hasSelection
                    ? $"Buat {selectedProduct.displayName} sesuai tujuan, target pengguna, " +
                      "konten, gaya penyajian, dan fokus AI yang kamu pilih."
                    : "Klik salah satu produk digital di sebelah kanan.";

            for (int i = 0; i < productOptions.Length; i++)
            {
                ProductOption option = productOptions[i];
                Graphic graphic = option?.highlightGraphic;
                if (graphic == null)
                    continue;

                graphic.color = option.card == selectedProduct
                    ? selectedColor
                    : defaultOptionColors[i];
            }
        }
    }
}
