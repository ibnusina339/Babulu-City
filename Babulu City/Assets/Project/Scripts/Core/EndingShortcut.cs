using BabuluCity.SaveSystem;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace BabuluCity.Core
{
    /// <summary>
    /// Pintasan testing: Ctrl+Shift+Alt+E langsung memuat scene ENDING memakai
    /// progres yang sedang berjalan, tanpa harus bermain sampai 9 Agustus.
    /// Progres disimpan lebih dulu karena EndingController membaca data save.
    /// </summary>
    public sealed class EndingShortcut : MonoBehaviour
    {
        const string EndingScene = "ENDING";

        /// <summary>
        /// Dipakai juga oleh aksi tombol E lain (buka laptop, tidur) supaya
        /// kombinasi pintasan ini tidak ikut memicu aksi tersebut.
        /// </summary>
        public static bool ShortcutModifiersHeld
        {
            get
            {
                Keyboard keyboard = Keyboard.current;
                return keyboard != null &&
                       keyboard.ctrlKey.isPressed &&
                       keyboard.shiftKey.isPressed &&
                       keyboard.altKey.isPressed;
            }
        }

        void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.eKey.wasPressedThisFrame)
                return;
            if (!ShortcutModifiersHeld)
                return;
            if (SceneManager.GetActiveScene().name == EndingScene)
                return;

            if (!Application.CanStreamedLevelBeLoaded(EndingScene))
            {
                Debug.LogError($"Scene '{EndingScene}' belum terdaftar di Build Settings.");
                return;
            }

            // SaveNow hanya berjalan di scene Main, jadi pemanggilan dari scene
            // lain aman diabaikan dan ending memakai save terakhir.
            GameSaveManager.SaveImportant();
            SceneManager.LoadScene(EndingScene);
        }
    }

    static class EndingShortcutBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap() => SceneBootstrap.RunOnEverySceneLoad(Install);

        static void Install()
        {
            if (Object.FindAnyObjectByType<EndingShortcut>() != null)
                return;

            // Host bertahan lintas scene agar pintasan tetap aktif di mana pun.
            var host = new GameObject("Ending Shortcut");
            Object.DontDestroyOnLoad(host);
            host.AddComponent<EndingShortcut>();
        }
    }
}
