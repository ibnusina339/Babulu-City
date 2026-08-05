using System;
using System.Collections;
using System.Linq;
using IntegratedApps;
using UnityEngine;
using UnityEngine.Serialization;

namespace ProdukLM
{
    [DisallowMultipleComponent]
    public sealed class ProdukLMGenerationLoadingUI : MonoBehaviour
    {
        [Header("Durasi nyata proses generate")]
        [FormerlySerializedAs("freeDurationSeconds")]
        [Min(0.1f)] public float generationDurationSeconds = 15f;

        [Header("Waktu game")]
        [Min(0f)] public float consumedGameMinutes = 10f;

        Transform[] bars;
        ProjectFlowManager flow;
        GameClockUI gameClock;
        Coroutine generationRoutine;
        float appliedGameMinutes;
        bool clockWasRunning;

        public bool IsGenerating => generationRoutine != null;

        void Awake()
        {
            ResolveBars();
            SetProgress(0f);
        }

        void OnDisable()
        {
            if (generationRoutine != null)
            {
                StopCoroutine(generationRoutine);
                generationRoutine = null;
            }
            RestoreClockPlayback();
        }

        public bool Begin(ProjectFlowManager manager)
        {
            if (manager == null || generationRoutine != null)
                return false;
            if (manager.State.GetNextEmptySlot() != null || !manager.CanCreateProductToday)
                return false;

            flow = manager;

            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            gameClock = UnityEngine.Object.FindAnyObjectByType<GameClockUI>(FindObjectsInactive.Include);
            if (gameClock != null)
            {
                clockWasRunning = gameClock.runAutomatically;
                gameClock.runAutomatically = false;
            }
            appliedGameMinutes = 0f;
            ResolveBars();
            SetProgress(0f);

            // Page2 tetap aktif karena loading memang menjadi bagian dari
            // hierarchy Tahap 2. Overlay loading diletakkan paling depan.
            generationRoutine = StartCoroutine(GenerateRoutine());
            return true;
        }

        IEnumerator GenerateRoutine()
        {
            float elapsed = 0f;
            float duration = Mathf.Max(0.1f, generationDurationSeconds);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                SetProgress(progress);
                ApplyGameTime(progress);
                yield return null;
            }

            SetProgress(1f);
            ApplyGameTime(1f);
            generationRoutine = null;

            bool generated = flow != null && flow.TryGenerate();
            if (!generated && flow != null)
                flow.slotAndLibraryPanel.SetActive(true);

            RestoreClockPlayback();
            gameObject.SetActive(false);
        }

        void ApplyGameTime(float progress)
        {
            float targetMinutes = consumedGameMinutes * Mathf.Clamp01(progress);
            float additionalMinutes = targetMinutes - appliedGameMinutes;
            if (additionalMinutes <= 0f)
                return;

            gameClock?.AdvanceHours(additionalMinutes / 60f);
            appliedGameMinutes = targetMinutes;
        }

        void RestoreClockPlayback()
        {
            if (gameClock != null)
                gameClock.runAutomatically = clockWasRunning;
        }

        void ResolveBars()
        {
            bars = GetComponentsInChildren<Transform>(true)
                .Where(item => item.name.Equals("bar", StringComparison.OrdinalIgnoreCase) ||
                               item.name.StartsWith("bar (", StringComparison.OrdinalIgnoreCase))
                .OrderBy(BarIndex)
                .Take(16)
                .ToArray();
        }

        void SetProgress(float progress)
        {
            if (bars == null || bars.Length == 0)
                ResolveBars();

            int visibleCount = Mathf.Clamp(
                Mathf.CeilToInt(Mathf.Clamp01(progress) * 16f), 0, 16);
            for (int i = 0; i < bars.Length; i++)
                bars[i].gameObject.SetActive(i < visibleCount);
        }

        static int BarIndex(Transform item)
        {
            if (item.name.Equals("bar", StringComparison.OrdinalIgnoreCase))
                return 0;

            int open = item.name.IndexOf('(');
            int close = item.name.IndexOf(')');
            return open >= 0 && close > open &&
                   int.TryParse(item.name.Substring(open + 1, close - open - 1), out int index)
                ? index
                : int.MaxValue;
        }
    }
}
