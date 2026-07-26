using UnityEngine;

namespace ProdukLM
{
    // Klik kanan di Project window -> Create -> ProdukLM -> Card
    // Satu asset = satu kartu. Affinity/Conflict tinggal drag-drop di Inspector.
    [CreateAssetMenu(fileName = "NewCard", menuName = "ProdukLM/Card")]
    public class CardData : ScriptableObject
    {
        [Header("Identitas")]
        public string cardId;
        public string displayName;
        public SlotType slotType;
        public Sprite icon;

        [Header("Dipakai di kalimat prompt, contoh: 'canva template'")]
        public string promptFragment;

        [Header("Hubungan dengan kartu lain")]
        public CardData[] affinityCards;
        public CardData[] conflictCards;
    }
}
