using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProdukLM
{
    // Taruh di panel Tahap 3 (layar hasil Generate).
    public class StatsResultUI : MonoBehaviour
    {
        [Header("5 Stats (isi salah satu atau dua-duanya per stat)")]
        public Slider qualitySlider;
        public TMP_Text qualityText;
        public Slider relevansiSlider;
        public TMP_Text relevansiText;
        public Slider estetikaSlider;
        public TMP_Text estetikaText;
        public Slider profesionalismeSlider;
        public TMP_Text profesionalismeText;
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
            var stats = ProjectFlowManager.Instance.LastResult;

            SetStat(qualitySlider, qualityText, stats.Quality);
            SetStat(relevansiSlider, relevansiText, stats.Relevansi);
            SetStat(estetikaSlider, estetikaText, stats.Estetika);
            SetStat(profesionalismeSlider, profesionalismeText, stats.Profesionalisme);
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
                instance.text = line;
            }
        }
    }
}
