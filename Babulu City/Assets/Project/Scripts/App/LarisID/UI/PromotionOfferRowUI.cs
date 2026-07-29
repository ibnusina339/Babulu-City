using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LarisID
{
    public class PromotionOfferRowUI : MonoBehaviour
    {
        public TMP_Text platformText;
        public TMP_Text promoterNameText;
        public TMP_Text offerDetailText;
        public Button selectButton;

        public void Setup(PromoterOffer offer, bool selected, Action onSelect)
        {
            gameObject.SetActive(true);
            platformText.text = offer.platform.ToString().ToUpperInvariant();
            promoterNameText.text = offer.promoterName;
            offerDetailText.text =
                $"Rp {offer.cost:N0}  •  {offer.durationDays} hari  •  " +
                $"estimasi tayangan +{offer.viewBoostPercent}%";
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(() => onSelect?.Invoke());

            Image background = selectButton.targetGraphic as Image;
            if (background != null)
                background.color = selected
                    ? new Color(.42f, .39f, 1f, 1f)
                    : new Color(.14f, .17f, .27f, 1f);
        }
    }
}
