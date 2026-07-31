using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace ProdukLM
{
    // Tier dan limit pada manager ini juga dipakai oleh scene ProdukLM lama.
    // Taruh script ini di satu GameObject kosong, misal "GameManager".
    // Semua script lain (SlotUI, CardUI, CardLibraryManager, dst) manggil
    // ProjectFlowManager.Instance buat baca/ubah state.
    public class ProjectFlowManager : MonoBehaviour
    {
        public static ProjectFlowManager Instance { get; private set; }

        public ProjectState State { get; private set; } = new ProjectState();
        public StatsResult LastResult { get; private set; }
        public List<string> LastFeedback { get; private set; } = new List<string>();

        [Header("Tier AI")]
        [SerializeField] AITier currentTier = AITier.Free;
        [SerializeField, Range(0f, 0.8f)] float freeQualityBoost;
        [SerializeField, Range(0f, 0.8f)] float plusQualityBoost = 0.18f;
        [SerializeField, Range(0f, 0.8f)] float proQualityBoost = 0.32f;

        [Header("Limit harian per tier (bisa diatur di Inspector)")]
        [FormerlySerializedAs("dailyProductLimit")]
        [SerializeField, Min(1)] int freeDailyLimit = 5;
        [SerializeField, Min(1)] int plusDailyLimit = 8;
        [SerializeField, Min(1)] int proDailyLimit = 12;

        public AITier CurrentTier => currentTier;
        public int DailyProductLimit
        {
            get
            {
                return currentTier switch
                {
                    AITier.Plus => Mathf.Max(1, plusDailyLimit),
                    AITier.Pro => Mathf.Max(1, proDailyLimit),
                    _ => Mathf.Max(1, freeDailyLimit)
                };
            }
        }
        public int ProductsCreatedToday => DailyGenerationCounter.Count;
        public int RemainingProductsToday => DailyGenerationCounter.Remaining(DailyProductLimit);
        public bool CanCreateProductToday => RemainingProductsToday > 0;

        // Dipanggil semua listener (PromptPreviewUI, CompatibilityMeterUI, dll)
        public event Action OnSlotChanged;
        public event Action OnGenerated; // dipanggil StatsResultUI setelah tombol Generate ditekan
        public event Action OnDailyLimitChanged;
        public event Action OnAITierChanged;
        public event Action<string> OnGenerationBlocked;

        [Header("Referensi panel (drag di Inspector)")]
        public GameObject productTypeSelectPanel; // Tahap 1: grid 3x2
        public GameObject slotAndLibraryPanel;    // Tahap 2: split screen
        public GameObject resultPanel;            // Tahap 3: layar hasil Generate

        void Awake()
        {
            if (productTypeSelectPanel == null || slotAndLibraryPanel == null || resultPanel == null)
            {
                Debug.LogError(
                    $"{nameof(ProjectFlowManager)} pada '{name}' belum memiliki semua referensi panel.",
                    this);
                enabled = false;
                return;
            }

            if (Instance != null && Instance != this)
            {
                Debug.LogError(
                    $"Duplikat {nameof(ProjectFlowManager)} ditemukan pada '{name}'. " +
                    $"Gunakan hanya satu instance di scene.",
                    this);
                enabled = false;
                return;
            }

            Instance = this;
            DailyGenerationCounter.EnsureCurrent();
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus && DailyGenerationCounter.EnsureCurrent())
                OnDailyLimitChanged?.Invoke();
        }

        // Dipanggil dari ProductTypeSelectUI saat pemain pilih Product Type
        public void StartProject(CardData productTypeCard)
        {
            State = new ProjectState();
            State.SetCard(SlotType.ProductType, productTypeCard);

            productTypeSelectPanel.SetActive(false);
            slotAndLibraryPanel.SetActive(true);
            resultPanel.SetActive(false);

            NotifySlotChanged();
        }

        // Dipanggil dari SlotUI.OnDrop
        public void AssignCardToSlot(SlotType slot, CardData card)
        {
            State.SetCard(slot, card);
            NotifySlotChanged();

            if (State.GetNextEmptySlot() == null)
                StartCoroutine(GenerateNextFrame());
        }

        IEnumerator GenerateNextFrame()
        {
            // Biarkan OnEndDrag membersihkan kartu yang dipilih lebih dulu.
            yield return null;
            TryGenerate();
        }

        // Dipanggil dari tombol Generate. Cuma valid kalau 6 slot udah penuh.
        public bool TryGenerate()
        {
            if (resultPanel.activeSelf)
                return false; // cegah Generate ganda mengurangi kuota dua kali

            if (State.GetNextEmptySlot() != null)
                return false; // masih ada slot kosong, jangan lanjut

            if (!CanCreateProductToday)
            {
                OnGenerationBlocked?.Invoke(
                    $"Batas produksi harian tercapai ({DailyProductLimit}/{DailyProductLimit}).");
                OnDailyLimitChanged?.Invoke();
                return false;
            }

            LastResult = AITierStats.ApplyBoost(
                StatsCalculator.Calculate(State),
                GetCurrentQualityBoost());
            LastFeedback = FeedbackGenerator.Generate(State);
            RegisterProductCreated();

            slotAndLibraryPanel.SetActive(false);
            resultPanel.SetActive(true);

            OnGenerated?.Invoke();
            return true;
        }

        // Dipanggil dari tombol "kembali ke Tahap 1" di panel Tahap 3
        public void BackToStart()
        {
            State = new ProjectState();
            LastResult = default;
            LastFeedback.Clear();

            resultPanel.SetActive(false);
            slotAndLibraryPanel.SetActive(false);
            productTypeSelectPanel.SetActive(true);
        }

        // Tahap 3 -> Tahap 2, atau Tahap 2 -> Tahap 1 sekaligus membatalkan project.
        public void BackOneStep()
        {
            if (resultPanel.activeSelf)
            {
                resultPanel.SetActive(false);
                ResetBuilderSelections();
                slotAndLibraryPanel.SetActive(true);
                NotifySlotChanged();
                return;
            }

            if (slotAndLibraryPanel.activeSelf)
                BackToStart();
        }

        public void SetAITier(AITier tier)
        {
            if (currentTier == tier)
                return;

            currentTier = tier;
            OnAITierChanged?.Invoke();
            OnDailyLimitChanged?.Invoke();
        }

        void NotifySlotChanged()
        {
            OnSlotChanged?.Invoke();
        }

        void ResetBuilderSelections()
        {
            // Semua slot dikosongkan. CardLibrary memiliki kartu Product Type,
            // jadi builder akan memulai lagi dari pilihan pertama.
            State = new ProjectState();
            LastResult = default;
            LastFeedback.Clear();
        }

        void RegisterProductCreated()
        {
            DailyGenerationCounter.Consume();
            OnDailyLimitChanged?.Invoke();
        }

        float GetCurrentQualityBoost()
        {
            return currentTier switch
            {
                AITier.Plus => plusQualityBoost,
                AITier.Pro => proQualityBoost,
                _ => freeQualityBoost
            };
        }
    }
}
