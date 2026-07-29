using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LarisID
{
    public class LarisProductRowUI : MonoBehaviour
    {
        public TMP_Text productNameText;
        public TMP_Text categoryText;
        public TMP_Text priceText;
        public TMP_Text statusText;
        public TMP_Text qualityText;
        public TMP_Text impressionsText;
        public TMP_Text clicksText;
        public TMP_Text salesText;
        public TMP_Text ratingText;
        public TMP_Text revenueText;
        public Button detailButton;

        public void Setup(LarisProduct product, Action onOpen)
        {
            gameObject.SetActive(true);
            productNameText.text = product.productName;
            categoryText.text = product.category.ToString();
            priceText.text = $"Rp {product.price:N0}";
            statusText.text = product.status.ToString();
            qualityText.text = product.quality.ToString();
            impressionsText.text = product.impressions.ToString("N0");
            clicksText.text = product.clicks.ToString("N0");
            salesText.text = product.sales.ToString("N0");
            ratingText.text = product.reviewCount > 0 ? product.rating.ToString("0.0") : "-";
            revenueText.text = $"Rp {product.revenue:N0}";

            detailButton.onClick.RemoveAllListeners();
            detailButton.onClick.AddListener(() => onOpen?.Invoke());
        }
    }
}
