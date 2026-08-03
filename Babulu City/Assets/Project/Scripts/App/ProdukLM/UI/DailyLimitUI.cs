using TMPro;
using UnityEngine;

namespace ProdukLM
{
    [RequireComponent(typeof(TMP_Text))]
    public class DailyLimitUI : MonoBehaviour
    {
        TMP_Text label;
        ProjectFlowManager flow;

        void Awake()
        {
            label = GetComponent<TMP_Text>();
        }

        void OnEnable()
        {
            ProjectFlowManager.OnInstanceReady += Bind;
            Bind(GetComponentInParent<ProjectFlowManager>(true) ?? ProjectFlowManager.Instance);
        }

        void OnDisable()
        {
            ProjectFlowManager.OnInstanceReady -= Bind;
            Unbind();
        }

        void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus && isActiveAndEnabled)
                Refresh();
        }

        void Bind(ProjectFlowManager manager)
        {
            if (manager == null || manager == flow)
                return;

            Unbind();
            flow = manager;
            flow.OnDailyLimitChanged += Refresh;
            flow.OnAITierChanged += Refresh;
            flow.OnGenerationBlocked += ShowBlockedMessage;
            Refresh();
        }

        void Unbind()
        {
            if (flow != null)
            {
                flow.OnDailyLimitChanged -= Refresh;
                flow.OnAITierChanged -= Refresh;
                flow.OnGenerationBlocked -= ShowBlockedMessage;
            }

            flow = null;
        }

        void Refresh()
        {
            if (flow == null || label == null)
                return;

            label.text = $"Limit Produk: {flow.RemainingProductsToday}/{flow.DailyProductLimit}";
        }

        void ShowBlockedMessage(string _)
        {
            Refresh();
        }
    }

    static class DailyLimitUIBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            foreach (TMP_Text text in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include))
            {
                if (!text.text.TrimStart().StartsWith("Limit Produk", System.StringComparison.OrdinalIgnoreCase))
                    continue;
                if (text.GetComponentInParent<ProjectFlowManager>(true) == null)
                    continue;
                if (text.GetComponent<DailyLimitUI>() == null)
                    text.gameObject.AddComponent<DailyLimitUI>();
            }
        }
    }
}
