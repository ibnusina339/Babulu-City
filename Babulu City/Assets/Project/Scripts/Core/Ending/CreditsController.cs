using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace BabuluCity.Ending
{
    /// <summary>
    /// Menggulirkan teks kredit ke atas seperti kredit film. Referensi diisi
    /// langsung oleh CreditsSceneBuilder saat scene dibuat, jadi komponen ini
    /// tidak mencari objek lewat nama.
    /// </summary>
    public sealed class CreditsController : MonoBehaviour
    {
        [Header("Referensi (diisi CreditsSceneBuilder)")]
        public RectTransform viewport;
        public TMP_Text creditsText;
        public GameObject shiftHint;

        [Header("Pengaturan Scroll")]
        [Tooltip("Lama scroll pada kecepatan normal, dalam detik nyata.")]
        [Min(1f)] public float scrollDurationSeconds = 20f;
        [Tooltip("Pengali kecepatan saat tombol SHIFT ditahan.")]
        [Min(1f)] public float fastForwardMultiplier = 3.5f;
        [Tooltip("Posisi istirahat baris terakhir relatif tinggi viewport (0 = bawah, 1 = atas).")]
        [Range(0f, 1f)] public float restingViewportFraction = 0.4f;

        Coroutine scrollRoutine;
        bool scrollFinished;

        void Awake()
        {
            if (viewport == null || creditsText == null)
            {
                Debug.LogWarning(
                    $"{nameof(CreditsController)} pada '{name}' belum punya referensi viewport/creditsText.",
                    this);
                return;
            }

            SetActive(shiftHint, true);
            scrollRoutine = StartCoroutine(ScrollRoutine());
        }

        void Update()
        {
            if (scrollFinished && AnyKeyPressed())
                SceneManager.LoadScene("StartScreen");
        }

        IEnumerator ScrollRoutine()
        {
            // Baris pertama menunggu satu frame supaya layout TMP sempat dihitung
            // sebelum preferredHeight dibaca.
            creditsText.ForceMeshUpdate();
            yield return null;
            creditsText.ForceMeshUpdate();

            float contentHeight = creditsText.preferredHeight;
            float viewportHeight = viewport.rect.height;

            RectTransform contentRect = creditsText.rectTransform;
            float startY = -viewportHeight * 0.5f;
            float restY = viewportHeight * (restingViewportFraction - 0.5f);
            float endY = contentHeight + restY;

            SetContentY(contentRect, startY);

            float elapsed = 0f;
            while (elapsed < scrollDurationSeconds)
            {
                bool fastForward = Keyboard.current != null &&
                    (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);
                elapsed += Time.unscaledDeltaTime * (fastForward ? fastForwardMultiplier : 1f);

                float t = Mathf.Clamp01(elapsed / scrollDurationSeconds);
                SetContentY(contentRect, Mathf.Lerp(startY, endY, t));
                yield return null;
            }

            SetContentY(contentRect, endY);
            scrollFinished = true;
            SetActive(shiftHint, false);
            scrollRoutine = null;
        }

        static void SetContentY(RectTransform rect, float y) =>
            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, y);

        static bool AnyKeyPressed()
        {
            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
                return true;
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                return true;
            return false;
        }

        static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
                target.SetActive(active);
        }
    }
}
