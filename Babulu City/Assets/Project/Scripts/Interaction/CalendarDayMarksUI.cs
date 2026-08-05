using System.Collections.Generic;
using System.Globalization;
using IntegratedApps;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Menyalakan tanda silang pada tanggal yang sudah dilewati.
///
/// Anak dari GameObject "Tanda Silang Hari" sudah diberi nama sesuai tanggal
/// ("1", "2", ... "8"), jadi tidak ada UI baru yang dibuat di sini. Tanggal
/// diambil dari <see cref="GameClockUI"/> (Time/Day System), bukan dari jam
/// komputer, sehingga kalender selalu sinkron dengan progres permainan.
/// </summary>
public sealed class CalendarDayMarksUI : MonoBehaviour
{
    [Header("Referensi")]
    [Tooltip("GameObject 'Tanda Silang Hari'. Dibiarkan kosong akan dicari otomatis di bawah objek ini.")]
    [SerializeField] Transform marksRoot;

    [Header("Pengaturan")]
    [Tooltip("Bulan yang ditampilkan kalender. Tanggal di bulan sebelumnya selalu disilang.")]
    [SerializeField, Range(1, 12)] int displayedMonth = 8;
    [Tooltip("Tanggal pertama yang punya Image tanda silang.")]
    [SerializeField, Min(1)] int firstDay = 1;
    [Tooltip("Tanggal terakhir yang punya Image tanda silang.")]
    // Scene berakhir saat tanggal 9 dimulai, jadi X hanya dibutuhkan untuk 1-8.
    [SerializeField, Min(1)] int lastDay = 8;

    GameClockUI gameClock;

    void Awake()
    {
        ResolveReferences();
    }

    void OnEnable()
    {
        // Kalender bisa dibuka kapan saja, jadi selalu segarkan saat tampil.
        SubscribeToClock();
        Refresh();
    }

    void OnDisable()
    {
        if (gameClock != null)
            gameClock.NewDayStarted -= OnNewDayStarted;
    }

    void OnNewDayStarted(int dayNumber) => Refresh();

    /// <summary>
    /// Menyilang setiap tanggal yang lebih kecil dari tanggal berjalan.
    /// Tanggal hari ini sengaja dibiarkan bersih.
    /// </summary>
    public void Refresh()
    {
        ResolveReferences();
        if (marksRoot == null)
        {
            Debug.LogWarning(
                $"{nameof(CalendarDayMarksUI)} pada '{name}' tidak menemukan 'Tanda Silang Hari'. " +
                "Isi field Marks Root di Inspector.",
                this);
            return;
        }

        if (gameClock == null)
        {
            Debug.LogWarning(
                $"{nameof(CalendarDayMarksUI)} pada '{name}' tidak menemukan GameClockUI, " +
                "tanda silang tidak dapat mengikuti hari permainan.",
                this);
            return;
        }

        // Tanggal diambil dari sistem waktu game, bukan DateTime.Now.
        System.DateTime today = gameClock.CurrentDate;
        var found = new HashSet<int>();

        foreach (Transform mark in marksRoot)
        {
            if (!int.TryParse(mark.name, NumberStyles.Integer, CultureInfo.InvariantCulture, out int markedDay))
            {
                Debug.LogWarning(
                    $"Anak '{mark.name}' pada '{marksRoot.name}' bukan angka tanggal, jadi dilewati. " +
                    "Nama objek tanda silang harus berupa tanggal, misalnya \"3\".",
                    mark);
                continue;
            }

            found.Add(markedDay);

            // Hanya tanggal yang sudah lewat yang disilang; hari ini dan
            // tanggal mendatang dibiarkan bersih.
            bool passed = today.Month > displayedMonth ||
                          (today.Month == displayedMonth && markedDay < today.Day);

            if (mark.gameObject.activeSelf != passed)
                mark.gameObject.SetActive(passed);

            if (passed)
                EnsureVisible(mark);
        }

        for (int day = firstDay; day <= lastDay; day++)
        {
            if (!found.Contains(day))
                Debug.LogWarning(
                    $"Image tanda silang untuk tanggal {day} tidak ditemukan di '{marksRoot.name}'. " +
                    $"Tambahkan objek bernama \"{day}\" atau sesuaikan rentang di Inspector.",
                    this);
        }
    }

    /// <summary>
    /// Tanda silang yang aktif tetapi tidak terlihat biasanya disebabkan
    /// parent nonaktif, alpha nol, atau warna transparan. Kondisi itu
    /// dilaporkan sekaligus dipulihkan supaya X benar-benar tampil.
    /// </summary>
    void EnsureVisible(Transform mark)
    {
        // Kalender Screen memang boleh nonaktif saat ditutup. Yang tidak boleh
        // nonaktif adalah container X-nya sendiri, karena state itu tetap akan
        // menyembunyikan semua tanda saat layar kalender dibuka lagi.
        if (!marksRoot.gameObject.activeSelf)
        {
            Debug.LogWarning(
                $"Parent '{marksRoot.name}' nonaktif; diaktifkan agar tanda silang terlihat.",
                marksRoot);
            marksRoot.gameObject.SetActive(true);
        }

        foreach (CanvasGroup group in mark.GetComponentsInChildren<CanvasGroup>(true))
        {
            if (group.alpha >= 0.99f)
                continue;
            Debug.LogWarning($"CanvasGroup pada '{mark.name}' beralpha {group.alpha}; dinaikkan ke 1.", mark);
            group.alpha = 1f;
        }

        Image[] images = mark.GetComponentsInChildren<Image>(true);
        if (images.Length == 0)
            Debug.LogWarning($"Objek tanggal '{mark.name}' tidak memiliki komponen Image X.", mark);
        foreach (Image image in images)
        {
            if (image.color.a >= 0.99f)
                continue;
            Debug.LogWarning($"Image '{mark.name}' transparan (alpha {image.color.a}); dinaikkan ke 1.", mark);
            Color color = image.color;
            color.a = 1f;
            image.color = color;
        }

        // Tanda silang harus berada di depan elemen kalender lain.
        mark.SetAsLastSibling();
    }

    void ResolveReferences()
    {
        if (marksRoot == null)
            marksRoot = FindChild(transform, "Tanda Silang Hari");

        if (gameClock == null)
            gameClock = Object.FindAnyObjectByType<GameClockUI>(FindObjectsInactive.Include);
    }

    void SubscribeToClock()
    {
        if (gameClock == null)
            return;

        gameClock.NewDayStarted -= OnNewDayStarted;
        gameClock.NewDayStarted += OnNewDayStarted;
    }

    static Transform FindChild(Transform root, string objectName)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child.name.Equals(objectName, System.StringComparison.OrdinalIgnoreCase))
                return child;
        return null;
    }
}
