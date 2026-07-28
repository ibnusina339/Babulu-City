using UnityEngine;
using UnityEngine.UI;

namespace ProdukLM
{
    // Taruh di tombol "Buat Lagi" / "Kembali" di panel Tahap 3.
    [RequireComponent(typeof(Button))]
    public class BackToStartButtonUI : MonoBehaviour
    {
        void Awake()
        {
            GetComponent<Button>().onClick.AddListener(() =>
            {
                ProjectFlowManager.Instance.BackToStart();
            });
        }
    }
}
