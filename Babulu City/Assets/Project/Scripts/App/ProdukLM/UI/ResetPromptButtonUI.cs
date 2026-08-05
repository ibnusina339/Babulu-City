using BabuluCity.Core;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProdukLM
{
    [RequireComponent(typeof(Button))]
    public sealed class ResetPromptButtonUI : MonoBehaviour
    {
        Button button;
        ProjectFlowManager flow;
        CanvasGroup visibility;
        Canvas[] localCanvases;

        void Awake()
        {
            button = GetComponent<Button>();
            visibility = GetComponent<CanvasGroup>();
            if (visibility == null)
                visibility = gameObject.AddComponent<CanvasGroup>();
            localCanvases = GetComponentsInChildren<Canvas>(true);
            button.onClick.AddListener(ResetPrompt);
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
                button.onClick.RemoveListener(ResetPrompt);
        }

        void ResetPrompt()
        {
            flow?.ResetPromptSlots();
        }

        void Bind(ProjectFlowManager manager)
        {
            if (manager == null || manager == flow) return;
            Unbind();
            flow = manager;
            flow.OnSlotChanged += Refresh;
            Refresh();
        }

        void Unbind()
        {
            if (flow == null) return;
            flow.OnSlotChanged -= Refresh;
            flow = null;
        }

        void Refresh()
        {
            if (button == null) button = GetComponent<Button>();
            // Reset baru tersedia setelah keenam kartu (termasuk tipe produk)
            // sudah lengkap, sesuai alur tahap 2.
            bool visible = flow != null && flow.State.GetNextEmptySlot() == null;
            button.interactable = visible;

            // Jangan SetActive(false) pada root karena script harus tetap hidup
            // untuk mengetahui kapan slot menjadi lengkap. CanvasGroup menyembunyikan
            // seluruh tombol, sementara Canvas lokal ikut dimatikan bila desain
            // memang membungkus Reset Prompt Button dalam canvas tersendiri.
            if (visibility != null)
            {
                visibility.alpha = visible ? 1f : 0f;
                visibility.interactable = visible;
                visibility.blocksRaycasts = visible;
            }
            if (localCanvases != null)
                foreach (Canvas canvas in localCanvases)
                    if (canvas != null) canvas.enabled = visible;
        }
    }

    static class ResetPromptButtonBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap() => SceneBootstrap.RunOnEverySceneLoad(Install);

        static void Install()
        {
            Transform page2 = null;
            foreach (Transform item in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            {
                if (item.name == "Page2-prompting")
                {
                    page2 = item;
                    break;
                }
            }
            if (page2 == null || page2.GetComponentInChildren<ResetPromptButtonUI>(true) != null)
                return;

            Transform send = null;
            foreach (Transform item in page2.GetComponentsInChildren<Transform>(true))
                if (item.name == "Send Button") { send = item; break; }
            if (send == null) return;

            GameObject reset = Object.Instantiate(send.gameObject, send.parent);
            reset.name = "Reset Prompt Button";
            GenerateButtonUI generate = reset.GetComponent<GenerateButtonUI>();
            if (generate != null) Object.Destroy(generate);

            RectTransform rect = reset.GetComponent<RectTransform>();
            RectTransform sendRect = send.GetComponent<RectTransform>();
            rect.anchoredPosition = sendRect.anchoredPosition + new Vector2(-165f, 0f);
            rect.sizeDelta = new Vector2(180f, Mathf.Max(64f, sendRect.sizeDelta.y * 0.65f));

            foreach (Transform child in reset.GetComponentsInChildren<Transform>(true))
                if (child != reset.transform)
                    child.gameObject.SetActive(false);

            GameObject labelObject = new GameObject("Reset Label", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(reset.transform, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            TMP_Text label = labelObject.GetComponent<TMP_Text>();
            label.text = "RESET";
            label.fontSize = 22f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;

            reset.AddComponent<ResetPromptButtonUI>();
        }
    }
}
