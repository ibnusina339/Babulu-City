using UnityEngine;
using UnityEngine.UI;

namespace ProdukLM
{
    [RequireComponent(typeof(Button))]
    public class BackButtonUI : MonoBehaviour
    {
        void Awake()
        {
            GetComponent<Button>().onClick.AddListener(OnClick);
        }

        void OnClick()
        {
            ProjectFlowManager.Instance.BackOneStep();
        }
    }
}
