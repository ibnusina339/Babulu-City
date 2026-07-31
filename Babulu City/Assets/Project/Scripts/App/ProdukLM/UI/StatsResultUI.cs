using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProdukLM
{
    // Taruh di panel Tahap 3 (layar hasil Generate).
    public class StatsResultUI : MonoBehaviour
    {
        [Header("Ringkasan produk")]
        public TMP_Text productNameText;
        public TMP_Text finalPromptText;
        public TMP_Text qualityLabelText;

        [Header("3 Stats - bebas ditata atau diganti di prefab")]
        public Slider qualitySlider;
        public TMP_Text qualityText;
        public Slider relevansiSlider;
        public TMP_Text relevansiText;
        public Slider nilaiJualSlider;
        public TMP_Text nilaiJualText;

        [Header("AI Analysis feedback")]
        public Transform feedbackContainer; // parent dengan Vertical Layout Group
        public TMP_Text feedbackLinePrefab; // prefab 1 baris teks

        void OnEnable()
        {
            ProjectFlowManager.Instance.OnGenerated += Refresh;
            Refresh(); // langsung refresh kalau panel ini aktif setelah Generate
        }

        void OnDisable()
        {
            if (ProjectFlowManager.Instance != null)
                ProjectFlowManager.Instance.OnGenerated -= Refresh;
        }

        void Refresh()
        {
            var flow = ProjectFlowManager.Instance;
            var stats = flow.LastResult;

            if (productNameText != null)
            {
                var productCard = flow.State.GetCard(SlotType.ProductType);
                productNameText.text = productCard != null ? productCard.displayName : "Produk Digital";
            }

            if (finalPromptText != null)
                finalPromptText.text = PromptBuilder.Build(flow.State);

            if (qualityLabelText != null)
                qualityLabelText.text = GetQualityLabel(stats.Quality);

            SetStat(qualitySlider, qualityText, stats.Quality);
            SetStat(relevansiSlider, relevansiText, stats.Relevansi);
            SetStat(nilaiJualSlider, nilaiJualText, stats.NilaiJual);

            RefreshFeedback(ProjectFlowManager.Instance.LastFeedback);
        }

        void SetStat(Slider slider, TMP_Text text, int value)
        {
            if (slider != null) slider.value = value; // pastikan Min=0, Max=100 di Inspector
            if (text != null) text.text = $"{value}%";
        }

        void RefreshFeedback(List<string> lines)
        {
            if (feedbackContainer == null || feedbackLinePrefab == null) return;

            foreach (Transform child in feedbackContainer)
                Destroy(child.gameObject);

            foreach (var line in lines)
            {
                var instance = Instantiate(feedbackLinePrefab, feedbackContainer);
                instance.gameObject.SetActive(true);
                instance.text = line;
            }
        }

        static string GetQualityLabel(int quality)
        {
            if (quality >= 90) return "Sangat Bagus";
            if (quality >= 75) return "Bagus";
            if (quality >= 60) return "Cukup";
            return "Perlu Ditingkatkan";
        }
    }
}
