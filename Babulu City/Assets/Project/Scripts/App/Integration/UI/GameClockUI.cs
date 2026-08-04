using System;
using System.Globalization;
using BabuluCity.Core;
using ProdukLM;
using TMPro;
using UnityEngine;

namespace IntegratedApps
{
    /// <summary>
    /// Jam permainan sederhana untuk scene Main. Kecepatannya masih berupa
    /// nilai dummy dan dapat disesuaikan dari Inspector tanpa mengubah UI.
    /// </summary>
    public class GameClockUI : MonoBehaviour
    {
        [Header("Referensi UI")]
        public TMP_Text[] clockTexts;
        public TMP_Text[] dateTexts;

        [Header("Waktu Dummy")]
        [Range(0, 23)] public int startHour = 20;
        [Range(1, 24)] public int endHour = 24;
        [Tooltip("2 detik = rentang 20.00-00.00 selesai dalam 8 menit nyata.")]
        [Min(0.01f)] public float realSecondsPerGameMinute = 2f;
        public string startDate = "02/08/2026";
        public bool runAutomatically = true;
        public bool useUnscaledTime = true;

        float elapsedRealSeconds;
        DateTime parsedStartDate;
        bool reachedEnd;
        int gameDayOffset;

        public bool ReachedEnd => reachedEnd;
        public int CurrentDayNumber => gameDayOffset + 1;
        public float ElapsedRealSeconds => elapsedRealSeconds;
        public int GameDayOffset => gameDayOffset;
        public DateTime CurrentDate => parsedStartDate.AddDays(gameDayOffset);
        public event Action DayEnded;
        public event Action<int> NewDayStarted;

        public int CurrentGameMinutes
        {
            get
            {
                int elapsedGameMinutes = Mathf.FloorToInt(
                    elapsedRealSeconds / Mathf.Max(0.01f, realSecondsPerGameMinute));
                return Mathf.Min(startHour * 60 + elapsedGameMinutes, endHour * 60);
            }
        }

        void Awake()
        {
            ResetClock();
        }

        void Update()
        {
            if (!runAutomatically || reachedEnd)
                return;

            elapsedRealSeconds += useUnscaledTime
                ? Time.unscaledDeltaTime
                : Time.deltaTime;

            RefreshDisplay();
        }

        public void ResetClock()
        {
            elapsedRealSeconds = 0f;
            reachedEnd = false;
            gameDayOffset = 0;

            if (!DateTime.TryParseExact(
                    startDate,
                    "dd/MM/yyyy",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out parsedStartDate))
            {
                parsedStartDate = new DateTime(2026, 7, 30);
            }

            RefreshDisplay();
        }

        public void BeginNextDay()
        {
            gameDayOffset++;
            elapsedRealSeconds = 0f;
            reachedEnd = false;
            // Limit ProdukLM mengikuti hari permainan, bukan tanggal komputer.
            // Dipanggil langsung dari clock supaya tetap reset walaupun window
            // ProdukLM atau ProjectFlowManager sedang nonaktif.
            DailyGenerationCounter.ResetForTesting();
            RefreshDisplay();
            NewDayStarted?.Invoke(gameDayOffset + 1);
        }

        public void RestoreState(int savedDayOffset, float savedElapsedRealSeconds)
        {
            gameDayOffset = Mathf.Clamp(savedDayOffset, 0, 7);
            elapsedRealSeconds = Mathf.Max(0f, savedElapsedRealSeconds);
            reachedEnd = false;
            RefreshDisplay();
        }

        /// <summary>
        /// Memajukan jam permainan untuk aktivitas yang memakan waktu,
        /// misalnya sesi belajar VentraMeet.
        /// </summary>
        public void AdvanceHours(float hours)
        {
            if (hours <= 0f || reachedEnd)
                return;

            elapsedRealSeconds += hours * 60f * Mathf.Max(0.01f, realSecondsPerGameMinute);
            RefreshDisplay();
        }

        void RefreshDisplay()
        {
            int startMinutes = startHour * 60;
            int endMinutes = endHour * 60;
            int elapsedGameMinutes = Mathf.FloorToInt(
                elapsedRealSeconds / Mathf.Max(0.01f, realSecondsPerGameMinute));
            int displayedMinutes = Mathf.Min(startMinutes + elapsedGameMinutes, endMinutes);

            bool wasAtEnd = reachedEnd;
            reachedEnd = displayedMinutes >= endMinutes;

            int displayHour = displayedMinutes / 60;
            int displayMinute = displayedMinutes % 60;
            bool crossedMidnight = displayHour >= 24;
            if (crossedMidnight)
                displayHour = 0;

            string clockValue = $"{displayHour:00}.{displayMinute:00}";
            string dateValue = parsedStartDate
                .AddDays(gameDayOffset + (crossedMidnight ? 1 : 0))
                .ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

            SetTexts(clockTexts, clockValue);
            SetTexts(dateTexts, dateValue);

            if (!wasAtEnd && reachedEnd)
                DayEnded?.Invoke();
        }

        static void SetTexts(TMP_Text[] targets, string value)
        {
            if (targets == null)
                return;

            foreach (TMP_Text target in targets)
            {
                if (target != null)
                    target.text = value;
            }
        }
    }

    static class GameClockBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap() => SceneBootstrap.RunOnEverySceneLoad(RestoreClockIfMissing);

        static void RestoreClockIfMissing()
        {
            GameClockUI existing = UnityEngine.Object.FindAnyObjectByType<GameClockUI>(FindObjectsInactive.Include);
            if (existing != null)
                return;

            TMP_Text clockText = FindText("Jam", "jam");
            TMP_Text dateText = FindText("Tanggal", "tanggal");
            if (clockText == null && dateText == null)
                return;

            Transform uiRoot = clockText != null ? clockText.transform.root : dateText.transform.root;
            GameClockUI clock = uiRoot.gameObject.AddComponent<GameClockUI>();
            clock.clockTexts = clockText != null ? new[] { clockText } : null;
            clock.dateTexts = dateText != null ? new[] { dateText } : null;
            clock.startHour = 20;
            clock.endHour = 24;
            clock.realSecondsPerGameMinute = 2f;
            clock.startDate = "02/08/2026";
            clock.runAutomatically = true;
            clock.useUnscaledTime = true;
            clock.ResetClock();
        }

        static TMP_Text FindText(params string[] acceptedNames)
        {
            foreach (TMP_Text text in UnityEngine.Object.FindObjectsByType<TMP_Text>(
                         FindObjectsInactive.Include))
            {
                foreach (string acceptedName in acceptedNames)
                {
                    if (text.name.Equals(acceptedName, StringComparison.OrdinalIgnoreCase))
                        return text;
                }
            }

            return null;
        }
    }
}
