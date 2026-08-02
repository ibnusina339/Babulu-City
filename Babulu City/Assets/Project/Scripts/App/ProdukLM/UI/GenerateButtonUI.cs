using UnityEngine;
using UnityEngine.UI;

namespace ProdukLM
{
    // Taruh di tombol "Generate" di panel Tahap 2.
    [RequireComponent(typeof(Button))]
    public class GenerateButtonUI : MonoBehaviour
    {
        ProjectFlowManager flow;
        Button button;

        void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(OnClick);
        }

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

        void OnDestroy()
        {
            if (button != null)
                button.onClick.RemoveListener(OnClick);
        }

        void Bind(ProjectFlowManager manager)
        {
            if (manager == null || flow == manager)
                return;

            Unbind();
            flow = manager;
            flow.OnSlotChanged += RefreshInteractable;
            flow.OnDailyLimitChanged += RefreshInteractable;
            RefreshInteractable();
        }

        void Unbind()
        {
            if (flow == null)
                return;

            flow.OnSlotChanged -= RefreshInteractable;
            flow.OnDailyLimitChanged -= RefreshInteractable;
            flow = null;
        }

        void OnClick()
        {
            flow?.TryGenerate();
        }

        // Tombol cuma aktif/kepencet kalau 6 slot udah penuh
        void RefreshInteractable()
        {
            if (button == null)
                button = GetComponent<Button>();

            bool allFilled = flow != null && flow.State.GetNextEmptySlot() == null;
            button.interactable = allFilled && flow != null && flow.CanCreateProductToday;
        }
    }
}
