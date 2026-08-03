using System.Collections;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace IntegratedApps
{
    /// <summary>
    /// Mekanik VentraMeet. Referensi dapat diganti dari Inspector ketika
    /// desain final sudah tersedia; nama hierarchy hanya dipakai sebagai fallback.
    /// </summary>
    public class VentraMeetUI : MonoBehaviour
    {
        [Header("Referensi UI")]
        [SerializeField] GameObject desktopAppButton;
        [SerializeField] GameObject confirmationScreen;
        [SerializeField] GameObject studyScreen;
        [SerializeField] Button confirmStudyButton;
        [SerializeField] Button backButton;
        [SerializeField] Image progressFill;

        [Header("Pengaturan Belajar")]
        [Min(1f)] [SerializeField] float studyDurationSeconds = 15f;
        [Min(0f)] [SerializeField] float consumedGameHours = 2f;
        [Min(0)] [SerializeField] int scorePerSession = 10;

        Coroutine studyRoutine;
        GameClockUI gameClock;
        int completedSessions;
        int studyScore;
        float appliedStudyHours;
        bool initialized;

        public int CompletedSessions => completedSessions;
        public int StudyScore => studyScore;
        public bool IsStudying => studyRoutine != null;

        void Awake()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (initialized)
                return;

            initialized = true;
            ResolveReferences();
            BindButtons();
            ShowDesktop();
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
            UnbindButtons();
        }

        public void OpenApp()
        {
            StopStudy(false);
            gameObject.SetActive(true);
            confirmationScreen.SetActive(true);
            studyScreen.SetActive(false);
            SetProgress(0f);
        }

        public void StartStudy()
        {
            if (studyRoutine != null || (gameClock != null && gameClock.ReachedEnd))
                return;

            appliedStudyHours = 0f;
            confirmationScreen.SetActive(false);
            studyScreen.SetActive(true);
            SetProgress(0f);
            studyRoutine = StartCoroutine(StudyRoutine());
        }

        public void ShowDesktop()
        {
            // Setelah belajar dikonfirmasi, biaya waktunya tetap dua jam.
            // Jika pemain keluar di tengah animasi, sisa waktu langsung diterapkan.
            StopStudy(true);
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
            completedSessions++;
            studyScore += scorePerSession;
            studyRoutine = null;
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
            gameClock = uiRoot.GetComponent<GameClockUI>();
            desktopAppButton ??= FindIn(uiRoot, "VentraMeet APP")?.gameObject;
            confirmationScreen ??= FindIn(transform, "KonfirmasiScreen")?.gameObject;
            studyScreen ??= FindIn(transform, "Zoom")?.gameObject;

            Transform confirmBox = FindIn(transform, "BelajarBOX (1)");
            Transform returnBox = FindIn(transform, "KembaliBOX");
            confirmStudyButton ??= EnsureButton(confirmBox);
            backButton ??= EnsureButton(returnBox);

            if (progressFill == null && studyScreen != null)
                progressFill = CreateFallbackProgress(studyScreen.transform);
        }

        void BindButtons()
        {
            Button appButton = EnsureButton(desktopAppButton != null ? desktopAppButton.transform : null);
            appButton?.onClick.AddListener(OpenApp);
            confirmStudyButton?.onClick.AddListener(StartStudy);
            backButton?.onClick.AddListener(ShowDesktop);
        }

        void UnbindButtons()
        {
            Button appButton = desktopAppButton != null ? desktopAppButton.GetComponent<Button>() : null;
            appButton?.onClick.RemoveListener(OpenApp);
            confirmStudyButton?.onClick.RemoveListener(StartStudy);
            backButton?.onClick.RemoveListener(ShowDesktop);
        }

        void SetProgress(float value)
        {
            if (progressFill != null)
                progressFill.fillAmount = Mathf.Clamp01(value);
        }

        static Transform FindIn(Transform root, string objectName)
        {
            if (root == null)
                return null;

            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == objectName)
                    return child;
            }

            return null;
        }

        static Button EnsureButton(Transform target)
        {
            if (target == null)
                return null;

            Button button = target.GetComponent<Button>();
            if (button == null)
                button = target.gameObject.AddComponent<Button>();

            if (button.targetGraphic == null)
                button.targetGraphic = target.GetComponent<Graphic>();

            return button;
        }

        static Image CreateFallbackProgress(Transform parent)
        {
            GameObject track = new GameObject("StudyProgress", typeof(RectTransform), typeof(Image));
            track.transform.SetParent(parent, false);
            RectTransform trackRect = (RectTransform)track.transform;
            trackRect.anchorMin = new Vector2(0.2f, 0.06f);
            trackRect.anchorMax = new Vector2(0.8f, 0.095f);
            trackRect.offsetMin = Vector2.zero;
            trackRect.offsetMax = Vector2.zero;
            track.GetComponent<Image>().color = new Color(0.04f, 0.08f, 0.14f, 0.9f);

            GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(track.transform, false);
            RectTransform fillRect = (RectTransform)fill.transform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(3f, 3f);
            fillRect.offsetMax = new Vector2(-3f, -3f);

            Image image = fill.GetComponent<Image>();
            image.color = new Color(0.15f, 0.75f, 1f, 1f);
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillOrigin = 0;
            image.fillAmount = 0f;
            return image;
        }
    }

    static class VentraMeetBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            foreach (Transform candidate in Object.FindObjectsByType<Transform>(
                         FindObjectsInactive.Include))
            {
                if (candidate.name != "VentraMeet" || candidate.GetComponent<VentraMeetUI>() != null)
                    continue;

                VentraMeetUI controller = candidate.gameObject.AddComponent<VentraMeetUI>();
                // Awake tidak selalu langsung dipanggil jika prefab sengaja inactive.
                // Inisialisasi eksplisit memastikan ikon desktop tetap mendapat listener.
                controller.Initialize();
                break;
            }
        }
    }
}
