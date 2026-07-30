using System.Linq;
using UnityEngine;

namespace ProdukLM
{
    // Taruh di GameObject panel kanan (card library). Butuh referensi ke
    // container (misal punya GridLayoutGroup) dan prefab kartu (pakai CardUI.cs).
    public class CardLibraryManager : MonoBehaviour
    {
        public Transform cardContainer;
        public CardUI cardPrefab;
        public CardData[] allCards; // isi semua CardData di sini lewat Inspector, atau load dari Resources

        void OnEnable()
        {
            if (ProjectFlowManager.Instance == null)
            {
                Debug.LogError(
                    $"{nameof(CardLibraryManager)} pada '{name}' tidak menemukan {nameof(ProjectFlowManager)}.",
                    this);
                return;
            }

            ProjectFlowManager.Instance.OnSlotChanged += Refresh;
            Refresh();
        }

        void OnDisable()
        {
            if (ProjectFlowManager.Instance != null)
                ProjectFlowManager.Instance.OnSlotChanged -= Refresh;
        }

        void Refresh()
        {
            var state = ProjectFlowManager.Instance.State;
            var nextSlot = state.GetNextEmptySlot();
            ClearCards();

            if (nextSlot == null)
                return; // semua slot sudah terisi

            if (cardContainer == null || cardPrefab == null)
            {
                Debug.LogError(
                    $"{nameof(CardLibraryManager)} pada '{name}' belum memiliki container atau prefab kartu.",
                    this);
                return;
            }

            var relevantCards = (allCards ?? System.Array.Empty<CardData>())
                .Where(c => c != null && c.slotType == nextSlot.Value);
            foreach (var card in relevantCards)
            {
                var instance = Instantiate(cardPrefab, cardContainer);
                instance.gameObject.SetActive(true);
                instance.SetData(card);
            }
        }

        void ClearCards()
        {
            if (cardContainer == null)
                return;

            foreach (Transform child in cardContainer)
            {
                // Dekorasi milik layout desainer tetap aman bila nanti
                // ditambahkan ke container yang sama.
                if (child.GetComponent<CardUI>() == null)
                    continue;

                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
        }
    }
}
