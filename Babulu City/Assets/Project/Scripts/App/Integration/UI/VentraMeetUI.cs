using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BabuluCity.SaveSystem;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace IntegratedApps
{
    /// <summary>Mekanik jadwal dan sesi belajar VentraMeet.</summary>
    public class VentraMeetUI : MonoBehaviour
    {
        [Header("Referensi UI")]
        [SerializeField] GameObject desktopAppButton;
        [SerializeField] GameObject confirmationScreen;
        [SerializeField] GameObject unavailableScreen;
        [SerializeField] GameObject missedScheduleScreen;
        [SerializeField] GameObject studyScreen;
        [SerializeField] Button confirmStudyButton;
        [SerializeField] Button backButton;
        [SerializeField] Image progressFill;
        [SerializeField] GameObject fastForwardRoot;

        [Header("Pengaturan Belajar")]
        [Min(1f)] [SerializeField] float studyDurationSeconds = 15f;
        [Min(0f)] [SerializeField] float consumedGameHours = 2f;
        [Min(0)] [SerializeField] int scorePerSession = 10;

        readonly List<GameObject> sequentialParticipants = new();
        readonly List<GameObject> fastForwardFrames = new();
        Coroutine studyRoutine;
        GameClockUI gameClock;
        int completedSessions;
        int studyScore;
        int studiedDateMask;
        float appliedStudyHours;
        bool initialized;

        public int CompletedSessions => completedSessions;
        public int StudyScore => studyScore;
        public int StudiedDateMask => studiedDateMask;
        public bool IsStudying => studyRoutine != null;
        public bool HasOpenModal => gameObject.activeInHierarchy;

        void Awake() => Initialize();

        public void Initialize()
        {
            if (initialized)
                return;
            initialized = true;
            ResolveReferences();
            BindButtons();
            HideScreens();
            gameObject.SetActive(false);
        }

        void Update()
        {
            if (gameObject.activeInHierarchy && EscapePressedThisFrame())
                ShowDesktop();
        }

        static bool EscapePressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Escape);
#else
            return false;
#endif
        }

        void OnDestroy()
        {
            Button appButton = desktopAppButton != null ? desktopAppButton.GetComponent<Button>() : null;
            appButton?.onClick.RemoveListener(OpenApp);
            confirmStudyButton?.onClick.RemoveListener(StartStudy);
            backButton?.onClick.RemoveListener(ShowDesktop);
        }

        public void OpenApp()
        {
            StopStudy(false);
            gameObject.SetActive(true);
            HideScreens();
            SetProgress(0f);
            gameClock ??= Object.FindAnyObjectByType<GameClockUI>(FindObjectsInactive.Include);

            if (!IsScheduledToday() || AlreadyStudiedToday())
                SetActive(unavailableScreen, true);
            else if (gameClock != null && gameClock.CurrentGameMinutes > 21 * 60 + 30)
                SetActive(missedScheduleScreen, true);
            else
                SetActive(confirmationScreen, true);
        }

        public void StartStudy()
        {
            if (studyRoutine != null || gameClock == null || gameClock.ReachedEnd)
                return;
            if (!IsScheduledToday() || AlreadyStudiedToday() || gameClock.CurrentGameMinutes > 21 * 60 + 30)
            {
                OpenApp();
                return;
            }

            appliedStudyHours = 0f;
            SetActive(confirmationScreen, false);
            SetActive(studyScreen, true);
            SetActive(fastForwardRoot, true);
            ResetSequentialVisuals();
            SetProgress(0f);
            studyRoutine = StartCoroutine(StudyRoutine());
        }

        public void ShowDesktop()
        {
            // Waktu tetap dikonsumsi penuh jika sesi sudah dikonfirmasi.
            StopStudy(true);
            HideScreens();
            gameObject.SetActive(false);
        }

        IEnumerator StudyRoutine()
        {
            float elapsed = 0f;
            float duration = Mathf.Max(1f, studyDurationSeconds);
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                SetProgress(progress);
                ApplyFastForward(progress);
                yield return null;
            }

            SetProgress(1f);
            ApplyFastForward(1f);
            completedSessions = Mathf.Min(4, completedSessions + 1);
            studyScore += scorePerSession;
            MarkTodayStudied();
            studyRoutine = null;
            GameSaveManager.SaveImportant();
            ShowDesktop();
        }

        void ApplyFastForward(float progress)
        {
            float targetHours = consumedGameHours * Mathf.Clamp01(progress);
            float additionalHours = targetHours - appliedStudyHours;
            if (additionalHours <= 0f)
                return;
            gameClock?.AdvanceHours(additionalHours);
            appliedStudyHours = targetHours;
        }

        void StopStudy(bool consumeRemainingTime)
        {
            if (studyRoutine == null)
                return;
            StopCoroutine(studyRoutine);
            studyRoutine = null;
            if (consumeRemainingTime)
                ApplyFastForward(1f);
        }

        void ResolveReferences()
        {
            Transform uiRoot = transform.root;
            gameClock = Object.FindAnyObjectByType<GameClockUI>(FindObjectsInactive.Include);
            desktopAppButton ??= FindIn(uiRoot, "VentraMeet APP", "VentraMeet App")?.gameObject;
            confirmationScreen ??= FindIn(transform, "KonfirmasiScreen")?.gameObject;
            unavailableScreen ??= FindIn(transform, "Batas Bimbel")?.gameObject;
            missedScheduleScreen ??= FindSceneObject("Jadwal terlewat");
            studyScreen ??= FindIn(transform, "Zoom")?.gameObject;
            fastForwardRoot ??= FindIn(studyScreen?.transform, "FastForward Animation")?.gameObject;
            confirmStudyButton ??= EnsureButton(FindIn(confirmationScreen?.transform, "Bimbel Button"));
            backButton ??= EnsureButton(FindIn(confirmationScreen?.transform, "Kembali Button"));
            BindCloseButton(unavailableScreen);
            BindCloseButton(missedScheduleScreen);
            ResolveSequentialVisuals();
        }

        void BindButtons()
        {
            Button appButton = EnsureButton(desktopAppButton?.transform);
            if (appButton != null)
            {
                appButton.onClick.RemoveListener(OpenApp);
                appButton.onClick.AddListener(OpenApp);
            }
            if (confirmStudyButton != null)
            {
                confirmStudyButton.onClick.RemoveListener(StartStudy);
                confirmStudyButton.onClick.AddListener(StartStudy);
            }
            if (backButton != null)
            {
                backButton.onClick.RemoveListener(ShowDesktop);
                backButton.onClick.AddListener(ShowDesktop);
            }
        }

        void SetProgress(float value)
        {
            value = Mathf.Clamp01(value);
            if (progressFill != null)
                progressFill.fillAmount = value;
            int visibleCount = Mathf.CeilToInt(value * sequentialParticipants.Count);
            for (int i = 0; i < sequentialParticipants.Count; i++)
                SetActive(sequentialParticipants[i], i < visibleCount);
            if (fastForwardFrames.Count > 0 && IsStudying)
            {
                int frame = Mathf.FloorToInt(Time.unscaledTime * 7f) % fastForwardFrames.Count;
                for (int i = 0; i < fastForwardFrames.Count; i++)
                    SetActive(fastForwardFrames[i], i == frame);
            }
        }

        public void RestoreProgress(int sessions, int score, int dateMask)
        {
            completedSessions = Mathf.Clamp(sessions, 0, 4);
            studyScore = Mathf.Max(0, score);
            studiedDateMask = Mathf.Max(0, dateMask);
        }

        bool IsScheduledToday()
        {
            if (gameClock == null)
                return false;
            System.DateTime date = gameClock.CurrentDate;
            return date.Year == 2026 && date.Month == 8 &&
                   (date.Day == 3 || date.Day == 5 || date.Day == 6 || date.Day == 8);
        }

        bool AlreadyStudiedToday()
        {
            if (gameClock == null)
                return false;
            int bit = Mathf.Clamp(gameClock.CurrentDate.Day - 2, 0, 30);
            return (studiedDateMask & (1 << bit)) != 0;
        }

        void MarkTodayStudied()
        {
            int bit = Mathf.Clamp(gameClock.CurrentDate.Day - 2, 0, 30);
            studiedDateMask |= 1 << bit;
        }

        void HideScreens()
        {
            SetActive(confirmationScreen, false);
            SetActive(unavailableScreen, false);
            SetActive(missedScheduleScreen, false);
            SetActive(studyScreen, false);
            SetActive(fastForwardRoot, false);
        }

        void ResolveSequentialVisuals()
        {
            sequentialParticipants.Clear();
            Transform panel = FindIn(studyScreen?.transform, "Panel");
            if (panel != null)
                sequentialParticipants.AddRange(panel.Cast<Transform>()
                    .Where(child => child.name.StartsWith("Peserta", System.StringComparison.OrdinalIgnoreCase))
                    .Select(child => child.gameObject));
            fastForwardFrames.Clear();
            if (fastForwardRoot != null)
                fastForwardFrames.AddRange(fastForwardRoot.transform.Cast<Transform>()
                    .Select(child => child.gameObject));
        }

        void ResetSequentialVisuals()
        {
            foreach (GameObject participant in sequentialParticipants)
                SetActive(participant, false);
            for (int i = 0; i < fastForwardFrames.Count; i++)
                SetActive(fastForwardFrames[i], i == 0);
        }

        void BindCloseButton(GameObject screen)
        {
            Button button = EnsureButton(FindIn(screen?.transform, "Kembali Button", "KembaliBOX"));
            if (button == null)
                return;
            button.onClick.RemoveListener(ShowDesktop);
            button.onClick.AddListener(ShowDesktop);
        }

        static GameObject FindSceneObject(string objectName)
        {
            foreach (Transform candidate in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
                if (candidate.name.Equals(objectName, System.StringComparison.OrdinalIgnoreCase))
                    return candidate.gameObject;
            return null;
        }

        static Transform FindIn(Transform root, params string[] names)
        {
            if (root == null)
                return null;
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                foreach (string name in names)
                    if (child.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                        return child;
            return null;
        }

        static Button EnsureButton(Transform target)
        {
            if (target == null)
                return null;
            Button button = target.GetComponent<Button>() ?? target.gameObject.AddComponent<Button>();
            button.targetGraphic ??= target.GetComponent<Graphic>();
            return button;
        }

        static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
                target.SetActive(active);
        }
    }

    static class VentraMeetBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            foreach (Transform candidate in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            {
                if (!candidate.name.Equals("VentraMeet", System.StringComparison.OrdinalIgnoreCase) ||
                    candidate.GetComponent<VentraMeetUI>() != null)
                    continue;
                VentraMeetUI controller = candidate.gameObject.AddComponent<VentraMeetUI>();
                controller.Initialize();
                break;
            }
        }
    }
}
