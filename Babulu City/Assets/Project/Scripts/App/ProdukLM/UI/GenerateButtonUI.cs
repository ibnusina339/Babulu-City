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
            if (button.targetGraphic == null && TryGetComponent(out Graphic graphic))
                button.targetGraphic = graphic;
            if (button.targetGraphic != null)
                button.targetGraphic.raycastTarget = true;
            button.onClick.AddListener(OnClick);
        }

        void OnEnable()
        {
            // Option Now dan Reset ditambahkan belakangan ke hierarchy. Pastikan
            // visual transparan milik keduanya tidak menutup raycast tombol Send.
            transform.SetAsLastSibling();
            ProjectFlowManager.OnInstanceReady += Bind;
            Bind(GetComponentInParent<ProjectFlowManager>(true) ?? ProjectFlowManager.Instance);
        }

        void LateUpdate()
        {
            // Menjaga state tetap sinkron meski perubahan hierarchy/desain
            // membuat event slot terlewat ketika panel sempat nonaktif.
            RefreshInteractable();
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
            // Ambil manager dari window yang sama setiap klik. Ini mencegah
            // tombol terhubung ke manager scene testing lain yang kebetulan
            // sedang terbuka secara additive di Editor.
            ProjectFlowManager localFlow = GetComponentInParent<ProjectFlowManager>(true);
            if (localFlow != null && localFlow != flow)
                Bind(localFlow);

            if (flow == null)
            {
                Debug.LogError("Send ProdukLM tidak menemukan ProjectFlowManager pada window yang sama.", this);
                return;
            }

            if (flow.State.GetNextEmptySlot() != null)
            {
                Debug.LogWarning($"Generate ditolak: slot {flow.State.GetNextEmptySlot()} belum terisi.", this);
                return;
            }

            if (!flow.CanCreateProductToday)
            {
                Debug.LogWarning(
                    $"Generate ditolak: limit harian habis ({flow.ProductsCreatedToday}/{flow.DailyProductLimit}).",
                    this);
                return;
            }

            ProdukLMGenerationLoadingUI loading = flow.GetComponentInChildren<ProdukLMGenerationLoadingUI>(true);
            if (loading != null)
            {
                if (!loading.Begin(flow))
                    Debug.LogWarning("Loading Generate ProdukLM gagal dimulai.", this);
                return;
            }

            // Fallback untuk scene lama yang belum mempunyai desain loading.
            if (!flow.TryGenerate())
                Debug.LogWarning("Generate ProdukLM ditolak oleh ProjectFlowManager.", this);
        }

        // Tombol cuma aktif/kepencet kalau 6 slot udah penuh
        void RefreshInteractable()
        {
            if (button == null)
                button = GetComponent<Button>();

            bool allFilled = flow != null && flow.State.GetNextEmptySlot() == null;
            // Send tetap bisa menerima klik ketika prompt lengkap. TryGenerate()
            // tetap menjadi sumber validasi limit harian dan tidak akan
            // mengurangi kuota bila limit sudah habis.
            button.interactable = allFilled;
        }
    }
}
