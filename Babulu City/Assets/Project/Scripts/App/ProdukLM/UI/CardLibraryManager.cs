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
            if (nextSlot == null) return; // semua slot sudah terisi, sembunyikan/nonaktifkan library

            foreach (Transform child in cardContainer)
                Destroy(child.gameObject);

            var relevantCards = allCards.Where(c => c.slotType == nextSlot.Value);
            foreach (var card in relevantCards)
            {
                var instance = Instantiate(cardPrefab, cardContainer);
                instance.data = card;
                // TODO: set icon/text di prefab dari card.icon dan card.displayName
            }
        }
    }
}
