using System;
using System.Collections;
using IntegratedApps;
using LarisID;
using ProdukLM;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Menampilkan teks interaksi yang sudah dibuat di canvas Interact Hint.
/// Area deteksi selalu mengikuti collider objek, bukan posisi UI.
/// </summary>
public sealed class PlayerInteractHintController : MonoBehaviour
{
    [Header("Teks Hint")]
    [SerializeField] GameObject laptopHint;
    [SerializeField] GameObject calendarHint;
    [SerializeField] GameObject bedHint;

    [Header("Area Interaksi")]
    [SerializeField] Collider2D laptopArea;
    [SerializeField] Collider2D calendarArea;
    [SerializeField] Collider2D bedArea;
    [SerializeField, Min(0f)] float nearbyEdgeDistance = 0.35f;

    LaptopProximityController laptopController;
    bool canSleep;
    bool sleeping;
    CanvasGroup sleepFade;

    void Awake()
    {
        ResolveReferences();
        HideAll();
    }

    void Update()
    {
        Vector2 playerPosition = transform.position;
        bool desktopOpened = laptopController != null && laptopController.IsLaptopOpened;

        bool showLaptop = !desktopOpened && IsInside(laptopArea, playerPosition);
        bool showCalendar = !desktopOpened && !showLaptop
            && IsNear(calendarArea, playerPosition, nearbyEdgeDistance);
        bool showBed = !desktopOpened && !showLaptop && !showCalendar
            && IsNear(bedArea, playerPosition, nearbyEdgeDistance);
        canSleep = showBed;

        SetActive(laptopHint, showLaptop);
        SetActive(calendarHint, showCalendar);
        SetActive(bedHint, showBed);

        if (canSleep && !sleeping && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            StartCoroutine(SleepAndStartNextDay());
    }

    IEnumerator SleepAndStartNextDay()
    {
        sleeping = true;
        PlayerMovement movement = GetComponent<PlayerMovement>();
        movement?.StopMovement();
        EnsureSleepFade();

        yield return FadeSleep(0f, 1f, 0.55f);

        GameClockUI clock = UnityEngine.Object.FindAnyObjectByType<GameClockUI>(FindObjectsInactive.Include);
        clock?.BeginNextDay();
        ProjectFlowManager.Instance?.BeginNewGameDay();

        LarisIDManager laris = UnityEngine.Object.FindAnyObjectByType<LarisIDManager>(FindObjectsInactive.Include);
        laris?.EnsureInitialized();
        if (laris != null)
        {
            // CurrentDay milik Laris menunjukkan hari yang sedang berjalan.
            // Setelah tidur pertama, kalender dan Laris sama-sama harus berada
            // di hari ke-2. Jika event penutupan pasar sudah menghitungnya,
            // loop tidak berjalan sehingga penjualan tidak dihitung dua kali.
            int targetDay = clock != null
                ? clock.CurrentDayNumber
                : laris.Marketplace.CurrentDay + 1;
            while (laris.Marketplace.CurrentDay < targetDay)
                laris.SimulateOneDay();
        }

        yield return new WaitForSecondsRealtime(0.25f);
        yield return FadeSleep(1f, 0f, 0.55f);
        movement?.ResumeMovement();
        sleeping = false;
    }

    void EnsureSleepFade()
    {
        if (sleepFade != null) return;
        GameObject canvasObject = new GameObject("Sleep Fade Canvas", typeof(Canvas), typeof(CanvasGroup));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;
        sleepFade = canvasObject.GetComponent<CanvasGroup>();
        sleepFade.blocksRaycasts = false;
        sleepFade.alpha = 0f;

        GameObject fade = new GameObject("Fade", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fade.transform.SetParent(canvasObject.transform, false);
        RectTransform rect = (RectTransform)fade.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        fade.GetComponent<Image>().color = Color.black;
    }

    IEnumerator FadeSleep(float from, float to, float duration)
    {
        sleepFade.blocksRaycasts = true;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            sleepFade.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        sleepFade.alpha = to;
        sleepFade.blocksRaycasts = to > 0.01f;
    }

    void OnDisable()
    {
        HideAll();
    }

    void ResolveReferences()
    {
        Transform laptop = FindTransform("Laptop");
        Transform calendar = FindTransform("calendar", "Kalender");
        Transform bed = FindTransform("kasur");

        laptopController = laptop != null
            ? laptop.GetComponent<LaptopProximityController>()
            : UnityEngine.Object.FindAnyObjectByType<LaptopProximityController>(FindObjectsInactive.Include);

        laptopArea ??= FindPreferredCollider(laptop, "interact", includeRoot: true);
        calendarArea ??= FindPreferredCollider(calendar, "interact", includeRoot: true);
        bedArea ??= FindPreferredCollider(bed, "batas", includeRoot: false);

        laptopHint ??= FindTransform("Buka Laptop")?.gameObject;
        calendarHint ??= FindTransform("Lihat Kalender")?.gameObject;
        bedHint ??= FindTransform("Tidur")?.gameObject;
    }

    static Collider2D FindPreferredCollider(Transform root, string preferredName, bool includeRoot)
    {
        if (root == null)
            return null;

        Collider2D fallback = includeRoot ? root.GetComponent<Collider2D>() : null;
        foreach (Collider2D area in root.GetComponentsInChildren<Collider2D>(true))
        {
            if (!includeRoot && area.transform == root)
                continue;

            fallback ??= area;
            if (area.name.Contains(preferredName, StringComparison.OrdinalIgnoreCase))
                return area;
        }

        return fallback;
    }

    static bool IsInside(Collider2D area, Vector2 point)
    {
        return area != null && area.enabled && area.gameObject.activeInHierarchy
            && area.OverlapPoint(point);
    }

    static bool IsNear(Collider2D area, Vector2 point, float distance)
    {
        if (area == null || !area.enabled || !area.gameObject.activeInHierarchy)
            return false;

        if (area.OverlapPoint(point))
            return true;

        return Vector2.Distance(point, area.ClosestPoint(point)) <= distance;
    }

    static Transform FindTransform(params string[] acceptedNames)
    {
        foreach (Transform candidate in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
        {
            foreach (string acceptedName in acceptedNames)
            {
                if (candidate.name.Equals(acceptedName, StringComparison.OrdinalIgnoreCase))
                    return candidate;
            }
        }

        return null;
    }

    void HideAll()
    {
        SetActive(laptopHint, false);
        SetActive(calendarHint, false);
        SetActive(bedHint, false);
    }

    static void SetActive(GameObject target, bool active)
    {
        if (target != null && target.activeSelf != active)
            target.SetActive(active);
    }
}

static class PlayerInteractHintBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        PlayerMovement player = UnityEngine.Object.FindAnyObjectByType<PlayerMovement>(FindObjectsInactive.Include);
        if (player != null && player.GetComponent<PlayerInteractHintController>() == null)
            player.gameObject.AddComponent<PlayerInteractHintController>();
    }
}
