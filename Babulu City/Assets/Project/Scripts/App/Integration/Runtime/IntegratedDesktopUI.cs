using System;
using System.Collections.Generic;
using System.Linq;
using LarisID;
using ProdukLM;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace IntegratedApps
{
    public class IntegratedDesktopUI : MonoBehaviour
    {
        [Header("System")]
        public LarisIDManager larisManager;

        [Header("Desktop dan window")]
        public GameObject produkLMWindow;
        public GameObject larisIDWindow;
        public Button openProdukLMButton;
        public Button openLarisIDButton;
        public Button closeProdukLMButton;
        public Button closeLarisIDButton;
        public Image produkLMDesktopIcon;
        public Image larisIDDesktopIcon;
        [Tooltip("Drag icon desain final ke sini nanti.")]
        public Sprite produkLMIcon;
        [Tooltip("Drag icon desain final ke sini nanti.")]
        public Sprite larisIDIcon;

        [Header("Limit produksi harian")]
        [Tooltip("Jumlah produk yang boleh dibuat setiap hari permainan.")]
        [FormerlySerializedAs("freeDailyLimit")]
        [Min(1)] public int dailyProductLimit = 5;
        public Button resetLimitTestButton;
        public TMP_Text dailyLimitText;

        [Header("ProdukLM - katalog kartu")]
        public List<CardData> cardCatalog = new();
        public SlotSelectorReferences[] slotSelectors;
        public TMP_Text promptPreviewText;
        public Button generateButton;
        public TMP_Text produkStatusText;

        [Header("ProdukLM - hasil dan save")]
        public GameObject resultEmptyState;
        public GameObject resultContent;
        public Image generatedFileIcon;
        public TMP_Text generatedFileTypeText;
        public TMP_Text generatedProductTypeText;
        public TMP_Text generatedTierText;
        public TMP_Text generatedQualityText;
        public TMP_Text generatedRelevanceText;
        public TMP_Text generatedSellValueText;
        public TMP_Text generatedPromptText;
        public TMP_InputField saveProductNameInput;
        public Button saveToLarisButton;
        public Button discardResultButton;
        public TMP_Text saveMessageText;

        [Header("Icon produk - bisa diganti di Inspector")]
        public Sprite pdfIcon;
        public Sprite docxIcon;
        public Sprite pptxIcon;
        public Sprite imageIcon;
        public Sprite genericFileIcon;

        readonly List<CardData>[] cardsBySlot = new List<CardData>[6];
        readonly int[] selectionIndices = new int[6];
        ProjectState generatedState;
        StatsResult generatedStats;
        string generatedIconKey;
        string generatedPrompt;
        bool hasGeneratedResult;
        bool generatedResultSaved;

        public int CurrentDailyLimit => Mathf.Max(1, dailyProductLimit);

        void Start()
        {
            BindButtons();
            BuildCardCatalog();
            ApplyReplaceableIcons();

            produkLMWindow.SetActive(false);
            larisIDWindow.SetActive(false);
            RefreshProdukLM();
            RefreshGeneratedResult();
        }

        void BindButtons()
        {
            openProdukLMButton.onClick.AddListener(OpenProdukLM);
            openLarisIDButton.onClick.AddListener(OpenLarisID);
            closeProdukLMButton.onClick.AddListener(() => produkLMWindow.SetActive(false));
            closeLarisIDButton.onClick.AddListener(() => larisIDWindow.SetActive(false));
            // Tombol tier AI sudah dihapus dari desain, jadi referensinya tidak
            // lagi dipakai. Sisanya dijaga null-safe supaya window tetap jalan
            // walaupun ada objek yang sudah dihapus dari scene.
            if (resetLimitTestButton != null)
                resetLimitTestButton.onClick.AddListener(ResetLimitForTesting);
            generateButton.onClick.AddListener(GenerateProduct);
            saveToLarisButton.onClick.AddListener(SaveGeneratedProduct);
            discardResultButton.onClick.AddListener(DiscardGeneratedResult);

            for (int i = 0; i < slotSelectors.Length; i++)
            {
                int captured = i;
                slotSelectors[i].previousButton.onClick.AddListener(() => ChangeCard(captured, -1));
                slotSelectors[i].nextButton.onClick.AddListener(() => ChangeCard(captured, 1));
            }
        }

        void BuildCardCatalog()
        {
            for (int i = 0; i < cardsBySlot.Length; i++)
            {
                SlotType slot = (SlotType)i;
                cardsBySlot[i] = cardCatalog
                    .Where(card => card != null && card.slotType == slot)
                    .OrderBy(card => card.displayName)
                    .ToList();
                selectionIndices[i] = 0;
            }
        }

        void ApplyReplaceableIcons()
        {
            if (produkLMIcon != null)
            {
                produkLMDesktopIcon.sprite = produkLMIcon;
                produkLMDesktopIcon.color = Color.white;
            }
            if (larisIDIcon != null)
            {
                larisIDDesktopIcon.sprite = larisIDIcon;
                larisIDDesktopIcon.color = Color.white;
            }
        }

        void OpenProdukLM()
        {
            larisIDWindow.SetActive(false);
            produkLMWindow.SetActive(true);
            produkLMWindow.transform.SetAsLastSibling();
            RefreshProdukLM();
        }

        void OpenLarisID()
        {
            produkLMWindow.SetActive(false);
            larisIDWindow.SetActive(true);
            larisIDWindow.transform.SetAsLastSibling();
        }

        void ResetLimitForTesting()
        {
            DailyGenerationCounter.ResetForTesting();
            produkStatusText.text = "Counter harian di-reset untuk testing.";
            RefreshProdukLM();
        }

        void ChangeCard(int selectorIndex, int direction)
        {
            if (selectorIndex < 0 || selectorIndex >= slotSelectors.Length)
                return;

            int slotIndex = (int)slotSelectors[selectorIndex].slotType;
            List<CardData> cards = cardsBySlot[slotIndex];
            if (cards == null || cards.Count == 0)
                return;

            selectionIndices[slotIndex] =
                (selectionIndices[slotIndex] + direction + cards.Count) % cards.Count;
            RefreshProdukLM();
        }

        void RefreshProdukLM()
        {
            ProjectState previewState = CreateStateFromSelections();
            for (int i = 0; i < slotSelectors.Length; i++)
            {
                int slotIndex = (int)slotSelectors[i].slotType;
                CardData card = previewState.GetCard(slotSelectors[i].slotType);
                slotSelectors[i].valueText.text = card != null ? card.displayName : "Kartu belum tersedia";
                bool available = cardsBySlot[slotIndex] != null && cardsBySlot[slotIndex].Count > 1;
                slotSelectors[i].previousButton.interactable = available;
                slotSelectors[i].nextButton.interactable = available;
            }

            promptPreviewText.text = PromptBuilder.Build(previewState);
            int remaining = DailyGenerationCounter.Remaining(CurrentDailyLimit);
            if (dailyLimitText != null)
                dailyLimitText.text = $"Sisa hari ini  {remaining}/{CurrentDailyLimit}";
            generateButton.interactable =
                remaining > 0 && previewState.GetNextEmptySlot() == null;
        }

        void GenerateProduct()
        {
            if (!DailyGenerationCounter.CanGenerate(CurrentDailyLimit))
            {
                produkStatusText.text = "Limit produksi hari ini sudah habis.";
                RefreshProdukLM();
                return;
            }

            ProjectState state = CreateStateFromSelections();
            if (state.GetNextEmptySlot() != null)
            {
                produkStatusText.text = "Semua pilihan harus terisi sebelum Generate.";
                return;
            }

            DailyGenerationCounter.Consume();
            generatedState = CloneState(state);
            generatedPrompt = PromptBuilder.Build(generatedState);
            // Kualitas hasil murni dari skor Affinity/Neutral/Conflict.
            generatedStats = StatsCalculator.Calculate(generatedState);
            generatedIconKey = ResolveIconKey(generatedState.GetCard(SlotType.ProductType));
            hasGeneratedResult = true;
            generatedResultSaved = false;
            saveProductNameInput.SetTextWithoutNotify(string.Empty);
            saveMessageText.text =
                "Generate sudah memakai 1 limit. Isi nama jika hasil ini ingin disimpan.";
            produkStatusText.text =
                "Produk berhasil dibuat. Limit tetap terpakai meskipun hasil dibuang.";
            RefreshGeneratedResult();
            RefreshProdukLM();
        }

        void SaveGeneratedProduct()
        {
            if (!hasGeneratedResult || generatedResultSaved)
                return;

            string productName = saveProductNameInput.text.Trim();
            if (string.IsNullOrWhiteSpace(productName))
            {
                saveMessageText.text = "Nama produk wajib diisi sebelum disimpan.";
                return;
            }

            CardData productCard = generatedState.GetCard(SlotType.ProductType);
            CardData audienceCard = generatedState.GetCard(SlotType.Audience);
            var data = new LarisProductImportData
            {
                productName = productName,
                iconKey = generatedIconKey,
                category = ResolveCategory(productCard),
                targetMarket = ResolveTarget(audienceCard),
                description = generatedPrompt,
                quality = generatedStats.Quality,
                relevance = generatedStats.Relevansi,
                sellValue = generatedStats.NilaiJual,
                aesthetic = Mathf.Clamp(Mathf.RoundToInt(
                    generatedStats.Quality * .70f + generatedStats.Relevansi * .30f), 0, 100),
                creativity = Mathf.Clamp(Mathf.RoundToInt(
                    generatedStats.Relevansi * .65f + 25f), 0, 100),
                professionalism = Mathf.Clamp(Mathf.RoundToInt(
                    generatedStats.Quality * .65f + generatedStats.NilaiJual * .35f), 0, 100)
            };

            larisManager.ImportProduct(data);
            generatedResultSaved = true;
            saveMessageText.text =
                $"'{productName}' masuk ke Library Laris.ID sebagai Draft. Atur harga lalu publikasikan manual.";
            saveToLarisButton.interactable = false;
        }

        void DiscardGeneratedResult()
        {
            hasGeneratedResult = false;
            generatedResultSaved = false;
            generatedState = null;
            generatedPrompt = string.Empty;
            saveProductNameInput.SetTextWithoutNotify(string.Empty);
            produkStatusText.text =
                "Hasil dibuang. Limit Generate yang sudah digunakan tidak dikembalikan.";
            RefreshGeneratedResult();
        }

        void RefreshGeneratedResult()
        {
            resultEmptyState.SetActive(!hasGeneratedResult);
            resultContent.SetActive(hasGeneratedResult);
            if (!hasGeneratedResult)
                return;

            SetFileIcon(generatedFileIcon, generatedFileTypeText, generatedIconKey);
            CardData productCard = generatedState.GetCard(SlotType.ProductType);
            generatedProductTypeText.text =
                productCard != null ? productCard.displayName : "Produk Digital";
            // Label tier lama tetap dijaga null-safe bila objeknya sudah dihapus.
            if (generatedTierText != null)
                generatedTierText.gameObject.SetActive(false);
            generatedQualityText.text = $"{generatedStats.Quality}%";
            generatedRelevanceText.text = $"{generatedStats.Relevansi}%";
            generatedSellValueText.text = $"{generatedStats.NilaiJual}%";
            generatedPromptText.text = generatedPrompt;
            saveToLarisButton.interactable = !generatedResultSaved;
        }

        ProjectState CreateStateFromSelections()
        {
            var state = new ProjectState();
            for (int i = 0; i < cardsBySlot.Length; i++)
            {
                List<CardData> cards = cardsBySlot[i];
                if (cards == null || cards.Count == 0)
                    continue;
                int index = Mathf.Clamp(selectionIndices[i], 0, cards.Count - 1);
                state.SetCard((SlotType)i, cards[index]);
            }
            return state;
        }

        static ProjectState CloneState(ProjectState source)
        {
            var clone = new ProjectState();
            for (int i = 0; i < 6; i++)
                clone.SetCard((SlotType)i, source.GetCard((SlotType)i));
            return clone;
        }

        void SetFileIcon(Image image, TMP_Text fallbackLabel, string iconKey)
        {
            Sprite sprite = ResolveFileSprite(iconKey);
            image.sprite = sprite;
            image.color = sprite != null ? Color.white : ResolveFallbackColor(iconKey);
            fallbackLabel.text = sprite != null
                ? string.Empty
                : (string.IsNullOrWhiteSpace(iconKey) ? "FILE" : iconKey.ToUpperInvariant());
        }

        Sprite ResolveFileSprite(string iconKey)
        {
            return (iconKey ?? string.Empty).ToLowerInvariant() switch
            {
                "pdf" => pdfIcon,
                "docx" => docxIcon,
                "pptx" => pptxIcon,
                "png" => imageIcon,
                _ => genericFileIcon
            };
        }

        static Color ResolveFallbackColor(string iconKey)
        {
            return (iconKey ?? string.Empty).ToLowerInvariant() switch
            {
                "pdf" => new Color(.85f, .22f, .30f),
                "docx" => new Color(.20f, .45f, .88f),
                "pptx" => new Color(.90f, .38f, .18f),
                "png" => new Color(.22f, .70f, .58f),
                _ => new Color(.42f, .39f, 1f)
            };
        }

        static string ResolveIconKey(CardData productCard)
        {
            string name = productCard != null ? productCard.displayName.ToLowerInvariant() : string.Empty;
            if (name.Contains("ppt") || name.Contains("presentasi")) return "pptx";
            if (name.Contains("dokumen")) return "docx";
            if (name.Contains("infograf")) return "png";
            return "pdf";
        }

        static ProductCategory ResolveCategory(CardData productCard)
        {
            string name = productCard != null ? productCard.displayName.ToLowerInvariant() : string.Empty;
            if (name.Contains("book")) return ProductCategory.Ebook;
            if (name.Contains("ppt") || name.Contains("presentasi")) return ProductCategory.Presentation;
            if (name.Contains("modul")) return ProductCategory.Education;
            if (name.Contains("infograf")) return ProductCategory.SocialMedia;
            return ProductCategory.Template;
        }

        static TargetMarket ResolveTarget(CardData audienceCard)
        {
            string name = audienceCard != null ? audienceCard.displayName.ToLowerInvariant() : string.Empty;
            if (name.Contains("siswa") || name.Contains("mahasiswa") || name.Contains("anak"))
                return TargetMarket.Students;
            if (name.Contains("creator"))
                return TargetMarket.ContentCreators;
            if (name.Contains("wirausaha") || name.Contains("investor"))
                return TargetMarket.Businesses;
            if (name.Contains("karyawan") || name.Contains("freelancer") || name.Contains("graduate"))
                return TargetMarket.Professionals;
            return TargetMarket.General;
        }
    }
}
