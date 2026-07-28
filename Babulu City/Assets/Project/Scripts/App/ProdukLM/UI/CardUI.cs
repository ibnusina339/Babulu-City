using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProdukLM
{
    // Taruh di prefab kartu yang di-spawn CardLibraryManager.
    // Butuh CanvasGroup di GameObject yang sama (buat matiin raycast pas drag).
    [RequireComponent(typeof(CanvasGroup))]
    public class CardUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public CardData data;
        public TMP_Text nameText; // drag child 'NameText' ke sini di prefab

        CanvasGroup canvasGroup;
        RectTransform rect;
        Transform originalParent;
        Vector2 originalPosition;
        Canvas rootCanvas;

        void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            rect = GetComponent<RectTransform>();
            rootCanvas = GetComponentInParent<Canvas>();
        }

        // Dipanggil CardLibraryManager tiap kartu baru di-spawn
        public void SetData(CardData card)
        {
            data = card;
            if (nameText != null)
                nameText.text = card.displayName;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            originalParent = transform.parent;
            originalPosition = rect.anchoredPosition;

            // Pindah ke root canvas biar nggak ke-clip sama panel library
            transform.SetParent(rootCanvas.transform, true);
            canvasGroup.blocksRaycasts = false; // biar SlotUI di bawahnya bisa deteksi OnDrop
        }

        public void OnDrag(PointerEventData eventData)
        {
            rect.position = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            canvasGroup.blocksRaycasts = true;

            // Kalau nggak ke-drop di slot manapun (SlotUI yang handle assign),
            // kartu snap-back ke posisi awal di library.
            if (transform.parent == rootCanvas.transform)
            {
                transform.SetParent(originalParent, true);
                rect.anchoredPosition = originalPosition;
            }
        }
    }
}
