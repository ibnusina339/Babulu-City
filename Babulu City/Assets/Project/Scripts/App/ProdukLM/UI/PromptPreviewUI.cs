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
            promptText.text = PromptBuilder.Build(ProjectFlowManager.Instance.State);
        }
    }
}
