using System;
using System.Linq;
using TMPro;
using UnityEngine;

namespace ProdukLM
{
    [DisallowMultipleComponent]
    public sealed class CurrentPromptOptionUI : MonoBehaviour
    {
        ProjectFlowManager flow;
        TMP_Text[] optionTexts;

        void Awake()
        {
            optionTexts = GetComponentsInChildren<TMP_Text>(true)
                .Where(IsOptionLabel)
                .ToArray();
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
            optionTexts ??= GetComponentsInChildren<TMP_Text>(true)
                .Where(IsOptionLabel).ToArray();
            SlotType? next = flow?.State.GetNextEmptySlot();

            foreach (TMP_Text text in optionTexts)
                text.gameObject.SetActive(next.HasValue && Matches(text, next.Value));
        }

        static bool IsOptionLabel(TMP_Text text)
        {
            string value = $"{text.name} {text.text}".ToLowerInvariant();
            return value.Contains("tujuan") || value.Contains("target pengguna") ||
                   value.Contains("konten") || value.Contains("gaya penyajian") ||
                   value.Contains("fokus ai");
        }

        static bool Matches(TMP_Text text, SlotType slot)
        {
            string value = $"{text.name} {text.text}".ToLowerInvariant();
            return slot switch
            {
                SlotType.Purpose => value.Contains("tujuan"),
                SlotType.Audience => value.Contains("target pengguna"),
                SlotType.ContentFocus => value.Contains("konten"),
                SlotType.Style => value.Contains("gaya penyajian"),
                SlotType.AIOptimization => value.Contains("fokus ai"),
                _ => false
            };
        }
    }

    static class CurrentPromptOptionBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            foreach (Transform item in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            {
                string normalized = item.name.Replace(" ", string.Empty)
                    .Replace("_", string.Empty).Replace("-", string.Empty);
                if (!normalized.Equals("OptionNow", StringComparison.OrdinalIgnoreCase) &&
                    !normalized.Equals("OpsiSekarang", StringComparison.OrdinalIgnoreCase) &&
                    !normalized.Equals("CurrentOption", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (item.GetComponent<CurrentPromptOptionUI>() == null)
                    item.gameObject.AddComponent<CurrentPromptOptionUI>();
                break;
            }
        }
    }
}
