using System;
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

        // Dipanggil semua listener (PromptPreviewUI, CompatibilityMeterUI, dll)
        public event Action OnSlotChanged;

        [Header("Referensi panel (drag di Inspector)")]
        public GameObject productTypeSelectPanel; // Tahap 1: grid 3x2
        public GameObject slotAndLibraryPanel;    // Tahap 2: split screen

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

            NotifySlotChanged();
        }

        // Dipanggil dari SlotUI.OnDrop
        public void AssignCardToSlot(SlotType slot, CardData card)
        {
            State.SetCard(slot, card);
            NotifySlotChanged();
        }

        void NotifySlotChanged()
        {
            OnSlotChanged?.Invoke();
        }
    }
}
