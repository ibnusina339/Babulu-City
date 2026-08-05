using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BabuluCity.Core;
using BabuluCity.SaveSystem;
using UnityEngine;
using UnityEngine.UI;

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
        [Tooltip("Opsional. Popup 'jadwal belum dimulai'. Bila kosong, popup 'Tidak ada Jadwal' dipakai dengan teks yang disesuaikan.")]
        [SerializeField] GameObject notStartedScreen;
        [SerializeField] GameObject studyScreen;
        [SerializeField] Button confirmStudyButton;
        [SerializeField] Button backButton;
        [SerializeField] Image progressFill;
        [SerializeField] GameObject fastForwardRoot;

        [Header("Pengaturan Belajar")]
        [Tooltip("Lama sesi bimbel dalam detik nyata. 7 detik = 2 jam waktu game.")]
        [Min(1f)] [SerializeField] float studyDurationSeconds = 7f;
        [Min(0f)] [SerializeField] float consumedGameHours = 2f;
        [Min(0)] [SerializeField] int scorePerSession = 10;
        [Tooltip("Kecepatan animasi fast forward dalam frame per detik. Hanya visual; durasi belajar tetap Study Duration Seconds.")]
        [Min(0.5f)] [SerializeField] float fastForwardFramesPerSecond = 3.5f;

        [Header("Jadwal Bimbel (tanggal Agustus)")]
        [Tooltip("Tanggal bimbel pada bulan Agustus.")]
        [SerializeField] int[] scheduleDays = { 3, 5, 6, 8 };
        [SerializeField, Range(0, 23)] int scheduleStartHour = 20;
        [SerializeField, Range(0, 59)] int scheduleStartMinute;
        [SerializeField, Range(0, 23)] int scheduleEndHour = 21;
        [SerializeField, Range(0, 59)] int scheduleEndMinute = 30;

        int ScheduleStartMinutes => scheduleStartHour * 60 + scheduleStartMinute;
        int ScheduleEndMinutes => scheduleEndHour * 60 + scheduleEndMinute;

        readonly List<GameObject> participants = new();
        readonly List<GameObject> fastForwardFrames = new();
        readonly Dictionary<Animator, float> originalAnimatorSpeeds = new();
        Coroutine studyRoutine;
        GameClockUI gameClock;
        int completedSessions;
        int studyScore;
        int studiedDateMask;
        float appliedStudyHours;
        bool clockPlaybackCaptured;
        bool clockWasRunning;
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

        void OnDestroy()
        {
            EscapeStack.Unregister(this);
            Button appButton = desktopAppButton != null ? desktopAppButton.GetComponent<Button>() : null;
            appButton?.onClick.RemoveListener(OpenApp);
            confirmStudyButton?.onClick.RemoveListener(StartStudy);
            backButton?.onClick.RemoveListener(CloseTopmostPanel);
        }

        void OnDisable()
        {
            if (studyRoutine != null)
            {
                StopCoroutine(studyRoutine);
                studyRoutine = null;
            }
            RestoreClockPlayback();
            RestoreMeetingAnimatorSpeed();
        }

        public void OpenApp()
        {
            StopStudy(false);
            gameObject.SetActive(true);
            HideScreens();
            SetProgress(0f);
            gameClock ??= Object.FindAnyObjectByType<GameClockUI>(FindObjectsInactive.Include);

            if (gameClock == null)
            {
                Debug.LogWarning(
                    $"{nameof(VentraMeetUI)} pada '{name}' tidak menemukan GameClockUI. " +
                    "Jadwal bimbel tidak dapat diperiksa.",
                    this);
                ShowScheduleScreen(unavailableScreen, null);
                return;
            }

            int nowMinutes = gameClock.CurrentGameMinutes;

            if (!IsScheduledToday() || AlreadyStudiedToday())
                ShowScheduleScreen(unavailableScreen, null);
            else if (nowMinutes > ScheduleEndMinutes)
                ShowScheduleScreen(missedScheduleScreen, null);
            else if (nowMinutes < ScheduleStartMinutes)
                ShowScheduleScreen(
                    notStartedScreen != null ? notStartedScreen : unavailableScreen,
                    $"Bimbel baru dimulai pukul {ScheduleStartHourText()}. Datang lagi nanti ya.");
            else
                ShowScheduleScreen(confirmationScreen, null);

            EscapeStack.Register(this, EscapeLayer.App, CloseTopmostPanel);
        }

        public void StartStudy()
        {
            if (studyRoutine != null || gameClock == null || gameClock.ReachedEnd)
                return;

            int nowMinutes = gameClock.CurrentGameMinutes;
            if (!IsScheduledToday() || AlreadyStudiedToday() ||
                nowMinutes > ScheduleEndMinutes || nowMinutes < ScheduleStartMinutes)
            {
                OpenApp();
                return;
            }

            // Jadwal ditandai terpakai sejak sesi dimulai supaya satu tanggal
            // tidak pernah dapat memulai dua coroutine belajar sekaligus.
            MarkTodayStudied();

            clockWasRunning = gameClock.runAutomatically;
            clockPlaybackCaptured = true;
            gameClock.runAutomatically = false;

            appliedStudyHours = 0f;
            SetActive(confirmationScreen, false);
            SetActive(studyScreen, true);
            SetActive(fastForwardRoot, true);
            ShowAllParticipants();
            SetMeetingAnimatorSpeed(0.5f);
            SetProgress(0f);
            studyRoutine = StartCoroutine(StudyRoutine());
        }

        /// <summary>
        /// Tombol Kembali dan ESC. Setiap panel VentraMeet adalah satu-satunya
        /// isi aplikasi, jadi menutup panel sama dengan kembali ke desktop.
        /// Popup keluar game tidak ikut terpicu karena ESC dirutekan EscapeStack.
        /// </summary>
        public void CloseTopmostPanel()
        {
            // Setelah belajar dikonfirmasi, sesi 7 detik harus selesai utuh.
            // Jika ESC ditekan, daftarkan kembali lapisan ini agar ESC berikutnya
            // tidak membuka popup keluar game di belakang meeting.
            if (IsStudying)
            {
                EscapeStack.Register(this, EscapeLayer.App, CloseTopmostPanel);
                return;
            }
            ShowDesktop();
        }

        public void ShowDesktop()
        {
            // Waktu tetap dikonsumsi penuh jika sesi sudah dikonfirmasi.
            StopStudy(true);
            ResetMeetingPanel();
            HideScreens();
            EscapeStack.Unregister(this);
            gameObject.SetActive(false);
        }

        void ShowScheduleScreen(GameObject screen, string overrideDescription)
        {
            if (screen == null)
                return;

            SetActive(screen, true);
            if (string.IsNullOrEmpty(overrideDescription))
                return;

            TMPro.TMP_Text description = FindIn(screen.transform, "deskripsi bimbel")
                ?.GetComponent<TMPro.TMP_Text>();
            if (description != null)
                description.text = overrideDescription;
        }

        string ScheduleStartHourText() =>
            $"{scheduleStartHour:00}.{scheduleStartMinute:00}";

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
            // Tanggalnya sudah ditandai saat sesi dimulai, di sini hanya nilai.
            completedSessions = Mathf.Min(4, completedSessions + 1);
            studyScore += scorePerSession;
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
            unavailableScreen ??= FindIn(transform, "Batas Bimbel", "Tidak ada Jadwal")?.gameObject;
            missedScheduleScreen ??= FindSceneObject("Jadwal terlewat");
            notStartedScreen ??= FindIn(transform, "Jadwal Belum Dimulai", "Belum Mulai")?.gameObject
                ?? FindSceneObject("Jadwal Belum Dimulai");
            studyScreen ??= FindIn(transform, "Zoom")?.gameObject;
            fastForwardRoot ??= FindIn(studyScreen?.transform, "FastForward Animation")?.gameObject;
            confirmStudyButton ??= EnsureButton(FindIn(confirmationScreen?.transform, "Bimbel Button"));
            backButton ??= EnsureButton(FindIn(confirmationScreen?.transform, "Kembali Button"));
            BindCloseButton(unavailableScreen);
            BindCloseButton(missedScheduleScreen);
            BindCloseButton(notStartedScreen);
            ResolveParticipantVisuals();
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
                backButton.onClick.RemoveListener(CloseTopmostPanel);
                backButton.onClick.AddListener(CloseTopmostPanel);
            }
        }

        void SetProgress(float value)
        {
            value = Mathf.Clamp01(value);
            if (progressFill != null)
                progressFill.fillAmount = value;

            // Peserta tidak lagi muncul bertahap; seluruhnya sudah dinyalakan
            // sekali saat panel meeting aktif lewat ShowAllParticipants().
            if (fastForwardFrames.Count > 0 && IsStudying)
            {
                int frame = Mathf.FloorToInt(Time.unscaledTime * fastForwardFramesPerSecond)
                            % fastForwardFrames.Count;
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
            if (gameClock == null || scheduleDays == null)
                return false;
            System.DateTime date = gameClock.CurrentDate;
            if (date.Month != 8)
                return false;
            foreach (int day in scheduleDays)
                if (day == date.Day)
                    return true;
            return false;
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
            SetActive(notStartedScreen, false);
            SetActive(studyScreen, false);
            SetActive(fastForwardRoot, false);
        }

        void ResolveParticipantVisuals()
        {
            participants.Clear();
            Transform panel = FindIn(studyScreen?.transform, "Panel");
            if (panel != null)
                participants.AddRange(panel.Cast<Transform>()
                    .Where(child => child.name.StartsWith("Peserta", System.StringComparison.OrdinalIgnoreCase))
                    .Select(child => child.gameObject));
            fastForwardFrames.Clear();
            if (fastForwardRoot != null)
                fastForwardFrames.AddRange(fastForwardRoot.transform.Cast<Transform>()
                    .Select(child => child.gameObject));

            originalAnimatorSpeeds.Clear();
            if (studyScreen != null)
            {
                foreach (Animator animator in studyScreen.GetComponentsInChildren<Animator>(true))
                    originalAnimatorSpeeds[animator] = animator.speed;
            }
        }

        void SetMeetingAnimatorSpeed(float multiplier)
        {
            foreach (KeyValuePair<Animator, float> entry in originalAnimatorSpeeds)
                if (entry.Key != null)
                    entry.Key.speed = entry.Value * multiplier;
        }

        void RestoreMeetingAnimatorSpeed()
        {
            foreach (KeyValuePair<Animator, float> entry in originalAnimatorSpeeds)
                if (entry.Key != null)
                    entry.Key.speed = entry.Value;
        }

        /// <summary>
        /// Seluruh peserta langsung tampil begitu panel meeting aktif, tanpa
        /// kemunculan bertahap. Objek pesertanya tetap yang sudah ada di prefab.
        /// </summary>
        void ShowAllParticipants()
        {
            foreach (GameObject participant in participants)
                SetActive(participant, true);
            for (int i = 0; i < fastForwardFrames.Count; i++)
                SetActive(fastForwardFrames[i], i == 0);
        }

        /// <summary>
        /// Mengembalikan panel meeting ke kondisi awal supaya siap dipakai lagi
        /// pada jadwal berikutnya.
        /// </summary>
        void ResetMeetingPanel()
        {
            RestoreClockPlayback();
            RestoreMeetingAnimatorSpeed();
            foreach (GameObject participant in participants)
                SetActive(participant, false);
            for (int i = 0; i < fastForwardFrames.Count; i++)
                SetActive(fastForwardFrames[i], i == 0);
            if (progressFill != null)
                progressFill.fillAmount = 0f;
        }

        void RestoreClockPlayback()
        {
            if (!clockPlaybackCaptured)
                return;
            if (gameClock != null)
                gameClock.runAutomatically = clockWasRunning;
            clockPlaybackCaptured = false;
        }

        void BindCloseButton(GameObject screen)
        {
            Button button = EnsureButton(FindIn(screen?.transform, "Kembali Button", "KembaliBOX"));
            if (button == null)
                return;
            button.onClick.RemoveListener(CloseTopmostPanel);
            button.onClick.AddListener(CloseTopmostPanel);
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
            if (!target.TryGetComponent(out Button button))
                button = target.gameObject.AddComponent<Button>();
            if (button.targetGraphic == null && target.TryGetComponent(out Graphic graphic))
                button.targetGraphic = graphic;
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
        static void Bootstrap() => SceneBootstrap.RunOnEverySceneLoad(Install);

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
