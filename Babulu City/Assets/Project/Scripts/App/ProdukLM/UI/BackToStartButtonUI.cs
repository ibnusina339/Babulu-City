using UnityEngine;
using UnityEngine.UI;

namespace ProdukLM
{
    // Komponen lama untuk tombol Result; sekarang kembali satu tahap agar pilihan bisa direvisi.
    [RequireComponent(typeof(Button))]
    public class BackToStartButtonUI : MonoBehaviour
    {
        void Awake()
        {
            GetComponent<Button>().onClick.AddListener(() =>
            {
                ProjectFlowManager.Instance.BackOneStep();
            });
        }
    }
}
