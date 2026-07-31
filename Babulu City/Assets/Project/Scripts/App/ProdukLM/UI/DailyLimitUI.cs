using TMPro;
using UnityEngine;

namespace ProdukLM
{
    [RequireComponent(typeof(TMP_Text))]
    public class DailyLimitUI : MonoBehaviour
    {
        TMP_Text label;

        void Awake()
        {
            label = GetComponent<TMP_Text>();
        }

        void OnEnable()
        {
            if (ProjectFlowManager.Instance == null)
                return;

            ProjectFlowManager.Instance.OnDailyLimitChanged += Refresh;
            ProjectFlowManager.Instance.OnGenerationBlocked += ShowBlockedMessage;
            Refresh();
        }

        void OnDisable()
        {
            if (ProjectFlowManager.Instance == null)
                return;

            ProjectFlowManager.Instance.OnDailyLimitChanged -= Refresh;
            ProjectFlowManager.Instance.OnGenerationBlocked -= ShowBlockedMessage;
        }

        void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus && isActiveAndEnabled)
                Refresh();
        }

        void Refresh()
        {
            var flow = ProjectFlowManager.Instance;
            if (flow == null || label == null)
                return;

            int remaining = flow.RemainingProductsToday;
            label.text = remaining > 0
                ? $"AI {flow.CurrentTier} • Sisa produksi: {remaining}/{flow.DailyProductLimit}"
                : $"AI {flow.CurrentTier} • Limit harian habis: 0/{flow.DailyProductLimit}";
        }

        void ShowBlockedMessage(string message)
        {
            if (label != null)
                label.text = message;
        }
    }
}
