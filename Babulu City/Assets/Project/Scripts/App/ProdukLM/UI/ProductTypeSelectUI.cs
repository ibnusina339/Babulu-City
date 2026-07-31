using UnityEngine;
using UnityEngine.UI;

namespace ProdukLM
{
    // Taruh di tiap tombol grid 3x2 (6 GameObject, masing-masing punya Button + script ini).
    // Isi field "card" di Inspector dengan CardData Product Type yang sesuai.
    [RequireComponent(typeof(Button))]
    public class ProductTypeSelectUI : MonoBehaviour
    {
        public CardData card;

        void Awake()
        {
            GetComponent<Button>().onClick.AddListener(OnClick);
        }

        void OnClick()
        {
            ProjectFlowManager.Instance.StartProject(card);
        }
    }
}
