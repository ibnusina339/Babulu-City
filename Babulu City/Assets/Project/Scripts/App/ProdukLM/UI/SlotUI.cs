using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace ProdukLM
{
    // Taruh di tiap GameObject slot (6 buah, satu per SlotType) di panel kiri.
    // Butuh Image (buat area drop) dan TMP_Text (buat nampilin nama kartu terisi).
    public class SlotUI : MonoBehaviour, IDropHandler
    {
        public SlotType slotType;
        public TMP_Text label;
        public GameObject activeIndicator; // border/highlight, aktifkan kalau ini slot kosong berikutnya

        void OnEnable()
        {
            ProjectFlowManager.Instance.OnSlotChanged += HandleSlotChanged;
            HandleSlotChanged(); // refresh langsung saat panel Tahap 2 baru aktif
        }

        void OnDisable()
        {
            if (ProjectFlowManager.Instance != null)
                ProjectFlowManager.Instance.OnSlotChanged -= HandleSlotChanged;
        }

        void HandleSlotChanged()
        {
            Refresh(ProjectFlowManager.Instance.State);
        }

        public void OnDrop(PointerEventData eventData)
        {
            var cardUI = eventData.pointerDrag?.GetComponent<CardUI>();
            if (cardUI == null) return;

            // Tolak kalau tipe kartu nggak cocok sama slot ini
            if (cardUI.data.slotType != slotType) return;

            cardUI.AcceptDrop();
            ProjectFlowManager.Instance.AssignCardToSlot(slotType, cardUI.data);
        }

        // Panggil ini dari listener OnSlotChanged buat refresh tampilan
        public void Refresh(ProjectState state)
        {
            bool filled = state.IsSlotFilled(slotType);
            label.text = filled ? state.GetCard(slotType).displayName : $"{slotType}: —";

            bool isNextEmpty = state.GetNextEmptySlot() == slotType;
            if (activeIndicator != null)
                activeIndicator.SetActive(isNextEmpty);
        }
    }
}
