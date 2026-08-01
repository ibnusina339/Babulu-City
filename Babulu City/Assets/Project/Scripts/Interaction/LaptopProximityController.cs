using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class LaptopProximityController : MonoBehaviour
{
    [Header("Referensi")]
    [SerializeField] private GameObject desktopRoot;

    [Header("Interaksi")]
    [SerializeField, Min(0.1f)] private float interactionRadius = 2.25f;

    [Header("Transisi")]
    [SerializeField, Min(0f)] private float fadeDuration = 0.35f;
    [SerializeField] private Color fadeColor = Color.black;

    private PlayerMovement playerMovement;
    private CanvasGroup fadeGroup;
    private bool laptopOpened;
    private bool transitioning;

    void Start()
    {
        playerMovement = FindAnyObjectByType<PlayerMovement>();
        CreateFadeOverlay();

        if (desktopRoot != null)
            desktopRoot.SetActive(false);
    }

    void Update()
    {
        if (transitioning || Keyboard.current == null)
            return;

        bool interactPressed = Keyboard.current.eKey.wasPressedThisFrame;
        bool escapePressed = Keyboard.current.escapeKey.wasPressedThisFrame;

        if (laptopOpened)
        {
            if (interactPressed || escapePressed)
                StartCoroutine(SetLaptopOpen(false));

            return;
        }

        if (interactPressed && IsPlayerInRange())
            StartCoroutine(SetLaptopOpen(true));
    }

    bool IsPlayerInRange()
    {
        if (playerMovement == null)
            playerMovement = FindAnyObjectByType<PlayerMovement>();

        if (playerMovement == null)
            return false;

        return Vector2.Distance(playerMovement.transform.position, transform.position)
            <= interactionRadius;
    }

    IEnumerator SetLaptopOpen(bool open)
    {
        transitioning = true;

        if (open)
            playerMovement?.StopMovement();

        yield return Fade(0f, 1f);

        if (desktopRoot != null)
            desktopRoot.SetActive(open);

        laptopOpened = open;
        yield return null;
        yield return Fade(1f, 0f);

        if (!open)
            playerMovement?.ResumeMovement();

        transitioning = false;
    }

    IEnumerator Fade(float from, float to)
    {
        if (fadeGroup == null || fadeDuration <= 0f)
        {
            if (fadeGroup != null)
                fadeGroup.alpha = to;
            yield break;
        }

        fadeGroup.blocksRaycasts = true;
        float elapsed = 0f;
        fadeGroup.alpha = from;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            fadeGroup.alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
            yield return null;
        }

        fadeGroup.alpha = to;
        fadeGroup.blocksRaycasts = to > 0.01f;
    }

    void CreateFadeOverlay()
    {
        var canvasObject = new GameObject("Laptop Fade Canvas");
        canvasObject.transform.SetParent(transform.root, false);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;

        fadeGroup = canvasObject.AddComponent<CanvasGroup>();
        fadeGroup.alpha = 0f;
        fadeGroup.blocksRaycasts = false;
        fadeGroup.interactable = false;

        var imageObject = new GameObject("Fade");
        imageObject.transform.SetParent(canvasObject.transform, false);

        RectTransform rect = imageObject.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = imageObject.AddComponent<Image>();
        image.color = fadeColor;
        image.raycastTarget = true;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}
