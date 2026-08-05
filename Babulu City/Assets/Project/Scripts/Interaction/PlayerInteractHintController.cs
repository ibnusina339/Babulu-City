using System;
using System.Collections;
using IntegratedApps;
using BabuluCity.Core;
using BabuluCity.SaveSystem;
using LarisID;
using ProdukLM;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

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
    [Tooltip("Hint 'Keluar Kalender' yang sudah ada di Canvas Interact Hint.")]
    [SerializeField] GameObject calendarExitHint;

    [Header("Layar Kalender")]
    [Tooltip("Canvas Kalender yang ditampilkan saat pemain melihat kalender, yaitu GameObject 'Kalender Screen'.")]
    [SerializeField] GameObject calendarViewRoot;

    [Header("Transisi Tidur")]
    [Tooltip("Lama teks 'Hari ke-N telah selesai' ditahan di layar hitam (detik nyata).")]
    [SerializeField, Min(0f)] float sleepMessageSeconds = 5f;
    [SerializeField, Min(0f)] float sleepFadeSeconds = 0.55f;

    [Header("Area Interaksi")]
    [SerializeField] Collider2D laptopArea;
    [SerializeField] Collider2D calendarArea;
    [SerializeField] Collider2D bedArea;
    [SerializeField] Collider2D playerBodyArea;
    [SerializeField, Min(0f)] float nearbyEdgeDistance = 0.35f;
    [SerializeField, Min(0f)] float bedContactTolerance = 0.03f;

    // Statis supaya pemeriksaan akhir permainan hanya berjalan sekali per sesi
    // dan scene ENDING tidak dimuat berulang.
    static bool endingLoaded;

    internal static void ResetEndingGuard() => endingLoaded = false;

    LaptopProximityController laptopController;
    Coroutine sleepTransition;
    bool canSleep;
    bool sleeping;
    bool viewingCalendar;
    CanvasGroup sleepFade;
    GameObject sleepScreenRoot;
    GameObject sleepConfirmPopup;
    GameObject sleepBlackScreen;
    TMP_Text sleepDayText;
    Button confirmSleepButton;
    Button cancelSleepButton;
    Vector2Int lastHintResolution;
    Rect lastSafeArea;
    Vector2 lastCanvasSize;
    float lastCanvasScale = -1f;

    void Awake()
    {
        ResolveReferences();
        ResolveSleepUI();
        ConfigureHintCanvas();
        HideAll();
    }

    void Update()
    {
        RefreshHintLayoutIfNeeded();

        // Ctrl+Shift+Alt+E adalah pintasan menuju ENDING, bukan perintah interaksi.
        bool interactPressed = Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame &&
            !EndingShortcut.ShortcutModifiersHeld;

        if (viewingCalendar)
        {
            // ESC ditangani terpusat lewat EscapeStack supaya satu penekanan
            // tidak sekaligus memunculkan popup keluar game.
            if (interactPressed)
                CloseCalendar();
            return;
        }

        Vector2 playerPosition = transform.position;
        bool desktopOpened = laptopController != null && laptopController.IsLaptopOpened;

        bool showLaptop = !desktopOpened && IsInside(laptopArea, playerPosition);
        bool showCalendar = !desktopOpened && !showLaptop
            && IsNear(calendarArea, playerPosition, nearbyEdgeDistance);
        bool showBed = !desktopOpened && !showLaptop && !showCalendar
            && IsTouching(bedArea, playerBodyArea, bedContactTolerance);
        canSleep = showBed;

        SetActive(laptopHint, showLaptop);
        SetActive(calendarHint, showCalendar);
        SetActive(bedHint, showBed);

        if (!interactPressed)
            return;

        if (showCalendar)
            OpenCalendar();
        else if (canSleep && !sleeping)
            RequestSleep();
    }

    void OpenCalendar()
    {
        if (sleeping || viewingCalendar)
            return;
        if (calendarViewRoot == null)
        {
            Debug.LogWarning(
                $"{nameof(PlayerInteractHintController)} pada '{name}' belum punya referensi " +
                "'Kalender Screen'. Isi field Calendar View Root di Inspector.",
                this);
            return;
        }

        viewingCalendar = true;
        GetComponent<PlayerMovement>()?.StopMovement();
        SetActive(calendarHint, false);
        SetActive(laptopHint, false);
        SetActive(bedHint, false);
        calendarViewRoot.SetActive(true);
        CalendarDayMarksUI marks = calendarViewRoot.GetComponentInChildren<CalendarDayMarksUI>(true);
        if (marks != null)
            marks.Refresh();
        SetActive(calendarExitHint, true);
        EscapeStack.Register(this, EscapeLayer.Screen, CloseCalendar);
    }

    void CloseCalendar()
    {
        SetActive(calendarViewRoot, false);
        SetActive(calendarExitHint, false);
        EscapeStack.Unregister(this);

        if (!viewingCalendar)
            return;

        viewingCalendar = false;
        GetComponent<PlayerMovement>()?.ResumeMovement();
    }

    void RequestSleep()
    {
        if (sleeping)
            return;
        sleeping = true;
        GetComponent<PlayerMovement>()?.StopMovement();
        if (sleepScreenRoot != null)
            sleepScreenRoot.SetActive(true);
        SetActive(sleepConfirmPopup, true);
        SetActive(sleepBlackScreen, false);

        if (sleepConfirmPopup == null)
            Debug.LogWarning(
                "Popup 'Sleep confirm' tidak ditemukan, ESC tidak dapat membatalkan tidur.",
                this);
        else
            EscapeStack.Register(sleepConfirmPopup, EscapeLayer.Popup, CancelSleep);
    }

    void CancelSleep()
    {
        // Konfirmasi tidak boleh dibatalkan setelah transisi tidur berjalan.
        if (sleepTransition != null)
            return;

        EscapeStack.Unregister(sleepConfirmPopup);
        SetActive(sleepConfirmPopup, false);
        if (sleepScreenRoot != null)
            sleepScreenRoot.SetActive(false);
        sleeping = false;
        GetComponent<PlayerMovement>()?.ResumeMovement();
    }

    void ConfirmSleep()
    {
        // Tombol bisa tertekan berkali-kali sebelum popup sempat menutup.
        if (sleepTransition != null)
            return;

        EscapeStack.Unregister(sleepConfirmPopup);
        SetActive(sleepConfirmPopup, false);
        sleepTransition = StartCoroutine(SleepAndStartNextDay());
    }

    IEnumerator SleepAndStartNextDay()
    {
        PlayerMovement movement = GetComponent<PlayerMovement>();
        movement?.StopMovement();
        EnsureSleepFade();
        if (sleepFade == null)
        {
            Debug.LogError(
                "Transisi tidur dibatalkan karena GameObject 'Sleep Black Screen' tidak ditemukan.",
                this);
            sleepTransition = null;
            sleeping = false;
            movement?.ResumeMovement();
            yield break;
        }

        GameClockUI clock = UnityEngine.Object.FindAnyObjectByType<GameClockUI>(FindObjectsInactive.Include);

        // 1. Simpan progress penting sebelum layar berubah.
        GameSaveManager.SaveImportant();

        // 2. Fade in ke black screen.
        SetActive(sleepBlackScreen, true);
        yield return FadeSleep(0f, 1f, sleepFadeSeconds);

        // 3. Teks memakai nomor hari yang baru saja selesai (1 Agustus = Hari ke-1).
        if (sleepDayText != null)
        {
            sleepDayText.text = clock != null
                ? $"Hari ke-{clock.CurrentDayNumber} telah selesai"
                : "Hari telah selesai";
        }

        // 4. Tahan teks memakai waktu nyata agar tidak terpengaruh Time.timeScale.
        yield return new WaitForSecondsRealtime(sleepMessageSeconds);

        // 5. Pindah ke hari berikutnya; BeginNextDay juga mengembalikan jam ke awal hari.
        clock?.BeginNextDay();
        ProjectFlowManager.Instance?.BeginNewGameDay();

        // 6. Kalender ikut tanggal baru.
        foreach (CalendarDayMarksUI marks in UnityEngine.Object.FindObjectsByType<CalendarDayMarksUI>(
                     FindObjectsInactive.Include))
            marks.Refresh();

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

        GameSaveManager.SaveImportant();

        // 7. Permainan berhenti begitu tanggal akhir tercapai. Flag statis
        // menjaga scene ENDING hanya dimuat sekali walaupun coroutine sempat
        // terpanggil lagi.
        if (clock != null && clock.CurrentDate >= GameClockUI.FinalDate && !endingLoaded)
        {
            endingLoaded = true;
            sleepTransition = null;
            SceneManager.LoadScene("ENDING");
            yield break;
        }

        // 8. Fade out dari black screen.
        yield return FadeSleep(1f, 0f, sleepFadeSeconds);
        SetActive(sleepBlackScreen, false);
        if (sleepScreenRoot != null)
            sleepScreenRoot.SetActive(false);

        // 7. Kontrol pemain kembali setelah seluruh transisi selesai.
        sleepTransition = null;
        sleeping = false;
        movement?.ResumeMovement();
    }

    void EnsureSleepFade()
    {
        if (sleepFade != null) return;
        if (sleepBlackScreen != null)
        {
            // GetComponent mengembalikan objek "fake null" di Editor bila
            // komponen tidak ada. Operator ?? tidak memakai operator == milik
            // Unity, sehingga AddComponent tidak pernah dijalankan dan baris
            // berikutnya melempar MissingComponentException.
            if (!sleepBlackScreen.TryGetComponent(out sleepFade))
                sleepFade = sleepBlackScreen.AddComponent<CanvasGroup>();
            sleepFade.alpha = 0f;
            sleepFade.blocksRaycasts = false;
            return;
        }
        // Desain Sleep Black Screen wajib berasal dari hierarchy yang sudah ada.
        // Jangan membuat Canvas/fade pengganti karena layout harus tetap milik desainer.
        Debug.LogError(
            "GameObject 'Sleep Black Screen' tidak ditemukan. Hubungkan referensi UI tidur yang sudah ada.",
            this);
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
        Transform laptop = FindWorldTransform("Laptop");
        // Prefab kalender yang dipasang ke scene ikut membawa objek UI bernama
        // "kalender". Tanpa filter UI, pencarian bisa mendapat panel UI itu
        // (tanpa Collider2D) sehingga area interaksi kalender selalu null dan
        // hint maupun tombol E tidak pernah aktif.
        Transform calendar = FindWorldTransform("calendar", "Kalender");
        Transform bed = FindWorldTransform("kasur");

        laptopController = laptop != null
            ? laptop.GetComponent<LaptopProximityController>()
            : UnityEngine.Object.FindAnyObjectByType<LaptopProximityController>(FindObjectsInactive.Include);

        laptopArea ??= FindPreferredCollider(laptop, "interact", includeRoot: true);
        calendarArea ??= FindPreferredCollider(calendar, "interact", includeRoot: true);
        bedArea ??= FindPreferredCollider(bed, "batas", includeRoot: false);
        playerBodyArea ??= GetComponent<Collider2D>();

        laptopHint ??= FindTransform("Buka Laptop")?.gameObject;
        calendarHint ??= FindTransform("Lihat Kalender")?.gameObject;
        bedHint ??= FindTransform("Tidur")?.gameObject;

        // Hint keluar memakai objek yang sudah ada di Canvas Interact Hint.
        calendarExitHint ??= FindTransform("Keluar Kalender", "Tutup Kalender")?.gameObject;
        calendarViewRoot ??= FindTransform("Kalender Screen")?.gameObject;

        // Area interaksi yang kosong membuat hint dan tombol E diam tanpa
        // pesan apa pun, jadi laporkan sekali di Awake supaya mudah dilacak.
        WarnIfMissing(laptopArea, "Laptop");
        WarnIfMissing(calendarArea, "calendar");
        WarnIfMissing(bedArea, "kasur");
    }

    void WarnIfMissing(Collider2D area, string objectName)
    {
        if (area == null)
            Debug.LogWarning(
                $"{nameof(PlayerInteractHintController)}: area interaksi '{objectName}' tidak ditemukan. " +
                "Pastikan objeknya ada di scene dan punya Collider2D (mis. child 'Interact Box').",
                this);
    }

    void ResolveSleepUI()
    {
        sleepScreenRoot = FindTransform("Sleep Screen")?.gameObject;
        sleepConfirmPopup = FindChild(sleepScreenRoot?.transform, "Sleep confirm")?.gameObject;
        sleepBlackScreen = FindChild(sleepScreenRoot?.transform, "Sleep Black Screen")?.gameObject;
        sleepDayText = FindChild(sleepBlackScreen?.transform, "Hari ke-")?.GetComponent<TMP_Text>();
        confirmSleepButton = EnsureButton(FindChild(sleepConfirmPopup?.transform, "Sleep Button"));
        cancelSleepButton = EnsureButton(FindChild(sleepConfirmPopup?.transform, "Kembali Button"));
        if (confirmSleepButton != null)
        {
            confirmSleepButton.onClick.RemoveListener(ConfirmSleep);
            confirmSleepButton.onClick.AddListener(ConfirmSleep);
        }
        if (cancelSleepButton != null)
        {
            cancelSleepButton.onClick.RemoveListener(CancelSleep);
            cancelSleepButton.onClick.AddListener(CancelSleep);
        }
        SetActive(sleepConfirmPopup, false);
        SetActive(sleepBlackScreen, false);
        if (sleepScreenRoot != null)
            sleepScreenRoot.SetActive(false);
    }

    static Transform FindChild(Transform root, string objectName)
    {
        if (root == null)
            return null;
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child.name.Equals(objectName, StringComparison.OrdinalIgnoreCase))
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

    void ConfigureHintCanvas()
    {
        GameObject hint = laptopHint ?? calendarHint ?? bedHint;
        Canvas canvas = hint != null ? hint.GetComponentInParent<Canvas>(true) : null;
        if (canvas == null)
            return;

        // Canvas "Interact Hint" tersimpan dalam keadaan nonaktif di scene.
        // Anak-anaknya tetap di-SetActive oleh Update, tetapi tidak pernah
        // terlihat karena parent-nya mati. Diaktifkan sebelum pemeriksaan
        // scaler supaya tetap berlaku walau layout-nya berbeda.
        if (!canvas.gameObject.activeSelf)
            canvas.gameObject.SetActive(true);

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        RectTransform canvasRect = canvas.transform as RectTransform;
        if (scaler == null || canvasRect == null)
            return;

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        // Scale nol pada root Canvas membuat posisi anak terlihat bergeser atau
        // bahkan menghilang ketika aspect ratio Game View diganti.
        canvasRect.localScale = Vector3.one;
        Canvas.ForceUpdateCanvases();

        // Ubah titik safe-area dari koordinat layar ke koordinat lokal Canvas.
        // Ini lebih stabil daripada membagi pixel dengan scaleFactor secara manual.
        Rect safe = Screen.safeArea;
        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, safe.min, uiCamera, out Vector2 localSafePoint);
        Vector2 safeOffset = localSafePoint - canvasRect.rect.min;
        PositionHint(laptopHint, safeOffset);
        PositionHint(calendarHint, safeOffset);
        PositionHint(bedHint, safeOffset);
        PositionHint(calendarExitHint, safeOffset);

        lastHintResolution = new Vector2Int(Screen.width, Screen.height);
        lastSafeArea = safe;
        lastCanvasSize = canvasRect.rect.size;
        lastCanvasScale = canvas.scaleFactor;
    }

    void RefreshHintLayoutIfNeeded()
    {
        GameObject hint = laptopHint ?? calendarHint ?? bedHint;
        Canvas canvas = hint != null ? hint.GetComponentInParent<Canvas>(true) : null;
        RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;
        Vector2Int resolution = new Vector2Int(Screen.width, Screen.height);

        if (resolution != lastHintResolution
            || Screen.safeArea != lastSafeArea
            || (canvasRect != null && canvasRect.rect.size != lastCanvasSize)
            || (canvas != null && !Mathf.Approximately(canvas.scaleFactor, lastCanvasScale)))
        {
            ConfigureHintCanvas();
        }
    }

    static void PositionHint(GameObject hint, Vector2 safeBottomLeft)
    {
        if (hint == null || hint.transform is not RectTransform rect)
            return;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = new Vector2(0f, 0f);
        rect.localScale = Vector3.one;
        rect.anchoredPosition = safeBottomLeft + new Vector2(28f, 82f);
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

    static bool IsTouching(Collider2D area, Collider2D playerBody, float tolerance)
    {
        if (area == null || playerBody == null
            || !area.enabled || !playerBody.enabled
            || !area.gameObject.activeInHierarchy || !playerBody.gameObject.activeInHierarchy)
        {
            return false;
        }

        if (playerBody.IsTouching(area))
            return true;

        ColliderDistance2D separation = playerBody.Distance(area);
        return separation.isOverlapped || separation.distance <= tolerance;
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

    /// <summary>
    /// Mencari objek dunia (bukan UI) yang membawa Collider2D. Objek UI memakai
    /// RectTransform sehingga mudah disaring, dan objek dengan collider
    /// diprioritaskan supaya nama yang kebetulan sama tidak salah terambil.
    /// </summary>
    static Transform FindWorldTransform(params string[] acceptedNames)
    {
        Transform fallback = null;

        foreach (Transform candidate in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
        {
            if (candidate is RectTransform)
                continue;

            bool matches = false;
            foreach (string acceptedName in acceptedNames)
            {
                if (candidate.name.Equals(acceptedName, StringComparison.OrdinalIgnoreCase))
                {
                    matches = true;
                    break;
                }
            }

            if (!matches)
                continue;

            if (candidate.GetComponentInChildren<Collider2D>(true) != null)
                return candidate;

            fallback ??= candidate;
        }

        return fallback;
    }

    void HideAll()
    {
        SetActive(laptopHint, false);
        SetActive(calendarHint, false);
        SetActive(bedHint, false);
        SetActive(calendarExitHint, false);
        SetActive(calendarViewRoot, false);
        EscapeStack.Unregister(this);
        viewingCalendar = false;
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
    static void Bootstrap() => SceneBootstrap.RunOnEverySceneLoad(Install);

    static void Install()
    {
        // Sesi permainan baru boleh memicu ending lagi.
        PlayerInteractHintController.ResetEndingGuard();

        PlayerMovement player = UnityEngine.Object.FindAnyObjectByType<PlayerMovement>(FindObjectsInactive.Include);
        if (player != null && player.GetComponent<PlayerInteractHintController>() == null)
            player.gameObject.AddComponent<PlayerInteractHintController>();
    }
}
