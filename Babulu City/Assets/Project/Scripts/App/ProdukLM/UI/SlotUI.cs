using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProdukLM
{
    // Taruh di tiap GameObject slot (6 buah, satu per SlotType) di panel kiri.
    // Butuh Image (buat area drop) dan TMP_Text (buat nampilin nama kartu terisi).
    public class SlotUI : MonoBehaviour, IDropHandler
    {
        public SlotType slotType;
        public TMP_Text label;
        public GameObject activeIndicator;

        [Header("Visual State")]
        public Graphic backgroundGraphic;
        public Outline focusOutline;
        public Color emptyColor = new Color(0.22f, 0.23f, 0.25f, 1f);
        public Color filledColor = new Color(0.12f, 0.25f, 0.42f, 1f);
        public Color focusOutlineColor = new Color(0.55f, 0.85f, 1f, 1f);

        void OnEnable()
        {
            if (ProjectFlowManager.Instance == null)
            {
                Debug.LogError(
                    $"{nameof(SlotUI)} pada '{name}' tidak menemukan {nameof(ProjectFlowManager)}.",
                    this);
                return;
            }

            ProjectFlowManager.Instance.OnSlotChanged += HandleSlotChanged;
            HandleSlotChanged();
        }

        void OnDisable()
        {
            if (ProjectFlowManager.Instance != null)
                ProjectFlowManager.Instance.OnSlotChanged -= HandleSlotChanged;
        }

        void HandleSlotChanged()
        {
            if (ProjectFlowManager.Instance != null)
                Refresh(ProjectFlowManager.Instance.State);
        }

        public void OnDrop(PointerEventData eventData)
        {
            var cardUI = eventData.pointerDrag?.GetComponent<CardUI>();
            if (cardUI == null || cardUI.data == null || ProjectFlowManager.Instance == null)
                return;

            if (cardUI.data.slotType != slotType)
                return;

            // Mencegah slot dilompati atau ditimpa lewat drag yang tidak sengaja.
            if (ProjectFlowManager.Instance.State.GetNextEmptySlot() != slotType)
                return;

            cardUI.AcceptDrop();
            ProjectFlowManager.Instance.AssignCardToSlot(slotType, cardUI.data);
        }

        public void Refresh(ProjectState state)
        {
            bool filled = state.IsSlotFilled(slotType);
            if (label != null)
                label.text = filled
                    ? state.GetCard(slotType).displayName
                    : GetSlotLabel(slotType);

            bool isNextEmpty = state.GetNextEmptySlot() == slotType;

            if (backgroundGraphic != null)
                backgroundGraphic.color = filled ? filledColor : emptyColor;

            if (focusOutline != null)
            {
                focusOutline.effectColor = focusOutlineColor;
                focusOutline.enabled = isNextEmpty;
            }

            if (activeIndicator != null)
                activeIndicator.SetActive(isNextEmpty);
        }

        static string GetSlotLabel(SlotType type)
        {
            return type switch
            {
                SlotType.ProductType => "Produk",
                SlotType.Purpose => "Tujuan",
                SlotType.Audience => "Target Pengguna",
                SlotType.ContentFocus => "Konten",
                SlotType.Style => "Gaya Penyajian",
                SlotType.AIOptimization => "Fokus AI",
                _ => type.ToString()
            };
        }
    }
}
