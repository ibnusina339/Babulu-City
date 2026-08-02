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
        ProjectFlowManager flow;
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
            ProjectFlowManager.OnInstanceReady += Bind;
            Bind(ProjectFlowManager.Instance);
        }

        void OnDisable()
        {
            ProjectFlowManager.OnInstanceReady -= Bind;
            Unbind();
        }

        void Bind(ProjectFlowManager manager)
        {
            if (manager == null || flow == manager)
                return;

            Unbind();
            flow = manager;
            flow.OnSlotChanged += HandleSlotChanged;
            HandleSlotChanged();
        }

        void Unbind()
        {
            if (flow == null)
                return;

            flow.OnSlotChanged -= HandleSlotChanged;
            flow = null;
        }

        void HandleSlotChanged()
        {
            if (flow != null)
                Refresh(flow.State);
        }

        public void OnDrop(PointerEventData eventData)
        {
            var cardUI = eventData.pointerDrag?.GetComponent<CardUI>();
            if (cardUI == null || cardUI.data == null || flow == null)
                return;

            if (cardUI.data.slotType != slotType)
                return;

            // Mencegah slot dilompati atau ditimpa lewat drag yang tidak sengaja.
            if (flow.State.GetNextEmptySlot() != slotType)
                return;

            cardUI.AcceptDrop();
            flow.AssignCardToSlot(slotType, cardUI.data);
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
