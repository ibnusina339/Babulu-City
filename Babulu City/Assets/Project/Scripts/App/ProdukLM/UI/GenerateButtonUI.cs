using UnityEngine;
using UnityEngine.UI;

namespace ProdukLM
{
    // Taruh di tombol "Generate" di panel Tahap 2.
    [RequireComponent(typeof(Button))]
    public class GenerateButtonUI : MonoBehaviour
    {
        void Awake()
        {
            GetComponent<Button>().onClick.AddListener(OnClick);
        }

        void OnEnable()
        {
            ProjectFlowManager.Instance.OnSlotChanged += RefreshInteractable;
            RefreshInteractable();
        }

        void OnDisable()
        {
            if (ProjectFlowManager.Instance != null)
                ProjectFlowManager.Instance.OnSlotChanged -= RefreshInteractable;
        }

        void OnClick()
        {
            ProjectFlowManager.Instance.TryGenerate();
        }

        // Tombol cuma aktif/kepencet kalau 6 slot udah penuh
        void RefreshInteractable()
        {
            bool allFilled = ProjectFlowManager.Instance.State.GetNextEmptySlot() == null;
            GetComponent<Button>().interactable = allFilled;
        }
    }
}
