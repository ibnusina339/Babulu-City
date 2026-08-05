using TMPro;
using BabuluCity.Core;
using BabuluCity.SaveSystem;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BabuluCity.UI
{
    /// <summary>
    /// Menghubungkan tombol StartScreen tanpa bergantung pada nama GameObject desain.
    /// Tombol dikenali dari teksnya agar hierarchy tetap bebas diubah desainer.
    /// </summary>
    public sealed class StartScreenController : MonoBehaviour
    {
        const string StartScreenScene = "StartScreen";
        const string MainScene = "Main";
        GameObject newGamePopup;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap() => SceneBootstrap.RunOnEverySceneLoad(Install);

        static void Install()
        {
            // Controller ini tidak disimpan di dalam scene StartScreen, jadi
            // harus dipasang ulang setiap scene tersebut dimuat. Tanpa itu,
            // kembali dari scene Main membuat tombol Mulai/Lanjutkan mati.
            if (SceneManager.GetActiveScene().name != StartScreenScene)
                return;

            StartScreenController controller = FindAnyObjectByType<StartScreenController>();
            if (controller == null)
            {
                var controllerObject = new GameObject("StartScreen Controller");
                controller = controllerObject.AddComponent<StartScreenController>();
            }

            controller.ConfigureButtons();
        }

        void Awake()
        {
            ConfigureButtons();
        }

        void ConfigureButtons()
        {
            ResolveNewGamePopup();
            Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include);

            foreach (Button button in buttons)
            {
                if (button.gameObject.scene != gameObject.scene)
                    continue;

                TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
                if (label == null)
                    continue;

                string buttonText = label.text.Trim().ToLowerInvariant();
                switch (buttonText)
                {
                    case "mulai game":
                        button.interactable = true;
                        button.onClick.RemoveAllListeners();
                        button.onClick.AddListener(RequestNewGame);
                        break;

                    case "lanjutkan":
                        button.interactable = GameSaveManager.HasSave;
                        button.onClick.RemoveAllListeners();
                        button.onClick.AddListener(GameSaveManager.RequestContinue);
                        break;

                    case "pengaturan":
                        // Tetap tampil, tetapi belum memiliki aksi pada versi ini.
                        button.interactable = true;
                        break;
                }
            }
        }

        void RequestNewGame()
        {
            if (!Application.CanStreamedLevelBeLoaded(MainScene))
            {
                Debug.LogError($"Scene '{MainScene}' belum terdaftar di Build Settings.");
                return;
            }

            if (GameSaveManager.HasSave && newGamePopup != null)
                newGamePopup.SetActive(true);
            else
                GameSaveManager.StartNewGame();
        }

        void ResolveNewGamePopup()
        {
            // "BacktoStartScreen" adalah prefab konfirmasi ESC yang sama dari
            // scene Main, dipakai ulang di sini dengan teks "Mulai Ulang?".
            // Nama GameObject dan tombolnya tetap mengikuti prefab asli.
            newGamePopup = FindTransform("NewGameConfirmPopup")?.gameObject
                ?? FindTransform("BacktoStartScreen")?.gameObject;
            if (newGamePopup == null)
                return;

            Transform panel = FindTransform("keluar konfirm", newGamePopup.transform) ?? newGamePopup.transform;
            Button confirm = FindButton(newGamePopup.transform, "Confirm New Game")
                ?? FindButton(panel, "Keluar BUtton")
                ?? FindButton(panel, "Keluar Button");
            Button cancel = FindButton(newGamePopup.transform, "Cancel New Game")
                ?? FindButton(panel, "Kembali Button");
            if (confirm != null)
            {
                confirm.onClick.RemoveAllListeners();
                confirm.onClick.AddListener(GameSaveManager.StartNewGame);
            }
            if (cancel != null)
            {
                cancel.onClick.RemoveAllListeners();
                cancel.onClick.AddListener(() => newGamePopup.SetActive(false));
            }
            newGamePopup.SetActive(false);
        }

        /// <summary>
        /// GetComponent mengembalikan objek "fake null" di Editor bila komponen
        /// tidak ada, dan operator ?? tidak memakai operator == milik Unity.
        /// Helper ini memastikan hasilnya benar-benar null agar rantai fallback
        /// nama tombol tetap berjalan.
        /// </summary>
        static Button FindButton(Transform root, string objectName)
        {
            Transform target = FindTransform(objectName, root);
            if (target == null || !target.TryGetComponent(out Button button))
                return null;
            return button;
        }

        static Transform FindTransform(string objectName, Transform root = null)
        {
            Transform[] transforms = root != null
                ? root.GetComponentsInChildren<Transform>(true)
                : FindObjectsByType<Transform>(FindObjectsInactive.Include);
            foreach (Transform candidate in transforms)
                if (candidate.name.Equals(objectName, System.StringComparison.OrdinalIgnoreCase))
                    return candidate;
            return null;
        }
    }
}
