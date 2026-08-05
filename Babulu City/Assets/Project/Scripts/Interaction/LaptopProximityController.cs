using System.Collections;
using BabuluCity.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class LaptopProximityController : MonoBehaviour
{
    [Header("Referensi")]
    [SerializeField] private GameObject desktopRoot;

    [Header("Interaksi")]
    [Tooltip("PolygonCollider2D Interact Box yang berada di bawah hierarchy Laptop.")]
    [SerializeField] private Collider2D interactionArea;

    [Header("Transisi")]
    [SerializeField, Min(0f)] private float fadeDuration = 0.35f;
    [SerializeField] private Color fadeColor = Color.black;

    [Header("Penutup Layar Desktop")]
    [SerializeField] private Color desktopBackdropColor = new Color(0.015f, 0.025f, 0.045f, 1f);

    private PlayerMovement playerMovement;
    private CanvasGroup fadeGroup;
    private bool laptopOpened;
    private bool transitioning;

    public bool IsLaptopOpened => laptopOpened;

    void Start()
    {
        playerMovement = FindAnyObjectByType<PlayerMovement>();
        Collider2D hierarchyArea = FindInteractionArea();
        if (hierarchyArea != null)
            interactionArea = hierarchyArea;

        EnsureDesktopBackdrop();
        CreateFadeOverlay();

        if (desktopRoot != null)
            desktopRoot.SetActive(false);
    }

    void Update()
    {
        if (transitioning || Keyboard.current == null)
            return;

        // Ketikan, termasuk huruf E, harus diterima input field dan tidak
        // dianggap sebagai perintah untuk keluar dari desktop.
        if (IsEditingText())
            return;

        // Ctrl+Shift+Alt+E adalah pintasan menuju ENDING, bukan perintah
        // membuka atau menutup laptop.
        bool interactPressed = Keyboard.current.eKey.wasPressedThisFrame &&
                               !EndingShortcut.ShortcutModifiersHeld;

        if (laptopOpened)
        {
            // ESC ditangani EscapeStack agar popup/aplikasi di dalam desktop
            // tertutup lebih dulu sebelum laptopnya sendiri.
            if (interactPressed)
                StartCoroutine(SetLaptopOpen(false));

            return;
        }

        if (interactPressed && IsPlayerInRange())
            StartCoroutine(SetLaptopOpen(true));
    }

    static bool IsEditingText()
    {
        GameObject selected = EventSystem.current?.currentSelectedGameObject;
        if (selected == null)
            return false;

        // GetComponent memberi "fake null" di Editor bila komponen tidak ada,
        // sehingga ?? tidak pernah jatuh ke pencarian parent.
        TMP_InputField input = selected.TryGetComponent(out TMP_InputField field)
            ? field
            : selected.GetComponentInParent<TMP_InputField>();
        return input != null && input.isFocused;
    }

    bool IsPlayerInRange()
    {
        if (playerMovement == null)
            playerMovement = FindAnyObjectByType<PlayerMovement>();

        if (playerMovement == null)
            return false;

        if (interactionArea == null)
            interactionArea = FindInteractionArea();

        if (interactionArea == null)
            return false;

        return interactionArea.OverlapPoint(playerMovement.transform.position);
    }

    Collider2D FindInteractionArea()
    {
        PolygonCollider2D fallback = null;
        foreach (PolygonCollider2D polygon in GetComponentsInChildren<PolygonCollider2D>(true))
        {
            fallback ??= polygon;
            if (polygon.name.Contains("interact", System.StringComparison.OrdinalIgnoreCase)
                || polygon.name.Contains("interac", System.StringComparison.OrdinalIgnoreCase))
                return polygon;
        }

        return fallback;
    }

    void EnsureDesktopBackdrop()
    {
        if (desktopRoot == null)
            return;

        Transform existing = desktopRoot.transform.Find("Desktop Fullscreen Backdrop");
        if (existing != null)
        {
            existing.SetAsFirstSibling();
            return;
        }

        GameObject backdrop = new GameObject(
            "Desktop Fullscreen Backdrop",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        backdrop.layer = desktopRoot.layer;
        backdrop.transform.SetParent(desktopRoot.transform, false);
        backdrop.transform.SetAsFirstSibling();

        RectTransform rect = (RectTransform)backdrop.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        rect.offsetMin = new Vector2(-8f, -8f);
        rect.offsetMax = new Vector2(8f, 8f);

        Image image = backdrop.GetComponent<Image>();
        image.color = desktopBackdropColor;
        image.raycastTarget = false;
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
        if (open)
            EscapeStack.Register(this, EscapeLayer.Screen, CloseLaptopFromEscape);
        else
            EscapeStack.Unregister(this);

        yield return null;
        yield return Fade(1f, 0f);

        if (!open)
            playerMovement?.ResumeMovement();

        transitioning = false;
    }

    void CloseLaptopFromEscape()
    {
        if (!laptopOpened || transitioning)
            return;
        StartCoroutine(SetLaptopOpen(false));
    }

    void OnDisable()
    {
        EscapeStack.Unregister(this);
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
        if (interactionArea != null)
        {
            Bounds bounds = interactionArea.bounds;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
    }
}
