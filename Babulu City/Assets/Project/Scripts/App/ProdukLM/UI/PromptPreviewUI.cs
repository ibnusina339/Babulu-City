using TMPro;
using UnityEngine;

namespace ProdukLM
{
    // Taruh di GameObject text preview kalimat prompt (bentangan atas layar).
    public class PromptPreviewUI : MonoBehaviour
    {
        public TMP_Text promptText;

        void OnEnable()
        {
            if (ProjectFlowManager.Instance == null)
            {
                Debug.LogError(
                    $"{nameof(PromptPreviewUI)} pada '{name}' tidak menemukan {nameof(ProjectFlowManager)}.",
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
            if (promptText != null && ProjectFlowManager.Instance != null)
                promptText.text = PromptBuilder.Build(ProjectFlowManager.Instance.State);
        }
    }
}
