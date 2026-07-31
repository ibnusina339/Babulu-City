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
        RectTransform rootCanvasRect;
        Camera rootCanvasCamera;
        bool dropAccepted;

        void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            rect = GetComponent<RectTransform>();

            // Seluruh badan kartu harus menjadi area drag. Prefab lama memiliki
            // Raycast Target nonaktif pada Image sehingga drag hanya terdeteksi
            // bila pointer tepat mengenai glyph teks.
            var dragGraphic = GetComponent<Graphic>();
            if (dragGraphic != null)
                dragGraphic.raycastTarget = true;
            if (nameText != null)
                nameText.raycastTarget = false;

            var parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas != null)
            {
                rootCanvas = parentCanvas.rootCanvas;
                rootCanvasRect = rootCanvas.GetComponent<RectTransform>();
                rootCanvasCamera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                    ? null
                    : rootCanvas.worldCamera;
            }
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
            if (rootCanvas == null)
            {
                Debug.LogError($"{nameof(CardUI)} pada '{name}' tidak berada di dalam Canvas.", this);
                return;
            }

            originalParent = transform.parent;
            originalPosition = rect.anchoredPosition;
            dropAccepted = false;

            // Pindah ke Canvas paling atas agar tidak dipotong Mask/Layout panel library.
            transform.SetParent(rootCanvas.transform, true);
            transform.SetAsLastSibling();
            canvasGroup.blocksRaycasts = false; // biar SlotUI di bawahnya bisa deteksi OnDrop
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (rootCanvasRect == null)
                return;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rootCanvasRect,
                    eventData.position,
                    rootCanvasCamera,
                    out var localPoint))
            {
                rect.localPosition = localPoint;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            canvasGroup.blocksRaycasts = true;

            if (dropAccepted)
            {
                // LibraryManager sudah membuat pilihan untuk slot berikutnya.
                // Hapus kartu yang barusan dipilih agar tidak ikut terbawa.
                Destroy(gameObject);
            }
            else if (rootCanvas != null && transform.parent == rootCanvas.transform)
            {
                // Drop ditolak atau dilepas di luar slot: kembalikan ke library.
                transform.SetParent(originalParent, true);
                rect.anchoredPosition = originalPosition;
            }
        }

        public void AcceptDrop()
        {
            dropAccepted = true;
        }
    }
}
