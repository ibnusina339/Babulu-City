using System;
using System.Globalization;
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
        [Min(0.01f)] public float realSecondsPerGameMinute = 7.5f;
        public string startDate = "30/07/2026";
        public bool runAutomatically = true;
        public bool useUnscaledTime = true;

        float elapsedRealSeconds;
        DateTime parsedStartDate;
        bool reachedEnd;

        public bool ReachedEnd => reachedEnd;

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

        void RefreshDisplay()
        {
            int startMinutes = startHour * 60;
            int endMinutes = endHour * 60;
            int elapsedGameMinutes = Mathf.FloorToInt(
                elapsedRealSeconds / Mathf.Max(0.01f, realSecondsPerGameMinute));
            int displayedMinutes = Mathf.Min(startMinutes + elapsedGameMinutes, endMinutes);

            reachedEnd = displayedMinutes >= endMinutes;

            int displayHour = displayedMinutes / 60;
            int displayMinute = displayedMinutes % 60;
            bool crossedMidnight = displayHour >= 24;
            if (crossedMidnight)
                displayHour = 0;

            string clockValue = $"{displayHour:00}.{displayMinute:00}";
            string dateValue = parsedStartDate
                .AddDays(crossedMidnight ? 1 : 0)
                .ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

            SetTexts(clockTexts, clockValue);
            SetTexts(dateTexts, dateValue);
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
}
