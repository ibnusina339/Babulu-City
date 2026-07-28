using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProdukLM
{
    // Taruh script ini di satu GameObject kosong, misal "GameManager".
    // Semua script lain (SlotUI, CardUI, CardLibraryManager, dst) manggil
    // ProjectFlowManager.Instance buat baca/ubah state.
    public class ProjectFlowManager : MonoBehaviour
    {
        public static ProjectFlowManager Instance { get; private set; }

        public ProjectState State { get; private set; } = new ProjectState();
        public StatsResult LastResult { get; private set; }
        public List<string> LastFeedback { get; private set; } = new List<string>();

        // Dipanggil semua listener (PromptPreviewUI, CompatibilityMeterUI, dll)
        public event Action OnSlotChanged;
        public event Action OnGenerated; // dipanggil StatsResultUI setelah tombol Generate ditekan

        [Header("Referensi panel (drag di Inspector)")]
        public GameObject productTypeSelectPanel; // Tahap 1: grid 3x2
        public GameObject slotAndLibraryPanel;    // Tahap 2: split screen
        public GameObject resultPanel;            // Tahap 3: layar hasil Generate

        void Awake()
        {
            Instance = this;
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
        }

        // Dipanggil dari tombol Generate. Cuma valid kalau 6 slot udah penuh.
        public bool TryGenerate()
        {
            if (State.GetNextEmptySlot() != null)
                return false; // masih ada slot kosong, jangan lanjut

            LastResult = StatsCalculator.Calculate(State);
            LastFeedback = FeedbackGenerator.Generate(State);

            slotAndLibraryPanel.SetActive(false);
            resultPanel.SetActive(true);

            OnGenerated?.Invoke();
            return true;
        }

        // Dipanggil dari tombol "kembali ke Tahap 1" di panel Tahap 3
        public void BackToStart()
        {
            resultPanel.SetActive(false);
            productTypeSelectPanel.SetActive(true);
        }

        void NotifySlotChanged()
        {
            OnSlotChanged?.Invoke();
        }
    }
}
