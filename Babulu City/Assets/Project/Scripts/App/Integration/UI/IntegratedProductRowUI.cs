using System;
using LarisID;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IntegratedApps
{
    public class IntegratedProductRowUI : MonoBehaviour
    {
        public Image productIcon;
        public TMP_Text fileTypeText;
        public TMP_Text productNameText;
        public TMP_Text statusText;
        public TMP_Text qualityText;
        public TMP_Text priceText;
        public TMP_Text ratingText;
        public Button openButton;

        public void Setup(
            LarisProduct product,
            Sprite icon,
            Color fallbackColor,
            Action openAction)
        {
            gameObject.SetActive(true);
            productIcon.sprite = icon;
            productIcon.color = icon != null ? Color.white : fallbackColor;
            fileTypeText.text = icon != null ? string.Empty : FileLabel(product.iconKey);
            productNameText.text = product.productName;
            statusText.text = product.status.ToString();
            qualityText.text = $"Q {product.quality}%";
            priceText.text = $"Rp {product.price:N0}";
            ratingText.text = product.reviewCount > 0 ? $"{product.rating:0.00}/5" : "Belum ada rating";
            openButton.onClick.RemoveAllListeners();
            openButton.onClick.AddListener(() => openAction?.Invoke());
        }

        static string FileLabel(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return "FILE";
            return key.ToUpperInvariant();
        }
    }
}
