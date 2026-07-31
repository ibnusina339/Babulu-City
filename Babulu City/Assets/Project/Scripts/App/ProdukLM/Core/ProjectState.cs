using System;

namespace ProdukLM
{
    // Bukan MonoBehaviour, murni data. Dipegang oleh ProjectFlowManager.
    public class ProjectState
    {
        public CardData[] selectedCards = new CardData[6]; // index sesuai SlotType

        public bool IsSlotFilled(SlotType slot) => selectedCards[(int)slot] != null;

        public void SetCard(SlotType slot, CardData card)
        {
            selectedCards[(int)slot] = card;
        }

        public CardData GetCard(SlotType slot) => selectedCards[(int)slot];

        // Slot kosong pertama, dipakai buat nentuin slot "aktif" saat ini
        public SlotType? GetNextEmptySlot()
        {
            for (int i = 0; i < selectedCards.Length; i++)
                if (selectedCards[i] == null)
                    return (SlotType)i;
            return null; // semua slot sudah terisi
        }
    }
}
