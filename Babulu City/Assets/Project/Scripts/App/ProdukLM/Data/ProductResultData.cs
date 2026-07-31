using UnityEngine;

namespace ProdukLM
{
    // Satu tingkat kualitas visual untuk satu Product Type.
    // Contoh: Canva Template tier "Rendah" pakai mockup polos,
    // tier "Tinggi" pakai mockup yang kelihatan profesional.
    [System.Serializable]
    public struct ProductTier
    {
        [Tooltip("Compatibility minimum (0-100) supaya tier ini yang dipakai")]
        public int minCompatibility;
        public string qualityLabel; // "Buruk", "Cukup", "Bagus", "Sangat Bagus"
        public Sprite mockupImage;  // gambar hasil jadi yang sudah dibuat di Photoshop/Figma
    }

    // Klik kanan -> Create -> ProdukLM -> Product Result
    // Satu asset per Product Type (Canva Template, Ebook, dst).
    [CreateAssetMenu(fileName = "NewProductResult", menuName = "ProdukLM/Product Result")]
    public class ProductResultData : ScriptableObject
    {
        public CardData productTypeCard; // link ke CardData Product Type yang sesuai

        [Tooltip("Urutkan dari minCompatibility terkecil ke terbesar")]
        public ProductTier[] tiers;

        public ProductTier GetTierForScore(int compatibility)
        {
            ProductTier result = tiers[0];
            foreach (var tier in tiers)
            {
                if (compatibility >= tier.minCompatibility)
                    result = tier;
            }
            return result;
        }
    }
}
