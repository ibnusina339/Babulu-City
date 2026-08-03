using TMPro;
using UnityEngine;

namespace ProdukLM
{
    // Taruh di GameObject text preview kalimat prompt (bentangan atas layar).
    public class PromptPreviewUI : MonoBehaviour
    {
        public TMP_Text promptText;
        ProjectFlowManager flow;

        void OnEnable()
        {
            ProjectFlowManager.OnInstanceReady += Bind;
            Bind(ProjectFlowManager.Instance);
        }

        void OnDisable()
        {
            ProjectFlowManager.OnInstanceReady -= Bind;
            Unbind();
        }

        void Bind(ProjectFlowManager manager)
        {
            if (manager == null || flow == manager)
                return;

            Unbind();
            flow = manager;
            flow.OnSlotChanged += Refresh;
            Refresh();
        }

        void Unbind()
        {
            if (flow == null)
                return;

            flow.OnSlotChanged -= Refresh;
            flow = null;
        }

        void Refresh()
        {
            if (promptText != null && flow != null)
                promptText.text = PromptBuilder.Build(flow.State);
        }
    }
}
